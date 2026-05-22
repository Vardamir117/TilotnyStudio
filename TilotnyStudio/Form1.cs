using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using static SharedFunctions;
using System.Globalization;
using System.Threading;

//todo *path for a unit's source file means it's in the megs. Might want to do something with that eventually

//todo used shared modfiles functions for mods, update to be able to handle local mod as well as workshop,
    //Handle a mod stack as a source instead of a single mod only
//consolidate reading functions better now that the conversion to which submod's files can be in the shared library
//TODO - generalize lua reading functions a lot
//tooltips everywhere
//Todo: Thorn's crash reports - test on not debug, give Proteus Comms BC. Game was probably running at the time
//.Save() should always be ConvertMainPathToMod, never with GetExtantPath and certainly never without a wrapper on the path
//TODO try to autodetect local mods. Try to pull mod from json? Else enable dropdown
//TODO enable cheats?
//https://github.com/bmk10/TGASharpLib-master-C--Convert-.tga-filetype-Targa-for-noise-3d-texture-game-2d-material-import
//https://github.com/alexgreenalex/tgasharplib

/* Sets affiliations on unit company and units in company, sets tech level to 0, sets build initially locked to no,
* determines corresponding required structures and sets according to them/user preference, handles filter prereqs,
* preserves corporation prereqs, handles influence, traces required structures back to templates, preserves Hapan House prereqs,
* adds to faction unit rosters (including crew costs), adds to factional bombardment lists, adds or removes from Custom Roster list, 
* recalculates affiliation of Corporation objects
* 
-# Known issues/limitations: The logic to guess ground factories is incomplete and will err on the side of heavy factory. Space is largely complete but may have edge cases

-# Required structures are done on templates when they exist. Attempting to make Munificent and subfaction Munificient use different structures will not work. Structures shared across factions (e.g. Zsinj and Hutt Pirate bases) cannot be enabled separately

-# Some missing data on units cannot be supplied (e.g. Supercruiser has no crew costs and will not cost crew when enabled)

-# The program makes no attempt to understand units affiliated with a faction but locked via script or lack of prereqs. May unlock mission rewards or units relying on build initally locked for early era locks, cannot enable units with scripted locks (e.g. Warlord stromtroopers). Corporations may become buildable on account of mission rewards that are not themselves buildable

-# Hapan House and corporation prereqs may not be edited
*/

namespace TilotnyStudio
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine("MyHandler caught : " + e.Message);
            Console.WriteLine("Runtime terminating: {0}", args.IsTerminating);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //tabPage2.Enabled = false;
            tabHidePanel.Size = new System.Drawing.Size(1000, tabHidePanel.Size.Height);

            string exePath = AppContext.BaseDirectory;
            if (File.Exists(UpOneFolder(UpOneFolder(exePath)) + "\\config.meg"))
            {
                globals.SourceMod = UpOneFolder(UpOneFolder(exePath));
                string uptwo = UpOneFolder(globals.SourceMod);
                globals.SourceModName = LastFolderOrFile(uptwo);
            }

            globals.ModFolder = UpOneFolder(UpOneFolder(UpOneFolder(UpOneFolder(UpOneFolder(UpOneFolder(globals.SourceMod)))))) + "\\steamapps\\common\\Star Wars Empire at War\\corruption\\Mods\\";
            globals.SourceMod += "\\";

            if (!Directory.Exists(globals.ModFolder))
            {
                Directory.CreateDirectory(globals.ModFolder);
            }
            var directories = Directory.GetDirectories(globals.ModFolder);
            foreach (string directory in directories)
            {
                if (File.Exists(directory + "\\Tilotny"))
                {
                    string modid = File.ReadAllLines(directory + "\\Tilotny")[0];
                    if (modid == LastFolderOrFile(UpOneFolder(UpOneFolder(globals.SourceMod))))
                    {
                        ModListBox.Items.Add(LastFolderOrFile(directory));
                    }
                }
            }            

            if (globals.SourceMod.Contains("1125571106"))
            {
                VersionComboBox.SelectedIndex = 0;
            }
            else if (globals.SourceMod.Contains("1976399102"))
            {
                VersionComboBox.SelectedIndex = 1;
            }
            else if (globals.SourceMod.Contains("3417277973"))
            {
                VersionComboBox.SelectedIndex = 2;
            }
        }

        public static entities entities = new entities();

        public static class globals
        {
            //1125571106 1976399102 3417277973 TR FotR Rev if it isn't really obvious
            public static string SourceMod = "C:\\Program Files (x86)\\Steam\\steamapps\\workshop\\content\\32470\\1976399102\\Data";
            public static string SourceModName = "1976399102";
            public static string ModFolder = "";
            public static string LocalMod = "";
            public static string LocalModName = "";
            public static bool unitsloaded = false;

            public static List<faction> factions = new List<faction>(); //Playable factions only
            public static List<string> allfactories = new List<string>();

            public static List<Text_Entry> Text = new List<Text_Entry>();

            public static string ContentLoaderPath = "";
            public static CultureInfo UIculture = Thread.CurrentThread.CurrentCulture;
        }

        public static class algorithm_data
        {
            public static int level2crew = 0;
            public static int level2shield = 0;
            public static int level3crew = 0;
            public static int level3shield = 0;
            public static int level4crew = 0;
            public static int level4shield = 0;
        }

        private string GetExtantPath(string MainPath)
        {
            string ModPath = ConvertMainPathToMod(MainPath, true);
            if (File.Exists(ModPath))
            {
                return ModPath;
            }
            return MainPath;
        }

        private string ConvertMainPathToMod(string MainPath, bool suppress_creation = false, bool keepdata = false)
        {
            int start = MainPath.IndexOf("\\Data\\") + 5; //5 being the length of Data\
            if (keepdata) start -= 5;
            string ModPath = globals.LocalMod + MainPath.Substring(start, MainPath.Length - start);
            //Make sure the folder exists
            if (!suppress_creation)
            {
                System.IO.Directory.CreateDirectory(UpOneFolder(ModPath));
            }
            return ModPath;
        }

        private List<string> getModFiles(string corepath, string extension)
        {
            List<string> corenne = new List<string>();
            List<string> prefound = new List<string>();
            string test = Path.Combine(globals.LocalMod, corepath);
            string[] tests = new string[0];
            try
            {
                tests = Directory.GetFiles(test, extension, SearchOption.AllDirectories);
            }
            catch { } //There's probably a cleaner way to do this
            foreach (string file in tests)
            {
                string truncated = file.Replace(globals.LocalMod + "\\", "");
                prefound.Add(truncated);
                corenne.Add(file);
            }
            test = Path.Combine(globals.SourceMod, corepath);
            try
            {
                tests = Directory.GetFiles(test, extension, SearchOption.AllDirectories);
            }
            catch { }
            foreach (string file in tests)
            {
                string truncated = file.Replace(globals.SourceMod, "");
                if (!prefound.Contains(truncated))
                {
                    prefound.Add(truncated);
                    corenne.Add(file);
                }
            }
            return corenne;
        }

        private string[] LoadText()
        {
            return File.ReadAllLines(globals.LocalMod+ "\\Text\\Submod_text.txt");
        }

        private string[] SetTextID(string[] lines, string ID, string value)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                string leading = lines[i].Substring(0, lines[i].IndexOf(","));
                if (leading == ID)
                {
                    lines[i] = ID + "," + value;
                    return lines;
                }
            }
            lines = lines.Append(ID + "," + value).ToArray();
            return lines;
        }
        private void SaveText(string[] lines)
        {
            File.WriteAllLines(globals.LocalMod + "\\Text\\Submod_text.txt", lines);
            //System.Diagnostics.Process.Start("cmd.exe", "/K cd \""+globals.LocalMod + "\\Text \" " + globals.LocalMod + "\\Text\\alphabetize-and-build.bat\"");
            string path = "\"" + globals.LocalMod + "\\Text\\datassembler.exe\"";
            string arg = "/b \"" + globals.LocalMod + "\\Text\\MasterTextFile_ENGLISH.txt\" -r:\"" + globals.LocalMod + "\\Text\\Submod_text.txt\"";
            System.Diagnostics.Process.Start("\"" + globals.LocalMod + "\\Text\\datassembler.exe\"", "/b \"" + globals.LocalMod + "\\Text\\MasterTextFile_ENGLISH.txt\" \"" + globals.LocalMod + "\\Text\\MasterTextFile_ENGLISH.dat\" -r:\"" + globals.LocalMod + "\\Text\\Submod_text.txt\"");
        }

        public struct faction
        {
            public string factionname;
            public string facingname;
            public string altshipyard; //Pirate base, ship market - if a unit has this as a prereq, do not use defaults
            public string level42shipyard; //Rancor Base, Star Forge Relay - a prereq unless alt is used
            public bool parallelshipyards; //Hapan shipyards - all shipyards on by default
            public List<string> offices; //Hapans ruining everything :(

            public List<string> factories;
            public List<string> shipyards;
            public List<string> specialfactories; //Droid factory, alchemical - only units with this as a prereq are placed here, else defer to:
            public List<string> specialfactorydefers; //Go here instead of special if deferred
            public int rank;
        }

        private void parseStructuresAndFactions(string[] files)
        {
            List<string> HapanOffices = new List<string>();
            foreach (string file in files)
            {
                XmlDocument doc = new XmlDocument();
                doc.PreserveWhitespace = true;
                doc.Load(GetExtantPath(file));
                XmlNode root = doc.DocumentElement;

                XmlNodeList structures = root.SelectNodes("descendant::SpecialStructure");

                foreach (XmlNode unit in structures)
                {
                    string name = unit.Attributes[0].Value;
                    XmlNode IHateHapes = unit.SelectSingleNode("descendant::Variant_Of_Existing_Type");
                    if (!(IHateHapes is null))
                    {
                        XmlNode SoMuchForJustThem = IHateHapes.LastChild;
                        if (!(SoMuchForJustThem is null))
                        {
                            if (SoMuchForJustThem.Value == "Hapan_Royal_Office") HapanOffices.Add(name);
                        }
                    }
                    XmlNode value = unit.SelectSingleNode("descendant::Affiliation");
                    if (!(value is null))
                    {
                        XmlNode aff = value.LastChild;
                        if (!(aff is null))
                        {
                            string[] split = fullTrim(aff.Value).Split(',');
                            foreach (string affil in split)
                            {
                                if (affil == "Independent_Forces" || affil == "Warlords") break; //Don't need to worry about these massive edge cases that can't build anyway
                                string facname = affil;
                                faction faction = new faction();
                                bool newf = true;
                                bool newinfo = false;
                                faction.factionname = facname;
                                faction.facingname = affil; //TODO get user facing name
                                faction.specialfactories = new List<string>();
                                faction.specialfactorydefers = new List<string>();
                                faction.parallelshipyards = false;
                                faction.level42shipyard = "";
                                faction.altshipyard = "";
                                foreach (faction fac in globals.factions)
                                {
                                    if (fac.factionname == facname)
                                    {
                                        faction = fac;
                                        newf = false;
                                    }
                                }
                                if (newf)
                                {
                                    faction.factories = new List<string>();
                                    faction.shipyards = new List<string>();
                                    faction.offices = new List<string>();
                                }
                                if (name.Contains("_Barracks") || name.Contains("_Vehicle_Factory"))
                                {
                                    faction.factories.Add(name);
                                    newinfo = true;
                                }
                                else if (name.Contains("_Shipyard"))
                                {
                                    faction.shipyards.Add(name);
                                    newinfo = true;
                                }
                                else if (name.Contains("_Office"))
                                {
                                    faction.offices.Add(name);
                                    newinfo = true;
                                }
                                if (newf && newinfo && facname != "WARLORDS") globals.factions.Add(faction);
                            }
                        }
                    }
                }
            }

            for (int i = globals.factions.Count-1; i >= 0; i--)
            {
                if (globals.factions[i].shipyards.Count == 0 || globals.factions[i].factories.Count == 0) globals.factions.RemoveAt(i);
            }
            if(HapanOffices.Count > 0)
            {
                foreach (faction faction in globals.factions)
                {
                    if (faction.factionname == "Hapes_Consortium")
                    {
                        foreach (string office in HapanOffices)
                        {
                            faction.offices.Add(office);
                        }
                        break;
                    }
                }
            }

            //Sort faction to match xml list, though do not add in nonplayables
            XmlDocument facs = new XmlDocument();
            facs.PreserveWhitespace = true;
            facs.Load(GetExtantPath(globals.SourceMod + "XML\\Factions.xml"));
            XmlNode facroot = facs.DocumentElement;

            var factions = facroot.SelectNodes("descendant::Faction");
            int rank = 0;

            FactionFilerListBox.Items.Clear();
            foreach (XmlElement faction in factions)
            {
                rank += 1;
                string facname = faction.GetAttribute("Name");
                for(int i=0;i< globals.factions.Count; i++)
                {
                    faction facstruct = globals.factions[i];
                    if (facname == facstruct.factionname)
                    {
                        facstruct.rank = rank;
                        globals.factions[i] = facstruct;
                        break;
                    }
                }
                FactionFilerListBox.Items.Add(facname);
            }
            globals.factions.Sort((s1, s2) => s1.rank.CompareTo(s2.rank));

            SetAffilMods();

            while (UnitAffilPanel.Controls.Count > 0)
            {
                foreach (Control tokill in UnitAffilPanel.Controls)
                {
                    UnitAffilPanel.Controls.Remove(tokill);
                }
            }

            for (int i = 0; i < globals.factions.Count; i++)
            {
                int basey = 120 * i;
                faction faction = globals.factions[i];
                string name = faction.factionname;

                var check = new CheckBox();
                check.Location = new Point(10, basey);
                check.Text = faction.facingname;
                check.Tag = name;
                check.Width = 220;
                UnitAffilPanel.Controls.Add(check);

                var list = new ListBox();
                list.Location = new Point(250, basey);
                list.Text = faction.facingname;
                list.Tag = name;
                list.Width = 250;
                list.SelectionMode = SelectionMode.MultiExtended;
                foreach (string shipyard in faction.shipyards)
                {
                    list.Items.Add(shipyard);
                }
                UnitAffilPanel.Controls.Add(list);
            }
        }

        private void parsePrereqs(List<unit> unitset, bool space)
        {
            for (int i = 0; i < unitset.Count; i++)
            {
                unit unit = unitset[i];
                /*if (unit.unitname == "Galney_House_Guard_Company") //The fastest way to examine a specific unit in debug
                {
                    bool visible = false;
                }*/
                unit.UsedStrucutures = new List<string>();
                unit.FullStrucutures = new List<string>();
                string[] structures = fullTrim(unit.reqstructures.Replace(",", "|")).Split('|');
                bool firstcorp = true;
                unit.level = 0;
                unit.influence = -1;
                unit.corporations = "";
                unit.office = "";
                foreach (string structure in structures)
                {
                    if (globals.allfactories.Contains(structure))
                    {
                        unit.UsedStrucutures.Add(structure);
                    }
                    if (space)
                    {
                        if (structure.Contains("Category_Dummy") && !structure.Contains("AI_Category_Dummy")) unit.corporations = structure;
                    }
                    else
                    {
                        if (structure.Contains("_HQ") || structure.Contains("_Capital") || structure == "Jedi_Ground_Barracks" || structure == "Dark_Empire_Cloning_Facility")
                        {
                            if (firstcorp)
                            {
                                firstcorp = false;
                                unit.corporations = "";
                            }
                            else unit.corporations += " | ";
                            unit.corporations += structure;
                        }
                        if (structure.Contains("_Office") && unit.office == "") unit.office = structure;
                    }
                    /* if (structure.Contains("INFLUENCE_")) //Moved to shared untemplate
                    {
                        int temp = 0;
                        if (structure.Contains("ONE")) temp = 1;
                        else if (structure.Contains("TWO")) temp = 2;
                        else if (structure.Contains("THREE")) temp = 3;
                        else if (structure.Contains("FOUR")) temp = 4;
                        else if (structure.Contains("FIVE")) temp = 5;
                        else if (structure.Contains("SIX")) temp = 6;
                        else if (structure.Contains("SEVEN")) temp = 7;
                        else if (structure.Contains("EIGHT")) temp = 8;
                        else if (structure.Contains("NINE")) temp = 9;
                        else if (structure.Contains("TEN")) temp = 10;
                        if (unit.influence <= 0 || unit.influence > temp) unit.influence = temp;
                    } */
                }

                foreach (string structure in unit.UsedStrucutures)
                {
                    if (space)
                    {
                        int temp = 0;
                        if (structure.Contains("One")) temp = 1;
                        else if (structure.Contains("Two")) temp = 2;
                        else if (structure.Contains("Three")) temp = 3;
                        else if (structure.Contains("Four")) temp = 4;
                        if (unit.level <= 0 || unit.level > temp) unit.level = temp;
                    }
                    else
                    {
                        if (structure.Contains("Barracks")) unit.level = 1;
                        else if (structure.Contains("Advanced")) unit.level = 4;
                    }
                }
                //Nothing to glean from required structues, must make best guess based on whatever pattern matching I made up
                if(unit.level == 0)
                {
                    if (space)
                    {//Space gets to cheat hard whenever the unit is buildable
                        /* if (unit.corporations == "Non_Capital_Category_Dummy") unit.level = 1; //moved to shared untemplate
                        else if (unit.corporations == "Heavy_Frigate_Category_Dummy") unit.level = 2;
                        else if (unit.corporations == "Capital_Category_Dummy") unit.level = 3;
                        else if (unit.corporations == "Dreadnought_Category_Dummy") unit.level = 4; */

                        if (unit.level == 0)
                        {
                            if (unit.categories.Contains("Corvette"))
                            {
                                unit.level = 1;
                            }
                            else if (unit.categories.Contains("SuperCapital") ||
                                (algorithm_data.level4crew >= 0 && unit.crew >= algorithm_data.level4crew) || (algorithm_data.level4shield >= 0 && unit.shield >= algorithm_data.level4shield))
                            {
                                unit.level = 4;
                            }
                            else if (unit.categories.Contains("Capital") ||
                                (algorithm_data.level3crew >= 0 && unit.crew >= algorithm_data.level3crew) || (algorithm_data.level3shield >= 0 && unit.shield >= algorithm_data.level3shield))
                            {
                                unit.level = 3;
                            }
                            else if ((algorithm_data.level2crew >= 0 && unit.crew >= algorithm_data.level2crew) || (algorithm_data.level2shield >= 0 && unit.shield >= algorithm_data.level2shield))
                            {
                                unit.level = 2;
                            }
                            else
                            {
                                unit.level = 1;
                            }
                        }
                    }
                    else
                    {
                        /*TODO al g rhythm
                         * Need to fix artillery and gunships being 1 per but usually heavy. Usually...

                        Ground algorithm - 2 per company usually = heavy. BUT
                        A-A5 is wild outlier and is light, somehow. idk. May just have to write it off 72 cp 325 cred/pop 400 hp?!?!?!?
                        Rana us light 74 cp 350 cred 200hp
                        Hailfire is light 71 cp 650 cred/pop?!?!?!? 150 hp. But CIS is piloted/droid split and this can actually be assumed as hvy
                        PX-4 is advanced, but falls under >400 hp
                        AT-AA can be under 70 cp 400 hp, 350 per pop
                        Octuptarra is adv idk 122cp 500 cred/pop
	                        Swift is 127 cp, see also hailfire credit... >=500 cred, and 120+ cp?
                        Lancet is adv and 500 cred, 57 cp...
                        AT-OT is adv 94cp, 325 cr
                        1H is hvy 132 cp 555 cr
                         */
                        if (unit.percompany == 1 && unit.companyunits[0].Contains("_Company_Dummy"))
                        {
                            unit.level = 1;
                            /*else //This is pretty much covered by hp where it does count and there are a lot of 
                            {
                                unit.level = 4;
                            }*/
                        }
                        else if (unit.hp > 400)
                        {
                            unit.level = 4;
                        }
                        else if (unit.percompany >= 4)
                        {
                            unit.level = 2;
                        }
                        else
                        {
                            unit.level = 3;
                        }
                    }
               }

                foreach (faction faction in globals.factions)
                {
                    List<string> facstructs = faction.factories;
                    if (space)
                    {
                        facstructs = faction.shipyards;
                    }
                    bool fillstructs = true;
                    foreach (string facstruct in facstructs)
                    {
                        if (unit.UsedStrucutures.Contains(facstruct))
                        {
                            fillstructs = false;
                            if (!unit.FullStrucutures.Contains(facstruct))
                            {
                                unit.FullStrucutures.Add(facstruct);
                            }
                        }
                    }
                    if (fillstructs)
                    {
                        if (space)
                        {
                            if (faction.parallelshipyards)
                            {
                                for (int j = 0; j < facstructs.Count; j++)
                                {
                                    if (!unit.FullStrucutures.Contains(facstructs[j]))
                                    {
                                        unit.FullStrucutures.Add(facstructs[j]);
                                    }
                                }
                            }
                            else
                            {
                                for (int j = unit.level - 1; j < 4; j++)
                                {
                                    if (!unit.FullStrucutures.Contains(facstructs[j]))
                                    {
                                        unit.FullStrucutures.Add(facstructs[j]);
                                    }
                                }
                                if (faction.level42shipyard != "")
                                {
                                    if (!unit.FullStrucutures.Contains(faction.level42shipyard))
                                    {
                                        unit.FullStrucutures.Add(faction.level42shipyard);
                                    }
                                }
                            }
                        }
                        else
                        {
                            int scopedlevel = unit.level;
                            while (scopedlevel > facstructs.Count)
                            {
                                scopedlevel--;
                            }
                            string target = facstructs[scopedlevel-1];
                            for (int j = 0; j < faction.specialfactories.Count; j++)
                            {
                                if (target == faction.specialfactories[j])
                                {
                                    target = faction.specialfactorydefers[j];
                                    break;
                                }
                            }

                            if (!unit.FullStrucutures.Contains(target))
                            {
                                unit.FullStrucutures.Add(target);
                            }
                        }
                    }
                }

                unitset[i] = unit;
            }
        }
        private void loadAffilData()
        {
            if (globals.unitsloaded)
            {
                AffilListBox.SelectedItems.Clear();
                return;
            }
            else globals.unitsloaded = true;
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
            globals.factions = new List<faction>();
            string[] files = Directory.GetFiles(globals.SourceMod + "XML\\Structures", "*.xml", SearchOption.AllDirectories);
            parseStructuresAndFactions(files);
            CorpLabel.Text = "";

            List<string> listfiles = getModFiles("XML\\Projectiles", "*.xml");
            parseProjectiles(entities);
            listfiles = getModFiles("XML\\Hardpoints", "*.xml");
            parseHardpoints(entities, globals.Text); //todo add Tilotny new hp file

            entities.spaceUnits = new List<unit>();
            entities.groundCompanies = new List<unit>();
            entities.groundUnits = new List<unit>();

            listfiles = getModFiles("XML\\Units", "*.xml");
            /*
            parseUnitFolder(listfiles, entities);
            parseGameConstants(GetExtantPath(globals.SourceMod + "XML\\GameConstants.xml"), entities);
            entities.groundCompanies.Sort((s1, s2) => s1.unitname.CompareTo(s2.unitname));
            entities.spaceUnits.Sort((s1, s2) => s1.unitname.CompareTo(s2.unitname));
            entities.groundUnits.Sort((s1, s2) => s1.unitname.CompareTo(s2.unitname));
            untemplate(entities.groundCompanies, entities.groundCompanies,globals.Text);
            untemplate(entities.spaceUnits, entities.spaceUnits, globals.Text);
            untemplate(entities.groundUnits, entities.groundUnits, globals.Text);
            entities.groundCompanies = unitToCompanyData(entities.groundCompanies, entities.groundUnits, entities.containers);*/
            parseObjects(entities);
            untemplate(entities);
            categorizeObjects(entities);
            entities.groundCompanies = unitToCompanyData(entities.groundCompanies, entities.groundUnits, entities.containers);
            parsePrereqs(entities.groundCompanies,false);
            parsePrereqs(entities.spaceUnits, true);

            Thread.CurrentThread.CurrentCulture = globals.UIculture;
            populateAffilUnits();
        }

        private void populateAffilUnits()
        {
            AffilListBox.Items.Clear();
            string search = AffilSearchTextBox.Text;
            List<unit> units;

            if (SpaceRadioButton.Checked) units = entities.spaceUnits;
            else if (UnitRadioButton.Checked) units = entities.groundUnits;
            else units = entities.groundCompanies;

            foreach (unit unit in units)
            {//Todo this can probably be cleaned up. Also you can't see gunships right now
                if (!unit.unitname.Contains("Template_") && !unit.unitname.Contains("Skirmish") && !unit.unitname.Contains("IA_") && !unit.unitname.Contains("Squadron") && !unit.unitname.Contains("Era_") && (UnitRadioButton.Checked && !unit.unitname.Contains("_Dummy") || (unit.cost > 1 && unit.pop > 0 && unit.fightermode <= 0 && !unit.shield_type.Contains("ShieldS_Gunship"))))
                {
                    bool affil_ok = true;
                    if (FactionFilerListBox.SelectedItems.Count > 0)
                    {
                        affil_ok = false;
                        foreach (string filter in FactionFilerListBox.SelectedItems)
                        {
                            if (unit.affiliations.Contains(filter))
                            {
                                affil_ok = true;
                                break;
                            }
                        }
                    }
                    if (affil_ok && (search == "" || (unit.unitname).ToLower().Contains(search.ToLower())))
                    {
                        AffilListBox.Items.Add(unit.unitname);
                    }
                }
            }
        }

        private void SetStatVisibility(int mode)
        {
            bool buildable = mode <= 1;
            bool tacticalunit = mode != 1;
            bool groundstuff = mode >= 1;
            bool spaceonly = mode == 0;
            bool companyonly = mode == 1;
            bool groundunitonly = mode == 2;

            PopLabel.Visible = buildable;
            PopBox.Visible = buildable;
            CostLabel.Visible = buildable;
            CostBox.Visible = buildable;
            BuildTimeLabel.Visible = buildable;
            BuildTimeBox.Visible = buildable;
            GUIRowLabel.Visible = buildable;
            GUIRowComboBox.Visible = buildable;

            hpLabel.Visible = tacticalunit;
            hpBox.Visible = tacticalunit;
            hpFinePrintLabel.Visible = tacticalunit;
            ATypeLabel.Visible = tacticalunit;
            ATypeComboBox.Visible = tacticalunit;
            ShieldLabel.Visible = tacticalunit;
            ShieldBox.Visible = tacticalunit;
            STypeLabel.Visible = tacticalunit;
            STypeComboBox.Visible = tacticalunit;
            RegenLabel.Visible = tacticalunit;
            RegenBox.Visible = tacticalunit;
            SpeedLabel.Visible = tacticalunit;
            SpeedBox.Visible = tacticalunit;
            AccelLabel.Visible = spaceonly;
            AccelBox.Visible = spaceonly;
            TurnLabel.Visible = tacticalunit;
            TurnBox.Visible = tacticalunit;

            ConcurrentLabel.Visible = spaceonly;//buildable; TODO Ground companies need different Lua based handling eventually
            ConcurrentBox.Visible = spaceonly;//buildable;
            LifetimeLabel.Visible = spaceonly;
            LifetimeBox.Visible = spaceonly;
        }

        private void SpaceRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            populateAffilUnits();
            foreach (ListBox control in UnitAffilPanel.Controls.OfType<ListBox>())
            {
                control.Items.Clear();
                faction faction = globals.factions[0]; //placeholder so the compiler doesn't complain. The next loop will match
                foreach (faction fac in globals.factions)
                {
                    if (fac.factionname == control.Tag.ToString())
                    {
                        faction = fac;
                        break;
                    }
                }
                foreach (string shipyard in faction.shipyards)
                {
                    control.Items.Add(shipyard);
                }
            }
            ATypeComboBox.Items.Clear();
            foreach (string type in entities.SpaceArmors) ATypeComboBox.Items.Add(type);
            STypeComboBox.Items.Clear();
            foreach (string type in entities.SpaceShields) STypeComboBox.Items.Add(type);
            FilterComboBox.Visible = true;
            FilterLabel.Visible = true;
            SetStatVisibility(0);
        }

        private void GroundRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            populateAffilUnits();
            foreach (ListBox control in UnitAffilPanel.Controls.OfType<ListBox>())
            {
                control.Items.Clear();
                faction faction = globals.factions[0]; //placeholder so the compiler doesn't complain. The next loop will match
                foreach (faction fac in globals.factions)
                {
                    if (fac.factionname == control.Tag.ToString())
                    {
                        faction = fac;
                        break;
                    }
                }
                foreach (string factory in faction.factories)
                {
                    control.Items.Add(factory);
                }
            }
            FilterComboBox.Visible = false;
            FilterLabel.Visible = false;

            SetStatVisibility(1);
        }

        private void UnitRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            populateAffilUnits();
            ATypeComboBox.Items.Clear();
            foreach (string type in entities.GroundArmors) ATypeComboBox.Items.Add(type);
            STypeComboBox.Items.Clear();
            foreach (string type in entities.GroundShields) STypeComboBox.Items.Add(type);

            SetStatVisibility(2);
        }

        private void AffilSearchTextBox_TextChanged(object sender, EventArgs e)
        {
            populateAffilUnits();
        }

        private void FactionFilerListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            populateAffilUnits();
        }

        private void FactionFilterClearButton_Click(object sender, EventArgs e)
        {
            FactionFilerListBox.SelectedItems.Clear();
        }

        private void InfluenceCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (InfluenceCheckBox.Checked)
            {
                foreach (CheckBox control in UnitAffilPanel.Controls.OfType<CheckBox>()) control.Checked = true;
                if (GroundRadioButton.Checked)
                {
                    foreach (ListBox control in UnitAffilPanel.Controls.OfType<ListBox>()) control.SelectedItems.Clear();
                }
            }
        }

        private void AffilListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool Multi = false;
            if (AffilListBox.SelectedItems.Count > 1)
            {
                Multi = true;
            }
            else if (AffilListBox.SelectedItems.Count == 1)
            {
                List<unit> units = entities.spaceUnits;
                if (GroundRadioButton.Checked)
                {
                    units = entities.groundCompanies;
                }
                else if (UnitRadioButton.Checked)
                {
                    units = entities.groundUnits;
                }
                foreach (unit unit in units)
                {
                    if (unit.unitname == AffilListBox.SelectedItem.ToString())
                    {
                        if (!UnitRadioButton.Checked)
                        {
                            if (SpaceRadioButton.Checked) CorpLabel.Text = "Filter: ";
                            else CorpLabel.Text = "Corporations: ";
                            CorpLabel.Text += unit.corporations;
                            PlanetTextBox.Text = SerializeStringArray(unit.planets);
                            if (unit.office != "") CorpLabel.Text += " " + unit.office;
                            foreach (CheckBox control in UnitAffilPanel.Controls.OfType<CheckBox>())
                            {
                                control.Checked = false;
                                foreach (string affil in unit.affiliations)
                                {
                                    if (control.Tag.ToString() == affil)
                                    {
                                        control.Checked = true;
                                    }
                                }
                            }
                            FilterComboBox.SelectedIndex = unit.level - 1;
                            foreach (ListBox control in UnitAffilPanel.Controls.OfType<ListBox>())
                            {
                                control.SelectedIndex = -1;
                                List<string> notself = new List<string>();
                                foreach (string item in control.Items)
                                {
                                    if (unit.UsedStrucutures.Contains(item))
                                    {
                                        notself.Add(item);
                                    }
                                    if (unit.FullStrucutures.Contains(item))
                                    {
                                        notself.Add(item);
                                    }
                                }
                                foreach (string item in notself)
                                {
                                    control.SelectedItems.Add(item);
                                }
                            }
                            if (unit.influence > 0)
                            {
                                InfluenceNumericUpDown.Value = unit.influence;
                                InfluenceCheckBox.Checked = true;
                            }
                            else
                            {
                                InfluenceNumericUpDown.Value = 1;
                                InfluenceCheckBox.Checked = false;
                            }
                        }
                        if (PopBox.Visible) PopBox.Value = unit.pop;
                        if (CostBox.Visible) CostBox.Value = unit.cost;
                        if (BuildTimeBox.Visible) BuildTimeBox.Value = unit.buildtime;
                        if (CrewBox.Visible) CrewBox.Value = unit.crew;
                        if (GUIRowComboBox.Visible)
                        {
                            if (unit.gui_row == 0) GUIRowComboBox.SelectedIndex = 1;
                            if (unit.gui_row == 1) GUIRowComboBox.SelectedIndex = 0;
                        }

                        if (unit.hp > 0)
                        {
                            if (hpBox.Visible) hpBox.Value = unit.hp;
                            if (unit.targetablehps) hpBox.Enabled = false;
                            else hpBox.Enabled = true;
                        }
                        else
                        {
                            hpBox.Value = 0;
                            hpBox.Enabled = false;
                        }
                        if (unit.armor_type != null && ATypeComboBox.Items.Contains(unit.armor_type))
                        {
                            if (ATypeComboBox.Visible) ATypeComboBox.SelectedIndex = ATypeComboBox.FindStringExact(unit.armor_type);
                            ATypeComboBox.Enabled = true;
                        }
                        else
                        {
                            ATypeComboBox.SelectedIndex = -1;
                            ATypeComboBox.Enabled = false;
                        }
                        if (unit.shield_type != null && STypeComboBox.Items.Contains(unit.shield_type))
                        {
                            if (STypeComboBox.Visible) STypeComboBox.SelectedIndex = STypeComboBox.FindStringExact(unit.shield_type);
                            STypeComboBox.Enabled = true;
                        }
                        else
                        {
                            STypeComboBox.SelectedIndex = -1;
                            STypeComboBox.Enabled = false;
                        }
                        if (unit.shield > 0)
                        {
                            if (ShieldBox.Visible) ShieldBox.Value = unit.shield;
                            ShieldBox.Enabled = true;
                        }
                        else
                        {
                            ShieldBox.Value = 0;
                            ShieldBox.Enabled = false;
                        }
                        if (unit.regen > 0)
                        {
                            if (RegenBox.Visible) RegenBox.Value = (Decimal)unit.regen;
                            RegenBox.Enabled = true;
                        }
                        else
                        {
                            RegenBox.Value = 0;
                            RegenBox.Enabled = false;
                        }
                        if (unit.speed > 0)
                        {
                            if (SpeedBox.Visible) SpeedBox.Value = (Decimal)unit.speed;
                            SpeedBox.Enabled = true;
                        }
                        else
                        {
                            SpeedBox.Value = 0;
                            SpeedBox.Enabled = false;
                        }
                        if (unit.min_speed > 0)
                        {
                            if (MinSpeedBox.Visible) MinSpeedBox.Value = (Decimal)unit.min_speed;
                            MinSpeedBox.Enabled = true;
                        }
                        else
                        {
                            MinSpeedBox.Value = 0;
                            MinSpeedBox.Enabled = false;
                        }
                        if (unit.turn > 0)
                        {
                            if (TurnBox.Visible) TurnBox.Value = (Decimal)unit.turn;
                            TurnBox.Enabled = true;
                        }
                        else
                        {
                            TurnBox.Value = 0;
                            TurnBox.Enabled = false;
                        }
                        TurnBox.Enabled = unit.elementName != "GroundInfantry";
                        if (unit.accel > 0)
                        {
                            if (AccelBox.Visible) AccelBox.Value = (Decimal)unit.accel;
                            AccelBox.Enabled = true;
                        }
                        else
                        {
                            AccelBox.Value = 0;
                            AccelBox.Enabled = false;
                        }
                        if (unit.limit_concurrent > 0)
                        {
                            if (ConcurrentBox.Visible) ConcurrentBox.Value = (Decimal)unit.limit_concurrent;
                            ConcurrentBox.Enabled = true;
                        }
                        else
                        {
                            ConcurrentBox.Value = 0;
                            ConcurrentBox.Enabled = false;
                        }
                        if (unit.limit_lifetime > 0)
                        {
                            if (LifetimeBox.Visible) LifetimeBox.Value = (Decimal)unit.limit_lifetime;
                            LifetimeBox.Enabled = true;
                        }
                        else
                        {
                            LifetimeBox.Value = 0;
                            LifetimeBox.Enabled = false;
                        }
                    }
                }
            }

            foreach (ListBox control in UnitAffilPanel.Controls.OfType<ListBox>())
            {
                control.Visible = !Multi;
                ReqStructLabel.Visible = !Multi;
                ReqStructWarning.Visible = Multi;
                CorpLabel.Visible = !Multi;
                InfluenceCheckBox.Visible = !Multi;
                InfluenceLabel.Visible = !Multi;
                InfluenceNumericUpDown.Visible = !Multi;
                PlanetTextBox.Visible = !Multi;
                PlanetLabel.Visible = !Multi;
                if (SpaceRadioButton.Checked) {
                    FilterComboBox.Visible = !Multi;
                    FilterLabel.Visible = !Multi;
                }
            }
        }

        private void AffilAllButton_Click(object sender, EventArgs e)
        {
            foreach (CheckBox control in UnitAffilPanel.Controls.OfType<CheckBox>()) control.Checked = true;
        }

        private void AffilNoneButton_Click(object sender, EventArgs e)
        {
            foreach (CheckBox control in UnitAffilPanel.Controls.OfType<CheckBox>()) control.Checked = false;
        }

        private void AffilClearButton_Click(object sender, EventArgs e)
        {
            AffilListBox.SelectedItems.Clear();
        }

        private void WriteXMLTag(string tagname, string value, XmlDocument doc, XmlNode XMLunit)
        {
            XmlNode tag = XMLunit.SelectSingleNode("descendant::" + tagname);
            if (tag != null) tag.InnerText = value;
            else
            {
                XmlNode elem = doc.CreateElement(tagname);
                elem.InnerText = value;
                XMLunit.AppendChild(elem);
            }
        }

        private void SaveUnitButton_Click(object sender, EventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
            if (UnitTabControl.SelectedIndex == 0)
            {
                string affils = "";
                List<string> affilist = new List<string>();
                bool first = true;

                string[] CustomLib = File.ReadAllLines(GetExtantPath(globals.SourceMod + "Scripts\\Library\\CustomLibrary.lua"));
                List<int> CustomStartIndexes = new List<int>();
                List<int> CustomEndIndexes = new List<int>();

                foreach (CheckBox control in UnitAffilPanel.Controls.OfType<CheckBox>())
                {
                    if (control.Checked)
                    {
                        if (!first)
                        {
                            affils += ", ";
                        }
                        string faction = (String)control.Tag;
                        affils += faction;
                        affilist.Add(faction);
                        first = false;
                    }
                }
                foreach (faction fac in globals.factions)
                {
                    string faction = fac.factionname;
                    int foundfaction = 0;
                    string faction2 = "[\"" + faction + "\"]";
                    string faction3 = faction.ToUpper(); //Capitalization changed between FotR 1.5 and TR 3.5
                    for (int index = 0; index < CustomLib.Length; index++)
                    {
                        if (CustomLib[index].Contains(faction2) || CustomLib[index].Contains(faction3)) foundfaction = 1;
                        if (foundfaction == 1 && CustomLib[index].Contains("RosterUnits"))
                        {
                            foundfaction = 2;
                            CustomStartIndexes.Add(index); //new objects will be added to the top line
                        }
                        if (foundfaction == 2 && CustomLib[index].Contains("}"))
                        {
                            CustomEndIndexes.Add(index);
                            break;
                        }
                    }
                }

                List<unit> unitlist = entities.spaceUnits;
                XmlDocument factiondoc = new XmlDocument();
                factiondoc.PreserveWhitespace = true;
                factiondoc.Load(GetExtantPath(globals.SourceMod + "XML\\Factions.xml"));
                XmlNode facroot = factiondoc.DocumentElement;
                var docfactions = facroot.SelectNodes("descendant::Faction");
                if (GroundRadioButton.Checked)
                {
                    unitlist = entities.groundCompanies;
                }
                List<string> TemplateList = new List<string>(); //Save names of units used as templates so latter modifications in the loop will not overwrite
                foreach (string unitname in AffilListBox.SelectedItems)
                {
                    string structurefield = "";
                    List<string> structurelist = new List<string>();
                    bool firststruct = true;
                    string structureSuffix = "";
                    string templateUnit = "";
                    string templatePath = "";

                    for (int i = 0; i < unitlist.Count; i++)
                    {
                        if (unitlist[i].unitname == unitname)
                        {
                            unit unit = unitlist[i];
                            foreach (XmlElement faction in docfactions)
                            {
                                string facname = faction.GetAttribute("Name");
                                if (SpaceRadioButton.Checked && affilist.Contains(facname) && unit.tooltip.Contains("TEXT_TOOLTIP_CAPABILITY_ORBITAL_BOMBARDMENT"))
                                {
                                    XmlNode node = faction.SelectSingleNode("descendant::Bombardment_Required_Orbital_Ships");
                                    string bombard = node.InnerText.Trim();
                                    string[] bombards = ReadXMLCSString(bombard);
                                    if (!bombards.Contains(unitname))
                                    {
                                        bombard = unitname + ",\r\n\t\t\t" + bombard;
                                        node.InnerText = bombard;
                                    }
                                }
                            }
                            for (int index = 0; index < globals.factions.Count; index++)
                            {
                                string facname = (string)globals.factions[index].factionname;
                                if (affilist.Contains(facname) && !unit.affiliations.Contains(facname))
                                {
                                    //Adding new
                                    CustomLib[CustomStartIndexes[index]] += "\"" + unitname + "\", ";
                                }
                                if (!affilist.Contains(facname) && unit.affiliations.Contains(facname))
                                {
                                    //Remove existing
                                    string tokill = "\"" + unitname + "\"";
                                    for (int j = CustomStartIndexes[index]; j < CustomEndIndexes[index]; j++)
                                    {
                                        CustomLib[j] = CustomLib[j].Replace(tokill + ",", "");
                                        CustomLib[j] = CustomLib[j].Replace(tokill, "");
                                    }
                                }
                            }

                            if (TemplateList.Contains(unitname))
                            {
                                foreach (string reqstru in unit.UsedStrucutures)
                                {
                                    structurelist.Add(reqstru);
                                    if (firststruct) firststruct = false;
                                    else structurefield += " | ";
                                    structurefield += reqstru;
                                }
                            }
                            if (AffilListBox.SelectedItems.Count > 1)
                            {
                                foreach (faction faction in globals.factions)
                                {
                                    if (affilist.Contains(faction.factionname))
                                    {
                                        List<string> facstructs = faction.factories;
                                        if (SpaceRadioButton.Checked)
                                        {
                                            facstructs = faction.shipyards;
                                        }
                                        foreach (string structure in facstructs)
                                        {
                                            if (unit.FullStrucutures.Contains(structure) && !structurelist.Contains(structure))
                                            {
                                                structurelist.Add(structure);
                                                if (firststruct) firststruct = false;
                                                else structurefield += " | ";
                                                structurefield += structure;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                unit.UsedStrucutures.Clear();
                                unit.FullStrucutures.Clear();
                                foreach (ListBox control in UnitAffilPanel.Controls.OfType<ListBox>())
                                {
                                    foreach (string structure in control.SelectedItems)
                                    {
                                        if (affilist.Contains(control.Tag))
                                        {
                                            if (!structurelist.Contains(structure))
                                            {
                                                structurelist.Add(structure);
                                                if (firststruct) firststruct = false;
                                                else structurefield += " | ";
                                                structurefield += structure;
                                                unit.UsedStrucutures.Add(structure);
                                            }
                                        }
                                        unit.FullStrucutures.Add(structure);
                                    }
                                }
                                if (InfluenceCheckBox.Checked)
                                {
                                    string[] dummies = { "ONE", "TWO", "THREE", "FOUR", "FIVE", "SIX", "SEVEN", "EIGHT", "NINE", "TEN" };
                                    string influencers = "";
                                    int mininf = (int)InfluenceNumericUpDown.Value - 1;
                                    for (int j = mininf; j < 10; j++)
                                    {
                                        if (j != mininf) influencers += " | ";
                                        influencers += "INFLUENCE_" + dummies[j];
                                    }
                                    if (influencers != "")
                                    {
                                        if (firststruct) firststruct = false;
                                        else structureSuffix += " |\r\n";
                                        structureSuffix += influencers;
                                    }
                                }
                            }

                            if (SpaceRadioButton.Checked)
                            {
                                if (firststruct) firststruct = false;
                                else structureSuffix += ",\r\n";
                                structureSuffix += "AI_Category_Dummy | ";
                                if (FilterComboBox.Visible && unit.level != FilterComboBox.SelectedIndex + 1)
                                {
                                    unit.corporations = "";
                                    unit.level = FilterComboBox.SelectedIndex + 1;
                                }

                                if (unit.corporations == "")
                                {
                                    if (unit.level == 1) unit.corporations = "Non_Capital_Category_Dummy";
                                    else if (unit.level == 2) unit.corporations = "Heavy_Frigate_Category_Dummy";
                                    else if (unit.level == 3) unit.corporations = "Capital_Category_Dummy";
                                    else if (unit.level == 4) unit.corporations = "Dreadnought_Category_Dummy";
                                }
                                structureSuffix += unit.corporations;

                                bool has_crew = false;
                                string checkval = "[\"" + unit.unitname.ToUpper() + "\"]";
                                string[] rosterset = File.ReadAllLines(GetExtantPath(globals.SourceMod + "Scripts\\Library\\roster-sets\\INFLUENCE.lua"));
                                foreach (string line in rosterset)
                                {
                                    if (line.Contains(checkval))
                                    {
                                        has_crew = true;
                                        break;
                                    }
                                }
                                if (!has_crew)
                                {
                                    foreach (string faction in affilist)
                                    {
                                        has_crew = false;
                                        string rosterpath = globals.SourceMod + "Scripts\\Library\\roster-sets\\" + faction.ToUpper() + ".lua";
                                        if (File.Exists(rosterpath)) //Actives but nonplayable do not have or need this
                                        {
                                            rosterset = File.ReadAllLines(GetExtantPath(rosterpath));
                                            foreach (string line in rosterset)
                                            {
                                                if (line.Contains(checkval))
                                                {
                                                    has_crew = true;
                                                    break;
                                                }
                                            }
                                            if (!has_crew)
                                            {
                                                if (!rosterset[rosterset.Length - 2].Contains(",")) rosterset[rosterset.Length - 2] += ",";
                                                rosterset[rosterset.Length - 1] = "    " + checkval + " = " + unit.crew.ToString() + ",";
                                                rosterset = rosterset.Append("}").ToArray();
                                                File.WriteAllLines(ConvertMainPathToMod(rosterpath), rosterset);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (unit.corporations != "")
                                {
                                    if (firststruct) firststruct = false;
                                    else structureSuffix += " |\r\n";
                                    structureSuffix += unit.corporations;
                                }
                                if (unit.office != "")
                                {
                                    if (firststruct) firststruct = false;
                                    else structureSuffix += ",\r\n";
                                    structureSuffix += unit.office;

                                    foreach (faction faction in globals.factions)
                                    {
                                        if (affilist.Contains(faction.factionname))
                                        {
                                            if (!faction.offices.Contains(unit.office) && !(structureSuffix.Contains(faction.offices[0])))
                                            {
                                                structureSuffix += " | " + faction.offices[0];
                                            }
                                        }
                                    }
                                }
                            }


                            XmlDocument doc = new XmlDocument();
                            doc.PreserveWhitespace = true;
                            string modfilepath = GetExtantPath(unitlist[i].datafile);
                            doc.Load(modfilepath);
                            XmlNode root = doc.DocumentElement;

                            XmlNodeList XMLunits = root.SelectNodes("descendant::" + unitlist[i].elementName);
                            foreach (XmlNode XMLunit in XMLunits)
                            {
                                if (XMLunit.Attributes[0].Value == unitname)
                                {
                                    WriteXMLTag("Affiliation", affils, doc, XMLunit);
                                    unit.affiliations = affilist;
                                    WriteXMLTag("Tech_Level", "0", doc, XMLunit);
                                    if(!SuppressCheckBox.Checked) WriteXMLTag("Build_Initially_Locked", "No", doc, XMLunit);
                                    if (PlanetTextBox.Text != "" || unit.planets.Length == 0)
                                    {
                                        WriteXMLTag("Required_Planets", PlanetTextBox.Text, doc, XMLunit);
                                        unit.planets = ReadWhiteSpaceAsCommas(PlanetTextBox.Text);
                                    }
                                    if ((unit.reqtemplate == "" || unit.reqtemplate == unitname))
                                    {
                                        WriteXMLTag("Required_Special_Structures", structurefield + structureSuffix, doc, XMLunit);
                                        unit.UsedStrucutures = structurelist;
                                    }
                                    else
                                    {
                                        templateUnit = unit.reqtemplate;
                                        templatePath = unit.reqfile;
                                        TemplateList.Add(unit.reqtemplate);
                                    }
                                    if (SpaceRadioButton.Checked) WriteXMLTag("Build_Tab_Space_Units", "Yes", doc, XMLunit);
                                    else WriteXMLTag("Build_Tab_Land_Units", "Yes", doc, XMLunit);

                                    unitlist[i] = unit;
                                    break;
                                }
                            }

                            doc.Save(ConvertMainPathToMod(unitlist[i].datafile));
                            if (templateUnit != "")
                            {
                                for (int j = 0; j < unitlist.Count; j++)
                                {
                                    if (unitlist[j].unitname == templateUnit)
                                    {
                                        unit abstraction = unitlist[j];
                                        foreach (string reqstru in abstraction.UsedStrucutures)
                                        {
                                            if (!structurelist.Contains(reqstru))
                                            {
                                                structurelist.Add(reqstru);
                                                if (firststruct) firststruct = false;
                                                else structurefield += " | ";
                                                structurefield += reqstru;
                                            }
                                        }
                                        abstraction.UsedStrucutures = structurelist;
                                        unitlist[j] = abstraction; //C# insists this must be 3 separate commands and not unitlist[j] = structurelist
                                        break;
                                    }
                                }

                                modfilepath = GetExtantPath(templatePath);
                                doc.Load(modfilepath);
                                root = doc.DocumentElement;

                                XMLunits = root.SelectNodes("descendant::" + unitlist[i].elementName); //Assuming the template has the same tag may be a problem, but is not that likely to be
                                bool notfound = true;

                                foreach (XmlNode XMLunit in XMLunits)
                                {
                                    if (XMLunit.Attributes[0].Value == templateUnit)
                                    {
                                        notfound = false;
                                        WriteXMLTag("Required_Special_Structures", structurefield + structureSuffix, doc, XMLunit);
                                        break;
                                    }
                                }
                                doc.Save(ConvertMainPathToMod(templatePath));
                                if (notfound) MessageBox.Show("Could not find template " + templateUnit + ". Please tell a dev the elementName cannot be assumed");
                            }
                            if (GroundRadioButton.Checked)
                            {
                                List<string> doneunits = new List<string>();
                                foreach (string groundunit in unit.companyunits)
                                {
                                    if (!doneunits.Contains(groundunit))
                                    {
                                        doneunits.Add(groundunit);
                                        foreach (unit unitdata in entities.groundUnits) //Todo probably need to separate infantry and vehicles
                                        {
                                            if (unitdata.unitname == groundunit)
                                            {
                                                modfilepath = GetExtantPath(unitdata.datafile);
                                                doc.Load(modfilepath);
                                                root = doc.DocumentElement;
                                                XMLunits = root.SelectNodes("descendant::" + unitdata.elementName);
                                                foreach (XmlNode XMLunit in XMLunits)
                                                {
                                                    if (XMLunit.Attributes[0].Value == unitdata.unitname)
                                                    {
                                                        WriteXMLTag("Affiliation", affils, doc, XMLunit);
                                                        break;
                                                    }
                                                }
                                                doc.Save(ConvertMainPathToMod(unitdata.datafile));
                                            }
                                        }
                                    }
                                }
                            }
                            unitlist[i] = unit;
                            break; //exit once you found the original unit in the outer loop
                        }
                    }
                }
                factiondoc.Save(ConvertMainPathToMod(globals.SourceMod + "XML\\Factions.xml")); //save bombard changes
                File.WriteAllLines(ConvertMainPathToMod(globals.SourceMod + "Scripts\\Library\\CustomLibrary.lua"), CustomLib);

                factiondoc = new XmlDocument();
                factiondoc.PreserveWhitespace = true;
                factiondoc.Load(GetExtantPath(globals.SourceMod + "XML\\Structures\\GalacticCorporations.xml"));
                facroot = factiondoc.DocumentElement;
                List<string> CheapNames = new List<string>();
                List<List<string>> CheapAffils = new List<List<string>>();
                var corps = facroot.SelectNodes("descendant::SpecialStructure");
                foreach (XmlElement corp in corps)
                {
                    string corpname = corp.GetAttribute("Name");
                    if (corpname.Contains("_Cheap"))
                    {
                        CheapNames.Add(corpname.Replace("_Cheap", ""));
                        List<string> CheapAff = new List<string>();
                        XmlNode value = corp.SelectSingleNode("descendant::Affiliation");
                        if (!(value is null))
                        {
                            XmlNode aff = value.LastChild;
                            if (!(aff is null))
                            {
                                string[] split = fullTrim(aff.Value).Split(',');
                                foreach (string affil in split)
                                {
                                    CheapAff.Add(affil);
                                }
                            }
                            CheapAffils.Add(CheapAff);
                        }
                    }
                }
                foreach (XmlElement corp in corps)
                {
                    string corpname = corp.GetAttribute("Name");
                    if (!corpname.Contains("_Cheap") && !corpname.Contains("Cloning_HQ"))
                    {
                        XmlNode value = corp.SelectSingleNode("descendant::Abilities");
                        if (!(value is null))
                        {
                            value = value.SelectSingleNode("descendant::Reduce_Production_Price_Ability");
                            if (!(value is null))
                            {
                                string cats = value.SelectSingleNode("descendant::Applicable_Unit_Categories").InnerText;
                                if (cats == "")
                                {
                                    List<string> corpaffils = new List<string>();
                                    List<string> cheaps = new List<string>();
                                    string newcorpaffil = "";
                                    bool firstcorp = true;
                                    for (int i = 0; i < CheapNames.Count; i++)
                                    {
                                        if (CheapNames[i] == corpname)
                                        {
                                            cheaps = CheapAffils[i];
                                            break;
                                        }
                                    }
                                    string[] discounts = ReadXMLCSString(value.SelectSingleNode("descendant::Applicable_Unit_Types").InnerText);

                                    foreach (unit unit in entities.spaceUnits)
                                    {
                                        if (discounts.Contains(unit.unitname))
                                        {
                                            foreach (string unitaffil in unit.affiliations)
                                            {
                                                if (!cheaps.Contains(unitaffil) && !corpaffils.Contains(unitaffil))
                                                {
                                                    if (firstcorp) firstcorp = false;
                                                    else newcorpaffil += ", ";
                                                    newcorpaffil += unitaffil;
                                                    corpaffils.Add(unitaffil);
                                                }
                                            }
                                        }
                                    }
                                    foreach (unit unit in entities.groundCompanies)
                                    {
                                        if (discounts.Contains(unit.unitname))
                                        {
                                            foreach (string unitaffil in unit.affiliations)
                                            {
                                                if (!cheaps.Contains(unitaffil) && !corpaffils.Contains(unitaffil))
                                                {
                                                    if (firstcorp) firstcorp = false;
                                                    else newcorpaffil += ", ";
                                                    newcorpaffil += unitaffil;
                                                    corpaffils.Add(unitaffil);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                factiondoc.Save(ConvertMainPathToMod(globals.SourceMod + "XML\\Structures\\GalacticCorporations.xml"));
            }
            else // unit stats section
            {
                List<unit> unitlist = entities.spaceUnits;
                if (GroundRadioButton.Checked) unitlist = entities.groundCompanies;
                else if (UnitRadioButton.Checked) unitlist = entities.groundUnits;

                for (int i = 0; i < unitlist.Count; i++)
                {
                    if (unitlist[i].unitname == (String)AffilListBox.SelectedItem)
                    {
                        int count = unitlist[i].variantchain.Count;
                        unit[] templates = new unit[count + 1];
                        for (int j = 0; j < count; j++)
                        {
                            for (int k = 0; k < unitlist.Count; k++)
                            {
                                if (unitlist[k].unitname == unitlist[i].variantchain[j])
                                {
                                    templates[j] = unitlist[k];
                                    break;
                                }
                            }
                        }
                        templates[count] = unitlist[i];

                        for (int j = count; j >= 0; j--)
                        {
                            bool MainUnit = j == count;
                            bool changed = false;
                            unit unit = templates[j];
                            XmlDocument doc = new XmlDocument();
                            doc.PreserveWhitespace = true;
                            string modfilepath = GetExtantPath(unit.datafile);
                            doc.Load(modfilepath);
                            XmlNodeList XMLunits = doc.DocumentElement.SelectNodes("descendant::" + unit.elementName);
                            XmlNode XMLunit = XMLunits[0];
                            foreach (XmlNode XMLunidad in XMLunits)
                            {
                                if (XMLunidad.Attributes[0].Value == unit.unitname)
                                {
                                    XMLunit = XMLunidad;
                                    break;
                                }
                            }

                            if (PopBox.Enabled && PopBox.Value != unit.pop && (MainUnit && (!StatTemplateCheckBox.Checked || unit.pop_baseID < 0) || templates[count].pop_baseID == j))
                            {
                                unit.pop = (int)PopBox.Value;
                                if (MainUnit)
                                {//Desync from variant chain if editing main
                                    unit.pop_baseID = -1;
                                    templates[count].pop_baseID = -1;
                                }
                                //todo if I hate myself, redo variant chains for things that inherit from the MainUnit so people get it within this session instead of having to restart the program
                                for (int k = 0; k < unitlist.Count; k++) //Update the stats of anything that pulls stats from it
                                {
                                    unit unit2 = unitlist[k];
                                    for (int l = 0; l < unit2.variantchain.Count; l++)
                                    {
                                        if(unit2.variantchain[l] == unit.unitname && unit2.pop_baseID == l)
                                        {
                                            unit2.pop = unit.pop;
                                            break;
                                        }
                                    } 
                                }
                                WriteXMLTag("Population_Value", unit.pop.ToString(), doc, XMLunit);
                                changed = true;
                            }

                            if (CostBox.Enabled && CostBox.Value != unit.cost && (MainUnit && (!StatTemplateCheckBox.Checked || unit.cost_baseID < 0) || templates[count].cost_baseID == j))
                            {
                                unit.cost = (int)CostBox.Value;
                                if (MainUnit)
                                {
                                    unit.cost_baseID = -1;
                                    templates[count].cost_baseID = -1;
                                }
                                for (int k = 0; k < unitlist.Count; k++)
                                {
                                    unit unit2 = unitlist[k];
                                    for (int l = 0; l < unit2.variantchain.Count; l++)
                                    {
                                        if (unit2.variantchain[l] == unit.unitname && unit2.cost_baseID == l)
                                        {
                                            unit2.cost = unit.cost;
                                            break;
                                        }
                                    }
                                }
                                WriteXMLTag("Build_Cost_Credits", unit.cost.ToString(), doc, XMLunit);
                                changed = true;
                            }

                            if (BuildTimeBox.Enabled && BuildTimeBox.Value != unit.buildtime && (MainUnit && (!StatTemplateCheckBox.Checked || unit.buildtime_baseID < 0) || templates[count].buildtime_baseID == j))
                            {
                                unit.buildtime = (int)BuildTimeBox.Value;
                                if (MainUnit)
                                {
                                    unit.buildtime_baseID = -1;
                                    templates[count].buildtime_baseID = -1;
                                }
                                for (int k = 0; k < unitlist.Count; k++)
                                {
                                    unit unit2 = unitlist[k];
                                    for (int l = 0; l < unit2.variantchain.Count; l++)
                                    {
                                        if (unit2.variantchain[l] == unit.unitname && unit2.buildtime_baseID == l)
                                        {
                                            unit2.buildtime = unit.buildtime;
                                            break;
                                        }
                                    }
                                }
                                WriteXMLTag("Build_Time_Seconds", unit.buildtime.ToString(), doc, XMLunit);
                                changed = true;
                            }

                            int GUIrow = 0;
                            if (GUIRowComboBox.SelectedIndex == 0) GUIrow = 1;
                            if (GUIRowComboBox.Enabled && GUIrow != unit.gui_row && (MainUnit && (!StatTemplateCheckBox.Checked || unit.gui_row_baseID < 0) || templates[count].gui_row_baseID == j))
                            {
                                unit.gui_row = GUIrow;
                                if (MainUnit)
                                {
                                    unit.gui_row_baseID = -1;
                                    templates[count].gui_row_baseID = -1;
                                }
                                for (int k = 0; k < unitlist.Count; k++)
                                {
                                    unit unit2 = unitlist[k];
                                    for (int l = 0; l < unit2.variantchain.Count; l++)
                                    {
                                        if (unit2.variantchain[l] == unit.unitname && unit2.gui_row_baseID == l)
                                        {
                                            unit2.gui_row = unit.gui_row;
                                            break;
                                        }
                                    }
                                }
                                WriteXMLTag("GUI_Row", unit.gui_row.ToString(), doc, XMLunit);
                                changed = true;
                            }

                            if (hpBox.Enabled && hpBox.Value != unit.hp && (MainUnit && unit.hp_baseID < 0 || templates[count].hp_baseID == j))
                            {
                                unit.hp = (int)hpBox.Value;
                                for (int k = 0; k < unitlist.Count; k++)
                                {
                                    unit unit2 = unitlist[k];
                                    for (int l = 0; l < unit2.variantchain.Count; l++)
                                    {
                                        if (unit2.variantchain[l] == unit.unitname && unit2.hp_baseID == l)
                                        {
                                            unit2.hp = unit.hp;
                                            break;
                                        }
                                    }
                                }
                                WriteXMLTag("Tactical_Health", unit.hp.ToString(), doc, XMLunit);
                                changed = true;
                            }

                            if (ATypeComboBox.Enabled && ATypeComboBox.Text != unit.armor_type && (MainUnit && unit.atype_baseID < 0 || templates[count].atype_baseID == j))
                            {
                                unit.armor_type = ATypeComboBox.Text;
                                for (int k = 0; k < unitlist.Count; k++)
                                {
                                    unit unit2 = unitlist[k];
                                    for (int l = 0; l < unit2.variantchain.Count; l++)
                                    {
                                        if (unit2.variantchain[l] == unit.unitname && unit2.atype_baseID == l)
                                        {
                                            unit2.armor_type = unit.armor_type;
                                            break;
                                        }
                                    }
                                }
                                WriteXMLTag("Armor_Type", unit.armor_type, doc, XMLunit);
                                changed = true;
                            }

                            if (ShieldBox.Enabled && ShieldBox.Value != unit.shield && (MainUnit && unit.shield_baseID < 0 || templates[count].shield_baseID == j))
                            {
                                unit.shield = (int)ShieldBox.Value;
                                for (int k = 0; k < unitlist.Count; k++)
                                {
                                    unit unit2 = unitlist[k];
                                    for (int l = 0; l < unit2.variantchain.Count; l++)
                                    {
                                        if (unit2.variantchain[l] == unit.unitname && unit2.shield_baseID == l)
                                        {
                                            unit2.shield = unit.shield;
                                            break;
                                        }
                                    }
                                }
                                WriteXMLTag("Shield_Points", unit.shield.ToString(), doc, XMLunit);
                                changed = true;
                            }

                            if (STypeComboBox.Enabled && STypeComboBox.Text != unit.shield_type && (MainUnit && unit.stype_baseID < 0 || templates[count].stype_baseID == j))
                            {
                                unit.shield_type = STypeComboBox.Text;
                                for (int k = 0; k < unitlist.Count; k++)
                                {
                                    unit unit2 = unitlist[k];
                                    for (int l = 0; l < unit2.variantchain.Count; l++)
                                    {
                                        if (unit2.variantchain[l] == unit.unitname && unit2.stype_baseID == l)
                                        {
                                            unit2.shield_type = unit.shield_type;
                                            break;
                                        }
                                    }
                                }
                                WriteXMLTag("Shield_Armor_Type", unit.shield_type, doc, XMLunit);
                                changed = true;
                            }

                            if (RegenBox.Enabled && RegenBox.Value != (decimal)unit.regen && (MainUnit && unit.regen_baseID < 0 || templates[count].regen_baseID == j))
                            {
                                unit.regen = (float)RegenBox.Value;
                                for (int k = 0; k < unitlist.Count; k++)
                                {
                                    unit unit2 = unitlist[k];
                                    for (int l = 0; l < unit2.variantchain.Count; l++)
                                    {
                                        if (unit2.variantchain[l] == unit.unitname && unit2.regen_baseID == l)
                                        {
                                            unit2.regen = unit.regen;
                                            break;
                                        }
                                    }
                                }
                                WriteXMLTag("Shield_Refresh_Rate", unit.regen.ToString(), doc, XMLunit);
                                changed = true;
                            }

                            if (SpeedBox.Enabled && SpeedBox.Value != (decimal)unit.speed && (MainUnit && unit.speed_baseID < 0 || templates[count].speed_baseID == j))
                            {
                                unit.speed = (float)SpeedBox.Value;
                                for (int k = 0; k < unitlist.Count; k++)
                                {
                                    unit unit2 = unitlist[k];
                                    for (int l = 0; l < unit2.variantchain.Count; l++)
                                    {
                                        if (unit2.variantchain[l] == unit.unitname && unit2.speed_baseID == l)
                                        {
                                            unit2.speed = unit.speed;
                                            break;
                                        }
                                    }
                                }
                                WriteXMLTag("Max_Speed", unit.speed.ToString(), doc, XMLunit);
                                changed = true;
                            }

                            if (MinSpeedBox.Enabled && MinSpeedBox.Visible && MinSpeedBox.Value != (decimal)unit.min_speed && (MainUnit && unit.min_speed_baseID < 0 || templates[count].min_speed_baseID == j))
                            {
                                unit.min_speed = (float)MinSpeedBox.Value;
                                for (int k = 0; k < unitlist.Count; k++)
                                {
                                    unit unit2 = unitlist[k];
                                    for (int l = 0; l < unit2.variantchain.Count; l++)
                                    {
                                        if (unit2.variantchain[l] == unit.unitname && unit2.min_speed_baseID == l)
                                        {
                                            unit2.min_speed = unit.min_speed;
                                            break;
                                        }
                                    }
                                }
                                WriteXMLTag("Min_Speed", unit.min_speed.ToString(), doc, XMLunit);
                                changed = true;
                            }

                            if (TurnBox.Enabled && TurnBox.Value != (decimal)unit.turn && (MainUnit && unit.turn_baseID < 0 || templates[count].turn_baseID == j))
                            {
                                unit.turn = (float)TurnBox.Value;
                                for (int k = 0; k < unitlist.Count; k++)
                                {
                                    unit unit2 = unitlist[k];
                                    for (int l = 0; l < unit2.variantchain.Count; l++)
                                    {
                                        if (unit2.variantchain[l] == unit.unitname && unit2.turn_baseID == l)
                                        {
                                            unit2.turn = unit.turn;
                                            break;
                                        }
                                    }
                                }
                                WriteXMLTag("Max_Rate_Of_Turn", unit.turn.ToString(), doc, XMLunit);
                                changed = true;
                            }

                            if (AccelBox.Enabled && AccelBox.Value != (decimal)unit.accel && (MainUnit && unit.accel_baseID < 0 || templates[count].accel_baseID == j))
                            {
                                unit.accel = (float)AccelBox.Value;
                                for (int k = 0; k < unitlist.Count; k++)
                                {
                                    unit unit2 = unitlist[k];
                                    for (int l = 0; l < unit2.variantchain.Count; l++)
                                    {
                                        if (unit2.variantchain[l] == unit.unitname && unit2.accel_baseID == l)
                                        {
                                            unit2.accel = unit.accel;
                                            break;
                                        }
                                    }
                                }
                                WriteXMLTag("OverrideAcceleration", unit.accel.ToString(), doc, XMLunit);
                                if (SpaceRadioButton.Checked) WriteXMLTag("OverrideDeceleration", (unit.accel * 2).ToString(), doc, XMLunit);
                                changed = true;
                            }

                            if (ConcurrentBox.Enabled && ConcurrentBox.Visible && ConcurrentBox.Value != unit.limit_concurrent && (MainUnit && unit.concurrent_baseID < 0 || templates[count].concurrent_baseID == j))
                            {
                                unit.limit_concurrent = (int)ConcurrentBox.Value;
                                for (int k = 0; k < unitlist.Count; k++)
                                {
                                    unit unit2 = unitlist[k];
                                    for (int l = 0; l < unit2.variantchain.Count; l++)
                                    {
                                        if (unit2.variantchain[l] == unit.unitname && unit2.concurrent_baseID == l)
                                        {
                                            unit2.limit_concurrent = unit.limit_concurrent;
                                            break;
                                        }
                                    }
                                }
                                WriteXMLTag("Build_Limit_Current_Per_Player", unit.limit_concurrent.ToString(), doc, XMLunit);
                                changed = true;
                            }

                            if (LifetimeBox.Enabled && LifetimeBox.Visible && LifetimeBox.Value != unit.limit_lifetime && (MainUnit && unit.lifetime_baseID < 0 || templates[count].lifetime_baseID == j))
                            {
                                unit.limit_lifetime = (int)LifetimeBox.Value;
                                for (int k = 0; k < unitlist.Count; k++)
                                {
                                    unit unit2 = unitlist[k];
                                    for (int l = 0; l < unit2.variantchain.Count; l++)
                                    {
                                        if (unit2.variantchain[l] == unit.unitname && unit2.lifetime_baseID == l)
                                        {
                                            unit2.limit_lifetime = unit.limit_lifetime;
                                            break;
                                        }
                                    }
                                }
                                WriteXMLTag("Build_Limit_Lifetime_Per_Player", unit.limit_lifetime.ToString(), doc, XMLunit);
                                changed = true;
                            }


                            if (MainUnit)
                            {
                                string[] textfile = LoadText();
                                string[] strings = SplitXMLWhitespaceList(unit.tooltip);
                                foreach (string TextID in strings)
                                {
                                    if (ATypeComboBox.Visible && (TextID.Contains("_HULL") || TextID.Contains("_HEALTH"))) {
                                        string corenne = "";
                                        if (SpaceRadioButton.Checked) corenne += "Hull: ";
                                        else corenne += "Health: ";
                                        corenne += hpBox.Value.ToString() + ArmorTypeString(unit.armor_type);

                                        textfile = SetTextID(textfile, TextID, corenne);
                                    }
                                    else if (STypeComboBox.Visible && TextID.Contains("_SHIELD"))
                                    {
                                        string corenne = "Shields: " +ShieldBox.Value.ToString() + " / [" + RegenBox.Value.ToString("0.#") + "/R]" + ArmorTypeString(unit.shield_type);
                                        textfile = SetTextID(textfile, TextID, corenne);
                                    }
                                    else if (SpeedBox.Visible && TextID.Contains("_MOVE"))
                                    {
                                        string corenne = "Speed: " + SpeedBox.Value.ToString("0.0#");
                                        if (AccelBox.Visible && AccelBox.Enabled) corenne += " | Accel: " + AccelBox.Value.ToString("0.0##");
                                        if (TurnBox.Visible && TurnBox.Enabled) corenne += " | Turn: " + TurnBox.Value.ToString("0.0");
                                        textfile = SetTextID(textfile, TextID, corenne);
                                    }
                                    else if (ConcurrentBox.Visible && ConcurrentBox.Enabled && TextID.Contains("_BUILDLIMIT"))
                                    {//TODO most infantry have (requires someone). Also it's Lua
                                        string corenne = "Build Limit: " + ConcurrentBox.Value.ToString() + " | Lifetime: ";
                                        if (LifetimeBox.Visible && LifetimeBox.Enabled) corenne += LifetimeBox.Value.ToString();
                                        else corenne += "∞";
                                        textfile = SetTextID(textfile, TextID, corenne);
                                    }
                                }

                                SaveText(textfile);
                            }

                            if (changed) {
                                doc.Save(ConvertMainPathToMod(unit.datafile));
                                for (int k = 0; k < unitlist.Count; k++)
                                {
                                    if (unitlist[k].unitname == unit.unitname)
                                    {
                                        unitlist[k] = unit;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Thread.CurrentThread.CurrentCulture = globals.UIculture;

            MessageBox.Show("Changes saved");
        }

        private void UnitTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (UnitTabControl.SelectedIndex)
            {
                case 0:
                    UnitRadioButton.Visible = false;
                    AffilListBox.SelectionMode = SelectionMode.MultiExtended;
                    if (UnitRadioButton.Checked) GroundRadioButton.Checked = true;
                    break;
                case 1:
                    UnitRadioButton.Visible = true;
                    AffilListBox.SelectionMode = SelectionMode.One;
                    break;
                    //default:
                    // code block
                    //break;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string newmod = NewModTextBox.Text.Replace(" ", "_");

            if (newmod == "")
            {
                MessageBox.Show("A mod name must be entered.");
                NewModTextBox.Select();
                return;
            }

            if (ModListBox.Items.Contains(newmod))
            {
                MessageBox.Show("A mod of that name already exists");
                ModListBox.SelectedItem = newmod;
                return;
            }

            globals.LocalMod = globals.ModFolder + newmod;
            System.IO.Directory.CreateDirectory(globals.LocalMod);
            File.WriteAllText(globals.LocalMod + "\\Tilotny", LastFolderOrFile(UpOneFolder(UpOneFolder(globals.SourceMod))));
            string[] files = Directory.GetFiles(globals.SourceMod + "Text", "*.txt", SearchOption.AllDirectories);

            CopyMainToMod(globals.SourceMod + "Text\\MasterTextFile_ENGLISH.dat");
            CopyMainToMod(globals.SourceMod + "Text\\datassembler.exe");
            System.Diagnostics.Process.Start("\"" + globals.LocalMod + "\\Data\\Text\\datassembler.exe\"", "/e \"" + globals.LocalMod + "\\Data\\Text\\MasterTextFile_ENGLISH.dat\" \"" + globals.LocalMod + "\\Data\\Text\\MasterTextFile_ENGLISH.txt\"");
            CopyMainToMod(globals.SourceMod + "Text\\Submod_text.txt");

            ModListBox.Items.Add(newmod);
            ModListBox.SelectedItem = newmod;
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete " + globals.LocalModName + "?", "Delete Submod", MessageBoxButtons.YesNoCancel) == DialogResult.Yes) //Creates the yes function
            {
                Directory.Delete(UpOneFolder(globals.LocalMod), true);
                ModListBox.Items.Remove(globals.LocalModName);
            }
        }

        private void CopyModButton_Click(object sender, EventArgs e)
        {
            string newmod = NewModTextBox.Text.Replace(" ", "_");

            if (newmod == "")
            {
                MessageBox.Show("A mod name must be entered.");
                NewModTextBox.Select();
                return;
            }

            if (ModListBox.Items.Contains(newmod))
            {
                MessageBox.Show("A mod of that name already exists");
                ModListBox.SelectedItem = newmod;
                return;
            }

            //copying folders recursively is apparently quite hard
        }

        private void ModListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ModListBox.SelectedItem != null)
            {
                setLocalMod(ModListBox.SelectedItem.ToString());
                CopyModButton.Enabled = true;
            }
            else
            {
                ModNameLabel.Text = "Active Submod: None";
                LaunchOptionsIndicator.Text = "";
                globals.LocalMod = "";
                globals.LocalModName = "";
                tabHidePanel.Visible = true;
                LaunchModButton.Enabled = false;
                ModFilesButton.Enabled = false;
                CopyModButton.Enabled = false;
            }
            globals.unitsloaded = false;
        }

        private void setLocalMod(string mod)
        {
            ModNameLabel.Text = "Active Mod: " + mod;
            LaunchOptionsIndicator.Text = "Modpath=Mods\\" + mod + " STEAMMOD=" + globals.SourceModName;

            globals.LocalMod = globals.ModFolder + mod + "\\Data";
            globals.LocalModName = mod;

            tabHidePanel.Visible = false;
            LaunchModButton.Enabled = true;
            ModFilesButton.Enabled = true;

            entities.modpaths.Clear();
            //Todo full custom mod stacks properly
            entities.modpaths.Add(globals.LocalMod);
            entities.modpaths.Add(globals.SourceMod);
        }

        private void CopyMainToMod(string MainPath)
        {
            File.Copy(MainPath, ConvertMainPathToMod(MainPath, false, true));
        }

        private void ReadFactionData()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
            while (FactionPanel.Controls.Count > 0)
            {
                foreach (Control tokill in FactionPanel.Controls)
                {
                    FactionPanel.Controls.Remove(tokill);
                }
            }
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.Load(GetExtantPath(globals.SourceMod + "XML\\Factions.xml"));
            XmlNode root = doc.DocumentElement;
            XmlNode cap = root.SelectSingleNode("descendant::Faction").SelectSingleNode("descendant::Space_Tactical_Unit_Cap");
            PopulationBox.Value = Convert.ToInt32(cap.LastChild.Value);
            var factions = root.SelectNodes("descendant::Faction");
            string[] gameconstants = File.ReadAllLines(GetExtantPath(globals.SourceMod+"Scripts\\Library"+globals.ContentLoaderPath+ "\\GameConstants.lua"));
            int factionnamestart = 0;
            for (int i = 0; i < gameconstants.Length; i++)
            {
                if (gameconstants[i].Contains("ALL_FACTION_NAMES"))
                {
                    factionnamestart = i + 1;
                    break;
                }
            }
            int factionCount = -1;
            foreach (XmlElement faction in factions)
            {
                string name = faction.GetAttribute("Name");
                string id = faction.SelectSingleNode("descendant::Text_ID").InnerText.Trim();
                string color = faction.SelectSingleNode("descendant::Color").InnerText.Trim();
                string taccolor = faction.SelectSingleNode("descendant::No_Colorization_Color").InnerText.Trim();
                factionCount += 1;
                int[] col = ReadXMLCSV(color);
                int[] tcol = ReadXMLCSV(taccolor);

                string LuaName = name.ToUpper() + " = ";
                bool found = false;
                for (int i = factionnamestart; i < factionnamestart + 100; i++)
                {
                    string visible = gameconstants[i];
                    if (visible.Contains(LuaName))
                    {
                        LuaName = visible.Substring(visible.IndexOf("\"")+1, visible.LastIndexOf("\"") - visible.IndexOf("\"") - 1);
                        if (LuaName == "REPORT_THIS_PROTEUS_BUG_B") //Probably best to hide this, but it's always going to be a bit weird when it shares an ID with nonplayable
                        {
                            LuaName = "Minor Warlords";
                        }
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    LuaName = "";
                }

                int basey = 55 * factionCount;
                int secondy = basey + 24;

                var label = new Label();
                label.Location = new Point(10, basey);
                label.Text = name;
                label.Tag = name;
                FactionPanel.Controls.Add(label);

                var namelabel = new Label();
                namelabel.Location = new Point(10, secondy);
                namelabel.Text = "Name:";
                namelabel.Width = 40;
                FactionPanel.Controls.Add(namelabel);

                var namebox = new TextBox();
                namebox.Location = new Point(50, secondy);
                namebox.Tag = name;
                namebox.Width = 150;
                namebox.Text = LuaName;
                FactionPanel.Controls.Add(namebox);

                int dropwidth = 40;

                var colorlabel = new Button();
                colorlabel.Location = new Point(220, basey);
                colorlabel.Tag = name;
                colorlabel.Text = "Faction Color RGB Alpha...";
                colorlabel.BackColor = Color.FromArgb(255, col[0], col[1], col[2]);
                colorlabel.Width = 370 + dropwidth - 220;
                colorlabel.Name = "MC_" + factionCount.ToString();
                colorlabel.Click += new EventHandler(this.ColorButton_Click);
                FactionPanel.Controls.Add(colorlabel);

                var rcolor = new NumericUpDown();
                rcolor.Location = new Point(220, secondy);
                rcolor.Tag = name;
                rcolor.Maximum = 255;
                rcolor.Value = col[0];
                rcolor.Width = dropwidth;
                rcolor.Name = "CR_" + factionCount.ToString();
                rcolor.ValueChanged += new EventHandler(this.NumericColor_Set);
                FactionPanel.Controls.Add(rcolor);

                var gcolor = new NumericUpDown();
                gcolor.Location = new Point(270, secondy);
                gcolor.Tag = name;
                gcolor.Maximum = 255;
                gcolor.Value = col[1];
                gcolor.Width = dropwidth;
                gcolor.Name = "CG_" + factionCount.ToString();
                gcolor.ValueChanged += new EventHandler(this.NumericColor_Set);
                FactionPanel.Controls.Add(gcolor);

                var bcolor = new NumericUpDown();
                bcolor.Location = new Point(320, secondy);
                bcolor.Tag = name;
                bcolor.Maximum = 255;
                bcolor.Value = col[2];
                bcolor.Width = dropwidth;
                bcolor.Name = "CB_" + factionCount.ToString();
                bcolor.ValueChanged += new EventHandler(this.NumericColor_Set);
                FactionPanel.Controls.Add(bcolor);

                var acolor = new NumericUpDown();
                acolor.Location = new Point(370, secondy);
                acolor.Tag = name;
                acolor.Maximum = 255;
                acolor.Value = col[3];
                acolor.Width = dropwidth;
                acolor.Name = "CA_" + factionCount.ToString();
                FactionPanel.Controls.Add(acolor);

                var tcolorlabel = new Button();
                tcolorlabel.Location = new Point(500, basey);
                tcolorlabel.Tag = name;
                tcolorlabel.Text = "Tactical Unit Color RGB Alpha...";
                tcolorlabel.BackColor = Color.FromArgb(255, tcol[0], tcol[1], tcol[2]);
                tcolorlabel.Width = 650 + dropwidth - 500; ;
                tcolorlabel.Name = "TC_" + factionCount.ToString();
                tcolorlabel.Click += new EventHandler(this.tColorButton_Click);
                FactionPanel.Controls.Add(tcolorlabel);

                var rtcolor = new NumericUpDown();
                rtcolor.Location = new Point(500, secondy);
                rtcolor.Tag = name;
                rtcolor.Maximum = 255;
                rtcolor.Value = tcol[0];
                rtcolor.Width = dropwidth;
                rtcolor.Name = "TR_" + factionCount.ToString();
                rtcolor.ValueChanged += new EventHandler(this.NumerictColor_Set);
                FactionPanel.Controls.Add(rtcolor);

                var gtcolor = new NumericUpDown();
                gtcolor.Location = new Point(550, secondy);
                gtcolor.Tag = name;
                gtcolor.Maximum = 255;
                gtcolor.Value = tcol[1];
                gtcolor.Width = dropwidth;
                gtcolor.Name = "TG_" + factionCount.ToString();
                gtcolor.ValueChanged += new EventHandler(this.NumerictColor_Set);
                FactionPanel.Controls.Add(gtcolor);

                var btcolor = new NumericUpDown();
                btcolor.Location = new Point(600, secondy);
                btcolor.Tag = name;
                btcolor.Maximum = 255;
                btcolor.Value = tcol[2];
                btcolor.Width = dropwidth;
                btcolor.Name = "TB_" + factionCount.ToString();
                btcolor.ValueChanged += new EventHandler(this.NumerictColor_Set);
                FactionPanel.Controls.Add(btcolor);

                var atcolor = new NumericUpDown();
                atcolor.Location = new Point(650, secondy);
                atcolor.Tag = name;
                atcolor.Maximum = 255;
                atcolor.Value = tcol[3];
                atcolor.Width = dropwidth;
                atcolor.Name = "TA_" + factionCount.ToString();
                FactionPanel.Controls.Add(atcolor);

                var transparent = new Button();
                transparent.Location = new Point(700, secondy);
                transparent.Tag = name;
                transparent.Text = "Set Transparent";
                transparent.Width = 120;
                transparent.Click += new EventHandler(this.TransparentModButton_Click);
                FactionPanel.Controls.Add(transparent);

                var copy = new Button();
                copy.Location = new Point(700, basey);
                copy.Tag = name;
                copy.Text = "Copy main color";
                copy.Width = 120;
                copy.Click += new EventHandler(this.CopyColorButton_Click);
                FactionPanel.Controls.Add(copy);

                doc = new XmlDocument();
                doc.PreserveWhitespace = true;
                doc.Load(GetExtantPath(globals.SourceMod + "XML\\GameConstants.xml"));
                root = doc.DocumentElement;
                string predict = root.SelectSingleNode("descendant::ShouldDisplayPredictionPaths").InnerText.Trim();
                if (predict.ToUpper().Contains("TRUE")) PathfindCheckBox.Checked = true;
            }

            Thread.CurrentThread.CurrentCulture = globals.UIculture;
        }

        private void SaveFactionButton_Click(object sender, EventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.Load(GetExtantPath(globals.SourceMod + "XML\\GameConstants.xml"));
            XmlNode constroot = doc.DocumentElement;
            string good = constroot.SelectSingleNode("descendant::Good_Side_Name").InnerText.Trim();
            string evil = constroot.SelectSingleNode("descendant::Evil_Side_Name").InnerText.Trim();
            string corrupt = constroot.SelectSingleNode("descendant::Corrupt_Side_Name").InnerText.Trim();
            XmlNode pathfind = constroot.SelectSingleNode("descendant::ShouldDisplayPredictionPaths");
            if (PathfindCheckBox.Checked) pathfind.InnerText = "true";
            else pathfind.InnerText = "false";
            string dest = globals.LocalMod + "\\XML\\GameConstants.xml";
            System.IO.Directory.CreateDirectory(UpOneFolder(dest));
            doc.PreserveWhitespace = true;
            doc.Save(dest);

            XmlDocument trades = new XmlDocument();
            trades.PreserveWhitespace = true;
            trades.Load(GetExtantPath(globals.SourceMod + "XML\\TradeRouteLines.xml"));
            XmlNode traderoot = trades.DocumentElement.SelectSingleNode("descendant::TradeRouteLine");
            XmlNodeList traderoutes = traderoot.SelectNodes("descendant::Settings_For_Faction");

            doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.Load(GetExtantPath(globals.SourceMod + "XML\\Factions.xml"));
            XmlNode root = doc.DocumentElement;
            XmlNodeList factions = root.SelectNodes("descendant::Faction");

            string constantsPath = globals.SourceMod + "Scripts\\Library" + globals.ContentLoaderPath + "\\GameConstants.lua";
            string[] gameconstants = File.ReadAllLines(GetExtantPath(constantsPath));
            int factionnamestart = 0;
            int factioncolorstart = 0;
            string[] textfile = LoadText();

            for (int i = 0; i < gameconstants.Length; i++)
            {
                if (gameconstants[i].Contains("FACTION_COLORS"))
                {
                    factioncolorstart = i + 1;
                }
                if (gameconstants[i].Contains("ALL_FACTION_NAMES"))
                {
                    factionnamestart = i + 1;
                }
                if (factioncolorstart > 0 && factionnamestart > 0) break;
            }

            foreach (XmlElement faction in factions)
            {
                string name = faction.GetAttribute("Name");
                int[] argbs = new int[8];
                string newStringID = "";
                foreach (NumericUpDown control in FactionPanel.Controls.OfType<NumericUpDown>())
                {
                    if (name == (String)control.Tag)
                    {
                        if (control.Name.Contains("CR")) argbs[0] = (int)control.Value;
                        else if(control.Name.Contains("CG")) argbs[1] = (int)control.Value;
                        else if (control.Name.Contains("CB")) argbs[2] = (int)control.Value;
                        else if (control.Name.Contains("CA")) argbs[3] = (int)control.Value;
                        else if (control.Name.Contains("TR")) argbs[4] = (int)control.Value;
                        else if (control.Name.Contains("TG")) argbs[5] = (int)control.Value;
                        else if (control.Name.Contains("TB")) argbs[6] = (int)control.Value;
                        else if (control.Name.Contains("TA")) argbs[7] = (int)control.Value;
                    }
                }
                foreach (TextBox control in FactionPanel.Controls.OfType<TextBox>())
                {
                    if (name == (String)control.Tag)
                    {
                        newStringID = control.Text;
                    }
                }
                if (name != "Imperial_Proteus")
                {
                    XmlNode text = faction.SelectSingleNode("descendant::Text_ID"); //FotR Underworld is the main offender, but it is theoretically possible any faction doesn't have one
                    if (text.LastChild != null)
                    {
                        string Text_ID = text.LastChild.Value.Trim();
                        textfile = SetTextID(textfile, Text_ID, newStringID);
                    }
                }

                XmlNode pop = faction.SelectSingleNode("descendant::Space_Tactical_Unit_Cap");
                pop.LastChild.Value = PopulationBox.Value.ToString();

                string Mcolor = argbs[0].ToString() + ", " + argbs[1].ToString() + ", " + argbs[2].ToString() + ", " + argbs[3].ToString();
                XmlNode color = faction.SelectSingleNode("descendant::Color");
                color.LastChild.Value = Mcolor;
                XmlNode dcolor = faction.SelectSingleNode("descendant::Display_Font_Color");
                dcolor.LastChild.Value = Mcolor;
                XmlNode rcolor = faction.SelectSingleNode("descendant::Space_Retreat_Countdown_Color_RGBA");
                if (rcolor != null) rcolor.LastChild.Value = Mcolor; //Unplayables don't have every field
                XmlNode lcolor = faction.SelectSingleNode("descendant::Land_Retreat_Countdown_Color_RGBA");
                if (lcolor != null) lcolor.LastChild.Value = Mcolor;
                XmlNode scolor = faction.SelectSingleNode("descendant::Selection_Blob_RGBA");
                if (scolor != null) scolor.LastChild.Value = Mcolor;

                if(name.ToUpper() == good)
                {
                    foreach (XmlNode route in traderoutes)
                    {
                        string routename = faction.GetAttribute("Good");
                        if (routename == name)
                        {
                            XmlNode ZOcolor = route.SelectSingleNode("descendant::Color_Zoomed_Out");
                            if (!ZOcolor.InnerText.Contains("150, 150, 150, 255"))
                            {
                                ZOcolor.InnerText = Mcolor;
                                XmlNode ZIcolor =  route.SelectSingleNode("descendant::Color_Zoomed_In");
                                ZIcolor.InnerText = Mcolor;
                            }
                        }
                    }
                }

                if (name.ToUpper() == evil)
                {
                    foreach (XmlNode route in traderoutes)
                    {
                        string routename = faction.GetAttribute("Evil");
                        if (routename == name)
                        {
                            XmlNode ZOcolor = route.SelectSingleNode("descendant::Color_Zoomed_Out");
                            if (!ZOcolor.InnerText.Contains("150, 150, 150, 255"))
                            {
                                ZOcolor.InnerText = Mcolor;
                                XmlNode ZIcolor = route.SelectSingleNode("descendant::Color_Zoomed_In");
                                ZIcolor.InnerText = Mcolor;
                            }
                        }
                    }
                }

                if (name.ToUpper() == corrupt)
                {
                    foreach (XmlNode route in traderoutes)
                    {
                        string routename = faction.GetAttribute("Corrupt");
                        if (routename == name)
                        {
                            XmlNode ZOcolor = route.SelectSingleNode("descendant::Color_Zoomed_Out");
                            if (!ZOcolor.InnerText.Contains("150, 150, 150, 255"))
                            {
                                ZOcolor.InnerText = Mcolor;
                                XmlNode ZIcolor = route.SelectSingleNode("descendant::Color_Zoomed_In");
                                ZIcolor.InnerText = Mcolor;
                            }
                        }
                    }
                }

                XmlNode tcolor = faction.SelectSingleNode("descendant::No_Colorization_Color");
                tcolor.LastChild.Value = argbs[4].ToString() + ", " + argbs[5].ToString() + ", " + argbs[6].ToString() + ", " + argbs[7].ToString();

                string LuaName = "[\"" + name.ToUpper() + "\"]";
                for (int i = factioncolorstart; i < factioncolorstart + 100; i++)
                {
                    string visible = gameconstants[i];
                    if (visible.Contains(LuaName))
                    {
                        gameconstants[i] = "        " + LuaName + " = {r = "+ argbs[0].ToString() + ", g = "+ argbs[1].ToString() + ", b = "+ argbs[2].ToString() + "},";
                        break;
                    }
                }
                LuaName = name.ToUpper() + " = ";
                for (int i = factionnamestart; i < factionnamestart + 100; i++)
                {
                    string visible = gameconstants[i];
                    if (visible.Contains(LuaName))
                    {
                        gameconstants[i] = "        "+LuaName + "\"" + newStringID + "\",";
                        if (name == "Imperial_Proteus")
                        {
                            gameconstants[i] = "        IMPERIAL_PROTEUS = \"REPORT_THIS_PROTEUS_BUG_B\",";
                        }
                        break;
                    }
                }
            }
            SaveText(textfile);

            dest = globals.LocalMod + "\\XML\\Factions.xml";
            doc.Save(dest);
            dest = globals.LocalMod + "\\XML\\TradeRouteLines.xml";
            trades.Save(dest);

            File.WriteAllLines(ConvertMainPathToMod(constantsPath),gameconstants);
            MessageBox.Show("Changes saved");
            Thread.CurrentThread.CurrentCulture = globals.UIculture;
        }

        private void LaunchModButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(globals.ModFolder + "..\\StarWarsG.exe", LaunchOptionsIndicator.Text);
        }

        private void ModFilesButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(UpOneFolder(globals.LocalMod));
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (tabControl1.SelectedIndex)
            {
                case 1:
                    ReadFactionData();
                    break;
                case 2:
                    loadAffilData();
                    SpaceRadioButton.Checked = true;
                    break;
                //default:
                    // code block
                    //break;
            }
        }

        private void ColorButton_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog1 = new ColorDialog();
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (NumericUpDown control in FactionPanel.Controls.OfType<NumericUpDown>())
                {
                    if (((Control)sender).Tag == control.Tag)
                    {
                        if (control.Name.Contains("C"))
                        {
                            if (control.Name.Contains("R")) control.Value = colorDialog1.Color.R;
                            else if (control.Name.Contains("G")) control.Value = colorDialog1.Color.G;
                            else if (control.Name.Contains("B")) control.Value = colorDialog1.Color.B;
                            //else if (control.Name.Contains("A")) Can't set A from dialog
                        }
                    }
                }
            }
        }

        private void tColorButton_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog1 = new ColorDialog();
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (NumericUpDown control in FactionPanel.Controls.OfType<NumericUpDown>())
                {
                    if (((Control)sender).Tag == control.Tag)
                    {

                        if (control.Name.Contains("T"))
                        {
                            if (control.Name.Contains("R")) control.Value = colorDialog1.Color.R;
                            else if (control.Name.Contains("G")) control.Value = colorDialog1.Color.G;
                            else if (control.Name.Contains("B")) control.Value = colorDialog1.Color.B;
                        }
                    }
                }
            }
        }

        private void TransparentModButton_Click(object sender, EventArgs e)
        {
            foreach (NumericUpDown control in FactionPanel.Controls.OfType<NumericUpDown>())
            {
                if (((Control)sender).Tag == control.Tag)
                {
                    if (control.Name.Contains("T")) control.Value = 255;
                }
            }
        }

        private void NumericColor_Set(object sender, EventArgs e)
        {
            foreach (Button control in FactionPanel.Controls.OfType<Button>())
            {
                if (((Control)sender).Tag == control.Tag)
                {
                    if (control.Name.Contains("MC"))
                    {
                        if (((NumericUpDown)sender).Name.Contains("R"))
                        {
                            control.BackColor = Color.FromArgb(255,(int)((NumericUpDown)sender).Value, control.BackColor.G, control.BackColor.B);
                        }
                        else if (((NumericUpDown)sender).Name.Contains("G"))
                        {
                            control.BackColor = Color.FromArgb(255, control.BackColor.R, (int)((NumericUpDown)sender).Value, control.BackColor.B);
                        }
                        else if (((NumericUpDown)sender).Name.Contains("B"))
                        {
                            control.BackColor = Color.FromArgb(255, control.BackColor.R, control.BackColor.G, (int)((NumericUpDown)sender).Value);
                        }
                    }
                }
            }
        }

        private void NumerictColor_Set(object sender, EventArgs e)
        {
            foreach (Button control in FactionPanel.Controls.OfType<Button>())
            {
                if (((Control)sender).Tag == control.Tag)
                {
                    if (control.Name.Contains("TC"))
                    {
                        if (((NumericUpDown)sender).Name.Contains("R"))
                        {
                            control.BackColor = Color.FromArgb(255, (int)((NumericUpDown)sender).Value, control.BackColor.G, control.BackColor.B);
                        }
                        else if (((NumericUpDown)sender).Name.Contains("G"))
                        {
                            control.BackColor = Color.FromArgb(255, control.BackColor.R, (int)((NumericUpDown)sender).Value, control.BackColor.B);
                        }
                        else if (((NumericUpDown)sender).Name.Contains("B"))
                        {
                            control.BackColor = Color.FromArgb(255, control.BackColor.R, control.BackColor.G, (int)((NumericUpDown)sender).Value);
                        }
                    }
                }
            }
        }

        private void CopyColorButton_Click(object sender, EventArgs e)
        {
            decimal[] color = new decimal[4];

            foreach (NumericUpDown control in FactionPanel.Controls.OfType<NumericUpDown>())
            {
                if (((Control)sender).Tag == control.Tag)
                {
                    if (control.Name.Contains("C"))
                    {
                        if (control.Name.Contains("R")) color[0] = control.Value;
                        else if (control.Name.Contains("G")) color[1] = control.Value;
                        else if (control.Name.Contains("B")) color[2] = control.Value;
                        else if (control.Name.Contains("A")) color[3] = control.Value;
                    }
                }
            }

            foreach (NumericUpDown control in FactionPanel.Controls.OfType<NumericUpDown>())
            {
                if (((Control)sender).Tag == control.Tag)
                {
                    if (control.Name.Contains("T"))
                    {
                        if (control.Name.Contains("R")) control.Value = color[0];
                        else if (control.Name.Contains("G")) control.Value = color[1];
                        else if (control.Name.Contains("B")) control.Value = color[2];
                        else if (control.Name.Contains("A")) control.Value = color[3];
                    }
                }
            }
        }

        private void VersionComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Set mod specific settings like factory behaviors/specail factories here
            //May include legacy versions of mods for backend changes, custom config files if people need them. To be enabled instead of autohandled then
            //Once mod contentloader is removed, add 3.5 Legacy version
            if ((String)VersionComboBox.SelectedItem == "Thrawn's Revenge")
            {
                globals.ContentLoaderPath = "\\eawx-mod-icw";
                algorithm_data.level2crew = 100;
                algorithm_data.level2shield = 1700;
                algorithm_data.level3crew = -1;
                algorithm_data.level3shield = -1;
                algorithm_data.level4crew = -1;
                algorithm_data.level4shield = -1;
            }
            else if ((String)VersionComboBox.SelectedItem == "Fall of the Republic")
            {
                globals.ContentLoaderPath = "\\eawx-mod-fotr";
                algorithm_data.level2crew = 70;
                algorithm_data.level2shield = -1;
                algorithm_data.level3crew = 190;
                algorithm_data.level3shield = 3500;
                algorithm_data.level4crew = 480;
                algorithm_data.level4shield = -1;
            }
            else if ((String)VersionComboBox.SelectedItem == "Revan's Revenge")
            {
                globals.ContentLoaderPath = "\\eawx-mod-rev";
                algorithm_data.level2crew = 30;
                algorithm_data.level2shield = -1;
                algorithm_data.level3crew = 100;
                algorithm_data.level3shield = 1600;
                algorithm_data.level4crew = 300;
                algorithm_data.level4shield = -1;
            }

            SetAffilMods();
        }

        private void SetAffilMods()
        {
            if (VersionComboBox.SelectedItem.ToString().Contains("Thrawn's Revenge"))
            {
                for (int i = 0; i < globals.factions.Count; i++)
                {
                    faction faction = globals.factions[i];
                    if (faction.factionname == "Hapes_Consortium")
                    {
                        faction.parallelshipyards = true;
                    }
                    else if (faction.factionname == "Zsinj_Empire")
                    {
                        faction.altshipyard = "Pirate_Base";
                        faction.level42shipyard = "Rancor_Base";
                        faction.shipyards.Add("Rancor_Base");
                        faction.shipyards.Add("Pirate_Base");
                    }
                    else if (faction.factionname == "Corporate_Sector")
                    {
                        faction.altshipyard = "CSA_Ship_Market";
                        faction.shipyards.Add("CSA_Ship_Market");
                    }
                    else if (faction.factionname == "Hutt_Cartels")
                    {
                        faction.altshipyard = "Pirate_Base";
                        faction.shipyards.Add("Pirate_Base");
                    }
                    /*else if (faction.factionname == "Rebel")
                    {
                        faction.altshipyard = "Pirate_Base";
                    }*/

                    globals.factions[i] = faction;
                }
            }
            else if (VersionComboBox.SelectedItem.ToString().Contains("Fall of the Republic"))
            {
                for (int i = 0; i < globals.factions.Count; i++)
                {
                    faction faction = globals.factions[i];
                    if (faction.factionname == "Rebel")
                    {
                        faction.altshipyard = "Sabaoth_HQ";
                        faction.shipyards.Add("Sabaoth_HQ");
                        faction.specialfactories.Add("R_Ground_Light_Vehicle_Factory");
                        faction.specialfactorydefers.Add("R_Ground_Heavy_Vehicle_Factory");
                    }
                    else if (faction.factionname == "Empire")
                    {
                        faction.factories.Remove("Jedi_Ground_Barracks"); //Treat as a corporation rather than factory
                        faction.shipyards.Add("Republic_Naval_Command_Centre");
                    }
                    else if (faction.factionname == "Sector_Forces")
                    {
                        faction.factories.Remove("Jedi_Ground_Barracks");
                        faction.factories.Remove("E_Ground_Barracks"); //SF has both, but for my purposes I want to only use SF_
                    }
                    else if (faction.factionname == "Hutt_Cartels")
                    {
                        faction.altshipyard = "Pirate_Base";
                        faction.shipyards.Add("Pirate_Base");
                    }

                    globals.factions[i] = faction;
                }
            }
            else if (VersionComboBox.SelectedItem.ToString().Contains("Revan's Revenge"))
            {
                for (int i = 0; i < globals.factions.Count; i++)
                {
                    faction faction = globals.factions[i];
                    if (faction.factionname == "Rebel")
                    {
                        faction.level42shipyard = "Star_Forge_Relay";
                        faction.shipyards.Add("Star_Forge_Relay");
                        faction.specialfactories.Add("R_Ground_Light_Vehicle_Factory");
                        faction.specialfactories.Add("R_Ground_Advanced_Vehicle_Factory");
                        faction.specialfactorydefers.Add("R_Ground_Heavy_Vehicle_Factory");
                        faction.specialfactorydefers.Add("R_Ground_Heavy_Vehicle_Factory");
                    }
                    /*else if (faction.factionname == "Hutt_Cartels")
                    {
                        faction.altshipyard = "Pirate_Base";
                    }*/

                    globals.factions[i] = faction;
                }
            }

            globals.allfactories = new List<string>();
            foreach (faction faction in globals.factions)
            {
                foreach (string factory in faction.factories)
                {
                    if (!globals.allfactories.Contains(factory))
                    {
                        globals.allfactories.Add(factory);
                    }
                }
                foreach (string shipyard in faction.shipyards)
                {
                    if (!globals.allfactories.Contains(shipyard))
                    {
                        globals.allfactories.Add(shipyard);
                    }
                }
                if (faction.level42shipyard != "" && !globals.allfactories.Contains(faction.level42shipyard))
                {
                    globals.allfactories.Add(faction.level42shipyard);
                }
                if (faction.altshipyard != "" && !globals.allfactories.Contains(faction.altshipyard))
                {
                    globals.allfactories.Add(faction.altshipyard);
                }
            }
        }

        private void FilterComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SpaceRadioButton.Checked)
            {
                foreach (ListBox control in UnitAffilPanel.Controls.OfType<ListBox>())
                {
                    control.SelectedItems.Clear();
                    for (int i = FilterComboBox.SelectedIndex; i < 4; i++)
                    {
                        control.SelectedItems.Add(control.Items[i]);
                    }
                }
            }
        }
    }
}
