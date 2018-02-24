using Darkages.Storage;
using Darkages.Types;
using Newtonsoft.Json;

namespace Darkages
{
    public class ServerConstants
    {
        [JsonProperty] public bool AssailsCancelSpells = true;

        [JsonProperty] public int AutoLootPickupDistance = 2;

        [JsonProperty] public string BadRequestMessage = "(Invalid Request)";

        [JsonProperty]
        /// <summary>
        /// WHat is the starting base armor class?
        /// </summary>
        public sbyte BaseAC = 70;

        [JsonProperty] public byte BaseMR = 70;

        [JsonProperty] public byte BaseStatAttribute = 3;

        [JsonProperty]
        /// <summary>
        /// Buffer Size - this can be changed, but the minimum recommended value
        /// should be between 2048 and 16384, by default we use 16384
        /// </summary>
        public int BufferSize = 0x4096;

        /// <summary>
        ///     SHould we Cache Objects?
        ///     Setting this option to true will enable the object manager
        ///     to save objects in memory to a disk, allowing them to persist
        ///     so if you close or restart the game server, they will be restored
        ///     and recached to memory.
        ///     For an example, using this option, if you drop an item on the ground
        ///     and then the server restarts or crashes, that item will be there when
        ///     it comes back online.
        /// </summary>
        [JsonProperty] public bool CacheObjects;

        [JsonProperty] public bool CancelCastingWhenWalking = true;

        [JsonProperty] public string CantCarryMoreMsg = "You can't carry more.";

        [JsonProperty] public string CantDoThat = "You can't do that.";

        [JsonProperty] public string CantDropItemMsg = "You can't drop that.";

        [JsonProperty] public string CantEquipThatMessage = "You can't use that.";

        [JsonProperty] public string CantUseThat = "You can't use that.";

        [JsonProperty]
        /// <summary>
        /// Prefix, Suffix for Spell chants.
        /// by default, using *
        /// </summary>
        public string ChantPrefix = "*";

        [JsonProperty] public string ChantSuffix = "*";

        [JsonProperty] public int ClickLootDistance = 3;

        [JsonProperty] public int ClientPacketSendErrorLimit = 3;

        [JsonProperty] public string ConAddedMessage = "You become more fit.";

        [JsonProperty] public int ConnectionCapacity = 2048;

        [JsonProperty]
        /// <summary>
        /// In seconds, what is the daytime interval
        /// </summary>
        public double DayTimeInterval = 30.0;

        [JsonProperty] public int DeathFreeLevelCap = 11;

        [JsonProperty]
        /// <summary>
        /// What is the Health Lost when restoring Life?
        /// This is a penalty value. and should reflect the type of game play difficulty.
        /// USDA uses 50. so this is the default value.
        /// </summary>
        public int DeathHPPenalty = 50;

        [JsonProperty] public int DeathMap = 88888;

        [JsonProperty] public bool DebugMode;

        [JsonProperty] public ItemColor DefaultItemColor = ItemColor.blue;

        [JsonProperty] public uint DefaultItemDurability = 1000;

        [JsonProperty] public uint DefaultItemValue = 500;

        [JsonProperty]
        /// <summary>
        /// Default Security Encryption Key.
        /// This is currently the MD5 Hash of our client.
        /// we will use it later to determine if the client has been modified
        /// </summary>
        public string DefaultKey = "73F2BE80DDEB1BFDC887AB1C3CA18365";

        [JsonProperty] public string DexAddedMessage = "You feel more flexible.";

        [JsonProperty]
        /// <summary>
        /// Number of Seconds to wait if a client is disconnected before
        /// They are disposed of completely.
        /// This allows time for all updateable components to handle the client
        /// and complete all operations before the client is disposed of.
        /// Default valie is 5. and is the recommended value for lorule.
        /// </summary>
        public int DisposeTimeout = 5;

        [JsonProperty] public string DoesNotFitMessage = "That does not fit you.";

        [JsonProperty] public bool DontTurnDuringRefresh;

        [JsonProperty] public double DropDecayInSeconds = 120;

        [JsonProperty] public int DurabilityRolloverLimit = 10;

        [JsonProperty] public int ERRORCAP = 5;

        [JsonProperty]
        /// <summary>
        /// in Seconds, What is the Frame Rate of our server?
        /// By Default we use 60 frames per second.
        /// 30 is also a good suitable frame value, but going to low will cause
        /// a visible delay. I don't recommend going lower then 20 frames per second.
        /// and i would not recommend going higher then 60 frames per second. unless you have a super duper computer!
        /// </summary>
        public int FRAMES = 60;

        [JsonProperty]
        /// <summary>
        /// Ms between 0 cooldown based skills.
        /// </summary>
        public double GlobalBaseSkillDelay = 650;

        [JsonProperty]
        /// <summary>
        /// What is the rate we invoke the Monolith?
        /// Monster Templates in the end control the spawn rates,
        /// But this controls how often we spin up the templates.
        /// By default,and to keep things lowcpu usage, i would not go below 1000ms.
        /// </summary>
        public double GlobalSpawnTimer = 1000.0f;

        [JsonProperty] public string GroupedAlreadyMsg = "noname is already in a group.";

        [JsonProperty] public string GroupRequestDeclinedMsg = "noname does not wish to join your group.";

        [JsonProperty]
        /// <summary>
        /// Client Handshake message.
        /// This can be anything stating with C, and ending with \n.
        /// </summary>
        public string HandShakeMessage = "CAN WE ALL GET ALONG\n";

        [JsonProperty]
        /// <summary>
        /// In milliseconds, what is the tolerance allowed to a chance to heal-out?
        /// using heals, items, skills that grant +HP?
        /// by default this is 300ms, You should increase this to increase the chances of healing out.
        /// i would not recommend a value higher then 750ms, as healing out will become to likely.
        /// </summary>
        public double HealoutTolerance = 10;

        [JsonProperty] public int HelperMenuId = -1;

        [JsonProperty] public string HelperMenuTemplateKey = "Lorule Helper";

        [JsonProperty] public string IntAddedMessage = "Your mind expands.";

        [JsonProperty] public string LevelUpMessage = "You have reached level {0}";

        [JsonProperty] public bool LimitWalkingWhenRefreshing;

        [JsonProperty]
        /// <summary>
        /// How long should an aisling linger around, before we declare them as logged-in?
        /// </summary>
        public double LingerState = 1000;

        [JsonProperty]
        /// <summary>
        /// Should we log Destroyed objects that are not accounted for by to the debug log?
        /// This means objects that have died for reasons outside of aisling control.
        /// like if a NPC or event destroyed an object. 
        /// (for example, a mundane killed a monster object).
        /// By default, This is disabled.
        /// </summary>
        public bool LogDestroyedObjects;

        [JsonProperty] public int LOGIN_PORT = 2610;

        [JsonProperty]
        /// <summary>
        /// Log Gameserver Recv Packets?
        /// </summary>
        public bool LogRecvPackets;

        [JsonProperty]
        /// <summary>
        /// Log Gameserver Sent Packets?
        /// </summary>
        public bool LogSentPackets;

        [JsonProperty] public bool LootSingleMode = true;

        [JsonProperty] public double MapUpdateInterval = 1500;

        [JsonProperty] public int MaxCarryGold = 100000000;

        [JsonProperty]
        /// <summary>
        /// How many seconds should we clear message bar for aislings?
        /// This timer will commence 5 seconds after the last message was sent.
        /// By default, 10 mimics the USDA rate.
        /// </summary>
        public double MessageClearInterval = 12;

        [JsonProperty] public double MinimalLatency = 10;

        [JsonProperty]
        /// <summary>
        /// What is the Lowest HP an aisling can reach under any circumstances?
        /// </summary>
        public int MinimumHp = 50;

        [JsonProperty] public double MonsterDamageFactor = 1.58;

        [JsonProperty] public int MonsterDamageMultipler = 5;

        [JsonProperty] public ushort MonsterDeathAnimationNumber = 350;

        [JsonProperty] public int MonsterSkillSuccessRate = 40;

        [JsonProperty] public int MonsterSpellSuccessRate = 30;

        [JsonProperty]
        /// <summary>
        /// This controls how often we check for dead mundanes,
        /// and controls when to spin up there templates for respawning.
        /// Mundanes are not supposed to die much, so i would keep it at 3.0
        /// to keep server usage down. but you can probably go as high as 60.0.
        /// this value is in seconds.
        /// </summary>
        public double MundaneRespawnInterval = 2.0;

        [JsonProperty] public string NotEnoughGoldToDropMsg = "You wish you had that much.";

        [JsonProperty] public double ObjectCacheInterval = 20;

        [JsonProperty] public double ObjectGarbageCollectorInterval = 500;

        [JsonProperty]
        /// <summary>
        /// In Seconds, How often should we check for new objects around aislings
        /// and remove out of view objects, add inview objects?
        /// This value is in seconds, And to keep usage low, I would not recommend going below 1.
        /// </summary>
        public double ObjectHandlerInterval = 1;

        [JsonProperty] public int PacketOverflowLimit = 1024;

        [JsonProperty]
        /// <summary>
        /// In Seconds, How often should we ping the client?
        /// this by default is every 35 seconds.
        /// this also controls the auto-save function.
        /// and auto-saves will also occur on this interval.
        /// </summary>
        public double PingInterval = 10.0;

        [JsonProperty] public bool RefreshOnWalkCollision = true;

        [JsonProperty]
        /// <summary>
        /// What is the time between aisling f5ing?
        /// </summary>
        public int RefreshRate = 300;

        [JsonProperty]
        /// <summary>
        /// What is the Regen Rate Expononent Modifier?
        /// 0.15 is about the same rate as USDA.
        /// </summary>
        public double RegenRate = 0.15;

        [JsonProperty]
        /// <summary>
        /// In Seconds, How often should we have active aislings?
        /// </summary>
        public double SaveRate = 30.0;

        [JsonProperty] public int SERVER_PORT = 2615;

        [JsonProperty] public string SERVER_TITLE = "Lorule Dark ages Server - Accepting Clients Version 7.18";

        [JsonProperty]
        /// <summary>
        /// What is the tolerance for packet sending overflow?
        /// This should be a low number, I recommend using a value between 1 and 10.
        /// </summary>
        public int ServerOverflowTolerate = 256;

        [JsonProperty] public string ServerTablePath = "server.tbl";

        [JsonProperty]
        public string ServerWelcomeMessage = "Welcome to Lorule, If you need help, Please use The [F1] menu.";

        [JsonProperty] public bool ShowMonsterDeathAnimation;

        [JsonProperty]
        /// <summary>
        /// The Number of sprites to load in a batch 0x07 Packet.
        /// I would not change this setting if you don't know what it does!
        /// Setting to high can crash the game client.
        /// I would recommend no lower then 12, no higher then 30.
        /// </summary>
        public int SpriteBatchSize = 24;

        [JsonProperty] public int StartingMap = 100;

        [JsonProperty] public Position StartingPosition = new Position(5, 5);

        [JsonProperty]
        /// <summary>
        /// Maximum Server Capacity for stat attributes
        /// Recommended to keep it below 255.
        /// </summary>
        public byte StatCap = 255;

        [JsonProperty] public string StrAddedMessage = "You become stronger.";

        [JsonProperty]
        /// <summary>
        /// In Seconds, what is the timeout value?
        /// Aisling will disconnect if no connection activity within this period.
        /// by default, 20 seconds is a pretty good indication.
        /// </summary>
        public double Timeout = 60;

        [JsonProperty] public string ToWeakToLift = "You are to weak to even lift it.";

        [JsonProperty] public bool TransFormAsParallel = true;

        [JsonProperty] public short TransitionPointX = 3;

        [JsonProperty] public short TransitionPointY = 3;

        [JsonProperty] public int TransitionZone = 9999;

        [JsonProperty] public bool UseFastRandom = true;

        [JsonProperty] public bool UseFastSqrtMethod;

        [JsonProperty] public bool UseIncrementalSerials = true;

        [JsonProperty] public string UserDroppedGoldMsg = "noname has dropped some money nearby.";

        [JsonProperty] public int Version = 718;

        [JsonProperty] public int VeryNearByProximity = 5;

        [JsonProperty] public ushort WarpAnimationNumber = 214;

        [JsonProperty] public double WarpUpdateTimer = 1.0;

        [JsonProperty] public double WeightIncreaseModifer = 3.5;

        [JsonProperty] public string WisAddedMessage = "Your will increases.";

        [JsonProperty] public double WithinRangeProximity = 10.0;

        [JsonProperty] public string YouDroppedGoldMsg = "you dropped some gold.";

        [JsonProperty] public int LootTableStackSize = 3;

        [JsonProperty] public int HpGainFactor = 5;

        [JsonProperty] public int MpGainFactor = 5;

        [JsonProperty] public int StatsPerLevel = 2;

        [JsonProperty] public string NoManaMessage = "Your will is too weak.";

        [JsonProperty] public double FasNadurStrength = 3.00;

        [JsonProperty] public double MorFasNadurStrength = 4.50;

        public override string ToString()
        {
            return StorageManager.Save(this)
                   ?? string.Empty;
        }
    }
}