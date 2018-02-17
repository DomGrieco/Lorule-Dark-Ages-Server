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


            //var s = GlobalSkillTemplateCache["Double Punch"];
            //s.Prerequisites = new LearningPredicate();
            //s.Prerequisites.Skill_Level_Required = 30;
            //s.Prerequisites.Skill_Required = "Assail";
            //s.Prerequisites.Skill_Tier_Required = 0;
            //s.Prerequisites.Str_Required = 12;
            //s.Prerequisites.Dex_Required = 8;
            //s.Prerequisites.Con_Required = 19;
            //s.Prerequisites.Class_Required = Class.Monk;
            //s.Prerequisites.Stage_Required = ClassStage.Class;
            //s.Prerequisites.ExpLevel_Required = 9;
            //s.Prerequisites.Gold_Required = 5000;
            //s.Prerequisites.Items_Required.Add(new ItemPredicate() { Item = "Dirk", AmountRequired = 1 });

            //s.NpcKey = "Learn Skills";

            //StorageManager.SKillBucket.Save(s, true);

            foreach (var s in GlobalItemTemplateCache.Values)
            {
                s.DropRate = Generator.Random.NextDouble() + (s.LevelRequired * 100 / 100 * 0.01);
            }

            GlobalItemTemplateCache["Dirk"].Flags |= ItemFlags.Stackable;
            GlobalItemTemplateCache["Dirk"].CanStack = true;
            GlobalItemTemplateCache["Dirk"].MaxStack = byte.MaxValue;


            GlobalSpellTemplateCache["deo saighead lamh"] = new SpellTemplate()
            {
                Animation = 128,
                BaseLines = 0,
                DamageExponent = 0.01,
                ElementalProperty = ElementManager.Element.Light,
                Icon = 179,
                ScriptKey = "Generic Elemental Mass",
                MinLines = 0,
                MaxLines = 9,
                Prerequisites = new LearningPredicate()
                {
                    Spell_Required = "deo saighead",
                    Spell_Level_Required = 100,
                    ExpLevel_Required = 14,
                    Int_Required = 30,
                    Wis_Required = 15,
                    Class_Required = Class.Priest,
                },
                Name = "deo saighead lamh",
                ManaCost = 30,
                LevelRate = 0.02,
                TierLevel = Tier.Tier2,
                MaxLevel = 100,
                NpcKey = "High Priest",
                Sound = 26,
                Pane = Pane.Spells,
                TargetType = SpellTemplate.SpellUseType.NoTarget,
            };

            Console.WriteLine("\n");
        }
    }
}