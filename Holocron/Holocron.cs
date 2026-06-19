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
using static Holocron.PlanetFilter;
using static Holocron.PlanetSort;
using static SharedFunctions;

/*
 *https://www.nuget.org/packages/HtmlAgilityPack/ handles invalid XML better
 *https://github.com/LorettaDevs/Loretta Lua parsing
 *
 *https://dev.to/karenpayneoregon/window-forms-dark-mode-33on in program.cs, requires .Net upgrade?
 *
 *parse sfx on abilities - selecting an ability with sfx enters a special mode for sfx
 *
 * use absence of changelogs to detect EaWX versions
 * 
 * missing text buttons for units. Campaigns? missions?
 * unused text button - see how bad the false positives are after doing
 * 
 * filter by fighter vs bomber
 * sort by fighter or bomber?
 *
 * Why are FotR skirmish infantry companies claiming to have infinite hp in the sort?
 * 
 * lock controls during load

 * more right clicks with function - save images on conquest and planet maps, save/detail accuracy table?...
 * Don't use messagebox - create dedicated listbox popup
 * 
 * 
 * add all space categories to filter page, not just targeting ones. Or just all categories and let them sort out ground?
 * 
 * 
 * armor matrix is broken in vanilla
 * 
 * 
 * sound section for units - parse sourcing information, play sounds?
 * 
 * 
 * parse skirmish prereqs and tactical build lists. Especially for MDU alikes
 * 
 * todo - log mode might need to add pulse interval depending on how sheets do it
 * log mode should ideally adjust reload nonproportionally
 * 
 * complement CP and dps are slightly different than sheet. Stop using floats? Or does some rounding need to be forced?
 * 
 * planet preferred revolt, influence modifiers
 * 
keep history updated whenever a tab or subtab is implemented
there might be a bit of oddness in the history tracking when factions share a name



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

            public static string dpsformat = "0.###";

            public static UnitSortClass UnitSortConfig = new UnitSortClass();
            public static UnitFilterClass UnitFilterConfig = new UnitFilterClass();
            public static contrast_values ContrastValues = new contrast_values();

            public static PlanetSortClass PlanetSortConfig = new PlanetSortClass();
            public static PlanetFilterClass PlanetFilterConfig = new PlanetFilterClass();

            public static List<shipname> shipnames = new List<shipname>();

            //Assume map picturebox is a square of odd pixel count, the same size between GC and planet pages
            public static int origin;
            public static float scale;

            public static bool allplanets = false; //todo make sure this is off for releases
            public static bool devmode = false;
            public static List<unit> MoneyStructures = new List<unit>();
            public static List<unit> DiscountEntities = new List<unit>();

            public static int tradebase = 50; //Todo read this from the data
            public static int tradehubmultiplier = 2;
        }

        public static class nav
        {
            public static List<int> maintab = new List<int>();
            public static List<int> secondary = new List<int>();
            public static List<string> item = new List<string>();

            public static int navindex = -1;
            public static bool suppresshistory = false;
        }

        public struct weighted_type_entry
        {
            public string name;
            public ulong categoryMask;
            public float weight;
        }

        public struct weighted_type_list
        {
            public ulong enemyCategoryMask;
            public List<weighted_type_entry> weightedTypes;
        }

        public struct contrast_values
        {
            public List<weighted_type_list> friendlyTypeLists;
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
        private DataGridView autoResolveSideAGrid;
        private DataGridView autoResolveSideBGrid;
        private DataGridView autoResolveContrastGrid;
        private bool autoResolveTablesInitialized = false;
        private int autoResolveLastBattleType = -1;

        private sealed class AutoResolveUnitChoice
        {
            public string DisplayName { get; set; }
            public unit Value { get; set; }
        }

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
            string exePath = AppContext.BaseDirectory;
            string localmodtest = UpOneFolder(UpOneFolder(UpOneFolder(UpOneFolder(exePath))));
            string modfolder = UpOneFolder(exePath);
            if (File.Exists(localmodtest + "\\StarWarsG.exe"))
            {
                globals.localmodpath = UpOneFolder(UpOneFolder(modfolder));
                globals.steammodpath = UpOneFolder(UpOneFolder(UpOneFolder(localmodtest))) + "\\workshop\\content\\32470";
                if(entities.modpaths.Count == 0)
                {
                    if (Directory.Exists(modfolder + "\\..\\TR") && Directory.Exists(modfolder + "\\..\\FotR") && Directory.Exists(modfolder + "\\..\\CoreSaga") && Directory.Exists(modfolder + "\\..\\Rev"))
                    {
                        DevChoice devChoice = new DevChoice();
                        devChoice.basepath = UpOneFolder(modfolder);
                        devChoice.ShowDialog();

                        entities.modpaths = devChoice.args;
                        globals.allplanets = devChoice.allplanet;
                        globals.devmode = true;
                    }
                    else entities.modpaths.Add(modfolder);
                }
            }
            else
            {
                localmodtest = UpOneFolder(UpOneFolder(localmodtest)) + "\\common\\Star Wars Empire at War\\corruption";
                if (File.Exists(localmodtest + "\\StarWarsG.exe"))
                {
                    if (entities.modpaths.Count == 0) entities.modpaths.Add(modfolder);
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
                    else
                    {
                        MessageBox.Show("Could not locate data files. Please place in the data folder of a Steam Workshop or local mod for Empire at War");
                        this.Close();
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
            load_mods();
            globals.UnitSortConfig.SortType = UnitSortTypes.Name;
            globals.UnitSortConfig.denomtype = "Absolute Value";
            globals.PlanetSortConfig.SortType = PlanetSortTypes.Name;
            UnitFilter INeedYourFunctions = new UnitFilter();
            globals.UnitFilterConfig = INeedYourFunctions.newFilter();
            PlanetFilter IStillNeedYourFunctions = new PlanetFilter();
            globals.PlanetFilterConfig = IStillNeedYourFunctions.newFilter();

            //todo stop right aligned? controls from resizing in stupid ways when the design tab is reopened
            //In lieu of a proper fix, make things right aligned at runtime...
            PlanetGCListBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right | AnchorStyles.Left;
            PlanetBTSTextBox.Anchor = AnchorStyles.Top |  AnchorStyles.Right | AnchorStyles.Left;
            GCPresentListbox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            GCPlanetListBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            GCStoryTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            ConquestBTSTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            FactionBTSTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            MapsInPlanetsListbox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            MapSearchBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            PlanetCampaignLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            PlanetGoToGCButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            PlanetMapLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            PlanetMapSearchLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            PlanetSpaceMapRB.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            PlanetGroundMapRB.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            UnitTextPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            FactionDescLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            ShipNameRichTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            UnitAvailPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            UnitStatPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            UnitSubunitPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            UnitAbilityPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            UnitBTSPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            UnitBTSTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;

            LookupTabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right | AnchorStyles.Left;

            this.WindowState = FormWindowState.Maximized;
        }
        
        private void load_mods()
        {
            Loading loadscreen = new Loading();
            loadscreen.Show();
            FactionListBox.Items.Clear();
            ComplementFactionListBox.Items.Clear();
            GCListBox.Items.Clear();
            PlanetListBox.Items.Clear();
            UnitListBox.Items.Clear();
            System.Threading.Thread t = new System.Threading.Thread(() => LoadThread(loadscreen));
            t.Start();
            MissionListBox.Items.Clear();
            SpawnListBox.Items.Clear();
            StandardFListBox.Items.Clear();
            RandomFListBox.Items.Clear();
            entities.mapcache = new List<string>();
            entities.terraincache = new List<int>();
            globals.shipnames.Clear();
            globals.MoneyStructures.Clear();
            globals.DiscountEntities.Clear();
        }

        private void LoadThread(Loading loadscreen)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            loadscreen.ChangeText("Reading text file");
            entities.Text = DatParser.ReadDat(getModFile("Text\\MasterTextFile_ENGLISH.dat", entities), ',', 0);
            loadscreen.SetQuote(getLoadQuote(entities));

            parsemodid(entities);

            loadscreen.ChangeText("Reading MEG files");
            parseMEGs(entities, UpOneFolder(UpOneFolder(globals.localmodpath)));

            loadscreen.ChangeText("Reading icon file");
            entities.IconData = DatParser.ReadMTD(entities);
            try
            {
                entities.MTmaster = (Bitmap)Image.FromFile(getModFile("Art\\Textures\\MT_CommandBar.tga", entities));
            }
            catch
            {
                try
                {
                    entities.MTmaster = (Bitmap)(new TGA(readModBytesOrMeg("Art\\Textures\\MT_CommandBar.tga", entities)));
                }
                catch
                {
                    entities.MTmaster = new Bitmap(50, 50);
                }
            };
            entities.readerrors = "";

            loadscreen.ChangeText("Parsing faction data");
            parseFactions(entities);
            foreach (faction faction in entities.factions)
            {
                FactionListBox.BeginInvoke(new Action(() => FactionListBox.Items.Add(faction.textname)));
                ComplementFactionListBox.BeginInvoke(new Action(() => ComplementFactionListBox.Items.Add(faction)));
            }
            ComplementFactionListBox.BeginInvoke(new Action(() => ComplementFactionListBox.SelectedIndex = 0));

            loadscreen.ChangeText("Parsing projectile data");
            List<string> listfiles = getModFiles("XML\\Projectiles", "*.xml", entities);
            parseProjectiles(entities);

            loadscreen.ChangeText("Parsing hardpoint data");
            parseHardpoints(entities);

            loadscreen.ChangeText("Parsing sound entries");
            parseSFX(entities);

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
            AutoResolveApplySettingsDefaults();

            loadscreen.ChangeText("Parsing object data");
            parseObjects(entities);
            loadscreen.ChangeText("Resolving object dependencies");
            untemplate(entities);
            loadscreen.ChangeText("Assembing company unit lists");
            unitToCompanyData(entities);
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
            /*loadscreen.ChangeText("Assembing company unit lists");
            entities.groundCompanies = unitToCompanyData(entities.groundCompanies, entities.groundUnits, entities.containers);
            entities.fighters = unitToCompanyData(entities.fighters, entities.fighters, entities.containers);
            entities.spaceUnits = unitToCompanyData(entities.spaceUnits, entities.spaceUnits, entities.containers); //Gunships
            loadscreen.ChangeText("Assembing hero companies");
            entities.heroCompanies = unitToCompanyData(entities.heroCompanies, entities.groundHeroes, entities.containers);*/
            //entities.spaceHeroes = unitToCompanyData(entities.spaceHeroes, entities.spaceUnits, entities.containers); //Todo: fix for the rare gunship hero
            //entities.spaceHeroes = unitToCompanyData(entities.spaceHeroes, entities.spaceUnits, entities.containers, true);
            loadscreen.ChangeText("Parsing planet data");
            parsePlanets(entities, globals.allplanets);
            loadscreen.ChangeText("Reading Planet specific spawn sets");
            readPlanetSpawnTables(entities);
            loadscreen.ChangeText("Parsing trade routes");
            parseTradeRoutes(entities);
            loadscreen.ChangeText("Parsing galactic conquests");
            parseGCs(entities);

            loadscreen.ChangeText("Reading AI contrast values");
            globals.ContrastValues = ReadContrastValues();
            //Assume picturebox is a square of odd pixel count
            globals.origin = (PlanetPictureBox.Width - 1) / 2;
            globals.scale = globals.origin/(entities.PlanetBounds + 25);
            //entities.hardpointhashes.Clear(); //No reason to keep these around after parsing. I found a reason
            //entities.projectilehashes.Clear();

            loadscreen.CloseLoadScreen();
        }

        private contrast_values ReadContrastValues()
        {
            contrast_values contrastValues = new contrast_values();
            contrastValues.friendlyTypeLists = new List<weighted_type_list>();
            contrastValues.typeScale = new Dictionary<string, float>();

            string[] lines = readModTextLinesOrMeg("Scripts\\Library\\PGAICommands.lua", entities);
            bool inContrastFunction = false;

            ulong currentEnemyCategoryMask = 0UL;
            List<string> currentNames = null;
            List<float> currentWeights = null;

            foreach (string raw in lines)
            {
                string line = raw.Trim();

                if (!inContrastFunction)
                {
                    if (line.StartsWith("function Set_Contrast_Values()")) inContrastFunction = true;
                    continue;
                }

                if (line == "end") break;

                if (line.StartsWith("EnemyContrastTypes[_e_cnt]"))
                {
                    currentEnemyCategoryMask = AutoResolveGetCategoryMask(LuaParser.ExtractLuaQuotedValue(line));
                    continue;
                }

                if (line.StartsWith("FriendlyContrastTypeNames"))
                {
                    currentNames = LuaParser.ParseLuaStringArray(line);
                    continue;
                }

                if (line.StartsWith("FriendlyContrastWeights"))
                {
                    currentWeights = LuaParser.ParseLuaFloatArray(line);

                    weighted_type_list list = new weighted_type_list();
                    list.enemyCategoryMask = currentEnemyCategoryMask;
                    list.weightedTypes = new List<weighted_type_entry>();
                    for (int i = 0; i < Math.Min(currentNames.Count, currentWeights.Count); i++)
                    {
                        weighted_type_entry entry = new weighted_type_entry();
                        entry.name = currentNames[i];
                        entry.categoryMask = AutoResolveGetCategoryMask(currentNames[i]);
                        entry.weight = currentWeights[i];
                        list.weightedTypes.Add(entry);
                    }

                    contrastValues.friendlyTypeLists.Add(list);
                    continue;
                }

                if (line.StartsWith("ContrastTypeScale["))
                {
                    string scaleType = LuaParser.ExtractLuaBracketQuotedKey(line);
                    float scale = float.Parse(LuaParser.ExtractLuaAssignedValue(line), CultureInfo.InvariantCulture);
                    contrastValues.typeScale[scaleType] = scale;
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
            galaxy,
            autoresolve,
            lookups,
        }

        enum lookupsubtabs
        {
            lkMatrix,
            lkProj,
            lkName,
            lkNameFile,
            lkReward,
            lkSpawn,
            lkCorporations,
            lkHero,
            lkStandard,
            lkRandom,
            lkRegional,
        }

        private void insert_history(int main, int secondary, string entity, bool go_to = false)
        {
            if (nav.suppresshistory) return;
            if (nav.navindex > 0 && nav.item[nav.navindex] == entity) return; //Don't duplicate entries when e.g. sorting reselects the same index. Technically should care if the others are the same too, but it's not often names should overlap
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

            MainTab.SelectedIndex = (int)main; //If lookups start demanding a tertiary //if(main < historymaintabs.lookups) 
            //else MainTab.SelectedIndex = (int)(main - 1 - historymaintabs.lookups);
            bool found = false;
            switch (main)
            {
                case historymaintabs.faction:
                    for(int i = 0; i < entities.factions.Count; i++)
                    {
                        if (item == entities.factions[i].codename)
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
                    if (!found) //unify goto units to not care about rb
                    {
                        UnitSearchTextBox.Text = "";
                        UnitFilter INeedYourFunctions = new UnitFilter();
                        globals.UnitFilterConfig = INeedYourFunctions.newFilter();
                        UnitFilterTypeLabel.Text = INeedYourFunctions.filterDocumentation;
                        globals.UnitFilterConfig.skirmishModes.Add(1); //Jumping to skirmish units may also be required. Hopefully nothing else?
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
                case historymaintabs.conquest:
                    for (int i = 0; i < GCListBox.Items.Count; i++)
                    {
                        if (item == ((galacticConquest)GCListBox.Items[i]).codename)
                        {
                            GCListBox.SelectedItem = GCListBox.Items[i];
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        ProgressiveCheckBox.Checked = true;
                        RegionalCheckBox.Checked = true;
                        HistoricalCheckBox.Checked = true;
                        InfinityCheckBox.Checked = true;
                        for (int i = 0; i < GCListBox.Items.Count; i++)
                        {
                            if (item == ((galacticConquest)GCListBox.Items[i]).codename)
                            {
                                GCListBox.SelectedItem = GCListBox.Items[i];
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
                    if (!found)
                    {
                        PlanetSearchBox.Text = "";
                        PlanetFilter IStillNeedYourFunctions = new PlanetFilter();
                        globals.PlanetFilterConfig = IStillNeedYourFunctions.newFilter();
                        PlanetFilterTypeLabel.Text = IStillNeedYourFunctions.filterDocumentation;
                        populatePlanetListbox();
                        for (int i = 0; i < PlanetListBox.Items.Count; i++)
                        {
                            if (item == ((planet)PlanetListBox.Items[i]).codename)
                            {
                                PlanetListBox.SelectedItem = PlanetListBox.Items[i];
                                break;
                            }
                        }
                    }
                    break;
                case historymaintabs.lookups:
                    LookupTabControl.SelectedIndex = secondary;
                    switch ((lookupsubtabs)secondary)
                    {
                        case lookupsubtabs.lkSpawn:
                            for (int i = 0; i < SpawnListBox.Items.Count; i++)
                            {
                                if (item == (string)SpawnListBox.Items[i])
                                {
                                    SpawnListBox.SelectedItem = SpawnListBox.Items[i];
                                    found = true;
                                    break;
                                }
                            }
                            break;
                        case lookupsubtabs.lkStandard:
                            for (int i = 0; i < StandardFListBox.Items.Count; i++)
                            {
                                if (item == (string)StandardFListBox.Items[i])
                                {
                                    StandardFListBox.SelectedItem = StandardFListBox.Items[i];
                                    found = true;
                                    break;
                                }
                            }
                            break;
                        case lookupsubtabs.lkRandom:
                            for (int i = 0; i < RandomFListBox.Items.Count; i++)
                            {
                                if (item == (string)RandomFListBox.Items[i])
                                {
                                    RandomFListBox.SelectedItem = RandomFListBox.Items[i];
                                    found = true;
                                    break;
                                }
                            }
                            break;
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
                case (int)historymaintabs.conquest:
                    if (GCListBox.SelectedItems.Count == 0) populateGCListbox();
                    break;
                case (int)historymaintabs.planet:
                    if (PlanetListBox.SelectedItems.Count == 0) populatePlanetListbox();
                    break;
                case (int)historymaintabs.lookups:
                    FillMatrixLookup();
                    break;
                case (int)historymaintabs.autoresolve:
                    AutoResolveCreateEditableTables();
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
                case 2:
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
                                        string namefile = getModFile(RemoveTopLevelFolder(file), entities);
                                        if (File.Exists(namefile))
                                        {
                                            string[] monikers = File.ReadAllLines(namefile);
                                            file = LastFolderOrFile(file).Replace(".txt", "");

                                            foreach (string moniker in monikers)
                                            {
                                                if(moniker != "")
                                                {
                                                    int index = globals.shipnames.FindIndex(s => s.name == moniker);
                                                    if (index >= 0)
                                                    {
                                                        shipname name = globals.shipnames[index];
                                                        name.units.Add(file);
                                                        globals.shipnames[index] = name;
                                                    }
                                                    else
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

                        foreach (unit hero in entities.spaceHeroes)
                        {
                            if (hero.tooltip.Contains("TEXT_TOOLTIP_COMMAND_")) //Not sure if this is in fact an efficiency gain
                            {//Todo benchmark this and sorting by name. Add loading screen?
                                foreach (string entry in SplitXMLWhitespaceList(hero.tooltip))
                                {
                                    string namestring = "";
                                    if (entry.Contains("TEXT_TOOLTIP_COMMAND_"))
                                    {
                                        namestring = Find_Text_Entry(entry, entities);
                                        if (namestring.Contains(", "))
                                        {//This pattern should be consistent among commands. Close enough, probably
                                            string heroname = Find_Text_Entry(hero.username, entities) + " (" + namestring.Substring(0, namestring.LastIndexOf(",")) + ")";
                                            namestring = namestring.Substring(namestring.LastIndexOf(",") + 2, namestring.Length - namestring.LastIndexOf(",") - 2);
                                            int index = globals.shipnames.FindIndex(s => s.name == namestring);
                                            if (index >= 0)
                                            {
                                                shipname name = globals.shipnames[index];
                                                name.heroes.Add(heroname);
                                                globals.shipnames[index] = name;
                                            }
                                            else
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
                        foreach (unit hero in entities.spaceHeroes)
                        {//todo: think of a way to capture cases like Lucid Voice, where the name is not also used as a hero flagship. Doesn't have COMMAND and isn't a fighter?
                            int index = globals.shipnames.FindIndex(s => s.name == hero.username);
                            if(index >= 0 && !hero.tooltip.Contains("TEXT_TOOLTIP_COMMAND_")) //If it's the name of a standalone named ship it shouldn't have this, whereas a major hero name and ship name can also happen to coexost (e.g. Rooks)
                            {
                                shipname name = globals.shipnames[index];
                                name.heroes.Add(hero.username + " (Standalone)");
                                globals.shipnames[index] = name;
                            }
                            else if (hero.unitclass == "TEXT_ENCYCLOPEDIA_CLASS_UNIQUE_SHIP")
                            {
                                shipname nuevo = new shipname();
                                nuevo.name = hero.username;
                                nuevo.units = new List<string>();
                                nuevo.heroes = new List<string>();
                                nuevo.heroes.Add(hero.username + " (Standalone)");
                                nuevo.unused = new List<string>();
                                globals.shipnames.Add(nuevo);
                            }
                        }

                        Checked = new List<string>();
                        List<string> unuseds = getModFiles("..\\Unused\\Unused ShipNames", "*.txt", entities);
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
                                        int index = globals.shipnames.FindIndex(s => s.name == moniker);
                                        if (index >= 0)
                                        {
                                            shipname name = globals.shipnames[index];
                                            name.unused.Add(file);
                                            globals.shipnames[index] = name;
                                        }
                                        else
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
                case 3:
                    if (NameListBox.Items.Count == 0) //todo ensure other histories have such conditions cleared by load_mods
                    {
                        List<string> namefiles = getModFiles("ShipNames", "*.txt", entities);
                        foreach (string file in namefiles)
                        {
                            string shortfile = LastFolderOrFile(file);
                            shortfile = shortfile.Substring(0, shortfile.LastIndexOf("."));
                            NameListBox.Items.Add(shortfile);
                        }
                    }
                    break;
                case 4:
                    if (MissionListBox.Items.Count == 0)
                    {
                        List<string> missionfiles = getModFiles("Scripts\\Library\\eawx-plugins\\intervention-missions\\rewards", "*.lua", entities);
                        foreach (string file in missionfiles)
                        {
                            MissionListBox.Items.Add(LastFolderOrFile(file).Replace("RewardTables_","").ToUpper().Replace(".LUA",""));
                        }
                    }
                    break;
                case 5:
                    if (SpawnListBox.Items.Count == 0) //todo may have to check a new location after modcontent loader dies
                    {
                        List<string> missionfiles = getModFiles("Scripts\\Library\\spawn-sets", "*.lua", entities);
                        foreach (string file in missionfiles)
                        {
                            if (!file.Contains("DEBUG")) SpawnListBox.Items.Add(LastFolderOrFile(file).ToUpper().Replace(".LUA", ""));
                        }
                    }
                    break;
                case 8:
                    if (StandardFListBox.Items.Count == 0)
                    {
                        List<string> missionfiles = getModFiles("Scripts\\Library\\standard-fighters", "*.lua", entities);
                        foreach (string file in missionfiles)
                        {
                            StandardFListBox.Items.Add(LastFolderOrFile(file).ToUpper().Replace(".LUA", ""));
                        }
                    }
                    break;
                case 9:
                    if (RandomFListBox.Items.Count == 0)
                    {
                        List<string> missionfiles = getModFiles("Scripts\\Library\\random-fighters", "*.lua", entities);
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
            foreach (unit item in entities.fighters) source.Add(item);
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
                foreach (unit item in entities.fighters) source.Add(item);
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
            List<unit> availableUnits = AutoResolveGetBattleTypeUnitSource()
                .Where(AutoResolveUnitHasSufficientInformation)
                .OrderBy(x => x.username)
                .ToList();

            AutoResolveApplyGridUnitChoices(availableUnits);
            AutoResolveFillContrastGrid();
        }

        private void AutoResolveCreateEditableTables()
        {
            if (autoResolveTablesInitialized) return;

            autoResolveSideAGrid = AutoResolveCreateSideGrid(new Point(12, 55));
            autoResolveSideBGrid = AutoResolveCreateSideGrid(new Point(399, 55));
            autoResolveContrastGrid = AutoResolveCreateContrastGrid(new Point(749, 32));

            tabAutoResolve.Controls.Add(autoResolveSideAGrid);
            tabAutoResolve.Controls.Add(autoResolveSideBGrid);
            tabAutoResolve.Controls.Add(autoResolveContrastGrid);

            autoResolveTablesInitialized = true;
        }

        private DataGridView AutoResolveCreateSideGrid(Point location)
        {
            DataGridView grid = new DataGridView();
            grid.Location = location;
            grid.Size = new Size(344, 180);
            grid.AllowUserToAddRows = true;
            grid.AllowUserToDeleteRows = true;
            grid.RowHeadersVisible = false;
            grid.AutoGenerateColumns = false;
            grid.SelectionMode = DataGridViewSelectionMode.CellSelect;

            DataGridViewComboBoxColumn unitColumn = new DataGridViewComboBoxColumn();
            unitColumn.Name = "Unit";
            unitColumn.HeaderText = "Unit";
            unitColumn.DisplayMember = "DisplayName";
            unitColumn.ValueMember = "Value";
            unitColumn.ValueType = typeof(unit);
            unitColumn.Width = 250;

            DataGridViewTextBoxColumn countColumn = new DataGridViewTextBoxColumn();
            countColumn.Name = "Count";
            countColumn.HeaderText = "Count";
            countColumn.ValueType = typeof(int);
            countColumn.Width = 70;

            grid.Columns.Add(unitColumn);
            grid.Columns.Add(countColumn);
            grid.CellValueChanged += AutoResolveGrid_CellValueChanged;
            grid.CurrentCellDirtyStateChanged += AutoResolveGrid_CurrentCellDirtyStateChanged;
            grid.UserDeletedRow += AutoResolveGrid_UserDeletedRow;

            return grid;
        }

        private DataGridView AutoResolveCreateContrastGrid(Point location)
        {
            DataGridView grid = new DataGridView();
            grid.Location = location;
            grid.Size = new Size(725, 746);
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToOrderColumns = false;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.AutoGenerateColumns = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;

            DataGridViewTextBoxColumn enemyColumn = new DataGridViewTextBoxColumn();
            enemyColumn.Name = "EnemyCategory";
            enemyColumn.HeaderText = "Enemy Category";
            enemyColumn.Width = 180;

            DataGridViewTextBoxColumn friendlyColumn = new DataGridViewTextBoxColumn();
            friendlyColumn.Name = "FriendlyCategory";
            friendlyColumn.HeaderText = "Friendly Category";
            friendlyColumn.Width = 300;

            DataGridViewTextBoxColumn weightColumn = new DataGridViewTextBoxColumn();
            weightColumn.Name = "Weight";
            weightColumn.HeaderText = "Weight";
            weightColumn.Width = 90;

            grid.Columns.Add(enemyColumn);
            grid.Columns.Add(friendlyColumn);
            grid.Columns.Add(weightColumn);

            return grid;
        }

        private void AutoResolveApplyGridUnitChoices(List<unit> availableUnits)
        {
            if (!autoResolveTablesInitialized) return;

            List<AutoResolveUnitChoice> choices = availableUnits
                .Select(x => new AutoResolveUnitChoice { DisplayName = x.username + " [" + x.unitname + "]", Value = x })
                .ToList();

            DataGridViewComboBoxColumn aCol = autoResolveSideAGrid.Columns[0] as DataGridViewComboBoxColumn;
            DataGridViewComboBoxColumn bCol = autoResolveSideBGrid.Columns[0] as DataGridViewComboBoxColumn;
            if (aCol != null) aCol.DataSource = choices;
            if (bCol != null) bCol.DataSource = choices;
        }

        private List<autoresolve_entry> AutoResolveBuildEntriesFromGrid(DataGridView grid)
        {
            List<autoresolve_entry> entries = new List<autoresolve_entry>();
            if (grid == null) return entries;

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;

                object rawUnit = row.Cells[0].Value;
                if (rawUnit == null) continue;

                unit selectedUnit;
                if (rawUnit is unit)
                {
                    selectedUnit = (unit)rawUnit;
                }
                else
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(selectedUnit.unitname)) continue;

                int count;
                if (!int.TryParse(Convert.ToString(row.Cells[1].Value), out count)) count = 1;
                count = Math.Max(1, count);

                entries.Add(new autoresolve_entry { source = selectedUnit, quantity = count });
            }

            return entries;
        }

        private void AutoResolveRebuildSideListsFromTables()
        {
            if (!autoResolveTablesInitialized) return;
            autoResolveSideA = AutoResolveBuildEntriesFromGrid(autoResolveSideAGrid);
            autoResolveSideB = AutoResolveBuildEntriesFromGrid(autoResolveSideBGrid);
        }

        private void AutoResolveClearUnitTables()
        {
            if (!autoResolveTablesInitialized) return;

            autoResolveSideAGrid.Rows.Clear();
            autoResolveSideBGrid.Rows.Clear();
            autoResolveSideA.Clear();
            autoResolveSideB.Clear();
        }

        private void AutoResolveGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            DataGridView grid = sender as DataGridView;
            if (grid != null && grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void AutoResolveGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            AutoResolveRebuildSideListsFromTables();
            AutoResolveUpdatePowerDisplay();
        }

        private void AutoResolveGrid_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            AutoResolveRebuildSideListsFromTables();
            AutoResolveUpdatePowerDisplay();
        }

        private void AutoResolveRefreshSideListboxes()
        {
            AutoResolveRebuildSideListsFromTables();
            AutoResolveFillContrastGrid();
            AutoResolveUpdatePowerDisplay();
        }

        private ulong AutoResolveGetCategoryMask(string category)
        {
            if (string.IsNullOrWhiteSpace(category) || entities.CategoryBitMasks == null) return 0UL;

            ulong value;
            if (entities.CategoryBitMasks.TryGetValue(category, out value)) return value;
            return 0UL;
        }

        private ulong AutoResolveGetCategoryMask(IEnumerable<string> categories)
        {
            ulong combined = 0UL;
            if (categories == null) return combined;

            foreach (string category in categories)
            {
                combined |= AutoResolveGetCategoryMask(category);
            }

            return combined;
        }

        private List<string> AutoResolveGetCategoriesFromMask(ulong categoryMask, IEnumerable<string> fallbackCategories)
        {
            List<string> categories = new List<string>();
            if (categoryMask != 0UL)
            {
                foreach (string category in entities.AllCategories)
                {
                    ulong mask = AutoResolveGetCategoryMask(category);
                    if (mask != 0UL && (categoryMask & mask) != 0UL) categories.Add(category);
                }

                if (categories.Count == 0 && entities.CategoryBitMasks != null)
                {
                    foreach (KeyValuePair<string, ulong> pair in entities.CategoryBitMasks)
                    {
                        if (pair.Value != 0UL && (categoryMask & pair.Value) != 0UL) categories.Add(pair.Key);
                    }
                }
            }

            if (categories.Count == 0 && fallbackCategories != null)
            {
                foreach (string category in fallbackCategories)
                {
                    if (!string.IsNullOrWhiteSpace(category)) categories.Add(category);
                }
            }

            return categories;
        }

        private string AutoResolveGetDisplayCategoryFromMask(ulong categoryMask, IEnumerable<string> fallbackCategories = null)
        {
            List<string> categories = AutoResolveGetCategoriesFromMask(categoryMask, fallbackCategories);
            return categories.Count > 0 ? categories[0] : null;
        }

        private List<TargetContrastPort.WeightedCategoryEntry> AutoResolveGetContrastWeights(ulong enemyTypeMask)
        {
            List<TargetContrastPort.WeightedCategoryEntry> weights = new List<TargetContrastPort.WeightedCategoryEntry>();
            if (globals.ContrastValues.friendlyTypeLists == null) return weights;

            for (int i = 0; i < globals.ContrastValues.friendlyTypeLists.Count; i++)
            {
                weighted_type_list weighted = globals.ContrastValues.friendlyTypeLists[i];
                if ((weighted.enemyCategoryMask & enemyTypeMask) == 0UL) continue;

                for (int j = 0; j < weighted.weightedTypes.Count; j++)
                {
                    weighted_type_entry weightedType = weighted.weightedTypes[j];
                    if (weightedType.categoryMask == 0UL) continue;

                    TargetContrastPort.WeightedCategoryEntry entry = new TargetContrastPort.WeightedCategoryEntry();
                    entry.CategoryMask = weightedType.categoryMask;
                    entry.Weight = weightedType.weight;
                    weights.Add(entry);
                }

                return weights;
            }

            return weights;
        }

        private void AutoResolveFillContrastGrid()
        {
            if (autoResolveContrastGrid == null) return;

            autoResolveContrastGrid.Rows.Clear();
            if (globals.ContrastValues.friendlyTypeLists == null) return;

            for (int i = 0; i < globals.ContrastValues.friendlyTypeLists.Count; i++)
            {
                weighted_type_list weighted = globals.ContrastValues.friendlyTypeLists[i];
                string enemyCategory = AutoResolveGetDisplayCategoryFromMask(weighted.enemyCategoryMask);
                if (string.IsNullOrWhiteSpace(enemyCategory)) enemyCategory = "(none)";

                if (weighted.weightedTypes == null || weighted.weightedTypes.Count == 0)
                {
                    autoResolveContrastGrid.Rows.Add(enemyCategory, "(none)", "");
                    continue;
                }

                for (int j = 0; j < weighted.weightedTypes.Count; j++)
                {
                    weighted_type_entry weightedType = weighted.weightedTypes[j];
                    string friendlyCategory = weightedType.name;
                    if (string.IsNullOrWhiteSpace(friendlyCategory))
                    {
                        friendlyCategory = AutoResolveGetDisplayCategoryFromMask(weightedType.categoryMask);
                    }
                    if (string.IsNullOrWhiteSpace(friendlyCategory)) friendlyCategory = "(none)";

                    autoResolveContrastGrid.Rows.Add(
                        j == 0 ? enemyCategory : "",
                        friendlyCategory,
                        weightedType.weight.ToString("0.###", CultureInfo.InvariantCulture));
                }
            }
        }

        private Dictionary<ulong, float> AutoResolveBuildCategoryPowerMap(List<autoresolve_entry> side, bool space)
        {
            Dictionary<ulong, float> values = new Dictionary<ulong, float>();

            foreach (autoresolve_entry entry in side)
            {
                bool isTransport = entry.source.behaviors != null && entry.source.behaviors.Contains("TRANSPORT");
                if (space && isTransport) continue;

                float power = Math.Max(0f, entry.source.cp) * Math.Max(1, entry.quantity);
                if (power <= 0f) continue;

                ulong categoryMask = AutoResolveGetCategoryMask(entry.source.categories);

                float extant;
                if (values.TryGetValue(categoryMask, out extant)) values[categoryMask] = extant + power;
                else values[categoryMask] = power;
            }

            return values;
        }

        private int AutoResolveGetSelectedTechLevel()
        {
            int techIndex = (int)AutoResolveTechLevelNumeric.Value;
            if (techIndex < 0 || techIndex > 5) techIndex = 0;
            return techIndex;
        }

        private List<AutoResolveBuiltObject> AutoResolveBuildGarrisonEntries(unit sourceUnit, Dictionary<string, unit> unitLookup, int techIndex)
        {
            List<AutoResolveBuiltObject> entries = new List<AutoResolveBuiltObject>();
            if (sourceUnit.garrison == null) return entries;

            for (int g = 0; g < sourceUnit.garrison.Count; g++)
            {
                garrison_entry spawn = sourceUnit.garrison[g];
                int count = 0;
                if (spawn.upfront != null && spawn.upfront.Length > techIndex) count = spawn.upfront[techIndex];
                if (count <= 0 || string.IsNullOrWhiteSpace(spawn.unitname)) continue;

                unit garrisonUnit;
                if (!unitLookup.TryGetValue(spawn.unitname, out garrisonUnit)) continue;

                float garrisonPower = Math.Max(0f, garrisonUnit.cp) * count;
                if (garrisonPower <= 0f) continue;

                ulong garrisonCategoryMask = AutoResolveGetCategoryMask(garrisonUnit.categories);
                string garrisonCategory = AutoResolveGetDisplayCategoryFromMask(garrisonCategoryMask, garrisonUnit.categories);

                entries.Add(new AutoResolveBuiltObject { CategoryMask = garrisonCategoryMask, ContrastCategory = garrisonCategory, Power = garrisonPower });
            }

            return entries;
        }

        private Dictionary<ulong, float> AutoResolveBuildGarrisonCategoryPowerMap(List<autoresolve_entry> side)
        {
            Dictionary<ulong, float> values = new Dictionary<ulong, float>();
            Dictionary<string, unit> unitLookup = AutoResolveGetUnitSource()
                .GroupBy(x => x.unitname, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            int techIndex = AutoResolveGetSelectedTechLevel();

            foreach (autoresolve_entry entry in side)
            {
                int entryCount = Math.Max(1, entry.quantity);
                List<AutoResolveBuiltObject> garrisonEntries = AutoResolveBuildGarrisonEntries(entry.source, unitLookup, techIndex);

                for (int g = 0; g < garrisonEntries.Count; g++)
                {
                    AutoResolveBuiltObject garrisonEntry = garrisonEntries[g];
                    if (garrisonEntry == null) continue;

                    float power = garrisonEntry.Power * entryCount;
                    if (power <= 0f) continue;

                    ulong categoryMask = garrisonEntry.CategoryMask;

                    float extant;
                    if (values.TryGetValue(categoryMask, out extant)) values[categoryMask] = extant + power;
                    else values[categoryMask] = power;
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

        private string AutoResolveBuildSidePowerDetails(string sideName, List<autoresolve_entry> ownEntries)
        {
            StringBuilder sb = new StringBuilder();
            bool space = AutoResolveBattleTypeComboBox.SelectedIndex == 0;
            Dictionary<ulong, float> ownCategories = AutoResolveBuildCategoryPowerMap(ownEntries, space);
            Dictionary<ulong, float> garrisonCategories = AutoResolveBuildGarrisonCategoryPowerMap(ownEntries);

            foreach (KeyValuePair<ulong, float> garrison in garrisonCategories)
            {
                float extant;
                if (ownCategories.TryGetValue(garrison.Key, out extant)) ownCategories[garrison.Key] = extant + garrison.Value;
                else ownCategories[garrison.Key] = garrison.Value;
            }

            float rawPower = AutoResolveGetRawPower(ownEntries);
            float garrisonPower = garrisonCategories.Values.Sum();
            float rawPowerWithGarrison = rawPower + garrisonPower;
            float appliedPower = ownCategories.Values.Sum();
            int totalPopulation = ownEntries.Sum(x => Math.Max(0, x.source.pop) * Math.Max(1, x.quantity));

            sb.AppendLine(sideName + " listed combat power: " + rawPowerWithGarrison.ToString("0.###", CultureInfo.InvariantCulture));
            sb.AppendLine(sideName + " total population: " + totalPopulation.ToString(CultureInfo.InvariantCulture));
            if (garrisonPower > 0f)
            {
                sb.AppendLine("  Includes garrison combat power: " + garrisonPower.ToString("0.###", CultureInfo.InvariantCulture));
            }
            sb.AppendLine(sideName + " autoresolve-applied power: " + appliedPower.ToString("0.###", CultureInfo.InvariantCulture));

            if (space)
            {
                float excludedTransports = rawPowerWithGarrison - appliedPower;
                if (excludedTransports > 0f)
                {
                    sb.AppendLine("  Space mode excludes transport contribution from force totals: " + excludedTransports.ToString("0.###", CultureInfo.InvariantCulture));
                }
            }

            if (ownCategories.Count == 0)
            {
                sb.AppendLine("  No autoresolve category force entries for this side.");
                return sb.ToString().TrimEnd();
            }

            sb.AppendLine("  Force category totals:");
            foreach (KeyValuePair<ulong, float> own in ownCategories.OrderByDescending(x => x.Value))
            {
                string categoryName = AutoResolveGetDisplayCategoryFromMask(own.Key);
                if (string.IsNullOrWhiteSpace(categoryName)) categoryName = "(none)";
                sb.AppendLine("    " + categoryName + ": " + own.Value.ToString("0.###", CultureInfo.InvariantCulture));
            }

            return sb.ToString().TrimEnd();
        }

        private string AutoResolveBuildPowerDisplay()
        {
            string battleType = AutoResolveBattleTypeComboBox.SelectedIndex == 0 ? "Space" : (AutoResolveBattleTypeComboBox.SelectedIndex == 1 ? "Land" : "(not selected)");
            return "Combat Power Preview\r\n" +
                "Battle Type: " + battleType + "\r\n\r\n" +
                AutoResolveBuildSidePowerDetails("Attacker", autoResolveSideA) +
                "\r\n\r\n" +
                AutoResolveBuildSidePowerDetails("Defender", autoResolveSideB);
        }

        private void AutoResolveUpdatePowerDisplay()
        {
            AutoResolveResultTextBox.Text = AutoResolveBuildPowerDisplay();
        }

        private void FillAutoResolveFactionSelection()
        {
            if (AutoResolveSideAFactionComboBox.Items.Count == 0)
            {
                foreach (faction faction in entities.factions)
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

        private void AutoResolveInputChanged(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, AutoResolveBattleTypeComboBox))
            {
                int selectedBattleType = AutoResolveBattleTypeComboBox.SelectedIndex;
                if (selectedBattleType != autoResolveLastBattleType)
                {
                    AutoResolveClearUnitTables();
                    autoResolveLastBattleType = selectedBattleType;
                }

                FillAutoResolveUnitSelection();
                AutoResolveRebuildSideListsFromTables();
            }
            AutoResolveUpdatePowerDisplay();
        }

        private List<AutoResolveCombatant> AutoResolveBuildCombatants(List<autoresolve_entry> entries, int owner, bool space)
        {
            List<AutoResolveCombatant> combatants = new List<AutoResolveCombatant>();
            Dictionary<string, unit> unitLookup = AutoResolveGetUnitSource()
                .GroupBy(x => x.unitname, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            foreach (autoresolve_entry entry in entries)
            {
                for (int i = 0; i < Math.Max(1, entry.quantity); i++)
                {
                    AutoResolveCombatant combatant = new AutoResolveCombatant();
                    combatant.TypeName = entry.source.unitname;
                    combatant.OwnerId = owner;
                    combatant.Power = Math.Max(0.01f, entry.source.cp);
                    combatant.IsEscort = false;
                    combatant.IsTransport = entry.source.behaviors != null && entry.source.behaviors.Contains("TRANSPORT");

                    ulong combatantCategoryMask = AutoResolveGetCategoryMask(entry.source.categories);
                    combatant.CategoryMask = combatantCategoryMask;

                    int techIndex = AutoResolveGetSelectedTechLevel();
                    List<AutoResolveBuiltObject> garrisonEntries = AutoResolveBuildGarrisonEntries(entry.source, unitLookup, techIndex);
                    for (int g = 0; g < garrisonEntries.Count; g++)
                    {
                        AutoResolveBuiltObject garrisonEntry = garrisonEntries[g];
                        if (garrisonEntry == null || garrisonEntry.Power <= 0f) continue;

                        combatant.GarrisonPower += garrisonEntry.Power;
                        combatant.GarrisonEntries.Add(new AutoResolveBuiltObject { CategoryMask = garrisonEntry.CategoryMask, ContrastCategory = garrisonEntry.ContrastCategory, Power = garrisonEntry.Power });
                    }

                    combatants.Add(combatant);
                }
            }

            return combatants;
        }

        private string AutoResolveOwnerToName(int owner)
        {
            if (owner >= 0 && owner < entities.factions.Count) return entities.factions[owner].textname;
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

        private string AutoResolveEngagementReportToText(AutoResolveEngagementReport report)
        {
            if (report == null) return "";

            string ownerName = AutoResolveOwnerToName(report.AttackerOwnerId);
            string source = AutoResolveUnitNameForDisplay(report.SourceTypeName) + " [idx " + report.SourceUnitIndex.ToString(CultureInfo.InvariantCulture) + "]";
            string sourceCategory = string.IsNullOrWhiteSpace(report.SourceCategory) ? "(none)" : report.SourceCategory;
            string targetCategory = string.IsNullOrWhiteSpace(report.TargetCategory) ? "(global)" : report.TargetCategory;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(ownerName + " engagement");
            sb.AppendLine("+--------------------------------------+--------------------------------+");
            sb.AppendLine("| Unit engagement by                  | " + source);
            sb.AppendLine("| Engagement kind                     | " + report.SourceKind);
            sb.AppendLine("| Multipliers                         | hero=" + report.HeroMultiplier.ToString("0.###", CultureInfo.InvariantCulture) + ", contrast=" + report.ContrastMultiplier.ToString("0.###", CultureInfo.InvariantCulture) + ", total=" + report.TotalMultiplier.ToString("0.###", CultureInfo.InvariantCulture));
            sb.AppendLine("| Scaled power before target apply    | " + report.ScaledPower.ToString("0.###", CultureInfo.InvariantCulture));
            sb.AppendLine("| Applied power using categories      | " + report.AppliedCombatPower.ToString("0.###", CultureInfo.InvariantCulture) + " source=" + sourceCategory + " -> target=" + targetCategory);
            sb.AppendLine("| Target power change                 | " + report.TargetCategoryBefore.ToString("0.###", CultureInfo.InvariantCulture) + " -> " + report.TargetCategoryAfter.ToString("0.###", CultureInfo.InvariantCulture) + " target " + targetCategory);
            sb.AppendLine("| Target global change                | " + report.TargetGlobalBefore.ToString("0.###", CultureInfo.InvariantCulture) + " -> " + report.TargetGlobalAfter.ToString("0.###", CultureInfo.InvariantCulture));
            sb.AppendLine("| Unit base power change              | " + report.SourcePowerBefore.ToString("0.###", CultureInfo.InvariantCulture) + " -> " + report.SourcePowerAfter.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append("+--------------------------------------+--------------------------------+");
            return sb.ToString();
        }

        private string AutoResolveBuildKillListText(AutoResolveBattle battleHistory, int ownerId)
        {
            if (battleHistory == null) return "(none)";

            List<IGrouping<string, KeyValuePair<string, int>>> killed = battleHistory.Killed
                .Where(x => x.Value == ownerId)
                .GroupBy(x => x.Key ?? "(unknown)", StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key)
                .ToList();

            if (killed.Count == 0) return "(none)";

            List<string> lines = killed
                .Select(g => g.Count().ToString(CultureInfo.InvariantCulture) + "x " + AutoResolveUnitNameForDisplay(g.Key))
                .ToList();

            return string.Join(", ", lines);
        }

        private string AutoResolveAttritionReportToText(AutoResolveAttritionReport report)
        {
            if (report == null) return "";

            string sideName = report.SideIndex == 0 ? "Attacker" : (report.SideIndex == 1 ? "Defender" : "Side " + report.SideIndex.ToString(CultureInfo.InvariantCulture));
            string ownerName = AutoResolveOwnerToName(report.SideOwnerId);
            string unitName = AutoResolveUnitNameForDisplay(report.UnitTypeName);

            return sideName + " (" + ownerName + ") attrition: " + unitName +
                " | force " + report.ForceBefore.ToString("0.###", CultureInfo.InvariantCulture) +
                " -> " + report.ForceAfter.ToString("0.###", CultureInfo.InvariantCulture) +
                " | decision=" + report.Decision +
                (string.IsNullOrWhiteSpace(report.Notes) ? "" : " | " + report.Notes);
        }

        private void AutoResolveSetNumericValue(NumericUpDown control, float value)
        {
            if (control == null) return;

            decimal configured = (decimal)value;
            if (configured < control.Minimum) configured = control.Minimum;
            if (configured > control.Maximum) configured = control.Maximum;
            control.Value = configured;
        }

        private void AutoResolveApplySettingsDefaults()
        {
            AutoResolveSetNumericValue(AutoResolveAttritionAllowanceNumeric, entities.AutoResolveSettings.AttritionAllowanceFactor);
            AutoResolveSetNumericValue(AutoResolveTransportLossesNumeric, entities.AutoResolveSettings.TransportLosses);
            AutoResolveSetNumericValue(AutoResolveRetreatLoserAttritionNumeric, entities.AutoResolveSettings.RetreatLoserAttrition);
            AutoResolveSetNumericValue(AutoResolveRetreatWinnerAttritionNumeric, entities.AutoResolveSettings.RetreatWinnerAttrition);
            AutoResolveSetNumericValue(AutoResolveLoserAttritionNumeric, entities.AutoResolveSettings.LoserAttrition);
            AutoResolveSetNumericValue(AutoResolveWinnerAttritionNumeric, entities.AutoResolveSettings.WinnerAttrition);
        }

        private void AutoResolveApplyAttritionInputs(AutoResolveClass sim)
        {
            sim.LoserAttrition = (float)AutoResolveLoserAttritionNumeric.Value;
            sim.WinnerAttrition = (float)AutoResolveWinnerAttritionNumeric.Value;
            sim.RetreatLoserAttrition = (float)AutoResolveRetreatLoserAttritionNumeric.Value;
            sim.RetreatWinnerAttrition = (float)AutoResolveRetreatWinnerAttritionNumeric.Value;
            sim.AttritionAllowanceFactor = (float)AutoResolveAttritionAllowanceNumeric.Value;
            sim.TransportLosses = (float)AutoResolveTransportLossesNumeric.Value;
        }

        private string AutoResolveAttritionInputsToText()
        {
            return "Attrition inputs: loser=" + AutoResolveLoserAttritionNumeric.Value.ToString(CultureInfo.InvariantCulture) +
                ", winner=" + AutoResolveWinnerAttritionNumeric.Value.ToString(CultureInfo.InvariantCulture) +
                ", retreat loser=" + AutoResolveRetreatLoserAttritionNumeric.Value.ToString(CultureInfo.InvariantCulture) +
                ", retreat winner=" + AutoResolveRetreatWinnerAttritionNumeric.Value.ToString(CultureInfo.InvariantCulture) +
                ", allowance factor=" + AutoResolveAttritionAllowanceNumeric.Value.ToString(CultureInfo.InvariantCulture);
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
            sim.ContrastWeightProvider = AutoResolveGetContrastWeights;
            sim.CategoryMaskProvider = AutoResolveGetCategoryMask;
            sim.CategoryNameProvider = mask => AutoResolveGetDisplayCategoryFromMask(mask);
            AutoResolveApplyAttritionInputs(sim);
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
            List<string> engagementLines = new List<string>();
            List<string> attritionLines = new List<string>();

            AutoResolveHResult roundResult = sim.Combat_Round(true);
            if (roundResult == AutoResolveHResult.E_AUTORESOLVE_NOT_READY)
            {
                AutoResolveResultTextBox.Text = powerSummary + "\r\n\r\nAuto resolve combat was not ready when attempting to execute a round.";
                return;
            }

            if (roundResult == AutoResolveHResult.S_AUTORESOLVE_COMBAT_RETREAT || roundResult == AutoResolveHResult.S_AUTORESOLVE_COMBAT_OVER)
            {
                List<AutoResolveEngagementReport> engagements = sim.Get_Last_Engagements();
                for (int i = 0; i < engagements.Count; i++)
                {
                    engagementLines.Add(AutoResolveEngagementReportToText(engagements[i]));
                }

                List<AutoResolveAttritionReport> attritionReports = sim.Get_Last_Attrition_Reports();
                for (int i = 0; i < attritionReports.Count; i++)
                {
                    attritionLines.Add(AutoResolveAttritionReportToText(attritionReports[i]));
                }
            }

            int winner = sim.Who_Won();
            string winnerName = winner < 0 ? "No winner" : AutoResolveOwnerToName(winner);
            string winnerDecision = sim.Get_Last_Winner_Decision();
            string attackerName = AutoResolveOwnerToName(autoResolveSideAOwner);
            string defenderName = AutoResolveOwnerToName(autoResolveSideBOwner);

            string resolutionType = roundResult == AutoResolveHResult.S_AUTORESOLVE_COMBAT_RETREAT
                ? "Retreat"
                : (roundResult == AutoResolveHResult.S_AUTORESOLVE_COMBAT_OVER ? "Combat Over" : "Unknown");

            int retreating = sim.Side_Is_Retreating();
            string retreatingName = retreating >= 0 ? AutoResolveOwnerToName(retreating) : "None";

            AutoResolveBattle battleHistory = sim.Get_Current_Battle_History();

            string outcome =
                "Auto Resolve complete\r\n" +
                "Battle Type: " + (space ? "Space" : "Land") + "\r\n" +
                AutoResolveAttritionInputsToText() + "\r\n" +
                "Resolution: " + resolutionType + "\r\n" +
                "Retreating Side: " + retreatingName + "\r\n" +
                "Rounds: " + rounds.ToString() + "\r\n" +
                "Attacker: " + attackerName + "\r\n" +
                "Defender: " + defenderName + "\r\n" +
                "Winner: " + winnerName + "\r\n" +
                "Winner selection: " + winnerDecision + "\r\n" +
                "Attacker losses: " + AutoResolveBuildKillListText(battleHistory, autoResolveSideAOwner) + "\r\n" +
                "Defender losses: " + AutoResolveBuildKillListText(battleHistory, autoResolveSideBOwner) + "\r\n";

            string roundDetail = engagementLines.Count > 0
                ? "\r\n\r\nPer-unit engagements:\r\n" + string.Join("\r\n", engagementLines)
                : "\r\n\r\nPer-unit engagements:\r\n(no engagements recorded)";

            string attritionDetail = attritionLines.Count > 0
                ? "\r\n\r\nAttrition application:\r\n" + string.Join("\r\n", attritionLines)
                : "\r\n\r\nAttrition application:\r\n(no attrition logs recorded)";

            AutoResolveResultTextBox.Text = powerSummary + "\r\n\r\n" + outcome + roundDetail + attritionDetail;
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
            MissionText.Text = File.ReadAllText(getModFile("Scripts\\Library\\eawx-plugins\\intervention-missions\\rewards\\RewardTables_" + MissionListBox.SelectedItem + ".lua", entities));
        }

        private void NameListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            NameText.Text = File.ReadAllText(getModFile("Shipnames\\" + NameListBox.SelectedItem + ".txt", entities));
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
            SpawnText.Text = File.ReadAllText(getModFile("Scripts\\Library\\spawn-sets\\" + SpawnListBox.SelectedItem + ".lua", entities));
            SpawnPlanetListBox.Items.Clear();
            spawnSet set = entities.spawnSets.FirstOrDefault(s => String.Equals(s.name, (string)SpawnListBox.SelectedItem, StringComparison.OrdinalIgnoreCase));
            if(!(set.name is null))
            {
                foreach (string planetname in set.planets)
                {
                    planet planet = entities.Planets.FirstOrDefault(s => String.Equals(s.codename, planetname, StringComparison.OrdinalIgnoreCase));
                    if (!(planet.codename is null)) SpawnPlanetListBox.Items.Add(planet);
                }
            }
        }

        private void SpawnGoTo_Click(object sender, EventArgs e)
        {
            if (SpawnPlanetListBox.SelectedItems.Count > 0)
            {
                insert_history((int)historymaintabs.planet, 0, ((planet)SpawnPlanetListBox.SelectedItem).codename, true);
            }
        }

        private void StandardFListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            StandardFText.Text = File.ReadAllText(getModFile("Scripts\\Library\\standard-fighters\\" + StandardFListBox.SelectedItem + ".lua", entities));
        }

        private void RandomFListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            RandomFText.Text = File.ReadAllText(getModFile("Scripts\\Library\\random-fighters\\" + RandomFListBox.SelectedItem + ".lua", entities));
        }

        private string colorString(int[] color)
        {
            if (color is null) return "";
            string corenne = "";
            bool furst = true;
            foreach(int component in color)
            {
                if (furst) furst = false;
                else corenne += ", ";
                corenne += component.ToString();
            }
            return corenne;
        }

        private Color factioncolor(int[] color)
        {
            if (color is null) return Color.Transparent;
            return Color.FromArgb(color[3], color[0], color[1], color[2]);
        }

        private void FactionListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            FactionFactoryListbox.Items.Clear();
            FactionUnitInternalLabel.Text = "";
            FactionUnitAvailabilityLabel.Text = "";
            if (FactionListBox.SelectedIndex < 0)
            {
                FactionNameLabel.Text = "";
                FactionInternalLabel.Text = "Internal Name: ";
                FactionLuaNameLabel.Text = "";
                FactionAILabel.Text = "";
                FactionAliasLabel.Text = "";
                FactionAbbreviationLabel.Text = "";
                FactionColorLabel.Text = "";
                FactionTColorLabel.Text = "";
                FactionLColorLabel.Text = "";

                FactionGCListbox.Items.Clear();
                FactionUnitListBox.Items.Clear();
                FactionFactoryOptionsListBox.Items.Clear();
                return;
            }
            faction faction = entities.factions[FactionListBox.SelectedIndex];
            FactionListBox.Tag = faction;
            insert_history((int)historymaintabs.faction, 0, faction.codename);

            FactionNameLabel.Text = faction.textname;
            FactionInternalLabel.Text = "Internal Name: " + faction.codename;
            if (faction.luaname == "") FactionLuaNameLabel.Text = "";
            else FactionLuaNameLabel.Text = "Lua Name: " + faction.luaname;
            if (faction.ai == "") FactionAILabel.Text = "";
            else FactionAILabel.Text = "AI Type: " + faction.ai;
            if (faction.alias == "" || faction.alias == faction.codename) FactionAliasLabel.Text = "";
            else FactionAliasLabel.Text = "Alias: " + faction.alias;
            if (faction.abbreviation == "") FactionAbbreviationLabel.Text = "";
            else FactionAbbreviationLabel.Text = "Abbreviation: " + faction.abbreviation;
            FactionColorLabel.Text = "Color: " + colorString(faction.color);
            FactionColorLabel.BackColor = factioncolor(faction.color);
            FactionTColorLabel.Text = "Tatical Color: " + colorString(faction.tcolor);
            FactionTColorLabel.BackColor = factioncolor(faction.tcolor);
            if (faction.luaname == "") FactionLColorLabel.Text = "";
            else FactionLColorLabel.Text = "Lua Color: " + colorString(faction.lcolor);
            FactionLColorLabel.BackColor = factioncolor(faction.lcolor);

            string Luapath =  getModFile("Scripts\\Story\\GCMenu_DescriptionText.lua", entities);
            FactionDescLabel.Text = "";
            if (Luapath != "")
            {
                string[] library = File.ReadAllLines(Luapath);
                int mode = 0;
                string upper = faction.codename.ToUpper();
                foreach (string line in library)
                {
                    if (mode == 0 && CheckLuaIndex(upper, line)) mode = 1;
                    if (mode == 1 && line.Contains("Overviews")) mode = 2;
                    if (mode == 2 && line.Contains("DEFAULT"))
                    {
                        FactionDescLabel.Text = line.Substring(line.LastIndexOf("=") + 1, line.Length - line.LastIndexOf("=") - 1).Trim().Replace("\"","");
                        break;
                    }
                }
            }
            if (faction.BTS != "") FactionBTSTextBox.Text = "Behind the scenes:\n" + faction.BTS;
            else FactionBTSTextBox.Text = "";

            populateFactionGCBox();
            populateFactionUnitBox();
            foreach (unit factory in entities.structures)
            {
                if (factory.affiliations.Contains(faction.codename))
                {
                    if(entities.groundCompanies.FindIndex(s => s.reqstructures.Contains(factory.unitname) && s.affiliations.Contains(faction.codename) && s.techlevel <= 5) >= 0) FactionFactoryListbox.Items.Add(factory);
                }
            }
            foreach (unit factory in entities.spaceStructures)
            {
                if (factory.affiliations.Contains(faction.codename) && !factory.unitname.ToUpper().Contains("_DUMMY") && !factory.unitname.ToUpper().Contains("INFLUENCE_"))
                {
                    if (entities.spaceUnits.FindIndex(s => s.reqstructures.Contains(factory.unitname) && s.affiliations.Contains(faction.codename) && s.techlevel <= 5) >= 0) FactionFactoryListbox.Items.Add(factory);
                }
            }
        }

        private void populateFactionGCBox()
        {
            FactionGCListbox.Items.Clear();
            string faction = ((faction)FactionListBox.Tag).codename;
            foreach (galacticConquest GC in entities.Conquests)
            {
                bool add = false;
                if (GC.Type == GCType.Progressive && FactionProgressiveCheckBox.Checked) add = true;
                if (GC.Type == GCType.Regional && FactionRegionalCheckBox.Checked) add = true;
                if (GC.Type == GCType.Historical && FactionHistoricalCheckBox.Checked) add = true;
                if ((GC.Type == GCType.Infinity || GC.Type == GCType.InfinityLayoutCopy) && FactionInfinityCheckBox.Checked) add = true;
                if (add && GC.factionsPresent.Contains(faction)) FactionGCListbox.Items.Add(GC);
            }
        }

        private void FactionUnitTypeRB_CheckedChanged(object sender, EventArgs e)
        {
            populateFactionUnitBox();
        }

        private void FactionUnitSearchTextBox_TextChanged(object sender, EventArgs e)
        {
            populateFactionUnitBox();
        }

        private void populateFactionUnitBox()
        {
            FactionUnitListBox.Items.Clear();
            string faction = ((faction)FactionListBox.Tag).codename;
            List<unit> src = entities.spaceUnits;
            if (FactionGroundTeamRB.Checked) src = entities.groundCompanies;
            else if (FactionSpaceHeroRB.Checked) src = entities.spaceHeroes;
            else if (FactionHeroTeamRB.Checked) src = entities.heroCompanies;
            foreach (unit unit in src)
            {
                if (unit.username.ToLower().Contains(FactionUnitSearchTextBox.Text.ToLower()) && unit.affiliations.Contains(faction) && !IsSkirmishObject(unit) && !IsMissionObject(unit) && !IsSurvivalObject(unit) && !IsGroundWar(unit) && !IsTransportObject(unit) && !unit.unitname.Contains("Cheat") && unit.influence == 0)
                {
                    FactionUnitListBox.Items.Add(unit);
                }
            }
        }

        private void FactionCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            populateFactionGCBox();
        }

        private void FactionFactoryListbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(FactionFactoryListbox.SelectedItems.Count > 0)
            {
                faction faction = (faction)FactionListBox.Tag; //todo support multi select units, make level one ships not match level 4 yards
                List<string> factories = new List<string>();
                foreach (unit factory in FactionFactoryListbox.SelectedItems) factories.Add(factory.unitname);
                FactionFactoryOptionsListBox.Items.Clear();
                foreach (unit unit in entities.groundCompanies)
                {
                    if (unit.techlevel <= 5 && unit.affiliations.Contains(faction.codename))
                    {
                        bool structmatch = false;
                        foreach (string req in ReadWhiteSpaceAsCommas(unit.reqstructures))
                        {
                            if (factories.Contains(req))
                            {
                                structmatch = true;
                                break;
                            }
                        }
                        if (structmatch) FactionFactoryOptionsListBox.Items.Add(unit);
                    }
                }
                foreach (unit unit in entities.spaceUnits)
                {
                    if (unit.techlevel <= 5 && unit.affiliations.Contains(faction.codename))
                    {
                        bool structmatch = false;
                        foreach (string req in ReadWhiteSpaceAsCommas(unit.reqstructures))
                        {
                            if (factories.Contains(req))
                            {
                                if (req.Contains("_Shipyard"))
                                {//Only match shipyards at the unit's level, not below. Multiselect lets all be checked
                                    if (req.Contains("_Level_Two") && unit.level < 2) continue;
                                    if (req.Contains("_Level_Three") && unit.level < 3) continue;
                                    if (req.Contains("_Level_Four") && unit.level < 4) continue;
                                }
                                structmatch = true;
                                break;
                            }
                        }
                        if (structmatch) FactionFactoryOptionsListBox.Items.Add(unit);
                    }
                }
            }
        }

        private void FactionGovListBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void FactionGotoConquestButton_Click(object sender, EventArgs e)
        {
            if(FactionGCListbox.SelectedItems.Count > 0)
            {
                insert_history((int)historymaintabs.conquest, 0, ((galacticConquest)FactionGCListbox.SelectedItem).codename, true);
            }
        }

        private void FactionGotoFactoryButton_Click(object sender, EventArgs e)
        {
            if (FactionFactoryListbox.SelectedItems.Count > 0)
            {
                string structure = ((unit)FactionFactoryListbox.SelectedItem).unitname;
                if (entities.spaceStructures.FindIndex(s => s.unitname == structure) >= 0) insert_history((int)historymaintabs.unit, 8, structure, true);
                else insert_history((int)historymaintabs.unit, 7, structure, true);
            }
        }

        private void FactionGotoBuildableButton_Click(object sender, EventArgs e)
        {
            if (FactionFactoryOptionsListBox.SelectedItems.Count > 0)
            {
                string unit = ((unit)FactionFactoryOptionsListBox.SelectedItem).unitname;
                if (entities.spaceUnits.FindIndex(s => s.unitname == unit) >= 0) insert_history((int)historymaintabs.unit, 0, unit, true);
                else insert_history((int)historymaintabs.unit, 1, unit, true);
            }
        }

        private void FactionGotoUnitButton_Click(object sender, EventArgs e)
        {
            if (FactionUnitListBox.SelectedItems.Count > 0)
            {
                string unit = ((unit)FactionUnitListBox.SelectedItem).unitname;
                if(FactionSpaceUnitRB.Checked) insert_history((int)historymaintabs.unit, 0, unit, true);
                else if (FactionGroundTeamRB.Checked) insert_history((int)historymaintabs.unit, 1, unit, true);
                else if (FactionSpaceHeroRB.Checked) insert_history((int)historymaintabs.unit, 4, unit, true);
                else if (FactionHeroTeamRB.Checked) insert_history((int)historymaintabs.unit, 5, unit, true);
            }
        }

        private void setFactionAvailText(unit unit)
        {
            faction faction = ((faction)FactionListBox.Tag);
            FactionUnitInternalLabel.Text = "Internal Name: " + unit.unitname;
            FactionUnitAvailabilityLabel.Text = checkUnitAvailibility(unit, faction);

            string spawnsetlib = getModFile("Scripts\\Library\\spawn-sets\\"+ faction.codename.ToUpper()+".lua", entities); //TODO make sure this is still correct when modcontentloader is cut
            if(spawnsetlib != "")
            {
                string filetext = File.ReadAllText(spawnsetlib);
                if (filetext.Contains("\"" + unit.unitname + "\"")) FactionUnitAvailabilityLabel.Text += "\nStarting Force Option";
            }
        }

        private void FactionUnitListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            unit unit = (unit)FactionUnitListBox.SelectedItem;
            setFactionAvailText(unit);
        }

        private void FactionFactoryOptionsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            unit unit = (unit)FactionFactoryOptionsListBox.SelectedItem;
                setFactionAvailText(unit);
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
                float abilityregen = (selectedUnit.regen * (float)UAShieldLabel.Tag);
                UnitShieldLabel.Text = "Shields: " + abilityshield + " / [" + abilityregen.ToString("0.##") + "/R]" + ArmorTypeString(selectedUnit.shield_type);
                if (!(selectedUnit.behaviors.Contains("SHIELDED") || selectedUnit.modebehaviors.Contains("SHIELDED"))) UnitShieldLabel.Text = "NO BEHAVIOR (" + selectedUnit.shield + "/" + selectedUnit.regen + " " + ArmorTypeString(selectedUnit.shield_type) + ")";
                float regenSeconds = selectedUnit.shield * 3 / abilityregen;
                if (regenSeconds >= 0) TimeToRegenLabel.Text = "Time to Regen: ";
                else
                {
                    TimeToRegenLabel.Text = "Time to Drain: ";
                    regenSeconds *= -1;
                }
                if (float.IsInfinity(regenSeconds)) TimeToRegenLabel.Text += regenSeconds.ToString("0.##");
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
            bool pd = false;
            bool heal = false;
            foreach (unitability able in selectedUnit.unitabilities) UnitAbilityListBox.Items.Add(able);
            AbilityListBox.Items.Clear();
            foreach (ability able in selectedUnit.abilities)
            {
                AbilityListBox.Items.Add(able);
                switch (able.type)
                {
                    case "Laser_Defense_Ability": //todo: check if first or last defined has priority and set a flag if it's first
                        pd = true;
                        PDRechargeLabel.Text = "PD Recharge: " + able.recharge;
                        PDRadiusLabel.Text = "PD Radius: " + able.radius;
                        break;
                    case "Force_Healing_Ability":
                        heal = true;
                        HealScoreLabel.Text = "Heal Score: " + getHealScore(able);
                        if(able.genericValue > 0) HealAmountLabel.Text = "Heal Amount: " + able.genericValue;
                        else HealAmountLabel.Text = "Heal Percent: " + able.duration * 100 + "%";
                        HealRechargeLabel.Text = "Heal Recharge: " + able.recharge;
                        HealRadiusLabel.Text = "Heal Radius: " + able.radius;
                        break;
                }
            }
            if (!pd)
            {
                PDRechargeLabel.Text = "";
                PDRadiusLabel.Text = "";
            }
            if (!heal)
            {
                HealScoreLabel.Text = "";
                HealAmountLabel.Text = "";
                HealRechargeLabel.Text = "";
                HealRadiusLabel.Text = "";
            }
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
            foreach (string entry in SplitXMLWhitespaceList(selectedUnit.tooltip)) UnitTooltipLabelRichTextBox.Text += Find_Text_Entry(entry, entities) + "\n";
            if (selectedUnit.BTS != "") UnitBTSTextBox.Text = "Behind the scenes:\n" + selectedUnit.BTS;
            else UnitBTSTextBox.Text = "";
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
                toolTip1.SetToolTip(MapsAndBombingRunLabel, "Terrain types on a planet with a different model than the standard one for the unit. On companies, this reflects any change in constituent units: not all units need have every listed model");
            }
            else if (selectedUnit.bombingRunUnit != "")
            {
                MapsAndBombingRunLabel.Text = "Bombing run unit: " + selectedUnit.bombingRunUnit;
                toolTip1.SetToolTip(MapsAndBombingRunLabel, "The type of bomber that this unit will unlock in land tactical. Multiple entries for a carrier represent changes across tech level, not a random choice");
            }
            else MapsAndBombingRunLabel.Text = "";
            if (selectedUnit.maintenance > 0 && selectedUnit.fightermode > 0) MaintenanceLabel.Text = "Maintenance (actual/calculated): " + selectedUnit.maintenance + "/" + (selectedUnit.buildtime * 30 / 50).ToString("0"); //Maintenance is weird, don't question the formula
            else MaintenanceLabel.Text = "";
            if (selectedUnit.variantchain.Count > 0)
            {
                VariantLabel.Text = "Variant Chain: " + selectedUnit.variantchain[selectedUnit.variantchain.Count-1];
                for (int i = selectedUnit.variantchain.Count - 2; i >= 0; i--) VariantLabel.Text += ", " + selectedUnit.variantchain[i];
            }
            else VariantLabel.Text = "";
            setDPSBreakdown(true);

            UnitSubunitListbox.Items.Clear();
            if (!(selectedUnit.consolidatedUnits is null) && selectedUnit.consolidatedUnits.Count > 0)
            {
                foreach (quantizedObject subunit in selectedUnit.consolidatedUnits) UnitSubunitListbox.Items.Add(subunit);
                setSubUnitVisibility(0);
            }
            else if (selectedUnit.garrison_lua.Count > 0 || !(selectedUnit.garrison is null) && selectedUnit.garrison.Count > 0)
            {
                float fighterUpfront = 0;
                float fighterReserve = 0;
                float bomberUpfront = 0;
                float bomberReserve = 0;
                float otherUpfront = 0;
                float otherReserve = 0;
                foreach (garrison_entry spawn in selectedUnit.garrison)
                {
                    if (spawn.tech[1])
                    {
                        if (spawn.fightermode == 2)
                        {
                            bomberUpfront += spawn.squad_size * spawn.upfront[1];
                            bomberReserve += spawn.squad_size * spawn.reserve[1];
                        }
                        else if (spawn.fightermode == 1)
                        {
                            fighterUpfront += spawn.squad_size * spawn.upfront[1];
                            fighterReserve += spawn.squad_size * spawn.reserve[1];
                        }
                        else
                        {
                            otherUpfront += spawn.squad_size * spawn.upfront[1];
                            otherReserve += spawn.squad_size * spawn.reserve[1];
                        }
                    }
                }
                if (fighterUpfront > 0) ComplementLabel.Text = "Fighters: " + fighterUpfront.ToString("0.##") + " / " + fighterReserve.ToString("0.##");
                if (fighterUpfront > 0 && bomberUpfront > 0) ComplementLabel.Text += " | ";
                if (bomberUpfront > 0) ComplementLabel.Text += "Bombers: " + bomberUpfront.ToString("0.##") + " / " + bomberReserve.ToString("0.##");
                if (otherUpfront > 0)
                {
                    if (fighterUpfront > 0 || bomberUpfront > 0) ComplementLabel.Text += " | Other: ";
                    else ComplementLabel.Text = "Spawned Units: ";
                    ComplementLabel.Text += otherUpfront.ToString("0.##") + " / " + otherReserve.ToString("0.##");
                }

                if (selectedUnit.garrison_lua.Count > 0)
                {
                    setSubUnitVisibility(2);
                    ComplementXMLCheckBox.Checked = false;
                    ComplementResearchListBox.Items.Clear();
                    List<string> researches = new List<string>();
                    foreach(garrison_lua gar in selectedUnit.garrison_lua)
                    {
                        foreach(string research in gar.ResearchRequired)
                        {
                            if (!researches.Contains(research))
                            {
                                researches.Add(research);
                                ComplementResearchListBox.Items.Add(research);
                            }
                        }
                        foreach (string research in gar.ResearchForbidden)
                        {
                            if (!researches.Contains(research))
                            {
                                researches.Add(research);
                                ComplementResearchListBox.Items.Add(research);
                            }
                        }
                    }
                }
                else setSubUnitVisibility(1);


                populateSubUnitComplements();
            }
            else
            {
                ComplementLabel.Text = "";
                setSubUnitVisibility(0);
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
                    unit req = entities.spaceStructures.FirstOrDefault(s => s.unitname.ToLower() == reqstru.ToLower());
                    if(req.unitname is null) req = entities.structures.FirstOrDefault(s => s.unitname.ToLower() == reqstru.ToLower());
                    req.username = getBuildingAffils(req);
                    req.sortstring = req.username;
                    ReqStructuresListBox.Items.Add(req);
                }
            }

            FactionAvailableListbox.Items.Clear();
            foreach(string affilation in selectedUnit.affiliations)
            {
                foreach (faction faction in entities.factions)
                { 
                    if (affilation == faction.codename)
                    {
                        FactionAvailableListbox.Items.Add(faction);
                        break;
                    }
                }
            }
            AvailabilityLabel.Text = "Select a faction to show availability data";

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

            List<string> spawnsets = getModFiles("Scripts\\Library\\spawn-sets", "*.lua", entities); //TODO make sure this is still correct when modcontentloader is cut
            UnitSpawnSetListBox.Items.Clear();
            foreach (string file in spawnsets)
            {
                string filetext = File.ReadAllText(file);
                if(filetext.Contains("\""+selectedUnit.unitname+"\"")) UnitSpawnSetListBox.Items.Add(LastFolderOrFile(file).ToUpper().Replace(".LUA", ""));
            }//todo goto after history for the lookup is set up

            UnitRequiredPlanetListbox.Items.Clear();
            foreach (string planet in selectedUnit.planets)
            {
                planet planetObj = entities.Planets.FirstOrDefault(s => s.codename == planet);
                if (!(planetObj.username is null) && planetObj.username != "") UnitRequiredPlanetListbox.Items.Add(planet);
            }
            UnitGCListbox.Items.Clear();
            foreach (galacticConquest GC in entities.Conquests)
            {
                foreach (string planet in selectedUnit.planets)
                {
                    int id = entities.Conquests.FindIndex(s => s.planets.Contains(planet));
                    if(id >= 0)
                    {
                        UnitGCListbox.Items.Add(GC);
                        break;
                    }
                }
            }
            UnitDiscountListBox.Items.Clear();
            getDiscountObjects();
            foreach (unit discounter in globals.DiscountEntities)
            {
                bool found = false;
                foreach(ability able in discounter.abilities)
                {
                    if(able.type == "Reduce_Production_Price_Ability")
                    {
                        if(able.applicable_types.Length > 0)
                        {
                            if (able.applicable_types.Contains(selectedUnit.unitname))
                            {
                                UnitDiscountListBox.Items.Add(discounter);
                                found = true;
                                break;
                            }
                        }
                    }
                    if (found) break;
                }
            }
            populateHostListBox();
            //Ability and weapon sounds don't have handy auto select features
            if (!UnitSFXBasicRB.Checked && !UnitSFXAttackRB.Checked && !UnitSFXDestroyedRB.Checked && !UnitSFXAbilityRB.Checked && !UnitSFXWeaponRB.Checked) UnitSFXBasicRB.Checked = true;
            populateUnitSFXList();
        }

        private void populateHostListBox()
        {
            if (UnitListBox.Tag is null) return;
            unit selectedUnit = (unit)UnitListBox.Tag;
            UnitHostListbox.Items.Clear();
            List<string> standards = new List<string>();
            List<string> randoms = new List<string>();
            string xmlname = selectedUnit.unitname;
            string luaname = xmlname.ToUpper();

            string[] lowerSuffixes = new string[] { "" };
            string[] upperSuffixes = new string[] { "" };
            if (UnitAllSquadSizesCheckBox.Visible)
            {
                string localcut = xmlname.Replace("_Double", "").Replace("_Half", "").Replace("_Third", "").Replace("_Triple", "");
                List<string> fighterfiles = getModFiles("Scripts\\Library\\standard-fighters", "*.lua", entities);
                string upper = "\"" + localcut.ToUpper() + "\"";
                foreach (string file in fighterfiles)
                {
                    string contents = File.ReadAllText(file);
                    if (contents.Contains(upper)) standards.Add(LastFolderOrFile(file).ToUpper().Replace(".LUA", ""));
                }
                fighterfiles = getModFiles("Scripts\\Library\\random-fighters", "*.lua", entities);
                foreach (string file in fighterfiles)
                {
                    string contents = File.ReadAllText(file);
                    if (contents.Contains(upper)) randoms.Add(LastFolderOrFile(file).ToUpper().Replace(".LUA", ""));
                }
                if (UnitAllSquadSizesCheckBox.Checked)
                {
                    lowerSuffixes = new string[] { "", "_Double", "_Half", "_Third", "_Triple" };
                    upperSuffixes = new string[] { "", "_DOUBLE", "_HALF", "_THIRD", "_TRIPLE" };

                    xmlname = localcut;
                    luaname = localcut.ToUpper();
                }
            }

            bool singlemode = false;
            string singlefaction = "";
            if(FactionAvailableListbox.SelectedItems.Count > 0)
            {
                singlemode = true;
                singlefaction = ((faction)FactionAvailableListbox.SelectedItem).codename;
            }

            foreach (unit unidad in entities.objects)
            {
                //bool found = false;
                if (unidad.companyunits.Count > 0)
                {
                    if (unidad.companyunits.Contains(selectedUnit.unitname))
                    {
                        UnitHostListbox.Items.Add(unidad);
                        continue;
                    }
                    if(!(unidad.subcompanies is null))
                    {
                        if (unidad.subcompanies.FindIndex(s => s.codename == selectedUnit.unitname) >= 0)
                        {
                            UnitHostListbox.Items.Add(unidad);
                            continue;
                        }
                    }
                }
                if (unidad.garrison.Count > 0)
                {
                    /*if (unidad.garrison.FindIndex(s => s.unitname == selectedUnit.unitname) >= 0)
                    {
                        UnitHostListbox.Items.Add(unidad);
                        continue;
                    }*/
                    foreach (string suffix in lowerSuffixes)
                    {
                        if (unidad.garrison.FindIndex(s => string.Equals(s.unitname, xmlname + suffix, StringComparison.OrdinalIgnoreCase)) >= 0)
                        {
                            UnitHostListbox.Items.Add(unidad);
                            continue;
                        }
                    }
                }
                if (unidad.garrison_lua.Count > 0 && !singlemode || unidad.affiliations.Contains(singlefaction))
                {
                    foreach (string suffix in upperSuffixes)
                    {
                        if (unidad.garrison_lua.FindIndex(s => s.unitname == luaname + suffix) >= 0)
                        {
                            UnitHostListbox.Items.Add(unidad);
                            continue;
                        }
                        if (unidad.garrison_lua.FindIndex(s => s.standard && standards.Contains(luaname + suffix)) >= 0)
                        {
                            UnitHostListbox.Items.Add(unidad);
                            continue;
                        }
                        if (unidad.garrison_lua.FindIndex(s => s.random && randoms.Contains(luaname + suffix)) >= 0)
                        {
                            UnitHostListbox.Items.Add(unidad);
                            continue;
                        }
                    }
                }
            }
        }

        private void UnitAllSquadSizesCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            populateHostListBox();
        }

        private void setSubUnitVisibility(int mode)
        {
            ComplementXMLCheckBox.Visible = mode == 2;

            ComplementTechLevelLabel.Visible = mode == 1;
            ComplementTechLevelBox.Visible = mode == 1;
            ComplementLuaTechLevelLabel.Visible = mode == 2;
            ComplementLuaTechLevelBox.Visible = mode == 2;
            LuaGarrisonPanel.Visible = mode == 2;
        }

        private void populateSubUnitComplements()
        {
            UnitSubunitListbox.Items.Clear();
            if (UnitListBox.Tag is null) return;
            unit unit = (unit)UnitListBox.Tag;
            if (ComplementTechLevelBox.Visible)
            {
                int tech = (int)ComplementTechLevelBox.Value;
                foreach (garrison_entry gar in unit.garrison)
                {
                    if (gar.tech[tech])
                    {
                        garrison_lua forListBox = new garrison_lua //Needs conversion from the list of counts to count, make the Lua version so Goto doesn't have to handle three distinct cases
                        {
                            unitname = gar.unitname,
                            username = gar.username,
                            upfront = gar.upfront[tech],
                            reserve = gar.reserve[tech],
                            squad_size = gar.squad_size,
                        };
                        UnitSubunitListbox.Items.Add(forListBox);
                    }
                }
            }
            else
            {
                string owner = ((faction)ComplementFactionListBox.SelectedItem).codename.ToUpper();
                string alias = ((faction)ComplementFactionListBox.SelectedItem).alias.ToUpper();
                int tech = (int)ComplementLuaTechLevelBox.Value;

                foreach (garrison_lua gar in unit.garrison_lua)
                {
                    if(gar.tech[tech] && (gar.ownerAlias == owner || gar.ownerAlias == alias || gar.ownerAlias == "DEFAULT"))
                    {
                        if(gar.ResearchRequired.Count > 0)
                        {
                            bool match = false;
                            foreach (string research in ComplementResearchListBox.SelectedItems)
                            {
                                if(gar.ResearchRequired.FindIndex(s => s == research) >= 0)
                                {
                                    match = true;
                                    break;
                                }
                            }
                            if (!match) continue;
                        }
                        if (gar.ResearchForbidden.Count > 0)
                        {
                            bool match = true;
                            foreach (string research in ComplementResearchListBox.SelectedItems)
                            {
                                if (gar.ResearchForbidden.FindIndex(s => s == research) >= 0)
                                {
                                    match = false;
                                    break;
                                }
                            }
                            if (!match) continue;
                        }
                        UnitSubunitListbox.Items.Add(gar);
                    }
                }
            }
        }

        private void populateSubUnitComplements_Wrapper(object sender, EventArgs e)
        {
            populateSubUnitComplements();
        }

        private void ComplementXMLCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ComplementTechLevelLabel.Visible = ComplementXMLCheckBox.Checked;
            ComplementTechLevelBox.Visible = ComplementXMLCheckBox.Checked;
            ComplementLuaTechLevelLabel.Visible = !ComplementXMLCheckBox.Checked;
            ComplementLuaTechLevelBox.Visible = !ComplementXMLCheckBox.Checked;
            LuaGarrisonPanel.Visible = !ComplementXMLCheckBox.Checked;

            populateSubUnitComplements();
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
                    unit.sortstring = Find_Text_Entry(unit.unitclass, entities);
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
                    unit.sortfloat = unit.cp;
                    if (globals.UnitSortConfig.complementCP)
                    {
                        foreach(garrison_entry gar in unit.garrison)
                        {
                            if (gar.tech[1])
                            {
                                float upfrontcp = gar.cp * gar.upfront[1]; //For debug purposes
                                float reserveratio = (float)Math.Pow(0.5, (double)gar.reserve[1] / gar.upfront[1]);
                                if (gar.reserve[1] == -1) reserveratio = 0;
                                float reservecp = upfrontcp * (1 - reserveratio);
                                unit.sortfloat += upfrontcp + reservecp;
                            }
                            
                        }
                    }
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
                case UnitSortTypes.Complement:
                    unit.sortfloat = 0;
                    foreach (garrison_entry gar in unit.garrison)
                    {
                        if (gar.tech[1])
                        {
                            int mode = globals.UnitSortConfig.fighterBomberMode;
                            if ((!SpaceRadioButton.Checked && !SpaceHeroRadioButton.Checked && !SpaceStructureRadioButton.Checked) || (gar.fightermode <= 0 && (mode < 1 || mode > 3)) || (gar.fightermode == 1 && !(mode == 2 || mode == 4)) || (gar.fightermode == 2 && !(mode == 1 || mode == 4)))
                            {
                                /* All Count
                                    Fighter Count
                                    Bomber Count
                                    Fighters and Bomber Count
                                    Other Count
                                    Combat Power

                                    Upfront
                                    Reserve
                                    Upfront+Reserve */
                                if (mode == 5)
                                {
                                    float upfrontcp = gar.cp * gar.upfront[1];
                                    if (globals.UnitSortConfig.upfrontReserveMode != 1) unit.sortfloat += upfrontcp;
                                    if (globals.UnitSortConfig.upfrontReserveMode != 0)
                                    {
                                        float reserveratio = (float)Math.Pow(0.5, (double)gar.reserve[1] / gar.upfront[1]);
                                        if (gar.reserve[1] == -1) reserveratio = 0;
                                        unit.sortfloat += upfrontcp * (1 - reserveratio);
                                    }
                                }
                                else
                                {
                                    if (globals.UnitSortConfig.upfrontReserveMode != 1) unit.sortfloat += gar.upfront[1] * gar.squad_size;
                                    if (globals.UnitSortConfig.upfrontReserveMode != 0) unit.sortfloat += gar.reserve[1] * gar.squad_size;
                                }
                            }
                        }

                    }
                    break;
                case UnitSortTypes.GarrisonCap:
                    unit.sortfloat = unit.garrison_slots;
                    break;
                case UnitSortTypes.GarrisonValue:
                    unit.sortfloat = unit.garrison_value;
                    break;
                case UnitSortTypes.NameCount:
                    unit.sortfloat = findUnitNameFile(unit, entities).Length; //Todo. Caching to improve performance? 
                    break;
                case UnitSortTypes.ChainStart:
                    if (unit.variantchain.Count > 0) unit.sortstring = unit.variantchain[unit.variantchain.Count - 1];
                    else unit.sortstring = " ";
                    break;
                case UnitSortTypes.ChainEnd:
                    if (unit.variantchain.Count > 0) unit.sortstring = unit.variantchain[0];
                    else unit.sortstring = " ";
                    break;
                case UnitSortTypes.pdRecharge:
                    ability able = unit.abilities.FirstOrDefault(s => s.type == "Laser_Defense_Ability"); //todo may need to find last instead
                    if (able.recharge > 0) unit.sortfloat = able.recharge;
                    else unit.sortfloat = float.PositiveInfinity;
                    break;
                case UnitSortTypes.pdRadius:
                    ability able2 = unit.abilities.FirstOrDefault(s => s.type == "Laser_Defense_Ability");
                    unit.sortfloat = able2.radius;
                    break;
                case UnitSortTypes.Heal:
                    ability healable = unit.abilities.FirstOrDefault(s => s.type == "Force_Healing_Ability"); //todo may need to find last instead
                    if(healable.recharge > 0)
                    {
                        switch (globals.UnitSortConfig.HealMode)
                        {
                            case 0:
                                unit.sortfloat = getHealScore(healable);
                                break;
                            case 1:
                                if (healable.genericValue > 0) unit.sortfloat = healable.genericValue;
                                else unit.sortfloat = healable.duration*100;
                                break;
                            case 2:
                                unit.sortfloat = healable.genericValue;
                                break;
                            case 3:
                                unit.sortfloat = healable.duration * 100;
                                break;
                            case 4:
                                unit.sortfloat = healable.radius;
                                break;
                            case 5:
                                unit.sortfloat = healable.recharge;
                                break;
                        }
                    }
                    else unit.sortfloat = 0;
                    break;
                case UnitSortTypes.Discount://todo: consider how to handle multiple independent
                    ability cheap = unit.abilities.FirstOrDefault(s => s.type == "Reduce_Production_Price_Ability");
                    unit.sortfloat = cheap.priceReduction;
                    break;
                case UnitSortTypes.TimeReduction:
                    ability fast = unit.abilities.FirstOrDefault(s => s.type == "Reduce_Production_Time_Ability");
                    unit.sortfloat = fast.timeReduction;
                    break;
                case UnitSortTypes.incomePercent:
                    ability cash = unit.abilities.FirstOrDefault(s => s.type == "Planet_Income_Bonus_Ability");
                    unit.sortfloat = cash.percentCredits;
                    break;
                case UnitSortTypes.incomeAmount:
                    ability money = unit.abilities.FirstOrDefault(s => s.type == "Planet_Income_Bonus_Ability");
                    unit.sortfloat = money.absoluteCredits;
                    break;
                case UnitSortTypes.CommandBonus:
                    List<ability> PerStack = new List<ability>();
                    bool anymod = false;
                    foreach(ability cmd in unit.abilities)
                    {
                        int mode = globals.UnitSortConfig.CommandTypeMode;
                        int stack = cmd.stacking;
                        if (cmd.damageBonus > 0 || cmd.healthBonus > 0 || cmd.shieldBonus > 0 || cmd.defenseBonus > 0 || cmd.speedBonus > 0)
                        {
                            bool notspecial = cmd.applicable_categories.Contains("All");
                            if (notspecial && mode == 0 || mode == 1 || !notspecial && mode == 2)
                            {
                                anymod = true;
                                int id = PerStack.FindIndex(s => s.stacking == stack);
                                if(id < 0) PerStack.Add(cmd);
                                else
                                {
                                    ability stackable = PerStack[id];
                                    if (stackable.damageBonus < cmd.damageBonus) stackable.damageBonus = cmd.damageBonus;
                                    if (stackable.healthBonus < cmd.healthBonus) stackable.healthBonus = cmd.healthBonus;
                                    if (stackable.shieldBonus < cmd.shieldBonus) stackable.shieldBonus = cmd.shieldBonus;
                                    if (stackable.defenseBonus < cmd.defenseBonus) stackable.defenseBonus = cmd.defenseBonus;
                                    if (stackable.speedBonus < cmd.speedBonus) stackable.speedBonus = cmd.speedBonus;
                                    PerStack[id] = stackable;
                                }
                            }
                        }
                        if(mode < 2 && cmd.type == "Battlefield_Modifier_Ability") //Vision mods are always universal
                        {
                            anymod = true;
                            int id = PerStack.FindIndex(s => s.stacking == stack);
                            if (id < 0) PerStack.Add(cmd);
                            else
                            {
                                ability stackable = PerStack[id];
                                if (stackable.genericValue < cmd.genericValue) stackable.genericValue = cmd.genericValue;
                                PerStack[id] = stackable;
                            }
                        }
                    }
                    if (anymod)
                    {
                        float dmg = 0;
                        float hp = 0;
                        float sh = 0;
                        float def = 0;
                        float speed = 0;
                        float fow = 0;
                        foreach (ability stacked in PerStack)
                        {
                            if (stacked.damageBonus > 0 && globals.UnitSortConfig.CommandCategories[0]) dmg += stacked.damageBonus;
                            if (stacked.healthBonus > 0 && globals.UnitSortConfig.CommandCategories[1]) hp += stacked.healthBonus;
                            if (stacked.shieldBonus > 0 && globals.UnitSortConfig.CommandCategories[2]) sh += stacked.shieldBonus;
                            if (stacked.defenseBonus > 0 && globals.UnitSortConfig.CommandCategories[3]) def += stacked.defenseBonus;
                            if (stacked.speedBonus > 0 && globals.UnitSortConfig.CommandCategories[4]) speed += stacked.speedBonus;
                            if (stacked.genericValue > 0 && globals.UnitSortConfig.CommandCategories[5]) fow += stacked.genericValue - 1; //Fow is special and is centered around 1
                        }

                        switch (globals.UnitSortConfig.CommandMode)
                        {
                            case 0:
                                unit.sortfloat = Math.Max(dmg, Math.Max(hp, Math.Max(sh, Math.Max(def, Math.Max(speed, fow)))));
                                break;
                            case 1:
                                float corenne = 0;
                                float den = 0;
                                if (dmg > 0)
                                {
                                    corenne += dmg;
                                    den++;
                                }
                                if (hp > 0)
                                {
                                    corenne += hp;
                                    den++;
                                }
                                if (sh > 0)
                                {
                                    corenne += sh;
                                    den++;
                                }
                                if (def > 0)
                                {
                                    corenne += def;
                                    den++;
                                }
                                if (speed > 0)
                                {
                                    corenne += speed;
                                    den++;
                                }
                                if (fow > 0)
                                {
                                    corenne += fow;
                                    den++;
                                }
                                unit.sortfloat = corenne / den;
                                break;
                        }
                    }
                    else unit.sortfloat = 0;
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
                }//Todo: might need to prevent division by 0? But if that results in an infinity symbol it's probably ok
                if (denom < 0) denom = 1;
                unit.sortfloat /= denom;
            }
            return unit;
        }

        private bool filterUnit(unit unit)
        {
            if (globals.UnitFilterConfig.factions.Count > 0)
            {
                bool match = false;
                foreach (faction filter in globals.UnitFilterConfig.factions)
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

            if (globals.UnitFilterConfig.complementMode > 0)
            {
                if (unit.garrison.Count > 0)
                {
                    if (globals.UnitFilterConfig.complementMode == 2) return false;
                }
                else
                {
                    if (globals.UnitFilterConfig.complementMode == 1) return false;
                }
            }

            if (!categoryFilter(unit, globals.UnitFilterConfig.skirmishModes)) return false;

            if (globals.UnitFilterConfig.pdMode > 0)
            {
                if (unit.abilities.FindIndex(s => s.type == "Laser_Defense_Ability") >= 0)
                {
                    if (globals.UnitFilterConfig.pdMode == 2) return false;
                }
                else
                {
                    if (globals.UnitFilterConfig.pdMode == 1) return false;
                }
            }

            if (globals.UnitFilterConfig.healMode > 0)
            {
                if (unit.abilities.FindIndex(s => s.type == "Force_Healing_Ability") >= 0)
                {
                    if (globals.UnitFilterConfig.healMode == 2) return false;
                }
                else
                {
                    if (globals.UnitFilterConfig.healMode == 1) return false;
                }
            }

            if (globals.UnitFilterConfig.discountMode > 0)
            {
                if (unit.abilities.FindIndex(s => s.type == "Reduce_Production_Price_Ability") >= 0)
                {
                    if (globals.UnitFilterConfig.discountMode == 2) return false;
                }
                else
                {
                    if (globals.UnitFilterConfig.discountMode == 1) return false;
                }
            }

            if (globals.UnitFilterConfig.incomeMode > 0)
            {
                if (unit.abilities.FindIndex(s => s.type == "Planet_Income_Bonus_Ability") >= 0)
                {
                    if (globals.UnitFilterConfig.incomeMode == 2) return false;
                }
                else
                {
                    if (globals.UnitFilterConfig.incomeMode == 1) return false;
                }
            }

            if (globals.UnitFilterConfig.commandMode > 0)
            {
                if (unit.abilities.FindIndex(s => s.type == "Combat_Bonus_Ability") >= 0)
                {
                    if (globals.UnitFilterConfig.commandMode == 2) return false;
                }
                else
                {
                    if (globals.UnitFilterConfig.commandMode == 1) return false;
                }
            }

            if (SpaceRadioButton.Checked) //This should not be relevant to other settings
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
            if (Building.unitname is null) return "";
            if (!Building.unitname.Contains("_HQ") && Building.affiliations.Count > 0 && Building.affiliations.Count <= 2 && Building.affiliations[0] != "Neutral")
            {
                Building.username += " (" + FactionNameFromCode(Building.affiliations[0], entities);
                for (int j = 1; j < Building.affiliations.Count; j++) Building.username += ", " + FactionNameFromCode(Building.affiliations[j],entities);
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
                if (unit.variantbase != "Infantry_Dummy_Template" && unit.unitname != "Infantry_Dummy_Template" && (!StructureRadioButton.Checked || !SpaceStructureRadioButton.Checked || (unit.hp > 1 && !unit.flags.Contains("NotOpportunityTarget"))) )
                {
                    if ((search == "" || (unit.username).ToLower().Contains(search.ToLower())) && filterUnit(unit))
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

            if (mode == 3) UnitAllSquadSizesCheckBox.Visible = true;
            else UnitAllSquadSizesCheckBox.Visible = false;

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
            if (hardpoint.blastRadius > 0) corenne *= (float)UnitAoEBox.Value;
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

                if (selected.firesound != "" || selected.diesound != "")
                {
                    UnitSFXBasicRB.Checked = false;
                    UnitSFXAttackRB.Checked = false;
                    UnitSFXDestroyedRB.Checked = false;
                    UnitSFXAbilityRB.Checked = false;
                    UnitSFXWeaponRB.Checked = false;
                    UnitSFXListbox.Items.Clear();
                    UnitSampleListBox.Items.Clear();
                    if(selected.firesound != "")
                    {
                        sfx sfx = entities.sfx.FirstOrDefault(s => s.name == selected.firesound);
                        if (!(sfx.name is null) && !string.Equals(sfx.name, "null", StringComparison.OrdinalIgnoreCase))
                        {
                            sfx.displayname = "Fire Sound";
                            UnitSFXListbox.Items.Add(sfx);
                        }
                    }
                    if (selected.diesound != "")
                    {
                        sfx sfx = entities.sfx.FirstOrDefault(s => s.name == selected.diesound);
                        if (!(sfx.name is null) && !string.Equals(sfx.name, "null", StringComparison.OrdinalIgnoreCase))
                        {
                            sfx.displayname = "Hardpoint Death";
                            UnitSFXListbox.Items.Add(sfx);
                        }
                    }
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
            UnitSortTypes[] redo = { UnitSortTypes.dpsRaw, UnitSortTypes.dpsAvg, UnitSortTypes.dpsArmor, UnitSortTypes.dpsShield };
            if (redo.Contains(globals.UnitSortConfig.SortType)) populateUnitListbox();
        }

        private void UnitAoEBox_ValueChanged(object sender, EventArgs e)
        {
            setTargetDPS();
            if (UnitListBox.SelectedItems.Count > 0) setDPSBreakdown();
            UnitSortTypes[] redo = { UnitSortTypes.dpsRaw, UnitSortTypes.dpsAvg, UnitSortTypes.dpsArmor, UnitSortTypes.dpsShield };
            if (redo.Contains(globals.UnitSortConfig.SortType)) populateUnitListbox();
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
            SFXPanel,
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
                UnitSFXPanel.Tag = UnitSFXPanel.Height;
                CollapseUnitSFXPanel.Tag = UnitSFXPanel.Location.Y - UnitAbilityPanel.Location.Y - UnitAbilityPanel.Height;
                UnitBTSPanel.Tag = UnitBTSPanel.Location.Y - UnitSFXPanel.Location.Y - UnitSFXPanel.Height; //Last is also a special case//UnitBTSPanel.Height;
            }

            int StatSize = (int)UnitStatPanel.Tag;
            int StatInterval = (int)CollapseUnitStatPanel.Tag;
            int AvailSize = (int)UnitAvailPanel.Tag;
            int AvailInterval = (int)CollapseUnitAvailPanel.Tag;
            int SubunitSize = (int)UnitSubunitPanel.Tag;
            int SubunitInterval = (int)CollapseUnitSubunitPanel.Tag;
            int AbilitySize = (int)UnitAbilityPanel.Tag;
            int AbilityInterval = (int)CollapseUnitAbilityPanel.Tag;
            int SFXSize = (int)UnitSFXPanel.Tag;
            int SFXInterval = (int)CollapseUnitSFXPanel.Tag;
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
                case UnitPanels.SFXPanel:
                    if (UnitSFXPanel.Height == 0)
                    {
                        UnitSFXPanel.Height = SFXSize;
                        setExpandedButton(CollapseUnitSFXPanel);
                    }
                    else
                    {
                        UnitSFXPanel.Height = 0;
                        setCollapsedButton(CollapseUnitSFXPanel, "Sounds");
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

            Yvalue = UnitAbilityPanel.Location.Y + Math.Max(UnitAbilityPanel.Height, CollapseUnitAbilityPanel.Height) + AbilityInterval;
            UnitSFXPanel.Location = new Point(UnitSFXPanel.Location.X, Yvalue);
            CollapseUnitSFXPanel.Location = new Point(CollapseUnitSFXPanel.Location.X, Yvalue);

            Yvalue = UnitSFXPanel.Location.Y + Math.Max(UnitSFXPanel.Height, CollapseUnitSFXPanel.Height) + BTSInterval;
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

        private void CollapseUnitSFXPanel_Click(object sender, EventArgs e)
        {
            collapsePanels(UnitPanels.SFXPanel);
        }

        private void gotoUnitUniversal(string obj)
        {
            int subtype = 0;
            if (entities.spaceUnits.FindIndex(s => s.unitname == obj) >= 0) subtype = 0;
            if (entities.groundCompanies.FindIndex(s => s.unitname == obj) >= 0) subtype = 1;
            if (entities.groundUnits.FindIndex(s => s.unitname == obj) >= 0) subtype = 2;
            if (entities.fighters.FindIndex(s => s.unitname == obj) >= 0) subtype = 3;
            if (entities.spaceHeroes.FindIndex(s => s.unitname == obj) >= 0) subtype = 4;
            if (entities.heroCompanies.FindIndex(s => s.unitname == obj) >= 0) subtype = 5;
            if (entities.groundHeroes.FindIndex(s => s.unitname == obj) >= 0) subtype = 6;
            if (entities.structures.FindIndex(s => s.unitname == obj) >= 0) subtype = 7;
            if (entities.spaceStructures.FindIndex(s => s.unitname == obj) >= 0) subtype = 8;
            insert_history((int)historymaintabs.unit, subtype, obj, true);
        }

        private void UnitSubunitGotoButton_Click(object sender, EventArgs e)
        {
            if (UnitSubunitListbox.SelectedItems.Count > 0)
            {
                string obj = "";
                if(ComplementTechLevelBox.Visible || ComplementLuaTechLevelBox.Visible)
                {
                    garrison_lua gar = ((garrison_lua)UnitSubunitListbox.SelectedItem);
                    obj = gar.unitname;
                    if(gar.standard)
                    {
                        insert_history((int)historymaintabs.lookups, (int)lookupsubtabs.lkStandard, obj, true);
                        return;
                    }
                    if (gar.random)
                    {
                        insert_history((int)historymaintabs.lookups, (int)lookupsubtabs.lkRandom, obj, true);
                        return;
                    }
                }
                else
                {
                    obj = ((quantizedObject)UnitSubunitListbox.SelectedItem).codename;
                    /*int subtype = 2;
                    if (SpaceRadioButton.Checked) subtype = 0;
                    else if (SpaceHeroRadioButton.Checked) subtype = 3; //Todo needs to handle unit and heroes
                    else if (HeroCompaniesRadioButton.Checked) subtype = 4;
                    insert_history((int)historymaintabs.unit, subtype, ((quantizedObject)UnitSubunitListbox.SelectedItem).codename, true);*/
                }
                gotoUnitUniversal(obj);
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
            filter.factions = entities.factions;
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
                UnitFilterTypeLabel.Text = filter.filterDocumentation;

                populateUnitListbox();
            }
        }

        private void readErrorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (entities.readerrors == "" || entities.readerrors is null) MessageBox.Show("No errors detectable by Holocron found");
            else
            {
                System.Windows.Forms.Clipboard.SetText(entities.readerrors);
                MessageBox.Show("Errors copied to clipboard" + entities.readerrors);
            }
        }

        private void GoToReqStructButton_Click(object sender, EventArgs e)
        {
            if (ReqStructuresListBox.SelectedItems.Count > 0)
            {
                if (GroundRadioButton.Checked || HeroCompaniesRadioButton.Checked) insert_history((int)historymaintabs.unit, 7, ((unit)ReqStructuresListBox.SelectedItem).unitname, true);
                else insert_history((int)historymaintabs.unit, 8, ((unit)ReqStructuresListBox.SelectedItem).unitname, true);
            }
        }

        private void UnitSpawnGotoButton_Click(object sender, EventArgs e)
        {
            if (UnitSpawnSetListBox.SelectedItems.Count > 0) insert_history((int)historymaintabs.lookups, (int)lookupsubtabs.lkSpawn, (string)UnitSpawnSetListBox.SelectedItem, true);
        }

        private void UnitGotoPlanetButton_Click(object sender, EventArgs e)
        {
            if (UnitRequiredPlanetListbox.SelectedItems.Count > 0) insert_history((int)historymaintabs.planet, 0, (string)UnitRequiredPlanetListbox.SelectedItem, true);
        }

        private void UnitGCGotoButton_Click(object sender, EventArgs e)
        {
            if (UnitGCListbox.SelectedItems.Count > 0)insert_history((int)historymaintabs.conquest, 0, ((galacticConquest)UnitGCListbox.SelectedItem).codename, true);
        }

        private void UnitDiscountGotoButton_Click(object sender, EventArgs e)
        {
            if (UnitDiscountListBox.SelectedItems.Count > 0) gotoUnitUniversal(((unit)UnitDiscountListBox.SelectedItem).unitname);
        }

        private void UnitGotoHostButton_Click(object sender, EventArgs e)
        {
            if (UnitHostListbox.SelectedItems.Count > 0) gotoUnitUniversal(((unit)UnitHostListbox.SelectedItem).unitname);
        }

        private void FactionAvailableListbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (FactionAvailableListbox.SelectedItems.Count > 0)
            {
                unit unit = (unit)UnitListBox.Tag;
                if (SpaceRadioButton.Checked || GroundRadioButton.Checked)
                {
                    AvailabilityLabel.Text = checkUnitAvailibility(unit, (faction)FactionAvailableListbox.SelectedItem);
                }
                else if (SpaceHeroRadioButton.Checked || HeroCompaniesRadioButton.Checked)
                {
                    AvailabilityLabel.Text = ""; //todo lots of script parsing
                }
                else AvailabilityLabel.Text = ""; //Structures could get something?
            }
            else AvailabilityLabel.Text = "";

            populateHostListBox();
        }

        private string readstatearray(bool[] statearray)
        {
            string corenne = "";
            bool laststate = false;
            int lastindex = -1;
            bool furst = true;
            for(int i = 0; i < statearray.Length; i++)
            {
                bool state = statearray[i];
                if (state && !laststate)
                {
                    if (furst) furst = false;
                    else corenne += ", ";
                    corenne += (i+1).ToString(); //Convert 0 index to 1 based
                    lastindex = i+1;
                }
                if (!state && laststate && i != lastindex)
                {
                    corenne += "-"+i.ToString();
                }
                laststate = state;
            }
            if (laststate && (statearray.Length) != lastindex) corenne += "-" + statearray.Length.ToString();
            return corenne;
        }

        private string checkUnitAvailibility(unit unit, faction faction)
        {
            string locks = "";
            string unlocks = "";
            bool firstlock = true;
            bool firstunlock = true;
            if (unit.fightermode > 0) return "";
            if (unit.techlevel > 5) locks = "Locked by tech level"; //todo add locked by req structures w/o affil (TR Hutt Keldabe)
            else
            {
                unlocks = "Unlocks: ";
                if (unit.locked < 1)
                {
                    unlocks += "Default";
                    firstunlock = false;
                }

                locks += "Locks: ";
                if (unit.locked == 1)
                {
                    locks += "Default";
                    firstlock = false;
                }

                string factionlower = faction.codename.ToLower();
                string unitlower = "\""+unit.unitname.ToLower()+"\"";

                //Tech states
                List<string> statefiles = getModFiles("Scripts\\Library\\eawx-states\\tech", "*.lua", entities);
                bool[] lockarray = new bool[statefiles.Count];
                bool[] unlockarray = new bool[statefiles.Count];
                foreach (string statefile in statefiles)
                {
                    string[] statedata = File.ReadAllText(statefile).ToLower().Replace('(', ')').Split(')');

                    bool parsenext = false;
                    foreach (string chunk in statedata)
                    {
                        if (parsenext)
                        {
                            parsenext = false;
                            string[] lazyparse2 = chunk.Replace('{', '}').Split('}');
                            if (lazyparse2.Length == 3)
                            {
                                if (lazyparse2[0].Contains(factionlower) && lazyparse2[1].Contains(unitlower))
                                {
                                    string techstring = LastFolderOrFile(statefile).ToLower();//.Replace("-era-", " ").Replace(".lua", "");
                                    int techid = -1;
                                    if (techstring.Contains("one"))
                                    {
                                        techid = 0;
                                    }
                                    else if (techstring.Contains("two"))
                                    {
                                        techid = 1;
                                    }
                                    else if (techstring.Contains("three"))
                                    {
                                        techid = 2;
                                    }
                                    else if (techstring.Contains("four"))
                                    {
                                        techid = 3;
                                    }
                                    else if (techstring.Contains("five"))
                                    {
                                        techid = 4;
                                    }
                                    else if (techstring.Contains("six"))
                                    {
                                        techid = 5;
                                    }
                                    else if (techstring.Contains("seven"))
                                    {
                                        techid = 6;
                                    }
                                    else if (techstring.Contains("eight"))
                                    {
                                        techid = 7;
                                    }
                                    else if (techstring.Contains("nine"))
                                    {
                                        techid = 8;
                                    }
                                    else if (techstring.Contains("ten"))
                                    {
                                        techid = 9;
                                    }
                                    else if (techstring.Contains("eleven"))
                                    {
                                        techid = 10;
                                    }
                                    else if (techstring.Contains("twelve"))
                                    {
                                        techid = 11;
                                    }
                                    if (techid < 0) break;


                                    if (lazyparse2[2].Contains("false"))
                                    {
                                        lockarray[techid] = true;
                                        break;
                                    }
                                    else
                                    {
                                        unlockarray[techid] = true;
                                        break;
                                    }
                                }
                            }
                        }
                        else if (chunk.Contains("unitutil.setlocklist")) parsenext = true;
                    }
                }
                //Turn final lockarrays into human readable results
                string states = readstatearray(unlockarray);
                if(states != "")
                {
                    if (firstunlock) firstunlock = false;
                    else unlocks += ", ";
                    unlocks += "Tech " + states;
                }
                states = readstatearray(lockarray);
                if (states != "")
                {
                    if (firstlock) firstlock = false;
                    else locks += ", ";
                    locks += "Tech " + states;
                }

                //Research
                string research = getModFile("Scripts\\Library\\eawx-plugins\\tech-handler\\TechHandler.lua", entities);
                if(research != "")
                {
                    string[] statedata = File.ReadAllText(research).Replace('(', ')').Split(')');

                    bool parsenext = false;
                    string researchname = "";
                    foreach (string chunk in statedata)
                    {
                        if (parsenext)
                        {
                            parsenext = false;
                            int fStart = -1;
                            int uStart = -1;
                            int lStart = -1;
                            int hStart = -1;

                            int commaCount = 0;
                            bool inArray = false;
                            bool breakout = false;

                            for (int i = 0; i < chunk.Length; i++)
                            {
                                char car = chunk[i];
                                if (!inArray && car == ',')
                                {
                                    commaCount++;
                                    switch (commaCount)
                                    {
                                        case 3:
                                            fStart = i+1;
                                            break;
                                        case 4:
                                            //if factions don't match, move one
                                            if(!chunk.Substring(fStart, i - 1 - fStart).ToLower().Contains("\""+factionlower+ "\"")) breakout = true;
                                            uStart = i + 1;
                                            break;
                                        case 5:
                                            if (chunk.Substring(uStart, i - 1 - uStart).ToLower().Contains(unitlower))
                                            {
                                                if (firstunlock) firstunlock = false;
                                                else unlocks += ", ";
                                                unlocks += researchname;
                                            }
                                            lStart = i + 1;
                                            break;
                                        case 6:
                                            if (chunk.Substring(lStart, i - 1 - lStart).ToLower().Contains(unitlower))
                                            {
                                                if (firstlock) firstlock = false;
                                                else locks += ", ";
                                                locks += researchname;
                                                breakout = true; //all data parsed, no reason to keep going
                                            }
                                            hStart = i + 1;
                                            break;
                                        /*case 7: todo: pull hero spawns from this. Move breakout
                                            if (chunk.Substring(hStart, i - 1 - hStart).Contains(unitlower))
                                            {
                                                
                                            }
                                            break;*/
                                    }
                                }
                                if (car == '{') inArray = true;
                                if (car == '}') inArray = false;
                                if (breakout) break;
                            }
                        }
                        if (chunk.Contains("GenericResearch"))
                        {
                            int selfdot = chunk.LastIndexOf('.');
                            if(selfdot >= 0)
                            {
                                researchname = chunk.Substring(selfdot+1, chunk.LastIndexOf('=') - selfdot - 2);
                                parsenext = true;
                            }
                        }
                    }
                }

                //todo regimes

                //GC master scripts
                foreach(galacticConquest GC in entities.Conquests)
                {
                    bool GClock = false;
                    bool GCunlock = false;
                    foreach (string plotfile in GC.StoryPlots)
                    {
                        XmlDocument doc = readModXmlOrMeg("XML\\" + plotfile, entities);
                        XmlNodeList Luas = doc.SelectNodes("descendant::Lua_Script");
                        foreach(XmlNode Lua in Luas)
                        {
                            if(!(Lua.InnerText is null))
                            {
                                string statefile = getModFile("Scripts\\Story\\" + Lua.InnerText.Trim() + ".lua", entities);
                                if(statefile != "")
                                {
                                    string[] lines = File.ReadAllLines(statefile);
                                    List<string> factionaliases = new List<string>();
                                    List<string> unitaliases = new List<string>();

                                    for (int i = 0; i < lines.Length; i++)
                                    {
                                        string line = lines[i].ToLower();
                                        if (line.Contains("find_player") && line.Contains(factionlower)) factionaliases.Add(line.Split('=')[0].Trim());
                                        if (line.Contains("find_object_type") && line.Contains(unitlower)) unitaliases.Add(line.Split('=')[0].Trim());
                                        bool u = line.Contains("unlock_tech");
                                        bool l = line.Contains(".lock_tech");
                                        if ((u || l) && (lines.Contains(factionlower) || factionaliases.Any(line.Contains)) && (lines.Contains(unitlower) || unitaliases.Any(line.Contains)))
                                        {
                                            if (u)
                                            {
                                                if (!GCunlock)
                                                {
                                                    GCunlock = true;
                                                    if (firstunlock) firstunlock = false;
                                                    else unlocks += ", ";
                                                    unlocks += GC.username;
                                                }
                                            }
                                            else if (l)
                                            {
                                                if (!GClock)
                                                {
                                                    GClock = true;
                                                    if (firstlock) firstlock = false;
                                                    else locks += ", ";
                                                    locks += GC.username;
                                                }
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                //Custom 


                //Unit is capable of being unlocked
            }

            string missionfile = getModFile("Scripts\\Library\\eawx-plugins\\intervention-missions\\rewards\\RewardTables_" + faction.codename.ToUpper() + ".lua", entities);
            if (File.Exists(missionfile))
            {
                string filetext = File.ReadAllText(missionfile);
                string[] split = filetext.Split('=');
                bool furst = true;
                for(int i = 1; i < split.Length; i++)
                {
                    if(split[i].Contains("\"" + unit.unitname + "\""))
                    {
                        if (furst)
                        {
                            locks += "\nMission Reward: ";
                            furst = false;
                        }
                        else locks += ", ";
                        string last = split[i - 1];
                        int newline = last.LastIndexOf("\n");
                        locks += last.Substring(newline+1, last.Length - newline - 1).Trim();
                    }
                }
            }
            return unlocks+"\n"+locks;
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
                UnitAbilityDescLabel.Text = Find_Text_Entry(able.desc, entities);

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

                if(able.sound != "" || able.deactivatesound != "")
                {
                    UnitSFXBasicRB.Checked = false;
                    UnitSFXAttackRB.Checked = false;
                    UnitSFXDestroyedRB.Checked = false;
                    UnitSFXAbilityRB.Checked = false;
                    UnitSFXWeaponRB.Checked = false;
                    UnitSFXListbox.Items.Clear();
                    UnitSampleListBox.Items.Clear();
                    if (able.sound != "")
                    {
                        sfx sfx = entities.sfx.FirstOrDefault(s => s.name == able.sound);
                        if (!(sfx.name is null) && !string.Equals(sfx.name, "null", StringComparison.OrdinalIgnoreCase))
                        {
                            sfx.displayname = "Unit Ability Sound";
                            UnitSFXListbox.Items.Add(sfx);
                        }
                    }
                    if (able.deactivatesound != "")
                    {
                        sfx sfx = entities.sfx.FirstOrDefault(s => s.name == able.deactivatesound);
                        if (!(sfx.name is null) && !string.Equals(sfx.name, "null", StringComparison.OrdinalIgnoreCase))
                        {
                            sfx.displayname = "Deactivation Sound";
                            UnitSFXListbox.Items.Add(sfx);
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
                    if (able.duration > 0 && able.type != "Force_Healing_Ability") AbilityTimeLabel.Text = "Duration: " + able.duration.ToString("0") + " ";
                    if (able.recharge > 0) AbilityTimeLabel.Text += "Recharge: " + able.recharge.ToString("0");
                    if (able.genericValue > 0 && able.type != "Force_Healing_Ability") AbilityValueLabel.Text = "Value: " + able.genericValue;
                    else if (able.type == "Force_Healing_Ability")
                    {
                        if (able.genericValue > 0) AbilityValueLabel.Text = "Heal Amount: " + able.genericValue + " ";
                        if (able.duration > 0 ) AbilityValueLabel.Text = "Heal Percent: " + able.duration*100+"%";
                    }
                    else AbilityValueLabel.Text = "";
                    if (able.percentCredits > 0) AbilityValueLabel.Text = "Planet Income Increase: " + able.percentCredits * 100 + "%";
                    if (able.absoluteCredits > 0) AbilityValueLabel.Text = "Planet Income Addition: " + able.absoluteCredits;
                    if (able.priceReduction > 0) AbilityValueLabel.Text = "Price Reduction: " + able.priceReduction * 100 + "%";
                    if (able.timeReduction > 0) AbilityValueLabel.Text = "Time Reduction: " + able.timeReduction * 100 + "%";
                    AbilityActivationRadiusLabel.Text = "";
                    if (able.minradius > 0) AbilityActivationRadiusLabel.Text = "Min Activation: " + able.minradius + " ";
                    if (able.maxradius > 0) AbilityActivationRadiusLabel.Text += "Max Activation: " + able.maxradius;
                    if (able.type == "Force_Healing_Ability" && !able.genericBool) AbilityActivationRadiusLabel.Text += "Heals all units in radius";
                    if (able.radius > 0) AbilityRadiusLabel.Text = "Radius: " + able.radius;
                    else AbilityRadiusLabel.Text = "";
                    if (able.linkedEntity != "") AbilityLinkedLabel.Text = "Linked Object: " + able.linkedEntity;
                    else AbilityLinkedLabel.Text = "";
                }
                if (able.stacking >= 0) AbilityStackingLabel.Text = "Stacking Category: " + able.stacking;
                else AbilityStackingLabel.Text = "";

                if (able.sound != "")
                {
                    UnitSFXBasicRB.Checked = false;
                    UnitSFXAttackRB.Checked = false;
                    UnitSFXDestroyedRB.Checked = false;
                    UnitSFXAbilityRB.Checked = false;
                    UnitSFXWeaponRB.Checked = false;
                    UnitSFXListbox.Items.Clear();
                    UnitSampleListBox.Items.Clear();
                    sfx sfx = entities.sfx.FirstOrDefault(s => s.name == able.sound);
                    if (!(sfx.name is null) && !string.Equals(sfx.name, "null", StringComparison.OrdinalIgnoreCase))
                    {
                        sfx.displayname = "Ability Sound";
                        UnitSFXListbox.Items.Add(sfx);
                    }
                }
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
                MessageBox.Show("Unit list saved to file");
            }
        }

        private void getDiscountCategory(List<unit> src)
        {
            foreach (unit unit in src)
            {
                bool toadd = false;
                foreach (ability able in unit.abilities)
                {
                    if (able.type == "Reduce_Production_Price_Ability")
                    {
                        toadd = true;
                        break;
                    }
                }
                if (toadd) globals.DiscountEntities.Add(unit);
            }
        }

        private void getDiscountObjects()
        {
            if (globals.DiscountEntities.Count == 0)
            {
                getDiscountCategory(entities.structures);
                getDiscountCategory(entities.spaceStructures);
                getDiscountCategory(entities.groundHeroes);
                getDiscountCategory(entities.spaceHeroes);
            }
        }

        private void populateUnitSFXList()
        {
            UnitSFXListbox.Items.Clear();
            UnitSampleListBox.Items.Clear();
            if (UnitListBox.Tag is null) return;
            unit unit = (unit)UnitListBox.Tag;
            if (UnitSFXBasicRB.Checked)
            {
                for (int i = 0; i < unit.BasicSFXEvents.Length; i++)
                {
                    if(unit.BasicSFXEvents[i] != "")
                    {
                        sfx sfx = entities.sfx.FirstOrDefault(s => s.name == unit.BasicSFXEvents[i]);
                        if(!(sfx.name is null) && !string.Equals(sfx.name, "null", StringComparison.OrdinalIgnoreCase))
                        {
                            sfx.displayname = Enum.GetName(typeof(basicSoundTypes), i);
                            sfx.displayname = sfx.displayname.Replace("SFXEvent_", "").Replace("_", " ");
                            UnitSFXListbox.Items.Add(sfx);
                        }
                    }
                }
            }
            else if (UnitSFXAttackRB.Checked)
            {
                for (int i = 0; i < unit.SFXEvent_Attack_Hardpoint.Count; i++)
                {
                    sfx sfx = entities.sfx.FirstOrDefault(s => s.name == unit.SFXEvent_Attack_Hardpoint[i]);
                    if (!(sfx.name is null) && !string.Equals(sfx.name, "null", StringComparison.OrdinalIgnoreCase))
                    {
                        sfx.displayname = unit.SFXEvent_Attack_Hardpoint_Type[i];
                        UnitSFXListbox.Items.Add(sfx);
                    }
                }
            }
            else if (UnitSFXDestroyedRB.Checked)
            {
                for (int i = 0; i < unit.SFXEvent_Hardpoint_Destroyed.Count; i++)
                {
                    sfx sfx = entities.sfx.FirstOrDefault(s => s.name == unit.SFXEvent_Hardpoint_Destroyed[i]);
                    if (!(sfx.name is null) && !string.Equals(sfx.name, "null", StringComparison.OrdinalIgnoreCase))
                    {
                        sfx.displayname = unit.SFXEvent_Hardpoint_Destroyed_Type[i];
                        UnitSFXListbox.Items.Add(sfx);
                    }
                }
            }
            else if (UnitSFXAbilityRB.Checked)
            {
                for (int i = 0; i < unit.unitabilities.Count; i++)
                {
                    unitability able = unit.unitabilities[i];
                    if (able.sound != "")
                    {
                        sfx sfx = entities.sfx.FirstOrDefault(s => s.name == able.sound);
                        if (!(sfx.name is null) && !string.Equals(sfx.name, "null", StringComparison.OrdinalIgnoreCase))
                        {
                            sfx.displayname = able.ToString();
                            UnitSFXListbox.Items.Add(sfx);
                        }
                    }
                    if (able.deactivatesound != "")
                    {
                        sfx sfx = entities.sfx.FirstOrDefault(s => s.name == able.deactivatesound);
                        if (!(sfx.name is null) && !string.Equals(sfx.name, "null", StringComparison.OrdinalIgnoreCase))
                        {
                            sfx.displayname = able.ToString() + " deactivate";
                            UnitSFXListbox.Items.Add(sfx);
                        }
                    }
                }
                for (int i = 0; i < unit.abilities.Count; i++)
                {
                    ability able = unit.abilities[i];
                    if (able.sound != "")
                    {
                        sfx sfx = entities.sfx.FirstOrDefault(s => s.name == able.sound);
                        if (!(sfx.name is null) && !string.Equals(sfx.name, "null", StringComparison.OrdinalIgnoreCase))
                        {
                            sfx.displayname = able.ToString();
                            UnitSFXListbox.Items.Add(sfx);
                        }
                    }
                }
            }
            else if (UnitSFXWeaponRB.Checked)
            {
                for (int i = 0; i < unit.consolidatedhps.Count; i++)
                {
                    hardpoint hp = unit.consolidatedhps[i];
                    if (hp.firesound != "")
                    {
                        sfx sfx = entities.sfx.FirstOrDefault(s => s.name == hp.firesound);
                        if (!(sfx.name is null) && !string.Equals(sfx.name, "null", StringComparison.OrdinalIgnoreCase))
                        {
                            sfx.displayname = convertProjectileToName(hp.projectile) + " Fire";
                            UnitSFXListbox.Items.Add(sfx);
                        }
                    }
                    if (hp.diesound != "")
                    {
                        sfx sfx = entities.sfx.FirstOrDefault(s => s.name == hp.diesound);
                        if (!(sfx.name is null) && !string.Equals(sfx.name, "null", StringComparison.OrdinalIgnoreCase))
                        {
                            sfx.displayname = convertProjectileToName(hp.projectile) + " Death";
                            UnitSFXListbox.Items.Add(sfx);
                        }
                    }
                }
            }

        }

        private void UnitSFXListbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UnitSampleListBox.Items.Clear();
            if (UnitSFXListbox.SelectedItems.Count > 0)
            {
                sfx sfx = (sfx)UnitSFXListbox.SelectedItem;
                if (!(sfx.samples is null))
                {
                    foreach (string sample in sfx.samples) UnitSampleListBox.Items.Add(sample);
                    UnitSampleListBox.SelectedIndex = 0;
                }
                UnitSFXNameLabel.Text = sfx.name;
                if (sfx.minpitch > 0) UnitSFXMinPitchLabel.Text = "Minimum Pitch: " + sfx.minpitch.ToString();
                else UnitSFXMinPitchLabel.Text = "";
                if (sfx.maxpitch > 0) UnitSFXMaxPitchLabel.Text = "Maximum Pitch: " + sfx.maxpitch.ToString();
                else UnitSFXMaxPitchLabel.Text = "";
            }  
        }

        private void UnitSampleListBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void UnitPlaySoundButton_Click(object sender, EventArgs e)
        {
           if(UnitSampleListBox.SelectedItems.Count > 0)
            {
                string path = (string)UnitSampleListBox.SelectedItem;
                byte[] byteArray = readModBytesOrMeg(path.ToLower().Replace("data\\", ""), entities);
                if (byteArray.Length > 0)
                {
                    Stream stream = new MemoryStream(byteArray);
                    System.Media.SoundPlayer player = new System.Media.SoundPlayer(@stream);
                    player.Play(); //todo implement pitch shifts
                }
            }
        }

        private void UnitSFXRB_CheckedChanged(object sender, EventArgs e)
        {
            populateUnitSFXList();
        }

        private void getMoneyStructures()
        {
            if (globals.MoneyStructures.Count == 0)
            {
                foreach (unit structure in entities.structures)
                {//Cost is for the Skirmish_Ground_Mining_Facility
                    if (structure.techlevel <= 5 && structure.affiliations.Count > 0 && structure.cost > 0 && ((structure.limit_planet > 0 && structure.limit_concurrent < 0) || structure.planets.Length > 0))
                    {
                        bool toadd = false;
                        foreach (ability able in structure.abilities)
                        {
                            if (able.type == "Planet_Income_Bonus_Ability" && able.absoluteCredits > 0 || able.percentCredits > 0) //Technically this fails to account for a postive in one and a negative in the other. This would require an analysis of the base income to see if it should be used
                            {
                                toadd = true;
                                break;
                            }
                        }
                        if (toadd) globals.MoneyStructures.Add(structure);
                    }
                }
                foreach (unit structure in entities.spaceStructures)
                {
                    if (structure.techlevel <= 5 && structure.affiliations.Count > 0 && ((structure.limit_planet > 0 && structure.limit_concurrent < 0) || structure.planets.Length > 0))
                    {
                        bool toadd = false;
                        foreach (ability able in structure.abilities)
                        {
                            if (able.type == "Planet_Income_Bonus_Ability" && able.absoluteCredits > 0 || able.percentCredits > 0)
                            {
                                toadd = true;
                                break;
                            }
                        }
                        if (toadd) globals.MoneyStructures.Add(structure);
                    }
                }
            }

        }

        private int getPotentialIncome(planet planet)
        {
            int add = 0;
            float mult = 1;
            //Init relevant trimmed list to perform better when calling for hundreds of planets on a sort
            getMoneyStructures();
            foreach (unit structure in globals.MoneyStructures)
            {
                string visible = structure.unitname;
                string[] vis = structure.planets;
                if (structure.planets.Length == 0 || structure.planets.Contains(planet.codename))
                {
                    foreach (ability able in structure.abilities)
                    {
                        if (able.type == "Planet_Income_Bonus_Ability")
                        {
                            int qty = 1;
                            if (structure.limit_planet > 1) qty = structure.limit_planet;
                            add += (int)able.absoluteCredits * qty;
                            mult += able.percentCredits * qty;
                        }
                    }
                }
            }
            return (int)(planet.credits * mult + add);
        }

        private void GoToSpawnSetButton_Click(object sender, EventArgs e)
        {
            insert_history((int)historymaintabs.lookups, (int)lookupsubtabs.lkSpawn, GoToSpawnSetButton.Text, true);
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
                PlanetHistoryTextBox.Text = "Population: " + Find_Text_Entry(planet.desc_pop, entities).Replace("\\n", "\n") + "\n\n" + "Fauna: " + Find_Text_Entry(planet.desc_fauna, entities) + "\n\n" + Find_Text_Entry(planet.desc_history, entities);
                PlanetCreditLabel.Text = "Income: " + planet.credits.ToString();
                PlanetShipyardLabel.Text = "Shipyard: " + planet.shipyard.ToString();
                PlanetStarbaseLabel.Text = "Starbase: " + planet.max_starbase.ToString();
                PlanetGroundSlotsLabel.Text = "Ground Slots: " + planet.land_structures.ToString();
                PlanetPopulationLabel.Text = "Population: " + planet.pop.ToString();
                PlanetHubLabel.Visible = planet.tradehub;

                int potential = getPotentialIncome(planet);
                if (globals.PlanetFilterConfig.GCs.Count == 1)
                {
                    galacticConquest GC = globals.PlanetFilterConfig.GCs[0];
                    int connections = 0;
                    List<string> connected = new List<string>();
                    foreach (tradeRoute route in GC.traderouteObjects)
                    {
                        if (route.planets[0] == planet.codename)
                        {
                            planet linked = GC.planetObjects.FirstOrDefault(s => s.codename == route.planets[1]);
                            if (!(linked.username is null)) connected.Add(linked.username);
                            connections++;
                        }
                        if (route.planets[1] == planet.codename)
                        {
                            planet linked = GC.planetObjects.FirstOrDefault(s => s.codename == route.planets[0]);
                            if (!(linked.username is null)) connected.Add(linked.username);
                            connections++;
                        }
                    }
                    PlanetConnectionsLabel.Text = "Connections: " + connections;
                    toolTip1.SetToolTip(PlanetConnectionsLabel, "Connected to:\n" + SerializeStringArray(connected));
                    int tradehub = 1;
                    if (planet.tradehub) tradehub = globals.tradehubmultiplier;
                    if (entities.modid == "") tradehub = 0;
                    potential += connections * globals.tradebase * tradehub;
                }
                else PlanetConnectionsLabel.Text = "";
                PlanetPotentialabel.Text = "Potential Income: " + potential.ToString();

                if (globals.PlanetSortConfig.SortType == PlanetSortTypes.Name) PlanetSortLabel.Text = ""; //Pretty redundant to show in this case
                else if (planet.sortstring != "") PlanetSortLabel.Text = "Sort: " + planet.sortstring;
                else PlanetSortLabel.Text = "Sort: " + planet.sortint.ToString();

                spawnSet set = entities.spawnSets.FirstOrDefault(s => s.planets.Contains(planet.codename.ToUpper()));
                if (set.planets is null)
                {
                    PlanetSpawnSetLabel.Visible = false;
                    GoToSpawnSetButton.Visible = false;
                }
                else
                {
                    PlanetSpawnSetLabel.Visible = true;
                    GoToSpawnSetButton.Visible = true;
                    GoToSpawnSetButton.Text = set.name;
                }

                SpaceMapLabel.Text = "Space Map: " + planet.spaceMap;
                if (planet.has_ground)
                {
                    GroundMapLabel.Text = "Ground Map: " + planet.groundMap;
                    TerrainTypeLabel.Text = "Terrain Type: " + getTerrainType(planet.groundMap, entities);
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
                    if (unit.planets.Contains(planet.codename)) PlanetStructureListBox.Items.Add(unit);
                }
                foreach (unit unit in entities.spaceStructures)
                {
                    if (!unit.unitname.Contains("_Shipyard"))
                    {
                        if (unit.planets.Contains(planet.codename)) PlanetStructureListBox.Items.Add(unit);
                    }
                }

                PlanetGCListBox.Items.Clear();
                foreach (galacticConquest GC in entities.Conquests)
                {
                    if (GC.planets.Contains(planet.codename)) PlanetGCListBox.Items.Add(GC);
                }

                PlanetBTSTextBox.Text = "";
                string BTS = "";
                string path = getModFile("Text\\BTSPlanet.txt", entities);

                if (path != "") BTS = readBTS(path, planet.codename);
                if (BTS != "") PlanetBTSTextBox.Text = "Behind the scenes\n\n" + BTS + "\n";

                BTS = "";
                path = getModFile("Text\\BTSMap.txt", entities);

                if (path != "") BTS = readBTS(path, planet.groundMap);
                if (BTS != "") PlanetBTSTextBox.Text += "Ground map information\n\n" + BTS + "\n";

                BTS = "";
                if (path != "") BTS = readBTS(path, planet.spaceMap);
                if (BTS != "") PlanetBTSTextBox.Text += "Space map information\n\n" + BTS + "\n";
            }
        }

        private void PlanetPictureBox_Click(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            float x = (me.Location.X - globals.origin) / globals.scale;
            float y = (me.Location.Y - globals.origin) / globals.scale;

            float close = float.PositiveInfinity;
            int closest = -1;

            for (int i = 0; i < PlanetListBox.Items.Count; i++)
            {
                planet planet = (planet)PlanetListBox.Items[i];
                float prox = (planet.x_coord - x) * (planet.x_coord - x) + (planet.y_coord - y) * (planet.y_coord - y);
                if (prox < close)
                {
                    close = prox;
                    closest = i;
                }
            }

            if (closest >= 0)
            {
                PlanetListBox.SelectedIndex = closest;
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

        private bool filterPlanet(planet planet)
        {
            if (globals.PlanetFilterConfig.GCs.Count > 0)
            {
                bool match = !globals.PlanetFilterConfig.UnionIntersection;
                foreach(galacticConquest GC in globals.PlanetFilterConfig.GCs)
                {
                    if (GC.planets.Contains(planet.codename))
                    {
                        if (globals.PlanetFilterConfig.UnionIntersection)
                        {
                            match = true;
                            break;
                        }
                    }
                    else
                    {
                        if (!globals.PlanetFilterConfig.UnionIntersection)
                        {
                            match = false;
                            break;
                        }
                    }
                }
                if (!match) return false;
            }

            int level = globals.PlanetFilterConfig.shipyardLevel;
            if (level < 0) level = 0;
            if (globals.PlanetFilterConfig.shipyardComparison == 0)
            {
                if (planet.shipyard < level) return false;
            }
            else if (globals.PlanetFilterConfig.shipyardComparison == 1)
            {
                if (planet.shipyard != level) return false;
            }
            else if (globals.PlanetFilterConfig.shipyardComparison == 2)
            {
                if (planet.shipyard > level) return false;
            }

            level = globals.PlanetFilterConfig.starbaseLevel;
            if (level < 1) level = 1;
            if (globals.PlanetFilterConfig.starbaseComparison == 0)
            {
                if (planet.max_starbase < level) return false;
            }
            else if (globals.PlanetFilterConfig.starbaseComparison == 1)
            {
                if (planet.max_starbase != level) return false;
            }
            else if (globals.PlanetFilterConfig.starbaseComparison == 2)
            {
                if (planet.max_starbase > level) return false;
            }

            level = globals.PlanetFilterConfig.slotsLevel;
            if (level < 0) level = 0;
            if (globals.PlanetFilterConfig.slotsComparison == 0)
            {
                if (planet.land_structures < level) return false;
            }
            else if (globals.PlanetFilterConfig.slotsComparison == 1)
            {
                if (planet.land_structures != level) return false;
            }
            else if (globals.PlanetFilterConfig.slotsComparison == 2)
            {
                if (planet.land_structures > level) return false;
            }

            level = globals.PlanetFilterConfig.incomeLevel;
            if (level < 0) level = 0;
            if (globals.PlanetFilterConfig.incomeComparison == 0)
            {
                if (planet.credits < level) return false;
            }
            else if (globals.PlanetFilterConfig.incomeComparison == 1)
            {
                if (planet.credits != level) return false;
            }
            else if (globals.PlanetFilterConfig.incomeComparison == 2)
            {
                if (planet.credits > level) return false;
            }

            level = globals.PlanetFilterConfig.potentialLevel;
            if (level < 0) level = 0;
            int potential = getPotentialIncome(planet);
            if (globals.PlanetFilterConfig.potentialComparison == 0)
            {
                if (potential < level) return false;
            }
            else if (globals.PlanetFilterConfig.potentialComparison == 1)
            {
                if (potential != level) return false;
            }
            else if (globals.PlanetFilterConfig.potentialComparison == 2)
            {
                if (potential > level) return false;
            }

            if (globals.PlanetFilterConfig.hubMode > 0)
            {
                if (planet.tradehub)
                {
                    if (globals.PlanetFilterConfig.hubMode == 2) return false;
                }
                else
                {
                    if (globals.PlanetFilterConfig.hubMode == 1) return false;
                }
            }

            if (globals.PlanetFilterConfig.spawnMode > 0)
            {
                spawnSet set = entities.spawnSets.FirstOrDefault(s => s.planets.Contains(planet.codename.ToUpper()));
                if (set.planets is null)
                {
                    if (globals.PlanetFilterConfig.spawnMode == 2) return false;
                }
                else
                {
                    if (globals.PlanetFilterConfig.spawnMode == 1) return false;
                }
            }

            if (globals.PlanetFilterConfig.unitFilter != unitFilter.any)
            {
                int space = entities.spaceUnits.FindIndex(s => s.planets.Contains(planet.codename));
                if (globals.PlanetFilterConfig.unitFilter == unitFilter.space && space < 0) return false;
                int ground = entities.groundCompanies.FindIndex(s => s.planets.Contains(planet.codename));
                if (globals.PlanetFilterConfig.unitFilter == unitFilter.ground && ground < 0) return false;
                if (globals.PlanetFilterConfig.unitFilter == unitFilter.has && (space < 0 && ground < 0)) return false;
                if (globals.PlanetFilterConfig.unitFilter == unitFilter.none && (space >= 0 || ground >= 0)) return false;
            }

            if (globals.PlanetFilterConfig.buildingFilter != buildingFilter.any)
            {
                int space = entities.spaceStructures.FindIndex(s => s.planets.Contains(planet.codename) && !s.unitname.Contains("_Shipyard")); //everything has a shipyard
                int ground = entities.structures.FindIndex(s => s.planets.Contains(planet.codename));
                if (space < 0 && ground < 0)
                {
                    if (globals.PlanetFilterConfig.buildingFilter == buildingFilter.none) return true;
                    return false;
                }
                else
                {
                    if (globals.PlanetFilterConfig.buildingFilter == buildingFilter.none) return false;
                    if (globals.PlanetFilterConfig.buildingFilter == buildingFilter.has) return true;
                    bool match = false;
                    if (globals.PlanetFilterConfig.buildingFilter == buildingFilter.nonfinancial)
                    {
                        //Can't use the shortcuts because a planet could have a mine/corp and a nonfinancial buidling
                        foreach(unit structure in entities.structures)
                        {
                            if (structure.planets.Contains(planet.codename))
                            {
                                bool unitmatch = true;
                                foreach (ability able in structure.abilities)
                                {
                                    if (able.type == "Reduce_Production_Price_Ability" || able.type == "Planet_Income_Bonus_Ability")
                                    {
                                        unitmatch = false;
                                        break;
                                    }
                                }
                                if (unitmatch)
                                {
                                    match = true;
                                    break;
                                }
                            }
                        }
                        if (!match)
                        {
                            foreach (unit structure in entities.spaceStructures)
                            {
                                if (structure.planets.Contains(planet.codename) && !structure.unitname.Contains("_Shipyard"))
                                {
                                    bool unitmatch = true;
                                    foreach (ability able in structure.abilities)
                                    {
                                        if (able.type == "Reduce_Production_Price_Ability" || able.type == "Planet_Income_Bonus_Ability")
                                        {
                                            unitmatch = false;
                                            break;
                                        }
                                    }
                                    if (unitmatch)
                                    {
                                        match = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (!match) return false;
                    }
                    if (globals.PlanetFilterConfig.buildingFilter != buildingFilter.income)
                    {
                        getDiscountObjects();
                        
                        foreach(unit corp in globals.DiscountEntities)
                        {
                            if (corp.planets.Contains(planet.codename))
                            {
                                match = true;
                                break;
                            }
                        }
                        if (match) return true;
                        else
                        {
                            if (globals.PlanetFilterConfig.buildingFilter == buildingFilter.discount) return false;
                        }
                    }
                    if (globals.PlanetFilterConfig.buildingFilter != buildingFilter.discount)
                    {
                        getMoneyStructures();

                        foreach (unit mine in globals.MoneyStructures)
                        {
                            if (mine.planets.Contains(planet.codename))
                            {
                                match = true;
                                break;
                            }
                        }
                        if (match) return true;
                        else return false;
                    }
                }
            }

            //Do this last because the performance is terrible
            if (globals.PlanetFilterConfig.terrains.Count > 0)
            {
                if (planet.has_ground)
                {
                    int terraintype = planet.terrain_id;
                    if (terraintype < 0)
                    {//Save changes on the fly so the user who isn't trying to use every planet terrain can skip the long load
                        terraintype = getTerrainIndex(planet.groundMap, entities);
                        int planetindex = entities.Planets.FindIndex(s => s.codename == planet.codename);
                        planet update = entities.Planets[planetindex];
                        update.terrain_id = terraintype;
                        entities.Planets[planetindex] = update;
                    }
                    if (!globals.PlanetFilterConfig.terrains.Contains(terraintype)) return false;
                }
                else return false;
            }

            return true;
        }

        private planet sortPlanet(planet planet)
        {
            planet.sortstring = ""; //Used for detecting if the int should be displayed
            switch (globals.PlanetSortConfig.SortType)
            {
                case PlanetSortTypes.Name:
                    planet.sortstring = planet.username;
                    break;
                case PlanetSortTypes.Income:
                    planet.sortint = planet.credits;
                    break;
                case PlanetSortTypes.shipyard:
                    planet.sortint = planet.shipyard;
                    break;
                case PlanetSortTypes.Starbase:
                    planet.sortint = planet.max_starbase;
                    break;
                case PlanetSortTypes.Slots:
                    planet.sortint = planet.land_structures;
                    break;
                case PlanetSortTypes.Pop:
                    planet.sortint = planet.pop;
                    break;
                case PlanetSortTypes.Potential:
                    planet.sortint = getPotentialIncome(planet);
                    break;
                case PlanetSortTypes.Terrain:

                    if (planet.has_ground)
                    {
                        int terraintype = planet.terrain_id;
                        if (terraintype < 0)
                        {//Save changes on the fly so the user who isn't trying to use every planet terrain can skip the long load
                            terraintype = getTerrainIndex(planet.groundMap, entities);
                            int planetindex = entities.Planets.FindIndex(s => s.codename == planet.codename);
                            planet update = entities.Planets[planetindex];
                            update.terrain_id = terraintype;
                            entities.Planets[planetindex] = update;
                        }
                        planet.sortstring = getTerrainName(terraintype);
                    }
                    else planet.sortstring = "Space Only";
                    break;
                case PlanetSortTypes.Production:
                    int mode = globals.PlanetSortConfig.productionMode;
                    planet.sortint = 0;
                    if (mode == 0 || mode == 1) planet.sortint += entities.structures.Count(x => x.planets.Contains(planet.codename)) + entities.spaceStructures.Count(x => x.planets.Contains(planet.codename) && !x.unitname.Contains("_Shipyard"));
                    if (mode == 0 || mode == 2 || mode == 3) planet.sortint += entities.spaceUnits.Count(x => x.planets.Contains(planet.codename));
                    if (mode == 0 || mode == 2 || mode == 4) planet.sortint += entities.groundCompanies.Count(x => x.planets.Contains(planet.codename));
                    break;
                case PlanetSortTypes.Usage:
                    int mood = globals.PlanetSortConfig.usageMode;
                    planet.sortint = 0;
                    if (mood == 1) planet.sortint = globals.PlanetFilterConfig.GCs.Count(x => x.planets.Contains(planet.codename));
                    if (mood == 0 || mood == 2 || mood == 6) planet.sortint += entities.Conquests.Count(x => x.planets.Contains(planet.codename) && x.Type == GCType.Progressive);
                    if (mood == 0 || mood == 3 || mood == 6) planet.sortint += entities.Conquests.Count(x => x.planets.Contains(planet.codename) && x.Type == GCType.Regional);
                    if (mood == 0 || mood == 4 || mood == 6) planet.sortint += entities.Conquests.Count(x => x.planets.Contains(planet.codename) && x.Type == GCType.Historical);
                    if (mood == 0 || mood == 5 || mood == 6) planet.sortint += entities.Conquests.Count(x => x.planets.Contains(planet.codename) && x.Type == GCType.Infinity);
                    if (mood == 6) planet.sortint += entities.Conquests.Count(x => x.planets.Contains(planet.codename) && x.Type == GCType.InfinityLayoutCopy);
                    break;
                case PlanetSortTypes.SpaceMap:
                    planet.sortstring = planet.spaceMap;
                    break;
                case PlanetSortTypes.GroundMap:
                    planet.sortstring = planet.groundMap;
                    break;
                case PlanetSortTypes.Internal:
                    planet.sortstring = planet.codename;
                    break;
                case PlanetSortTypes.X:
                    planet.sortint = (int)planet.x_coord;
                    break;
                case PlanetSortTypes.Y:
                    planet.sortint = (int)planet.y_coord;
                    break;
                case PlanetSortTypes.R:
                    planet.sortint = (int)Math.Sqrt((planet.x_coord * planet.x_coord + planet.y_coord * planet.y_coord));
                    break;
            }

            return planet;
        }

        private void populatePlanetListbox()
        {
            string save = "";
            if (PlanetListBox.SelectedItems.Count > 0) save = ((planet)PlanetListBox.SelectedItem).codename;
            Bitmap Starfield = new Bitmap(PlanetPictureBox.Width, PlanetPictureBox.Height);
            Graphics g = Graphics.FromImage(Starfield);
            g.FillRectangle(new SolidBrush(Color.Black), 0, 0, PlanetPictureBox.Width, PlanetPictureBox.Height);

            List<quantizedObject> spaceMaps = new List<quantizedObject>();
            List<quantizedObject> groundMaps = new List<quantizedObject>();
            List<planet> sorted = new List<planet>();
            PlanetListBox.Items.Clear();
            for (int i = 0; i < entities.Planets.Count; i++) //Can't use for each because terrain type is saved as it is calculated
            {
                planet planet = entities.Planets[i];
                if (planet.codename != "Galaxy_Core_Art_Model" && planet.username.ToLower().Contains(PlanetSearchBox.Text.ToLower()) && filterPlanet(planet))
                {
                    sorted.Add(sortPlanet(planet));
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
            if (globals.PlanetSortConfig.SortType <= PlanetSortTypes.Name)
            {
                if (globals.PlanetSortConfig.Descending)
                {
                    sorted.Sort(delegate (planet b, planet a)
                    {
                        int xdiff = a.sortstring.CompareTo(b.sortstring);
                        if (xdiff != 0) return xdiff;
                        else return a.username.CompareTo(b.username);
                    });
                }
                else
                {
                    sorted.Sort(delegate (planet a, planet b)
                    {
                        int xdiff = a.sortstring.CompareTo(b.sortstring);
                        if (xdiff != 0) return xdiff;
                        else return a.username.CompareTo(b.username);
                    });
                }
            }
            else
            {
                if (globals.PlanetSortConfig.Descending)
                {
                    sorted.Sort(delegate (planet b, planet a)
                    {
                        int xdiff = a.sortint.CompareTo(b.sortint);
                        if (xdiff != 0) return xdiff;
                        else return a.username.CompareTo(b.username);
                    });
                }
                else
                {
                    sorted.Sort(delegate (planet a, planet b)
                    {
                        int xdiff = a.sortint.CompareTo(b.sortint);
                        if (xdiff != 0) return xdiff;
                        else return a.username.CompareTo(b.username);
                    });
                }
            }

            foreach (planet planet in sorted)
            {
                PlanetListBox.Items.Add(sortPlanet(planet));
                if (planet.codename == save) PlanetListBox.SelectedItem = planet;
            }

            PlanetPictureBox.Tag = Starfield;
            PlanetPictureBox.Image = Starfield;

            spaceMaps.Sort((s2, s1) => s1.quantity.CompareTo(s2.quantity));
            groundMaps.Sort((s2, s1) => s1.quantity.CompareTo(s2.quantity));
            PlanetSpaceMapRB.Tag = spaceMaps;
            PlanetGroundMapRB.Tag = groundMaps;
            populateMapListbox();

            PlanetMatchesLabel.Text = "Matches: " + PlanetListBox.Items.Count;
        }

        private void PlanetSearchBox_TextChanged(object sender, EventArgs e)
        {
            populatePlanetListbox();
        }

        private void populateMapListbox()
        {
            MapsInPlanetsListbox.Items.Clear();
            List<quantizedObject> maps = (List<quantizedObject>)PlanetSpaceMapRB.Tag;
            if(PlanetGroundMapRB.Checked) maps = (List<quantizedObject>)PlanetGroundMapRB.Tag;
            foreach (quantizedObject q in maps)
            {
                if(q.username.Contains(MapSearchBox.Text.ToUpper())) MapsInPlanetsListbox.Items.Add(q);
            }
        }

        private void MapSearchBox_TextChanged(object sender, EventArgs e)
        {
            populateMapListbox();
        }

        private void PlanetSpaceMapRB_CheckedChanged(object sender, EventArgs e)
        {
            populateMapListbox();
        }

        private void PlanetGroundMapRB_CheckedChanged(object sender, EventArgs e)
        {
            populateMapListbox();
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

        private void PlanetGoToGCButton_Click(object sender, EventArgs e)
        {
            if (PlanetGCListBox.SelectedItems.Count > 0)
            {
                insert_history((int)historymaintabs.conquest, 0, ((galacticConquest)PlanetGCListBox.SelectedItem).codename, true);
            }
        }

        private void PlanetStructuresGoToButton_Click(object sender, EventArgs e)
        {
            if (PlanetStructureListBox.SelectedItems.Count > 0)
            {
                string structure = ((unit)PlanetStructureListBox.SelectedItem).unitname;
                if (entities.spaceStructures.FindIndex(s => s.unitname == structure) >= 0) insert_history((int)historymaintabs.unit, 8, structure, true);
                else insert_history((int)historymaintabs.unit, 7, structure, true);
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


        private void PlanetSortButton_Click(object sender, EventArgs e)
        {
            PlanetSort sort = new PlanetSort();
            sort.sortConfig = globals.PlanetSortConfig;
            //todo add the others
            sort.ShowDialog();

            if (!sort.cancel)
            {
                globals.PlanetSortConfig = sort.sortConfig;
                PlanetSortTypeLabel.Text = sort.sortDocumentation;

                populatePlanetListbox();
            }
        }

        private void PlanetFilterButton_Click(object sender, EventArgs e)
        {
            PlanetFilter filter = new PlanetFilter();
            filter.filterConfig = globals.PlanetFilterConfig;
            filter.GCFull = entities.Conquests;

            filter.ShowDialog();

            if (!filter.cancel)
            {
                globals.PlanetFilterConfig = filter.filterConfig;
                PlanetFilterTypeLabel.Text = filter.filterDocumentation;

                populatePlanetListbox();
            }
        }

        private void PlanetExportButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog fil = new SaveFileDialog();
            fil.Filter = ("Text Files (*.txt)|*.txt|All files (*.*)|*.*");
            fil.Title = "Export Planet list";
            if (fil.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter filewrite = new StreamWriter(fil.FileName))
                {
                    foreach (planet planet in PlanetListBox.Items) filewrite.WriteLine(planet);
                }
                MessageBox.Show("Planet list saved to file");
            }
        }

        private void PlanetMissingTextButton_Click(object sender, EventArgs e)
        {
            string corenne = "";
            foreach(planet planet in PlanetListBox.Items)
            {
                if (planet.username.Contains("TEXT_OBJECT_STAR_SYSTEM_")) corenne += planet.username + ",\n";
                if (entities.Text.FindIndex(s => s.identifier == planet.desc_fauna) < 0) corenne += planet.desc_fauna + ",\n";
                if (entities.Text.FindIndex(s => s.identifier == planet.desc_history) < 0) corenne += planet.desc_history + ",\n";
                if (entities.Text.FindIndex(s => s.identifier == planet.desc_pop) < 0) corenne += planet.desc_pop + ",\n";
            }
            if(corenne == "") MessageBox.Show("No missing text detected");
            else
            {
                TextDetail deets = new TextDetail();
                deets.detail = corenne;
                deets.Show();
            }
        }

        private void populateGCListbox()
        {
            GCListBox.Items.Clear();
            foreach (galacticConquest conquest in entities.Conquests)
            {
                bool add = false;
                if (conquest.Type == GCType.Progressive && ProgressiveCheckBox.Checked) add = true;
                if (conquest.Type == GCType.Regional && RegionalCheckBox.Checked) add = true;
                if (conquest.Type == GCType.Historical && HistoricalCheckBox.Checked) add = true;
                if ((conquest.Type == GCType.Infinity || conquest.Type == GCType.InfinityLayoutCopy) && InfinityCheckBox.Checked) add = true;
                if(add) GCListBox.Items.Add(conquest);
            }
        }

        private string getDialogPath(string filename)
        {
            return getModFile("Scripts\\Story\\" + filename + ".txt", entities);
        }

        public struct speechevent
        {
            public string title;
            public string speech;
            public override string ToString()
            {
                return title;
            }
        }

        private void GCListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(GCListBox.SelectedItems.Count > 0)
            {
                galacticConquest Campaign = (galacticConquest)GCListBox.SelectedItem;
                insert_history((int)historymaintabs.conquest, 0, Campaign.codename);
                GCNameLabel.Text = Campaign.username;
                GCCampaignSetLabel.Text = "Campaign Set: " + Campaign.campaign_set;
                GCInternalNameLabel.Text = "Internal Name: " + Campaign.codename;

                GCListBox.Tag = Campaign;

                GCActiveListBox.Items.Clear();
                foreach (string faction in Campaign.factionsPlayable)
                {
                    if (faction == "CCoGM")
                    {
                        if (!Campaign.factionsPlayable.Contains("Empire")) GCActiveListBox.Items.Add(FactionFromCodeName("Empire", entities));
                    }
                    else
                    {
                        if (faction is null || faction == "") GCActiveListBox.Items.Add("");
                        else GCActiveListBox.Items.Add(FactionFromCodeName(faction, entities));
                    }
                }
                GCActiveListBox.SelectedIndex = 0;

                GCPresentListbox.Items.Clear();
                foreach (string faction in Campaign.factionsPresent)
                {
                    GCPresentListbox.Items.Add(FactionFromCodeName(faction, entities));
                }

                GCPlanetLabel.Text = "Planets: " + Campaign.planetObjects.Count;

                ConquestBTSTextBox.Text = "";
                string BTS = "";
                string path = getModFile("Text\\BTSConquest.txt", entities);
                string id = Campaign.campaign_set.Replace("_CCoGM", "");
                if (id.Contains("_Era_")) id = id.Remove(id.IndexOf("_Era_"));

                if (path != "") BTS = readBTS(path, id);
                if (BTS != "") ConquestBTSTextBox.Text = "Behind the scenes\n\n" + BTS;

                List<string> dialogs = new List<string>();
                List<string> speeches = new List<string>();
                List<speechevent> speechevents = new List<speechevent>();
                foreach (string plotfile in Campaign.StoryPlots)
                {
                    XmlDocument storyplot = readModXmlOrMeg("XML\\" + plotfile, entities);
                    XmlNodeList plots = storyplot.SelectNodes("descendant::Active_Plot");
                    foreach (XmlNode plot in plots)
                    {
                        if (!(plot.InnerText is null) && plot.InnerText != "Conquests\\Player_Agnostic_Plot.xml" && plot.InnerText != "Conquests\\Documentation.xml") //Skip the EaWX universal plots
                        {
                            XmlDocument doc = readModXmlOrMeg("XML\\" + plot.InnerText, entities);
                            XmlNodeList events = doc.SelectNodes("descendant::Event");
                            foreach(XmlNode even in events)
                            {
                                XmlNode dialog = even.SelectSingleNode("descendant::Story_Dialog");
                                if(!(dialog is null)) 
                                {
                                    string filepath = dialog.InnerText.Trim();
                                    if (filepath != "" && !dialogs.Contains(filepath) && getDialogPath(filepath) != "") dialogs.Add(filepath);
                                }
                                XmlNode reward = even.SelectSingleNode("descendant::Reward_Type");
                                if (!(reward is null))
                                {
                                    string rewardtype = reward.InnerText.Trim();
                                    if(rewardtype == "MULTIMEDIA" || rewardtype == "SCREEN_TEXT")
                                    {
                                        string title = even.Attributes[0].Value;
                                        if (!speeches.Contains(title) && !title.Contains("Template_") && title != "About1" && title != "About2" && title !="About3")
                                        {
                                            XmlNode param = even.SelectSingleNode("descendant::Reward_Param1");
                                            if (!(param is null))
                                            {
                                                string text = param.InnerText.Trim();
                                                speechevent entry = new speechevent
                                                {
                                                    title = title.Replace("_", " "),
                                                    speech = Find_Text_Entry(text, entities),
                                                };
                                                speechevents.Add(entry);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                GCDialogListBox.Items.Clear();
                foreach (string dialog in dialogs) GCDialogListBox.Items.Add(dialog);

                speechevents.Sort((s1, s2) => s1.title.CompareTo(s2.title));
                GCSpeechListBox.Items.Clear();
                foreach (speechevent speech in speechevents) GCSpeechListBox.Items.Add(speech);
            }
            else
            {
                GCChapterListBox.SelectedItems.Clear();
                GCStoryTextBox.Text = "";
            }
        }

        private void ProgressiveCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            populateGCListbox();
        }

        private void RegionalCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            populateGCListbox();
        }

        private void HistoricalCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            populateGCListbox();
        }

        private void InfinityCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            populateGCListbox();
        }

        private void drawGC()
        {
            GCPresentListbox.SelectedItems.Clear();
            GCPlanetListBox.SelectedItems.Clear();
            int factionindex = GCActiveListBox.SelectedIndex;
            if (factionindex >= 0)
            {
                List<planet> affiled = new List<planet>();
                int total_income = 0; //Todo consider mines
                galacticConquest selected = (galacticConquest)GCListBox.Tag;
                for (int i = 0; i < selected.planetObjects.Count; i++)
                {
                    planet planet = selected.planetObjects[i];
                    total_income += planet.credits;
                    bool notfound = true;
                    for (int j = 0; j < selected.forceLocation[factionindex].Count; j++)
                    {
                        if (selected.forceLocation[factionindex][j] == planet.codename)
                        {
                            planet.owner = FactionFromCodeName(selected.forceOwner[factionindex][j], entities);
                            if (planet.owner.codename == "" || planet.owner.codename is null) planet.owner = FactionFromCodeName("Neutral", entities);
                            notfound = false;
                            break;
                        }
                    }
                    if (notfound) planet.owner = FactionFromCodeName("Neutral", entities);
                    affiled.Add(planet);
                }
                GCPlanetLabel.Text = "Planets: " + GCPlanetListBox.Items.Count;
                GCActiveListBox.Tag = affiled;
                GCPresentIncomeLabel.Tag = total_income;

                Bitmap Starfield = new Bitmap(GCPictureBox.Width, GCPictureBox.Height);
                Graphics g = Graphics.FromImage(Starfield);
                g.FillRectangle(new SolidBrush(Color.Black), 0, 0, GCPictureBox.Width, GCPictureBox.Height);

                List<string> bads = new List<string>();

                if (GCTradeRouterCheckBox.Checked)
                {
                    foreach (tradeRoute route in selected.traderouteObjects)
                    {
                        planet A = affiled.FirstOrDefault(s => s.codename == route.planets[0]);
                        planet B = affiled.FirstOrDefault(s => s.codename == route.planets[1]);

                        if (!(A.codename is null || B.codename is null)) //TODO handle one being nil and drawing a route to 0, 0 better
                        {
                            if (A.owner.codename == B.owner.codename)
                            {
                                Pen drawPen = new Pen(Color.FromArgb(255, A.owner.color[0], A.owner.color[1], A.owner.color[2]), 1);
                                g.DrawLine(drawPen, A.x_coord * globals.scale + globals.origin, A.y_coord * globals.scale + globals.origin, B.x_coord * globals.scale + globals.origin, B.y_coord * globals.scale + globals.origin);
                            }
                            else
                            {
                                Pen drawPen = new Pen(Color.Gray, 1);
                                g.DrawLine(drawPen, A.x_coord * globals.scale + globals.origin, A.y_coord * globals.scale + globals.origin, B.x_coord * globals.scale + globals.origin, B.y_coord * globals.scale + globals.origin);
                            }
                        }
                        else
                        {
                            bads.Add(route.name);
                        }
                    }
                }

                if(globals.devmode && bads.Count > 0) MessageBox.Show("Routes referencing nonexistent planets:\n" + SerializeStringArray(bads));

                foreach (planet planet in affiled)
                {
                    int x = (Int32)(planet.x_coord * globals.scale + globals.origin) - 2;
                    int y = (Int32)(planet.y_coord * globals.scale + globals.origin) - 2;
                    Color brush = Color.FromArgb(255, planet.owner.color[0], planet.owner.color[1], planet.owner.color[2]);
                    g.FillEllipse(new SolidBrush(brush), x, y, 5, 5);
                }

                GCPictureBox.Tag = Starfield;

                populateGCPlanetListBox();
            }
        }
        private void GCActiveListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            drawGC();
        }

        private void GCTradeRouterCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            drawGC();
        }

        private void GCPictureBox_Click(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            float x = (me.Location.X - globals.origin) / globals.scale;
            float y = (me.Location.Y - globals.origin) / globals.scale;

            float close = float.PositiveInfinity;
            int closest = -1;

            for(int i = 0; i< GCPlanetListBox.Items.Count; i++)
            {
                planet planet = (planet)GCPlanetListBox.Items[i];
                float prox = (planet.x_coord - x) * (planet.x_coord - x) + (planet.y_coord - y) * (planet.y_coord - y);
                if (prox < close)
                {
                    close = prox;
                    closest = i;
                }
            }

            if(closest >= 0)
            {
                GCPlanetListBox.SelectedIndex = closest;
            }
        }

        private void populateGCPlanetListBox()
        {
            GCPlanetListBox.Items.Clear();
            foreach (planet planet in (List<planet>)GCActiveListBox.Tag)
            {
                if(GCPresentListbox.SelectedItems.Count == 0) GCPlanetListBox.Items.Add(planet);
                else
                {
                    faction active = (faction)GCPresentListbox.SelectedItem;
                    if(active.codename == planet.owner.codename) GCPlanetListBox.Items.Add(planet);
                }
            }
            GCPictureBox.Image = (Bitmap)GCPictureBox.Tag;
        }



        private void GCPresentListbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(GCPresentListbox.SelectedItems.Count > 0)
            {
                galacticConquest GC = (galacticConquest)GCListBox.SelectedItem;

                int planetcount = 0;
                int borderplanets = 0;
                int income = 0;
                int level_4 = 0;
                int level_3 = 0;
                int level_2 = 0;
                int level_1 = 0;
                List<string> borderingplanets = new List<string>();
                List<string> borderingfactions = new List<string>();

                faction active = (faction)GCPresentListbox.SelectedItem;
                List<planet> ownedPlanets = (List<planet>)GCActiveListBox.Tag;
                foreach (planet planet in ownedPlanets)
                {
                    if (active.codename == planet.owner.codename)
                    {
                        planetcount++;
                        income += planet.credits;
                        if (planet.shipyard == 4) level_4++;
                        if (planet.shipyard == 3) level_3++;
                        if (planet.shipyard == 2) level_2++;
                        if (planet.shipyard == 1) level_1++;

                        bool bordered = false;
                        List<tradeRoute> links = GC.traderouteObjects.FindAll(s => s.planets[0] == planet.codename);
                        foreach(tradeRoute route in links)
                        {
                            planet connected = ownedPlanets.FirstOrDefault(s => s.codename == route.planets[1]);
                            if(!(connected.codename is null))
                            {
                                string owner = connected.owner.codename;
                                if(owner != planet.owner.codename && !(connected.owner.ai == "None" || (connected.owner.ai == "" && !connected.owner.playable)))
                                {
                                    bordered = true;
                                    if(!borderingplanets.Contains(connected.username)) borderingplanets.Add(connected.username);
                                    if (!borderingfactions.Contains(connected.owner.textname)) borderingfactions.Add(connected.owner.textname);
                                }
                            }
                        }
                        links = GC.traderouteObjects.FindAll(s => s.planets[1] == planet.codename);
                        foreach (tradeRoute route in links)
                        {
                            planet connected = ownedPlanets.FirstOrDefault(s => s.codename == route.planets[0]);
                            if (!(connected.codename is null))
                            {
                                string owner = connected.owner.codename;
                                if (owner != planet.owner.codename && !(connected.owner.ai == "None" || (connected.owner.ai == "" && !connected.owner.playable)))
                                {
                                    bordered = true;
                                    if (!borderingplanets.Contains(connected.username)) borderingplanets.Add(connected.username);
                                    if (!borderingfactions.Contains(connected.owner.textname)) borderingfactions.Add(connected.owner.textname);
                                }
                            }
                        }
                        if(bordered) borderplanets++;
                    }
                    borderingplanets.Sort();
                }

                GCPresentPlanetsLabel.Text = "Planets: " + planetcount + " (" + (100 * planetcount / GC.planetObjects.Count).ToString("0") + "% of total)";
                GCPresentIncomeLabel.Text = "Income: " + income + " ("+ (100 * income / (int)GCPresentIncomeLabel.Tag).ToString("0") + "% of total)";
                GCPresentShipyardsLabel.Text = "Level 4 Shipyards: " + level_4 + "\nLevel 3 Shipyards: " + level_3 + "\nLevel 2 Shipyards: " + level_2 + "\nLevel 1 Shipyards: " + level_1;
                GCPresentBorderLabel.Text = "Border Planets: " + borderplanets + " (" + (100 * borderplanets / planetcount).ToString("0") + "% of owned territory)";
                GCPresentBorderingLabel.Text = "Bordering Planets: " + borderingplanets.Count;
                toolTip1.SetToolTip(GCPresentBorderingLabel, "Count of distinct planets owned by another playable/active faction bordering this faction\n" + SerializeStringArray(borderingplanets));
                GCPresentBorderFactionsLabel.Text = "Bordering Factions: " + borderingfactions.Count;
                toolTip1.SetToolTip(GCPresentBorderFactionsLabel, "Count of distinct playable/active factions bordering this faction's territory\n" + SerializeStringArray(borderingfactions));
            }
            else
            {
                GCPresentPlanetsLabel.Text = "";
                GCPresentIncomeLabel.Text = "";
                GCPresentShipyardsLabel.Text = "";
                GCPresentBorderLabel.Text = "";
                GCPresentBorderingLabel.Text = "";
                GCPresentBorderFactionsLabel.Text = "";
            }
            populateGCPlanetListBox();
        }

        private void GCPlanetListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (GCPlanetListBox.SelectedItems.Count > 0)
            {
                planet planet = (planet)GCPlanetListBox.SelectedItem;
                GCPictureBox.Image = new Bitmap(GCPictureBox.Width, GCPictureBox.Height);
                Graphics g = Graphics.FromImage(GCPictureBox.Image);
                g.DrawImage((Bitmap)GCPictureBox.Tag, 0, 0, new Rectangle(0, 0, GCPictureBox.Width, GCPictureBox.Height), GraphicsUnit.Pixel);
                int x = (Int32)(planet.x_coord * globals.scale + globals.origin) - 4;
                int y = (Int32)(planet.y_coord * globals.scale + globals.origin) - 4;
                Color brush = Color.FromArgb(255, planet.owner.color[0], planet.owner.color[1], planet.owner.color[2]);
                g.FillEllipse(new SolidBrush(brush), x, y, 9, 9);

                galacticConquest GC = (galacticConquest)GCListBox.SelectedItem;

                GCPlanetOwnerLabel.Text = "Owner: " + planet.owner.textname;
                GCPlanetIncomeLabel.Text = "Income: " + planet.credits;
                GCPlanetShipyardLabel.Text = "Shipyard Level: " + planet.shipyard;
                int connections = 0;
                List<string> connected = new List<string>();
                foreach(tradeRoute route in GC.traderouteObjects)
                {
                    if (route.planets[0] == planet.codename)
                    {
                        planet linked = GC.planetObjects.FirstOrDefault(s => s.codename == route.planets[1]);
                        if(!(linked.username is null)) connected.Add(linked.username);
                        connections++;
                    }
                    if (route.planets[1] == planet.codename)
                    {
                        planet linked = GC.planetObjects.FirstOrDefault(s => s.codename == route.planets[0]);
                        if (!(linked.username is null)) connected.Add(linked.username);
                        connections++;
                    }
                }
                GCPlanetConnectionLabel.Text = "Connections: " + connections;
                toolTip1.SetToolTip(GCPlanetConnectionLabel, "Connected to:\n" + SerializeStringArray(connected));
                int tradehub = 1;
                if (planet.tradehub) tradehub = globals.tradehubmultiplier;
                if (entities.modid == "") tradehub = 0;
                GCPlanetPotentialLabel.Text = "Potential Income: " + (getPotentialIncome(planet) + connections * globals.tradebase * tradehub);
                GCPlanetForceLabel.Text = "Forces:";

                for(int i = 0; i < GC.forceLocation[GCActiveListBox.SelectedIndex].Count; i++)
                {//Todo read name from entities.objects
                    if (GC.forceLocation[GCActiveListBox.SelectedIndex][i] == planet.codename)
                    {
                        string aswrit = GC.forceType[GCActiveListBox.SelectedIndex][i];
                        unit userfacing = entities.objects.FirstOrDefault(s => s.unitname == aswrit);
                        if (!(userfacing.username is null) && userfacing.username != "") aswrit = userfacing.username;
                        GCPlanetForceLabel.Text += "\n" + aswrit;
                    }
                }
            }
            else
            {
                GCPlanetOwnerLabel.Text = "";
                GCPlanetIncomeLabel.Text = "";
                GCPlanetShipyardLabel.Text = "";
                GCPlanetConnectionLabel.Text = "";
                GCPlanetPotentialLabel.Text = "";
                GCPlanetForceLabel.Text = "";
            }
        }

        private void GCPresentClearButton_Click(object sender, EventArgs e)
        {
            GCPresentListbox.SelectedItems.Clear();
        }

        private void GCGoToPlanetButton_Click(object sender, EventArgs e)
        {
            if (GCPlanetListBox.SelectedItems.Count > 0)
            {
                insert_history((int)historymaintabs.planet, 0, ((planet)GCPlanetListBox.SelectedItem).codename, true);
            }
        }

        private void GCDialogListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(GCDialogListBox.SelectedItems.Count > 0)
            {
                GCChapterListBox.Items.Clear();
                string[] dialogfile = File.ReadAllLines(getDialogPath((string)GCDialogListBox.SelectedItem));
                foreach(string line in dialogfile)
                {
                    if (line.Contains("[CHAPTER ")) GCChapterListBox.Items.Add(line.Replace("[CHAPTER ", "").Replace("]", ""));
                }
                if (GCChapterListBox.Items.Count > 0) GCChapterListBox.SelectedIndex = 0;
            }
        }

        private void GCChapterListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string[] dialogfile = File.ReadAllLines(getDialogPath((string)GCDialogListBox.SelectedItem));
            bool active = false;
            string dialogtext = "";
            foreach (string line in dialogfile)
            {
                if (line.Contains("[CHAPTER " + (string)GCChapterListBox.SelectedItem + "]")) active = true;
                else if (line.Contains("[CHAPTER ") && active) break;

                if (active)
                {
                    if(line.Length > 6)
                    {
                        //if (line.Substring(0, 5) == "TITLE") dialogtext += Find_Text_Entry(line.Substring(6, line.Length - 6)) + "\n"; Theoretically valid, but tends to be duplicated in the first line anyway
                        if (line.Substring(0, 5) == "TEXT ") dialogtext += Find_Text_Entry(line.Substring(5, line.Length - 5), entities) + "\n";
                        else if (line.Substring(0, 7) == "NEWLINE") dialogtext += "\n";
                    }
                }
            }
            GCStoryTextBox.Text = dialogtext;
        }

        private void GCSpeechListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (GCSpeechListBox.SelectedItems.Count > 0)
            {
                GCStoryTextBox.Text = ((speechevent)GCSpeechListBox.SelectedItem).speech;
            }
        }

        private void AbilityTargetUnitLabel_Click(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            if (me.Button == MouseButtons.Left) MessageBox.Show(AbilityTargetUnitLabel.Text);
            else if (me.Button == MouseButtons.Right) System.Windows.Forms.Clipboard.SetText(AbilityTargetUnitLabel.Text);
        }

        private void SpeechCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            //GCSpeechListBox.Visible = SpeechCheckBox.Checked; //Auto alignment is weird
            GCDialogListBox.Visible = !SpeechCheckBox.Checked;
            GCChapterListBox.Visible = !SpeechCheckBox.Checked;
            GCChapterLabel.Visible = !SpeechCheckBox.Checked;
        }

        private void CheckWeaponMismatchButton_Click(object sender, EventArgs e)
        {
            if (UnitListBox.Tag is null) return;
            unit unit = (unit)UnitListBox.Tag;
            string corenne = "";
            List<hardpoint> hps = unit.consolidatedhps;
            foreach (string hardpoint in unit.Hardpoints)
            {
                int index = LookupUntemplateID(hardpoint);
                for (int j = 0; j < entities.hardpointhashes[index].Count; j++)
                {
                    hardpoint hp2 = entities.hardpoints[entities.hardpointhashes[index][j]];
                    if (hp2.name == hardpoint)
                    {
                        for (int k = 0; k < hps.Count; k++)
                        {
                            if (hpEquality(hp2, hps[k]))
                            {
                                if (hp2.firesound != hps[k].firesound) corenne += hp2.name + " fire: " + hp2.firesound + "\n";
                                if (hp2.diesound != hps[k].diesound) corenne += hp2.name + " die: " + hp2.diesound + "\n";
                                break;
                            }
                        }
                        break;
                    }
                }
            }

            if (corenne == "") MessageBox.Show("All hard points of the same type have matching sounds");
            else
            {
                TextDetail deets = new TextDetail();
                deets.detail = "The following hardpoint sounds do not match others of the same type\n\n" + corenne;
                deets.Show();
            }
        }

        //Don't put any functions below here if you want it to still compile
    }
}
