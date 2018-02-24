using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using Darkages.Common;
using Darkages.Network.Game;
using Darkages.Network.Login;
using Darkages.Network.Object;
using Darkages.Storage;
using Darkages.Storage.locales.debuffs;
using Darkages.Types;

namespace Darkages
{
    public class ServerContext : ObjectManager
    {
        public static int Errors;
        public static int DefaultPort;
        public static bool Running;
        public static GameServer Game;
        public static LoginServer Lobby;
        public static ServerConstants Config;
        public static IPAddress Ipaddress => IPAddress.Parse(File.ReadAllText("server.tbl"));
        public static string StoragePath => @"..\..\..\Storage\Locales";        
        public static List<Redirect> GlobalRedirects = new List<Redirect>();
        public static List<Metafile> GlobalMetaCache = new List<Metafile>();

        public static Dictionary<int, Area> GlobalMapCache =
            new Dictionary<int, Area>();

        public static Dictionary<string, MonsterTemplate> GlobalMonsterTemplateCache =
            new Dictionary<string, MonsterTemplate>();

        public static Dictionary<string, SkillTemplate> GlobalSkillTemplateCache =
            new Dictionary<string, SkillTemplate>();

        public static Dictionary<string, SpellTemplate> GlobalSpellTemplateCache =
            new Dictionary<string, SpellTemplate>();

        public static Dictionary<string, ItemTemplate> GlobalItemTemplateCache =
            new Dictionary<string, ItemTemplate>();

        public static Dictionary<string, MundaneTemplate> GlobalMundaneTemplateCache =
            new Dictionary<string, MundaneTemplate>();

        public static List<WarpTemplate> GlobalWarpTemplateCache =
            new List<WarpTemplate>();

        public static Dictionary<int, WorldMapTemplate> GlobalWorldMapTemplateCache =
            new Dictionary<int, WorldMapTemplate>();

        public static Board[] Community = new Board[7];

        public static void LoadSkillTemplates()
        {
            Console.WriteLine("\n----- Loading Skills -----");
            StorageManager.SKillBucket.CacheFromStorage();
            Console.WriteLine(" ... Skill Templates Loaded: {0}", GlobalSkillTemplateCache.Count);
        }

        public static void LoadSpellTemplates()
        {
            Console.WriteLine("\n----- Loading Spells -----");
            StorageManager.SpellBucket.CacheFromStorage();
            Console.WriteLine(" ... Spell Templates Loaded: {0}", GlobalSpellTemplateCache.Count);
        }

        public static void LoadItemTemplates()
        {
            Console.WriteLine("\n----- Loading Items -----");
            StorageManager.ItemBucket.CacheFromStorage();
            Console.WriteLine(" ... Item Templates Loaded: {0}", GlobalItemTemplateCache.Count);
        }

        public static void LoadMonsterTemplates()
        {
            Console.WriteLine("\n----- Loading Monsters -----");
            StorageManager.MonsterBucket.CacheFromStorage();
            Console.WriteLine(" ... Monster Templates Loaded: {0}", GlobalMonsterTemplateCache.Count);
        }

        public static void LoadMundaneTemplates()
        {
            Console.WriteLine("\n----- Loading Mundanes -----");
            StorageManager.MundaneBucket.CacheFromStorage();
            Console.WriteLine(" ... Mundane Templates Loaded: {0}", GlobalMundaneTemplateCache.Count);
        }

        public static void LoadWarpTemplates()
        {
            Console.WriteLine("\n----- Loading Warp Portals -----");
            StorageManager.WarpBucket.CacheFromStorage();
            Console.WriteLine(" ... Warp Templates Loaded: {0}", GlobalWarpTemplateCache.Count);
        }

        public static void LoadWorldMapTemplates()
        {
            Console.WriteLine("\n----- Loading World Maps -----");
            StorageManager.WorldMapBucket.CacheFromStorage();
            Console.WriteLine(" ... World Map Templates Loaded: {0}", GlobalWorldMapTemplateCache.Count);
        }

        public static void LoadMaps()
        {
            Console.WriteLine("\n----- Loading Maps -----");
            StorageManager.AreaBucket.CacheFromStorage();
            Console.WriteLine(" -> Map Templates Loaded: {0}", GlobalMapCache.Count);
        }

        private static void StartServers()
        {
            Running = false;

            redo:
            if (Errors > Config.ERRORCAP)
                Process.GetCurrentProcess().Kill();

            try
            {
                Lobby = new LoginServer(Config.ConnectionCapacity);
                Lobby.Start(Config.LOGIN_PORT);
                Game = new GameServer(Config.ConnectionCapacity);
                Game.Start(DefaultPort);

                Running = true;
            }
            catch (Exception)
            {
                ++DefaultPort;
                Errors++;
                goto redo;
            }
        }

        /// <summary>
        ///     EP
        /// </summary>
        public virtual void Start()
        {
            Startup();
        }

        public static void Startup()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Config.SERVER_TITLE);
            Console.WriteLine("----------------------------------------------------------------------");

            LoadConstants();
            LoadAndCacheStorage();
            StartServers();

            Console.WriteLine("\n----------------------------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("{0} Online.", Config.SERVER_TITLE);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void EmptyCacheCollectors()
        {
            GlobalItemTemplateCache = new Dictionary<string, ItemTemplate>();
            GlobalMapCache = new Dictionary<int, Area>();
            GlobalMetaCache = new List<Metafile>();
            GlobalMonsterTemplateCache = new Dictionary<string, MonsterTemplate>();
            GlobalMundaneTemplateCache = new Dictionary<string, MundaneTemplate>();
            GlobalRedirects = new List<Redirect>();
            GlobalSkillTemplateCache = new Dictionary<string, SkillTemplate>();
            GlobalSpellTemplateCache = new Dictionary<string, SpellTemplate>();
            GlobalWarpTemplateCache = new List<WarpTemplate>();
            GlobalWorldMapTemplateCache = new Dictionary<int, WorldMapTemplate>();
        }

        public static void LoadConstants()
        {
            var _config_ = StorageManager.Load<ServerConstants>();

            if (_config_ == null)
            {
                Console.WriteLine("No config found. Generating defaults.");
                Config = new ServerConstants();
                StorageManager.Save(Config);
            }
            else
            {
                Config = StorageManager.Load<ServerConstants>();
            }

            InitFromConfig();
        }

        public static void InitFromConfig()
        {
            DefaultPort = Config.SERVER_PORT;

            if (!Directory.Exists(StoragePath))
                Directory.CreateDirectory(StoragePath);
        }

        public static void LoadMetaDatabase()
        {
            Console.WriteLine("\n----- Loading Meta Database -----");
            GlobalMetaCache.AddRange(MetafileManager.GetMetafiles());
            Console.WriteLine(" -> Building Meta Cache: {0} loaded.", GlobalMetaCache.Count);
        }

        public static void SaveCommunityAssets()
        {
            List<Board> tmp;

            lock (Community)
            {
                tmp = new List<Board>(Community);
            }

            foreach (var asset in tmp)
            {
                asset.Save();
            }
        }

        public static void CacheCommunityAssets()
        {
            Community = 
                Board.CacheFromStorage().OrderBy(i => i.Index).ToArray();
        }

        public static void LoadAndCacheStorage()
        {
            EmptyCacheCollectors();
            {
                LoadMetaDatabase();
                LoadMaps();
                LoadSkillTemplates();
                LoadSpellTemplates();
                LoadItemTemplates();
                LoadMonsterTemplates();
                LoadMundaneTemplates();
                LoadWarpTemplates();
                LoadWorldMapTemplates();
                CacheCommunityAssets();
            }

            var w = new WarpTemplate();
            w.WarpType = WarpType.Map;
            w.LevelRequired = 1;
            w.Name = "To Lost Wilderness";
            w.To = new Warp()
            {
                AreaID = 101,
                Location = new Position(46, 22),
            };
            w.WarpRadius = 0;
            w.ActivationMapId = 99;
            w.Activations = new List<Warp>();

            w.Activations.Add(new Warp()
            {
                AreaID = 99,
                Location = new Position(43, 99),
                PortalKey = 0
            });
            w.Activations.Add(new Warp()
            {
                AreaID = 99,
                Location = new Position(42, 99),
                PortalKey = 0
            });
            w.Activations.Add(new Warp()
            {
                AreaID = 99,
                Location = new Position(44, 99),
                PortalKey = 0
            });



            StorageManager.WarpBucket.Save(w);

            GlobalWarpTemplateCache.Add(w);


            var spell = new SpellTemplate();
            spell.Name = "beag ioc";
            spell.LevelRate = 1.5;
            spell.ManaCost = 60;
            spell.Icon = 28;
            spell.MaxLevel = 100;
            spell.Pane = Pane.Spells;
            spell.Sound = 8;
            spell.Animation = 267;
            spell.ScriptKey = "beag ioc";
            spell.TargetType = SpellTemplate.SpellUseType.ChooseTarget;
            spell.TierLevel = Tier.Tier1;
            spell.BaseLines = 0;

            GlobalSpellTemplateCache["beag ioc"] = spell;

            var spell2 = new SpellTemplate();
            spell2.Name = "fas nadur";
            spell2.LevelRate = 0.01;
            spell2.ManaCost = 10;
            spell2.Icon = 119;
            spell2.MaxLevel = 70;
            spell2.Pane = Pane.Spells;
            spell2.Sound = 20;
            spell2.Animation = 126;
            spell2.ScriptKey = "fas nadur";
            spell2.TargetType = SpellTemplate.SpellUseType.ChooseTarget;
            spell2.TierLevel = Tier.Tier1;
            spell2.BaseLines = 4;
            spell2.Debuff = new debuff_fasnadur();

            GlobalSpellTemplateCache["fas nadur"] = spell2;

            var npc = new MundaneTemplate();
            npc.Name = "Sage Master";
            npc.ScriptKey = "welcome aisling";
            npc.Speech.Add("You lost mate?");
            npc.Speech.Add("You better gear up bud.");
            npc.Speech.Add("You might end up in someones rape dungeon.");
            npc.Speech.Add("and by that, i mean mine.");
            npc.Speech.Add("do you need a ride little boy?");
            npc.X = 34;
            npc.Y = 13;
            npc.Image = 0x415B;
            npc.Direction = (byte)Direction.South;
            npc.Level = 99;
            npc.EnableSpeech = true;
            npc.QuestKey = "welcome trials 1";
            npc.AreaID = 101;
            npc.EnableAttacking = true;
            npc.EnableWalking = false;
            npc.EnableTurning = true;
            npc.EnableCasting = true;
            npc.Spells = new List<string>() { "deo saighead lamh", "ard cradh", "pramh", "ard sal", "ard srad", "mor dion", "beag ioc fein" };
            GlobalMundaneTemplateCache["Sage Master"] = npc;

            var npc2 = new MundaneTemplate();
            npc2.Name = "Arch Wizard";
            npc2.ScriptKey = "welcome aisling";
            npc2.Speech.Add("I'll Save you.");
            npc2.Speech.Add("Stick with me kid.");
            npc2.Speech.Add("it's a nice night, tonight.");
            npc2.Speech.Add("Right?");
            npc2.Speech.Add("Exit this way.");
            npc2.Speech.Add("Head for Rucession, if i were you.");
            npc2.X = 49;
            npc2.Y = 18;
            npc2.Image = 0x4162;
            npc2.Direction = (byte)Direction.East;
            npc2.Level = 99;
            npc2.EnableSpeech = true;
            npc2.QuestKey = "welcome trials 2";
            npc2.AreaID = 101;
            npc2.EnableAttacking = true;
            npc2.EnableWalking = false;
            npc2.EnableTurning = true;
            npc2.EnableCasting = true;
            npc2.Spells = new List<string>() { "deo saighead", "mor cradh", "pramh" };
            GlobalMundaneTemplateCache["Arch Wizard"] = npc2;


            var npc3 = new MundaneTemplate();
            npc3.Name = "Priest";
            npc3.ScriptKey = "welcome aisling";
            npc3.X = 38;
            npc3.Y = 19;
            npc3.Image = 0x415E;
            npc3.Direction = (byte)Direction.South;
            npc3.Level = 99;
            npc3.QuestKey = "welcome trials 3";
            npc3.AreaID = 101;
            npc3.EnableAttacking = true;
            npc3.EnableWalking = false;
            npc3.EnableTurning = true;
            npc3.EnableCasting = true;
            npc3.Spells = new List<string>() { "beag ioc", "cradh", "final destination" };
            GlobalMundaneTemplateCache["Priest"] = npc3;

            

            Console.WriteLine("\n");
        }
    }
}