using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using System.Drawing;

//https://modtools.petrolution.net/docs/MegFileFormat 
//todo not finding cin_projectiles in vanilla, not handiling weapons swap that defines two projectiles
//ideally read the first and set linked obejct of swap ability to second, enable somehow

//todo make slightly altered unittocompanies for gunship squadrons. Gunships should probably be in space units
//alternatively, make it operate on the full list rather than sorted sublists

//Recurse fighter files if they have inheritance
//Store conditions: tech level (also xml), era, research, faction/alias. When evaluating fighter count use 0, not researched, and native or (Empire slot if it doesn't exist)
//Generate the relevant constraints (always faction) for the subunit view (e.g. if Bwing/E, then add it to research dropdown) and let settings be done accordingly
//STANDARD fighters are simply shown as is but file contents are on lookup page. Until I get ambitious enough to parse the files. For arbitrary globals with global name as constraint?
//Standard can be taken into account on subsquad by simply seeing if the file contains the unit name + "_squadron" or don't search quotes. Or even better, search for contents of subunit listbox, which tells you fighter squadrons that conatin it
//For fighter units search all squadrons, limit squadron types to exacty name/ STANARD_HALF if half
//Affil also controls subsquad where used box. Hero "affils" will updated by custom esque expansion anyway
//add unit/hero/structure radio to where used. Go ahead and parse the latter two? Hero can cover ground and space with the open tab telling me which

public static class SharedFunctions
{
    public static string UpOneFolder(string Path)
    {
        return Path.Substring(0, Path.LastIndexOf("\\"));
    }

    public static string LastFolderOrFile(string Path)
    {
        return Path.Substring(Path.LastIndexOf("\\") + 1, Path.Length - Path.LastIndexOf("\\") - 1);
    }

    public static string Extension(string Path)
    {
        return Path.Substring(Path.LastIndexOf(".") + 1, Path.Length - Path.LastIndexOf(".") - 1);
    }

    public static string RemoveTopLevelFolder(string Path)
    {
        return Path.Substring(Path.IndexOf("\\") + 1, Path.Length - Path.IndexOf("\\") - 1);
    }

    public static byte[] getFileFromMegs(string corePath, entities entities)
    {
        string upper = corePath.ToUpper();
        byte[] corenne = new byte[0];
        int hash = LookupUntemplateID(corePath); //todo benchmark aganst the usual hash lookup
        int fileIndex = entities.MEGentries.FindIndex(x => x.hash == hash);
        if (fileIndex > -1)
        {
            bool loopit = true;
            while (loopit)
            {
                int readhash = entities.MEGentries[fileIndex].hash;
                if (hash == readhash)
                {
                    if (upper == entities.MEGentries[fileIndex].filename)
                    {
                        loopit = false; //found matching hash and string
                        MEGentry entry = entities.MEGentries[fileIndex];
                        int megindex = entry.MEGid;
                        int start = entry.startindex;
                        int len = entry.length;
                        corenne = new byte[len];
                        Buffer.BlockCopy(entities.MEGdata[megindex], start, corenne, 0, len);
                    }
                    else fileIndex++; //Hash collision, see if the next matches
                }
                else
                {
                    loopit = false;
                    fileIndex = -1; //there were only collisions
                }
            }
        }

        return corenne;
    }

    public static XmlDocument readModXmlOrMeg(string corepath, entities entities) //todo probably want a Lua/txt version eventually
    {
        XmlDocument doc = new XmlDocument();

        string filepath = getModFile(corepath);
        if (filepath != "") doc.Load(filepath);
        else
        {
            byte[] megfile = SharedFunctions.getFileFromMegs(corepath, entities);
            if(megfile.Length > 0)
            {
                doc.Load(new MemoryStream(megfile));
            }
            else
            {
                entities.readerrors += "\n" + corepath + "cannot be found in loose files or megs";
            }
        }

        return doc;
    }

    public static string getModFile(string corepath)
    {
        string corenne = "";
        foreach (string modpath in entities.modpaths)
        {
            string test = Path.Combine(modpath, corepath);
            if (File.Exists(test))
            {
                corenne = test;
                break;
            }
        }
        return corenne;
    }

    public static List<string> getModFiles(string corepath, string extension)
    {
        List<string> corenne = new List<string>();
        List<string> prefound = new List<string>();
        for (int i = 0; i < entities.modpaths.Count; i++)
        {
            string test = Path.Combine(entities.modpaths[i], corepath);
            string[] tests = new string[0];
            try
            {
                tests = Directory.GetFiles(test, extension, SearchOption.AllDirectories);
            }
            catch { } //It cannot be guaranteed that the subdirectory exists in all mods, but it should exist in some if the modpaths are set reasonably
            foreach (string file in tests)
            {
                string truncated = file.Replace(entities.modpaths[i], "");
                if (i == 0 || !prefound.Contains(truncated))
                {
                    prefound.Add(truncated);
                    corenne.Add(file);
                }
            }
        }
        return corenne;
    }

    public static string Find_Text_Entry(string textid)
    {
        foreach (Text_Entry entry in entities.Text)
        {
            if (entry.identifier == textid)
            {
                return entry.entry;
            }
        }
        return textid;
    }

    public static string fullTrim(string input)
    {
        return input.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");
    }

    public static string[] ReadWhiteSpaceAsCommas(string input)
    {
        string corenne = input.Trim().Replace(" ", ",").Replace("\t", ",").Replace("\r", ",").Replace("\n", ",");
        while (corenne.Contains(",,"))
        {
            corenne = corenne.Replace(",,", ",");
        }
        return corenne.Trim(',').Split(',');
    }

    public static string SerializeStringArray(string[] strings)
    {
        string corenne = "";
        bool furst = true;
        foreach(string str in strings)
        {
            if (furst) furst = false;
            else corenne += ", ";
            corenne += str;
        }
        return corenne;
    }

    public static string SerializeStringArray(List<string> strings)
    {
        string corenne = "";
        bool furst = true;
        foreach (string str in strings)
        {
            if (furst) furst = false;
            else corenne += ", ";
            corenne += str;
        }
        return corenne;
    }

    public static int[] ReadXMLCSV(string input)
    {
        string[] split = input.Split(',');
        int[] corenne = new int[split.Length];
        for (int i = 0; i < split.Length; i++)
        {
            corenne[i] = Convert.ToInt32(split[i].Trim());
        }
        return corenne;
    }
    public static string[] ReadXMLCSString(string input)
    {
        string[] split = input.Split(',');
        string[] corenne = new string[split.Length];
        for (int i = 0; i < split.Length; i++)
        {
            corenne[i] = split[i].Trim();
        }
        return corenne;
    }
    public static string ArmorTypeString(string input)
    {
        string corenne = input.Substring(input.IndexOf("_") + 1, input.Length - input.IndexOf("_") - 1).Replace("_", " ");
        corenne = System.Text.RegularExpressions.Regex.Replace(corenne, "[A-Z]", " $0");

        if (corenne == "") return corenne;
        return " (" + corenne.Substring(1, corenne.Length - 1) + ")";
    }

    public static string[] SplitXMLWhitespaceList (string list)
    {
        string trimmedtt = list.Trim();
        trimmedtt = trimmedtt.Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
        while (trimmedtt.Contains("  ")) trimmedtt = trimmedtt.Replace("  ", " ");
        return trimmedtt.Split(' ');
    }

    public static faction FactionFromCodeName(string codename, entities entities)
    {
         return entities.factions.FirstOrDefault(s => s.codename == codename);
    }

    public static string FactionNameFromCode(string codename, entities entities)
    {
        if (codename == "CCoGM") return codename;
        faction fac = entities.factions.FirstOrDefault(s => s.codename == codename);
        return fac.textname;
    }

    public static float getWeapMultiplier(string type, WeaponMods weap, bool shield)
    {
        if (!(weap.weaponType is null))
        {
            List<ArmorMod> src = weap.HpMods;
            if (shield) src = weap.ShieldMods;
            foreach (ArmorMod armor in src)
            {
                if (armor.armorType == type) return armor.modifier;
            }
        }
        return 1;
    }

    public static WeaponMods GetWeaponMods(string type)
    {
        foreach (WeaponMods weaps in entities.ArmorMods)
        {
            if (weaps.weaponType == type)
            {
                return weaps;
            }
        }
        return new WeaponMods();
    }

    public static float getArmorMultiplier(string type, ArmorMods arms)
    {
        if(!(arms.armorType is null))
        {
            foreach (ArmorMod weapon in arms.WeaponMods)
            {
                if (weapon.armorType == type) return weapon.modifier;
            }
        }
        return 1;
    }

    public static ArmorMods GetArmorMods(string type)
    {
        foreach (ArmorMods arms in entities.ArmorIndexedMods)
        {
            if (arms.armorType == type)
            {
                return arms;
            }
        }
        return new ArmorMods();
    }

    public static void parseContainer(XmlNode unit, entities entities)
    {
        string name = unit.Attributes[0].Value;
        /*string garrison_type = "";
        int garrison_value = -1;

        XmlNode value = unit.SelectSingleNode("descendant::Garrison_Category");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                garrison_type = value.LastChild.Value;
            }
        }
        value = unit.SelectSingleNode("descendant::Garrison_Value");
        if (!(value is null))
        {
            garrison_value = Int32.Parse(value.LastChild.Value);
        }

        container contain = new container
        {
            name = name,
            garrison_value = garrison_value,
            garrison_type = garrison_type,
        };

        entities.containers.Add(contain);*/
    }

    public static bool parseUnit(XmlNode unit, entities entities, string file)
    {
        if (unit.LocalName == "Planet") return true;
        //Todo Eclipse hps failed completely for unclear reasons when using LastChild rather than InnerText. May want to switch all fields over?
        string name = unit.Attributes[0].Value;
        //System.Xml.XmlNode value = unit.SelectSingleNode("descendant::Ship_Class");
        //if (!(value is null)){ //Filters out fighters but can also cause problems with gunships
        //    string check = value.LastChild.Value.ToLower();
        //    if (check == "fighter" || check == "bomber") return;
        //}
        string variant = "";
        string username = "";
        string shield_type = "";
        string armor_type = "";
        string[] planets = new string[0];
        string unitclass = "";
        string tooltip = "";
        string icon = "";
        string model = "";
        string transport = "";
        string garrison_type = "";
        string locomotor_type = "";
        string container = "";
        string bombingRunUnit = "";
        List<string> terrainMaps = new List<string>();
        int crew = -1;
        int shield = -1;
        float regen = -1;
        int hp = -1;
        float speed = -1;
        float min_speed = -1;
        float accel = -1;
        float turn = -1;
        float range = -1;
        int techlevel = -1;
        bool hero = false;
        int locked = -1;
        int cost = -1;
        int buildtime = -1;
        int skirmcost = -1;
        float skirmbuildtime = -1;
        float maintenance = -1;
        float cp = -1;
        int limit_concurrent = -1;
        int limit_lifetime = -1;
        int pop = -1;
        int percompany = -1;
        int gui_row = -1;
        int garrison_slots = -1;
        int garrison_value = -1;
        string reqstructures = null;
        string reqorbit = "";
        string BTS = "";
        hardpoint builtin = new hardpoint();
        List<string> categories = new List<string>();
        List<string> affiliations = new List<string>();
        List<string> companyunits = new List<string>();
        string[] Hardpoints = new string[0];
        List<string> flags = new List<string>();
        List<string> behaviors = new List<string>();
        List<string> modebehaviors = new List<string>();
        List<ability> abilities = new List<ability>();
        List<unitability> unitabilities = new List<unitability>();
        List<garrison_entry> garrison = new List<garrison_entry>();

        System.Xml.XmlNode value = unit.SelectSingleNode("descendant::Tech_Level");
        if (!(value is null))
        {
            techlevel = Int32.Parse(value.LastChild.Value);
        }
        value = unit.SelectSingleNode("descendant::Build_Initially_Locked");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                if (value.LastChild.Value.ToLower() == "yes") locked = 1; //May need to check "true" as well?
                else locked = 0; //Distinguish explicitly unlocked vs undefined = unlcoked for sufficiently complex variant chains
            }
        }
        value = unit.SelectSingleNode("descendant::Text_ID");
        if (!(value is null))
        {
            if (!(value.LastChild is null)) username = value.LastChild.Value;
        }
        value = unit.SelectSingleNode("descendant::Build_Cost_Credits");
        if (!(value is null))
        {
            cost = Int32.Parse(value.LastChild.Value);
        }
        value = unit.SelectSingleNode("descendant::Build_Time_Seconds");
        if (!(value is null))
        {
            buildtime = Int32.Parse(value.LastChild.Value);
        }
        value = unit.SelectSingleNode("descendant::Tactical_Build_Cost_Multiplayer");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                string trimmed = value.LastChild.Value.Trim();
                if (trimmed != "") skirmcost = Int32.Parse(value.LastChild.Value);
            }
        }
        value = unit.SelectSingleNode("descendant::Tactical_Build_Time_Seconds");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                string trimmed = value.LastChild.Value.Trim();
                if (trimmed != "") skirmbuildtime = Single.Parse(value.LastChild.Value);
            }
        }
        value = unit.SelectSingleNode("descendant::Population_Value");
        if (!(value is null))
        {
            pop = Int32.Parse(value.LastChild.Value);
        }
        value = unit.SelectSingleNode("descendant::AI_Combat_Power");
        if (!(value is null))
        {
            if (!(value.LastChild is null)) cp = Single.Parse(value.LastChild.Value);
        }
        value = unit.SelectSingleNode("descendant::Maintenance_Cost");
        if (!(value is null))
        {
            if (!(value.LastChild is null)) maintenance = Single.Parse(value.LastChild.Value);
        }
        XmlNodeList values = unit.SelectNodes("descendant::Company_Units");
        foreach (XmlNode val in values)
        {
            if (!(val is null))
            {
                XmlNode comp = val.LastChild;
                if (!(comp is null))
                {
                    string[] units = ReadWhiteSpaceAsCommas(comp.Value);
                    if (percompany < 0) percompany = units.Length;
                    else percompany += units.Length;
                    foreach (string incompany in units)
                    {
                        if(incompany != "") companyunits.Add(incompany);
                    }
                }
            }
        }
        values = unit.SelectNodes("descendant::Squadron_Units");
        foreach(XmlNode val in values)
        {
            if (!(val is null))
            {
                XmlNode comp = val.LastChild;
                if (!(comp is null))
                {
                    string[] units = ReadWhiteSpaceAsCommas(comp.Value);
                    if (percompany < 0) percompany = units.Length;
                    else percompany += units.Length;
                    foreach (string incompany in units)
                    {
                        if (incompany != "") companyunits.Add(incompany);
                    }
                }
            }
        }
        for(int tech = 0; tech <= 5; tech++)
        {
            values = unit.SelectNodes("descendant::Starting_Spawned_Units_Tech_"+ tech.ToString());
            foreach (XmlNode val in values)
            {
                if (!(val is null))
                {
                    XmlNode spawn = val.LastChild;
                    if (!(spawn is null))
                    {
                        string[] data = ReadWhiteSpaceAsCommas(spawn.Value);
                        garrison_entry gar = new garrison_entry
                        {
                            unitname = data[0],
                            parsingupfront = Int32.Parse(data[1]),
                            parsingreserve = 0,
                            parsingtech = tech,
                        };
                        garrison.Add(gar);
                    }
                }
            }
            values = unit.SelectNodes("descendant::Reserve_Spawned_Units_Tech_" + tech.ToString());
            foreach (XmlNode val in values)
            {
                if (!(val is null))
                {
                    XmlNode spawn = val.LastChild;
                    if (!(spawn is null))
                    {
                        string[] data = ReadWhiteSpaceAsCommas(spawn.Value);
                        for(int i = 0; i < garrison.Count; i++)
                        {
                            garrison_entry gar = garrison[i];
                            if(gar.parsingtech == tech && gar.unitname == data[0])
                            {
                                gar.parsingreserve = Int32.Parse(data[1]);
                                garrison[i] = gar;
                                break;
                            }
                        }
                    }
                }
            }
        }
        value = unit.SelectSingleNode("descendant::Land_Model_Name");
        if (!(value is null))
        {
            if (!(value.LastChild is null)) model = value.LastChild.Value;
        }
        values = unit.SelectNodes("descendant::Land_Terrain_Model_Mapping");
        if(values.Count > 0)
        {
            foreach (XmlNode val in values)
            {
                {
                    XmlNode comp = val.LastChild;
                    if (!(comp is null))
                    {
                        string[] maps = fullTrim(comp.Value).Split(',');
                        foreach (string map in maps) terrainMaps.Add(map);
                    }
                }
            }
        }
        value = unit.SelectSingleNode("descendant::Affiliation");
        if (!(value is null))
        {
            XmlNode aff = value.LastChild;
            if (!(aff is null))
            {
                string[] split = fullTrim(aff.Value).Split(',');
                foreach (string affil in split)
                {
                    affiliations.Add(affil);
                }
            }
        }
        value = unit.SelectSingleNode("descendant::HardPoints");
        if (!(value is null))
        {
            Hardpoints = ReadWhiteSpaceAsCommas(value.InnerText);
        }
        /*if (name == "T4A_Company") //parse debug
        {
            bool visible = false;
        }*/
        value = unit.SelectSingleNode("descendant::Required_Special_Structures");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                reqstructures = value.LastChild.Value;
            }
            else reqstructures = "";
        }
        value = unit.SelectSingleNode("descendant::Required_Orbiting_Units");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                reqorbit = value.LastChild.Value;
            }
        }
        value = unit.SelectSingleNode("descendant::Shield_Armor_Type");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                shield_type = fullTrim(value.LastChild.Value);
            }
        }
        value = unit.SelectSingleNode("descendant::Armor_Type");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                armor_type = fullTrim(value.LastChild.Value);
            }
        }
        value = unit.SelectSingleNode("descendant::Encyclopedia_Unit_Class");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                unitclass = value.LastChild.Value;
                string[] crewarray = unitclass.Split(' ');
                string crewst = crewarray[crewarray.Length - 1];
                int.TryParse(crewst.Replace(",", ""), out crew);
                unitclass = unitclass.Trim();
            }
        }
        value = unit.SelectSingleNode("descendant::Required_Planets");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                planets = ReadWhiteSpaceAsCommas(value.LastChild.Value);
            }
        }
        value = unit.SelectSingleNode("descendant::Encyclopedia_Text");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                tooltip = value.LastChild.Value;
            }
        }
        value = unit.SelectSingleNode("descendant::Icon_Name");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                icon = value.LastChild.Value;
            }
        }
        value = unit.SelectSingleNode("descendant::Land_Bomber_Type");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                bombingRunUnit = value.LastChild.Value;
            }
        }
        value = unit.SelectSingleNode("descendant::Shield_Points");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                shield = Int32.Parse(value.LastChild.Value);
            }
        }
        value = unit.SelectSingleNode("descendant::Shield_Refresh_Rate");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                regen = float.Parse(value.LastChild.Value);
            }
        }
        value = unit.SelectSingleNode("descendant::Tactical_Health");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                hp = Int32.Parse(value.LastChild.Value);
            }
        }
        value = unit.SelectSingleNode("descendant::CategoryMask");
        if (!(value is null))
        {
            XmlNode mask = value.LastChild;
            if (!(mask is null))
            {
                string[] split = fullTrim(mask.Value).Split('|');
                foreach (string msk in split)
                {
                    categories.Add(msk);
                }
            }
        }
        value = unit.SelectSingleNode("descendant::GUI_Row");
        if (!(value is null))
        {
            gui_row = Int32.Parse(value.LastChild.Value);
        }
        value = unit.SelectSingleNode("descendant::Build_Limit_Current_Per_Player");
        if (!(value is null))
        {
            if(!(value.LastChild is null)) limit_concurrent = Int32.Parse(value.LastChild.Value);
        }
        value = unit.SelectSingleNode("descendant::Build_Limit_Lifetime_Per_Player");
        if (!(value is null))
        {
            limit_lifetime = Int32.Parse(value.LastChild.Value);
        }
        value = unit.SelectSingleNode("descendant::Max_Speed");
        if (!(value is null))
        {
            speed = Single.Parse(value.LastChild.Value);
        }
        value = unit.SelectSingleNode("descendant::Min_Speed");
        if (!(value is null))
        {
            min_speed = Single.Parse(value.LastChild.Value);
        }
        value = unit.SelectSingleNode("descendant::OverrideAcceleration");
        if (!(value is null))
        {
            accel = Single.Parse(value.LastChild.Value);
        }
        value = unit.SelectSingleNode("descendant::Max_Rate_Of_Turn");
        if (!(value is null))
        {
            turn = Single.Parse(value.LastChild.Value);
        }
        value = unit.SelectSingleNode("descendant::BTS");
        if (!(value is null))
        {
            BTS = value.InnerText.Trim().Replace("\t","");
        }
        value = unit.SelectSingleNode("descendant::Targeting_Max_Attack_Distance");
        if (!(value is null))
        {
            range = Single.Parse(value.LastChild.Value);
        }
        value = unit.SelectSingleNode("descendant::Company_Transport_Unit");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                transport = value.LastChild.Value;
            }
        }
        value = unit.SelectSingleNode("descendant::Garrison_Category");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                garrison_type = value.LastChild.Value;
            }
        }
        value = unit.SelectSingleNode("descendant::Num_Garrison_Slots");
        if (!(value is null))
        {
            garrison_slots = Int32.Parse(value.LastChild.Value);
        }
        value = unit.SelectSingleNode("descendant::Garrison_Value");
        if (!(value is null))
        {
            garrison_value = Int32.Parse(value.LastChild.Value);
        }
        value = unit.SelectSingleNode("descendant::Type");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                locomotor_type = value.LastChild.Value;
            }
        }
        value = unit.SelectSingleNode("descendant::Is_Named_Hero");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                if(value.LastChild.Value.ToLower().Contains("yes")) hero = true;
            }
        }
        else
        {
            value = unit.SelectSingleNode("descendant::Is_Generic_Hero");
            if (!(value is null))
            {
                if (!(value.LastChild is null))
                {
                    if (value.LastChild.Value.ToLower().Contains("yes")) hero = true;
                }
            }
        }
        value = unit.SelectSingleNode("descendant::Create_Team_Type");
        if (!(value is null))
        {
            if (!(value.LastChild is null))
            {
                container = value.LastChild.Value.Trim();
            }
        }
        value = unit.SelectSingleNode("descendant::Property_Flags");
        if (!(value is null))
        {
            XmlNode mask = value.LastChild;
            if (!(mask is null))
            {
                string[] split = fullTrim(mask.Value).Split('|');
                foreach (string flg in split)
                {
                    flags.Add(flg);
                }
            }
        }
        value = unit.SelectSingleNode("descendant::Behavior");
        if (!(value is null))
        {
            XmlNode mask = value.LastChild;
            if (!(mask is null))
            {
                string[] split = fullTrim(mask.Value).Split(',');
                foreach (string behave in split)
                {
                    behaviors.Add(behave);
                }
            }
        }
        value = unit.SelectSingleNode("descendant::LandBehavior");
        if (!(value is null))
        {
            XmlNode mask = value.LastChild;
            if (!(mask is null))
            {
                string[] split = fullTrim(mask.Value).Split(',');
                foreach (string behave in split)
                {
                    modebehaviors.Add(behave); //Shouldn't need space and land behaviors separately, unless heroes prove annoying in the future
                }
            }
        }
        value = unit.SelectSingleNode("descendant::SpaceBehavior");
        if (!(value is null))
        {
            XmlNode mask = value.LastChild;
            if (!(mask is null))
            {
                string[] split = fullTrim(mask.Value).Split(',');
                foreach (string behave in split)
                {
                    modebehaviors.Add(behave);
                }
            }
        }
        value = unit.SelectSingleNode("descendant::Projectile_Types");
        if (!(value is null))
        {
            string proj = value.InnerText.Trim().ToLower();
            builtin.name = "biw";
            builtin.hpType = "BUILT-IN_WEAPON";
            builtin.hp = -1;
            builtin.targetable = false;
            builtin.quantity = 1;
            builtin.range = range;
            if (proj.Contains("_grenade"))
            {
                builtin.range = 250; //Todo: read gravity tags properly instead of hardcoding
            }
            builtin.projectile = proj;
            builtin.inaccuracyAmounts = new List<float>();
            builtin.inaccuracyTypes = new List<string>();
            bool notfound = true;
            int index = LookupUntemplateID(proj);
            if (index < entities.projectilehashes.Count)
            {
                for (int j = 0; j < entities.projectilehashes[index].Count; j++)
                {
                    projectile project = entities.projectiles[entities.projectilehashes[index][j]];
                    if (project.name.ToLower() == proj)
                    {
                        builtin.damageType = project.damageType;
                        builtin.damageAmount = project.damageAmount;
                        builtin.blastRadius = project.blastRadius;
                        notfound = false;
                        break;
                    }
                }
            }
            if (notfound && proj != "") entities.readerrors += "\nCannot find projectile type " + value.InnerText.Trim() + " for unit " + name;
            value = unit.SelectSingleNode("descendant::Projectile_Fire_Recharge_Seconds");
            if (!(value is null))
            {
                builtin.recharge = Single.Parse(value.LastChild.Value);
            }
            value = unit.SelectSingleNode("descendant::Projectile_Fire_Pulse_Count");
            if (!(value is null))
            {
                builtin.pulseCount = Single.Parse(value.LastChild.Value);
            }
            value = unit.SelectSingleNode("descendant::Projectile_Fire_Pulse_Delay_Seconds");
            if (!(value is null))
            {
                builtin.pulseDelay = Single.Parse(value.LastChild.Value);
            }
            value = unit.SelectSingleNode("descendant::Fire_Category_Restrictions");
            if (!(value is null))
            {
                XmlNode res = value.LastChild;
                if (!(res is null))
                {
                    string[] split = fullTrim(res.Value).Split(',');
                    foreach (string restrict in split)
                    {
                        builtin.inaccuracyTypes.Add(restrict);
                        builtin.inaccuracyAmounts.Add(100);
                    }
                }
            }
            XmlNodeList inaccs = unit.SelectNodes("descendant::Targeting_Fire_Inaccuracy");
            foreach (XmlNode inacc in inaccs)
            {
                if (!(inacc is null))
                {
                    if (!(inacc.LastChild is null))
                    {
                        string[] vals = fullTrim(inacc.LastChild.Value).Split(',');
                        if (vals.Length > 1)
                        {
                            builtin.inaccuracyTypes.Add(vals[0]);
                            builtin.inaccuracyAmounts.Add(float.Parse(vals[1]));
                        }
                    }
                }
            }
        }
        value = unit.SelectSingleNode("descendant::Unit_Abilities_Data");
        if (!(value is null))
        {
            XmlNodeList abilityNodes = value.SelectNodes("descendant::Unit_Ability");
            foreach (XmlNode able in abilityNodes)
            {
                unitability forlist = new unitability();
                forlist.icon = "";
                forlist.username = "";
                forlist.ability = "";
                forlist.desc = "";
                forlist.damageMod = 1;
                forlist.defenseMod = 1;
                forlist.reloadMod = 1;
                forlist.shieldMod = 1;
                forlist.speedMod = 1;

                if (!(able is null))
                {
                    XmlNode abdata = able.SelectSingleNode("descendant::Type");
                    if (!(abdata is null))
                    {
                        if (!(abdata.LastChild is null)) forlist.type = abdata.LastChild.Value;
                        abdata = able.SelectSingleNode("descendant::Alternate_Name_Text");
                        if (!(abdata is null) && !(abdata.LastChild is null)) forlist.username = abdata.LastChild.Value;
                        else
                        {//These seem to be consistent, unlike icon names
                            forlist.username = "TEXT_TOOLTIP_ABILITY_" + forlist.type + "_NAME";
                        }
                        forlist.username = Find_Text_Entry(forlist.username);
                        abdata = able.SelectSingleNode("descendant::Alternate_Description_Text");
                        if (!(abdata is null) && !(abdata.LastChild is null)) forlist.desc = abdata.LastChild.Value;
                        else
                        {
                            forlist.desc = "TEXT_TOOLTIP_ABILITY_" + forlist.type + "_DESCRIPTION";
                        }
                        abdata = able.SelectSingleNode("descendant::GUI_Activated_Ability_Name");
                        if (!(abdata is null) && !(abdata.LastChild is null)) forlist.ability = abdata.LastChild.Value.Trim();
                        abdata = able.SelectSingleNode("descendant::Recharge_Seconds");
                        if (!(abdata is null) && !(abdata.LastChild is null)) forlist.recharge = Single.Parse(abdata.LastChild.Value.Replace("f", "").Replace("F", ""));
                        abdata = able.SelectSingleNode("descendant::Expiration_Seconds");
                        if (!(abdata is null) && !(abdata.LastChild is null)) forlist.expiration = Single.Parse(abdata.LastChild.Value.Replace("f", "").Replace("F", ""));
                        abdata = able.SelectSingleNode("descendant::Effective_Radius");
                        if (!(abdata is null) && !(abdata.LastChild is null)) forlist.radius = Single.Parse(abdata.LastChild.Value.Replace("f", "").Replace("F", ""));
                        abdata = able.SelectSingleNode("descendant::Damage_Percent_When_Activated");
                        if (!(abdata is null) && !(abdata.LastChild is null)) forlist.selfdamage = Single.Parse(abdata.LastChild.Value.Replace("f", "").Replace("F", ""));
                        XmlNodeList mods = able.SelectNodes("descendant::Mod_Multiplier");
                        foreach (XmlNode mod in mods)
                        {
                            if (!(mod is null))
                            {
                                if (!(mod.LastChild is null))
                                {
                                    string[] vals = fullTrim(mod.LastChild.Value).Split(',');
                                    if (vals.Length > 1)
                                    {
                                        float mult = float.Parse(vals[1].Replace("f", "").Replace("F", ""));
                                        switch (vals[0])
                                        {
                                            case "WEAPON_DELAY_MULTIPLIER":
                                                forlist.reloadMod *= mult;
                                                break;
                                            case "FIRE_RATE_MULTIPLIER":
                                                forlist.reloadMod /= mult;
                                                break;
                                            case "SHIELD_REGEN_MULTIPLIER":
                                                forlist.shieldMod = mult;
                                                break;
                                            case "SHIELD_REGEN_INTERVAL_MULTIPLIER": //Technically this is not what it does, but the end math is the same
                                                forlist.shieldMod /= mult;
                                                break;
                                            case "TAKE_DAMAGE_MULTIPLIER":
                                                forlist.defenseMod = mult;
                                                break;
                                            case "CAUSE_DAMAGE_MULTIPLIER":
                                                forlist.damageMod = mult;
                                                break;
                                            case "SPEED_MULTIPLIER":
                                                forlist.speedMod = mult;
                                                break;
                                        }
                                    }
                                }
                            }
                        }

                        abdata = able.SelectSingleNode("descendant::Alternate_Icon_Name");
                        if (!(abdata is null) && !(abdata.LastChild is null)) forlist.icon = abdata.LastChild.Value;
                        else
                        {
                            switch (forlist.type)
                            { //Could do a default block for the nontrivial number that are simply I_SA_ namehere .tga, but this is already set up
                                case "AREA_EFFECT_CONVERT":
                                    forlist.icon = "I_SA_JOIN_ME.TGA";
                                    break;
                                case "AREA_EFFECT_HEAL":
                                    forlist.icon = "I_SA_AREA_HEAL.TGA";
                                    break;
                                case "AREA_EFFECT_STUN":
                                    forlist.icon = "I_SA_ELECTRONIC_SCRAMBLE.TGA";
                                    break;
                                case "BARRAGE":
                                    forlist.icon = "I_SA_BARRAGE_AREA.TGA";
                                    break;
                                case "BERSERKER":
                                    forlist.icon = "I_SA_BERSERKER.TGA";
                                    break;
                                case "BLAST":
                                    forlist.icon = "I_SA_BLAST.TGA";
                                    break;
                                case "BUZZ_DROIDS":
                                    forlist.icon = "I_SA_BUZZ_DROIDS.TGA";
                                    break;
                                case "CABLE_ATTACK":
                                    forlist.icon = "I_SA_TOW_CABLE_ATTACK.TGA";
                                    break;
                                case "CAPTURE_VEHICLE":
                                    forlist.icon = "I_SA_CAPTURE_VEHICLES.TGA";
                                    break;
                                case "CLUSTER_BOMB":
                                    forlist.icon = "I_SA_CLUSTER_BOMB.TGA";
                                    break;
                                case "CONCENTRATE_FIRE":
                                    forlist.icon = "I_SA_ALL_SHIPS_CONCENTRATE_FIRE.TGA";
                                    break;
                                case "CORRUPT_SYSTEMS":
                                    forlist.icon = "I_SA_CORRUPT_SYSTEMS.TGA";
                                    break;
                                case "DEFEND":
                                    forlist.icon = "I_SA_DEFEND_MODE.TGA";
                                    break;
                                case "DEPLOY":
                                    forlist.icon = "I_SA_DEPLOY.TGA"; //TODO figure out when to use I_SA_DEPLOY_SQUAD.TGA
                                    break;//Randomly switch to I_SA_UNDEPLOY.TGA for the easter egg?
                                case "DEPLOY_TROOPERS":
                                    forlist.icon = "I_SA_DEPLOY_STORMTROOPERS.TGA";
                                    break;
                                case "DETONATE_REMOTE_BOMB":
                                    forlist.icon = "I_SA_DETONATE_REMOTE_BOMB.TGA";
                                    break;
                                case "DRAIN_LIFE":
                                    forlist.icon = "I_SA_DRAIN_LIFE.TGA";
                                    break;
                                case "EJECT_VEHICLE_THIEF":
                                    forlist.icon = "I_SA_CAPTURE_VEHICLES.TGA"; //Not entirely sure if this is right
                                    break;
                                case "ENERGY_WEAPON":
                                    forlist.icon = "I_SA_FIRE_ENERGY_WEAPON.TGA";
                                    break;
                                case "FIRE_LOBBING_SUPERWEAPON":
                                    forlist.icon = "";
                                    break;
                                case "FLAME_THROWER":
                                    forlist.icon = "I_SA_FLAME_THROWER.TGA";
                                    break;
                                case "FORCE_CLOAK":
                                    forlist.icon = "I_SA_FORCE_CLOAK.TGA";
                                    break;
                                case "FORCE_CONFUSE":
                                    forlist.icon = "I_SA_FORCE_CONFUSE.TGA";
                                    break;
                                case "FORCE_LIGHTNING":
                                    forlist.icon = "I_SA_FORCE_LIGHTING.TGA";
                                    break;
                                case "FORCE_SIGHT":
                                    forlist.icon = "I_SA_FORCE_SIGHT.TGA";
                                    break;
                                case "FORCE_TELEKINESIS":
                                    forlist.icon = "I_SA_FORCE_CRUSH.TGA";
                                    break;
                                case "FORCE_WHIRLWIND":
                                    forlist.icon = "I_SA_FORCE_PUSH.TGA";
                                    break;
                                case "FOW_REVEAL_PING":
                                    forlist.icon = "I_SA_SENSOR_PING.TGA";
                                    break;
                                case "FULL_SALVO":
                                    forlist.icon = "I_SA_FULL_SALVO.TGA";
                                    break;
                                case "HARMONIC_BOMB":
                                    forlist.icon = "I_SA_HARMONIC_BOMB.TGA";
                                    break;
                                case "HUNT":
                                    forlist.icon = "I_SA_HUNT.TGA";
                                    break;
                                case "INFECTION":
                                    forlist.icon = "I_SA_INFECTION.TGA";
                                    break;
                                case "INTERDICT":
                                    forlist.icon = "I_SA_INTERDICT.TGA";
                                    break;
                                case "INVULNERABILITY":
                                    forlist.icon = "I_SA_EVASIVE_MANEUVERS.TGA";
                                    break;
                                case "ION_CANNON_SHOT":
                                    forlist.icon = "I_SA_ION_CANNON_SHOT.TGA";
                                    break;
                                case "JET_PACK":
                                    forlist.icon = "I_SA_JETPACK_JUMP.TGA";
                                    break;
                                case "LASER_DEFENSE":
                                    forlist.icon = "I_SA_LASER_DEFENSE.TGA";
                                    break;
                                case "LEECH_SHIELDS":
                                    forlist.icon = "I_SA_LEECH_SHIELDS.TGA";
                                    break;
                                case "LUCKY_SHOT":
                                    forlist.icon = "I_SA_LUCKY_SHOT.TGA";
                                    break;
                                case "LURE":
                                    forlist.icon = "I_SA_LURE.TGA";
                                    break;
                                case "MAXIMUM_FIREPOWER":
                                    forlist.icon = "I_SA_MAXIMUM_FIREPOWER.TGA";
                                    break;
                                case "MISSILE_SHIELD":
                                    forlist.icon = "I_SA_MISSILE_JAMMER.TGA"; //todo sort out I_SA_MISSILE_JAMMING.TGA
                                    break;
                                case "PLACE_REMOTE_BOMB":
                                    forlist.icon = "I_SA_STICKY_BOMB.TGA";
                                    break;
                                case "POWER_TO_WEAPONS":
                                    forlist.icon = "I_SA_POWER_TO_WEAPONS.TGA";
                                    break;
                                case "PROXIMITY_MINES":
                                    forlist.icon = "I_SA_PROXIMITY_MINES.TGA";
                                    break;
                                case "RADIOACTIVE_CONTAMINATE":
                                    forlist.icon = "I_SA_CONTAMINATE.TGA";
                                    break;
                                case "REPLENISH_WINGMEN":
                                    forlist.icon = "I_SA_COVER_ME.TGA";
                                    break;
                                case "ROCKET_ATTACK":
                                    forlist.icon = "I_SA_ROCKET_ATTACK.TGA";
                                    break;
                                case "SELF_DESTRUCT":
                                    forlist.icon = "I_SA_SELF_DESTRUCT.TGA";
                                    break;
                                case "SABER_THROW":
                                    forlist.icon = "I_SA_SABER_THROW.TGA";
                                    break;
                                case "SENSOR_JAMMING":
                                    forlist.icon = "I_SA_SENSOR_JAMMING.TGA";
                                    break;
                                case "SHIELD_FLARE":
                                    forlist.icon = "I_SA_SHIELD_FLARE.TGA";
                                    break;
                                case "SPOILER_LOCK":
                                    forlist.icon = "I_SA_S_FOIL_MODE.TGA";
                                    break;
                                case "SPREAD_OUT":
                                    forlist.icon = "I_SA_SPREAD_OUT.TGA";
                                    break;
                                case "SPRINT":
                                    forlist.icon = "I_SA_SPRINT.TGA";
                                    break;
                                case "STICKY_BOMB":
                                    forlist.icon = "I_SA_STICKY_BOMB.TGA";
                                    break;
                                case "STIM_PACK":
                                    forlist.icon = "I_SA_STIM_PACK.TGA";
                                    break;
                                case "STEALTH":
                                    forlist.icon = "I_SA_STEALTH.TGA";
                                    break;
                                case "STUN":
                                    forlist.icon = "I_SA_STUN.TGA";
                                    break;
                                case "SUMMON":
                                    forlist.icon = "I_SA_SUMMON.TGA";
                                    break;
                                case "SWAP_WEAPONS":
                                    forlist.icon = "I_SA_FORCE_CLOAK.TGA";
                                    break;
                                case "TARGETED_HACK":
                                    forlist.icon = "I_SA_HACK_TURRET.TGA";
                                    break;
                                case "TACTICAL_BRIBE":
                                    forlist.icon = "I_SA_TACTICAL_BRIBE.TGA";
                                    break;
                                case "TARGETED_INVULNERABILITY":
                                    forlist.icon = "I_SA_FORCE_PROTECT.TGA";
                                    break;
                                case "TARGETED_REPAIR":
                                    forlist.icon = "I_SA_REPAIR_VEHICLE.TGA";
                                    break;
                                case "TRACTOR_BEAM":
                                    forlist.icon = "I_SA_TRACTOR_BEAM.TGA";
                                    break;
                                case "TURBO":
                                    forlist.icon = "I_SA_POWER_TO_ENGINES.TGA";
                                    break;
                                case "UNTARGETED_STICKY_BOMB":
                                    forlist.icon = "I_SA_DROP_BOMB.TGA";
                                    break;
                                case "WEAKEN_ENEMY":
                                    forlist.icon = "I_SA_WEAKEN_ENEMY.TGA";
                                    break;
                            }
                        }
                        unitabilities.Add(forlist);
                    }
                    //else <Unit_Abilities_Data SubObjectList="No"> < Unit_Ability /> does not in fact block inheritance of unit abilities. It might work for regular abilities though?
                }
            }
        }
        value = unit.SelectSingleNode("descendant::Abilities");
        if (!(value is null))
        {
            XmlNodeList abilityNodes = value.SelectNodes("*");
            foreach (XmlNode able in abilityNodes)
            {
                ability forlist = new ability
                {
                    activation = "",
                    applicable_types = new string[0],
                    applicable_categories = new string[0],
                    excluded_types = new string[0],
                    linkedEntity = "",
                    recharge = -1,
                    radius = -1,
                    minradius = -1,
                    maxradius = -1,
                    duration = -1,
                    genericBool = false,
                    genericValue = -1,
                    stacking = -1,

                    damageBonus = 0, //Use the value of having no effect rather than a flag for invalid
                    defenseBonus = 0,
                    shieldBonus = 0,
                    healthBonus = 0,
                    speedBonus = 0,
                };
                forlist.type = able.Name;
                if (able.Attributes is null || able.Attributes.Count == 0) {
                    entities.readerrors += "\nUnexpected ability " + able.Name + " on unit " + name;
                    continue;
                }
                forlist.name = able.Attributes[0].Value;

                //Widely used generic tags
                XmlNode abdata = able.SelectSingleNode("descendant::Activation_Style");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.activation = abdata.LastChild.Value.Trim();
                abdata = able.SelectSingleNode("descendant::Applicable_Unit_Types");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.applicable_types = ReadWhiteSpaceAsCommas(abdata.LastChild.Value);
                abdata = able.SelectSingleNode("descendant::Excluded_Unit_Types");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.excluded_types = ReadWhiteSpaceAsCommas(abdata.LastChild.Value);
                abdata = able.SelectSingleNode("descendant::Applicable_Unit_Categories");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.applicable_categories = ReadWhiteSpaceAsCommas(abdata.LastChild.Value.Replace("|",""));
                abdata = able.SelectSingleNode("descendant::Activation_Min_Range");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.minradius = Single.Parse(abdata.LastChild.Value);
                abdata = able.SelectSingleNode("descendant::Activation_Max_Range");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.maxradius = Single.Parse(abdata.LastChild.Value);
                abdata = able.SelectSingleNode("descendant::Spawned_Object_Type");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.linkedEntity = abdata.LastChild.Value.Trim();

                //command bonus
                abdata = able.SelectSingleNode("descendant::Damage_Bonus_Percentage");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.damageBonus = Single.Parse(abdata.LastChild.Value);
                abdata = able.SelectSingleNode("descendant::Health_Bonus_Percentage");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.healthBonus = Single.Parse(abdata.LastChild.Value);
                abdata = able.SelectSingleNode("descendant::Shield_Bonus_Percentage");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.shieldBonus = Single.Parse(abdata.LastChild.Value);
                abdata = able.SelectSingleNode("descendant::Defense_Bonus_Percentage");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.defenseBonus = Single.Parse(abdata.LastChild.Value);
                abdata = able.SelectSingleNode("descendant::Movement_Speed_Bonus_Percentage");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.speedBonus = Single.Parse(abdata.LastChild.Value);
                abdata = able.SelectSingleNode("descendant::Stacking_Category");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.stacking = Int32.Parse(abdata.LastChild.Value);

                abdata = able.SelectSingleNode("descendant::FOW_Reveal_Range_Multiplier");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.genericValue = Single.Parse(abdata.LastChild.Value);

                //admin
                abdata = able.SelectSingleNode("descendant::Price_Reduction_Percentage"); //todo these probably need to go to different variables
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.genericValue = Single.Parse(abdata.LastChild.Value);
                abdata = able.SelectSingleNode("descendant::Time_Reduction_Percentage");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.genericValue = Single.Parse(abdata.LastChild.Value);
                abdata = able.SelectSingleNode("descendant::Percentage_Income_Modifier");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.genericValue = Single.Parse(abdata.LastChild.Value);
                abdata = able.SelectSingleNode("descendant::Absolute_Income_Modifier");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.genericValue = Single.Parse(abdata.LastChild.Value);

                //Point defense
                abdata = able.SelectSingleNode("descendant::Projectile_Types_Targeted");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.applicable_types = ReadWhiteSpaceAsCommas(abdata.LastChild.Value);
                abdata = able.SelectSingleNode("descendant::Recharge_Time_In_Secs");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.recharge = Single.Parse(abdata.LastChild.Value);
                abdata = able.SelectSingleNode("descendant::Protection_Radius");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.radius = Single.Parse(abdata.LastChild.Value);
                abdata = able.SelectSingleNode("descendant::Defense_Duration_In_Secs");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.duration = Single.Parse(abdata.LastChild.Value);

                //Heal
                abdata = able.SelectSingleNode("descendant::Heal_Amount");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.genericValue = Single.Parse(abdata.LastChild.Value);
                abdata = able.SelectSingleNode("descendant::Heal_Range");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.radius = Single.Parse(abdata.LastChild.Value);
                abdata = able.SelectSingleNode("descendant::Heal_Interval_In_Secs");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.recharge = Single.Parse(abdata.LastChild.Value);
                abdata = able.SelectSingleNode("descendant::Single_Target_Heal");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.genericBool = abdata.LastChild.Value.ToLower().Contains("yes");

                //Stun
                abdata = able.SelectSingleNode("descendant::Stun_Range");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.radius = Single.Parse(abdata.LastChild.Value);
                abdata = able.SelectSingleNode("descendant::Stunned_Particle_Effect");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.linkedEntity = abdata.LastChild.Value.Trim();
                abdata = able.SelectSingleNode("descendant::Stun_Time_In_Secs");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.duration = Single.Parse(abdata.LastChild.Value);

                //Grenade
                abdata = able.SelectSingleNode("descendant::Grenade_Type");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.linkedEntity = abdata.LastChild.Value.Trim();
                abdata = able.SelectSingleNode("descendant::Grenade_Explode_Timer_In_Secs");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.duration = Single.Parse(abdata.LastChild.Value);

                abdata = able.SelectSingleNode("descendant::Bomb_Countdown_Seconds");
                if (!(abdata is null) && !(abdata.LastChild is null)) forlist.duration = Single.Parse(abdata.LastChild.Value);

                switch (forlist.type)
                {
                    case "Laser_Defense_Ability": //todo, save PD stats to fields in main unit?
                    break;
                        //todo get damage on mines, grenades... Also average and target type it
                        //todo be able to add command bonuses to global list for any number of heroes on the field
                        /*
                         * todo interdiction mines documentation. It's just fighter spawn data, right?
                            Repair fighters needs numbers
                            Battle mediation and suchlike
                        UNTARGETED_STICKY_BOMB is entirely a unita bility, not an ability
                         */
                }

                abilities.Add(forlist);
            }
        }


        value = unit.SelectSingleNode("descendant::Variant_Of_Existing_Type");
        if (!(value is null))
        {
            variant = fullTrim(value.LastChild.Value);
        }
        //else
        //{//This happens on gunships and I'll need to filter those out before it can be useful
        //needsvariant = needsvariant; //MessageBox.Show(name + " is missing data and variant of. Panic!");
        //}

        unit unidad = new unit
        {
            variantof = variant,
            variantbase = variant, //Stores the high level variantof, unlike the top field which is erased in the detemplating process
            unitname = name,
            username = username,
            elementName = unit.LocalName,
            icon = icon,
            bombingRunUnit = bombingRunUnit,
            cost = cost,
            buildtime = buildtime,
            skirmcost = skirmcost,
            skirmbuildtime = skirmbuildtime,
            affiliations = affiliations,
            pop = pop,
            techlevel = techlevel,
            locked = locked,
            reqstructures = reqstructures,
            reqorbit = reqorbit,
            cp = cp,
            maintenance = maintenance,
            percompany = percompany,
            companyunits = companyunits,
            datafile = file,
            reqfile = file,
            reqtemplate = name,
            shield_type = shield_type,
            armor_type = armor_type,
            unitclass = unitclass,
            planets = planets,
            tooltip = tooltip,
            crew = crew,
            shield = shield,
            regen = regen,
            hp = hp,
            gui_row = gui_row,
            limit_concurrent = limit_concurrent,
            limit_lifetime = limit_lifetime,
            speed = speed,
            min_speed = min_speed,
            accel = accel,
            turn = turn,
            range = range,
            categories = categories,
            Hardpoints = Hardpoints,
            model = model,
            terrainMaps = terrainMaps,
            variantchain = new List<string>(),
            BTS = BTS,
            transport = transport,
            garrison_slots = garrison_slots,
            garrison_value = garrison_value,
            garrison_type = garrison_type,
            flags = flags,
            modebehaviors = modebehaviors,
            behaviors = behaviors,
            locomotor_type = locomotor_type,
            container = container,
            cost_baseID = -1,
            pop_baseID = -1,
            crew_baseID = -1,
            buildtime_baseID = -1,
            skirmcost_baseID = -1,
            skirmbuildtime_baseID = -1,
            atype_baseID = -1,
            stype_baseID = -1,
            hp_baseID = -1,
            shield_baseID = -1,
            regen_baseID = -1,
            gui_row_baseID = -1,
            concurrent_baseID = -1,
            lifetime_baseID = -1,
            speed_baseID = -1,
            min_speed_baseID = -1,
            accel_baseID = -1,
            turn_baseID = -1,
            range_baseID = -1,
            lomotor_baseID = -1,
            garrisonSlots_baseID = -1,
            garrisonValue_baseID = -1,
            garrisonType_baseID = -1,
            maintenance_baseID = -1,
            abilities = abilities,
            unitabilities = unitabilities,
            hero = hero,
            builtin = builtin,
            garrison = garrison,
        };

        entities.objects.Add(unidad);

        /*if (type == 1)
        {
            entities.groundCompanies.Add(unidad);
        }
        else if (type == 2)
        {
            entities.groundUnits.Add(unidad);
        }
        else if (type == 3)
        {
            entities.spaceHeroes.Add(unidad);
        }
        else if (type == 4)
        {
            entities.heroCompanies.Add(unidad);
        }
        else if (type == 5)
        {
            entities.groundHeroes.Add(unidad);
        }
        else if (type == 6)
        {
            entities.structures.Add(unidad);
        }
        /*else if (type == 7)
        {
            globals.groundInfantry.Add(unidad);
        }
        else if (type == 8)
        {
            globals.squadrons.Add(unidad);
        }
        else if (type == 9)
        {
            globals.upgrades.Add(unidad);
        }*/
        /*else
        {
            entities.spaceUnits.Add(unidad);
        }*/

        return false;
    }

    public static void parseObjectFile(string objectfile, entities entities)
    {
        string path = getModFile(objectfile);
        if (path == "") path = "*" + objectfile;

        XmlDocument doc = readModXmlOrMeg(objectfile, entities);
        XmlNode root = doc.DocumentElement;
        if (root is null) return;

        XmlNodeList objects = root.SelectNodes("*");
        foreach (XmlNode entity in objects)
        {
            if (!(entity is null))
            {
                if (!(entity.LastChild is null))
                {
                    if (parseUnit(entity, entities, path)) break;
                }
            }
            //parseUnit(file, entities);
        }
    }

    public static void parseObjects(entities entities)
    {
        entities.objects.Clear();

        XmlDocument doc = readModXmlOrMeg("XML\\GameObjectFiles.xml", entities);
        XmlNode root = doc.DocumentElement;

        XmlNodeList files = root.SelectNodes("*");
        foreach (XmlNode file in files)
        {
            if(!(file is null))
            {
                if (!(file.LastChild is null))
                {
                    string filepath = file.LastChild.Value.Trim().ToUpper();
                    if (!(filepath.Contains("DEBUG_DUMMIES") || filepath.Contains("PROPS\\") || filepath.Contains("\\DEATH_CLONES") || filepath.Contains("PLANET") || filepath.Contains("PROJECTILE")))
                    {//Further effiency gains can probably be made. But segregating hardpoints might not be ok in the general case
                        parseObjectFile("XML\\" + filepath, entities);
                    }
                }
            }
        }

        entities.objects.Sort((s1, s2) => s1.unitname.CompareTo(s2.unitname));
    }

    public static void parseUnitFolder(List<string> files, entities entities)
    {
        //TODO check if new unit files exist
        foreach (string file in files)
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.Load(file);
            XmlNode root = doc.DocumentElement;

            /*XmlNodeList spaces = root.SelectNodes("descendant::SpaceUnit");
            XmlNodeList grounds = root.SelectNodes("descendant::GroundCompany");
            XmlNodeList groundvehicles = root.SelectNodes("descendant::GroundVehicle");
            XmlNodeList indigs = root.SelectNodes("descendant::Indigenous_Unit");
            XmlNodeList groundinfantry = root.SelectNodes("descendant::GroundInfantry");
            XmlNodeList containers = root.SelectNodes("descendant::Container");
            XmlNodeList heroes = root.SelectNodes("descendant::HeroUnit"); //Apparently hero templates live here. Probably should make this whole process more extensible
            //todo read structures: either have to read all at once or figure out

            //XmlNodeList squadrons = root.SelectNodes("descendant::Squadron");
            //XmlNodeList ups = root.SelectNodes("descendant::DummyStructure");

            foreach (XmlNode unit in grounds)
            {
                parseUnit(unit, 1, file, entities, true);
            }

            foreach (XmlNode unit in spaces)
            {
                parseUnit(unit, 0, file, entities, true);
            }

            foreach (XmlNode unit in groundvehicles)
            {
                parseUnit(unit, 2, file, entities, true);
            }

            foreach (XmlNode unit in indigs)
            {
                parseUnit(unit, 2, file, entities, true);
            }

            foreach (XmlNode unit in groundinfantry)
            {
                parseUnit(unit, 2, file, entities, true); //7 originally, for now combine with vehicles
            }
            //Speed up later indexing by this value
            entities.spaceUnits.Sort((s1, s2) => s1.unitname.CompareTo(s2.unitname));
            entities.groundCompanies.Sort((s1, s2) => s1.unitname.CompareTo(s2.unitname));
            entities.groundUnits.Sort((s1, s2) => s1.unitname.CompareTo(s2.unitname));

            foreach (XmlNode unit in heroes)
            {
                parseUnit(unit, 5, file, entities, true);
            }

            /*foreach (XmlNode unit in squadrons)
            {
                parseUnit(unit, 8, file, entities, true);
            }

            foreach (XmlNode unit in ups)
            {
                parseUnit(unit, 9, modfile);
            }

            foreach (XmlNode container in containers)
            {
                parseContainer(container, entities);
            }*/
        }
    }

    public static void parseHeroFolder(entities entities)
    {
        List<string> files = getModFiles("XML\\Heroes", "*.xml");
        foreach (string file in files)
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.Load(file);
            XmlNode root = doc.DocumentElement;

            /*XmlNodeList spaces = root.SelectNodes("descendant::UniqueUnit");
            XmlNodeList spaces2 = root.SelectNodes("descendant::GenericCommander");
            XmlNodeList grounds = root.SelectNodes("descendant::HeroCompany");
            XmlNodeList grounds2 = root.SelectNodes("descendant::GenericCommanderCompany");
            XmlNodeList groundinfantry = root.SelectNodes("descendant::GroundInfantry");
            XmlNodeList groundvehicles = root.SelectNodes("descendant::HeroUnit");

            foreach (XmlNode unit in grounds)
            {
                parseUnit(unit, 4, file, entities, true);
            }

            foreach (XmlNode unit in grounds2)
            {
                parseUnit(unit, 4, file, entities, true);
            }

            foreach (XmlNode unit in groundinfantry)
            {
                parseUnit(unit, 5, file, entities, true);
            }

            foreach (XmlNode unit in spaces)
            {
                parseUnit(unit, 3, file, entities, true);
            }

            foreach (XmlNode unit in spaces2)
            {
                parseUnit(unit, 3, file, entities, true);
            }

            foreach (XmlNode unit in groundvehicles)
            {
                parseUnit(unit, 5, file, entities, true);
            }*/
        }
    }

    public static void parseStructureFolder(entities entities)
    {
        List<string> files = getModFiles("XML\\Structures", "*.xml");
        foreach (string file in files)
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.Load(file);
            XmlNode root = doc.DocumentElement;

            /*XmlNodeList structs = root.SelectNodes("*");

            foreach (XmlNode unit in structs)
            {
                parseUnit(unit, 6, file, entities, true);
            }*/
        }

        entities.structures.Sort((s1, s2) => s1.unitname.CompareTo(s2.unitname));
    }

    public static void parseMEGs(entities entities)
    {
        entities.MEGdata.Clear();
        entities.MEGentries.Clear();
        entities.MEGhashes.Clear();
        string path = getModFile("Megafiles.xml");
        XmlDocument doc = new XmlDocument();
        doc.PreserveWhitespace = true;
        doc.Load(path);

        XmlNode root = doc.DocumentElement;
        XmlNodeList megs = root.SelectNodes("*");
        int megindex = 0;
        foreach (XmlNode meg in megs)
        {
            if(!(meg is null))
            {
                string megpath = getModFile(LastFolderOrFile(meg.InnerText));
                if (File.Exists(megpath))
                {
                    bool saveMeg = false;
                    int overwriteindex = -1;
                    List<MEGentry> entriesinFile = new List<MEGentry>(); //Store new files separately until completion to catch new megs overwriting old
                    byte[] megbytes = File.ReadAllBytes(megpath);
                    int filenamecount = DatParser.make32(megbytes, 0);
                    bool[] saveFiles = new bool[filenamecount];
                    int[] finalindices = new int[filenamecount];
                    int filecount = DatParser.make32(megbytes, 0);
                    int byteid = 8;
                    int finalindex = 0;
                    for (int file = 0; file < filenamecount; file++)
                    {
                        int strlen = DatParser.make16(megbytes, byteid);
                        byteid += 2;
                        byte[] dest = new byte[strlen];
                        Buffer.BlockCopy(megbytes, byteid,dest,0, strlen);
                        string str = RemoveTopLevelFolder(System.Text.Encoding.Default.GetString(dest)).ToUpper();
                        byteid += strlen;

                        string ext = Extension(str);
                        if (ext == "XML" || ext == "LUA" || ext == "MTD" || ext == "TED" || ext == "TGA") //todo ignore TGA except for MTCommandbar, don't save the whole meg for teds
                        {
                            saveMeg = true; //Don't set for TED eventually
                            int hash = LookupUntemplateID(str);
                            overwriteindex = entities.MEGentries.FindIndex(x => x.hash == hash);
                            if (overwriteindex > -1)
                            {
                                bool loopit = true;
                                while(loopit)
                                {
                                    int readhash = entities.MEGentries[overwriteindex].hash;
                                    if (hash == readhash)
                                    {
                                        if (str == entities.MEGentries[overwriteindex].filename) loopit = false; //found matching hash and string
                                        else overwriteindex++; //Hash collision, see if the next matches
                                    }
                                    else
                                    {
                                        loopit = false;
                                        overwriteindex = -1; //there were only collisions
                                    }
                                }
                            }

                            if (overwriteindex < 0)
                            {
                                finalindices[file] = finalindex;
                                finalindex++;
                                saveFiles[file] = true;
                                MEGentry entry = new MEGentry
                                {
                                    filename = str,
                                    hash = hash,
                                    MEGid = megindex,
                                };
                                entriesinFile.Add(entry);
                            }
                            //handled in the loop for file size data
                        }
                    }
                    for (int file = 0; file < filecount; file++)
                    {
                        byteid += 8;
                        int len = DatParser.make32(megbytes, byteid);
                        byteid += 4;
                        int start = DatParser.make32(megbytes, byteid);
                        byteid += 4;
                        int nameindex = DatParser.make32(megbytes, byteid);
                        byteid += 4;
                        if (saveFiles[nameindex])
                        {//todo read ted terrain byte here
                            int final = finalindices[nameindex]; //Used vs total in the file
                            MEGentry entry = entriesinFile[final];
                            entry.startindex = start;
                            entry.length = len;
                            entriesinFile[final] = entry;
                        }
                        if(overwriteindex > -1)
                        {
                            MEGentry entry = entities.MEGentries[overwriteindex];
                            entry.MEGid = megindex;
                            entry.startindex = start;
                            entry.length = len;
                            entities.MEGentries[overwriteindex] = entry;
                        }
                    }

                    if (saveMeg)
                    {
                        foreach (MEGentry entry in entriesinFile) entities.MEGentries.Add(entry);
                        entities.MEGdata.Add(megbytes);
                        megindex++;
                        entities.MEGentries.Sort((s1, s2) => s1.hash.CompareTo(s2.hash));
                    }
                }
            }
        }
    }

    public static void parseGameConstants(entities entities)
    {
        entities.SpaceArmors.Clear();
        entities.SpaceShields.Clear();
        entities.GroundArmors.Clear();
        entities.GroundShields.Clear();
        XmlDocument consts = readModXmlOrMeg("XML\\GameConstants.xml", entities);
        XmlNode typedef = consts.DocumentElement.SelectSingleNode("descendant::Armor_Types");
        string[] types = fullTrim(typedef.InnerText).Split(',');
        foreach (string type in types)
        {
            if (type.Contains("ArmourS_") || type.Contains("ArmorS_")) entities.SpaceArmors.Add(type);
            else if (type.Contains("ShieldS_")) entities.SpaceShields.Add(type);
            else if (type.Contains("ArmourG_") || type.Contains("ArmorG_")) entities.GroundArmors.Add(type);
            else if (type.Contains("ShieldG_")) entities.GroundShields.Add(type);
            //entities.AllArmors.Add(type); Already populated from categories

            ArmorMods armors = new ArmorMods();
            armors.armorType = type;
            armors.WeaponMods = new List<ArmorMod>();
            entities.ArmorIndexedMods.Add(armors);
        }
        typedef = consts.DocumentElement.SelectSingleNode("descendant::Damage_Types");
        types = fullTrim(typedef.InnerText).Split(',');
        foreach (string type in types)
        {
            entities.DamageTypes.Add(type);
            if (type.Contains("DamageS_")) entities.SpaceDamageTypes.Add(type);
            if (type.Contains("DamageL_")) entities.GroundDamageTypes.Add(type);
            WeaponMods weaps = new WeaponMods();
            weaps.weaponType = type;
            weaps.HpMods = new List<ArmorMod>();
            weaps.ShieldMods = new List<ArmorMod>();
            entities.ArmorMods.Add(weaps);
        }
        XmlNodeList moddefs = consts.DocumentElement.SelectNodes("descendant::Damage_To_Armor_Mod");
        foreach(XmlNode moddef in moddefs)
        {
            string[] mods = fullTrim(moddef.InnerText).Split(',');
            string damage = mods[0];
            string type = mods[1];
            float modifier = float.Parse(mods[2]);
            for (int i = 0; i < entities.ArmorIndexedMods.Count; i++)
            {
                if (entities.ArmorIndexedMods[i].armorType == type)
                {
                    ArmorMods armors = entities.ArmorIndexedMods[i];
                    ArmorMod mod = new ArmorMod();
                    mod.armorType = damage;
                    mod.modifier = modifier;
                    armors.WeaponMods.Add(mod);
                    entities.ArmorIndexedMods[i] = armors;
                    break;
                }
            }
            for (int i = 0; i < entities.ArmorMods.Count; i++)
            {
                if (entities.ArmorMods[i].weaponType == damage)
                {
                    WeaponMods weaps = entities.ArmorMods[i];
                    ArmorMod mod = new ArmorMod();
                    mod.armorType = type;
                    type = type.ToUpper();
                    mod.modifier = modifier;
                    if (type.Contains("SHIELD")) weaps.ShieldMods.Add(mod);
                    else if (type.Contains("ARMOR") || type.Contains("ARMOUR")) weaps.HpMods.Add(mod);
                    entities.ArmorMods[i] = weaps;
                    break;
                }
            }
            //else MessageBox.Show("Unrecognized damage type : " + damage + " attempting to set damage to " + mods[1]);
        }
        for (int i = 0; i < entities.ArmorMods.Count; i++)
        {
            float medianA = 1;
            float medianS = 1;
            entities.ArmorMods[i].HpMods.Sort((s1, s2) => s1.modifier.CompareTo(s2.modifier));
            entities.ArmorMods[i].ShieldMods.Sort((s1, s2) => s1.modifier.CompareTo(s2.modifier));
            List<ArmorMod> heavyFightersSuck = new List<ArmorMod>();
            foreach(ArmorMod armor in entities.ArmorMods[i].ShieldMods)
            {
                if (armor.armorType != "ShieldS_FighterHeavy") heavyFightersSuck.Add(armor); //Heavy fighter is not on sheet. On armor this is compensated by ArmourG_Hero also being there and 0, but shields need to be fixed a hard way 
            }
            int count = entities.ArmorMods[i].HpMods.Count;
            if(count > 0)
            {
                if (count % 2 == 1) medianA = entities.ArmorMods[i].HpMods[(count - 1) / 2].modifier;
                else medianA = (entities.ArmorMods[i].HpMods[(count) / 2 - 1].modifier + entities.ArmorMods[i].HpMods[(count) / 2].modifier) / 2;
            }
            count = heavyFightersSuck.Count;
            if(count > 0)
            {
                if (count % 2 == 1) medianS = heavyFightersSuck[(count - 1) / 2].modifier;
                else medianS = (heavyFightersSuck[(count) / 2 - 1].modifier + heavyFightersSuck[(count) / 2].modifier) / 2;
            }
            entities.ArmorMods[i].median = (medianA + medianS) / 2;
        }
        for (int i = 0; i < entities.ArmorIndexedMods.Count; i++)
        {
            //Hardcoding seems like the best way, these need to exactly match the sheet clacs
            string[] groundshields = { "DamageL_InfantryBlaster", "DamageL_AntiInfantryBlaster", "DamageL_AntiVehicleBlaster", "DamageL_Lightsaber", "DamageL_Ion" };
            string[] groundarmor = { "DamageL_InfantryBlaster", "DamageL_AntiInfantryBlaster", "DamageL_AntiVehicleBlaster", "DamageL_ProtonWarhead", "DamageL_ConcussionWarhead", "DamageL_ThermalWarhead", "DamageL_MassDriver", "DamageL_Lightsaber", "DamageL_Flak", "DamageL_Ion" };
            string[] spaceshields = { "DamageS_Laser", "DamageS_TurboIon", "DamageS_Turbolaser", "DamageS_Concussion", "DamageS_Flechette", "DamageS_Proton" };
            string[] spacearmor = { "DamageS_Laser", "DamageS_Turbolaser", "DamageS_Concussion", "DamageS_Flechette", "DamageS_Proton" };

            float sum = 0;
            int count = 0;
            bool checkall = true;
            string[] weapstocheck = groundshields;
            string armortype = entities.ArmorIndexedMods[i].armorType;
            if (armortype.Contains("ShieldG_"))
            {
                checkall = false;
            }
            else if (armortype.Contains("ArmourG_"))
            {
                checkall = false;
                weapstocheck = groundarmor;
            }
            else if (armortype.Contains("ShieldS_"))
            {
                checkall = false;
                weapstocheck = spaceshields;
            }
            else if (armortype.Contains("ArmourS_"))
            {
                checkall = false;
                weapstocheck = spacearmor;
            }
            foreach (ArmorMod weapon in entities.ArmorIndexedMods[i].WeaponMods) {
                if(checkall || weapstocheck.Contains(weapon.armorType))
                {
                    sum += weapon.modifier;
                    count++;
                }
            }
            if (count > 0)
            {
                entities.ArmorIndexedMods[i].average = sum / count;
                if (entities.ArmorIndexedMods[i].average > 0.9 && !armortype.Contains("Fighter")) entities.ArmorIndexedMods[i].average = 0.9F;
                entities.ArmorIndexedMods[i].average = 1 - entities.ArmorIndexedMods[i].average;
            }
            else entities.ArmorIndexedMods[i].average = 0; //Would be 0 except that it's capped
        }
    }

    public static void parseCategories(entities entities)
    {
        XmlDocument doc = readModXmlOrMeg("XML\\Enum\\GameObjectCategoryType.xml", entities);
        XmlNode root = doc.DocumentElement;
        XmlNodeList cats = root.SelectNodes("*");
        foreach (XmlNode cat in cats)
        {
            string name = cat.Name;
            ulong hex = Convert.ToUInt64(fullTrim(cat.InnerText), 16);

            entities.AllArmors.Add(name);
            if(hex < 256 && name != "None") entities.SpaceCategories.Add(name);
            else if (hex <= 4096) entities.GroundCategories.Add(name);
            if(name == "InfantryHero") entities.GroundCategories.Add(name);
            if (name == "VehicleHero") entities.GroundCategories.Add(name);
            if (name == "SpaceHero") entities.SpaceCategories.Add(name);
        }
    }

    public static void parseFlags(entities entities)
    {
        XmlDocument doc = readModXmlOrMeg("XML\\Enum\\GameObjectPropertiesType.xml", entities);
        XmlNode root = doc.DocumentElement;
        XmlNodeList cats = root.SelectNodes("*");
        foreach (XmlNode cat in cats)
        {
            string name = cat.Name;
            ulong hex = Convert.ToUInt64(fullTrim(cat.InnerText), 16);

            entities.AllFlags.Add(name);
        }
    }

    public static void parsemodid(string path, entities entities)
    {
        if(path == "")
        {
            entities.modid = "";
            return;
        }
        XmlDocument doc = new XmlDocument();
        doc.PreserveWhitespace = true;
        doc.Load(path);
        XmlNode root = doc.DocumentElement;
        XmlNodeList objects = root.SelectNodes("*");
        foreach (XmlNode id in objects)
        {
            entities.modid = id.Attributes[0].Value;
        }
    }

    public static void parseProjectiles(entities entities)
    {
        entities.projectiles.Clear();
        XmlDocument projdoc = readModXmlOrMeg("XML\\GameObjectFiles.xml", entities);
        XmlNode projroot = projdoc.DocumentElement;

        XmlNodeList files = projroot.SelectNodes("*");

        foreach (XmlNode file in files)
        {
            if (!(file is null))
            {
                if (!(file.LastChild is null) && (entities.modid == "" || file.InnerText.ToUpper().Contains("PROJECTILE"))) //If an EaWX modid is defined, make a simplifiying assumption
                {
                    XmlDocument doc = readModXmlOrMeg("XML\\" + file.LastChild.Value.Trim(), entities);
                    XmlNode root = doc.DocumentElement;
                    if (root is null) continue;

                    XmlNodeList projectiles = root.SelectNodes("descendant::Projectile");

                    foreach (XmlNode proj in projectiles)
                    {
                        string name = proj.Attributes[0].Value;
                        string variantof = "";
                        string damageType = "";
                        float damageAmount = -1;
                        float blastRadius = -1;
                        float speed = -1;
                        float turn = -1;
                        XmlNode value = proj.SelectSingleNode("descendant::Variant_of_Existing_Type");
                        if (!(value is null))
                        {
                            if (!(value.LastChild is null))
                            {
                                variantof = fullTrim(value.LastChild.Value);
                            }
                        }
                        value = proj.SelectSingleNode("descendant::Variant_Of_Existing_Type"); //I hate capitalization
                        if (!(value is null))
                        {
                            if (!(value.LastChild is null))
                            {
                                variantof = fullTrim(value.LastChild.Value);
                            }
                        }
                        value = proj.SelectSingleNode("descendant::Damage_Type");
                        if (!(value is null))
                        {
                            if (!(value.LastChild is null))
                            {
                                damageType = fullTrim(value.LastChild.Value);
                            }
                        }
                        value = proj.SelectSingleNode("descendant::Projectile_Damage");
                        if (!(value is null))
                        {
                            if (!(value.LastChild is null))
                            {
                                damageAmount = float.Parse(value.LastChild.Value);
                            }
                        }
                        value = proj.SelectSingleNode("descendant::Projectile_Blast_Area_Damage");
                        if (!(value is null))
                        {
                            if (!(value.LastChild is null))
                            {
                                damageAmount += float.Parse(value.LastChild.Value);
                            }
                        }
                        value = proj.SelectSingleNode("descendant::Projectile_Blast_Area_Range");
                        if (!(value is null))
                        {
                            if (!(value.LastChild is null))
                            {
                                blastRadius += float.Parse(value.LastChild.Value);
                            }
                        }
                        value = proj.SelectSingleNode("descendant::Max_Speed");
                        if (!(value is null))
                        {
                            if (!(value.LastChild is null))
                            {
                                speed = float.Parse(value.LastChild.Value);
                            }
                        }
                        value = proj.SelectSingleNode("descendant::Max_Rate_Of_Turn");
                        if (!(value is null))
                        {
                            if (!(value.LastChild is null))
                            {
                                turn = float.Parse(value.LastChild.Value);
                            }
                        }
                        projectile project = new projectile
                        {
                            name = name,
                            variantof = variantof,
                            damageType = damageType,
                            damageAmount = damageAmount,
                            blastRadius = blastRadius,
                            speed = speed,
                            turn = turn,
                        };
                        entities.projectiles.Add(project);
                    }
                }
            }
        }

        entities.projectiles.Sort((s1, s2) => s1.name.CompareTo(s2.name));

        entities.projectilehashes = new List<List<int>>();
        for (int i = 0; i < entities.projectiles.Count; i++)
        {
            int index = LookupUntemplateID(entities.projectiles[i].name.ToLower());
            if (entities.projectilehashes.Count <= index)
            {
                for (int j = entities.projectilehashes.Count; j <= index; j++) entities.projectilehashes.Add(new List<int>());
            }
            entities.projectilehashes[index].Add(i);
        }

        bool needs_templates = true;
        while (needs_templates)
        {
            needs_templates = false;
            for (int i = 0; i < entities.projectiles.Count; i++)
            {
                projectile proj = entities.projectiles[i];
                if (proj.variantof != "")
                {
                    int index = LookupUntemplateID(proj.variantof.ToLower());
                    for (int j = 0; j < entities.projectilehashes[index].Count; j++)
                    {
                        projectile proj2 = entities.projectiles[entities.projectilehashes[index][j]];
                        if (proj2.name == proj.variantof)
                        {
                            if (proj.damageType == "") proj.damageType = proj2.damageType;
                            if (proj.damageAmount < 0) proj.damageAmount = proj2.damageAmount;
                            if (proj.blastRadius < 0) proj.blastRadius = proj2.blastRadius;
                            if (proj.speed < 0) proj.speed = proj2.speed;
                            if (proj.turn < 0) proj.turn = proj2.turn;
                            proj.variantof = proj2.variantof;
                            if (proj2.variantof != "")
                            {
                                needs_templates = true; //Another iteration required
                            }
                            break;
                        }
                    }
                }

                if (proj.damageType == "") proj.damageType = "Damage_Default";
                entities.projectiles[i] = proj;
            }
        }
    }

    public static void parseHardpoints(entities entities, List<Text_Entry> Text)
    {//Must parse after projectiles
        entities.hardpoints.Clear();
        XmlDocument hpdoc = readModXmlOrMeg("XML\\HardPointDataFiles.xml", entities);
        XmlNode hproot = hpdoc.DocumentElement;

        XmlNodeList files = hproot.SelectNodes("*");

        foreach (XmlNode file in files)
        {
            if (!(file is null))
            {
                if (!(file.LastChild is null))
                {
                    XmlDocument doc = readModXmlOrMeg("XML\\" + file.LastChild.Value.Trim(), entities);
                    
                    XmlNode root = doc.DocumentElement;
                    if (root is null) continue; //Skip files that don't exist
                    XmlNodeList hardpoints = root.SelectNodes("descendant::HardPoint");

                    foreach (XmlNode hp in hardpoints)
                    {
                        string name = hp.Attributes[0].Value;
                        bool targetable = false;
                        string text = "";
                        string damageType = "Damage_Default";
                        string hpType = "";
                        float health = -1;
                        float damageAmount = -1;
                        float blastRadius = -1;
                        float recharge = -1; //average of max and min
                        float pulseCount = -1;
                        float pulseDelay = -1;
                        //float coneWidth = -1;
                        //float coneHeight = -1;
                        float fullsalvomod = 1;
                        float range = -1;
                        List<float> inaccuracyAmounts = new List<float>();
                        List<string> inaccuracyTypes = new List<string>();
                        XmlNode value = hp.SelectSingleNode("descendant::Is_Targetable");
                        try
                        {
                            if (!(value is null))
                            {
                                if (!(value.LastChild is null))
                                {
                                    string target = value.LastChild.Value.ToUpper();
                                    if (target.Contains("YES")) targetable = true;
                                }
                            }
                            value = hp.SelectSingleNode("descendant::Tooltip_Text");
                            if (!(value is null))
                            {
                                if (!(value.LastChild is null))
                                {
                                    text = fullTrim(value.LastChild.Value);
                                }
                            }
                            value = hp.SelectSingleNode("descendant::Fire_Projectile_Type");
                            if (!(value is null))
                            {
                                if (!(value.LastChild is null))
                                {
                                    string proj = fullTrim(value.LastChild.Value);
                                    string lower = proj.ToLower();
                                    bool notfound = true;
                                    int index = LookupUntemplateID(proj);
                                    if(index < entities.projectilehashes.Count)
                                    {
                                        for (int j = 0; j < entities.projectilehashes[index].Count; j++)
                                        {
                                            projectile project = entities.projectiles[entities.projectilehashes[index][j]];
                                            if (project.name.ToLower() == lower)
                                            {
                                                damageType = project.damageType;
                                                damageAmount = project.damageAmount;
                                                blastRadius = project.blastRadius;
                                                notfound = false;
                                                break;
                                            }
                                        }
                                    }
                                    if (notfound && proj != "") entities.readerrors += "\nCannot find projectile type " + fullTrim(value.LastChild.Value) + " for hardpoint " + name;
                                    if (text == "") text = proj;
                                }
                            }
                            value = hp.SelectSingleNode("descendant::Type");
                            if (!(value is null))
                            {
                                if (!(value.LastChild is null))
                                {
                                    hpType = fullTrim(value.LastChild.Value);
                                    if (text == "") text = hpType;
                                }
                            }
                            value = hp.SelectSingleNode("descendant::Health");
                            if (!(value is null))
                            {
                                if (!(value.LastChild is null))
                                {
                                    health = float.Parse(value.LastChild.Value);
                                }
                            }
                            value = hp.SelectSingleNode("descendant::Fire_Min_Recharge_Seconds");
                            if (!(value is null))
                            {
                                if (!(value.LastChild is null))
                                {
                                    float rcharge = float.Parse(value.LastChild.Value);
                                    if (recharge < 0) recharge = rcharge;
                                    else recharge = (recharge + rcharge) / 2;
                                }
                            }
                            value = hp.SelectSingleNode("descendant::Fire_Max_Recharge_Seconds");
                            if (!(value is null))
                            {
                                if (!(value.LastChild is null))
                                {
                                    float rcharge = float.Parse(value.LastChild.Value);
                                    if (recharge < 0) recharge = rcharge;
                                    else recharge = (recharge + rcharge) / 2;
                                }
                            }
                            value = hp.SelectSingleNode("descendant::Fire_Pulse_Count");
                            if (!(value is null))
                            {
                                if (!(value.LastChild is null))
                                {
                                    pulseCount = float.Parse(value.LastChild.Value);
                                }
                            }
                            value = hp.SelectSingleNode("descendant::Fire_Pulse_Delay_Seconds");
                            if (!(value is null))
                            {
                                if (!(value.LastChild is null))
                                {
                                    pulseDelay = float.Parse(value.LastChild.Value);
                                }
                            }
                            /*value = hp.SelectSingleNode("descendant::Fire_Cone_Width");
                            if (!(value is null))
                            {
                                if (!(value.LastChild is null))
                                {
                                    coneWidth = float.Parse(value.LastChild.Value);
                                }
                            }
                            value = hp.SelectSingleNode("descendant::Fire_Cone_Height");
                            if (!(value is null))
                            {
                                if (!(value.LastChild is null))
                                {
                                    coneHeight = float.Parse(value.LastChild.Value);
                                }
                            }*/
                            value = hp.SelectSingleNode("descendant::Fire_Range_Distance");
                            if (!(value is null))
                            {
                                if (!(value.LastChild is null))
                                {
                                    range = float.Parse(value.LastChild.Value);
                                }
                            }
                            value = hp.SelectSingleNode("descendant::Full_Salvo_Weapon_Delay_Multiplier");
                            if (!(value is null))
                            {
                                if (!(value.LastChild is null))
                                {
                                    fullsalvomod = float.Parse(value.LastChild.Value);
                                }
                            }
                            value = hp.SelectSingleNode("descendant::Fire_Category_Restrictions");
                            if (!(value is null))
                            {
                                XmlNode res = value.LastChild;
                                if (!(res is null))
                                {
                                    string[] split = fullTrim(res.Value).Split(',');
                                    foreach (string restrict in split)
                                    {
                                        inaccuracyTypes.Add(restrict);
                                        inaccuracyAmounts.Add(100);
                                    }
                                }
                            }
                            XmlNodeList inaccs = hp.SelectNodes("descendant::Fire_Inaccuracy_Distance");
                            foreach (XmlNode inacc in inaccs)
                            {
                                if (!(inacc is null))
                                {
                                    if (!(inacc.LastChild is null))
                                    {
                                        string[] vals = fullTrim(inacc.LastChild.Value).Split(',');
                                        if (vals.Length > 1)
                                        {
                                            inaccuracyTypes.Add(vals[0]);
                                            inaccuracyAmounts.Add(float.Parse(vals[1]));
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                        hardpoint hard = new hardpoint
                        {
                            name = name,
                            projectile = Find_Text_Entry(text),
                            quantity = 1,
                            damageType = damageType,
                            hpType = hpType,
                            targetable = targetable,
                            hp = health,
                            damageAmount = damageAmount,
                            blastRadius = blastRadius,
                            recharge = recharge,
                            pulseCount = pulseCount,
                            pulseDelay = pulseDelay,
                            //coneWidth = coneWidth,
                            //coneHeight = coneHeight,
                            range = range,
                            inaccuracyTypes = inaccuracyTypes,
                            inaccuracyAmounts = inaccuracyAmounts,
                            fullsalvomod = fullsalvomod,
                        };
                        entities.hardpoints.Add(hard);
                    }
                }
            }
        }
        entities.hardpoints.Sort((s1, s2) => s1.name.CompareTo(s2.name));

        entities.hardpointhashes = new List<List<int>>();
        for (int i = 0; i < entities.hardpoints.Count; i++)
        {
            int index = LookupUntemplateID(entities.hardpoints[i].name);
            if (entities.hardpointhashes.Count <= index)
            {
                for (int j = entities.hardpointhashes.Count; j <= index; j++) entities.hardpointhashes.Add(new List<int>());
            }
            entities.hardpointhashes[index].Add(i);
        }
    }

    public static string ReadXMLElement(string line)
    {
        int start = line.IndexOf(">") + 1;
        int end = line.LastIndexOf("<");

        return line.Substring(start, end - start);
    }

    public static void parsePlanetsTheHardWay(entities entities, bool allplanets = false)
    {//beating builtin functions for efficiency turns out to be quite difficult
        string path;
        if (allplanets) path = entities.modpaths[entities.modpaths.Count - 1] + "\\XML\\Planets.xml";
        else path = getModFile("XML\\Planets.xml");
        entities.Planets.Clear();
        entities.PlanetBounds = 0;
        string[] lines = File.ReadAllLines(path);

        string codename = "";
        string username = "";
        string description = "";
        string population_desc = "";
        string fauna = "";
        float x = 0;
        float y = 0;
        int credits = 0;
        int shipyard = 0;
        string groundMap = "";
        string spaceMap = "";
        string terrain = "";
        string mapTerrain = "";
        bool has_ground = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (line.Contains("<Planet Name="))
            {
                username = line.Substring(line.IndexOf("\"") + 1, line.IndexOf(">") - line.IndexOf("\"") - 2);
                description = "";
                population_desc = "";
                fauna = "";
                x = 0;
                y = 0;
                credits = 0;
                shipyard = 0;
                groundMap = "";
                spaceMap = "";
                terrain = "";
                has_ground = false;
            }
            else if (line.Contains("</Planet>"))
            {
                planet planet_obj = new planet
                {
                    codename = codename,
                    username = username,
                    desc_history = description,
                    x_coord = x,
                    y_coord = -y, //Inversion intentional
                    credits = credits,
                    groundMap = groundMap,
                    spaceMap = spaceMap,
                    terrain = terrain,
                    has_ground = has_ground,
                    desc_pop = population_desc,
                    shipyard = shipyard,
                    desc_fauna = fauna,
                };
                entities.Planets.Add(planet_obj);
            }
            else if (line.Contains("<Text_ID>")) username = Find_Text_Entry(ReadXMLElement(line));
            else if (line.Contains("<Galactic_Position>"))
            {
                string[] split = ReadWhiteSpaceAsCommas(ReadXMLElement(line));
                x = Single.Parse(split[0]);
                y = Single.Parse(split[1]);
                entities.PlanetBounds = Math.Max(entities.PlanetBounds, x);
                entities.PlanetBounds = Math.Max(entities.PlanetBounds, -x);
                entities.PlanetBounds = Math.Max(entities.PlanetBounds, y);
                entities.PlanetBounds = Math.Max(entities.PlanetBounds, -y);
            }
            else if (line.Contains("<Planet_Surface_Accessible>") && line.ToLower().Contains("yes")) has_ground = true;
            else if (line.Contains("<Planet_Credit_Value>")) credits = Int32.Parse(ReadXMLElement(line));
            else if (line.Contains("<Planet_Ability_Name>"))
            {
                switch (ReadXMLElement(line))
                {
                    case "TEXT_PLANET_LIGHT":
                        shipyard = 1;
                        break;
                    case "TEXT_PLANET_HEAVY":
                        shipyard = 2;
                        break;
                    case "TEXT_PLANET_CAPITAL":
                        shipyard = 3;
                        break;
                    case "TEXT_PLANET_DREAD":
                        shipyard = 4;
                        break;
                }
            }
            else if (line.Contains("<Land_Tactical_Map>"))
            {
                groundMap = ReadXMLElement(line);
                if (groundMap != "")
                {
                    string mappath = getModFile("Art\\Maps\\" + groundMap);
                    if (mappath != "")
                    {
                        byte[] data = File.ReadAllBytes(mappath); //byte 46 is terrain type
                        switch (data[46])
                        {
                            default:
                                mapTerrain = "Temperate";
                                break;
                            case 1:
                                mapTerrain = "Arctic";
                                break;
                            case 2:
                                mapTerrain = "Desert";
                                break;
                            case 3:
                                mapTerrain = "Forest";
                                break;
                            case 4:
                                mapTerrain = "Swamp";
                                break;
                            case 5:
                                mapTerrain = "Volcanic";
                                break;
                            case 6:
                                mapTerrain = "Space";
                                break;
                            case 7:
                                mapTerrain = "Space";
                                break;
                        }
                    }
                }
                else if (line.Contains("<Space_Tactical_Map>")) groundMap = ReadXMLElement(line);
            }
        }

        entities.Planets.Sort((s1, s2) => s1.codename.CompareTo(s2.codename));
    }

    public static void parsePlanetsXMLLoop(entities entities, bool allplanets = false)
    {//If there were any gains, they were modest
        string path;
        if (allplanets) path = entities.modpaths[entities.modpaths.Count-1] + "\\XML\\Planets.xml";
        else path = getModFile("XML\\Planets.xml");
        entities.Planets.Clear();
        entities.PlanetBounds = 0;
        XmlDocument doc = new XmlDocument();
        doc.PreserveWhitespace = true;
        doc.Load(path);
        XmlNode root = doc.DocumentElement;
        XmlNodeList planets = root.SelectNodes("*");
        foreach (XmlNode planet in planets)
        {
            string codename = planet.Attributes[0].Value;
            string username = "";
            string description = "";
            string population_desc = "";
            string fauna = "";
            float x = 0;
            float y = 0;
            int credits = 0;
            int shipyard = 0;
            string groundMap = "";
            string spaceMap = "";
            string terrain = "";
            string mapTerrain = "";
            bool has_ground = false;

            XmlNodeList xmlNodes = planet.SelectNodes("*");
            foreach (XmlNode value in xmlNodes)
            {
                switch (value.Name)
                {
                    case "Text_ID":
                        username = Find_Text_Entry(value.InnerText);
                        break;
                    case "Galactic_Position":
                        string[] split = ReadWhiteSpaceAsCommas(value.InnerText);
                        x = Single.Parse(split[0]);
                        y = Single.Parse(split[1]);
                        entities.PlanetBounds = Math.Max(entities.PlanetBounds, x);
                        entities.PlanetBounds = Math.Max(entities.PlanetBounds, -x);
                        entities.PlanetBounds = Math.Max(entities.PlanetBounds, y);
                        entities.PlanetBounds = Math.Max(entities.PlanetBounds, -y);
                        break;
                    case "Planet_Surface_Accessible":
                        if (value.InnerText.ToLower().Contains("yes")) has_ground = true;
                        break;
                    case "Describe_History":
                        description = value.InnerText;
                        break;
                    case "Describe_Wildlife":
                        population_desc = value.InnerText;
                        break;
                    case "Planet_Credit_Value":
                        credits = Int32.Parse(value.InnerText);
                        break;
                    case "Planet_Ability_Name":
                        switch (value.InnerText)
                        {
                            case "TEXT_PLANET_LIGHT":
                                shipyard = 1;
                                break;
                            case "TEXT_PLANET_HEAVY":
                                shipyard = 2;
                                break;
                            case "TEXT_PLANET_CAPITAL":
                                shipyard = 3;
                                break;
                            case "TEXT_PLANET_DREAD":
                                shipyard = 4;
                                break;
                        }
                        break;
                    case "Land_Tactical_Map":
                        groundMap = value.InnerText;
                        if (groundMap != "")
                        {
                            string mappath = getModFile("Art\\Maps\\" + groundMap);
                            if (mappath != "")
                            {
                                byte[] data = File.ReadAllBytes(mappath); //byte 46 is terrain type
                                switch (data[46])
                                {
                                    default:
                                        mapTerrain = "Temperate";
                                        break;
                                    case 1:
                                        mapTerrain = "Arctic";
                                        break;
                                    case 2:
                                        mapTerrain = "Desert";
                                        break;
                                    case 3:
                                        mapTerrain = "Forest";
                                        break;
                                    case 4:
                                        mapTerrain = "Swamp";
                                        break;
                                    case 5:
                                        mapTerrain = "Volcanic";
                                        break;
                                    case 6:
                                        mapTerrain = "Space";
                                        break;
                                    case 7:
                                        mapTerrain = "Space";
                                        break;
                                }
                            }
                        }
                        break;
                    case "Space_Tactical_Map":
                        spaceMap = value.InnerText;
                        break;
                }
            }

        planet planet_obj = new planet
            {
                codename = codename,
                username = username,
                desc_history = description,
                x_coord = x,
                y_coord = -y, //Inversion intentional
                credits = credits,
                groundMap = groundMap,
                spaceMap = spaceMap,
                terrain = terrain,
                has_ground = has_ground,
                desc_pop = population_desc,
                shipyard = shipyard,
                desc_fauna = fauna,
            };
            entities.Planets.Add(planet_obj);
        }

        entities.Planets.Sort((s1, s2) => s1.codename.CompareTo(s2.codename));
    }

    public static string getTerrainType(string map)
    {
        string mapTerrain = "";
        string mappath = getModFile("Art\\Maps\\" + map); //todo get preread value from meg
        if (mappath != "")
        {
            byte[] data = File.ReadAllBytes(mappath); //byte 46 is terrain type
            switch (data[46])
            {
                default:
                    mapTerrain = "Temperate";
                    break;
                case 1:
                    mapTerrain = "Arctic";
                    break;
                case 2:
                    mapTerrain = "Desert";
                    break;
                case 3:
                    mapTerrain = "Forest";
                    break;
                case 4:
                    mapTerrain = "Swamp";
                    break;
                case 5:
                    mapTerrain = "Volcanic";
                    break;
                case 6:
                    mapTerrain = "Space";
                    break;
                case 7:
                    mapTerrain = "Space";
                    break;
            }
        }

        return mapTerrain;
    }

    public static void parsePlanets(entities entities, bool allplanets = false)
    {//todo: split parse and file finding functions. Search the hard way for files unless allplanets is set
        XmlDocument doc = new XmlDocument();
        if (allplanets) doc.Load(entities.modpaths[entities.modpaths.Count - 1] + "\\XML\\Planets.xml");
        else doc = readModXmlOrMeg("XML\\Planets.xml", entities);
        if (doc is null) return; //Todo: need to massively rethink and support variant of existing type for the general case.
        entities.Planets.Clear();
        entities.PlanetBounds = 0;
        XmlNode root = doc.DocumentElement;
        if (root is null) return;
        XmlNodeList planets = root.SelectNodes("*");
        foreach (XmlNode planet in planets)
        {
            string codename = planet.Attributes[0].Value;
            string username = "";
            string description = "";
            string population_desc = "";
            string fauna = "";
            float x = 0;
            float y = 0;
            int credits = 0;
            int shipyard = 0;
            int land_structures = 0;
            int max_starbase = 0;
            string groundMap = "";
            string spaceMap = "";
            string terrain = "";
            string weather = "";
            bool has_ground = false;
            bool tradehub = false;

            XmlNode value = planet.SelectSingleNode("descendant::Text_ID");
            if (!(value is null))
            {
                username = Find_Text_Entry(value.InnerText);
            }
            value = planet.SelectSingleNode("descendant::Galactic_Position");
            if (!(value is null))
            {
                string[] split = ReadWhiteSpaceAsCommas(value.InnerText);
                x = Single.Parse(split[0]);
                y = Single.Parse(split[1]);
                entities.PlanetBounds = Math.Max(entities.PlanetBounds, x);
                entities.PlanetBounds = Math.Max(entities.PlanetBounds, -x);
                entities.PlanetBounds = Math.Max(entities.PlanetBounds, y);
                entities.PlanetBounds = Math.Max(entities.PlanetBounds, -y);
            }
            value = planet.SelectSingleNode("descendant::Planet_Surface_Accessible");
            if (!(value is null))
            {
                if (value.InnerText.ToLower().Contains("yes")) has_ground = true;
            }
            value = planet.SelectSingleNode("descendant::Describe_History");
            if (!(value is null))
            {
                description = value.InnerText;
            }
            value = planet.SelectSingleNode("descendant::Describe_Population");
            if (!(value is null))
            {
                population_desc = value.InnerText;
            }
            value = planet.SelectSingleNode("descendant::Describe_Wildlife");
            if (!(value is null))
            {
                fauna = value.InnerText;
            }
            value = planet.SelectSingleNode("descendant::Encyclopedia_Weather_Name");
            if (!(value is null))
            {
                weather = value.InnerText;
                if (weather.Contains("_TRADE")) tradehub = true;

            }
            /*value = planet.SelectSingleNode("descendant::Terrain");
            if (!(value is null))
            {
                terrain = value.InnerText;
            }*/
            value = planet.SelectSingleNode("descendant::Planet_Credit_Value");
            if (!(value is null))
            {
                credits = Int32.Parse(value.InnerText);
            }
            value = planet.SelectSingleNode("descendant::Special_Structures_Land");
            if (!(value is null))
            {
                land_structures = Int32.Parse(value.InnerText);
            }
            value = planet.SelectSingleNode("descendant::Max_Space_Base");
            if (!(value is null))
            {
                max_starbase = Int32.Parse(value.InnerText);
            }
            value = planet.SelectSingleNode("descendant::Planet_Ability_Name");
            if (!(value is null))
            {
                switch (value.InnerText)
                {
                    case "TEXT_PLANET_LIGHT":
                        shipyard = 1;
                        break;
                    case "TEXT_PLANET_HEAVY":
                        shipyard = 2;
                        break;
                    case "TEXT_PLANET_CAPITAL":
                        shipyard = 3;
                        break;
                    case "TEXT_PLANET_DREAD":
                        shipyard = 4;
                        break;
                }
            }
            value = planet.SelectSingleNode("descendant::Land_Tactical_Map");
            if (!(value is null))
            {
                groundMap = value.InnerText;
            }
            value = planet.SelectSingleNode("descendant::Space_Tactical_Map");
            if (!(value is null))
            {
                spaceMap = value.InnerText;
            }

            planet planet_obj = new planet
            {
                codename = codename,
                username = username,
                desc_history = description,
                x_coord = x,
                y_coord = -y, //Inversion intentional
                credits = credits,
                groundMap = groundMap,
                spaceMap = spaceMap,
                terrain = terrain,
                has_ground = has_ground,
                desc_pop = population_desc,
                shipyard = shipyard,
                desc_fauna = fauna,
                land_structures = land_structures,
                max_starbase = max_starbase,
                desc_weather = weather,
                tradehub = tradehub,
            };
            entities.Planets.Add(planet_obj);
        }

        entities.Planets.Sort((s1, s2) => s1.codename.CompareTo(s2.codename));
    }

    public static void parseGCs(entities entities)
    {
        entities.Conquests.Clear();
        XmlDocument gcdoc = readModXmlOrMeg("XML\\CampaignFiles.xml", entities);
        XmlNode gcroot = gcdoc.DocumentElement;

        XmlNodeList files = gcroot.SelectNodes("*");

        foreach (XmlNode file in files)
        {
            if (!(file is null))
            {
                if (!(file.LastChild is null))
                {
                    string filename = file.LastChild.Value.Trim();
                    XmlDocument doc = readModXmlOrMeg("XML\\" + filename, entities);

                    XmlNode root = doc.DocumentElement;
                    if (root is null) continue; //Skip files that don't exist
                    XmlNodeList campaigns = root.SelectNodes("*");

                    foreach (XmlNode campaign in campaigns)
                    {
                        string codename = campaign.Attributes[0].Value;
                        bool CCoGM = false;
                        if (codename.Contains("_CCoGM"))
                        {
                            CCoGM = true;
                            //codename = codename.Replace("_CCoGM", "");
                        }
                        string set = "";
                        string username = "";
                        string desc = "";
                        string[] planets = new string[0];
                        string faction = "";
                        string[] routes = new string[0];
                        List<string> factionsPresent = new List<string>();
                        List<List<string>> forceOwner = new List<List<string>>();
                        List<List<string>> forceLocation = new List<List<string>>();
                        List<List<string>> forceType = new List<List<string>>();
                        GCType Type = GCType.Infinity; //Probably the most useful default case
                        if (filename.Contains("\\Progressive\\")) Type = GCType.Progressive;
                        else if (filename.Contains("\\Regional\\")) Type = GCType.Regional;
                        else if (filename.Contains("\\Historical\\")) Type = GCType.Historical;
                        else if (filename.Contains("\\Custom\\") && !filename.Contains("_Influencers_Era_1") || filename.Contains("\\FTGU\\") && !filename.Contains("FTGU_Era_1")) Type = GCType.InfinityLayoutCopy;

                        bool addCampaign = true;

                        XmlNode value = campaign.SelectSingleNode("descendant::Campaign_Set");
                        try
                        {
                            if (!(value is null))
                            {
                                if (!(value.LastChild is null))
                                {
                                    set = value.LastChild.Value.Trim();
                                }
                            }
                            //if (CCoGM) set = set.Replace("_CCoGM", "");
                            if (set.Contains("Era")) username = set.Replace("_"," ");
                            else
                            {
                                value = campaign.SelectSingleNode("descendant::Text_ID");
                                if (!(value is null))
                                {
                                    if (!(value.LastChild is null))
                                    {
                                        username = Find_Text_Entry(fullTrim(value.LastChild.Value));
                                    }
                                }
                            }
                            value = campaign.SelectSingleNode("descendant::Description_Text"); //todo read lua instead if present
                            if (!(value is null))
                            {
                                if (!(value.LastChild is null))
                                {
                                    desc = fullTrim(value.LastChild.Value);
                                }
                            }
                            value = campaign.SelectSingleNode("descendant::Locations");
                            if (!(value is null))
                            {
                                if (!(value.LastChild is null))
                                {
                                    planets = ReadWhiteSpaceAsCommas(value.LastChild.Value);
                                    if (planets.Length <= 5) continue; //It's some loader or other such fake GC, hopefully
                                }
                            }
                            value = campaign.SelectSingleNode("descendant::Trade_Routes");
                            if (!(value is null))
                            {
                                if (!(value.LastChild is null))
                                {
                                    routes = ReadWhiteSpaceAsCommas(value.LastChild.Value);
                                }
                            }
                            value = campaign.SelectSingleNode("descendant::Starting_Active_Player");
                            if (!(value is null))
                            {
                                if (!(value.LastChild is null))
                                {
                                    if (CCoGM) faction = "CCoGM";
                                    else faction = fullTrim(value.LastChild.Value);

                                    bool match = false;
                                    for (int i = entities.Conquests.Count-1; i >= 0; i--) //It'll almost alway be the last one
                                    {
                                        galacticConquest conq = entities.Conquests[i];
                                        if (conq.campaign_set == set)
                                        {
                                            match = true;
                                            bool match2 = false;
                                            if (conq.planets.Length == planets.Length)
                                            {
                                                bool match3 = false;
                                                for(int j = 0; j < planets.Length; j++)
                                                {
                                                    if (conq.planets[j] != planets[j])
                                                    {
                                                        match3 = true;
                                                        break;
                                                    }
                                                }
                                                if (match3)
                                                {
                                                    if (!conq.username.Contains("{"))
                                                    {
                                                        conq.username = conq.username += " {" + FactionNameFromCode(conq.factionsPlayable[0], entities) + "}"; //todo May need to handle cases for multiple factions that share layout but not with others? Custom FTGU Proteus is probably fine like this. A few lines down too. Twice
                                                        entities.Conquests[i] = conq;
                                                    }
                                                    break;
                                                }
                                                match2 = true;
                                            }
                                            if (match2)
                                            {
                                                conq.factionsPlayable.Add(faction);
                                                XmlNodeList valuez = campaign.SelectNodes("descendant::Starting_Forces");
                                                List<string> owners = new List<string>();
                                                List<string> locations = new List<string>();
                                                List<string> types = new List<string>();
                                                foreach (XmlNode force in valuez)
                                                {
                                                    string[] forcedata = ReadWhiteSpaceAsCommas(force.InnerText);
                                                    if (!forcedata[2].Contains("Custom_GC_") && !forcedata[2].Contains("Era_"))
                                                    {
                                                        if (!factionsPresent.Contains(forcedata[0])) factionsPresent.Add(forcedata[0]);
                                                        owners.Add(forcedata[0]);
                                                        locations.Add(forcedata[1]);
                                                        types.Add(forcedata[2]);
                                                    }
                                                }
                                                entities.Conquests[i] = conq;
                                                entities.Conquests[i].forceOwner.Add(owners);
                                                entities.Conquests[i].forceLocation.Add(locations);
                                                entities.Conquests[i].forceType.Add(types);
                                                addCampaign = false;
                                                break;
                                            }
                                            else
                                            {
                                                if (!conq.username.Contains("{"))
                                                {
                                                    conq.username = conq.username += " {" + FactionNameFromCode(conq.factionsPlayable[0], entities) + "}";
                                                    entities.Conquests[i] = conq;
                                                }
                                            }
                                        }
                                        else break;
                                    }
                                    if (match && addCampaign)
                                    {
                                        username = username += " {" + FactionNameFromCode(faction, entities) + "}";
                                    }
                                }
                            }
                            if (addCampaign)
                            {
                                XmlNodeList values = campaign.SelectNodes("descendant::Starting_Forces");
                                List<string> owners = new List<string>();
                                List<string> locations = new List<string>();
                                List<string> types = new List<string>();

                                foreach (XmlNode force in values)
                                {
                                    string[] forcedata = ReadWhiteSpaceAsCommas(force.InnerText);
                                    if (!forcedata[2].Contains("Custom_GC_") && !forcedata[2].Contains("Era_"))
                                    {
                                        if (!factionsPresent.Contains(forcedata[0])) factionsPresent.Add(forcedata[0]);
                                        owners.Add(forcedata[0]);
                                        locations.Add(forcedata[1]);
                                        types.Add(forcedata[2]);
                                    }
                                }
                                forceOwner.Add(owners);
                                forceLocation.Add(locations);
                                forceType.Add(types);
                            }
                        }
                        catch (Exception e)
                        {
                            entities.readerrors += "\n" + e.Message + " while parsing GC " + codename;
                        }
                        if (addCampaign)
                        {
                            List<planet> planetObjects = new List<planet>();
                            foreach (string planetname in planets)
                            {
                                if (planetname != "Galaxy_Core_Art_Model")
                                {//todo benchmark a hash setup
                                    planet planet = entities.Planets.FirstOrDefault(s => s.codename == planetname);
                                    if (!(planet.codename is null))
                                    {
                                        planetObjects.Add(planet);
                                    }
                                }
                            }

                            List<tradeRoute> traderouteObjects = new List<tradeRoute>();
                            foreach (string route in routes)
                            {
                                tradeRoute traderoute = entities.Routes.FirstOrDefault(s => s.name == route);
                                if (!(traderoute.name is null))
                                {
                                    traderouteObjects.Add(traderoute);
                                }
                            }

                            galacticConquest camp = new galacticConquest
                            {
                                codename = codename,
                                username = username,
                                factionsPlayable = new List<string> { faction },
                                campaign_set = set,
                                desc = Find_Text_Entry(desc),
                                factionsPresent = factionsPresent,
                                planets = planets,
                                traderoutes = routes,
                                forceOwner = forceOwner,
                                forceLocation = forceLocation,
                                forceType = forceType,
                                Type = Type,
                                planetObjects = planetObjects,
                                traderouteObjects = traderouteObjects,
                            };
                            entities.Conquests.Add(camp);
                        }
                    }
                }
            }
        }
    }

    public static void parseTradeRoutes(entities entities)
    {
        entities.Routes.Clear();
        XmlDocument trdoc = readModXmlOrMeg("XML\\TradeRouteFiles.xml", entities);
        XmlNode trroot = trdoc.DocumentElement;

        XmlNodeList files = trroot.SelectNodes("*");

        foreach (XmlNode file in files)
        {
            if (!(file.LastChild is null))
            {
                XmlDocument doc = readModXmlOrMeg("XML\\" + file.LastChild.Value.Trim(), entities);

                XmlNode root = doc.DocumentElement;
                if (root is null) continue; //Skip files that don't exist
                XmlNodeList routes = root.SelectNodes("descendant::TradeRoute");

                foreach (XmlNode route in routes)
                {
                    string name = route.Attributes[0].Value;
                    string[] planets = new string[2];
                    System.Xml.XmlNode value = route.SelectSingleNode("descendant::Point_A");
                    if (!(value is null))
                    {
                        planets[0] = value.LastChild.Value.Trim();
                    }
                    value = route.SelectSingleNode("descendant::Point_B");
                    if (!(value is null))
                    {
                        planets[1] = value.LastChild.Value.Trim();
                    }
                    tradeRoute tradeRoute = new tradeRoute
                    {
                        name = name,
                        planets = planets,
                    };
                    entities.Routes.Add(tradeRoute);
                }
            }
        }


    }

    public static List<quantizedObject> quantizedAdd(List<quantizedObject> l, quantizedObject q)
    {
        for(int i = 0; i < l.Count; i++)
        {
            if (quantizedequality(l[i], q))
            {
                quantizedObject q2 = l[i];
                q2.quantity += q.quantity;
                l[i] = q2;
                return l;
            }
        }
        l.Add(q);
        return l;
    }

    static bool quantizedequality(quantizedObject q1, quantizedObject q2)
    {
        if (q1.codename != q2.codename) return false;
        if (q1.username != q2.username) return false;
        return true;
    }

    public static bool hpEquality(hardpoint hp1, hardpoint hp2) //all but name and quantity
    {
        if (hp1.projectile != hp2.projectile) return false;
        if (hp1.damageType != hp2.damageType) return false;
        if (hp1.targetable != hp2.targetable) return false;
        if (hp1.hpType != hp2.hpType) return false;
        if (hp1.hp != hp2.hp) return false;
        if (hp1.damageAmount != hp2.damageAmount) return false;
        if (hp1.blastRadius != hp2.blastRadius) return false;
        if (hp1.recharge != hp2.recharge) return false;
        if (hp1.pulseCount != hp2.pulseCount) return false;
        if (hp1.pulseDelay != hp2.pulseDelay) return false;
        if (hp1.fullsalvomod != hp2.fullsalvomod) return false;
        //if (hp1.coneWidth != hp2.coneWidth) return false; //This is a pretty bad reason to split
        //if (hp1.coneHeight != hp2.coneHeight) return false;
        if (hp1.range != hp2.range) return false;
        if (hp1.inaccuracyTypes is null)
        {
            if (!(hp2.inaccuracyTypes is null)) return false;
        }
        else
        {
            if (hp1.inaccuracyTypes.Count != hp2.inaccuracyTypes.Count) return false;
            for (int i = 0; i < hp1.inaccuracyTypes.Count; i++)
            {
                if (hp1.inaccuracyTypes[i] != hp2.inaccuracyTypes[i]) return false;
            }
            if (hp1.inaccuracyAmounts.Count != hp2.inaccuracyAmounts.Count) return false;
            for (int i = 0; i < hp1.inaccuracyAmounts.Count; i++)
            {
                if (hp1.inaccuracyAmounts[i] != hp2.inaccuracyAmounts[i]) return false;
            }
        }

        return true;
    }

    public static int LookupUntemplateID(string unitname)//Technically this is a hash. Perhaps there's a better option
    {
        string lower = unitname.ToLower();
        int sum = 0;
        foreach (char letter in lower)
        {
            sum += (int)letter;
        }
        return sum % 3000; // - unitname.Length * (int)' ' used to do this to keep the overall array size down, but a modulo works much better at capping overall size. Does cause a few more more hash collisions
    }

    public static List<List<int>> setUnitHashes(List<unit> units)
    {
        List<List<int>> lookuptable = new List<List<int>>();
        for (int i = 0; i < units.Count; i++)
        {
            int index = LookupUntemplateID(units[i].unitname);
            if (lookuptable.Count <= index)
            {
                for (int j = lookuptable.Count; j <= index; j++) lookuptable.Add(new List<int>());
            }
            lookuptable[index].Add(i);
        }

        return lookuptable;
    }

    public static void untemplate(entities entities)
    {
        List<List<int>> lookuptable = setUnitHashes(entities.objects);
        bool needs_templates = true;
        while (needs_templates)
        {
            needs_templates = false;
            for (int i = 0; i < entities.objects.Count; i++)
            {
                unit unidad = entities.objects[i];
                /*if (unidad.unitname == "T4A_Company") //parse debug
                {
                    bool visible = false;
                }*/
                if (unidad.variantof != "")
                {
                    int index = LookupUntemplateID(entities.objects[i].variantof);
                    if(index < lookuptable.Count)
                    {
                        foreach (int cachedindex in lookuptable[index]) //(unit unidad2 in entities.objects)
                        {
                            unit unidad2 = entities.objects[cachedindex];
                            if (unidad2.unitname == unidad.variantof)
                            {
                                if (unidad.affiliations.Count == 0) unidad.affiliations = unidad2.affiliations;
                                if (unidad.companyunits.Count == 0) unidad.companyunits = unidad2.companyunits;
                                if (unidad.categories.Count == 0) unidad.categories = unidad2.categories;
                                if (unidad.flags.Count == 0) unidad.flags = unidad2.flags;
                                if (unidad.Hardpoints.Length == 0) unidad.Hardpoints = unidad2.Hardpoints;
                                if (unidad.behaviors.Count == 0) unidad.behaviors = unidad2.behaviors;
                                if (unidad.modebehaviors.Count == 0) unidad.modebehaviors = unidad2.modebehaviors;
                                if (unidad.pop < 0)
                                {
                                    unidad.pop = unidad2.pop;
                                    unidad.pop_baseID = unidad2.pop_baseID + 1; //Default is -1. Either 0, the first variant in the chain (will be ovewritten if unidad2 is not the ultimate source), or one up from whereever it was
                                                                                //Not knowing where you are beforehand does mean things that are never defined claim to be defined by the first link in the chain
                                                                                //Adjusted in the postprocessing loop
                                }
                                if (unidad.cost < 0)
                                {
                                    unidad.cost = unidad2.cost;
                                    unidad.cost_baseID = unidad2.cost_baseID + 1;
                                }
                                if (unidad.buildtime < 0)
                                {
                                    unidad.buildtime = unidad2.buildtime;
                                    unidad.buildtime_baseID = unidad2.buildtime_baseID + 1;
                                }
                                if (unidad.skirmcost < 0)
                                {
                                    unidad.skirmcost = unidad2.skirmcost;
                                    unidad.skirmcost_baseID = unidad2.skirmcost_baseID + 1;
                                }
                                if (unidad.skirmbuildtime < 0)
                                {
                                    unidad.skirmbuildtime = unidad2.skirmbuildtime;
                                    unidad.skirmbuildtime_baseID = unidad2.skirmbuildtime_baseID + 1;
                                }
                                if (unidad.techlevel < 0) unidad.techlevel = unidad2.techlevel;
                                if (unidad.locked < 0) unidad.locked = unidad2.locked;
                                if (unidad.reqstructures == null && unidad2.reqstructures != null)
                                {
                                    unidad.reqstructures = unidad2.reqstructures;
                                    unidad.reqtemplate = unidad2.unitname;
                                    unidad.reqfile = unidad2.datafile;
                                }
                                if (unidad.reqorbit == "") unidad.reqorbit = unidad2.reqorbit;
                                if (unidad.shield_type == "")
                                {
                                    unidad.shield_type = unidad2.shield_type;
                                    unidad.stype_baseID = unidad2.stype_baseID + 1;
                                }
                                if (unidad.armor_type == "")
                                {
                                    unidad.armor_type = unidad2.armor_type;
                                    unidad.atype_baseID = unidad2.atype_baseID + 1;
                                }
                                if (unidad.unitclass == "") unidad.unitclass = unidad2.unitclass;
                                if (unidad.planets.Length == 0) unidad.planets = unidad2.planets;
                                if (unidad.tooltip == "") unidad.tooltip = unidad2.tooltip;
                                if (unidad.username == "") unidad.username = unidad2.username;
                                if (unidad.icon == "") unidad.icon = unidad2.icon;
                                if (unidad.model == "") unidad.model = unidad2.model;
                                if (unidad.terrainMaps.Count == 0) unidad.terrainMaps = unidad2.terrainMaps;
                                if (!unidad.hero) unidad.hero = unidad2.hero; //todo might want an indeterminate ternary in theory, but a generic unit varianting off a hero should be rare
                                if (unidad.BTS == "" && (unidad.hero == unidad2.hero)) unidad.BTS = unidad2.BTS; //BTS should not be inherited from units to heroes
                                if (unidad.bombingRunUnit == "") unidad.bombingRunUnit = unidad2.bombingRunUnit;
                                if (unidad.transport == "") unidad.transport = unidad2.transport;
                                if (unidad.locomotor_type == "") unidad.locomotor_type = unidad2.locomotor_type;
                                if (unidad.container == "") unidad.container = unidad2.container;
                                if (unidad.crew < 0)
                                {
                                    unidad.crew = unidad2.crew;
                                    unidad.crew_baseID = unidad2.crew_baseID + 1;
                                }
                                if (unidad.pop < 0) unidad.pop = unidad2.pop;
                                if (unidad.cp < 0) unidad.cp = unidad2.cp;
                                if (unidad.maintenance < 0)
                                {
                                    unidad.maintenance = unidad2.maintenance;
                                    unidad.maintenance_baseID = unidad2.maintenance_baseID + 1;
                                }
                                if (unidad.shield < 0)
                                {
                                    unidad.shield = unidad2.shield;
                                    unidad.shield_baseID = unidad2.shield_baseID + 1;
                                }
                                if (unidad.regen < 0)
                                {
                                    unidad.regen = unidad2.regen;
                                    unidad.regen_baseID = unidad2.regen_baseID + 1;
                                }
                                if (unidad.hp < 0)
                                {
                                    unidad.hp = unidad2.hp;
                                    unidad.hp_baseID = unidad2.hp_baseID + 1;
                                }
                                if (unidad.gui_row < 0)
                                {
                                    unidad.gui_row = unidad2.gui_row;
                                    unidad.gui_row_baseID = unidad2.gui_row_baseID + 1;
                                }
                                if (unidad.limit_concurrent < 0)
                                {
                                    unidad.limit_concurrent = unidad2.limit_concurrent;
                                    unidad.concurrent_baseID = unidad2.concurrent_baseID + 1;
                                }
                                if (unidad.limit_lifetime < 0)
                                {
                                    unidad.limit_lifetime = unidad2.limit_lifetime;
                                    unidad.lifetime_baseID = unidad2.lifetime_baseID + 1;
                                }
                                if (unidad.speed < 0)
                                {
                                    unidad.speed = unidad2.speed;
                                    unidad.speed_baseID = unidad2.speed_baseID + 1;
                                }
                                if (unidad.min_speed < 0)
                                {
                                    unidad.min_speed = unidad2.min_speed;
                                    unidad.min_speed_baseID = unidad2.min_speed_baseID + 1;
                                }
                                if (unidad.accel < 0)
                                {
                                    unidad.accel = unidad2.accel;
                                    unidad.accel_baseID = unidad2.accel_baseID + 1;
                                }
                                if (unidad.turn < 0)
                                {
                                    unidad.turn = unidad2.turn;
                                    unidad.turn_baseID = unidad2.turn_baseID + 1;
                                }
                                if (unidad.range < 0)
                                {
                                    unidad.range = unidad2.range;
                                    unidad.range_baseID = unidad2.range_baseID + 1;
                                }
                                if (unidad.percompany < 0) unidad.percompany = unidad2.percompany;
                                unidad.variantof = unidad2.variantof;
                                if (unidad.garrison_slots < 0)
                                {
                                    unidad.garrison_slots = unidad2.garrison_slots;
                                    unidad.garrisonSlots_baseID = unidad2.garrisonSlots_baseID + 1;
                                }
                                if (unidad.garrison_value < 0)
                                {
                                    unidad.garrison_value = unidad2.garrison_value;
                                    unidad.garrisonValue_baseID = unidad2.garrisonValue_baseID + 1;
                                }
                                if (unidad.garrison_type == "")
                                {
                                    unidad.garrison_type = unidad2.garrison_type;
                                    unidad.garrisonType_baseID = unidad2.garrisonType_baseID + 1;
                                }
                                if (unidad2.variantof != "")
                                {
                                    needs_templates = true; //Another iteration required
                                }
                                if (unidad.abilities.Count == 0) unidad.abilities = unidad2.abilities;
                                if (unidad.unitabilities.Count == 0) unidad.unitabilities = unidad2.unitabilities;
                                if (unidad.builtin.hpType is null) unidad.builtin = unidad2.builtin;
                                if (unidad.garrison.Count == 0) unidad.garrison = unidad2.garrison; //todo track id of variant in case editing of this field is someday enabled
                                entities.objects[i] = unidad;
                                break;
                            }
                        }
                    }
                }
            }
        }

        for (int i = 0; i < entities.objects.Count; i++)
        {
            unit unidad = entities.objects[i];
            if (unidad.reqstructures == null) unidad.reqstructures = "";
            if (unidad.locked < 0) unidad.locked = 0; //undefined evaluates to not locked
            unidad.structid = i;
            unidad.username = Find_Text_Entry(unidad.username);
            unidad.sortstring = unidad.username; //Default to no extra sort value display

            //can't create the variant chain in the main detemplating loop because the order is undefined:
            //a variant of a variant might inherit from the original template in two steps or from the midpoint that has already inherited
            if (unidad.variantbase != "")
            {
                string nextlink = unidad.variantbase;
                unidad.variantchain.Add(nextlink);
                bool needloop = true;
                while (needloop)
                {
                    needloop = false; //be optimistic about leaving until proven otherwise
                    for (int j = 0; j < entities.objects.Count; j++)
                    {
                        if (entities.objects[j].unitname == nextlink)
                        {
                            unit recurse = entities.objects[j];
                            if (recurse.variantchain.Count > 0)
                            {//if the unit's template already had its variant chain set, can simply copy it past the template
                                foreach (string link in recurse.variantchain) unidad.variantchain.Add(link);
                            }
                            else
                            {
                                if (recurse.variantbase != "")
                                {
                                    nextlink = recurse.variantbase;
                                    unidad.variantchain.Add(nextlink);
                                    needloop = true;
                                }
                            }
                            break;
                        }
                    }
                }
            }
            //Set values that are not defined to claim no definition instead of the base unit as a quirk of the process
            if (unidad.pop < 0) unidad.pop_baseID = -1;
            if (unidad.cost < 0) unidad.cost_baseID = -1;
            if (unidad.buildtime < 0) unidad.buildtime_baseID = -1;
            if (unidad.skirmcost < 0) unidad.skirmcost_baseID = -1;
            if (unidad.skirmbuildtime < 0) unidad.skirmbuildtime_baseID = -1;
            if (unidad.shield_type == "") unidad.stype_baseID = -1;
            if (unidad.armor_type == "") unidad.atype_baseID = -1;
            if (unidad.crew < 0) unidad.crew_baseID = -1;
            if (unidad.shield < 0) unidad.shield_baseID = -1;
            if (unidad.regen < 0) unidad.regen_baseID = -1;
            if (unidad.hp < 0) unidad.hp_baseID = -1;
            if (unidad.maintenance < 0) unidad.maintenance_baseID = -1;
            if (unidad.gui_row < 0) unidad.gui_row_baseID = -1;
            if (unidad.limit_concurrent < 0) unidad.concurrent_baseID = -1;
            if (unidad.limit_lifetime < 0) unidad.lifetime_baseID = -1;
            if (unidad.speed < 0) unidad.speed_baseID = -1;
            if (unidad.min_speed < 0) unidad.min_speed_baseID = -1;
            if (unidad.accel < 0) unidad.accel_baseID = -1;
            if (unidad.turn < 0) unidad.turn_baseID = -1;
            if (unidad.range < 0) unidad.range_baseID = -1;
            if (unidad.garrisonSlots_baseID < 0) unidad.garrisonSlots_baseID = -1;
            if (unidad.garrisonType_baseID < 0) unidad.garrisonType_baseID = -1;
            if (unidad.garrisonValue_baseID < 0) unidad.garrisonValue_baseID = -1;

            unidad.targetablehps = false;
            List<hardpoint> hps = new List<hardpoint>();
            unidad.consolidatedhps = consolidateHardpoints(unidad, hps);
            foreach (hardpoint hp in hps)
            {
                if (hp.targetable)
                {
                    unidad.targetablehps = true;
                }
                break;
            }

            if (unidad.terrainMaps.Count > 0)
            {
                List<string> newmaps = new List<string>();
                if (unidad.terrainMaps.Count > 1)
                {
                    for (int j = 0; j + 1 < unidad.terrainMaps.Count; j += 2)
                    {
                        if (unidad.terrainMaps[j + 1].ToLower() != unidad.model.ToLower())
                        {
                            newmaps.Add(unidad.terrainMaps[j]);
                        }
                    }
                }
                unidad.terrainMaps = newmaps;
            }

            if (unidad.reqstructures.Contains("Non_Capital_Category_Dummy")) unidad.level = 1;
            else if (unidad.reqstructures.Contains("Heavy_Frigate_Category_Dummy")) unidad.level = 2;
            else if (unidad.reqstructures.Contains("Capital_Category_Dummy")) unidad.level = 3;
            else if (unidad.reqstructures.Contains("Dreadnought_Category_Dummy")) unidad.level = 4;
           
            if (unidad.reqstructures.Contains("INFLUENCE_"))
            {
                int temp = 0;
                if (unidad.reqstructures.Contains("ONE")) temp = 1;
                else if (unidad.reqstructures.Contains("TWO")) temp = 2;
                else if (unidad.reqstructures.Contains("THREE")) temp = 3;
                else if (unidad.reqstructures.Contains("FOUR")) temp = 4;
                else if (unidad.reqstructures.Contains("FIVE")) temp = 5;
                else if (unidad.reqstructures.Contains("SIX")) temp = 6;
                else if (unidad.reqstructures.Contains("SEVEN")) temp = 7;
                else if (unidad.reqstructures.Contains("EIGHT")) temp = 8;
                else if (unidad.reqstructures.Contains("NINE")) temp = 9;
                else if (unidad.reqstructures.Contains("TEN")) temp = 10;
                if (unidad.influence <= 0 || unidad.influence > temp) unidad.influence = temp;
            }

            if (unidad.behaviors.Contains("FIGHTER_LOCOMOTOR") || unidad.modebehaviors.Contains("FIGHTER_LOCOMOTOR"))
            {
                if(unidad.categories.Contains("Gunship")) unidad.fightermode = 0;
                else if(unidad.bombingRunUnit != "") unidad.fightermode = 2;
                else unidad.fightermode = 1;
            }
            else unidad.fightermode = -1;

            if(unidad.garrison.Count > 0)
            {
                int highest_tech = 0; //The structure of parseing should mean the inital set is in ascending tech order
                List<garrison_entry> new_garrison = new List<garrison_entry>();
                foreach (garrison_entry initial_entry in unidad.garrison)
                {
                    int newtech = initial_entry.parsingtech;
                    bool found = false;
                    for (int j = 0; j < new_garrison.Count; j++)
                    {
                        garrison_entry new_gar = new_garrison[j];
                        if (newtech > highest_tech)
                        {
                            //If there is a gap in tech, copy last defined tech to all values in it
                            for (int techid = highest_tech + 1; techid < newtech; techid++)
                            {
                                new_gar.tech[techid] = new_gar.tech[highest_tech];
                                new_gar.upfront[techid] = new_gar.upfront[highest_tech];
                                new_gar.reserve[techid] = new_gar.reserve[highest_tech];
                            }
                        }
                        if (initial_entry.unitname == new_gar.unitname)
                        {
                            found = true;
                            new_gar.tech[newtech] = true;
                            new_gar.upfront[newtech] = initial_entry.parsingupfront;
                            new_gar.reserve[newtech] = initial_entry.parsingreserve;
                            new_garrison[j] = new_gar;
                            //do not break or any skipped units will not have tech updated
                        }
                    }
                    if (!found)
                    {
                        garrison_entry new_garr = new garrison_entry
                        {
                            unitname = initial_entry.unitname,
                            tech = new bool[6],
                            upfront = new int[6],
                            reserve = new int[6],
                        };
                        //todo get user facing name, squad size float, is bomber from unit data
                        new_garr.tech[newtech] = true;
                        new_garr.upfront[newtech] = initial_entry.parsingupfront;
                        new_garr.reserve[newtech] = initial_entry.parsingreserve;
                        new_garrison.Add(new_garr);
                    }
                    highest_tech = newtech;
                }
                for (int j = 0; j < new_garrison.Count; j++) //Copy last defined tech to any undefined up to the max
                {
                    garrison_entry new_gar = new_garrison[j];
                    for (int techid = highest_tech + 1; techid < 6; techid++)
                    {
                        new_gar.tech[techid] = new_gar.tech[highest_tech];
                        new_gar.upfront[techid] = new_gar.upfront[highest_tech];
                        new_gar.reserve[techid] = new_gar.reserve[highest_tech];
                    }
                    new_garrison[j] = new_gar;
                }
                unidad.garrison = new_garrison;
            }

            entities.objects[i] = unidad;
        }
    }

    public static void categorizeObjects(entities entities)
    {
        entities.spaceUnits.Clear();
        entities.groundCompanies.Clear();
        entities.groundUnits.Clear();
        entities.spaceHeroes.Clear();
        entities.heroCompanies.Clear();
        entities.groundHeroes.Clear();
        entities.structures.Clear();
        entities.containers.Clear();

        foreach(unit entity in entities.objects) //I love consistency!
        {
            if (entity.behaviors.Contains("DUMMY_GROUND_STRUCTURE") || entity.modebehaviors.Contains("DUMMY_GROUND_STRUCTURE"))
            {
                entities.structures.Add(entity);
            }
            else if ((entity.behaviors.Contains("DUMMY_ORBITAL_STRUCTURE") || entity.modebehaviors.Contains("DUMMY_ORBITAL_STRUCTURE")) && (entity.elementName != "W_DummyStructure" || entity.elementName != "MiscObject"))
            {
                entities.spaceStructures.Add(entity);
            }//todo fightermode > 0 once gunships are in a better spot
            else if (entity.fightermode >= 0 || entity.behaviors.Contains("DUMMY_SPACE_FIGHTER_SQUADRON") || entity.modebehaviors.Contains("DUMMY_SPACE_FIGHTER_SQUADRON"))
            {
                entities.fighters.Add(entity);
            }
            else if (entity.behaviors.Contains("SIMPLE_SPACE_LOCOMOTOR") || entity.modebehaviors.Contains("SIMPLE_SPACE_LOCOMOTOR"))
            {
                if (entity.hero) entities.spaceHeroes.Add(entity);
                else entities.spaceUnits.Add(entity);
            }
            else if (entity.percompany > 0)
            {
                if (entity.hero) entities.heroCompanies.Add(entity);
                else entities.groundCompanies.Add(entity);
            }
            else if (entity.behaviors.Contains("LAND_TEAM_CONTAINER_LOCOMOTOR") || entity.modebehaviors.Contains("LAND_TEAM_CONTAINER_LOCOMOTOR"))
            {
                entities.containers.Add(entity);
            }
            else if (entity.behaviors.Contains("WALK_LOCOMOTOR") || entity.behaviors.Contains("LAND_TEAM_INFANTRY_LOCOMOTOR") || entity.behaviors.Contains("FLYING_LOCOMOTOR") || entity.modebehaviors.Contains("WALK_LOCOMOTOR") || entity.modebehaviors.Contains("LAND_TEAM_INFANTRY_LOCOMOTOR") || entity.modebehaviors.Contains("FLYING_LOCOMOTOR"))
            {
                if (entity.hero) entities.groundHeroes.Add(entity);
                else entities.groundUnits.Add(entity);
            }
        }

        entities.spaceUnits.Sort((s1, s2) => s1.unitname.CompareTo(s2.unitname));
        entities.groundCompanies.Sort((s1, s2) => s1.unitname.CompareTo(s2.unitname));
        entities.groundUnits.Sort((s1, s2) => s1.unitname.CompareTo(s2.unitname));
        entities.structures.Sort((s1, s2) => s1.unitname.CompareTo(s2.unitname));
        entities.spaceHeroes.Sort((s1, s2) => s1.unitname.CompareTo(s2.unitname));
        entities.heroCompanies.Sort((s1, s2) => s1.unitname.CompareTo(s2.unitname));
        entities.groundHeroes.Sort((s1, s2) => s1.unitname.CompareTo(s2.unitname));
        entities.containers.Sort((s1, s2) => s1.unitname.CompareTo(s2.unitname));
    }

    public static List<hardpoint> consolidateHardpoints(unit unidad, List<hardpoint> hps)
    {
        if (!(unidad.builtin.hpType is null))
        {
            if (unidad.builtin.range < 0)
            {
                unidad.builtin.range = unidad.range;
            }
            bool found = false;
            for (int j = 0; j < hps.Count; j++)
            {
                if (hpEquality(hps[j], unidad.builtin))
                {
                    hardpoint increment = hps[j];
                    increment.quantity++;
                    hps[j] = increment;
                    found = true;
                    break;
                }
            }
            if (!found) hps.Add(unidad.builtin); ;
        }
        foreach (string hardpoint in unidad.Hardpoints)
        {
            int index = LookupUntemplateID(hardpoint);
            for (int j = 0; j < entities.hardpointhashes[index].Count; j++)
            {
                hardpoint hp2 = entities.hardpoints[entities.hardpointhashes[index][j]];
                if (hp2.name == hardpoint)
                {
                    bool found = false;
                    for (int k = 0; k < hps.Count; k++)
                    {
                        if (hpEquality(hp2, hps[k]))
                        {
                            hardpoint increment = hps[k];
                            increment.quantity++;
                            hps[k] = increment;
                            found = true;
                            break;
                        }
                    }
                    if (!found) hps.Add(hp2);
                    break;
                }
            }
        }

        return hps;
    }

    public static List<hardpoint> consolidateSubcompanyHardpoints(unit subcompany, List<hardpoint> hps)
    {
        foreach (hardpoint hp in subcompany.consolidatedhps)
        {
            bool found = false;
            for (int k = 0; k < hps.Count; k++)
            {
                if (hpEquality(hp, hps[k]))
                {
                    hardpoint increment = hps[k];
                    increment.quantity += hp.quantity;
                    hps[k] = increment;
                    found = true;
                    break;
                }
            }
            if (!found) hps.Add(hp);
        }
        return hps;
    }

    public static List<String> getGroundUnitLibrary(string unitname)
    {
        List<string> corenne = new List<string>();
        List<string> paths = new List<string>();//Check several old ways of doing this fr backwards compatibility
        paths.Add(getModFile("Scripts\\Library\\gameobjects\\ground\\company-objects\\" + unitname + ".lua"));//TODO I am only guessing this is the final version after mod content loader is dead
        paths.Add(getModFile("Scripts\\Library\\eawx-mod-" + entities.modid + "\\gameobjects\\ground\\company-objects\\" + unitname + ".lua"));
        paths.Add(getModFile("Scripts\\Library\\eawx-mod-" + entities.modid + "\\gameobjects\\company-objects\\" + unitname + ".lua"));

        foreach(string path in paths)
        {
            if (path != "") //fail return for getModFile
            {
                string[] UnitLib = File.ReadAllLines(path);
                string spawn = "";
                foreach (string line in UnitLib)
                {
                    if (line.Contains("[\""))
                    {//Between [" and "] is the spawn name. May need to handle the version without that in the future
                        spawn = line.Substring(line.IndexOf("[") + 2, line.IndexOf("]") - line.IndexOf("[") - 3);
                    }
                    if (line.Contains("Initial"))
                    {
                        string trimmed = fullTrim(line);
                        int amount = trimmed.IndexOf("Initial=");
                        string qty = trimmed.Substring(amount + 8, trimmed.IndexOf(",") - amount - 8);
                        amount = Int32.Parse(qty);
                        for (int i = 0; i < amount; i++) corenne.Add(spawn);
                    }
                }
            }
        }
        return corenne;
    }

    public static List<unit> unitToCompanyData(List<unit> companies, List<unit> units, List<unit> containers, bool skip_step2 = false)
    {//todo hash units, companies, and containers. But even in TR this is ~2 seconds and a minor gain
        string limpath = getModFile("Scripts\\Library\\BuildLimitLibrary.lua");
        string[] luabuild = new string[0];
        if (limpath != "") luabuild = File.ReadAllLines(limpath);
        for (int i = 0; i < companies.Count; i++)
        {
            unit company = companies[i];
            if (company.percompany > 0)
            {
                string caps = "\"" + company.unitname.ToUpper() + "\"";
                for (int j = 0; j < luabuild.Length; j++)
                {
                    if (luabuild[j].Contains(caps))
                    {
                        bool unclosed = true;
                        int k = j;
                        while (unclosed && k < luabuild.Length) //Shouldn't need the fallback if there are no syntax errors, but...
                        {
                            if (luabuild[k].Contains("}")) unclosed = false;
                            if (luabuild[k].Contains("current_limit"))
                            {
                                string trimmed = fullTrim(luabuild[k]);
                                int start = trimmed.LastIndexOf("=") + 1;
                                int end = trimmed.LastIndexOf(",");
                                if (trimmed.Contains("}")) end = trimmed.LastIndexOf("}");
                                if (end < 0) end = trimmed.Length;
                                string value = trimmed.Substring(start, end - start);
                                company.limit_concurrent = Int32.Parse(value);
                            }
                            if (luabuild[k].Contains("lifetime_limit"))
                            {
                                string trimmed = fullTrim(luabuild[k]);
                                int start = trimmed.LastIndexOf("=") + 1;
                                int end = trimmed.LastIndexOf(",");
                                if (trimmed.Contains("}")) end = trimmed.LastIndexOf("}"); //Not used when written in 2026-05-02, but let's be thorough! For that matter the next two lines never mattered as of now
                                if (end < 0) end = trimmed.Length;
                                string value = trimmed.Substring(start, end - start);
                                company.limit_lifetime = Int32.Parse(value);
                            }
                            k++;
                        }
                    }
                }

                if (company.consolidatedhps.Count == 0) company.consolidatedhps = new List<hardpoint>();
                if (company.subcompanies is null || company.subcompanies.Count == 0) company.subcompanies = new List<quantizedObject>();
                if (company.consolidatedUnits is null || company.consolidatedUnits.Count == 0) company.consolidatedUnits = new List<quantizedObject>();
                //If spawner setup, get the list of spawned objects from the spawner's lua file and replace the original list with it
                List<string> newcompanyunits = new List<string>();
                foreach (string target in company.companyunits)
                {
                    if (target.Contains("_Dummy"))
                    {
                        List<string> returned = getGroundUnitLibrary(target);
                        foreach (string unit in returned) newcompanyunits.Add(unit);
                    }
                    //else newcompanyunits.Add(target); //Will need this if spawners are ever mixed with regular units
                }
                if (newcompanyunits.Count > 0) company.companyunits = newcompanyunits;

                //Convert identical units in company (you know, most of them) into a multiplier instead of another pass through the loop
                foreach(string unidad in company.companyunits)
                {
                    quantizedObject q = new quantizedObject
                    {
                        quantity = 1,
                        codename = unidad,
                        username = "", //We don't know it yet, get it the same time as stats
                    };
                    company.consolidatedUnits = quantizedAdd(company.consolidatedUnits, q);
                }
                company.hp = 0;
                company.shield = 0;
                company.regen = 0;
                company.garrison_slots = 0;
                company.garrison_value = 0;
                bool use_unit_garrison = true;
                if (company.container != "")
                {
                    foreach(unit contain in containers)
                    {
                        if(contain.unitname == company.container)
                        {
                            use_unit_garrison = false;
                            company.garrison_value = contain.garrison_value;
                            company.garrison_type = contain.garrison_type; //The container probably should win even if the company has one defined
                            break;
                        }
                    }
                }
                for (int j = 0; j < company.consolidatedUnits.Count; j++)
                {
                    quantizedObject q = company.consolidatedUnits[j];
                    foreach (unit unit in units)
                    {
                        if (unit.unitname.ToUpper() == q.codename.ToUpper())
                        {
                            if (unit.percompany <= 0)
                            {
                                q.codename = unit.unitname; //Check upper for Lua spawns that use units that are probably capitalized
                                q.username = unit.username;
                                company.consolidatedUnits[j] = q;
                                if (company.BTS == "") company.BTS = unit.BTS;
                                foreach(string terrain in unit.terrainMaps)
                                {
                                    if (!company.terrainMaps.Contains(terrain)) company.terrainMaps.Add(terrain);
                                }
                                company.hp += unit.hp * q.quantity;
                                company.shield += unit.shield * q.quantity;
                                if (company.armor_type == "") company.armor_type = unit.armor_type; //There can't be many situations where armor types are mixed within a company
                                if (company.shield_type == "") company.shield_type = unit.shield_type; //And even if there are, that is going to confuse everything about any calcs using it
                                company.regen += unit.regen * q.quantity;
                                company.garrison_slots += unit.garrison_slots * q.quantity;
                                if(use_unit_garrison) company.garrison_value += unit.garrison_value * q.quantity;
                                if (company.locomotor_type == "") company.locomotor_type = unit.locomotor_type;
                                if (company.speed < 0) company.speed = unit.speed;
                                if (company.accel < 0) company.accel = unit.accel;
                                if (company.turn < 0) company.turn = unit.turn;
                                if (company.garrison_type == "") company.garrison_type = unit.garrison_type;
                                if (company.categories.Count == 0) company.categories = unit.categories;
                                if (company.flags.Count == 0) company.flags = unit.flags;
                                foreach(string behavior in unit.behaviors)
                                {
                                    if (!company.behaviors.Contains(behavior)) company.behaviors.Add(behavior);
                                }
                                foreach (string behavior in unit.modebehaviors)
                                {
                                    if (!company.modebehaviors.Contains(behavior)) company.modebehaviors.Add(behavior);
                                }
                                for (int k = 0; k < q.quantity; k++) company.consolidatedhps = consolidateHardpoints(unit, company.consolidatedhps);
                                //company.categories = unit.categories;
                            }
                            break;
                        }
                    }
                    if (!skip_step2 && companies != units)
                    {
                        foreach (unit unit in companies)
                        {
                            if (unit.unitname.ToUpper() == q.codename.ToUpper()) //Lua values return in all caps
                            {
                                quantizedObject q2 = new quantizedObject
                                {
                                    quantity = q.quantity,
                                    codename = unit.unitname,
                                    username = unit.username
                                };
                                company.subcompanies = quantizedAdd(company.subcompanies, q2);
                                break;
                            }
                        }
                    }
                }
            }
            companies[i] = company;
        }
        if (!skip_step2 && companies != units)
        {
            for (int i = 0; i < companies.Count; i++)
            {
                unit company = companies[i];
                List<hardpoint> hps = new List<hardpoint>();
                if (company.subcompanies.Count > 0)
                {
                    company.consolidatedUnits.Clear();
                    company.hp = 0;
                    company.shield = 0;
                    company.percompany = 0;
                    company.regen = 0;
                    company.garrison_slots = 0;
                    company.garrison_value = 0;
                    foreach (quantizedObject subcomp in company.subcompanies)
                    {
                        foreach (unit subcompany in companies)
                        {
                            if (subcompany.unitname == subcomp.codename)
                            {
                                company.percompany += subcompany.percompany * subcomp.quantity;
                                if (company.BTS == "") company.BTS = subcompany.BTS;
                                foreach (string terrain in subcompany.terrainMaps)
                                {
                                    if (!company.terrainMaps.Contains(terrain)) company.terrainMaps.Add(terrain);
                                }
                                company.hp += subcompany.hp * subcomp.quantity;
                                company.shield += subcompany.shield * subcomp.quantity;
                                if (company.armor_type == "") company.armor_type = subcompany.armor_type; //There can't be many situations where armor types are mixed within a company
                                if (company.shield_type == "") company.shield_type = subcompany.shield_type; //And even if there are, that is going to confuse everything about any calcs using it
                                company.regen += subcompany.regen * subcomp.quantity;
                                company.garrison_slots += subcompany.garrison_slots * subcomp.quantity;
                                company.garrison_value += subcompany.garrison_value * subcomp.quantity;
                                if (company.locomotor_type == "") company.locomotor_type = subcompany.locomotor_type;
                                if (company.speed < 0) company.speed = subcompany.speed;
                                if (company.accel < 0) company.accel = subcompany.accel;
                                if (company.turn < 0) company.turn = subcompany.turn;
                                if (company.garrison_type == "") company.garrison_type = subcompany.garrison_type;
                                if (company.categories.Count == 0) company.categories = subcompany.categories;
                                if (company.flags.Count == 0) company.flags = subcompany.flags;
                                foreach (string behavior in subcompany.behaviors)
                                {
                                    if (!company.behaviors.Contains(behavior)) company.behaviors.Add(behavior);
                                }
                                foreach (string behavior in subcompany.modebehaviors)
                                {
                                    if (!company.modebehaviors.Contains(behavior)) company.modebehaviors.Add(behavior);
                                }
                                for (int j = 0; j < subcomp.quantity; j++)
                                {
                                    company.consolidatedhps = consolidateSubcompanyHardpoints(subcompany, company.consolidatedhps);
                                    foreach (quantizedObject unit in subcompany.consolidatedUnits) company.consolidatedUnits = quantizedAdd(company.consolidatedUnits, unit);
                                }
                                //company.categories = unit.categories;
                                //else company.subcompanies.Add(unit.unitname); I don't believe there are any or ever will be any reason for triply nested companies
                            }
                        }
                    }
                }
                companies[i] = company;
            }
        }
        return companies;
    }

    public static string[] findUnitNameFile(unit unit, entities entities)
    {
        string[] corenne = new string[0];

        string lowername = unit.unitname.ToLower();
        string transport = unit.transport.ToLower();
        string path = getModFile("XML\\GameConstants.xml");
        XmlDocument consts = readModXmlOrMeg("XML\\GameConstants.xml", entities);
        XmlNodeList listsets = consts.DocumentElement.SelectNodes("descendant::ShipNameTextFiles");
        foreach (XmlNode listset in listsets)
        {
            string[] types = ReadWhiteSpaceAsCommas(listset.InnerText);
            if (types.Length > 1)
            {
                for (int i = 0; i + 1 < types.Length; i += 2)
                {
                    string low = types[i].ToLower();
                    if (low == lowername || low == transport)
                    {
                        string namepath = getModFile(RemoveTopLevelFolder(types[i + 1]));
                        if (File.Exists(namepath)) return File.ReadAllLines(namepath);
                    }
                }
            }
        }
        return corenne;
    }

}

public struct MEGentry
{
    public string filename;
    public int hash;
    public int MEGid;
    public int startindex;
    public int length;
}

public struct projectile
{
    public string name;
    public string variantof;
    public string damageType;
    public float damageAmount;
    public float blastRadius;
    public float speed;
    public float turn;
}
public struct hardpoint
{
    public string name;
    public string projectile;
    public string damageType;
    public string hpType;
    public int quantity;
    public bool targetable;
    public float hp;
    public float damageAmount;
    public float blastRadius;
    public float recharge; //average of max and min
    public float pulseCount;
    public float pulseDelay;
    //public float coneWidth;
    //public float coneHeight;
    public float range;
    public float fullsalvomod; //todo deploy and rocket attack handling
    public List<float> inaccuracyAmounts;
    public List<string> inaccuracyTypes;

    public override string ToString()
    {
        if (range < 0) return quantity.ToString() + "x " + projectile;
        string proj = projectile.Replace("Proj_", "").Replace("proj_", "").Replace("ship_", ""); //TODO I'm sure I've missed some colors
        proj = proj.Replace("_Blue", "").Replace("_blue", "");
        proj = proj.Replace("_Red", "").Replace("_red", "");
        proj = proj.Replace("_Green", "").Replace("_green", "");
        proj = proj.Replace("_Yellow", "").Replace("_yellow", "");
        proj = proj.Replace("_Purple", "").Replace("_purple", "");
        return quantity.ToString() + "x " + proj.Replace("_", " ") + ": " + pulseCount.ToString() + " / " + recharge.ToString("0.0") + "s / " + range.ToString();
    }
}

public struct quantizedObject
{
    public string codename;
    public string username;
    public int quantity;

    public override string ToString()
    {
        return quantity.ToString() + "x " + username;
    }
}

public struct faction
{
    public string codename;
    public string textname;
    public bool playable;

    public int[] color;
    public int[] tcolor;

    public override string ToString()
    {
        return textname;
    }
}

public struct unit
{
    public string variantof;
    public string variantbase;
    public string unitname;
    public string username;
    public string datafile;
    public string elementName;
    public string bombingRunUnit;
    public int cost;
    public int buildtime;
    public int skirmcost;
    public float skirmbuildtime; //Probably none of these are actually ints
    public List<string> affiliations;
    public int pop;
    public int techlevel;
    public int fightermode;
    public int locked; //A boolean, but there needs to be an indeterminate state for inheritance
    public bool targetablehps;
    public bool hero;
    public string reqstructures;
    public string reqorbit;
    public string reqtemplate;
    public string reqfile;
    public float cp;
    public float maintenance;
    public int percompany;
    public string shield_type;
    public string armor_type;
    public string unitclass;
    public string[] planets;
    public string tooltip;
    public string icon;
    public string model;
    public string transport;
    public string garrison_type;
    public string locomotor_type;
    public string container;
    public int crew;
    public int shield;
    public float regen;
    public int hp;
    public int gui_row;
    public int limit_concurrent;
    public int limit_lifetime;
    public int garrison_slots;
    public int garrison_value;
    public float speed;
    public float min_speed;
    public float accel;
    public float turn;
    public float range;
    public List<string> categories;
    public List<string> flags;
    public List<string> behaviors;
    public List<string> modebehaviors;
    public List<string> variantchain;
    public List<string> terrainMaps;
    public int cost_baseID;
    public int pop_baseID;
    public int crew_baseID;
    public int buildtime_baseID;
    public int skirmcost_baseID;
    public int skirmbuildtime_baseID;
    public int atype_baseID;
    public int stype_baseID;
    public int hp_baseID;
    public int shield_baseID;
    public int regen_baseID;
    public int gui_row_baseID;
    public int concurrent_baseID;
    public int lifetime_baseID;
    public int speed_baseID;
    public int min_speed_baseID;
    public int accel_baseID;
    public int turn_baseID;
    public int range_baseID;
    public int lomotor_baseID;
    public int garrisonSlots_baseID;
    public int garrisonValue_baseID;
    public int garrisonType_baseID;
    public int maintenance_baseID;
    public hardpoint builtin;
    public List<string> companyunits;
    public List<quantizedObject> consolidatedUnits;
    public List<quantizedObject> subcompanies;
    public string office;
    public string corporations; //filters in space
    public List<string> UsedStrucutures;
    public List<string> FullStrucutures;
    public string[] Hardpoints;
    public List<hardpoint> consolidatedhps;
    public int level;
    public int influence;
    public int structid;
    public string BTS;
    public string sortstring;
    public float sortfloat;
    public List<ability> abilities;
    public List<unitability> unitabilities;
    public List<garrison_entry> garrison;
    //public string weather;
    //public string movementclass;
    //public string lua_script;

    public override string ToString()
    {
        if (sortstring == username) return username;
        else if (sortstring != "") return "(" + sortstring + ") " + username;
        else return "(" + sortfloat.ToString("0.###") + ") " + username;
    }
}

public struct ability
{
    public string name;
    public string type;
    public string activation;
    public string linkedEntity;
    public string[] applicable_categories;
    public string[] applicable_types;
    public string[] excluded_types;

    public bool genericBool;
    public int stacking;
    public float genericValue;
    public float radius;
    public float minradius;
    public float maxradius;
    public float recharge;
    public float duration;
    public float damageBonus;
    public float speedBonus;
    public float defenseBonus;
    public float shieldBonus;
    public float healthBonus;

    public override string ToString()
    {
       return name;
    }
}

public struct unitability
{
    public string type; //todo get table for default values if alternates are not defined
    public string username; //Alternate_Name_Text
    public string desc; //Alternate_Description_Text
    public string icon; //Alternate_Icon_Name
    public string ability; //GUI_Activated_Ability_Name

    public float recharge;
    public float expiration;
    public float selfdamage;
    public float radius;
    public float speedMod;
    public float reloadMod;
    public float shieldMod;
    public float defenseMod;
    public float damageMod;

    public override string ToString()
    {
        return username;
    }
}

public struct garrison_entry
{
    public string unitname;
    public string username;
    public int[] upfront;
    public int[] reserve;
    public float squad_size;
    public int parsingtech;
    public int parsingupfront;
    public int parsingreserve;
    public bool bomber;
    public bool[] tech;
}

public struct garrison_lua
{
    public string unitname;
    public string username;
    public int upfront;
    public int reserve;
    public float squad_size;
    public bool bomber;
    public bool standard;
    public bool[] tech;
    public bool[] era; //also regime
    public List<string> ResearchRequired;
    public List<string> ResearchForbidden;
    public List<string> HeroOverrides;
}

public struct container
{
    public string name;
    public string garrison_type;
    public int garrison_value;
}

public struct planet
{
    public string codename;
    public string username;
    public string tooltip;
    public string desc_history;
    public string desc_pop;
    public string desc_fauna;
    public string desc_terrain;
    public string desc_weather;
    public string desc_tactical;
    public string desc_advantage;
    public string terrain;
    public string groundMap;
    public string spaceMap;
    public string destroyedMap;
    public faction owner; //For usage within a GC
    public bool has_ground;
    public bool tradehub;
    public float x_coord;
    public float y_coord;
    public int credits;
    public int destroyed_credits;
    public int pop;
    public int max_starbase;
    public int land_structures;
    public int shipyard;

    public override string ToString()
    {
        return username; //todo sort options
    }
}

public struct tradeRoute
{
    public string name;
    public string[] planets;
}

public enum GCType
{
    Progressive,
    Regional,
    Historical,
    Infinity,
    InfinityLayoutCopy
}

public struct galacticConquest
{
    public string codename;
    public string username;
    public string campaign_set;
    public string desc;
    public string[] planets;
    public string[] traderoutes;
    public List<string> factionsPlayable;
    public List<string> factionsPresent;
    public List<List<string>> forceOwner; //Minimally just for factions present, then benchmark to saving everything. Properly parse only on GCListboxClick
    public List<List<string>> forceLocation; //May be faster to make an array dimmed to match number of read Starting_Forces
    public List<List<string>> forceType;
    public GCType Type;
    public List<planet> planetObjects;
    public List<tradeRoute> traderouteObjects;

    public override string ToString()
    {
        return username;
    }
}

public struct entities {
    public static List<string> modpaths = new List<string>();
    public static List<Text_Entry> Text = new List<Text_Entry>();

    public static List<Byte[]> MEGdata = new List<byte[]>();
    public static List<MEGentry> MEGentries = new List<MEGentry>();
    public static List<List<int>> MEGhashes = new List<List<int>>();

    public static List<faction> factions = new List<faction>();

    public static List<projectile> projectiles = new List<projectile>();
    public static List<List<int>> projectilehashes = new List<List<int>>();

    public static List<hardpoint> hardpoints = new List<hardpoint>();
    public static List<List<int>> hardpointhashes = new List<List<int>>(); //Lookups are much better than looping through the list. This setup significantly beat FirstOrDefault(s => s.name == hardpoint); when benchmarked

    public static List<unit> objects = new List<unit>(); //Universal list for initial parsing

    public static List<unit> spaceUnits = new List<unit>();
    public static List<unit> groundCompanies = new List<unit>();
    public static List<unit> groundUnits = new List<unit>();
    public static List<unit> fighters = new List<unit>();

    /*public static List<List<int>> spaceUnithashes = new List<List<int>>();
    public static List<List<int>> groundCompanyhashes = new List<List<int>>();
    public static List<List<int>> groundUnithashes = new List<List<int>>();*/

    public static List<unit> spaceHeroes = new List<unit>();
    public static List<unit> heroCompanies = new List<unit>();
    public static List<unit> groundHeroes = new List<unit>();

    public static List<unit> structures = new List<unit>();
    public static List<unit> spaceStructures = new List<unit>();

    /*public static List<List<int>> structurehashes = new List<List<int>>();
    public static List<List<int>> spaceHerohashes = new List<List<int>>();
    public static List<List<int>> heroCompanyhashes = new List<List<int>>();
    public static List<List<int>> groundHerohashes = new List<List<int>>();*/

    public static List<unit> containers = new List<unit>();

    public static List<List<int>> containerhashes = new List<List<int>>();

    public static List<string> SpaceArmors = new List<string>();
    public static List<string> SpaceShields = new List<string>();
    public static List<string> GroundArmors = new List<string>();
    public static List<string> GroundShields = new List<string>();

    public static List<string> AllArmors = new List<string>();
    public static List<string> DamageTypes = new List<string>();
    public static List<string> SpaceDamageTypes = new List<string>();
    public static List<string> GroundDamageTypes = new List<string>();
    public static List<string> AllFlags = new List<string>();
    public static List<WeaponMods> ArmorMods = new List<WeaponMods>();
    public static List<ArmorMods> ArmorIndexedMods = new List<ArmorMods>();

    public static List<planet> Planets = new List<planet>();
    public static float PlanetBounds = 0;
    public static List<tradeRoute> Routes = new List<tradeRoute>();
    //public static List<List<int>> Routeshashes = new List<List<int>>();
    public static List<galacticConquest> Conquests = new List<galacticConquest>();

    public static List<string> AllCategories = new List<string>();
    public static List<string> SpaceCategories = new List<string>();
    public static List<string> GroundCategories = new List<string>();

    public static Bitmap MTmaster;
    public static List<IconData> IconData = new List<IconData>();

    public static string modid; //Should be deprecated in Rev 1.0, but keep around for compatibility
    public static string readerrors = "";
}

public class ArmorMods
{
    public string armorType;
    public List<ArmorMod> WeaponMods;
    public float average;
}

public class WeaponMods
{
    public string weaponType;
    public List<ArmorMod> HpMods;
    public List<ArmorMod> ShieldMods;
    public float median;
}

public class ArmorMod
{
    public string armorType;
    public float modifier;
}

public class IconData
{
    public string id;
    public int origin_x;
    public int origin_y;
    public int size_x;
    public int size_y;
}

public static class crcGlobals
{
    public static uint[] crcTable = new uint[256];

    public static void initTable()
    {
        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 1) == 1)
                {
                    crc = (crc >> 1) ^ 0xEDB88320;
                }
                else
                {
                    crc = (crc >> 1);
                }
            }
            crcTable[i] = crc & 0xFFFFFFFF;
        }
    }
}

public class Text_Entry : IComparable<Text_Entry>
{
    public string identifier;
    public string entry;
    public uint crc;

    public static byte[] toBytes(char[] value)
    {
        byte[] corenne = new byte[2 * value.Length];
        for (int i = 0; i < value.Length; i++)
        {
            byte[] temp = BitConverter.GetBytes(value[i]);
            corenne[2 * i] = temp[0];
            corenne[2 * i + 1] = temp[1];
        }
        return corenne;
    }

    public static Text_Entry FromCsv(string csvLine, char delimiter)
    {
        if (csvLine.Length == 0 || !csvLine.Contains(delimiter))
        {
            return new Text_Entry();
        }

        Text_Entry entry = new Text_Entry();
        int firstdelimit = csvLine.IndexOf(delimiter);
        entry.identifier = csvLine.Substring(0, firstdelimit);
        entry.entry = csvLine.Substring(firstdelimit + 1, csvLine.Length - firstdelimit - 1);

        uint check = 0xFFFFFFFF;
        byte[] win1252Bytes = Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding("windows-1252"), toBytes(entry.identifier.ToCharArray(0, entry.identifier.Length)));
        for (int j = 0; j < win1252Bytes.Length; j++)
        {
            check = ((check >> 8) & 0x00FFFFFF) ^ crcGlobals.crcTable[(check ^ win1252Bytes[j]) & 0xFF];
        }
        check ^= 0xFFFFFFFF;
        entry.crc = check;

        return entry;
    }
    public int CompareTo(Text_Entry other)
    {
        // Numeric sort by crc
        return this.crc.CompareTo(other.crc);
    }
}

class Key_Pair : IComparable<Key_Pair>
{
    public string identifier;
    public string entry;

    public int CompareTo(Key_Pair other)
    {
        // Alphabetize sort by id
        return this.identifier.CompareTo(other.identifier);
    }
}

public static class DatParser
{
    static byte[] tobytesLE(uint value)
    {
        byte[] corenne = new byte[4];
        corenne[0] = Convert.ToByte(value & 0xff);
        corenne[1] = Convert.ToByte((value & 0xff00) >> 8);
        corenne[2] = Convert.ToByte((value & 0xff0000) >> 16);
        corenne[3] = Convert.ToByte((value & 0xff000000) >> 24);

        return corenne;
    }

    public static int make16(byte[] source, int startindex)
    {
        int corenne;
        corenne = Convert.ToInt32(source[startindex]);
        corenne += Convert.ToInt32(source[startindex + 1]) << 8;

        return corenne;
    }

    public static int make32(byte[] source, int startindex)
    {
        int corenne;
        corenne = Convert.ToInt32(source[startindex]);
        corenne += Convert.ToInt32(source[startindex + 1]) << 8;
        corenne += Convert.ToInt32(source[startindex + 2]) << 16;
        corenne += Convert.ToInt32(source[startindex + 3]) << 24;

        return corenne;
    }

    static string readentry(byte[] source, int startindex, int length)
    {
        byte[] entrybytes = new byte[2 * length];
        for (int i = 0; i < 2 * length; i++)
        {
            entrybytes[i] = source[startindex + i];
        }

        return System.Text.Encoding.Unicode.GetString(entrybytes);
    }

    static string readid(byte[] source, int startindex, int length)
    {
        byte[] entrybytes = new byte[length];
        for (int i = 0; i < length; i++)
        {
            entrybytes[i] = source[startindex + i];
        }

        return System.Text.Encoding.GetEncoding("windows-1252").GetString(entrybytes);
    }

    public static List<IconData> ReadMTD(entities entities)
    {
        List<IconData> corenne = new List<IconData>();
        string source = "Art\\Textures\\MT_CommandBar.mtd";
        byte[] mtdfile;
        string filesource = SharedFunctions.getModFile("Art\\Textures\\MT_CommandBar.mtd");
        if(filesource != "") mtdfile = System.IO.File.ReadAllBytes(filesource);
        else mtdfile = SharedFunctions.getFileFromMegs(source, entities);

        int total_entries = make32(mtdfile, 0);
        for (int index = 4; index < total_entries*81+4; index+=5) //cover last 32 bit and 8 bit alpha boolean
        {
            IconData ico = new IconData();
            for(int i = index; i<index+64; i++)
            {
                if(mtdfile[i] == 0) break;
                ico.id += (char)mtdfile[i];
            }
            index += 64;
            ico.origin_x = make32(mtdfile, index);
            index += 4;
            ico.origin_y = make32(mtdfile, index);
            index += 4;
            ico.size_x = make32(mtdfile, index);
            index += 4;
            ico.size_y = make32(mtdfile, index);
            //index += 4;
            corenne.Add(ico);
        }

        return corenne;
    }

    public static IconData GetIconData(string id, entities entities)
    {
        foreach(IconData i in entities.IconData)
        {
            if (i.id.ToUpper() == id.ToUpper()) return i;
        }
        return new IconData();
    }
    public static List<Text_Entry> ReadDat(string source, char delimiter, int sorttype)
    {
        List<Text_Entry> entries = new List<Text_Entry>();
        if (!File.Exists(source))
        {
            Console.WriteLine("File " + source + " not found");
            return entries;
        }

        byte[] datfile = System.IO.File.ReadAllBytes(source);

        int total_entries = make32(datfile, 0);
        int index = 4;

        int[] textlength = new int[total_entries];
        int[] idlength = new int[total_entries];

        for (int i = 0; i < total_entries; i++)
        {
            Text_Entry nuevo = new Text_Entry();
            nuevo.crc = (uint)make32(datfile, index);
            entries.Add(nuevo);
            index += 4;
            textlength[i] = make32(datfile, index);
            index += 4;
            idlength[i] = make32(datfile, index);
            index += 4;
        }
        for (int i = 0; i < total_entries; i++)
        {
            entries[i].entry = readentry(datfile, index, textlength[i]);
            index += 2 * textlength[i];
        }
        for (int i = 0; i < total_entries; i++)
        {
            entries[i].identifier = readid(datfile, index, idlength[i]);
            index += idlength[i];
        }

        if (sorttype == 1)
        {
            for (int i = 0; i < total_entries; i++)
            {
                string temp = entries[i].identifier;
                entries[i].identifier = entries[i].entry;
                entries[i].entry = temp;
            }
        }

        if (sorttype != 2)
        {
            entries.Sort();
        }

        if (sorttype == 1)
        {
            for (int i = 0; i < total_entries; i++)
            {
                string temp = entries[i].identifier;
                entries[i].identifier = entries[i].entry;
                entries[i].entry = temp;
            }
        }

        return entries;
    }
}
