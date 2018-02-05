﻿using Darkages.Systems.Loot.Interfaces;
using Newtonsoft.Json;
using static Darkages.Types.ElementManager;

namespace Darkages.Types
{
    public class ItemUpgrade : ILootDefinition
    {
        public virtual string Name { get; set; }
        public virtual int Weight { get; set; }
        public virtual int Upgrade { get; set; }
    }

    public class Common : ItemUpgrade
    {
        public override string Name => "Upgraded";
        public override int Weight => 90;
        public override int Upgrade => 1;
    }

    public class Uncommon : ItemUpgrade
    {
        public override string Name => "Enhanced";
        public override int Weight => 85;
        public override int Upgrade => 2;
    }
    public class Rare : ItemUpgrade
    {
        public override string Name => "Rare";
        public override int Weight => 15;
        public override int Upgrade => 3;
    }
    public class Epic : ItemUpgrade
    {
        public override string Name => "Epic";
        public override int Weight => 10;
        public override int Upgrade => 4;
    }
    public class Legendary : ItemUpgrade
    {
        public override string Name => "Legendary";
        public override int Weight => 5;
        public override int Upgrade => 5;
    }
    public class Mythical : ItemUpgrade
    {
        public override string Name => "Mythical";
        public override int Weight => 3;
        public override int Upgrade => 6;
    }
    public class Godly : ItemUpgrade
    {
        public override string Name => "Godly";
        public override int Weight => 2;
        public override int Upgrade => 7;
    }
    public class Forsaken : ItemUpgrade
    {
        public override string Name => "Forsaken";
        public override int Weight => 1;
        public override int Upgrade => 8;
    }

    public class ItemTemplate : Template, ILootDefinition
    {
        public int ID { get; set; }

        public bool CanStack { get; set; }

        public byte MaxStack { get; set; }

        public ushort Image { get; set; }

        public ushort DisplayImage { get; set; }

        public string ScriptName { get; set; }

        public Gender Gender { get; set; }

        public StatusOperator HealthModifer { get; set; }

        public StatusOperator ManaModifer { get; set; }

        public StatusOperator StrModifer { get; set; }

        public StatusOperator IntModifer { get; set; }

        public StatusOperator WisModifer { get; set; }

        public StatusOperator ConModifer { get; set; }

        public StatusOperator DexModifer { get; set; }

        public StatusOperator AcModifer { get; set; }

        public StatusOperator MrModifer { get; set; }

        public StatusOperator HitModifer { get; set; }

        public StatusOperator DmgModifer { get; set; }

        public SpellOperator SpellOperator { get; set; }

        public Element OffenseElement { get; set; }

        public Element DefenseElement { get; set; }

        public byte CarryWeight { get; set; }

        public ItemFlags Flags { get; set; }

        public uint MaxDurability { get; set; }

        public uint Value { get; set; }

        public int EquipmentSlot { get; set; }

        public string NpcKey { get; set; }

        public Class Class { get; set; }

        public byte LevelRequired { get; set; }

        public int DmgMin { get; set; }

        public int DmgMax { get; set; }

        public double DropRate { get; set; }

        public ClassStage StageRequired { get; set; }

        public bool HasPants { get; set; }

        public ItemColor Color { get; set; }

        [JsonIgnore]
        public int Weight
        {
            get => (int)DropRate; set { }
        }
    }
}