using System;
using System.Collections.Generic;
using System.Linq;

namespace Holocron
{
    // Loads an x64 FoC save game into the native galacticConquest struct, resolving the CRCs and player IDs
    // in the save against the parsed mod data in entities (Planets, objects, factions, Routes, Conquests).
    // A save is rejected with SaveGameRejectedException unless its galactic conquest name exactly matches a
    // campaign in entities.Conquests (ignoring case, which the engine does not preserve).
    //
    // The result describes the save as a single-perspective GC: factionsPlayable holds the local player's
    // faction, and index 0 of forceOwner / forceLocation / forceType lists every unit and structure on the
    // map, like Starting_Forces. Each planet's forces are ordered with the planet owner's objects first,
    // because Holocron takes a planet's owner from the first force at that location; planetObjects[].owner is
    // also set directly from the save. Fleets in hyperspace are listed at the planet they are travelling to,
    // and ground companies are listed instead of the transports that carry them on the galaxy map.
    // Thrown when a readable save cannot be loaded against the current mod data, e.g. its galactic conquest
    // is not defined (renamed, removed, or a different mod).
    public class SaveGameRejectedException : Exception
    {
        public SaveGameRejectedException(string message) : base(message) { }
    }

    public static class SaveGameReader
    {
        public class Result
        {
            public SaveGameFile Save;
            public galacticConquest Conquest;
            public string MatchedCampaign = ""; // Codename of that entry (merged GCs keep the first faction's name)
            public int UnknownTypes;        // Objects whose type CRC matched no planet or unit in entities
            public int UnresolvedLocations; // Units that are not at, orbiting or travelling to any planet
            public List<string> Warnings = new List<string>();
        }

        public static Result Load(string path, entities entities)
        {
            return Convert(SaveGameFile.Load(path), entities);
        }

        public static Result Convert(SaveGameFile save, entities entities)
        {
            if (crcGlobals.crcTable[1] == 0) crcGlobals.initTable();

            Result result = new Result { Save = save };
            result.Warnings.AddRange(save.Warnings);

            Dictionary<uint, planet> planetTypes = new Dictionary<uint, planet>();
            foreach (planet p in entities.Planets)
            {
                if (string.IsNullOrEmpty(p.codename)) continue;
                uint crc = TypeCRC(p.codename);
                if (!planetTypes.ContainsKey(crc)) planetTypes.Add(crc, p);
            }

            Dictionary<uint, string> unitTypes = new Dictionary<uint, string>();
            foreach (unit u in entities.objects)
            {
                if (string.IsNullOrEmpty(u.unitname)) continue;
                uint crc = TypeCRC(u.unitname);
                if (!unitTypes.ContainsKey(crc)) unitTypes.Add(crc, u.unitname);
            }

            Dictionary<uint, tradeRoute> routeTypes = new Dictionary<uint, tradeRoute>();
            foreach (tradeRoute r in entities.Routes)
            {
                if (string.IsNullOrEmpty(r.name)) continue;
                uint crc = TypeCRC(r.name);
                if (!routeTypes.ContainsKey(crc)) routeTypes.Add(crc, r);
            }

            // Player ID -> faction codename as spelled in entities (the save stores upper case).
            Dictionary<int, string> playerFactions = new Dictionary<int, string>();
            foreach (SavePlayer player in save.Players)
            {
                playerFactions[player.ID] = MatchFaction(player.Faction, entities);
            }

            Dictionary<ulong, SaveObject> byAddress = new Dictionary<ulong, SaveObject>();
            Dictionary<int, SaveObject> byID = new Dictionary<int, SaveObject>();
            foreach (SaveObject obj in save.Objects)
            {
                if (obj.Address != 0 && !byAddress.ContainsKey(obj.Address)) byAddress.Add(obj.Address, obj);
                if (!byID.ContainsKey(obj.ID)) byID.Add(obj.ID, obj);
            }

            // Planets present in the save, keyed by object.
            Dictionary<SaveObject, planet> savePlanets = new Dictionary<SaveObject, planet>();
            foreach (SaveObject obj in save.Objects)
            {
                if (!obj.IsPlanet) continue;
                if (planetTypes.TryGetValue(obj.TypeCRC, out planet p))
                {
                    p.owner = OwnerFaction(obj.OwnerID, playerFactions, entities);
                    savePlanets.Add(obj, p);
                }
                else
                {
                    result.UnknownTypes++;
                    result.Warnings.Add("Planet object " + obj.ID + " has unknown type CRC 0x" + obj.TypeCRC.ToString("X8") + ".");
                }
            }

            string localFaction = playerFactions.TryGetValue(save.LocalPlayerID, out string lf) ? lf : "";
            galacticConquest matched = MatchCampaign(save.CampaignName, entities);
            if (matched.codename is null)
            {
                throw new SaveGameRejectedException("Save \"" + save.SaveName + "\" is from galactic conquest " + save.CampaignName + ", which is not in the loaded mod data.");
            }
            result.MatchedCampaign = matched.codename;

            // Planet order: the campaign's Locations order when known, then any others in save order.
            List<planet> planetObjects = new List<planet>();
            HashSet<string> added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (matched.planets != null)
            {
                foreach (string name in matched.planets)
                {
                    foreach (planet p in savePlanets.Values)
                    {
                        if (string.Equals(p.codename, name, StringComparison.OrdinalIgnoreCase) && added.Add(p.codename)) planetObjects.Add(p);
                    }
                }
            }
            foreach (planet p in savePlanets.Values)
            {
                if (added.Add(p.codename)) planetObjects.Add(p);
            }
            Dictionary<string, int> planetOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < planetObjects.Count; i++) planetOrder[planetObjects[i].codename] = i;

            List<tradeRoute> routes = BuildTradeRoutes(savePlanets, byID, routeTypes, result);

            // A ground company in galactic mode is held by its company transport (e.g. a landing craft).
            // Like Starting_Forces, list the company rather than the transport, unless no contents are known.
            HashSet<ulong> transportsWithKnownContents = new HashSet<ulong>();
            foreach (SaveObject obj in save.Objects)
            {
                if (obj.ParentAddress != 0 && unitTypes.ContainsKey(obj.TypeCRC) && byAddress.TryGetValue(obj.ParentAddress, out SaveObject container) && container.IsTransport)
                {
                    transportsWithKnownContents.Add(container.Address);
                }
            }
            List<SaveObject> planetList = savePlanets.Keys.ToList();

            // Forces: every non-planet, non-fleet object whose type is a known unit or structure.
            List<ForceEntry> forces = new List<ForceEntry>();
            foreach (SaveObject obj in save.Objects)
            {
                if (obj.IsPlanet || obj.IsFleet) continue;
                if (!unitTypes.TryGetValue(obj.TypeCRC, out string typeName))
                {
                    if (!planetTypes.ContainsKey(obj.TypeCRC)) result.UnknownTypes++;
                    continue;
                }
                if (obj.IsTransport && transportsWithKnownContents.Contains(obj.Address)) continue;
                SaveObject location = ResolvePlanet(obj, byAddress, planetList);
                if (location is null || !savePlanets.TryGetValue(location, out planet at))
                {
                    result.UnresolvedLocations++;
                    continue;
                }
                string owner = playerFactions.TryGetValue(obj.OwnerID, out string f) ? f : "";
                forces.Add(new ForceEntry
                {
                    Owner = owner,
                    Location = at.codename,
                    Type = typeName,
                    PlanetIndex = planetOrder[at.codename],
                    OwnsPlanet = string.Equals(owner, at.owner.codename, StringComparison.OrdinalIgnoreCase),
                    SaveOrder = forces.Count,
                });
            }
            forces.Sort((a, b) =>
            {
                int c = a.PlanetIndex.CompareTo(b.PlanetIndex);
                if (c == 0) c = b.OwnsPlanet.CompareTo(a.OwnsPlanet);
                if (c == 0) c = a.SaveOrder.CompareTo(b.SaveOrder);
                return c;
            });

            List<string> factionsPresent = new List<string>();
            foreach (planet p in planetObjects)
            {
                if (!string.IsNullOrEmpty(p.owner.codename) && !factionsPresent.Contains(p.owner.codename)) factionsPresent.Add(p.owner.codename);
            }
            foreach (ForceEntry force in forces)
            {
                if (force.Owner != "" && !factionsPresent.Contains(force.Owner)) factionsPresent.Add(force.Owner);
            }


            result.Conquest = new galacticConquest
            {
                codename = save.CampaignName,
                username = save.SaveName + " [" + matched.username + "]",
                campaign_set = matched.campaign_set,
                desc = matched.desc,
                planets = planetObjects.Select(p => p.codename).ToArray(),
                traderoutes = routes.Select(r => r.name).ToArray(),
                factionsPlayable = new List<string> { localFaction },
                factionsPresent = factionsPresent,
                StoryPlots = matched.StoryPlots != null ? new List<string>(matched.StoryPlots) : new List<string>(),
                forceOwner = new List<List<string>> { forces.Select(x => x.Owner).ToList() },
                forceLocation = new List<List<string>> { forces.Select(x => x.Location).ToList() },
                forceType = new List<List<string>> { forces.Select(x => x.Type).ToList() },
                Type = matched.Type,
                planetObjects = planetObjects,
                traderouteObjects = routes,
            };

            if (result.UnknownTypes > 0) result.Warnings.Add(result.UnknownTypes + " objects have types not found in the loaded mod data.");
            if (result.UnresolvedLocations > 0) result.Warnings.Add(result.UnresolvedLocations + " units could not be placed at a planet.");
            return result;
        }

        private struct ForceEntry
        {
            public string Owner;
            public string Location;
            public string Type;
            public int PlanetIndex;
            public bool OwnsPlanet;
            public int SaveOrder;
        }

        // CRC32 of the upper-case name, as GameObjectTypeManagerClass::Find_Object_Type expects.
        public static uint TypeCRC(string name)
        {
            return Text_Entry.calculateCRC(name.ToUpperInvariant());
        }

        private static string MatchFaction(string saveFaction, entities entities)
        {
            faction match = entities.factions.FirstOrDefault(f => string.Equals(f.codename, saveFaction, StringComparison.OrdinalIgnoreCase));
            return match.codename ?? saveFaction;
        }

        private static faction OwnerFaction(int playerID, Dictionary<int, string> playerFactions, entities entities)
        {
            faction owner = default(faction);
            if (playerFactions.TryGetValue(playerID, out string codename)) owner = SharedFunctions.FactionFromCodeName(codename, entities);
            if (string.IsNullOrEmpty(owner.codename)) owner = entities.factions.FirstOrDefault(f => string.Equals(f.codename, "Neutral", StringComparison.OrdinalIgnoreCase));
            return owner;
        }

        // Galactic objects that sit at a planet without being contained by it share its position.
        private const float PlanetPositionTolerance = 1.0f;

        // Walks the ParentContainerObject chain (unit -> company transport -> fleet -> planet), falling back
        // to the attach parent. A fleet without a parent is between planets, so its orbited planet or else
        // its destination is used. Objects with neither are matched to a planet by position.
        private static SaveObject ResolvePlanet(SaveObject obj, Dictionary<ulong, SaveObject> byAddress, List<SaveObject> planets)
        {
            SaveObject current = obj;
            for (int depth = 0; depth < 32 && current != null; depth++)
            {
                if (current.IsPlanet) return current;
                if (current.ParentAddress != 0 && byAddress.TryGetValue(current.ParentAddress, out SaveObject parent))
                {
                    current = parent;
                    continue;
                }
                if (current.IsFleet)
                {
                    if (current.PlanetOrbitedAddress != 0 && byAddress.TryGetValue(current.PlanetOrbitedAddress, out SaveObject orbited)) return orbited;
                    if (current.PlanetEnrouteToAddress != 0 && byAddress.TryGetValue(current.PlanetEnrouteToAddress, out SaveObject destination)) return destination;
                }
                if (current.AttachParentAddress != 0 && byAddress.TryGetValue(current.AttachParentAddress, out SaveObject attachedTo))
                {
                    current = attachedTo;
                    continue;
                }
                return PlanetAtPosition(current, planets);
            }
            return null;
        }

        private static SaveObject PlanetAtPosition(SaveObject obj, List<SaveObject> planets)
        {
            if (!obj.HasPosition) return null;
            SaveObject best = null;
            float bestDistance = PlanetPositionTolerance * PlanetPositionTolerance;
            foreach (SaveObject p in planets)
            {
                if (!p.HasPosition) continue;
                float dx = p.X - obj.X, dy = p.Y - obj.Y;
                float distance = dx * dx + dy * dy;
                if (distance <= bestDistance)
                {
                    best = p;
                    bestDistance = distance;
                }
            }
            return best;
        }

        // Finds the GC whose XML Campaign Name equals the save's campaign name. parseGCs merges campaigns that
        // share a Campaign_Set and layout into the first one's entry, so every merged name in campaignNames is
        // checked as well as the codename. The engine stores the name upper-cased, so case is ignored; there is
        // no other fuzzy matching.
        private static galacticConquest MatchCampaign(string campaign, entities entities)
        {
            Func<string, bool> same = name => string.Equals(name, campaign, StringComparison.OrdinalIgnoreCase);
            return entities.Conquests.FirstOrDefault(c => same(c.codename) || (c.campaignNames != null && c.campaignNames.Any(same)));
        }

        // Each route is stored on both of its planets; keep one entry per route. Routes missing from the mod
        // data are kept under a synthetic name so the map still draws them.
        private static List<tradeRoute> BuildTradeRoutes(Dictionary<SaveObject, planet> savePlanets, Dictionary<int, SaveObject> byID, Dictionary<uint, tradeRoute> routeTypes, Result result)
        {
            List<tradeRoute> routes = new List<tradeRoute>();
            HashSet<uint> seenRoutes = new HashSet<uint>();
            HashSet<string> seenPairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<SaveObject, planet> entry in savePlanets)
            {
                foreach (SaveTradeLink link in entry.Key.TradeLinks)
                {
                    if (!byID.TryGetValue(link.OtherPlanetID, out SaveObject otherObj) || !savePlanets.TryGetValue(otherObj, out planet other)) continue;
                    string a = entry.Value.codename, b = other.codename;
                    string pair = string.CompareOrdinal(a.ToUpperInvariant(), b.ToUpperInvariant()) < 0 ? a + "|" + b : b + "|" + a;
                    if (!seenPairs.Add(pair) || !seenRoutes.Add(link.RouteCRC)) continue;

                    if (routeTypes.TryGetValue(link.RouteCRC, out tradeRoute route))
                    {
                        // Keep the route's own planet strings so codename comparisons in Holocron match.
                        routes.Add(route);
                    }
                    else
                    {
                        result.Warnings.Add("Trade route CRC 0x" + link.RouteCRC.ToString("X8") + " (" + a + " - " + b + ") is not in the loaded mod data.");
                        routes.Add(new tradeRoute { name = a + "_" + b, planets = new[] { a, b } });
                    }
                }
            }
            return routes;
        }
    }
}
