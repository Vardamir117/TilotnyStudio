using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using static Holocron.UnitFilter;
using static Holocron.UnitSort;
using static SharedFunctions;

/*
 * 
 * todo - subunits on ground units tells you which companies it is in?
 * 
 * todo - hash setup for projectiles and hardpoints? It did help a lot in untemplating
 * 
 * 
 * armor matrix is broken in vanilla
 * 
 * 
 * use user facing faction names on structure listboxes - probably do need to pull holo_faction into entities
 * 
 * 
 * 
 * 
 * 
 * todo - log mode might need to add pulse interval depending on how sheets do it
 * log mode should ideally adjust reload nonproportionally
 * 
 * try to keep general for all EaW mods. arse unit files from gameconstants instead of assuming path?
 * 
 * Gunship untangle problem - to unit to companies pre-categorization?
keep history updated whenever a tab or subtab is implemented
there might be a bit of oddness in the history tracking when factions share a name
Tilot - up two levels, does swfoc.exe exist? If yes enable dropdown

lookup for name lists? Just listbox of file names with count as a sort option

Venator BTS should explain the turrets

lexical filter for fighter, single/other squadron, half, third, double, triple

auto parse sfx for bts comments? go through file, saving last comment that doesn't have asterisks, when finding a unit SFX stop. Maybe keep a minimum distance
don't save on inital parse, reconstruct from variant chain on unit selection?
Audio lookup page?
*/


namespace Holocron
{
    public partial class Holocron : Form
    {
        public static class globals
        {
            public static string localmodpath;
            public static string steammodpath;

            public static List<holo_faction> factions = new List<holo_faction>();
            public static string dpsformat = "0.###";

            public static UnitSortClass UnitSortConfig = new UnitSortClass();
            public static UnitFilterClass UnitFilterConfig = new UnitFilterClass();
            public static contrast_values ContrastValues = new contrast_values();

            public static List<shipname> shipnames = new List<shipname>();

            //Assume map picturebox is a square of odd pixel count, the same size between GC and planet pages
            public static int origin;
            public static float scale;

            public static bool allplanets = false; //todo make sure this is off for releases
        }

        public static class nav
        {
            public static List<int> maintab = new List<int>();
            public static List<int> secondary = new List<int>();
            public static List<string> item = new List<string>();

            public static int navindex = -1;
            public static bool suppresshistory = false;
        }

        public struct weighted_type_list
        {
            public List<string> typeNames;
            public List<float> weights;
        }

        public struct contrast_values
        {
            public List<string> enemyTypes;
            public List<weighted_type_list> friendlyTypeLists;
            public List<List<string>> friendlyTypeNames;
            public List<List<float>> friendlyTypeWeights;
            public Dictionary<string, float> typeScale;
        }

        public struct autoresolve_entry
        {
            public unit source;
            public int quantity;

            public override string ToString()
            {
                return quantity.ToString() + "x " + source.username + " [" + source.unitname + "]";
            }
        }

        public static entities entities = new entities();

        private List<autoresolve_entry> autoResolveSideA = new List<autoresolve_entry>();
        private List<autoresolve_entry> autoResolveSideB = new List<autoresolve_entry>();
        private int autoResolveSideAOwner = 0;
        private int autoResolveSideBOwner = 1;

        public Holocron()
        {
            InitializeComponent();
        }

        private void Holocron_Load(object sender, EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                string[] split = args[1].Split(';');
                for (int i=0; i< split.Length; i++) { //First arg is exe, second is semicolon delimited mod args
                    entities.modpaths.Add(split[i]);
                }
            }
            else
            {
                string exePath = AppContext.BaseDirectory;
                string localmodtest = UpOneFolder(UpOneFolder(UpOneFolder(UpOneFolder(exePath))));
                string modfolder = UpOneFolder(exePath);
                if (File.Exists(localmodtest + "\\StarWarsG.exe"))
                {
                    globals.localmodpath = UpOneFolder(UpOneFolder(modfolder));
                    globals.steammodpath = UpOneFolder(UpOneFolder(UpOneFolder(localmodtest))) + "\\workshop\\content\\32470";
                    if (Directory.Exists(modfolder + "\\..\\TR") && Directory.Exists(modfolder + "\\..\\FotR") && Directory.Exists(modfolder + "\\..\\CoreSaga") && Directory.Exists(modfolder + "\\..\\Rev"))
                    {
                        DevChoice devChoice = new DevChoice();
                        devChoice.basepath = UpOneFolder(modfolder);
                        devChoice.ShowDialog();

                        entities.modpaths = devChoice.args;
                        globals.allplanets = devChoice.allplanet;
                    }
                    else entities.modpaths.Add(modfolder);
                }
                else
                {
                    localmodtest = UpOneFolder(UpOneFolder(localmodtest)) + "\\common\\Star Wars Empire at War\\corruption";
                    if (File.Exists(localmodtest + "\\StarWarsG.exe"))
                    {
                        entities.modpaths.Add(modfolder);
                        globals.steammodpath = UpOneFolder(UpOneFolder(modfolder));
                        globals.localmodpath = localmodtest + "\\Mods";
                    }
                    else
                    {//Run on real mod data from the debugger
                        if (File.Exists("debugpaths.cfg"))
                        {
                            string[] lines = File.ReadAllLines("debugpaths.cfg");
                            globals.localmodpath = lines[0];
                            globals.steammodpath = lines[1];

                            for (int i = 2; i < lines.Length; i++) entities.modpaths.Add(lines[i]);
                        }
                        //globals.localmodpath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Star Wars Empire at War\\corruption\\Mods";
                        //globals.steammodpath = "C:\\Program Files (x86)\\Steam\\steamapps\\workshop\\content\\32470";
                        //1125571106 1976399102 3417277973
                        //Workshop
                        // entities.modpaths.Add("C:\\Program Files (x86)\\Steam\\steamapps\\workshop\\content\\32470\\3417277973\\Data");

                        //Dev build
                        
                        //entities.modpaths.Add("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Star Wars Empire at War\\corruption\\Mods\\Imperial_Civil_War\\Rev\\Data");
                        //entities.modpaths.Add("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Star Wars Empire at War\\corruption\\Mods\\Imperial_Civil_War\\TR\\Data");
                        //entities.modpaths.Add("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Star Wars Empire at War\\corruption\\Mods\\Imperial_Civil_War\\FotR\\Data");
                        //entities.modpaths.Add("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Star Wars Empire at War\\corruption\\Mods\\Imperial_Civil_War\\CoreSaga\\Data");
                        //entities.modpaths.Add("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Star Wars Empire at War\\corruption\\Mods\\Imperial_Civil_War\\Data");
                        

                        //Vanillua
                        //entities.modpaths.Add("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Star Wars Empire at War\\corruption\\Data");
                        //entities.modpaths.Add("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Star Wars Empire at War\\GameData\\Data");
                    }
                }
            }
            load_mods();
            globals.UnitSortConfig.SortType = UnitSortTypes.Name;
            globals.UnitSortConfig.denomtype = "Absolute Value";
            UnitFilter INeedYourFunctions = new UnitFilter();
            globals.UnitFilterConfig = INeedYourFunctions.newFilter();

            //todo stop right aligned? controls from resizing in stupid ways when the desn tab is reopened
            MapsInPlanetsListbox.Size = new Size(336, 1005);
            MapSearchBox.Size = new Size(336, 22);
        }
        
        private void load_mods()
        {
            Loading loadscreen = new Loading();
            loadscreen.Show();
            globals.factions.Clear();
            FactionListBox.Items.Clear();
            System.Threading.Thread t = new System.Threading.Thread(() => LoadThread(loadscreen));
            t.Start();
            MissionListBox.Items.Clear();
            SpawnListBox.Items.Clear();
            StandardFListBox.Items.Clear();
            RandomFListBox.Items.Clear();
            globals.shipnames.Clear();
        }

        private void LoadThread(Loading loadscreen)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
            loadscreen.ChangeText("Reading text file");
            entities.Text = DatParser.ReadDat(getModFile("Text\\MasterTextFile_ENGLISH.dat"), ',', 0);
            Random rnd = new Random();
            for(int i = 0; i < 1000; i++) //Don't search too long
            {
                string quote = entities.Text[rnd.Next(0, entities.Text.Count-1)].entry;
                if (quote.Length > 149)
                {//any filtering based on id or entry goes in this if
                    loadscreen.SetQuote(quote);
                    break;
                }
            }

            parsemodid(getModFile("XML\\Mod_Id.xml"), entities);

            loadscreen.ChangeText("Reading MEG files");
            parseMEGs(entities);

            loadscreen.ChangeText("Reading icon file");
            entities.IconData = DatParser.ReadMTD(entities);
            try
            {
                entities.MTmaster = (Bitmap)Image.FromFile(getModFile("Art\\Textures\\MT_CommandBar.tga"));
            }
            catch
            {
                entities.MTmaster = new Bitmap(50, 50);
            };
            entities.readerrors = "";

            loadscreen.ChangeText("Parsing faction data");
            XmlDocument doc = readModXmlOrMeg("XML\\Factions.xml", entities);
            XmlNode root = doc.DocumentElement;

            var factions = root.SelectNodes("descendant::Faction");
            foreach (XmlElement faction in factions)
            {
                holo_faction newfaction = new holo_faction();
                string name = faction.GetAttribute("Name");
                string id = faction.SelectSingleNode("descendant::Text_ID").InnerText.Trim();
                string color = faction.SelectSingleNode("descendant::Color").InnerText.Trim();
                string taccolor = faction.SelectSingleNode("descendant::No_Colorization_Color").InnerText.Trim();

                int[] col = ReadXMLCSV(color);
                int[] tcol = ReadXMLCSV(taccolor);

                newfaction.codename = name;
                newfaction.textname = Find_Text_Entry(id);
                globals.factions.Add(newfaction);
                FactionListBox.BeginInvoke(new Action(() => FactionListBox.Items.Add(newfaction.textname)));
            }

            loadscreen.ChangeText("Parsing projectile data");
            List<string> listfiles = getModFiles("XML\\Projectiles", "*.xml");
            parseProjectiles(entities);

            loadscreen.ChangeText("Parsing hardpoint data");
            parseHardpoints(entities, entities.Text);

            entities.spaceUnits = new List<unit>();
            entities.groundCompanies = new List<unit>();
            entities.groundUnits = new List<unit>();
            entities.structures = new List<unit>();
            entities.spaceHeroes = new List<unit>();
            entities.heroCompanies = new List<unit>();
            entities.groundHeroes = new List<unit>();

            loadscreen.ChangeText("Parsing constants");
            parseCategories(entities);
            parseFlags(entities);
            parseGameConstants(entities);

            loadscreen.ChangeText("Parsing object data");
            parseObjects(entities);
            loadscreen.ChangeText("Resolving object dependencies");
            untemplate(entities);
            loadscreen.ChangeText("Categorizing object types");
            categorizeObjects(entities);
            /*listfiles = getModFiles("XML\\Units", "*.xml");
            parseUnitFolder(listfiles, entities);
            loadscreen.ChangeText("Parsing hero data");
            parseHeroFolder(entities);
            loadscreen.ChangeText("Parsing structure data");
            parseStructureFolder(entities);*/
            /*untemplate(entities.structures, entities.structures, entities.Text);
            loadscreen.ChangeText("Resolving unit dependencies");
            untemplate(entities.groundCompanies, entities.groundCompanies, entities.Text);
            untemplate(entities.spaceUnits, entities.spaceUnits, entities.Text);
            untemplate(entities.groundUnits, entities.groundUnits, entities.Text);
            loadscreen.ChangeText("Resolving hero dependencies");
            untemplate(entities.heroCompanies, entities.groundCompanies, entities.Text);
            untemplate(entities.spaceHeroes, entities.spaceUnits, entities.Text);
            untemplate(entities.groundHeroes, entities.groundUnits, entities.Text);*/
            loadscreen.ChangeText("Assembing company unit lists");
            entities.groundCompanies = unitToCompanyData(entities.groundCompanies, entities.groundUnits, entities.containers);
            entities.fighters = unitToCompanyData(entities.fighters, entities.fighters, entities.containers);
            entities.spaceUnits = unitToCompanyData(entities.spaceUnits, entities.spaceUnits, entities.containers); //Gunships
            loadscreen.ChangeText("Assembing hero companies");
            entities.heroCompanies = unitToCompanyData(entities.heroCompanies, entities.groundHeroes, entities.containers);
            //entities.spaceHeroes = unitToCompanyData(entities.spaceHeroes, entities.spaceUnits, entities.containers); //Todo: fix for the rare gunship hero
            //entities.spaceHeroes = unitToCompanyData(entities.spaceHeroes, entities.spaceUnits, entities.containers, true);
            loadscreen.ChangeText("Parsing planet data");
            parsePlanets(entities, globals.allplanets);
            loadscreen.ChangeText("Parsing Galactic Conquests");
            parseGCs(entities);

            loadscreen.ChangeText("Reading AI contrast values");
            globals.ContrastValues = ReadContrastValues();
            //Assume picturebox is a square of odd pixel count
            globals.origin = (PlanetPictureBox.Width - 1) / 2;
            globals.scale = globals.origin/(entities.PlanetBounds + 25);
            entities.hardpointhashes.Clear(); //No reason to keep these around after parsing
            entities.projectilehashes.Clear();

            loadscreen.CloseLoadScreen();
        }

        private contrast_values ReadContrastValues()
        {
            contrast_values contrastValues = new contrast_values();
            contrastValues.enemyTypes = new List<string>();
            contrastValues.friendlyTypeLists = new List<weighted_type_list>();
            contrastValues.friendlyTypeNames = new List<List<string>>();
            contrastValues.friendlyTypeWeights = new List<List<float>>();
            contrastValues.typeScale = new Dictionary<string, float>();

            string path = getModFile("Scripts\\Library\\PGAICommands.lua");
            if (!File.Exists(path)) return contrastValues;

            string[] lines = File.ReadAllLines(path);
            bool in_contrast_function = false;
            string current_enemy = "";
            List<string> current_names = null;
            List<float> current_weights = null;

            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (!in_contrast_function)
                {
                    if (line.StartsWith("function Set_Contrast_Values()")) in_contrast_function = true;
                    continue;
                }

                if (line == "end") break;

                if (line.StartsWith("EnemyContrastTypes[_e_cnt]"))
                {
                    current_enemy = LuaParser.ExtractLuaQuotedValue(line);
                }
                else if (line.StartsWith("FriendlyContrastTypeNames"))
                {
                    current_names = LuaParser.ParseLuaStringArray(line);
                }
                else if (line.StartsWith("FriendlyContrastWeights"))
                {
                    current_weights = LuaParser.ParseLuaFloatArray(line);
                }
                else if (line.StartsWith("ContrastTypeScale["))
                {
                    string scale_type = LuaParser.ExtractLuaBracketQuotedKey(line);
                    float scale;
                    if (scale_type != "" && float.TryParse(LuaParser.ExtractLuaAssignedValue(line), NumberStyles.Float, CultureInfo.InvariantCulture, out scale))
                    {
                        contrastValues.typeScale[scale_type] = scale;
                    }
                }

                if (current_enemy != "" && current_names != null && current_weights != null)
                {
                    weighted_type_list list = new weighted_type_list();
                    list.typeNames = current_names;
                    list.weights = current_weights;

                    contrastValues.enemyTypes.Add(current_enemy);
                    contrastValues.friendlyTypeLists.Add(list);
                    contrastValues.friendlyTypeNames.Add(new List<string>(current_names));
                    contrastValues.friendlyTypeWeights.Add(new List<float>(current_weights));

                    current_enemy = "";
                    current_names = null;
                    current_weights = null;
                }
            }

            return contrastValues;
        }

        private void submodsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SubmodSetup subs = new SubmodSetup();
            subs.localmodpath = globals.localmodpath;
            subs.steammodpath = globals.steammodpath;
            subs.ShowDialog();

            if (subs.reload)
            {
                entities.modpaths = subs.Args;
                load_mods();
                MainTab.SelectedIndex = 0;
            }
        }

        enum historymaintabs
        {
            faction,
            conquest,
            planet,
            unit,
            govs,
            lookups,
            galaxy,
            autoresolve,
        }

        private void insert_history(int main, int secondary, string entity, bool go_to = false)
        {
            if (nav.suppresshistory) return;
            if(nav.navindex < nav.maintab.Count-1)
            {
                for(int i = nav.maintab.Count - 1; i > nav.navindex; i--)
                {
                    nav.maintab.RemoveAt(i);
                    nav.secondary.RemoveAt(i);
                    nav.item.RemoveAt(i);
                }
            }
            nav.maintab.Add(main);
            nav.secondary.Add(secondary);
            nav.item.Add(entity);
            nav.navindex++;
            if (go_to) goto_history(nav.navindex);
        }

        private void goto_history(int historyid)
        {
            nav.suppresshistory = true;

            historymaintabs main = (historymaintabs)nav.maintab[historyid];
            int secondary = nav.secondary[historyid];
            string item = nav.item[historyid];

            MainTab.SelectedIndex = (int)main;
            bool found = false;
            switch (main)
            {
                case historymaintabs.faction:
                    for(int i = 0; i < globals.factions.Count; i++)
                    {
                        if (item == globals.factions[i].codename)
                        {
                            FactionListBox.SelectedItem = FactionListBox.Items[i];
                        }
                    }
                    break;
                case historymaintabs.unit:
                    switch (secondary)
                    {
                        case 0:
                            SpaceRadioButton.Checked = true;
                            break;
                        case 1:
                            GroundRadioButton.Checked = true;
                            break;
                        case 2:
                            UnitRadioButton.Checked = true;
                            break;
                        case 3:
                            FighterRadioButton.Checked = true;
                            break;
                        case 4:
                            SpaceHeroRadioButton.Checked = true;
                            break;
                        case 5:
                            HeroCompaniesRadioButton.Checked = true;
                            break;
                        case 6:
                            GroundHeroRadioButton.Checked = true;
                            break;
                        case 7:
                            StructureRadioButton.Checked = true;
                            break;
                        case 8:
                            SpaceStructureRadioButton.Checked = true;
                            break;
                    }
                    for (int i = 0; i < UnitListBox.Items.Count; i++)
                    {
                        if (item == ((unit)UnitListBox.Items[i]).unitname)
                        {
                            UnitListBox.SelectedItem = UnitListBox.Items[i];
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        UnitSearchTextBox.Text = "";
                        UnitFilter INeedYourFunctions = new UnitFilter();
                        globals.UnitFilterConfig = INeedYourFunctions.newFilter();
                        UnitFIlterTypeLabel.Text = INeedYourFunctions.filterDocumentation;
                        populateUnitListbox();
                        for (int i = 0; i < UnitListBox.Items.Count; i++)
                        {
                            if (item == ((unit)UnitListBox.Items[i]).unitname)
                            {
                                UnitListBox.SelectedItem = UnitListBox.Items[i];
                                break;
                            }
                        }
                    }
                    break;
                case historymaintabs.planet:
                    for (int i = 0; i < PlanetListBox.Items.Count; i++)
                    {
                        if (item == ((planet)PlanetListBox.Items[i]).codename)
                        {
                            PlanetListBox.SelectedItem = PlanetListBox.Items[i];
                            found = true;
                            break;
                        }
                    }
                    break;
                default:
                    // code block
                    break;
            }

            nav.suppresshistory = false;
        }

        private void backToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(nav.navindex > 0)
            {
                nav.navindex--;
                goto_history(nav.navindex);
            }
        }

        private void forwardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (nav.navindex < nav.maintab.Count - 1)
            {
                nav.navindex++;
                goto_history(nav.navindex);
            }
        }

        private void MainTab_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (MainTab.SelectedIndex)
            {
                case (int)historymaintabs.faction:
                    break;
                case (int)historymaintabs.unit:
                    if (UnitListBox.SelectedItems.Count == 0)
                    {
                        ResetAbilitySelection();
                        SpaceRadioButton.Checked = true;
                        populateUnitListbox();
                    }
                    break;
                case (int)historymaintabs.planet:
                    if (PlanetListBox.SelectedItems.Count == 0) populatePlanetListbox();
                    break;
                case (int)historymaintabs.lookups:
                    FillMatrixLookup();
                    break;
                case (int)historymaintabs.autoresolve:
                    FillAutoResolveUnitSelection();
                    FillAutoResolveFactionSelection();
                    AutoResolveRefreshSideListboxes();
                    break;
                default:
                    // code block
                    break;
            }

        }

        private void LookupTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (LookupTabControl.SelectedIndex) //todo add to history someday. use lookups*100 + this index
            {
                //case 0: The matrix doesn't need filling from here, actually
                    //break;
                case 1:
                    //todo - more sorting and filtering? filter by unit and hero names only? Filter out dropship and/or cadet?
                    //How are there two acclamator_I instances for Acclamator? Because it's in the list twice. Probably leave that in
                    //Do ground heroes? There are so few with names...
                    if (globals.shipnames.Count == 0)
                    {
                        List<string> Checked = new List<string>();
                        XmlDocument consts = readModXmlOrMeg("XML\\GameConstants.xml", entities);
                        XmlNodeList listsets = consts.DocumentElement.SelectNodes("descendant::ShipNameTextFiles");
                        foreach (XmlNode listset in listsets)
                        {
                            string[] types = ReadWhiteSpaceAsCommas(listset.InnerText);
                            if (types.Length > 1)
                            {
                                for (int i = 1; i < types.Length; i += 2)
                                {
                                    string file = types[i];
                                    if (!Checked.Contains(file))
                                    {
                                        Checked.Add(file);
                                        string namefile = getModFile(RemoveTopLevelFolder(file));
                                        if (File.Exists(namefile))
                                        {
                                            string[] monikers = File.ReadAllLines(namefile);
                                            file = LastFolderOrFile(file).Replace(".txt", "");

                                            foreach (string moniker in monikers)
                                            {
                                                if(moniker != "")
                                                {
                                                    bool notfound = true;
                                                    for (int j = 0; j < globals.shipnames.Count; j++)
                                                    {
                                                        shipname name = globals.shipnames[j];
                                                        if (name.name == moniker)
                                                        {
                                                            name.units.Add(file);
                                                            globals.shipnames[j] = name;
                                                            notfound = false;
                                                            break;
                                                        }
                                                    }
                                                    if (notfound)
                                                    {
                                                        shipname nuevo = new shipname();
                                                        nuevo.name = moniker;
                                                        nuevo.units = new List<string>();
                                                        nuevo.units.Add(file);
                                                        nuevo.heroes = new List<string>();
                                                        nuevo.unused = new List<string>();
                                                        globals.shipnames.Add(nuevo);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        Checked = new List<string>();
                        foreach (unit hero in entities.spaceHeroes)
                        {
                            if (hero.tooltip.Contains("TEXT_TOOLTIP_COMMAND_")) //Not sure if this is in fact an efficiency gain
                            {//Todo benchmark this and sorting by name. Add loading screen?
                                foreach (string entry in SplitXMLWhitespaceList(hero.tooltip))
                                {
                                    string namestring = "";
                                    if (entry.Contains("TEXT_TOOLTIP_COMMAND_"))
                                    {
                                        namestring = Find_Text_Entry(entry);
                                        if (namestring.Contains(", "))
                                        {//This pattern should be consistent among commands. Close enough, probably
                                            string heroname = Find_Text_Entry(hero.username) + " (" + namestring.Substring(0, namestring.LastIndexOf(",")) + ")";
                                            namestring = namestring.Substring(namestring.LastIndexOf(",") + 2, namestring.Length - namestring.LastIndexOf(",") - 2);
                                            if (!Checked.Contains(heroname))
                                            {
                                                Checked.Add(heroname);

                                                bool notfound = true;
                                                for (int j = 0; j < globals.shipnames.Count; j++)
                                                {
                                                    shipname name = globals.shipnames[j];
                                                    if (name.name == namestring)
                                                    {
                                                        name.heroes.Add(heroname);
                                                        globals.shipnames[j] = name;
                                                        notfound = false;
                                                        break;
                                                    }
                                                }
                                                if (notfound)
                                                {
                                                    shipname nuevo = new shipname();
                                                    nuevo.name = namestring;
                                                    nuevo.units = new List<string>();
                                                    nuevo.heroes = new List<string>();
                                                    nuevo.heroes.Add(heroname);
                                                    nuevo.unused = new List<string>();
                                                    globals.shipnames.Add(nuevo);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        Checked = new List<string>();
                        List<string> unuseds = getModFiles("..\\Unused\\Unused ShipNames", "*.txt");
                        foreach (string unused in unuseds)
                        {
                            string file = unused;
                            if (!Checked.Contains(file))
                            {
                                Checked.Add(file);
                                string[] monikers = File.ReadAllLines(file);
                                file = LastFolderOrFile(file).Replace(".txt", "");

                                foreach (string moniker in monikers)
                                {
                                    if(moniker != "")
                                    {
                                        bool notfound = true;
                                        for (int j = 0; j < globals.shipnames.Count; j++)
                                        {
                                            shipname name = globals.shipnames[j];
                                            if (name.name == moniker)
                                            {
                                                name.unused.Add(file);
                                                globals.shipnames[j] = name;
                                                notfound = false;
                                                break;
                                            }
                                        }
                                        if (notfound)
                                        {
                                            shipname nuevo = new shipname();
                                            nuevo.name = moniker;
                                            nuevo.units = new List<string>();
                                            nuevo.heroes = new List<string>();
                                            nuevo.unused = new List<string>();
                                            nuevo.unused.Add(file);
                                            globals.shipnames.Add(nuevo);
                                        }
                                    }
                                }
                            }
                        }

                    }
                    FillShipnameListbox();
                    break;
                case 2:
                    if (MissionListBox.Items.Count == 0) //todo ensure other histories have such conditions cleared by load_mods
                    {
                        List<string> missionfiles = getModFiles("Scripts\\Library\\eawx-plugins\\intervention-missions\\rewards", "*.lua");
                        foreach (string file in missionfiles)
                        {
                            MissionListBox.Items.Add(LastFolderOrFile(file).Replace("RewardTables_","").ToUpper().Replace(".LUA",""));
                        }
                    }
                    break;
                case 3:
                    if (SpawnListBox.Items.Count == 0) //todo may have to check a new location after modcontent loader dies
                    {
                        List<string> missionfiles = getModFiles("Scripts\\Library\\spawn-sets", "*.lua");
                        foreach (string file in missionfiles)
                        {
                            SpawnListBox.Items.Add(LastFolderOrFile(file).ToUpper().Replace(".LUA", ""));
                        }
                    }
                    break;
                case 6:
                    if (StandardFListBox.Items.Count == 0)
                    {
                        List<string> missionfiles = getModFiles("Scripts\\Library\\standard-fighters", "*.lua");
                        foreach (string file in missionfiles)
                        {
                            StandardFListBox.Items.Add(LastFolderOrFile(file).ToUpper().Replace(".LUA", ""));
                        }
                    }
                    break;
                case 7:
                    if (RandomFListBox.Items.Count == 0)
                    {
                        List<string> missionfiles = getModFiles("Scripts\\Library\\random-fighters", "*.lua");
                        foreach (string file in missionfiles)
                        {
                            RandomFListBox.Items.Add(LastFolderOrFile(file).ToUpper().Replace(".LUA", ""));
                        }
                    }
                    break;
                default:
                    // code block
                    break;
            }
            
        }

        private void MatrixSpaceRB_CheckedChanged(object sender, EventArgs e)
        {
            FillMatrixLookup();
        }

        private void MatrixGroundRB_CheckedChanged(object sender, EventArgs e)
        {
            FillMatrixLookup();
        }

        private List<unit> AutoResolveGetUnitSource()
        {
            List<unit> source = new List<unit>();
            foreach (unit item in entities.spaceUnits) source.Add(item);
            foreach (unit item in entities.groundCompanies) source.Add(item);
            foreach (unit item in entities.groundUnits) source.Add(item);
            foreach (unit item in entities.structures) source.Add(item);
            foreach (unit item in entities.spaceHeroes) source.Add(item);
            foreach (unit item in entities.heroCompanies) source.Add(item);
            foreach (unit item in entities.groundHeroes) source.Add(item);
            return source;
        }

        private List<unit> AutoResolveGetBattleTypeUnitSource()
        {
            List<unit> source = new List<unit>();

            if (AutoResolveBattleTypeComboBox.SelectedIndex == 0)
            {
                foreach (unit item in entities.spaceUnits) source.Add(item);
                foreach (unit item in entities.spaceHeroes) source.Add(item);
            }
            else if (AutoResolveBattleTypeComboBox.SelectedIndex == 1)
            {
                foreach (unit item in entities.groundCompanies) source.Add(item);
                foreach (unit item in entities.groundUnits) source.Add(item);
                foreach (unit item in entities.structures) source.Add(item);
                foreach (unit item in entities.heroCompanies) source.Add(item);
                foreach (unit item in entities.groundHeroes) source.Add(item);
            }
            else
            {
                source = AutoResolveGetUnitSource();
            }

            return source;
        }

        private void FillAutoResolveUnitSelection()
        {
            unit priorSelection = new unit();
            bool hadSelection = AutoResolveUnitComboBox.SelectedItem != null;
            if (hadSelection) priorSelection = (unit)AutoResolveUnitComboBox.SelectedItem;

            AutoResolveUnitComboBox.Items.Clear();
            foreach (unit item in AutoResolveGetBattleTypeUnitSource().OrderBy(x => x.username))
            {
                if (AutoResolveUnitHasSufficientInformation(item)) AutoResolveUnitComboBox.Items.Add(item);
            }

            if (AutoResolveUnitComboBox.Items.Count == 0) return;

            if (hadSelection)
            {
                int match = -1;
                for (int i = 0; i < AutoResolveUnitComboBox.Items.Count; i++)
                {
                    unit compare = (unit)AutoResolveUnitComboBox.Items[i];
                    if (string.Equals(compare.unitname, priorSelection.unitname, StringComparison.OrdinalIgnoreCase))
                    {
                        match = i;
                        break;
                    }
                }
                AutoResolveUnitComboBox.SelectedIndex = match >= 0 ? match : 0;
            }
            else AutoResolveUnitComboBox.SelectedIndex = 0;
        }

        private void AutoResolveRefreshSideListboxes()
        {
            AutoResolveSideAListBox.Items.Clear();
            AutoResolveSideBListBox.Items.Clear();

            foreach (autoresolve_entry entry in autoResolveSideA) AutoResolveSideAListBox.Items.Add(entry);
            foreach (autoresolve_entry entry in autoResolveSideB) AutoResolveSideBListBox.Items.Add(entry);

            AutoResolveUpdatePowerDisplay();
        }

        private float AutoResolveGetContrastWeight(string enemyType, string friendlyType)
        {
            if (globals.ContrastValues.enemyTypes == null || globals.ContrastValues.friendlyTypeLists == null) return 1f;

            int entries = Math.Min(globals.ContrastValues.enemyTypes.Count, globals.ContrastValues.friendlyTypeLists.Count);
            for (int i = 0; i < entries; i++)
            {
                if (!string.Equals(globals.ContrastValues.enemyTypes[i], enemyType, StringComparison.OrdinalIgnoreCase)) continue;

                weighted_type_list weighted = globals.ContrastValues.friendlyTypeLists[i];
                if (weighted.typeNames == null || weighted.weights == null) return 1f;

                int pairCount = Math.Min(weighted.typeNames.Count, weighted.weights.Count);
                for (int j = 0; j < pairCount; j++)
                {
                    if (string.Equals(weighted.typeNames[j], friendlyType, StringComparison.OrdinalIgnoreCase)) return weighted.weights[j];
                }
                return 1f;
            }

            return 1f;
        }

        private float AutoResolveGetContrastScale(string enemyType)
        {
            if (globals.ContrastValues.typeScale == null) return 1f;

            float value;
            if (globals.ContrastValues.typeScale.TryGetValue(enemyType, out value)) return value;

            foreach (KeyValuePair<string, float> kv in globals.ContrastValues.typeScale)
            {
                if (string.Equals(kv.Key, enemyType, StringComparison.OrdinalIgnoreCase)) return kv.Value;
            }

            return 1f;
        }

        private Dictionary<string, float> AutoResolveBuildCategoryPowerMap(List<autoresolve_entry> side)
        {
            Dictionary<string, float> values = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

            foreach (autoresolve_entry entry in side)
            {
                float power = Math.Max(0f, entry.source.cp) * Math.Max(1, entry.quantity);
                if (power <= 0f || entry.source.categories == null) continue;

                HashSet<string> unitCats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string category in entry.source.categories)
                {
                    if (string.IsNullOrWhiteSpace(category) || !unitCats.Add(category)) continue;

                    float extant;
                    if (values.TryGetValue(category, out extant)) values[category] = extant + power;
                    else values[category] = power;
                }
            }

            return values;
        }

        private float AutoResolveGetRawPower(List<autoresolve_entry> side)
        {
            float total = 0f;
            foreach (autoresolve_entry entry in side)
            {
                total += Math.Max(0f, entry.source.cp) * Math.Max(1, entry.quantity);
            }
            return total;
        }

        private string AutoResolveBuildSidePowerDetails(string sideName, List<autoresolve_entry> ownEntries, List<autoresolve_entry> opposingEntries)
        {
            StringBuilder sb = new StringBuilder();
            Dictionary<string, float> ownCategories = AutoResolveBuildCategoryPowerMap(ownEntries);
            Dictionary<string, float> opposingCategories = AutoResolveBuildCategoryPowerMap(opposingEntries);

            sb.AppendLine(sideName + " raw combat power: " + AutoResolveGetRawPower(ownEntries).ToString("0.###", CultureInfo.InvariantCulture));

            if (ownCategories.Count == 0)
            {
                sb.AppendLine("  No contrast categories available for this side.");
                return sb.ToString().TrimEnd();
            }

            sb.AppendLine("  Contrast categories present:");
            foreach (KeyValuePair<string, float> own in ownCategories.OrderByDescending(x => x.Value))
            {
                sb.AppendLine("    " + own.Key + ": " + own.Value.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (opposingCategories.Count == 0)
            {
                sb.AppendLine("  No opposing categories available to evaluate multipliers.");
                return sb.ToString().TrimEnd();
            }

            sb.AppendLine("  Effective power options (best multiplier per friendly category):");
            float effectiveTotal = 0f;
            foreach (KeyValuePair<string, float> own in ownCategories.OrderByDescending(x => x.Value))
            {
                float bestWeight = 1f;
                float bestScale = 1f;
                float bestMultiplier = 1f;
                string bestEnemyType = "(default)";

                foreach (KeyValuePair<string, float> enemy in opposingCategories)
                {
                    float weight = AutoResolveGetContrastWeight(enemy.Key, own.Key);
                    float scale = AutoResolveGetContrastScale(enemy.Key);
                    float multiplier = weight * scale;
                    if (multiplier > bestMultiplier)
                    {
                        bestMultiplier = multiplier;
                        bestWeight = weight;
                        bestScale = scale;
                        bestEnemyType = enemy.Key;
                    }
                }

                float weightedPower = own.Value * bestMultiplier;
                if (weightedPower > effectiveTotal) effectiveTotal = weightedPower;
                sb.AppendLine("    " + own.Key + " base " + own.Value.ToString("0.###", CultureInfo.InvariantCulture) +
                    " | best vs " + bestEnemyType +
                    " (weight " + bestWeight.ToString("0.###", CultureInfo.InvariantCulture) +
                    " x scale " + bestScale.ToString("0.###", CultureInfo.InvariantCulture) +
                    " = " + bestMultiplier.ToString("0.###", CultureInfo.InvariantCulture) +
                    ") => " + weightedPower.ToString("0.###", CultureInfo.InvariantCulture));
            }

            sb.AppendLine("  Effective combat power total (highest option): " + effectiveTotal.ToString("0.###", CultureInfo.InvariantCulture));
            return sb.ToString().TrimEnd();
        }

        private string AutoResolveBuildPowerDisplay()
        {
            string battleType = AutoResolveBattleTypeComboBox.SelectedIndex == 0 ? "Space" : (AutoResolveBattleTypeComboBox.SelectedIndex == 1 ? "Land" : "(not selected)");
            return "Combat Power Preview\r\n" +
                "Battle Type: " + battleType + "\r\n\r\n" +
                AutoResolveBuildSidePowerDetails("Attacker", autoResolveSideA, autoResolveSideB) +
                "\r\n\r\n" +
                AutoResolveBuildSidePowerDetails("Defender", autoResolveSideB, autoResolveSideA);
        }

        private void AutoResolveUpdatePowerDisplay()
        {
            AutoResolveResultTextBox.Text = AutoResolveBuildPowerDisplay();
        }

        private void FillAutoResolveFactionSelection()
        {
            if (AutoResolveSideAFactionComboBox.Items.Count == 0)
            {
                foreach (holo_faction faction in globals.factions)
                {
                    AutoResolveSideAFactionComboBox.Items.Add(faction.textname);
                    AutoResolveSideBFactionComboBox.Items.Add(faction.textname);
                }
            }

            if (AutoResolveSideAFactionComboBox.Items.Count == 0) return;

            if (autoResolveSideAOwner < 0 || autoResolveSideAOwner >= AutoResolveSideAFactionComboBox.Items.Count) autoResolveSideAOwner = 0;
            if (autoResolveSideBOwner < 0 || autoResolveSideBOwner >= AutoResolveSideBFactionComboBox.Items.Count) autoResolveSideBOwner = Math.Min(1, AutoResolveSideBFactionComboBox.Items.Count - 1);

            AutoResolveSideAFactionComboBox.SelectedIndex = autoResolveSideAOwner;
            AutoResolveSideBFactionComboBox.SelectedIndex = autoResolveSideBOwner;
            AutoResolveUpdatePowerDisplay();
        }

        private bool AutoResolveUnitHasSufficientInformation(unit candidate)
        {
            return candidate.cp > 0 && !candidate.unitname.Contains("Dummy") && !candidate.unitname.Contains("Death_Clone");
        }

        private autoresolve_entry AutoResolveCreateEntryFromSelected()
        {
            autoresolve_entry entry = new autoresolve_entry();
            if (AutoResolveUnitComboBox.SelectedItem == null) return entry;

            entry.source = (unit)AutoResolveUnitComboBox.SelectedItem;
            entry.quantity = Math.Max(1, (int)AutoResolveUnitCountNumeric.Value);
            return entry;
        }

        private void AutoResolveAddToSideAButton_Click(object sender, EventArgs e)
        {
            if (AutoResolveUnitComboBox.SelectedItem == null) return;
            if (AutoResolveSideAFactionComboBox.SelectedIndex >= 0) autoResolveSideAOwner = AutoResolveSideAFactionComboBox.SelectedIndex;
            autoresolve_entry entry = AutoResolveCreateEntryFromSelected();

            int extant = autoResolveSideA.FindIndex(x => x.source.unitname == entry.source.unitname);
            if (extant >= 0)
            {
                autoresolve_entry edited = autoResolveSideA[extant];
                edited.quantity += entry.quantity;
                autoResolveSideA[extant] = edited;
            }
            else autoResolveSideA.Add(entry);

            AutoResolveRefreshSideListboxes();
        }

        private void AutoResolveAddToSideBButton_Click(object sender, EventArgs e)
        {
            if (AutoResolveUnitComboBox.SelectedItem == null) return;
            if (AutoResolveSideBFactionComboBox.SelectedIndex >= 0) autoResolveSideBOwner = AutoResolveSideBFactionComboBox.SelectedIndex;
            autoresolve_entry entry = AutoResolveCreateEntryFromSelected();

            int extant = autoResolveSideB.FindIndex(x => x.source.unitname == entry.source.unitname);
            if (extant >= 0)
            {
                autoresolve_entry edited = autoResolveSideB[extant];
                edited.quantity += entry.quantity;
                autoResolveSideB[extant] = edited;
            }
            else autoResolveSideB.Add(entry);

            AutoResolveRefreshSideListboxes();
        }

        private void AutoResolveClearSideAButton_Click(object sender, EventArgs e)
        {
            autoResolveSideA.Clear();
            AutoResolveRefreshSideListboxes();
        }

        private void AutoResolveClearSideBButton_Click(object sender, EventArgs e)
        {
            autoResolveSideB.Clear();
            AutoResolveRefreshSideListboxes();
        }

        private void AutoResolveInputChanged(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, AutoResolveBattleTypeComboBox)) FillAutoResolveUnitSelection();
            AutoResolveUpdatePowerDisplay();
        }

        private List<AutoResolveCombatant> AutoResolveBuildCombatants(List<autoresolve_entry> entries, int owner, bool space)
        {
            List<AutoResolveCombatant> corenne = new List<AutoResolveCombatant>();

            foreach (autoresolve_entry entry in entries)
            {
                for (int i = 0; i < Math.Max(1, entry.quantity); i++)
                {
                    AutoResolveCombatant combatant = new AutoResolveCombatant();
                    combatant.TypeName = entry.source.unitname;
                    combatant.OwnerId = owner;
                    combatant.Power = Math.Max(0.01f, entry.source.cp);
                    combatant.Health = 1.0f;
                    combatant.IsEscort = false;
                    combatant.IsTransport = entry.source.behaviors != null && entry.source.behaviors.Contains("Transport");
                    combatant.ContrastCategories = new List<string>();
                    foreach (string category in entry.source.categories) combatant.ContrastCategories.Add(category);
                    corenne.Add(combatant);
                }
            }

            return corenne;
        }

        private string AutoResolveOwnerToName(int owner)
        {
            if (owner >= 0 && owner < globals.factions.Count) return globals.factions[owner].textname;
            return "Owner " + owner.ToString();
        }

        private string AutoResolveUnitNameForDisplay(string unitTypeName)
        {
            if (string.IsNullOrWhiteSpace(unitTypeName) || unitTypeName == "(none)" || unitTypeName == "(tie)") return unitTypeName;

            foreach (unit item in AutoResolveGetUnitSource())
            {
                if (string.Equals(item.unitname, unitTypeName, StringComparison.OrdinalIgnoreCase)) return item.username + " [" + item.unitname + "]";
            }

            return unitTypeName;
        }

        private string AutoResolveRoundReportToText(AutoResolveRoundReport report)
        {
            if (report == null) return "";

            string winner;
            if (report.WinnerSideIndex < 0) winner = "Tie";
            else if (report.WinnerSideIndex == 0) winner = "Attacker";
            else winner = "Defender";

            return "Round " + report.RoundNumber.ToString() + ": " +
                "Attacker unit " + AutoResolveUnitNameForDisplay(report.SideAUnit) + " (power " + report.SideAPower.ToString("0.###", CultureInfo.InvariantCulture) + ") vs " +
                "Defender unit " + AutoResolveUnitNameForDisplay(report.SideBUnit) + " (power " + report.SideBPower.ToString("0.###", CultureInfo.InvariantCulture) + ") -> " +
                winner + " wins exchange" +
                (report.WinnerSideIndex >= 0 ? " with " + AutoResolveUnitNameForDisplay(report.WinningUnit) : "") +
                "; target " + AutoResolveUnitNameForDisplay(report.LosingUnit) +
                (report.LosingUnitDestroyed ? " was destroyed." : " survived.");
        }

        private void AutoResolveRunButton_Click(object sender, EventArgs e)
        {
            string powerSummary = AutoResolveBuildPowerDisplay();

            if (AutoResolveBattleTypeComboBox.SelectedIndex < 0)
            {
                AutoResolveResultTextBox.Text = powerSummary + "\r\n\r\nSelect a battle type (Space or Land) before running auto resolve.";
                return;
            }
            if (AutoResolveSideAFactionComboBox.SelectedIndex < 0 || AutoResolveSideBFactionComboBox.SelectedIndex < 0)
            {
                AutoResolveResultTextBox.Text = powerSummary + "\r\n\r\nSelect attacker and defender factions before running auto resolve.";
                return;
            }
            if (autoResolveSideA.Count == 0 || autoResolveSideB.Count == 0)
            {
                AutoResolveResultTextBox.Text = powerSummary + "\r\n\r\nAdd at least one unit to both attacker and defender before running auto resolve.";
                return;
            }

            autoResolveSideAOwner = AutoResolveSideAFactionComboBox.SelectedIndex;
            autoResolveSideBOwner = AutoResolveSideBFactionComboBox.SelectedIndex;

            bool space = AutoResolveBattleTypeComboBox.SelectedIndex == 0;
            List<AutoResolveCombatant> attacker = AutoResolveBuildCombatants(autoResolveSideA, autoResolveSideAOwner, space);
            List<AutoResolveCombatant> defender = AutoResolveBuildCombatants(autoResolveSideB, autoResolveSideBOwner, space);

            AutoResolveClass sim = new AutoResolveClass();
            AutoResolveHResult prep = space ? sim.Prepare_For_Space() : sim.Prepare_For_Land();
            if (prep != AutoResolveHResult.S_OK)
            {
                AutoResolveResultTextBox.Text = powerSummary + "\r\n\r\nFailed to prepare auto resolve simulation: " + prep.ToString();
                return;
            }

            foreach (AutoResolveCombatant combatant in attacker)
            {
                AutoResolveHResult add = sim.Add_Combatant(combatant);
                if (add != AutoResolveHResult.S_OK)
                {
                    AutoResolveResultTextBox.Text = powerSummary + "\r\n\r\nFailed to add attacker combatant: " + add.ToString();
                    return;
                }
            }
            foreach (AutoResolveCombatant combatant in defender)
            {
                AutoResolveHResult add = sim.Add_Combatant(combatant);
                if (add != AutoResolveHResult.S_OK)
                {
                    AutoResolveResultTextBox.Text = powerSummary + "\r\n\r\nFailed to add defender combatant: " + add.ToString();
                    return;
                }
            }

            AutoResolveHResult start = sim.Initiate_Combat(autoResolveSideAOwner);
            if (start != AutoResolveHResult.S_OK)
            {
                AutoResolveResultTextBox.Text = powerSummary + "\r\n\r\nFailed to start auto resolve simulation: " + start.ToString();
                return;
            }

            int rounds = 0;
            List<string> roundLines = new List<string>();
            while (sim.Who_Won() < 0 && sim.Get_Visible_Queue_Size(0) > 0 && sim.Get_Visible_Queue_Size(1) > 0 && rounds < 500)
            {
                sim.Combat_Round(true);
                rounds++;
                roundLines.Add(AutoResolveRoundReportToText(sim.Get_Last_Round_Report()));
            }

            int winner = sim.Who_Won();
            string winnerName = winner < 0 ? "No winner" : AutoResolveOwnerToName(winner);
            string attackerName = AutoResolveOwnerToName(autoResolveSideAOwner);
            string defenderName = AutoResolveOwnerToName(autoResolveSideBOwner);

            string outcome =
                "Auto Resolve complete\r\n" +
                "Battle Type: " + (space ? "Space" : "Land") + "\r\n" +
                "Rounds: " + rounds.ToString() + "\r\n" +
                "Attacker: " + attackerName + "\r\n" +
                "Defender: " + defenderName + "\r\n" +
                "Winner: " + winnerName + "\r\n" +
                "Side A remaining: " + sim.Get_Visible_Queue_Size(0).ToString() + " (health ratio " + sim.Get_Health_Ratio(0).ToString("0.###", CultureInfo.InvariantCulture) + ")\r\n" +
                "Side B remaining: " + sim.Get_Visible_Queue_Size(1).ToString() + " (health ratio " + sim.Get_Health_Ratio(1).ToString("0.###", CultureInfo.InvariantCulture) + ")";

            string roundDetail = roundLines.Count > 0
                ? "\r\n\r\nRound-by-round exchanges:\r\n" + string.Join("\r\n", roundLines)
                : "\r\n\r\nRound-by-round exchanges:\r\n(no rounds executed)";

            AutoResolveResultTextBox.Text = powerSummary + "\r\n\r\n" + outcome + roundDetail;
        }


        private void FillMatrixLookup()
        {
            MatrixGrid.Columns.Clear();
            List<string> damages = entities.SpaceDamageTypes;
            List<string> armors = new List<string>();
            if (MatrixSpaceRB.Checked)
            {
                if(entities.SpaceDamageTypes.Count > 0) damages = entities.SpaceDamageTypes;
                else damages = entities.DamageTypes;

                if (entities.SpaceArmors.Count > 0)
                {
                    foreach (string armor in entities.SpaceArmors) armors.Add(armor);
                    foreach (string armor in entities.SpaceShields) armors.Add(armor);
                }
                else damages = entities.AllArmors;
            }
            else
            {
                if (entities.GroundDamageTypes.Count > 0) damages = entities.GroundDamageTypes;
                else damages = entities.DamageTypes;

                if (entities.GroundArmors.Count > 0)
                {
                    foreach (string armor in entities.GroundArmors) armors.Add(armor);
                    foreach (string armor in entities.GroundShields) armors.Add(armor);
                }
                else damages = entities.AllArmors;
            }
            int x = armors.Count;
            int y = damages.Count;
            MatrixGrid.Columns.Add("DamageNames", "");
            for (int i = 0; i < y; i++)
            {
                MatrixGrid.Columns.Add(damages[i], damages[i].Replace("DamageL_","").Replace("DamageS_", ""));
            }
            for (int i = 0; i < x; i++)
            {
                string[] row = new string[y + 1];
                row[0] = armors[i];
                ArmorMods mods = GetArmorMods(armors[i]);
                for (int j = 1; j < y + 1; j++)
                {
                    row[j] = "1";
                    foreach (ArmorMod mod in mods.WeaponMods)
                    {
                        if (mod.armorType == MatrixGrid.Columns[j].Name)
                        {
                            row[j] = mod.modifier.ToString();
                            break;
                        }
                    }

                }
                MatrixGrid.Rows.Add(row);
            }

            //Automatically shrink (mostly) columns as needed
            for (int i = 0; i<MatrixGrid.Columns.Count; i++)
            {
                MatrixGrid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
            //turn off to allow resizing but save and restore auto value
            for (int i = 0; i < MatrixGrid.Columns.Count; i++)
            {
                int width = MatrixGrid.Columns[i].Width;
                MatrixGrid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                MatrixGrid.Columns[i].Width = width - 10; //It's a bit generous with the margins
            }
        }

        private void FillShipnameListbox()
        {
            //Todo way to select only hero > 0 and units > 0
            ShipnameListBox.Items.Clear();
            if (ShipnameSortNameRB.Checked) globals.shipnames.Sort((s1, s2) => s1.name.CompareTo(s2.name));
            if (ShipnameSortUnitRB.Checked) globals.shipnames.Sort((s2, s1) => s1.units.Count.CompareTo(s2.units.Count));
            if (ShipnameSortHeroRB.Checked) globals.shipnames.Sort((s2, s1) => s1.heroes.Count.CompareTo(s2.heroes.Count));
            if (ShipnameSortAllRB.Checked) globals.shipnames.Sort((s2, s1) => (s1.units.Count + s1.heroes.Count).CompareTo(s2.units.Count + s2.heroes.Count));
            //globals.shipnames.OrderByDescending(s1 => (s1.units.Count + s1.heroes.Count)).ThenBy(s1 => s1.name); //This ends up pure alphabetical
            foreach (shipname moniker in globals.shipnames)
            {
                if (moniker.name.ToLower().Contains(ShipnameSearchTextBox.Text.ToLower())) {
                    bool heroOk = false;
                    bool groundOk = false;
                    bool cadetOk = false;
                    bool spaceOk = false;
                    if (ShipnameFilterHeroCheckbox.Checked && moniker.heroes.Count > 0) heroOk = true;
                    int ground = 0;
                    int cadet = 0;
                    foreach (string unit in moniker.units)
                    {
                        if (unit.Contains("Dropship_")) ground++;
                        if (unit.Contains("Cadet_")) cadet++;
                    }
                    if (ShipnameFilterGroundCheckbox.Checked && ground > 0) groundOk = true;
                    if (ShipnameFilterMinorCheckbox.Checked && cadet > 0) cadetOk = true;
                    if (ShipnameFilterSpaceCheckbox.Checked && moniker.units.Count > cadet + ground) spaceOk = true;
                    if ((heroOk || groundOk || cadetOk || spaceOk) && !ShipnamesFilterDuplicatesCheckbox.Checked || (heroOk && spaceOk && ShipnamesFilterDuplicatesCheckbox.Checked)) ShipnameListBox.Items.Add(moniker); //Unused measn this is always ok? Or perhaps it marks space_ok, since that is the main use
                }
            }

            ShipnameCountLabel.Text = "Matches: " + ShipnameListBox.Items.Count;
        }

        private void ShipnameSearchTextBox_TextChanged(object sender, EventArgs e)
        {
            FillShipnameListbox(); //Make these all the same handler
        }

        private void ShipnameSortNameRB_CheckedChanged(object sender, EventArgs e)
        {
            FillShipnameListbox();
        }

        private void ShipnameSortUnitRB_CheckedChanged(object sender, EventArgs e)
        {
            FillShipnameListbox();
        }

        private void ShipnameSortHeroRB_CheckedChanged(object sender, EventArgs e)
        {
            FillShipnameListbox();
        }

        private void ShipnameSortAllRB_CheckedChanged(object sender, EventArgs e)
        {
            FillShipnameListbox();
        }

        private void ShipnameFilterSpaceCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            FillShipnameListbox();
        }

        private void ShipnameFilterHeroCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            FillShipnameListbox();
        }

        private void ShipnameFilterGroundCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            FillShipnameListbox();
        }

        private void ShipnameFilterMinorCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            FillShipnameListbox();
        }

        private void ShipnamesFilterDuplicatesCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            ShipnameFilterSpaceCheckbox.Enabled = !ShipnamesFilterDuplicatesCheckbox.Checked;
            ShipnameFilterGroundCheckbox.Enabled = !ShipnamesFilterDuplicatesCheckbox.Checked;
            ShipnameFilterMinorCheckbox.Enabled = !ShipnamesFilterDuplicatesCheckbox.Checked;
            ShipnameFilterHeroCheckbox.Enabled = !ShipnamesFilterDuplicatesCheckbox.Checked;
            FillShipnameListbox();
        }

        private void ShipnameListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(ShipnameListBox.SelectedItems.Count > 0)
            {
                shipname moniker = (shipname)ShipnameListBox.SelectedItem;
                ShipnameDetailLabel.Text = moniker.name;
                if(moniker.units.Count > 0) ShipnameDetailLabel.Text += "\n\nUsed in name lists:\n"+ moniker.units[0];
                for (int i = 1; i < moniker.units.Count; i++) ShipnameDetailLabel.Text += ", " + moniker.units[i];
                if (moniker.heroes.Count > 0) ShipnameDetailLabel.Text += "\n\nUsed by heroes:\n" + moniker.heroes[0];
                for (int i = 1; i < moniker.heroes.Count; i++) ShipnameDetailLabel.Text += ", " + moniker.heroes[i];
                if (moniker.unused.Count > 0) ShipnameDetailLabel.Text += "\n\nPlanned for future lists:\n" + moniker.unused[0];
                for (int i = 1; i < moniker.unused.Count; i++) ShipnameDetailLabel.Text += ", " + moniker.unused[i];
            }
        }

        private void MissionListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            MissionText.Text = File.ReadAllText(getModFile("Scripts\\Library\\eawx-plugins\\intervention-missions\\rewards\\RewardTables_" + MissionListBox.SelectedItem + ".lua"));
        }

        public struct shipname
        {
            public string name;
            public List<string> heroes;
            public List<string> units;
            public List<string> unused;

            public override string ToString()
            {
                return name + " (" + units.Count + "," + heroes.Count + ")";
            }
        }

        private void SpawnListBox_SelectedIndexChanged(object sender, EventArgs e)
        {//Todo list planets where used
            SpawnText.Text = File.ReadAllText(getModFile("Scripts\\Library\\spawn-sets\\" + SpawnListBox.SelectedItem + ".lua"));
        }

        private void StandardFListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            StandardFText.Text = File.ReadAllText(getModFile("Scripts\\Library\\standard-fighters\\" + StandardFListBox.SelectedItem + ".lua"));
        }

        private void RandomFListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            RandomFText.Text = File.ReadAllText(getModFile("Scripts\\Library\\random-fighters\\" + RandomFListBox.SelectedItem + ".lua"));
        }

        private void FactionListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            FactionSpaceListBox.Items.Clear();
            FactionGroundListbox.Items.Clear();
            if (FactionListBox.SelectedIndex < 0)
            {
                FactionNameLabel.Text = "";
                FactionInternalLabel.Text = "Internal Name: ";
                return;
            }
            holo_faction faction = globals.factions[FactionListBox.SelectedIndex];
            insert_history((int)historymaintabs.faction, 0, faction.codename);

            FactionNameLabel.Text = faction.textname;
            FactionInternalLabel.Text = "Internal Name: " + faction.codename;
            foreach(unit unit in entities.spaceUnits)
            {
                if (unit.affiliations.Contains(faction.codename)) FactionSpaceListBox.Items.Add(unit);
            }
            foreach (unit unit in entities.groundCompanies)
            {
                if (unit.affiliations.Contains(faction.codename)) FactionGroundListbox.Items.Add(unit);
            }
        }

        private bool FullSalvoOn()
        {
            if (UnitAbilityListBox.SelectedItems.Count == 0) return false;
            return (((unitability)UnitAbilityListBox.SelectedItem).type == "FULL_SALVO");
        }

        private void setDPSBreakdown(bool reset_hplistbox = false)
        {
            unit selectedUnit = (unit)UnitListBox.Tag;
            if (reset_hplistbox) UnitHPListbox.Items.Clear();
            List<string> types = new List<string>();
            List<float> damage = new List<float>();
            List<float> avgMults = new List<float>();
            List<float> ranges = new List<float>();
            foreach (hardpoint hp in selectedUnit.consolidatedhps)
            {
                if (reset_hplistbox) UnitHPListbox.Items.Add(hp);
                bool found = false;
                if (hp.range > 0 && !ranges.Contains(hp.range)) ranges.Add(hp.range);
                for (int i = 0; i < types.Count; i++)
                {
                    if (types[i] == hp.damageType)
                    {
                        damage[i] += getDPS(hp) * (float)UADamageLabel.Tag / (float)UAReloadLabel.Tag;
                        if (FullSalvoOn()) damage[i] /= hp.fullsalvomod;
                        found = true;
                        break;
                    }
                }
                if (!found && hp.damageAmount > 0)
                {
                    types.Add(hp.damageType);
                    float dps = getDPS(hp) * (float)UADamageLabel.Tag / (float)UAReloadLabel.Tag;
                    if (FullSalvoOn()) dps /= hp.fullsalvomod;
                    damage.Add(dps);
                    foreach (WeaponMods weaps in entities.ArmorMods)
                    {
                        if (weaps.weaponType == hp.damageType)
                        {
                            avgMults.Add(weaps.median);
                            break;
                        }
                    }
                }
            }
            RawDPSLabel.Text = "Base DPS totals:";
            float average = 0;
            for (int i = 0; i < types.Count; i++)
            {
                RawDPSLabel.Text += "\n" + types[i] + ": " + damage[i].ToString(globals.dpsformat);
                float averagemult = 1;
                if (i < avgMults.Count) averagemult = avgMults[i];
                average += (damage[i] * averagemult);
            }
            RawDPSLabel.Text += "\nAverage DPS Total: " + average.ToString(globals.dpsformat);

            setTargetDPS();
            if (reset_hplistbox)
            {
                TargetRangeBox.Items.Clear();
                ranges.Sort((s1, s2) => (s1).CompareTo(s2));
                if (ranges.Count > 0)
                {
                    foreach (float range in ranges) TargetRangeBox.Items.Add(range);
                    TargetRangeBox.SelectedIndex = 0;
                }
                UnitHPListbox_SelectedIndexChanged(UnitHPListbox, new EventArgs());
            }
        }

        private void setAbilityDependentStats()
        {
            unit selectedUnit = (unit)UnitListBox.Tag;
            if (SpaceRadioButton.Checked) UnitHpLabel.Text = "Hull: ";
            else UnitHpLabel.Text = "Health: ";
            if (selectedUnit.armor_type != "")
            {
                int abilityhp = (int)(selectedUnit.hp / (float)UADefenseLabel.Tag);
                UnitHpLabel.Text += abilityhp.ToString() + ArmorTypeString(selectedUnit.armor_type);
                ArmorMods mods = GetArmorMods(selectedUnit.armor_type);
                UnitHpAvgLabel.Text = "Modified HP: " + (abilityhp / (1 - mods.average)).ToString("0");
            }
            else
            {
                UnitHpLabel.Text = "";
            }
            if (selectedUnit.shield <= 0)
            {
                UnitShieldLabel.Text = "Shields: N/A";
                TimeToRegenLabel.Text = "";
                UnitShieldAvgLabel.Text = "";
            }
            else
            {
                int abilityshield = (int)(selectedUnit.shield / (float)UADefenseLabel.Tag);
                float abilityregen = (int)(selectedUnit.regen * (float)UAShieldLabel.Tag);
                UnitShieldLabel.Text = "Shields: " + abilityshield + " / [" + abilityregen + "/R]" + ArmorTypeString(selectedUnit.shield_type);
                if (!(selectedUnit.behaviors.Contains("SHIELDED") || selectedUnit.modebehaviors.Contains("SHIELDED"))) UnitShieldLabel.Text = "NO BEHAVIOR (" + selectedUnit.shield + "/" + selectedUnit.regen + " " + ArmorTypeString(selectedUnit.shield_type) + ")";
                float regenSeconds = selectedUnit.shield * 3 / abilityregen;
                if (regenSeconds >= 0) TimeToRegenLabel.Text = "Time to Regen: ";
                else
                {
                    TimeToRegenLabel.Text = "Time to Drain: ";
                    regenSeconds *= -1;
                }
                if (float.IsInfinity(regenSeconds)) TimeToRegenLabel.Text += regenSeconds.ToString();
                else TimeToRegenLabel.Text += (Math.Floor(regenSeconds / 60)).ToString() + ":" + ((int)regenSeconds % 60).ToString("00");
                ArmorMods mods = GetArmorMods(selectedUnit.shield_type);
                UnitShieldAvgLabel.Text = "Modified: " + (abilityshield / (1 - mods.average)).ToString("0");
            }
            UnitSpeedLabel.Text = "Speed: " + (selectedUnit.speed * (float)UASpeedLabel.Tag).ToString("0.0#");
            if (true) UnitSpeedLabel.Text += " | Accel: " + selectedUnit.accel.ToString("0.0##"); //todo hide appropriately
            if (true) UnitSpeedLabel.Text += " | Turn: " + selectedUnit.turn.ToString("0.0");
        }

        private void UnitListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UnitTooltipLabelRichTextBox.Text = "";
            IconPictureBox.Image = new Bitmap(IconPictureBox.Width, IconPictureBox.Height);
            if (UnitListBox.SelectedIndex < 0)
            {
                UnitNameLabel.Text = "Name: ";
                UnitInternalLabel.Text = "Internal Name: ";
                return;
            }
            unit selectedUnit = (unit)UnitListBox.SelectedItem;
            UnitListBox.Tag = selectedUnit;

            //abilities
            UnitAbilityListBox.Items.Clear();
            foreach (unitability able in selectedUnit.unitabilities) UnitAbilityListBox.Items.Add(able);
            AbilityListBox.Items.Clear();
            foreach (ability able in selectedUnit.abilities) AbilityListBox.Items.Add(able);
            ResetAbilitySelection();

            IconData icondata = DatParser.GetIconData(selectedUnit.icon, entities);
            if (icondata.size_x > 0 && entities.MTmaster != null)
            {
                // Create a Graphics object to do the drawing, *with the new bitmap as the target*
                using (Graphics g = Graphics.FromImage(IconPictureBox.Image))
                {
                    g.DrawImage(entities.MTmaster, 0, 0, new Rectangle(icondata.origin_x, icondata.origin_y, icondata.size_x, icondata.size_y), GraphicsUnit.Pixel);
                }
            }
            int tab = 0;
            if(GroundRadioButton.Checked) tab = 1;
            else if (UnitRadioButton.Checked) tab = 2;
            else if (FighterRadioButton.Checked) tab = 3;
            else if (SpaceHeroRadioButton.Checked) tab = 4;
            else if (HeroCompaniesRadioButton.Checked) tab = 5;
            else if (GroundHeroRadioButton.Checked) tab = 6;
            else if (StructureRadioButton.Checked) tab = 7;
            else if (SpaceStructureRadioButton.Checked) tab = 8;
            insert_history((int)historymaintabs.unit, tab, selectedUnit.unitname);

            ShipNameRichTextBox.Text = "";
            string[] monikers = findUnitNameFile(selectedUnit, entities);
            foreach(string moniker in monikers)
            {
                ShipNameRichTextBox.Text += moniker + "\n";
            }

            UnitNameLabel.Text = "Name: " + selectedUnit.username;
            UnitInternalLabel.Text = "Internal Name: " + selectedUnit.unitname;
            foreach (string entry in SplitXMLWhitespaceList(selectedUnit.tooltip)) UnitTooltipLabelRichTextBox.Text += Find_Text_Entry(entry) + "\n";
            if (selectedUnit.BTS != "") BTSRichTextBox.Text = "Behind the scenes:\n" + selectedUnit.BTS;
            else BTSRichTextBox.Text = "";
            setAbilityDependentStats();

            if (selectedUnit.pop > 0)  UnitPopLabel.Text = "Population: " + selectedUnit.pop.ToString();
            else UnitPopLabel.Text = "";
            if (selectedUnit.cost > 1) UnitCostLabel.Text = "Cost: " + selectedUnit.cost.ToString();
            else UnitCostLabel.Text = "";
            if (selectedUnit.skirmcost > 1) UnitSkirmCostLabel.Text = "Skirmish: " + selectedUnit.skirmcost.ToString();
            else UnitSkirmCostLabel.Text = "";
            if (selectedUnit.crew > 0) UnitCrewLabel.Text = "Crew: " + selectedUnit.crew.ToString();
            else UnitCrewLabel.Text = "";
            if (selectedUnit.buildtime > 0) UnitTimeLabel.Text = "Build Time: " + selectedUnit.buildtime.ToString();
            else UnitTimeLabel.Text = "";
            if (selectedUnit.skirmbuildtime > 0) UnitSkirmTimeLabel.Text = "Skirmish: " + selectedUnit.skirmbuildtime.ToString();
            else UnitSkirmTimeLabel.Text = "";
            if (selectedUnit.percompany > 0) UnitsPerLabel.Text = "Units in Company: " + selectedUnit.percompany.ToString();
            else UnitsPerLabel.Text = "";
            if (selectedUnit.terrainMaps.Count > 0)
            {
                bool firstmap = true;
                MapsAndBombingRunLabel.Text = "Terrain Specific Models: ";
                foreach (string map in selectedUnit.terrainMaps)
                {
                    if (firstmap) firstmap = false;
                    else MapsAndBombingRunLabel.Text += ", ";
                    MapsAndBombingRunLabel.Text += map;
                }
            }
            else if (selectedUnit.bombingRunUnit != "") MapsAndBombingRunLabel.Text = "Bombing run unit: " + selectedUnit.bombingRunUnit;
            else MapsAndBombingRunLabel.Text = "";
            if (selectedUnit.maintenance > 0 && selectedUnit.fightermode > 0) MaintenanceLabel.Text = "Maintenance (actual/calculated): " + selectedUnit.maintenance + "/" + (selectedUnit.buildtime * 30 / 50).ToString("0"); //Maintenance is weird, don't question the formula
            else MaintenanceLabel.Text = "";
            setDPSBreakdown(true);

            UnitSubunitListbox.Items.Clear();
            if (!(selectedUnit.consolidatedUnits is null) && selectedUnit.consolidatedUnits.Count > 0)
            {
                foreach (quantizedObject subunit in selectedUnit.consolidatedUnits) UnitSubunitListbox.Items.Add(subunit);
            }
            UnitSubSquadListbox.Items.Clear();
            if (!(selectedUnit.subcompanies is null) && selectedUnit.subcompanies.Count > 0)
            {
                foreach (quantizedObject subsquad in selectedUnit.subcompanies) UnitSubSquadListbox.Items.Add(subsquad);
            }

            if (globals.UnitSortConfig.SortType == UnitSortTypes.Name) SortValueLabel.Text = ""; //Pretty redundant to show in this case
            else if (selectedUnit.sortstring != "") SortValueLabel.Text = "Sort: " + selectedUnit.sortstring;
            else SortValueLabel.Text = "Sort: " + selectedUnit.sortfloat.ToString(globals.dpsformat);

            if(FactionListBox.SelectedItems.Count > 0) IncomingDamageBox_SelectedIndexChanged(IncomingDamageBox, e);
            if (selectedUnit.garrison_slots > 0) GarrisonSlotLabel.Text = "Garrison Slots: " + selectedUnit.garrison_slots.ToString();
            else GarrisonSlotLabel.Text = "";
            if (selectedUnit.garrison_value > 0) GarrisonValueLabel.Text = "Garrison Value: " + selectedUnit.garrison_value.ToString();
            else GarrisonValueLabel.Text = "";
            if (selectedUnit.garrison_type!= "") GarrisonTypeLabel.Text = "Garrison Type: " + selectedUnit.garrison_type;
            else GarrisonTypeLabel.Text = "";
            if (GroundRadioButton.Checked || UnitRadioButton.Checked) LocomotorLabel.Text = "Locomotor: " + selectedUnit.locomotor_type; //todo ground hero and company
            else if (selectedUnit.fightermode >= 0) LocomotorLabel.Text = "Locomotor: Fighter";
            else LocomotorLabel.Text = "";
            AlphaCheckBox.Checked = selectedUnit.fightermode > 0; //0 = gunship, which does have the locomotor but uses normal cp calcs

            CategoryLabel.Text = "Category: ";
            bool furst = true;
            foreach (string category in selectedUnit.categories)
            {
                if (furst) furst = false;
                else CategoryLabel.Text += ", ";
                CategoryLabel.Text += category;
            }
            FlagLabel.Text = "Property Flags: ";
            furst = true;
            foreach (string flag in selectedUnit.flags)
            {
                if (furst) furst = false;
                else FlagLabel.Text += ", ";
                FlagLabel.Text += flag;
            }

            //Availability
            ReqStructuresListBox.Items.Clear();
            foreach (string reqstru in fullTrim(selectedUnit.reqstructures.Replace(",", "|")).Split('|'))
            {
                if (!reqstru.Contains("_Dummy") && !(reqstru.Contains("INFLUENCE_")))
                {
                    foreach(unit building in entities.structures)
                    {
                        if(building.unitname.ToLower() == reqstru.ToLower())
                        {
                            unit building2 = building;
                            building2.username = getBuildingAffils(building2);
                            building2.sortstring = building2.username;
                            ReqStructuresListBox.Items.Add(building2);
                            break;
                        }
                    }
                }
            }

            FactionAvailableListbox.Items.Clear();
            foreach(string affilation in selectedUnit.affiliations)
            {
                foreach (holo_faction faction in globals.factions)
                { 
                    if (affilation == faction.codename)
                    {
                        FactionAvailableListbox.Items.Add(faction);
                        break;
                    }
                }
            }
            AvailabilityLabel.Text = "";

            //TODO add required planets (and what GC the unit can be built in!

            if (selectedUnit.level > 0) ShipyardLabel.Text = "Filter Shipyard: " + selectedUnit.level;
            else ShipyardLabel.Text = "";

            if (selectedUnit.limit_concurrent > 0 || selectedUnit.limit_lifetime > 0)
            {
                BuildLimitLabel.Text = "Build Limit";
                if (selectedUnit.limit_concurrent > 0) BuildLimitLabel.Text += " Concurrent " + selectedUnit.limit_concurrent;
                if (selectedUnit.limit_lifetime > 0) BuildLimitLabel.Text += " Lifetime " + selectedUnit.limit_lifetime;
            }
            else BuildLimitLabel.Text = "";

            if (selectedUnit.influence > 0) InfluenceLabel.Text = "Influence Level: " + selectedUnit.influence;
            else InfluenceLabel.Text = "";

            if (selectedUnit.reqorbit != "") ReqUnitLabel.Text = "Required Units: " + selectedUnit.reqorbit;
            else ReqUnitLabel.Text = "";

            List<string> spawnsets = getModFiles("Scripts\\Library\\spawn-sets", "*.lua"); //TODO make sure this is still correct when modcontentloader is cut
            SpawnSetListBox.Items.Clear();
            foreach (string file in spawnsets)
            {
                string filetext = File.ReadAllText(file);
                if(filetext.Contains("\""+selectedUnit.unitname+"\"")) SpawnSetListBox.Items.Add(LastFolderOrFile(file).ToUpper().Replace(".LUA", ""));
            }//todo goto after history for the lookup is set up
        }

        private unit sortUnit(unit unit)
        {
            unit.sortstring = ""; //Used for detecting if the float should be displayed
            switch (globals.UnitSortConfig.SortType)
            {
                case UnitSortTypes.Name:
                    unit.sortstring = unit.username;
                    break;
                case UnitSortTypes.Pop:
                    unit.sortfloat = unit.pop;
                    break;
                case UnitSortTypes.Internal:
                    unit.sortstring = unit.unitname;
                    break;
                case UnitSortTypes.Class:
                    unit.sortstring = Find_Text_Entry(unit.unitclass);
                    break;
                case UnitSortTypes.Price:
                    unit.sortfloat = unit.cost;
                    break;
                case UnitSortTypes.SkPrice:
                    unit.sortfloat = unit.skirmcost;
                    break;
                case UnitSortTypes.Time:
                    unit.sortfloat = unit.buildtime;
                    break;
                case UnitSortTypes.SkTime:
                    unit.sortfloat = unit.skirmbuildtime;
                    break;
                case UnitSortTypes.CompanySize:
                    unit.sortfloat = unit.percompany;
                    break;
                case UnitSortTypes.Shipyard:
                    unit.sortfloat = unit.level;
                    break;
                case UnitSortTypes.CP:
                    unit.sortfloat = unit.cp; //todo include complement cp
                    break;
                case UnitSortTypes.Durability: //TODO modifiers for reflect/absorb
                    float shield = unit.shield;
                    if (shield < 0 || !unit.behaviors.Contains("SHIELDED")) shield = 0;
                    if (globals.UnitSortConfig.DurabilityMode == 2)
                    {
                        ArmorMods mods = GetArmorMods(unit.armor_type);
                        unit.sortfloat = unit.hp / (1 - mods.average);
                        mods = GetArmorMods(unit.shield_type);
                        unit.sortfloat += shield / (1 - mods.average);
                    }
                    else if (globals.UnitSortConfig.DurabilityMode == 3)
                    {
                        WeaponMods weaps = GetWeaponMods(IncomingDamageBox.Text);
                        if (unit.armor_type != "")
                        {
                            float mult = getWeapMultiplier(unit.armor_type, weaps, false);
                            unit.sortfloat = unit.hp / mult;
                            mult = getWeapMultiplier(unit.shield_type, weaps, false);
                            unit.sortfloat += shield / mult;
                        }
                        else unit.sortfloat = shield + unit.hp;
                    }
                    else unit.sortfloat = shield + unit.hp;
                    break;
                case UnitSortTypes.HP:
                    unit.sortfloat = unit.hp;
                    if (globals.UnitSortConfig.DurabilityMode == 2)
                    {
                        ArmorMods mods = GetArmorMods(unit.armor_type);
                        unit.sortfloat /= 1 - mods.average;
                    }
                    else if (globals.UnitSortConfig.DurabilityMode == 3)
                    {
                        WeaponMods weaps = GetWeaponMods(IncomingDamageBox.Text);
                        if (unit.armor_type != "")
                        {
                            unit.sortfloat /= getWeapMultiplier(unit.armor_type, weaps, false);
                        }
                    }
                    break;
                case UnitSortTypes.Shield:
                    unit.sortfloat = unit.shield;
                    if (unit.sortfloat < 0 || !unit.behaviors.Contains("SHIELDED")) unit.sortfloat = 0;
                    if (globals.UnitSortConfig.DurabilityMode == 2)
                    {
                        ArmorMods mods = GetArmorMods(unit.shield_type);
                        unit.sortfloat /= 1 - mods.average;
                    }
                    else if (globals.UnitSortConfig.DurabilityMode == 3)
                    {
                        WeaponMods weaps = GetWeaponMods(IncomingDamageBox.Text);
                        if (unit.armor_type != "")
                        {
                            unit.sortfloat /= getWeapMultiplier(unit.shield_type, weaps, false);
                        }
                    }
                    break;
                case UnitSortTypes.Regen:
                    unit.sortfloat = unit.regen;
                    if (globals.UnitSortConfig.DurabilityMode == 2)
                    {
                        ArmorMods mods = GetArmorMods(unit.shield_type);
                        unit.sortfloat /= 1 - mods.average;
                    }
                    else if (globals.UnitSortConfig.DurabilityMode == 3)
                    {
                        WeaponMods weaps = GetWeaponMods(IncomingDamageBox.Text);
                        if (unit.armor_type != "")
                        {
                            unit.sortfloat /= getWeapMultiplier(unit.shield_type, weaps, false);
                        }
                    }
                    break;
                case UnitSortTypes.SType:
                    unit.sortstring = unit.shield_type;
                    break;
                case UnitSortTypes.AType:
                    unit.sortstring = unit.armor_type;
                    break;
                case UnitSortTypes.Speed:
                    unit.sortfloat = unit.speed;
                    break;
                case UnitSortTypes.Accel:
                    unit.sortfloat = unit.accel;
                    break;
                case UnitSortTypes.Turn:
                    unit.sortfloat = unit.turn;
                    break;
                case UnitSortTypes.dpsRaw:
                    unit.sortfloat = 0;
                    foreach(hardpoint hp in unit.consolidatedhps)
                    {
                        if (hp.damageType == IncomingDamageBox.Text)
                        {
                            float dps = getDPS(hp);
                            if (globals.UnitSortConfig.Accuracy)
                            {
                                dps *= GetHPAccuracyMod(hp);
                            }
                            unit.sortfloat += dps;
                        }
                    }
                    break;
                case UnitSortTypes.dpsAvg:
                    unit.sortfloat = 0;
                    foreach (hardpoint hp in unit.consolidatedhps)
                    {
                        if(hp.damageType != "") //Turns out hangars and engines will do something when routed through the calcs
                        {
                            float dps = getDPS(hp);
                            dps *= GetWeaponMods(hp.damageType).median;
                            if (globals.UnitSortConfig.Accuracy)
                            {
                                dps *= GetHPAccuracyMod(hp);
                            }
                            unit.sortfloat += dps;
                        }
                    }
                    break;
                case UnitSortTypes.dpsArmor:
                    unit.sortfloat = 0;
                    foreach (hardpoint hp in unit.consolidatedhps)
                    {
                        if (hp.damageType != "")
                        {
                            float dps = getDPS(hp);
                            WeaponMods weap = GetWeaponMods(hp.damageType);
                            dps *= getWeapMultiplier(TargetArmorBox.Text, weap, false);
                            if (globals.UnitSortConfig.Accuracy)
                            {
                                dps *= GetHPAccuracyMod(hp);
                            }
                            unit.sortfloat += dps;
                        }
                    }
                    break;
                case UnitSortTypes.dpsShield:
                    unit.sortfloat = 0;
                    foreach (hardpoint hp in unit.consolidatedhps)
                    {
                        if (hp.damageType != "")
                        {
                            float dps = getDPS(hp);
                            WeaponMods weap = GetWeaponMods(hp.damageType);
                            dps *= getWeapMultiplier(TargetShieldBox.Text, weap, true);
                            if (globals.UnitSortConfig.Accuracy)
                            {
                                dps *= GetHPAccuracyMod(hp);
                            }
                            unit.sortfloat += dps;
                        }
                    }
                    break;
                //todo Complement
                //case UnitSortTypes.Complement:
                //    unit.sortfloat = unit.regen;
                //    break;
                case UnitSortTypes.GarrisonCap:
                    unit.sortfloat = unit.garrison_slots;
                    break;
                case UnitSortTypes.GarrisonValue:
                    unit.sortfloat = unit.garrison_value;
                    break;
            }

            //TODO remove bad sort conditions on rb change
            if(globals.UnitSortConfig.SortType > UnitSortTypes.Name) //Numerical
            {
                float denom = 1;
                switch (globals.UnitSortConfig.denomtype)
                {
                    //case "Absolute Value": //Just the fallback
                    //    break;
                    case "Per Combat Power":
                        denom = unit.cp;
                        break;
                    case "Per Population":
                        denom = unit.pop;
                        break;
                    case "Per Credit":
                        denom = unit.cost;
                        break;
                    case "Per Skirmish Price":
                        denom = unit.skirmcost;
                        break;
                    case "Per Build Time":
                        denom = unit.buildtime;
                        break;
                    case "Per Skirmish Time":
                        denom = unit.skirmbuildtime;
                        break;
                    case "Per Crew":
                        denom = unit.crew;
                        break;
                    case "Per Unit in Company":
                        denom = unit.percompany;
                        break;
                }//Todo: might need to prvent division by 0? But if that results in an infinity symbol it's probably ok
                unit.sortfloat /= denom;
            }
            return unit;
        }

        private bool filterUnit(unit unit)
        {
            if (globals.UnitFilterConfig.factions.Count > 0)
            {
                bool match = false;
                foreach (holo_faction filter in globals.UnitFilterConfig.factions)
                {
                    if (unit.affiliations.Contains(filter.codename))
                    {
                        match = true;
                        break;
                    }
                }
                if (!match && !globals.UnitFilterConfig.invFaction || match && globals.UnitFilterConfig.invFaction) return false;
            }

            if (globals.UnitFilterConfig.categories.Count > 0)
            {
                bool match = false;
                foreach (string filter in globals.UnitFilterConfig.categories)
                {
                    if (unit.categories.Contains(filter))
                    {
                        match = true;
                        break;
                    }
                }
                if (!match && !globals.UnitFilterConfig.invCats || match && globals.UnitFilterConfig.invCats) return false;
            }

            if (globals.UnitFilterConfig.flags.Count > 0)
            {
                bool match = false;
                foreach (string filter in globals.UnitFilterConfig.flags)
                {
                    if (unit.flags.Contains(filter))
                    {
                        match = true;
                        break;
                    }
                }
                if (!match && !globals.UnitFilterConfig.invFlags || match && globals.UnitFilterConfig.invFlags) return false;
            }

            if (globals.UnitFilterConfig.atypes.Count > 0)
            {
                bool match = false;
                foreach (string filter in globals.UnitFilterConfig.atypes)
                {
                    if (unit.armor_type == filter)
                    {
                        match = true;
                        break;
                    }
                }
                if (!match && !globals.UnitFilterConfig.invAT || match && globals.UnitFilterConfig.invAT) return false;
            }

            if (globals.UnitFilterConfig.stypes.Count > 0)
            {
                bool match = false;
                foreach (string filter in globals.UnitFilterConfig.stypes)
                {
                    if (unit.shield_type == filter)
                    {
                        match = true;
                        break;
                    }
                }
                if (!match && !globals.UnitFilterConfig.invST || match && globals.UnitFilterConfig.invST) return false;
            }

            if (globals.UnitFilterConfig.buildableMode > 0)
            {
                if(unit.techlevel <= 5 && unit.fightermode <=0)// && unit.cost > 1) //For a loose definition of buildable, but one only prone to false positives
                {
                    if (globals.UnitFilterConfig.buildableMode == 2) return false;
                }
                else
                {
                    if (globals.UnitFilterConfig.buildableMode == 1) return false;
                }
            }

            if (globals.UnitFilterConfig.planetMode > 0)
            {
                if (unit.planets.Length > 0)
                {
                    if (globals.UnitFilterConfig.planetMode == 2) return false;
                }
                else
                {
                    if (globals.UnitFilterConfig.planetMode == 1) return false;
                }
            }

            if (globals.UnitFilterConfig.influenceMode > 0)
            {
                if (unit.influence > 0)
                {
                    if (globals.UnitFilterConfig.influenceMode == 2) return false;
                }
                else
                {
                    if (globals.UnitFilterConfig.influenceMode == 1) return false;
                }
            }

            if (globals.UnitFilterConfig.limitMode > 0)
            {
                if (unit.limit_concurrent > 0 || unit.limit_lifetime > 0)
                {
                    if (globals.UnitFilterConfig.limitMode == 2) return false;
                }
                else
                {
                    if (globals.UnitFilterConfig.limitMode == 1) return false;
                }
            }

            //todo fighters

            if (SpaceRadioButton.Checked) //This should not be relelvant to other settings
            {
                int level = globals.UnitFilterConfig.shipyardLevel;
                if (level < 0) level = 0;
                if (globals.UnitFilterConfig.shipyardComparison == 0)
                {
                    if (unit.level < level) return false;
                }
                else if (globals.UnitFilterConfig.shipyardComparison == 1)
                {
                    if (unit.level != level) return false;
                }
                else if (globals.UnitFilterConfig.shipyardComparison == 2)
                {
                    if (unit.level > level) return false;
                }
            }

            return true;
        }

        private string getBuildingAffils(unit Building)
        {
            if (!Building.unitname.Contains("_HQ") && Building.affiliations.Count > 0 && Building.affiliations.Count <= 2 && Building.affiliations[0] != "Neutral")
            {
                Building.username += " (" + Building.affiliations[0];
                for (int j = 1; j < Building.affiliations.Count; j++) Building.username += ", " + Building.affiliations[j];
                Building.username += ")";
            }
            return Building.username;
        }
        private void populateUnitListbox()
        {
            string save = "";
            if (UnitListBox.SelectedItems.Count > 0) save = ((unit)UnitListBox.SelectedItem).unitname;
            UnitListBox.Items.Clear();
            string search = UnitSearchTextBox.Text;
            List<unit> units;
            List<unit> sorted = new List<unit>();

            if (SpaceRadioButton.Checked) units = entities.spaceUnits;
            else if (UnitRadioButton.Checked) units = entities.groundUnits;
            else if (FighterRadioButton.Checked) units = entities.fighters;
            else if (SpaceHeroRadioButton.Checked) units = entities.spaceHeroes;
            else if (HeroCompaniesRadioButton.Checked) units = entities.heroCompanies;
            else if (GroundHeroRadioButton.Checked) units = entities.groundHeroes;
            else if (StructureRadioButton.Checked) units = entities.structures;
            else if (SpaceStructureRadioButton.Checked) units = entities.spaceStructures;
            else units = entities.groundCompanies;

            for (int i = 0; i< units.Count; i++)
            {
                unit unit = units[i];
                if (unit.variantbase != "Infantry_Dummy_Template" && unit.unitname != "Infantry_Dummy_Template" && !unit.unitname.Contains("Template_") && !unit.unitname.Contains("_Captured") && (!StructureRadioButton.Checked || !SpaceStructureRadioButton.Checked || (unit.hp > 1 && !unit.flags.Contains("NotOpportunityTarget"))) )
                {
                    if (filterUnit(unit) && (search == "" || (unit.username).ToLower().Contains(search.ToLower())))
                    {
                        if (StructureRadioButton.Checked || SpaceStructureRadioButton.Checked)
                        {
                            unit.username = getBuildingAffils(unit);
                        }
                        sorted.Add(sortUnit(unit));
                    }
                }
            }

            //sorted.OrderBy(s1 => s1.armor_type).ThenBy(s1 => s1.username);
            if (globals.UnitSortConfig.SortType <= UnitSortTypes.Name)
            {
                if (globals.UnitSortConfig.Descending)
                {
                    sorted.Sort(delegate (unit b, unit a)
                    {
                        int xdiff = a.sortstring.CompareTo(b.sortstring);
                        if (xdiff != 0) return xdiff;
                        else return a.username.CompareTo(b.username);
                    });
                }
                else
                {
                    sorted.Sort(delegate (unit a, unit b)
                    {
                        int xdiff = a.sortstring.CompareTo(b.sortstring);
                        if (xdiff != 0) return xdiff;
                        else return a.username.CompareTo(b.username);
                    });
                }
            }
            else
            {
                if (globals.UnitSortConfig.Descending)//todo numerator functions
                {
                    sorted.Sort(delegate (unit b, unit a)
                    {
                        int xdiff = a.sortfloat.CompareTo(b.sortfloat);
                        if (xdiff != 0) return xdiff;
                        else return a.username.CompareTo(b.username);
                    });
                }
                else
                {
                    sorted.Sort(delegate (unit a, unit b)
                    {
                        int xdiff = a.sortfloat.CompareTo(b.sortfloat);
                        if (xdiff != 0) return xdiff;
                        else return a.username.CompareTo(b.username);
                    });
                }
            }

            foreach (unit unit in sorted)
            {
                UnitListBox.Items.Add(unit);
                if (unit.unitname == save) UnitListBox.SelectedItem = unit;
            }
        }

        private void ArmorBoxTypeFill(bool space, bool shield)
        {
            ComboBox box = TargetArmorBox;
            if (shield) box = TargetShieldBox;
            box.Items.Clear();
            List<string> src = entities.AllArmors;
            if (space)
            {
                if (shield)
                {
                    if (entities.SpaceShields.Count > 0) src = entities.SpaceShields;
                }
                else
                {
                    if (entities.SpaceArmors.Count > 0) src = entities.SpaceArmors;
                } 
            }
            else
            {
                if (shield)
                {
                    if (entities.GroundShields.Count > 0) src = entities.GroundShields;
                }
                else
                {
                    if (entities.GroundArmors.Count > 0) src = entities.GroundArmors;
                }
            }
            foreach (string armor in src) box.Items.Add(armor);
            if(box.Items.Count > 0) box.SelectedIndex = 0;
        }

        private void CategoryBoxTypeFill(bool space)
        {
            TargetCategoryBox.Items.Clear();
            List<string> src = entities.AllCategories;
            if (space) {
                if (entities.SpaceCategories.Count > 0) src = entities.SpaceCategories;
            }
            else
            {
                if (entities.GroundCategories.Count > 0) src = entities.GroundCategories;
            }
            foreach (string armor in src) TargetCategoryBox.Items.Add(armor);
            if (TargetCategoryBox.Items.Count > 0) TargetCategoryBox.SelectedIndex = 0;
        }

        private void IncomingBoxTypeFill(bool space)
        {
            IncomingDamageBox.Items.Clear();
            IncomingDamageBox.Tag = space;
            List<string> src = entities.DamageTypes;
            if (space)
            {
                if (entities.SpaceDamageTypes.Count > 0) src = entities.SpaceDamageTypes;
            }
            else
            {
                if (entities.GroundDamageTypes.Count > 0) src = entities.GroundDamageTypes;
            }
            foreach (string weapon in src) IncomingDamageBox.Items.Add(weapon);
            if (IncomingDamageBox.Items.Count > 0) IncomingDamageBox.SelectedIndex = 0;
        }

        private void handleUnitCheckboxTick(int mode)
        {
            bool buildable = mode <= 1;
            bool tacticalunit = mode != 1;
            bool groundstuff = mode >= 1;
            bool spaceonly = mode == 0 || mode == 3 || mode == 4;
            bool companyonly = mode == 1;
            bool groundunitonly = mode == 2;

            ArmorBoxTypeFill(spaceonly, true);
            ArmorBoxTypeFill(spaceonly, false);
            CategoryBoxTypeFill(spaceonly);
            IncomingBoxTypeFill(spaceonly);
            if (companyonly) StatDisclaimerLabel.Text = "Stats represent the aggregate for the company, not individual units";
            else StatDisclaimerLabel.Text = "Stats parsed from XML data. Please report any that differ from the description";
        }

        private void SpaceRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            handleUnitCheckboxTick(0);
            populateUnitListbox();
        }

        private void GroundRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            handleUnitCheckboxTick(1);
            populateUnitListbox();
        }

        private void UnitRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            handleUnitCheckboxTick(2);
            populateUnitListbox();
        }

        private void FighterRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            handleUnitCheckboxTick(3);
            populateUnitListbox();
        }

        private void SpaceHeroRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            handleUnitCheckboxTick(4);
            populateUnitListbox();
        }

        private void HeroCompaniesRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            handleUnitCheckboxTick(5);
            populateUnitListbox();
        }

        private void GroundHeroRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            handleUnitCheckboxTick(6);
            populateUnitListbox();
        }

        private void StructureRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            handleUnitCheckboxTick(7);
            populateUnitListbox();
        }

        private void SpaceStructureRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            handleUnitCheckboxTick(8);
            populateUnitListbox();
        }

        private void UnitSearchTextBox_TextChanged(object sender, EventArgs e)
        {
            populateUnitListbox();
        }

        private float getDPS(hardpoint hardpoint, bool suppressQty = false) //todo: extra param to force alpha/not for cp calcs, be able to incorporate selected ability values 
        {
            float reload = hardpoint.recharge + (hardpoint.pulseCount - 1) * hardpoint.pulseDelay;
            if (AlphaCheckBox.Checked) reload = (float)Math.Log10(reload);
            float corenne = hardpoint.damageAmount * hardpoint.pulseCount / reload;
            if (!suppressQty) corenne *= hardpoint.quantity;
            return corenne;
        }

        private string getDPSString(hardpoint hardpoint, bool suppressQty = false)
        {
            return getDPS(hardpoint, suppressQty).ToString(globals.dpsformat);
        }

        private float DPSOnTarget(unit unit, bool shield)
        {
            float dps = 0;
            foreach (hardpoint hp in unit.consolidatedhps)
            {
                if(hp.range >= (float)TargetRangeBox.SelectedItem)
                {
                    WeaponMods weap = GetWeaponMods(hp.damageType);
                    string type = TargetArmorBox.Text;
                    if (shield) type = TargetShieldBox.Text;
                    float mult = getWeapMultiplier(type, weap, shield);
                    dps += getDPS(hp) * mult;
                }
            }
            return dps;
        }
        private float GetHPAccuracyMod(hardpoint hp)
        {
            for(int i = 0; i < hp.inaccuracyTypes.Count; i++)
            {
                if(hp.inaccuracyTypes[i] == TargetCategoryBox.Text || hp.inaccuracyTypes[i] == "All")
                {
                    return (100 - hp.inaccuracyAmounts[i]) / 100;
                }
            }
            return 1;
        }

        private void setTargetDPS()
        {
            if(UnitListBox.SelectedItems.Count > 0 && TargetRangeBox.SelectedIndex >= 0)
            {
                unit selected = (unit)UnitListBox.SelectedItem;

                float armordps = 0;
                float shielddps = 0;
                float avgdps = 0;
                float acc_armordps = 0;
                float acc_shielddps = 0;
                float acc_avgdps = 0;
                foreach (hardpoint hp in selected.consolidatedhps)
                {
                    if (hp.range >= (float)TargetRangeBox.SelectedItem)
                    {
                        float accmod = GetHPAccuracyMod(hp);
                        WeaponMods weap = GetWeaponMods(hp.damageType);
                        float mult = getWeapMultiplier(TargetArmorBox.Text, weap, false);
                        float dps = getDPS(hp);
                        dps *= (float)UADamageLabel.Tag / (float)UAReloadLabel.Tag;
                        if (FullSalvoOn()) dps /= hp.fullsalvomod;
                        armordps += dps * mult;
                        acc_armordps += dps * mult * accmod;
                        mult = getWeapMultiplier(TargetShieldBox.Text, weap, true);
                        shielddps += dps * mult;
                        acc_shielddps += dps * mult * accmod;
                        avgdps += dps * weap.median;
                        acc_avgdps += dps * weap.median * accmod;
                    }
                }
                TargetArmorLabel.Text = "DPS on Target: " + armordps.ToString(globals.dpsformat);
                TargetShieldLabel.Text = "DPS on Target: " + shielddps.ToString(globals.dpsformat);
                TargetAvgLabel.Text = "Average at range: " + avgdps.ToString(globals.dpsformat);
                TargetAccuracyLabel.Text = "Approximate accuracy modifiers\nDPS on Target Armor: " + acc_armordps.ToString(globals.dpsformat) + "\nDPS on Target Shield: " + acc_shielddps.ToString(globals.dpsformat) + "\nAverage on Target: " + acc_avgdps.ToString(globals.dpsformat);
            }
        }

        private void UnitHPListbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (UnitHPListbox.SelectedItems.Count > 0)
            {
                hardpoint selected = (hardpoint)UnitHPListbox.SelectedItem;
                HPDetailLabel.Text = "HP Type: " + selected.hpType;
                if (selected.damageAmount > 0)
                {
                    float mult = GetWeaponMods(selected.damageType).median;
                    HPDetailLabel.Text += "\nShot Damage: " + selected.damageAmount * (float)UADamageLabel.Tag + ";  Average Applied: " + (selected.damageAmount * mult).ToString(globals.dpsformat);
                    try //Just in case
                    {
                        float dps = getDPS(selected, true) * (float)UADamageLabel.Tag / (float)UAReloadLabel.Tag;
                        if (FullSalvoOn()) dps /= selected.fullsalvomod;
                        HPDetailLabel.Text += "\nDamage per second: " + dps.ToString(globals.dpsformat) + ";  Average Applied: " + (dps * mult).ToString(globals.dpsformat);
                    }
                    catch { };
                    HPDetailLabel.Text += "\nDamage Type: " + selected.damageType;
                }
                if (selected.pulseDelay > 0) HPDetailLabel.Text += "\nPulse Delay: " + selected.pulseDelay;
                if (selected.fullsalvomod != 1) HPDetailLabel.Text += " Full Salvo modifier: " + selected.fullsalvomod;
                if (selected.blastRadius > 0) HPDetailLabel.Text += "\nBlast Radius: " + selected.blastRadius;
                if (selected.targetable && selected.hp > 0) HPDetailLabel.Text += "\nHealth: " + selected.hp;
                else HPDetailLabel.Text += "\nNontargetable";

                HPAccuracyLabel.Text = "Hardpoint Accuracy Modifiers:";
                for (int i = 0; i < selected.inaccuracyTypes.Count; i++)
                {
                    HPAccuracyLabel.Text += "\n" + selected.inaccuracyTypes[i] + ": " + selected.inaccuracyAmounts[i].ToString() + "%";
                }
            }
            else
            {
                HPDetailLabel.Text = "HP Type:";
                HPAccuracyLabel.Text = "Hardpoint Accuracy Modifiers:";
            }
        }

        private void TargetArmorBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            setTargetDPS();
            if (globals.UnitSortConfig.SortType == UnitSortTypes.dpsArmor) populateUnitListbox();
        }

        private void TargetShieldBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            setTargetDPS();
            if (globals.UnitSortConfig.SortType == UnitSortTypes.dpsShield) populateUnitListbox();
        }

        private void TargetRangeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            setTargetDPS();
        }

        private void TargetCategoryBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            setTargetDPS();

            UnitSortTypes[] redo = { UnitSortTypes.dpsRaw, UnitSortTypes.dpsAvg, UnitSortTypes.dpsArmor, UnitSortTypes.dpsShield };
            if (globals.UnitSortConfig.Accuracy && redo.Contains(globals.UnitSortConfig.SortType)) populateUnitListbox();
        }

        private void AlphaCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            setTargetDPS();
            if(UnitListBox.SelectedItems.Count > 0) setDPSBreakdown();
            UnitHPListbox_SelectedIndexChanged(UnitHPListbox.SelectedItem, e);
        }

        private void IncomingDamageBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (UnitListBox.SelectedItems.Count > 0)
            {
                unit selected = (unit)UnitListBox.SelectedItem;
                IncomingDamageLabel.Text = "Effective values against damage type:";
                bool space = (bool)IncomingDamageBox.Tag;

                WeaponMods weaps = GetWeaponMods(IncomingDamageBox.Text);
                if (selected.armor_type != "")
                {
                    float mult = getWeapMultiplier(selected.armor_type, weaps, false);
                    IncomingDamageLabel.Text += "\nModified Health: " + (selected.hp / mult).ToString("0");
                }
                if (selected.shield_type != "")
                {
                    float mult = getWeapMultiplier(selected.shield_type, weaps, true);
                    IncomingDamageLabel.Text += "\nModified Shields: " + (selected.shield / mult).ToString("0");
                }
            }

            UnitSortTypes[] redo = { UnitSortTypes.Durability, UnitSortTypes.HP, UnitSortTypes.Shield, UnitSortTypes.Regen };
            if (globals.UnitSortConfig.SortType == UnitSortTypes.dpsRaw || globals.UnitSortConfig.DurabilityMode == 3 && redo.Contains(globals.UnitSortConfig.SortType)) populateUnitListbox();
        }

        enum UnitPanels {
            TextPanel,
            AvailPanel,
            StatPanel,
            SubunitPanel,
            AbilityPanel,
            BTSPanel
        }

        void setExpandedButton(Button button)
        {
            button.Width = 24;
            button.Text = "/\\"; //don't ever let them tell you ASCII art is dead
            button.TextAlign = ContentAlignment.MiddleCenter;
        }

        void setCollapsedButton(Button button, string label)
        {
            button.Width = 90;
            button.Text = "\\/ " + label;
            button.TextAlign = ContentAlignment.MiddleLeft;
        }

        private void collapsePanels(UnitPanels toggleID)
        {
            int TextSize = 0;
            int.TryParse(UnitTextPanel.Tag.ToString(), out TextSize);

            if(TextSize == 0) //init time, save all the sizes
            {
                UnitTextPanel.Tag = UnitTextPanel.Height;
                TextSize = UnitTextPanel.Height;
                //The first one never changes Y values
                UnitAvailPanel.Tag = UnitAvailPanel.Height;
                CollapseUnitAvailPanel.Tag = UnitAvailPanel.Location.Y - UnitTextPanel.Location.Y - UnitTextPanel.Height;
                UnitStatPanel.Tag = UnitStatPanel.Height;
                CollapseUnitStatPanel.Tag = UnitStatPanel.Location.Y - UnitAvailPanel.Location.Y - UnitAvailPanel.Height;
                UnitSubunitPanel.Tag = UnitSubunitPanel.Height;
                CollapseUnitSubunitPanel.Tag = UnitSubunitPanel.Location.Y - UnitStatPanel.Location.Y - UnitStatPanel.Height;
                UnitAbilityPanel.Tag = UnitAbilityPanel.Height;
                CollapseUnitAbilityPanel.Tag = UnitAbilityPanel.Location.Y - UnitSubunitPanel.Location.Y - UnitSubunitPanel.Height;
                UnitBTSPanel.Tag = UnitBTSPanel.Location.Y - UnitAbilityPanel.Location.Y - UnitAbilityPanel.Height; //Last is also a special case//UnitBTSPanel.Height;
            }

            int StatSize = (int)UnitStatPanel.Tag;
            int StatInterval = (int)CollapseUnitStatPanel.Tag;
            int AvailSize = (int)UnitAvailPanel.Tag;
            int AvailInterval = (int)CollapseUnitAvailPanel.Tag;
            int SubunitSize = (int)UnitSubunitPanel.Tag;
            int SubunitInterval = (int)CollapseUnitSubunitPanel.Tag;
            int AbilitySize = (int)UnitAbilityPanel.Tag;
            int AbilityInterval = (int)CollapseUnitAbilityPanel.Tag;
            int BTSInterval = (int)UnitBTSPanel.Tag;

            switch (toggleID) //toggle panel and associated buttons
            {
                case UnitPanels.TextPanel:
                    if(UnitTextPanel.Height == 0)
                    {
                        UnitTextPanel.Height = TextSize;
                        setExpandedButton(CollapseUnitTextPanel);
                    }
                    else
                    {
                        UnitTextPanel.Height = 0;
                        setCollapsedButton(CollapseUnitTextPanel, "Unit Card");
                    }
                    break;
                case UnitPanels.AvailPanel:
                    if (UnitAvailPanel.Height == 0)
                    {
                        UnitAvailPanel.Height = AvailSize;
                        setExpandedButton(CollapseUnitAvailPanel);
                    }
                    else
                    {
                        UnitAvailPanel.Height = 0;
                        setCollapsedButton(CollapseUnitAvailPanel, "Availability");
                    }
                    break;
                case UnitPanels.StatPanel:
                    if (UnitStatPanel.Height == 0)
                    {
                        UnitStatPanel.Height = StatSize;
                        setExpandedButton(CollapseUnitStatPanel);
                    }
                    else
                    {
                        UnitStatPanel.Height = 0;
                        setCollapsedButton(CollapseUnitStatPanel, "Stats");
                    }
                    break;
                case UnitPanels.SubunitPanel:
                    if (UnitSubunitPanel.Height == 0)
                    {
                        UnitSubunitPanel.Height = SubunitSize;
                        setExpandedButton(CollapseUnitSubunitPanel);
                    }
                    else
                    {
                        UnitSubunitPanel.Height = 0;
                        setCollapsedButton(CollapseUnitSubunitPanel, "Subunits");
                    }
                    break;
                case UnitPanels.AbilityPanel:
                    if (UnitAbilityPanel.Height == 0)
                    {
                        UnitAbilityPanel.Height = AbilitySize;
                        setExpandedButton(CollapseUnitAbilityPanel);
                    }
                    else
                    {
                        UnitAbilityPanel.Height = 0;
                        setCollapsedButton(CollapseUnitAbilityPanel, "Abilities");
                    }
                    break;
                default:
                    break;
            }

            //redraw
            int Yvalue = UnitTextPanel.Location.Y + Math.Max(UnitTextPanel.Height, CollapseUnitAvailPanel.Height) + AvailInterval;
            UnitAvailPanel.Location = new Point(UnitAvailPanel.Location.X, Yvalue);
            CollapseUnitAvailPanel.Location = new Point(CollapseUnitAvailPanel.Location.X, Yvalue);

            Yvalue = UnitAvailPanel.Location.Y + Math.Max(UnitAvailPanel.Height, CollapseUnitStatPanel.Height) + StatInterval;
            UnitStatPanel.Location = new Point(UnitStatPanel.Location.X, Yvalue);
            CollapseUnitStatPanel.Location = new Point(CollapseUnitStatPanel.Location.X, Yvalue);

            Yvalue = UnitStatPanel.Location.Y + Math.Max(UnitStatPanel.Height, CollapseUnitStatPanel.Height) + SubunitInterval;
            UnitSubunitPanel.Location = new Point(UnitSubunitPanel.Location.X, Yvalue);
            CollapseUnitSubunitPanel.Location = new Point(CollapseUnitSubunitPanel.Location.X, Yvalue);

            Yvalue = UnitSubunitPanel.Location.Y + Math.Max(UnitSubunitPanel.Height, CollapseUnitSubunitPanel.Height) + AbilityInterval;
            UnitAbilityPanel.Location = new Point(UnitAbilityPanel.Location.X, Yvalue);
            CollapseUnitAbilityPanel.Location = new Point(CollapseUnitAbilityPanel.Location.X, Yvalue);

            Yvalue = UnitAbilityPanel.Location.Y + Math.Max(UnitAbilityPanel.Height, CollapseUnitAbilityPanel.Height) + BTSInterval;
            UnitBTSPanel.Location = new Point(UnitBTSPanel.Location.X, Yvalue);
        }

        private void CollapseUnitTextPanel_Click(object sender, EventArgs e)
        {
            collapsePanels(UnitPanels.TextPanel);
        }

        private void CollapseUnitAvailPanel_Click(object sender, EventArgs e)
        {
            collapsePanels(UnitPanels.AvailPanel);
        }

        private void CollapseUnitStatPanel_Click(object sender, EventArgs e)
        {
            collapsePanels(UnitPanels.StatPanel);
        }

        private void CollapseUnitSubunitPanel_Click(object sender, EventArgs e)
        {
            collapsePanels(UnitPanels.SubunitPanel);
        }

        private void CollapseUnitAbilityPanel_Click(object sender, EventArgs e)
        {
            collapsePanels(UnitPanels.AbilityPanel);
        }

        private void UnitSubunitGotoButton_Click(object sender, EventArgs e)
        {
            //todo change the second argument to go to fighters and fighter squadrons appropriately
            if (UnitSubunitListbox.SelectedItems.Count > 0)
            {
                int subtype = 2;
                if (SpaceRadioButton.Checked) subtype = 0;
                else if (SpaceHeroRadioButton.Checked) subtype = 3; //Todo needs to handle unit and heroes
                else if (HeroCompaniesRadioButton.Checked) subtype = 4;
                insert_history((int)historymaintabs.unit, subtype, ((quantizedObject)UnitSubunitListbox.SelectedItem).codename, true);
            }
        }

        private void UnitSubsquadGotoButton_Click(object sender, EventArgs e)
        {
            if (UnitSubSquadListbox.SelectedItems.Count > 0)
            {
                int subtype = 1;
                if (HeroCompaniesRadioButton.Checked) subtype = 4;
                insert_history((int)historymaintabs.unit, subtype, ((quantizedObject)UnitSubSquadListbox.SelectedItem).codename, true);
            }

        }

        private void UnitSortButton_Click(object sender, EventArgs e)
        {
            UnitSort sort = new UnitSort();
            sort.sortConfig = globals.UnitSortConfig;
            if (SpaceRadioButton.Checked) sort.UnitRBtype = 0;
            else if (GroundRadioButton.Checked) sort.UnitRBtype = 1;
            else if (UnitRadioButton.Checked) sort.UnitRBtype = 2;
            else if (FighterRadioButton.Checked) sort.UnitRBtype = 3;
            else if (SpaceHeroRadioButton.Checked) sort.UnitRBtype = 4;
            else if (HeroCompaniesRadioButton.Checked) sort.UnitRBtype = 5;
            else if (GroundHeroRadioButton.Checked) sort.UnitRBtype = 6;
            else if (StructureRadioButton.Checked) sort.UnitRBtype = 7;
            else if (SpaceStructureRadioButton.Checked) sort.UnitRBtype = 8;
            //todo add the others
            sort.ShowDialog();

            if (!sort.cancel)
            {
                globals.UnitSortConfig = sort.sortConfig;
                UnitSortTypeLabel.Text = sort.sortDocumentation;

                populateUnitListbox();
            }
        }

        private void UnitFilterButton_Click(object sender, EventArgs e)
        {
            UnitFilter filter = new UnitFilter();
            filter.filterConfig = globals.UnitFilterConfig;
            filter.factions = globals.factions;
            filter.flags = entities.AllFlags;
            if (SpaceRadioButton.Checked) filter.UnitRBtype = 0;
            else if (GroundRadioButton.Checked) filter.UnitRBtype = 1;
            else if (UnitRadioButton.Checked) filter.UnitRBtype = 2;
            else if (FighterRadioButton.Checked) filter.UnitRBtype = 3;
            else if (SpaceHeroRadioButton.Checked) filter.UnitRBtype = 4;
            else if (HeroCompaniesRadioButton.Checked) filter.UnitRBtype = 5;
            else if (GroundHeroRadioButton.Checked) filter.UnitRBtype = 6;
            else if (StructureRadioButton.Checked) filter.UnitRBtype = 7;
            else if (SpaceStructureRadioButton.Checked) filter.UnitRBtype = 8;
            filter.categories = entities.AllCategories;
            filter.atypes = entities.AllArmors;
            filter.stypes = entities.AllArmors;
            if (SpaceRadioButton.Checked)
            {
                if (entities.SpaceCategories.Count > 0) filter.categories = entities.SpaceCategories;
                if (entities.SpaceArmors.Count > 0) filter.atypes = entities.SpaceArmors;
                if (entities.SpaceShields.Count > 0) filter.stypes = entities.SpaceShields;
            }
            else if (filter.UnitRBtype > 0)
            {
                if (entities.GroundCategories.Count > 0) filter.categories = entities.GroundCategories;
                if (entities.GroundArmors.Count > 0) filter.atypes = entities.GroundArmors;
                if (entities.GroundShields.Count > 0) filter.stypes = entities.GroundShields;
            }

            //todo add the others
            filter.ShowDialog();

            if (!filter.cancel)
            {
                globals.UnitFilterConfig = filter.filterConfig;
                UnitFIlterTypeLabel.Text = filter.filterDocumentation;

                populateUnitListbox();
            }
        }

        private void readErrorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Clipboard.SetText(entities.readerrors);
            if (entities.readerrors == "") MessageBox.Show("No errors detectable by Holocron found");
            else MessageBox.Show("Errors copied to clipboard" + entities.readerrors);
        }

        private void GoToReqStructButton_Click(object sender, EventArgs e)
        {
            if (ReqStructuresListBox.SelectedItems.Count > 0)
            {
                if (GroundRadioButton.Checked || HeroCompaniesRadioButton.Checked) insert_history((int)historymaintabs.unit, 7, ((unit)ReqStructuresListBox.SelectedItem).unitname, true);
                else insert_history((int)historymaintabs.unit, 8, ((unit)ReqStructuresListBox.SelectedItem).unitname, true);
            }
        }

        private void FactionAvailableListbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (FactionAvailableListbox.SelectedItems.Count > 0)
            {
                unit unit = (unit)UnitListBox.Tag;
                if (SpaceRadioButton.Checked || GroundRadioButton.Checked)
                {
                    AvailabilityLabel.Text = checkUnitAvailibility(unit, (holo_faction)FactionAvailableListbox.SelectedItem);
                }
                else if (SpaceHeroRadioButton.Checked || HeroCompaniesRadioButton.Checked)
                {
                    AvailabilityLabel.Text = ""; //todo lots of script parsing
                }
                else AvailabilityLabel.Text = ""; //Structures could get something?
            }
            else AvailabilityLabel.Text = "";
        }

        private string checkUnitAvailibility(unit unit, holo_faction faction)
        {
            string corenne = "";
            if (unit.fightermode > 0) return corenne;
            if (unit.techlevel > 5) corenne = "Locked by tech level"; //todo add locked by req structures w/o affil (TR Hutt Keldabe)
            else
            {
                corenne = "Unlocks: ";
                if (unit.locked < 1) corenne += "Default";

                corenne += "\nLocks: ";
                if (unit.locked == 1) corenne += "Default";
            }

            string missionfile = getModFile("Scripts\\Library\\eawx-plugins\\intervention-missions\\rewards\\RewardTables_" + faction.codename.ToUpper() + ".lua");
            if (File.Exists(missionfile))
            {
                string filetext = File.ReadAllText(missionfile);
                if (filetext.Contains("\"" + unit.unitname + "\"")) corenne += "\nMission Reward";
            }
            return corenne;
        }

        private void UnitAbilityListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(UnitAbilityListBox.SelectedItems.Count > 0)
            {
                unitability able = (unitability)UnitAbilityListBox.SelectedItem;
                AbilityPictureBox.Image = new Bitmap(IconPictureBox.Width, IconPictureBox.Height);
                IconData icondata = DatParser.GetIconData(able.icon, entities);
                if (icondata.size_x > 0 && entities.MTmaster != null)
                {
                    // Create a Graphics object to do the drawing, *with the new bitmap as the target*
                    using (Graphics g = Graphics.FromImage(AbilityPictureBox.Image))
                    {
                        g.DrawImage(entities.MTmaster, 0, 0, new Rectangle(icondata.origin_x, icondata.origin_y, icondata.size_x, icondata.size_y), GraphicsUnit.Pixel);
                    }
                }
                UnitAbilityNameLabel.Text = able.username;
                UnitAbilityDescLabel.Text = Find_Text_Entry(able.desc);

                UATimeLabel.Text = "";
                if (able.expiration > 0) UATimeLabel.Text = "Duration: " + able.expiration.ToString("0")+" ";
                if (able.recharge > 0) UATimeLabel.Text = "Recharge: " + able.recharge.ToString("0");
                if (able.damageMod != 1) UADamageLabel.Text = "Damage Modifer: " + able.damageMod.ToString("0.###");
                else UADamageLabel.Text = "";
                if (able.defenseMod != 1) UADefenseLabel.Text = "Defense Modifer: " + able.defenseMod.ToString("0.###");
                else UADefenseLabel.Text = "";
                if (able.reloadMod != 1) UAReloadLabel.Text = "Reload Modifer: " + able.reloadMod.ToString("0.###");
                else UAReloadLabel.Text = "";
                if (able.shieldMod != 1) UAShieldLabel.Text = "Shield Modifer: " + able.shieldMod.ToString("0.###");
                else UAShieldLabel.Text = "";
                if (able.speedMod != 1) UASpeedLabel.Text = "Speed Modifer: " + able.speedMod.ToString("0.###");
                else UASpeedLabel.Text = "";
                UAStimLabel.Text = "";
                if (able.selfdamage > 0) UAStimLabel.Text = "Damage on use: " + able.selfdamage.ToString("0")+ "% ";
                else UAStimLabel.Text = "";
                if (able.radius > 0) UARadiusLabel.Text = "Radius: " + able.radius.ToString("0");
                else UARadiusLabel.Text = "";
                UADamageLabel.Tag = able.damageMod;
                UADefenseLabel.Tag = able.defenseMod;
                UAReloadLabel.Tag = able.reloadMod;
                UAShieldLabel.Tag = able.shieldMod;
                UASpeedLabel.Tag = able.speedMod;
                setDPSBreakdown();
                setTargetDPS();
                UnitHPListbox_SelectedIndexChanged(UnitHPListbox, new EventArgs());
                setAbilityDependentStats();

                if (able.ability != "")
                {
                    foreach(ability ability in AbilityListBox.Items)
                    {
                        if (ability.name == able.ability)
                        {
                            AbilityListBox.SelectedItem = ability;
                            break;
                        }    
                    }
                }

            }
        }

        private void AbilityListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AbilityListBox.SelectedItems.Count > 0)
            {
                ability able = (ability)AbilityListBox.SelectedItem;
                AbilityTypeLabel.Text = "Type: " + able.type;
                AbilityActivationLabel.Text = "Activation: " + able.activation;
                if(able.activation == "User_Input")
                {
                    foreach (unitability ability in UnitAbilityListBox.Items)
                    {
                        if (ability.ability == able.name) UnitAbilityListBox.SelectedItem = ability;
                        break;
                    }
                }
                if (able.applicable_categories.Length > 0) AbilityTargetTypeLabel.Text = "Target Categories: " + SerializeStringArray(able.applicable_categories);
                else AbilityTargetTypeLabel.Text = "";
                if (able.excluded_types.Length > 0) AbilityExcludedUnitLabel.Text = "Excluded Targets: " + SerializeStringArray(able.excluded_types);
                else AbilityExcludedUnitLabel.Text = "";
                if (able.applicable_types.Length > 0) AbilityTargetUnitLabel.Text = "Target Units:" + SerializeStringArray(able.applicable_types);
                else AbilityTargetUnitLabel.Text = "";

                if(able.type == "Combat_Bonus_Ability")
                {//Repurpose labels entirely
                    if (able.damageBonus != 0) AbilityTimeLabel.Text = "Damage bonus: " + able.damageBonus;
                    else AbilityTimeLabel.Text = "";
                    if (able.defenseBonus != 0) AbilityValueLabel.Text = "Defense bonus: " + able.defenseBonus;
                    else AbilityValueLabel.Text = "";
                    if (able.speedBonus != 0) AbilityActivationRadiusLabel.Text = "Speed bonus: " + able.speedBonus;
                    else AbilityActivationRadiusLabel.Text = "";
                    if (able.shieldBonus != 0) AbilityRadiusLabel.Text = "Shield bonus: " + able.shieldBonus;
                    else AbilityRadiusLabel.Text = "";
                    if (able.healthBonus != 0) AbilityLinkedLabel.Text = "Health bonus: " + able.healthBonus;
                    else AbilityLinkedLabel.Text = "";
                }
                else
                {//Todo rename some of these to be more specific based on type
                    AbilityTimeLabel.Text = "";
                    if (able.duration > 0) AbilityTimeLabel.Text = "Duration: " + able.duration.ToString("0") + " ";
                    if (able.recharge > 0) AbilityTimeLabel.Text = "Recharge: " + able.recharge.ToString("0");
                    if (able.genericValue > 0) AbilityValueLabel.Text = "Value: " + able.genericValue;
                    else AbilityValueLabel.Text = "";
                    AbilityActivationRadiusLabel.Text = "";
                    if (able.minradius > 0) AbilityActivationRadiusLabel.Text = "Min Activation: " + able.minradius + " ";
                    if (able.maxradius > 0) AbilityActivationRadiusLabel.Text = "Max Activation: " + able.maxradius;
                    if(able.type == "Force_Healing_Ability" && !able.genericBool) AbilityActivationRadiusLabel.Text = "Heals all units in radius";
                    if (able.radius > 0) AbilityRadiusLabel.Text = "Radius: " + able.radius;
                    else AbilityRadiusLabel.Text = "";
                    if (able.linkedEntity != "") AbilityLinkedLabel.Text = "Linked Object: " + able.linkedEntity;
                    else AbilityLinkedLabel.Text = "";
                }
                if (able.stacking >= 0) AbilityStackingLabel.Text = "Stacking Category: " + able.stacking;
                else AbilityStackingLabel.Text = "";
            }
        }

        private void ResetAbilitySelection()
        {
            AbilityPictureBox.Image = new Bitmap(IconPictureBox.Width, IconPictureBox.Height);
            UnitAbilityNameLabel.Text = "";
            UnitAbilityDescLabel.Text = "";
            UADamageLabel.Text = "";
            UADefenseLabel.Text = "";
            UAReloadLabel.Text = "";
            UAShieldLabel.Text = "";
            UASpeedLabel.Text = "";
            UADamageLabel.Tag = (float)1.0; //Casting a float to a double is highly verboten, as it turns out
            UADefenseLabel.Tag = (float)1.0;
            UAReloadLabel.Tag = (float)1.0;
            UAShieldLabel.Tag = (float)1.0;
            UASpeedLabel.Tag = (float)1.0;

            AbilityTypeLabel.Text = "";
            AbilityActivationLabel.Text = "";
            AbilityTargetTypeLabel.Text = "";
            AbilityTargetUnitLabel.Text = "";
            AbilityExcludedUnitLabel.Text = "";
            AbilityTimeLabel.Text = "";
            AbilityValueLabel.Text = "";
            AbilityActivationRadiusLabel.Text = "";
            AbilityRadiusLabel.Text = "";
            AbilityLinkedLabel.Text = "";
            AbilityStackingLabel.Text = "";
        }

        private void ClearAbilityButton_Click(object sender, EventArgs e)
        {
            UnitAbilityListBox.SelectedItems.Clear();
            AbilityListBox.SelectedItems.Clear();
            ResetAbilitySelection();
            setDPSBreakdown();
            setTargetDPS();
            UnitHPListbox_SelectedIndexChanged(UnitHPListbox, new EventArgs());
            setAbilityDependentStats();
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog fil = new SaveFileDialog();
            fil.Filter = ("Text Files (*.txt)|*.txt|All files (*.*)|*.*");
            fil.Title = "Export Unit list";
            if (fil.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter filewrite = new StreamWriter(fil.FileName))
                {
                    foreach (unit unit in UnitListBox.Items) filewrite.WriteLine(unit);
                }
            }
            MessageBox.Show("Unit list saved to file");

        }

        private void PlanetListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(PlanetListBox.SelectedItems.Count > 0)
            {
                planet planet = (planet)PlanetListBox.SelectedItem;
                PlanetListBox.Tag = planet;
                insert_history((int)historymaintabs.planet, 0, planet.codename);
                PlanetNameLabel.Text = planet.username;
                PlanetCodeLabel.Text = "Internal Name: " + planet.codename;
                PlanetHistoryTextBox.Text = "Population: " + Find_Text_Entry(planet.desc_pop).Replace("\\n", "\n") + "\n\n" + "Fauna: " + Find_Text_Entry(planet.desc_fauna) + "\n\n" + Find_Text_Entry(planet.desc_history);
                PlanetCreditLabel.Text = "Income: " + planet.credits.ToString();
                PlanetShipyardLabel.Text = "Shipyard: " + planet.shipyard.ToString();
                PlanetStarbaseLabel.Text = "Starbase: " + planet.max_starbase.ToString();
                PlanetGroundSlotsLabel.Text = "Ground Slots: " + planet.land_structures.ToString();

                SpaceMapLabel.Text = "Space Map: " + planet.spaceMap;
                if (planet.has_ground)
                {
                    GroundMapLabel.Text = "Ground Map: " + planet.groundMap;
                    TerrainTypeLabel.Text = "Terrain Type: " + planet.mapTerrain;// + "    Terrain Type: " + planet.terrain;
                }
                else
                {
                    GroundMapLabel.Text = "Space Only";
                    TerrainTypeLabel.Text = "";
                }
                PlanetCoordinateLabel.Text = "X: " + planet.x_coord.ToString("0") + "  Y: " + planet.y_coord.ToString("0");

                SharedMapListBox.Items.Clear();
                SharedSpaceMapListBox.Items.Clear();
                List<planet> sorted = new List<planet>();
                List<planet> sortedSpace = new List<planet>();
                for (int i = 0; i < entities.Planets.Count; i++)
                {
                    planet checkPlanet = entities.Planets[i];
                    if (planet.has_ground && checkPlanet.codename != planet.codename && checkPlanet.groundMap == planet.groundMap)
                    {
                        checkPlanet.destroyed_credits = (Int32)((checkPlanet.x_coord - planet.x_coord) * (checkPlanet.x_coord - planet.x_coord) + (checkPlanet.y_coord - planet.y_coord) * (checkPlanet.y_coord - planet.y_coord));
                        sorted.Add(checkPlanet);
                    }
                    if (checkPlanet.codename != planet.codename && checkPlanet.spaceMap == planet.spaceMap)
                    {
                        checkPlanet.destroyed_credits = (Int32)((checkPlanet.x_coord - planet.x_coord) * (checkPlanet.x_coord - planet.x_coord) + (checkPlanet.y_coord - planet.y_coord) * (checkPlanet.y_coord - planet.y_coord));
                        sortedSpace.Add(checkPlanet);
                    }
                }
                sorted.Sort((s1, s2) => s1.destroyed_credits.CompareTo(s2.destroyed_credits));
                sortedSpace.Sort((s1, s2) => s1.destroyed_credits.CompareTo(s2.destroyed_credits));
                foreach (planet sort in sorted) SharedMapListBox.Items.Add(sort);
                foreach (planet sort in sortedSpace) SharedSpaceMapListBox.Items.Add(sort);

                MarkPlanets();

                PlanetGroundListBox.Items.Clear();
                foreach (unit unit in entities.groundCompanies)
                {
                    if (unit.planets.Contains(planet.codename)) PlanetGroundListBox.Items.Add(unit);
                }
                PlanetSpaceListBox.Items.Clear();
                foreach (unit unit in entities.spaceUnits)
                {
                    if (unit.planets.Contains(planet.codename)) PlanetSpaceListBox.Items.Add(unit);
                }
                PlanetStructureListBox.Items.Clear();
                foreach (unit unit in entities.structures)
                {
                    if (!unit.unitname.Contains("_Shipyard"))
                    {
                        if (unit.planets.Contains(planet.codename)) PlanetStructureListBox.Items.Add(unit);
                    }
                }

                PlanetBTSTextBox.Text = "";
                string BTS = "";
                string path = getModFile("Text\\BTSPlanet.txt");

                if (path != "") BTS = readBTS(path, planet.codename);
                if (BTS != "") PlanetBTSTextBox.Text = "Behind the scenes\n\n" + BTS + "\n";

                BTS = "";
                path = getModFile("Text\\BTSMap.txt");

                if (path != "") BTS = readBTS(path, planet.groundMap);
                if (BTS != "") PlanetBTSTextBox.Text += "Ground map information\n\n" + BTS + "\n";

                BTS = "";
                if (path != "") BTS = readBTS(path, planet.spaceMap);
                if (BTS != "") PlanetBTSTextBox.Text += "Space map information\n\n" + BTS + "\n";
            }
        }

        private string readBTS(string path, string search)
        {
            string corenne = "";
            string[] BTS = File.ReadAllLines(path);
            foreach(string line in BTS)
            {
                int firstcomma = line.IndexOf(",");
                if (firstcomma > 0)
                {
                    string id = line.Substring(0, firstcomma);
                    bool found = false;
                    if (id.Contains(";"))
                    {
                        string[] split = id.Split(';');
                        if (split.Contains(search)) found = true;
                    }
                    else
                    {
                        if (id == search) found = true;
                    }

                    if (found) corenne += line.Substring(firstcomma + 1, line.Length - firstcomma - 1).Replace("\\n", "\n") + "\n\n";
                }
            }
            return corenne;
        }

        private void SharedMapListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            MarkPlanets();
        }

        private void SharedSpaceMapListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            MarkPlanets();
        }

        private void MarkPlanets()
        {
            planet planet = (planet)PlanetListBox.Tag;
            PlanetPictureBox.Image = new Bitmap(PlanetPictureBox.Width, PlanetPictureBox.Height);
            Graphics g = Graphics.FromImage(PlanetPictureBox.Image);
            g.DrawImage((Bitmap)PlanetPictureBox.Tag, 0, 0, new Rectangle(0, 0, PlanetPictureBox.Width, PlanetPictureBox.Height), GraphicsUnit.Pixel);
            int x = (Int32)(planet.x_coord * globals.scale + globals.origin) - 2;
            int y = (Int32)(planet.y_coord * globals.scale + globals.origin) - 2;
            g.FillEllipse(new SolidBrush(Color.White), x, y, 5, 5);

            foreach (planet shared in SharedMapListBox.SelectedItems)
            {
                x = (Int32)(shared.x_coord * globals.scale + globals.origin) - 1;
                y = (Int32)(shared.y_coord * globals.scale + globals.origin) - 1;
                g.FillEllipse(new SolidBrush(Color.White), x, y, 3, 3);
            }

            foreach (planet shared in SharedSpaceMapListBox.SelectedItems)
            {
                x = (Int32)(shared.x_coord * globals.scale + globals.origin) - 1;
                y = (Int32)(shared.y_coord * globals.scale + globals.origin) - 1;
                g.FillEllipse(new SolidBrush(Color.White), x, y, 3, 3);
            }
        }

        private void populatePlanetListbox()
        {
            Bitmap Starfield = new Bitmap(PlanetPictureBox.Width, PlanetPictureBox.Height);
            Graphics g = Graphics.FromImage(Starfield);
            g.FillRectangle(new SolidBrush(Color.Black), 0, 0, PlanetPictureBox.Width, PlanetPictureBox.Height);

            List<quantizedObject> spaceMaps = new List<quantizedObject>();
            List<quantizedObject> groundMaps = new List<quantizedObject>();
            PlanetListBox.Items.Clear();
            foreach (planet planet in entities.Planets)
            {
                if (planet.codename != "Galaxy_Core_Art_Model" && planet.username.ToLower().Contains(PlanetSearchBox.Text.ToLower()))
                {
                    PlanetListBox.Items.Add(planet);
                    int x = (Int32)(planet.x_coord * globals.scale + globals.origin);
                    int y = (Int32)(planet.y_coord * globals.scale + globals.origin);
                    g.FillRectangle(new SolidBrush(Color.White), x, y, 1, 1);

                    quantizedObject q = new quantizedObject
                    {
                        quantity = 1,
                        username = planet.spaceMap,
                    };
                    spaceMaps = quantizedAdd(spaceMaps, q);
                    if (planet.has_ground)
                    {
                        q.username = planet.groundMap;
                        groundMaps = quantizedAdd(groundMaps, q);
                    }
                }
            }

            PlanetPictureBox.Tag = Starfield;
            PlanetPictureBox.Image = Starfield;

            spaceMaps.Sort((s2, s1) => s1.quantity.CompareTo(s2.quantity));
            groundMaps.Sort((s2, s1) => s1.quantity.CompareTo(s2.quantity));
            PlanetSpaceMapRB.Tag = spaceMaps;
            PlanetGroundMapRB.Tag = groundMaps;
            MapsInPlanetsListbox.Items.Clear();
            if (PlanetSpaceMapRB.Checked)
            {
                foreach (quantizedObject q in (List<quantizedObject>)PlanetSpaceMapRB.Tag) {
                    if(q.username.Contains(MapSearchBox.Text)) MapsInPlanetsListbox.Items.Add(q);
                }
            }
            else
            {
                foreach (quantizedObject q in (List<quantizedObject>)PlanetGroundMapRB.Tag)
                {
                    if (q.username.Contains(MapSearchBox.Text)) MapsInPlanetsListbox.Items.Add(q);
                }
            }

            PlanetMatchesLabel.Text = "Matches: " + PlanetListBox.Items.Count;
        }

        private void PlanetSearchBox_TextChanged(object sender, EventArgs e)
        {
            populatePlanetListbox();
        }

        private void PlanetSpaceMapRB_CheckedChanged(object sender, EventArgs e)
        {
            MapsInPlanetsListbox.Items.Clear();
            foreach (quantizedObject q in (List<quantizedObject>)PlanetSpaceMapRB.Tag) MapsInPlanetsListbox.Items.Add(q);
        }

        private void PlanetGroundMapRB_CheckedChanged(object sender, EventArgs e)
        {
            MapsInPlanetsListbox.Items.Clear();
            foreach (quantizedObject q in (List<quantizedObject>)PlanetGroundMapRB.Tag) MapsInPlanetsListbox.Items.Add(q);
        }

        private void MapsInPlanetsListbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (MapsInPlanetsListbox.SelectedItems.Count > 0)
            {
                quantizedObject q = (quantizedObject)MapsInPlanetsListbox.SelectedItem;
                string planets = "";
                bool furst = true;
                foreach(planet planet in PlanetListBox.Items)
                {
                    if(PlanetSpaceMapRB.Checked && planet.spaceMap == q.username || PlanetGroundMapRB.Checked && planet.groundMap == q.username)
                    {
                        if (furst) furst = false;
                        else planets += ", ";
                        planets += planet.username;
                    }
                }
                MessageBox.Show(q.ToString() + "\n\n" + planets);

            }
        }

        private void PlanetStructuresGoToButton_Click(object sender, EventArgs e)
        {
            if (PlanetStructureListBox.SelectedItems.Count > 0)
            {
                //todo handle space structures
                insert_history((int)historymaintabs.unit, 7, ((unit)PlanetStructureListBox.SelectedItem).unitname, true);
            }
        }

        private void PlanetSpaceUnitsGoToButton_Click(object sender, EventArgs e)
        {
            if (PlanetSpaceListBox.SelectedItems.Count > 0)
            {
                insert_history((int)historymaintabs.unit, 0, ((unit)PlanetSpaceListBox.SelectedItem).unitname, true);
            }
        }

        private void PlanetGroundUnitsGoToButton_Click(object sender, EventArgs e)
        {
            if (PlanetGroundListBox.SelectedItems.Count > 0)
            {
                insert_history((int)historymaintabs.unit, 1, ((unit)PlanetGroundListBox.SelectedItem).unitname, true);
            }
        }

        private void AutoResolveUnitComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        //Don't put any functions below here if you want it to still compile
    }
}
