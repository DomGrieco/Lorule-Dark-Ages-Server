using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace Darkages.Types
{
    public class MonsterTemplate : Template
    {
        [JsonProperty] [Description("What Drops?")]
        public Collection<string> Drops = new Collection<string>();

        [Description("Template ID. Must be Unique.")]
        public int ID { get; set; }

        [Description("What sprite ID? range from 0x4000 - 0x8000 ")]
        public ushort Image { get; set; }

        [Description("What script will this monster run?")]
        public string ScriptName { get; set; }

        [Description("Leave Empty unless SpawnQualifer = Reactor.")]
        public string ReactorScriptName { get; set; }

        [Description("Leave empty unless SpawnQualifer = Defined.")]
        public ushort DefinedX { get; set; }

        [Description("Leave empty unless SpawnQualifer = Defined.")]
        public ushort DefinedY { get; set; }

        public LootQualifer LootType { get; set; }

        public MoodQualifer MoodType { get; set; }

        public SpawnQualifer SpawnType { get; set; }

        public ElementQualifer ElementType { get; set; }

        public PathQualifer PathQualifer { get; set; }

        public byte Level { get; set; }

        public int MaximumHP { get; set; }

        public int MaximumMP { get; set; }

        public int SpawnerID { get; set; }

        public int AreaID { get; set; }

        public int MovementSpeed { get; set; }

        public int CastSpeed { get; set; }

        public bool IsBoss { get; set; }

        [Description("Monsters spawned will not exceed this.")]
        public int SpawnMax { get; set; }

        [Description("In seconds, what is the respawn rate?")]
        public int SpawnRate { get; set; }

        [Description("How many monsters will i spawn at any single time?")]
        public int SpawnSize { get; set; }

        public int AttackSpeed { get; set; }

        public bool IgnoreCollision { get; set; }

        [Description("Does this monster have various other sprites? use 0 if not.")]
        public int ImageVarience { get; set; }

        [Description("Does this aisling spawn if no aislings are on this map? default = false")]
        public bool SpawnOnlyOnActiveMaps { get; set; }

        [Description("Does this monster grow stonger over time? default = false")]
        public bool Grow { get; set; }

        [JsonProperty]
        [Description("What Spells will this monster cast?")]
        public Collection<string> SpellScripts { get; set; }

        [JsonProperty]
        [Description("What Skills will this monster use?")]
        public Collection<string> SkillScripts { get; set; }

        public ElementManager.Element DefenseElement { get; set; }

        public ElementManager.Element OffenseElement { get; set; }

        [JsonProperty]
        public List<Position> Waypoints { get; set; }

        public bool UpdateMapWide { get; set; }

        [JsonIgnore]
        public DateTime NextAvailableSpawn { get; set; }

        [JsonIgnore]
        public bool Ready => DateTime.UtcNow > NextAvailableSpawn;

        public bool ReadyToSpawn()
        {
            if (Ready)
            {
                NextAvailableSpawn = DateTime.UtcNow.AddSeconds(SpawnRate);
                return true;
            }
            return false;
        }
    }
}