using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Holocron
{
    // Low-level reader for Empire at War: Forces of Corruption x64 save games (*.PetroglyphFoC64Save).
    // Pure binary parsing with no dependency on the mod data in entities; SaveGameReader maps the result
    // onto the galacticConquest struct.
    //
    // File layout (from SaveLoadManagerClass::Load / Skip_Rich_Save_Game_Header in StarWarsI64.exe):
    //   "RGMH" rich header, 0x2028 bytes; int at +0x14 is the size of the BMP preview that follows it.
    //   Then a sequence of Petroglyph chunks: u32 id, u32 size (bit 31 set = container of child chunks).
    //   Leaf chunks are often lists of micro chunks: u8 id, u8 size, data.
    //
    // Top-level chunks:
    //   1000  save header micro chunks (0 name, 3 local player ID, 5 multiplayer, 6 build number)
    //   1001  GameModeManagerClass: child 1 = manager micro chunks (micro 2 = mode type per mode,
    //         0 galactic / 1 land / 2 space); child 1000+i = game mode i
    //   1002  PlayerListClass: child 1 per player, whose child 0 is PlayerClass::Load_Data_Chunk
    //
    // Galactic game mode chunk:
    //   child 0 = GameModeClass base data, whose child 1 is the GameObjectManagerClass object list
    //   child 1 = galactic data; micro 1 is the campaign (GC) name
    //
    // GameObjectManager child 0x44C = one object:
    //   0x454 leaf       micro 0 = the object's pre-save address (target of saved pointers)
    //   0x459 container  GameObjectClass::Load; child 0x4B0 = core micro chunks:
    //                    0 ID, 1 type name CRC, 2 owner player ID, 4 position (3 floats),
    //                    6 ParentContainerObject pointer, 0x16 AttachParentObject pointer
    //   0x452 container  TransportDataPackClass (present on galactic company transports)
    //   0x450 container  PlanetaryDataPackClass; child 2 = trade links:
    //                    micro 0x16 count, then pairs of 0x18 other planet object ID, 0x17 route name CRC
    //   0x451 container  FleetDataPackClass; child 1 = atomic micro chunks:
    //                    1 fleet name, 2 PlanetOrbited pointer, 3 PlanetEnrouteTo pointer
    public class SaveGameFile
    {
        public const uint RichHeaderMagic = 0x484D4752; // "RGMH"
        public const int RichHeaderSize = 0x2028;

        public string FilePath;
        public string SaveName = "";
        public int LocalPlayerID = -1;
        public int BuildNumber;
        public bool IsMultiplayer;
        public string CampaignName = "";
        public List<SavePlayer> Players = new List<SavePlayer>();
        public List<SaveObject> Objects = new List<SaveObject>();
        public List<string> Warnings = new List<string>();

        public SavePlayer GetPlayer(int id)
        {
            return Players.Find(p => p.ID == id);
        }

        public SavePlayer LocalPlayer => GetPlayer(LocalPlayerID);

        public static SaveGameFile Load(string path)
        {
            SaveGameFile save = new SaveGameFile { FilePath = path };
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16))
            {
                SaveChunkReader reader = new SaveChunkReader(stream);
                save.Parse(reader);
            }
            return save;
        }

        private void Parse(SaveChunkReader reader)
        {
            long start = reader.SkipRichHeader();
            SaveChunk? gameModes = null;

            foreach (SaveChunk chunk in reader.Children(start, reader.Length))
            {
                switch (chunk.ID)
                {
                    case 1000: ParseHeader(reader.ReadMicroChunks(chunk)); break;
                    case 1001: gameModes = chunk; break;
                    case 1002: ParsePlayers(reader, chunk); break;
                }
            }

            if (gameModes is null) throw new InvalidDataException("Save has no game mode data (chunk 1001).");
            ParseGameModes(reader, gameModes.Value);
        }

        private void ParseHeader(List<MicroChunk> micros)
        {
            foreach (MicroChunk micro in micros)
            {
                switch (micro.ID)
                {
                    case 0: SaveName = micro.WideString(); break;
                    case 3: LocalPlayerID = micro.Int32(); break;
                    case 5: IsMultiplayer = micro.Bool(); break;
                    case 6: BuildNumber = micro.Int32(); break;
                }
            }
        }

        private void ParsePlayers(SaveChunkReader reader, SaveChunk playerList)
        {
            foreach (SaveChunk playerChunk in reader.Children(playerList))
            {
                if (playerChunk.ID != 1 || !playerChunk.IsContainer) continue;
                foreach (SaveChunk data in reader.Children(playerChunk))
                {
                    if (data.ID != 0 || data.IsContainer) continue;
                    SavePlayer player = new SavePlayer();
                    foreach (MicroChunk micro in reader.ReadMicroChunks(data))
                    {
                        switch (micro.ID)
                        {
                            case 0: player.Name = micro.WideString(); break;
                            case 1: player.ID = micro.Int32(); break;
                            case 6: player.Faction = micro.String(); break;
                            case 7: player.Credits = micro.Single(); break;
                            case 9: player.TechLevel = micro.Int32(); break;
                            case 0x15: player.IsActive = micro.Bool(); break;
                            case 0x16: player.IsHuman = micro.Bool(); break;
                        }
                    }
                    Players.Add(player);
                    break;
                }
            }
        }

        private void ParseGameModes(SaveChunkReader reader, SaveChunk manager)
        {
            List<int> modeTypes = new List<int>();
            List<SaveChunk> modes = new List<SaveChunk>();
            foreach (SaveChunk chunk in reader.Children(manager))
            {
                if (chunk.ID == 1 && !chunk.IsContainer)
                {
                    foreach (MicroChunk micro in reader.ReadMicroChunks(chunk))
                    {
                        if (micro.ID == 2) modeTypes.Add(micro.Int32());
                    }
                }
                else if (chunk.ID >= 1000 && chunk.IsContainer) modes.Add(chunk);
            }

            // Mode type micro chunks are not always written (galactic-only saves omit them). The galactic
            // mode is the only one whose own chunk 1 carries a campaign name, so fall back to that.
            int galacticIndex = modeTypes.IndexOf(0);
            if (galacticIndex >= 0 && galacticIndex < modes.Count)
            {
                if (ParseGalacticMode(reader, modes[galacticIndex])) return;
            }
            else
            {
                foreach (SaveChunk mode in modes)
                {
                    if (ParseGalacticMode(reader, mode)) return;
                }
            }
            throw new InvalidDataException("Save does not contain a galactic conquest game mode (skirmish or tactical-only save?).");
        }

        // Returns false without changing any state if the mode is not a galactic conquest.
        private bool ParseGalacticMode(SaveChunkReader reader, SaveChunk mode)
        {
            SaveChunk? objectManager = null;
            string campaign = "";
            foreach (SaveChunk chunk in reader.Children(mode))
            {
                if (chunk.ID == 0 && chunk.IsContainer)
                {
                    foreach (SaveChunk baseChunk in reader.Children(chunk))
                    {
                        if (baseChunk.ID == 1 && baseChunk.IsContainer) objectManager = baseChunk;
                    }
                }
                else if (chunk.ID == 1 && !chunk.IsContainer)
                {
                    foreach (MicroChunk micro in reader.ReadMicroChunks(chunk))
                    {
                        if (micro.ID == 1) campaign = micro.String();
                    }
                }
            }

            if (campaign == "" || objectManager is null) return false;
            CampaignName = campaign;
            foreach (SaveChunk chunk in reader.Children(objectManager.Value))
            {
                if (chunk.ID == 0x44C && chunk.IsContainer)
                {
                    SaveObject obj = ParseObject(reader, chunk);
                    if (obj != null) Objects.Add(obj);
                }
            }
            return true;
        }

        private SaveObject ParseObject(SaveChunkReader reader, SaveChunk objectChunk)
        {
            SaveObject obj = new SaveObject();
            bool hasCore = false;
            foreach (SaveChunk chunk in reader.Children(objectChunk))
            {
                switch (chunk.ID)
                {
                    case 0x454:
                        foreach (MicroChunk micro in reader.ReadMicroChunks(chunk))
                        {
                            if (micro.ID == 0) obj.Address = micro.UInt64();
                        }
                        break;
                    case 0x44D: // Load_Core_Data called directly on the chunk (map objects)
                        ParseCore(obj, reader.ReadMicroChunks(chunk));
                        hasCore = true;
                        break;
                    case 0x459:
                        foreach (SaveChunk sub in reader.Children(chunk))
                        {
                            if (sub.ID == 0x4B0 && !sub.IsContainer)
                            {
                                ParseCore(obj, reader.ReadMicroChunks(sub));
                                hasCore = true;
                            }
                        }
                        break;
                    case 0x450:
                        obj.IsPlanet = true;
                        foreach (SaveChunk sub in reader.Children(chunk))
                        {
                            if (sub.ID == 2 && !sub.IsContainer) ParseTradeLinks(obj, reader.ReadMicroChunks(sub));
                        }
                        break;
                    case 0x452:
                        obj.IsTransport = true;
                        break;
                    case 0x451:
                        obj.IsFleet = true;
                        foreach (SaveChunk sub in reader.Children(chunk))
                        {
                            if (sub.ID == 1 && !sub.IsContainer) ParseFleet(obj, reader.ReadMicroChunks(sub));
                        }
                        break;
                }
            }

            if (!hasCore)
            {
                Warnings.Add("Object at 0x" + objectChunk.Start.ToString("X") + " has no core data; skipped.");
                return null;
            }
            return obj;
        }

        private static void ParseCore(SaveObject obj, List<MicroChunk> micros)
        {
            foreach (MicroChunk micro in micros)
            {
                switch (micro.ID)
                {
                    case 0: obj.ID = micro.Int32(); break;
                    case 1: obj.TypeCRC = micro.UInt32(); break;
                    case 2: obj.OwnerID = micro.Int32(); break;
                    case 4:
                        if (micro.Data.Length >= 12)
                        {
                            obj.X = BitConverter.ToSingle(micro.Data, 0);
                            obj.Y = BitConverter.ToSingle(micro.Data, 4);
                            obj.HasPosition = true;
                        }
                        break;
                    case 6: obj.ParentAddress = micro.Pointer(); break;
                    case 0x16: obj.AttachParentAddress = micro.Pointer(); break;
                }
            }
        }

        private static void ParseTradeLinks(SaveObject obj, List<MicroChunk> micros)
        {
            int otherPlanet = -1;
            foreach (MicroChunk micro in micros)
            {
                if (micro.ID == 0x18) otherPlanet = micro.Int32();
                else if (micro.ID == 0x17)
                {
                    obj.TradeLinks.Add(new SaveTradeLink { OtherPlanetID = otherPlanet, RouteCRC = micro.UInt32() });
                    otherPlanet = -1;
                }
            }
        }

        private static void ParseFleet(SaveObject obj, List<MicroChunk> micros)
        {
            foreach (MicroChunk micro in micros)
            {
                switch (micro.ID)
                {
                    case 1: obj.FleetName = micro.WideString(); break;
                    case 2: obj.PlanetOrbitedAddress = micro.Pointer(); break;
                    case 3: obj.PlanetEnrouteToAddress = micro.Pointer(); break;
                }
            }
        }
    }

    public class SavePlayer
    {
        public int ID = -1;
        public string Name = "";
        public string Faction = ""; // Upper-case faction name as stored by the engine, e.g. "REBEL"
        public float Credits;
        public int TechLevel;
        public bool IsActive;
        public bool IsHuman;

        public override string ToString()
        {
            return Name + " (" + Faction + ")";
        }
    }

    public class SaveObject
    {
        public int ID;
        public uint TypeCRC;     // CRC32 of the upper-case object type name
        public int OwnerID = -1; // SavePlayer.ID
        public ulong Address;
        public ulong ParentAddress;       // Container: fleet, transport or planet
        public ulong AttachParentAddress; // Object this one is attached to
        public bool HasPosition;
        public float X;
        public float Y;
        public bool IsPlanet;
        public bool IsFleet;
        public bool IsTransport;
        public string FleetName = "";
        public ulong PlanetOrbitedAddress;
        public ulong PlanetEnrouteToAddress;
        public List<SaveTradeLink> TradeLinks = new List<SaveTradeLink>();
    }

    public struct SaveTradeLink
    {
        public int OtherPlanetID;
        public uint RouteCRC;
    }

    public struct SaveChunk
    {
        public uint ID;
        public bool IsContainer;
        public long Start;     // Offset of the chunk header
        public long DataStart;
        public long DataEnd;
    }

    public struct MicroChunk
    {
        public byte ID;
        public byte[] Data;

        public int Int32() { return Data.Length >= 4 ? BitConverter.ToInt32(Data, 0) : 0; }
        public uint UInt32() { return Data.Length >= 4 ? BitConverter.ToUInt32(Data, 0) : 0; }
        public ulong UInt64() { return Data.Length >= 8 ? BitConverter.ToUInt64(Data, 0) : 0; }
        public float Single() { return Data.Length >= 4 ? BitConverter.ToSingle(Data, 0) : 0; }
        public bool Bool() { return Data.Length >= 1 && Data[0] != 0; }

        // Saved pointers are 8 bytes in the x64 build (4 in editor maps).
        public ulong Pointer() { return Data.Length >= 8 ? UInt64() : UInt32(); }

        public string String()
        {
            return Encoding.GetEncoding(1252).GetString(Data).TrimEnd('\0');
        }

        public string WideString()
        {
            return Encoding.Unicode.GetString(Data, 0, Data.Length & ~1).TrimEnd('\0');
        }
    }

    // Seeks through the chunk tree so only the small parts of a save (often 100MB+) are read.
    public class SaveChunkReader
    {
        private readonly Stream stream;
        private readonly BinaryReader reader;

        public long Length => stream.Length;

        public SaveChunkReader(Stream stream)
        {
            this.stream = stream;
            reader = new BinaryReader(stream);
        }

        public long SkipRichHeader()
        {
            if (stream.Length < 8) throw new InvalidDataException("File is too small to be a save game.");
            stream.Position = 0;
            if (reader.ReadUInt32() != SaveGameFile.RichHeaderMagic) return 0;
            if (stream.Length < SaveGameFile.RichHeaderSize) throw new InvalidDataException("Truncated save game header.");
            stream.Position = 0x14;
            long start = SaveGameFile.RichHeaderSize + (long)reader.ReadUInt32();
            if (start > stream.Length) throw new InvalidDataException("Save game preview image extends past end of file.");
            return start;
        }

        public IEnumerable<SaveChunk> Children(SaveChunk parent)
        {
            if (!parent.IsContainer) throw new InvalidDataException("Chunk 0x" + parent.ID.ToString("X") + " at 0x" + parent.Start.ToString("X") + " is not a container.");
            return Children(parent.DataStart, parent.DataEnd);
        }

        public IEnumerable<SaveChunk> Children(long start, long end)
        {
            long position = start;
            while (position + 8 <= end)
            {
                stream.Position = position;
                uint id = reader.ReadUInt32();
                uint size = reader.ReadUInt32();
                long dataEnd = position + 8 + (size & 0x7FFFFFFF);
                if (dataEnd > end)
                {
                    throw new InvalidDataException("Chunk 0x" + id.ToString("X") + " at 0x" + position.ToString("X") + " overruns its parent (ends 0x" + dataEnd.ToString("X") + ", parent ends 0x" + end.ToString("X") + ").");
                }
                yield return new SaveChunk { ID = id, IsContainer = (size & 0x80000000) != 0, Start = position, DataStart = position + 8, DataEnd = dataEnd };
                position = dataEnd;
            }
        }

        public List<MicroChunk> ReadMicroChunks(SaveChunk chunk)
        {
            if (chunk.IsContainer) throw new InvalidDataException("Chunk 0x" + chunk.ID.ToString("X") + " at 0x" + chunk.Start.ToString("X") + " is a container, not micro chunks.");
            stream.Position = chunk.DataStart;
            byte[] data = reader.ReadBytes(checked((int)(chunk.DataEnd - chunk.DataStart)));
            List<MicroChunk> micros = new List<MicroChunk>();
            int position = 0;
            while (position + 2 <= data.Length)
            {
                byte id = data[position];
                int size = data[position + 1];
                if (position + 2 + size > data.Length)
                {
                    throw new InvalidDataException("Micro chunk 0x" + id.ToString("X") + " at 0x" + (chunk.DataStart + position).ToString("X") + " overruns chunk 0x" + chunk.ID.ToString("X") + ".");
                }
                byte[] payload = new byte[size];
                Buffer.BlockCopy(data, position + 2, payload, 0, size);
                micros.Add(new MicroChunk { ID = id, Data = payload });
                position += 2 + size;
            }
            return micros;
        }
    }
}
