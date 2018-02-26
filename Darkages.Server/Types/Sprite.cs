using Darkages.Common;
using Darkages.Network;
using Darkages.Network.Game;
using Darkages.Network.Object;
using Darkages.Network.ServerFormats;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using static Darkages.Types.ElementManager;
using static Darkages.Types.PrimaryStat;

namespace Darkages.Types
{
    public abstract class Sprite : ObjectManager
    {
        public readonly Random rnd = new Random();

        [JsonIgnore] public int[][] Directions =
        {
            new[] {+0, -1},
            new[] {+1, +0},
            new[] {+0, +1},
            new[] {-1, +0}
        };

        [JsonIgnore] public int[][] facingTable =
        {
            new[] {-1, +3, -1},
            new[] {+0, -1, +2},
            new[] {-1, +1, -1}
        };

        public Sprite()
        {
            if (this is Aisling)
                Content = TileContent.Aisling;
            if (this is Monster)
                Content = TileContent.Monster;
            if (this is Mundane)
                Content = TileContent.Mundane;
            if (this is Money)
                Content = TileContent.None;
            if (this is Item)
                Content = TileContent.None;

            Amplified = 0;

            Buffs = new ConcurrentDictionary<string, Buff>();
            Debuffs = new ConcurrentDictionary<string, Debuff>();
        }

        [JsonIgnore] public GameClient Client { get; set; }

        [JsonIgnore] public Area Map => ServerContext.GlobalMapCache[CurrentMapId] ?? null;

        [JsonIgnore] public TileContent Content { get; set; }

        public ConcurrentDictionary<string, Debuff> Debuffs { get; set; }

        public ConcurrentDictionary<string, Buff> Buffs { get; set; }

        public int Serial { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public byte Direction { get; set; }

        public int CurrentMapId { get; set; }

        public Element OffenseElement { get; set; }

        public Element DefenseElement { get; set; }

        public int Amplified { get; set; }

        public PrimaryStat MajorAttribute { get; set; }

        [JsonIgnore] public Sprite Target { get; set; }

        [JsonIgnore] public Position Position => new Position(X, Y);

        [JsonIgnore] public bool Attackable => this is Monster || this is Aisling || this is Mundane;

        [JsonIgnore] public DateTime AbandonedDate { get; set; }

        [JsonIgnore] public DateTime CreationDate { get; set; }

        [JsonIgnore] public DateTime LastUpdated { get; set; }

        public bool IsPrimaryStat()
        {
            var sums = new List<int>();
            {
                sums.Add(Str);
                sums.Add(Int);
                sums.Add(Wis);
                sums.Add(Con);
                sums.Add(Dex);
            }

            return sums.Max() == GetPrimaryAttribute();
        }

        public int GetPrimaryAttribute()
        {
            switch (MajorAttribute)
            {
                case PrimaryStat.STR:
                    return Str;
                case PrimaryStat.INT:
                    return Int;
                case PrimaryStat.WIS:
                    return Wis;
                case PrimaryStat.CON:
                    return Con;
                case PrimaryStat.DEX:
                    return Dex;
                default:
                    return 0;
            }
        }

        public bool HasBuff(string buff)
        {
            if (Buffs == null || Buffs.Count == 0)
                return false;

            return Buffs.ContainsKey(buff);
        }

        public bool HasDebuff(string debuff)
        {
            if (Debuffs == null || Debuffs.Count == 0)
                return false;

            return Debuffs.ContainsKey(debuff);
        }

        public bool RemoveBuff(string buff)
        {
            if (HasBuff(buff))
            {
                var buffobj = Buffs[buff];
                buffobj?.OnEnded(this, buffobj);

                return true;
            }

            return false;
        }
    
        public bool RemoveDebuff(string debuff)
        {
            if (HasDebuff(debuff))
            {
                var buffobj = Debuffs[debuff];
                buffobj?.OnEnded(this, buffobj);

                return true;
            }

            return false;
        }

        public int GetBaseDamage(Sprite target)
        {
            var formula = 0;

            if (this is Monster)
                formula = (int)((this as Monster).Template.Level * 3.58) * 4;

            if (this is Mundane)
                formula = (int)((this as Mundane).Template.Level * 3.58) * 4;

            return Math.Abs(formula) + 1;
        }

        public void RemoveAllBuffs()
        {
            foreach (var buff in Buffs)
                RemoveBuff(buff.Key);
        }

        public void RemoveAllDebuffs()
        {
            foreach (var debuff in Debuffs)
                RemoveDebuff(debuff.Key);
        }

        public void RemoveBuffsAndDebuffs()
        {
            RemoveAllBuffs();
            RemoveAllDebuffs();
        }

        public void ApplyDamage(Sprite source,
            int dmg,
            Element element,
            byte sound = 1)
        {
            var saved = source.OffenseElement;
            source.OffenseElement = element;
            ApplyDamage(source, dmg, false, sound, null);
            source.OffenseElement = saved;
        }

        public void ApplyDamage(Sprite Source, int dmg,
            bool truedamage = false,
            byte sound = 1,
            Action<int> dmgcb = null)
        {
            if (!WithinRangeOf(Source))
                return;

            if (!(this is Aisling))
            {
                if (AislingsNearby().Length == 0)
                    return;
            }


            if (!Attackable)
                return;

            if (!CanBeAttackedHere(Source))
                return;

            if (this is Monster)
            {
                (this as Monster)?.AppendTags(Source);
                (this as Monster)?.Script?.OnAttacked(Source?.Client);
            }


            if (Source is Aisling)
            {
                var client = Source as Aisling;
                if (client.EquipmentManager.Weapon != null
                    && client.EquipmentManager.Weapon.Item != null && client.Weapon > 0)
                {
                    var weapon = client.EquipmentManager.Weapon.Item;

                    lock (rnd)
                    {
                        dmg += rnd.Next(weapon.Template.DmgMin + 1, weapon.Template.DmgMax + 5) + client.BonusDmg * 10 / 100;
                    }
                }
            }

            if (this is Aisling)
            {
                var client = this as Aisling;
                if (client != null && client.DamageCounter++ % 2 == 0 && dmg > 0)
                    client.EquipmentManager.DecreaseDurability();
            }


            if (truedamage)
            {
                CurrentHp -= dmg;

                if (CurrentHp < 0)
                    CurrentHp = 0;
            }
            else
            {
                Target = Source;

                if (HasBuff("dion") || HasBuff("mor dion"))
                {
                    var empty = new ServerFormat13
                    {
                        Serial = Serial,
                        Health = byte.MaxValue,
                        Sound = sound
                    };

                    Show(Scope.VeryNearbyAislings, empty);
                }
                else
                {
                    if (HasDebuff("sleep"))
                        dmg <<= 1;

                    RemoveDebuff("sleep");


                    var amplifier = 1.00;

                    amplifier = CalcaluteElementalAmplifier(Source.OffenseElement, amplifier);
                    amplifier *= 
                          Amplified == 1 ? ServerContext.Config.FasNadurStrength    :
                          Amplified == 2 ? ServerContext.Config.MorFasNadurStrength : 1.00;

                    dmg = ComputeDmgFromAc(dmg);
                    dmg = CompleteDamageApplication(dmg, sound, dmgcb, amplifier);
                }
            }

            (this as Aisling)?.Client.SendStats(StatusFlags.StructB);
            (Source as Aisling)?.Client.SendStats(StatusFlags.StructB);
        }

        private double CalcaluteElementalAmplifier(Element element, double amplifier)
        {
            //Fire -> Wind
            if (element == Element.Fire)
            {
                if (DefenseElement == Element.Wind)
                    amplifier = 2.50;
                else
                    amplifier = 1.00;
            }

            //Wind -> Earth
            if (element == Element.Wind)
            {
                if (DefenseElement == Element.Earth)
                    amplifier = 2.50;
                else
                    amplifier = 1.00;
            }

            //Water -> Fire
            if (element == Element.Water)
            {
                if (DefenseElement == Element.Fire)
                    amplifier = 2.50;
                else
                    amplifier = 1.00;
            }

            //Earth -> Water
            if (element == Element.Earth)
            {
                if (DefenseElement == Element.Water)
                    amplifier = 2.50;
                else
                    amplifier = 1.00;
            }

            //Dark -> All
            if (element == Element.Dark)
            {
                if (DefenseElement != Element.Dark)
                    amplifier = 2.75;
                else
                    amplifier = 1.00;
            }

            //Light -> All
            if (element == Element.Light)
            {
                if (DefenseElement != Element.Light)
                    amplifier = 3.50;
                else
                    amplifier = 1.00;
            }


            //Counter
            if (element == DefenseElement)
            {
                if (DefenseElement == Element.None && element != Element.None)
                    amplifier = 2.50;
                else
                    amplifier = 1.00;
            }


            return amplifier;
        }

        private int CompleteDamageApplication(int dmg, byte sound, Action<int> dmgcb, double amplifier)
        {
            if (dmg <= 0)
                dmg = 1;

            if (CurrentHp > MaximumHp)
                CurrentHp = MaximumHp;

            var dealth = (int)(Math.Abs(dmg * amplifier));

            CurrentHp -= dealth;

            if (CurrentHp < 0)
                CurrentHp = 0;

            var hpbar = new ServerFormat13
            {
                Serial = Serial,
                Health = (ushort)((double)100 * CurrentHp / MaximumHp),
                Sound = sound
            };

            Show(Scope.VeryNearbyAislings, hpbar);
            {
                dmgcb?.Invoke(dealth);
            }
            return dmg;
        }

        /// <summary>
        ///     Checks the source of damage and if it's a player, check if the target is a player.
        ///     is true, checks weather or not damage can be applied on the map they are on both on.
        /// </summary>
        /// <param name="Source">Player applying damage.</param>
        /// <returns>true : false</returns>
        public bool CanBeAttackedHere(Sprite Source)
        {
            if (Source is Aisling && this is Aisling)
                if (CurrentMapId > 0 && ServerContext.GlobalMapCache.ContainsKey(CurrentMapId))
                    if (!ServerContext.GlobalMapCache[CurrentMapId].Flags.HasFlag(MapFlags.PlayerKill))
                        return false;

            return true;
        }

        /// <summary>
        ///     Sends Format With Target Scope.
        /// </summary>
        public void Show<T>(Scope op, T format, Sprite[] definer = null) where T : NetworkFormat
        {
            switch (op)
            {
                case Scope.Self:
                    Client?.Send(format);
                    break;
                case Scope.NearbyAislingsExludingSelf:
                    foreach (var gc in GetObjects<Aisling>(that => WithinRangeOf(that)))
                        if (gc.Serial != Serial)
                        {
                            if (this is Aisling)
                            {
                                if (!gc.Client.CanSeeHidden() && (this as Aisling).Invisible)
                                    if (format is ServerFormat33)
                                        return;

                                if (!gc.Client.CanSeeGhosts() && (this as Aisling).Dead)
                                    if (format is ServerFormat33)
                                        return;
                            }

                            gc.Client.Send(format);
                        }

                    break;
                case Scope.NearbyAislings:
                    foreach (var gc in GetObjects<Aisling>(that => WithinRangeOf(that)))
                    {
                        if (this is Aisling)
                        {
                            if (!gc.Client.CanSeeHidden() && (this as Aisling).Invisible)
                                if (format is ServerFormat33)
                                    return;

                            if (!gc.Client.CanSeeGhosts() && (this as Aisling).Dead)
                                if (format is ServerFormat33)
                                    return;
                        }

                        gc.Client.Send(format);
                    }

                    break;
                case Scope.VeryNearbyAislings:
                    foreach (var gc in GetObjects<Aisling>(that =>
                        WithinRangeOf(that, ServerContext.Config.VeryNearByProximity)))
                    {
                        if (this is Aisling)
                        {
                            if (!gc.Client.CanSeeHidden() && (this as Aisling).Invisible)
                                if (format is ServerFormat33)
                                    return;

                            if (!gc.Client.CanSeeGhosts() && (this as Aisling).Dead)
                                if (format is ServerFormat33)
                                    return;
                        }

                        gc.Client.Send(format);
                    }

                    break;
                case Scope.AislingsOnSameMap:
                    foreach (var gc in GetObjects<Aisling>(that => CurrentMapId == that.CurrentMapId))
                    {
                        if (this is Aisling)
                        {
                            if (!gc.Client.CanSeeHidden() && (this as Aisling).Invisible)
                                if (format is ServerFormat33)
                                    return;

                            if (!gc.Client.CanSeeGhosts() && (this as Aisling).Dead)
                                if (format is ServerFormat33)
                                    return;
                        }

                        gc.Client.Send(format);
                    }

                    break;
                case Scope.GroupMembers:
                {
                    if (this is Aisling)
                        foreach (var gc in GetObjects<Aisling>(that => (this as Aisling).GroupParty.Has(that)))
                        {
                            if (this is Aisling)
                            {
                                if (!gc.Client.CanSeeHidden() && (this as Aisling).Invisible)
                                    if (format is ServerFormat33)
                                        return;

                                if (!gc.Client.CanSeeGhosts() && (this as Aisling).Dead)
                                    if (format is ServerFormat33)
                                        return;
                            }

                            gc.Client.Send(format);
                        }
                }
                    break;
                case Scope.NearbyGroupMembersExcludingSelf:
                {
                    if (this is Aisling)
                        foreach (var gc in GetObjects<Aisling>(that =>
                            that.WithinRangeOf(this) && (this as Aisling).GroupParty.Has(that)))
                        {
                            if (this is Aisling)
                            {
                                if (!gc.Client.CanSeeHidden() && (this as Aisling).Invisible)
                                    if (format is ServerFormat33)
                                        return;

                                if (!gc.Client.CanSeeGhosts() && (this as Aisling).Dead)
                                    if (format is ServerFormat33)
                                        return;
                            }

                            gc.Client.Send(format);
                        }
                }
                    break;
                case Scope.NearbyGroupMembers:
                {
                    if (this is Aisling)
                        foreach (var gc in GetObjects<Aisling>(that =>
                            that.WithinRangeOf(this) && (this as Aisling).GroupParty.Has(that, true)))
                        {
                            if (this is Aisling)
                            {
                                if (!gc.Client.CanSeeHidden() && (this as Aisling).Invisible)
                                    if (format is ServerFormat33)
                                        return;

                                if (!gc.Client.CanSeeGhosts() && (this as Aisling).Dead)
                                    if (format is ServerFormat33)
                                        return;
                            }

                            gc.Client.Send(format);
                        }
                }
                    break;
                case Scope.DefinedAislings:
                    if (definer != null && definer.Length > 0)
                        foreach (var gc in definer)
                        {
                            if (this is Aisling)
                            {
                                if (!gc.Client.CanSeeHidden() && (this as Aisling).Invisible)
                                    if (format is ServerFormat33)
                                        return;

                                if (!gc.Client.CanSeeGhosts() && (this as Aisling).Dead)
                                    if (format is ServerFormat33)
                                        return;
                            }

                            (gc as Aisling).Client.Send(format);
                        }

                    break;
            }
        }

        private int ComputeDmgFromAc(int dmg)
        {
            var before = dmg;

            var hi = Ac + 95 - 100;
            var lo = Ac - 95 - 100;

            var accumulator = Math.Abs(hi) + Math.Abs(lo) / 10;
            dmg = dmg * accumulator / 100;

            return dmg;
        }

        public Sprite GetSprite(int x, int y)
        {
            return GetObject(i => i.X == x && i.Y == y, Get.All);
        }

        public Sprite[] GetSprites(int x, int y)
        {
            return GetObjects(i => i.X == x && i.Y == y, Get.All);
        }


        public List<Sprite> GetInfront(Sprite sprite, int tileCount = 1)
        {
            return _GetInfront(tileCount).Where(i => i != null && i.Serial != sprite.Serial).ToList();
        }

        public List<Sprite> GetInfront(int tileCount = 1)
        {
            //if (this is Aisling)
            //    return _GetInfront(tileCount).Intersect(
            //        (this as Aisling).ViewableObjects).ToList();

            return _GetInfront(tileCount).ToList();
        }


        private List<Sprite> _GetInfront(int tileCount = 1)
        {
            List<Sprite> results = new List<Sprite>();

            for (var i = 1; i <= tileCount; i++)
            {
                switch (Direction)
                {
                    case 0:
                        results.AddRange(GetSprites(X, Y - i));
                        break;
                    case 1:
                        results.AddRange(GetSprites(X + i, Y));
                        break;
                    case 2:
                        results.AddRange(GetSprites(X, Y + i));
                        break;
                    case 3:
                        results.AddRange(GetSprites(X - i, Y));
                        break;
                }
            }

            return results;
        }

        public void RemoveFrom(Aisling nearbyAisling)
        {
            if (nearbyAisling != null)
            {
                nearbyAisling.Show(Scope.Self, new ServerFormat0E(Serial));

                if (this is Item || this is Money)
                {
                    if (AislingsNearby().Length == 0 && BelongsTo(nearbyAisling))
                        AbandonedDate = DateTime.UtcNow;
                }
            }
        }

        public bool BelongsTo(Sprite subject)
        {
            if (this is Item)
            {
                if ((this as Item).AuthenticatedAislings.FirstOrDefault(i => i.Serial == subject.Serial) == null)
                {
                    return false;
                }
            }

            return true;
        }

        public void ShowTo(Aisling nearbyAisling)
        {
            if (nearbyAisling != null)
            {
                nearbyAisling.Show(Scope.Self, new ServerFormat07(new[] { this }));
            }
        }

        public bool WithinRangeOf(int x, int y, int distance)
        {
            var other = new Aisling();
            other.X = x;
            other.Y = y;
            other.CurrentMapId = CurrentMapId;
            return WithinRangeOf(other, distance);
        }

        public bool WithinRangeOf(Sprite other)
        {
            if (other == null)
                return false;

            if (CurrentMapId != other.CurrentMapId)
                return false;

            return WithinRangeOf(other, (int) ServerContext.Config.WithinRangeProximity);
        }

        public bool WithinRangeOf(Sprite other, int distance)
        {
            if (other == null)
                return false;

            var xDist = Math.Abs(X - other.X);
            var yDist = Math.Abs(Y - other.Y);

            if (xDist > distance ||
                yDist > distance)
                return false;

            if (CurrentMapId != other.CurrentMapId)
                return false;

            var dist = Extensions.Sqrt((float) (Math.Pow(xDist, 2) + Math.Pow(yDist, 2)));
            return dist <= distance;
        }

        public bool WithinRangeOf(int x, int y)
        {
            var xDist = Math.Abs(X - x);
            var yDist = Math.Abs(Y - y);

            if (xDist > ServerContext.Config.WithinRangeProximity ||
                yDist > ServerContext.Config.WithinRangeProximity)
                return false;

            var dist = Extensions.Sqrt((float) (Math.Pow(xDist, 2) + Math.Pow(yDist, 2)));
            return dist <= ServerContext.Config.WithinRangeProximity;
        }


        public bool Facing(int x, int y)
        {
            switch ((Direction) Direction)
            {
                case Types.Direction.North:
                    return X == x && Y - 1 == y;
                case Types.Direction.South:
                    return X == x && Y + 1 == y;
                case Types.Direction.East:
                    return X + 1 == x && Y == y;
                case Types.Direction.West:
                    return X - 1 == x && Y == y;
            }

            return false;
        }

        public bool Facing(Sprite other, out int direction)
        {
            return Facing(other.X, other.Y, out direction);
        }

        public bool Facing(int x, int y, out int direction)
        {
            var xDist = (x - X).Clamp(-1, +1);
            var yDist = (y - Y).Clamp(-1, +1);

            direction = facingTable[xDist + 1][yDist + 1];
            return Direction == direction;
        }


        public void Remove()
        {
            if (this is Monster)
                Remove<Monster>();

            if (this is Aisling)
                Remove<Aisling>();

            if (this is Money)
                Remove<Money>();

            if (this is Item)
                Remove<Item>();

            if (this is Mundane)
                Remove<Mundane>();
        }

        public Aisling[] AislingsNearby()
        {
            return GetObjects<Aisling>(i => i.WithinRangeOf(this));
        }

        public Monster[] MonstersNearby()
        {
            return GetObjects<Monster>(i => i.WithinRangeOf(this));
        }

        public Mundane[] MundanesNearby()
        {
            return GetObjects<Mundane>(i => i.WithinRangeOf(this));
        }


        /// <summary>
        ///     Use this to Remove Sprites
        ///     It will remove them from ingame to who those effected.
        ///     and invoke the objectmanager.
        /// </summary>
        public void Remove<T>() where T : Sprite, new()
        {
            var nearby = GetObjects<Aisling>(i => i.WithinRangeOf(this));
            var response = new ServerFormat0E(Serial);

            foreach (var o in nearby)
                o?.Client?.Send(response);

            if (this is Monster)
                DelObject(this as Monster);
            if (this is Aisling)
                DelObject(this as Aisling);
            if (this is Money)
                DelObject(this as Money);
            if (this is Item)
                DelObject(this as Item);
            if (this is Mundane)
                DelObject(this as Mundane);

            Map?.Update(X, Y, TileContent.None);
        }

        public void UpdateBuffs(TimeSpan elapsedTime)
        {
            Buff[] buff_Copy;

            lock (Buffs)
            {
                buff_Copy = new List<Buff>(Buffs.Values).ToArray();
            }

            for (var i = 0; i < buff_Copy.Length; i++)
                buff_Copy[i].Update(this, elapsedTime);
        }

        public void UpdateDebuffs(TimeSpan elapsedTime)
        {
            Debuff[] debuff_Copy;

            lock (Debuffs)
            {
                debuff_Copy = new List<Debuff>(Debuffs.Values).ToArray();
            }

            for (var i = 0; i < debuff_Copy.Length; i++)
                debuff_Copy[i].Update(this, elapsedTime);
        }

        /// <summary>
        ///     Show all nearby aislings, this sprite has turned.
        /// </summary>
        public virtual void Turn()
        {
            if (!CanUpdate())
                return;

            var nearby = GetObjects<Aisling>(i => i.WithinRangeOf(this));

            foreach (var o in nearby)
                o?.Client?.Send(new ServerFormat11
                {
                    Direction = Direction,
                    Serial = Serial
                });

            ServerContext.Game.ObjectPulseController?.OnObjectUpdate(this);
        }

        public void WalkTo(int x, int y)
        {
            if (!CanUpdate())
                return;

            try
            {
                var buffer = new byte[2];
                var length = float.PositiveInfinity;
                var offset = 0;

                for (byte i = 0; i < 4; i++)
                {
                    var newX = X + Directions[i][0];
                    var newY = Y + Directions[i][1];

                    if (newX == x &&
                        newY == y)
                        continue;

                    if (Map.IsWall(this, newX, newY))
                        continue;

                    var xDist = x - newX;
                    var yDist = y - newY;
                    var tDist = Extensions.Sqrt(xDist * xDist + yDist * yDist);

                    if (length < tDist)
                        continue;

                    if (length > tDist)
                    {
                        length = tDist;
                        offset = 0;
                    }

                    checked
                    {
                        buffer[offset] = i;
                    }

                    offset++;
                }

                if (offset == 0)
                    return;

                lock (rnd)
                {
                    Direction = buffer[rnd.Next(0, offset)];
                }

                if (!Walk())
                    return;

            }
            catch
            {
                // ignored
            }
        }

        public virtual void Wander()
        {
            if (!CanUpdate())
                return;

            var savedDirection = (byte)((object)(Direction));
            var update         = false;

            lock (rnd)
            {
                Direction = (byte) rnd.Next(0, 4);

                if (Direction != savedDirection)
                {
                    update = true;
                }
            }

            if (!Walk() && update)
            {
                Show(Scope.NearbyAislings, new ServerFormat11()
                {
                    Direction = this.Direction,
                    Serial = this.Serial
                });
            }
        }

        public bool CanUpdate()
        {
            if (IsSleeping || IsFrozen)
                return false;

            if (this is Monster || this is Mundane)
            {
                if (CurrentHp == 0)
                    return false;
            }

            return true;
        }

        public virtual bool Walk()
        {
            if (!CanUpdate())
                return false;

            var savedX = X;
            var savedY = Y;



            if (Direction == 0)
            {
                if (Map.IsWall(this, X, Y - 1))
                {
                    if (this is Aisling)
                    {
                        var obj = GetObject<Aisling>(i => i.X == X && i.Y == Y - 1);
                        if (obj != null && obj.Dead)
                        {
                            Y--;
                            goto step;
                        }

                        if (obj == null)
                        {
                            Y--;
                            goto step;
                        }
                    }

                    return false;
                }

                Y--;
            }

            if (Direction == 1)
            {
                if (Map.IsWall(this, X + 1, Y))
                {
                    if (this is Aisling)
                    {
                        var obj = GetObject<Aisling>(i => i.X == X + 1 && i.Y == Y);
                        if (obj != null && obj.Dead)
                        {
                            X++;
                            goto step;
                        }

                        if (obj == null)
                        {
                            X++;
                            goto step;
                        }
                    }

                    return false;
                }

                X++;
            }

            if (Direction == 2)
            {
                if (Map.IsWall(this, X, Y + 1))
                {
                    if (this is Aisling)
                    {
                        var obj = GetObject<Aisling>(i => i.X == X && i.Y == Y + 1);
                        if (obj != null && obj.Dead)
                        {
                            Y++;
                            goto step;
                        }

                        if (obj == null)
                        {
                            Y++;
                            goto step;
                        }
                    }

                    return false;
                }

                Y++;
            }

            if (Direction == 3)
            {
                if (Map.IsWall(this, X - 1, Y))
                {
                    if (this is Aisling)
                    {
                        var obj = GetObject<Aisling>(i => i.X == X - 1 && i.Y == Y);
                        if (obj != null && obj.Dead)
                        {
                            X--;
                            goto step;
                        }

                        if (obj == null)
                        {
                            X--;
                            goto step;
                        }
                    }

                    return false;
                }

                X--;
            }

            step:
            X = X.Clamp(X, Map.Cols - 1);
            Y = Y.Clamp(Y, Map.Rows - 1);


            if (Content != TileContent.Aisling)
            {
                var obj = AislingsNearby().OrderBy(i => 
                    Position.DistanceFrom(i.Position))
                    .FirstOrDefault();

                if (obj != null)
                {
                    if (obj.X == X && obj.Y == Y || savedX == X && savedY == Y)
                        return false;

                }
            }

            CompleteWalk(savedX, savedY);
            ServerContext.Game.ObjectPulseController?.OnObjectUpdate(this);

            return true;
        }

        private void CompleteWalk(int savedX, int savedY)
        {
            Map.Update(savedX, savedY, TileContent.None);
            Map.Update(X, Y, Content);

            if (this is Aisling)
            {
                Client.Send(new ServerFormat0B
                {
                    Direction = Direction,
                    LastX = (ushort) savedX,
                    LastY = (ushort) savedY
                });

                Client.Send(new ServerFormat32());
            }

            //create format to send to all nearby users.
            var response = new ServerFormat0C
            {
                Direction = Direction,
                Serial = Serial,
                X = (short) savedX,
                Y = (short) savedY
            };

            if (this is Monster)
            {
                var nearby = GetObjects<Aisling>(i => i.WithinRangeOf(this) && i.InsideView(this));
                if (nearby.Length > 0)
                    foreach (var obj in nearby)
                        obj.Show(Scope.Self, response, nearby);
            }

            if (this is Mundane)
            {
                var nearby = GetObjects<Aisling>(i => i.WithinRangeOf(this) && i.InsideView(this));
                if (nearby.Length > 0)
                    foreach (var obj in nearby)
                        obj.Show(Scope.Self, response, nearby);

            }


            if (this is Aisling)
            {
                Client.Aisling.Show(this is Aisling
                    ? Scope.NearbyAislingsExludingSelf
                    : Scope.NearbyAislings, response);
            }
        }

        #region Attributes

        public int CurrentHp { get; set; }

        public int CurrentMp { get; set; }

        public int _MaximumHp { get; set; }

        public int _MaximumMp { get; set; }

        [JsonIgnore] public int MaximumHp => _MaximumHp + BonusHp;

        [JsonIgnore] public int MaximumMp => _MaximumMp + BonusMp;

        public byte _Str { get; set; }

        public byte _Int { get; set; }

        public byte _Wis { get; set; }

        public byte _Con { get; set; }

        public byte _Dex { get; set; }

        public byte _Mr { get; set; }

        public byte _Dmg { get; set; }

        public byte _Hit { get; set; }

        [JsonIgnore] public byte Str => (byte) (_Str + BonusStr);

        [JsonIgnore] public byte Int => (byte) (_Int + BonusInt);

        [JsonIgnore] public byte Wis => (byte) (_Wis + BonusWis);

        [JsonIgnore] public byte Con => (byte) (_Con + BonusCon);

        [JsonIgnore] public byte Dex => (byte) (_Dex + BonusDex);

        [JsonIgnore] public int  Ac => BonusAc;

        [JsonIgnore] public byte Mr => (byte) (_Mr + BonusMr);

        [JsonIgnore] public byte Dmg => (byte) (_Dmg + BonusDmg);

        [JsonIgnore] public byte Hit => (byte) (_Hit + BonusHit);

        [JsonIgnore] public byte BonusStr { get; set; }

        [JsonIgnore] public byte BonusInt { get; set; }

        [JsonIgnore] public byte BonusWis { get; set; }

        [JsonIgnore] public byte BonusCon { get; set; }

        [JsonIgnore] public byte BonusDex { get; set; }

        [JsonIgnore] public byte BonusMr { get; set; }

        [JsonIgnore] public int BonusAc { get; set; }

        [JsonIgnore] public byte BonusHit { get; set; }

        [JsonIgnore] public byte BonusDmg { get; set; }

        [JsonIgnore] public int BonusHp { get; set; }

        [JsonIgnore] public int BonusMp { get; set; }

        #endregion

        #region Status

        [JsonIgnore] public bool IsSleeping => HasDebuff("sleep");

        [JsonIgnore] public bool IsFrozen => HasDebuff("frozen");

        [JsonIgnore] public bool IsPoisoned => HasDebuff("poison");

        [JsonIgnore] public bool IsBleeding => HasDebuff("bleeding");

        [JsonIgnore] public bool IsBlind => HasDebuff("blind");

        [JsonIgnore] public bool IsConfused => HasDebuff("confused");

        #endregion

        public void SendAnimation(ushort Animation, Sprite To, Sprite From, byte speed = 100)
        {
            var format = new ServerFormat29((uint)From.Serial, (uint)To.Serial, Animation, 0, speed);
            {
                Show(Scope.NearbyAislings, format);
            }
        }
    }
}