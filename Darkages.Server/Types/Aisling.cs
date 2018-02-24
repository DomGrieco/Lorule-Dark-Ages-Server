using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Darkages.Network.ServerFormats;
using Darkages.Types;
using Newtonsoft.Json;

namespace Darkages
{
    public class Aisling : Sprite
    {
        [JsonIgnore] public int DamageCounter = 0;

        [JsonIgnore]
        public ConcurrentDictionary<int, Sprite> ViewFrustrum
            = new ConcurrentDictionary<int, Sprite>();

        [JsonIgnore]
        public bool HadDeathExperience { get; set; }

        [JsonIgnore]
        public CursedSachel Remains { get; set; }

        [JsonIgnore]
        public List<Sprite> ViewableObjects
        {
            get
            {
                return ViewFrustrum.Select(i => i.Value).ToList();
            }
        }


        public Aisling()
        {
            OffenseElement = ElementManager.Element.None;
            DefenseElement = ElementManager.Element.None;
            Clan = "";
            Nation = 3;
            Flags = AislingFlags.Normal;
            LegendBook = new Legend();
            ClanTitle = string.Empty;
            ClanRank = string.Empty;
            ActiveSpellInfo = null;
            LoggedIn = false;
            ActiveStatus = ActivityStatus.Awake;
            PortalSession = new PortalSession();
            Quests = new List<Quest>();
            PartyStatus = GroupStatus.AcceptingRequests;
            InvitePrivleges = true;
            LeaderPrivleges = false;
            Remains = new CursedSachel(this);
        }

        public int CurrentWeight { get; set; }
        public int MaximumWeight { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastLogged { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int AbpLevel { get; set; }
        public int AbpTotal { get; set; }
        public int AbpNext { get; set; }
        public int ExpLevel { get; set; }
        public int ExpTotal { get; set; }
        public int ExpNext { get; set; }
        public Gender Gender { get; set; }
        public int Title { get; set; }
        public AislingFlags Flags { get; set; }
        public int AreaID { get; set; }
        public int ClassID { get; set; }
        public int GamePoints { get; set; }
        public int GoldPoints { get; set; }
        public int StatPoints { get; set; }
        public int Medal { get; set; }
        public int LIcon { get; set; }
        public int RIcon { get; set; }
        public int BodyColor { get; set; }
        public int BodyStyle { get; set; }
        public int FaceColor { get; set; }
        public int FaceStyle { get; set; }
        public byte HairColor { get; set; }
        public byte HairStyle { get; set; }
        public byte Boots { get; set; }
        public int Helmet { get; set; }
        public byte Shield { get; set; }
        public ushort Weapon { get; set; }
        public ushort Armor { get; set; }
        public byte OverCoat { get; set; }
        public byte Pants { get; set; }
        public byte[] PictureData { get; set; }
        public string ProfileMessage { get; set; }
        public bool LoggedIn { get; set; }
        public byte Nation { get; set; }
        public string Clan { get; set; }
        public byte Resting { get; set; }
        public GroupStatus PartyStatus { get; set; }

        [JsonIgnore] public Party GroupParty { get; set; }

        [JsonIgnore] [Browsable(false)] public bool IsCastingSpell { get; set; }

        public SkillBook SkillBook { get; set; }

        public SpellBook SpellBook { get; set; }

        public Inventory Inventory { get; set; }

        public EquipmentManager EquipmentManager { get; set; }

        public ActivityStatus ActiveStatus { get; set; }

        [JsonIgnore] public bool Dead => Flags == AislingFlags.Dead;

        [JsonIgnore] public bool Invisible => Flags == AislingFlags.Invisible;

        public Dictionary<string, int> MonsterKillCounters = new Dictionary<string, int>();

        public byte Blind { get; set; }

        public byte HeadAccessory1 { get; set; }

        public byte HeadAccessory2 { get; set; }

        [JsonIgnore] [Browsable(false)] public new Position Position => new Position(X, Y);

        public ClassStage Stage { get; set; }

        public Class Path { get; set; }

        [Browsable(false)] public Legend LegendBook { get; set; }

        public BodySprite Display { get; set; }

        public string ClanTitle { get; set; }

        public string ClanRank { get; set; }

        [JsonIgnore] public CastInfo ActiveSpellInfo { get; set; }

        public List<Quest> Quests { get; set; }

        [JsonIgnore] public List<Aisling> PartyMembers => GroupParty?.Members;

        [JsonIgnore]
        public PortalSession PortalSession { get; set; }

        [JsonIgnore]
        public Position LastPosition { get; set; }

        [JsonIgnore]
        public int LastMapId { get; set; }

        [JsonIgnore] public bool LeaderPrivleges { get; set; }

        [JsonIgnore] public bool InvitePrivleges { get; set; }

        public byte BootColor { get; set; }
        public byte NameColor { get; set; }

        public bool InsideView(Sprite obj)
        {
            if (ViewFrustrum.Count == 0)
                return false;

            return ViewFrustrum.ContainsKey(obj.Serial);
        }

        public void RemoveFromView(Sprite obj)
        {
            ViewFrustrum.TryRemove(obj.Serial, out var removed);
        }

        public void GoHome()
        {
            var DestinationMap = ServerContext.Config.TransitionZone;

            if (ServerContext.GlobalMapCache.ContainsKey(DestinationMap))
            {
                var targetMap = ServerContext.GlobalMapCache[DestinationMap];

                Client.LeaveArea(true, false);
                Client.Aisling.X = ServerContext.Config.TransitionPointX;
                Client.Aisling.Y = ServerContext.Config.TransitionPointY;
                Client.Aisling.AreaID = DestinationMap;
                Client.Aisling.CurrentMapId = DestinationMap;
                Client.EnterArea();
                Client.Refresh();
            }
        }

        public void View(Sprite obj)
        {
            if (!InsideView(obj))
                ViewFrustrum.TryAdd(obj.Serial, obj);
        }

        public void CastSpell(Spell spell)
        {
            if (!spell.CanUse())
            {
                spell.InUse = false;
                return;
            }


            if (spell.InUse)
                return;

            var info = Client.Aisling.ActiveSpellInfo;

            if (info != null)
            {

                if (!string.IsNullOrEmpty(info.Data))
                    spell.Script.Arguments = info.Data;

                var target = GetObject(i => i.Serial == info.Target, Get.Monsters | Get.Aislings | Get.Mundanes);
                spell.InUse = true;

                if (target != null)
                {
                    if (target is Aisling)
                        spell.Script.OnUse(this, target as Aisling);
                    if (target is Monster)
                        spell.Script.OnUse(this, target as Monster);
                    if (target is Mundane)
                        spell.Script.OnUse(this, target as Mundane);
                }
                else
                {
                    spell.Script.OnUse(this, this);
                }
            }
            else
            {
                spell.Script.OnUse(this, this);
            }

            spell.NextAvailableUse = DateTime.UtcNow.AddSeconds(info.SpellLines > 0 ? 0.5 : 0.2);
            spell.InUse = false;
        }

        public bool HasKilled(Template templateContext, int number)
        {
            if (MonsterKillCounters.ContainsKey(templateContext.Name))
            {
                return MonsterKillCounters[templateContext.Name] >= number;
            }

            return false;
        }

        public static Aisling Create()
        {
            var result = new Aisling
            {
                Created = DateTime.UtcNow,
                LastLogged = DateTime.UtcNow,

                Username = string.Empty,
                Password = string.Empty,
                X = ServerContext.Config.StartingPosition.X,
                Y = ServerContext.Config.StartingPosition.Y,

                AbpLevel = 0,
                AbpTotal = 0,
                AbpNext = 0,

                ExpLevel = 1,
                ExpTotal = 1,
                ExpNext = 600,
                Gender = 0,
                Title = 0,
                Flags = 0,
                AreaID = ServerContext.Config.StartingMap,
                ClassID = 0,
                Stage = ClassStage.Class,
                Path = Class.Peasant,
                CurrentHp = 60,
                CurrentMp = 30,
                _MaximumHp = 60,
                _MaximumMp = 30,
                _Str = 3,
                _Int = 3,
                _Wis = 3,
                _Con = 3,
                _Dex = 3,                

                GamePoints = 0,
                GoldPoints = 1000,
                StatPoints = 0,

                Medal = 0,
                LIcon = 0,
                RIcon = 0,

                BodyColor = 0,
                BodyStyle = 0,
                FaceColor = 0,
                FaceStyle = 0,
                HairColor = 0,
                HairStyle = 0,
                CurrentMapId = ServerContext.Config.StartingMap,
                SkillBook = new SkillBook(),
                SpellBook = new SpellBook(),
                Inventory = new Inventory(),
                EquipmentManager = new EquipmentManager(null),
                NameColor = 1,
                BootColor = 0,
                Amplified = 0
            };

            //Skill.GiveTo(result, "Assail");

            int idx = 1;
            foreach (var skill in ServerContext.GlobalSkillTemplateCache.Keys)
            {
                if (ServerContext.GlobalSkillTemplateCache[skill].Pane == Pane.Tools)
                    continue;

                Skill.GiveTo(result, skill);
            }


            foreach (var skill in ServerContext.GlobalSkillTemplateCache.Keys)
            {
                if (ServerContext.GlobalSkillTemplateCache[skill].Pane == Pane.Tools)
                {
                    Skill.GiveTo(result, skill, (byte)(72 + idx));
                    idx++;
                }
            }

            foreach (var spell in ServerContext.GlobalSpellTemplateCache.Keys)
            {
                if (ServerContext.GlobalSpellTemplateCache[spell].Pane == Pane.Tools)
                    continue;

                Spell.GiveTo(result, spell);
            }

            idx = 1;
            foreach (var spell in ServerContext.GlobalSpellTemplateCache.Keys)
            {
                if (ServerContext.GlobalSpellTemplateCache[spell].Pane == Pane.Tools)
                {
                    Spell.GiveTo(result, spell, (byte)(72 + idx));
                    idx++;
                }
            }

            if (DateTime.UtcNow.Year <= 2019)
            {
                result.LegendBook.AddLegend(new Legend.LegendItem
                {
                    Category = "Event",
                    Color = (byte)LegendColor.Green,
                    Icon = (byte)LegendIcon.Victory,
                    Value = string.Format("Aisling Age of Aquarius")
                });
            }

            return result;
        }

        /// <summary>
        ///     Removes Aisling, Sends Remove packet to nearby aislings
        ///     and removes itself from the ObjectManager.
        /// </summary>
        public void Remove(bool update = false, bool delete = true)
        {
            if (Map != null)
            {
                Map.Update(X, Y, TileContent.None);
                ServerContext.GlobalMapCache[Map.ID].Update(X, Y, TileContent.None);
            }

            if (update)
                Show(Scope.NearbyAislingsExludingSelf, new ServerFormat0E(Serial));

            try
            {
                var objs = GetObjects(i => i.WithinRangeOf(this) && i.Target != null && i.Target.Serial == Serial, Get.Monsters | Get.Mundanes);
                for (int i = 0; i < objs.Length; i++)
                    objs[i].Target = null;
            }
            finally
            {
                if (delete)
                    DelObject(this);
            }
        }

        public bool HasSkill<T>(SkillScope scope) where T : Template, new()
        {
            var obj = new T();

            if (obj is SkillTemplate)
                if ((scope & SkillScope.Assail) == SkillScope.Assail)
                    return SkillBook.Get(i => i != null && i.Template != null
                                                        && i.Template.Type == SkillScope.Assail).Length > 0;

            return false;
        }


        /// <summary>
        ///     This Method will return all skills that are assail-like, Assail, Clobber, Ect.
        /// </summary>
        public Skill[] GetAssails(SkillScope scope)
        {
            if ((scope & SkillScope.Assail) == SkillScope.Assail)
                return SkillBook.Get(i => i != null && i.Template != null
                                                    && i.Template.Type == SkillScope.Assail).ToArray();

            return null;
        }

        public void UpdateStats()
        {
            Client?.SendStats(StatusFlags.All);
        }
    }
}