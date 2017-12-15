﻿using Darkages.Common;
using Darkages.Network.ClientFormats;
using Darkages.Network.ServerFormats;
using Darkages.Scripting;
using Darkages.Security;
using Darkages.Storage;
using Darkages.Types;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Darkages.Network.Game
{
    public partial class GameServer : NetworkServer<GameClient>
    {
        /// <summary>
        /// Activate Assails
        /// </summary>
        private static void ActivateAssails(GameClient client)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            if (client.IsDead())
                return;
            #endregion

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
            {
                client.Interupt();
                return;
            }

            if (!client.Aisling.HasSkill<SkillTemplate>(SkillScope.Assail)) return;
            foreach (var skill in client.Aisling.GetAssails(SkillScope.Assail))
            {
                if (skill == null) continue;
                if (!skill.CanUse()) continue;
                if (skill.Template == null) continue;
                if (skill.Script == null) continue;
                if (skill.InUse) continue;
                skill.InUse = true;
                skill.Script.OnUse(client.Aisling);
                if (skill.Template.Cooldown > 0)
                    skill.NextAvailableUse = DateTime.UtcNow.AddSeconds(skill.Template.Cooldown);
                skill.NextAvailableUse = DateTime.UtcNow.AddMilliseconds(1000);
                skill.InUse = false;
            }
        }

        /// <summary>
        /// CancelIfCasting - Use this Method to check and or abort if casting.
        /// </summary>
        public static void CancelIfCasting(GameClient client)
        {
            if (client.IsRefreshing || !client.Aisling.LoggedIn)
            {
                client.Aisling.ActiveSpellInfo = null;
                client.Aisling.IsCastingSpell = false;
                client.Send(new ServerFormat48());
                return;
            }

            client.Aisling.ActiveSpellInfo = null;
            client.Aisling.IsCastingSpell = false;
            client.Send(new ServerFormat48());
        }

        /// <summary>
        /// Enter Game
        /// </summary>
        private void EnterGame(GameClient client, ClientFormat10 format)
        {
            var redirect = ServerContext.GlobalRedirects.FirstOrDefault(o => o.Serial == format.ID);
            if (redirect == null)
            {
                ClientDisconnected(client);
                return;
            }

            var aisling = Clone(StorageManager.AislingBucket.Load(redirect.Name));

            if (aisling.Username.Equals("lol", StringComparison.InvariantCultureIgnoreCase))
            {
                aisling.Flags |= AislingFlags.GM;
            }


            if (aisling != null)
            {
                client.Aisling = aisling;
            }

            if (client.Aisling == null)
            {
                base.ClientDisconnected(client);
                return;
            }

            Console.WriteLine("[{0}] Connection Established to Game Server", client.Aisling.Username);

            client.Encryption.Parameters = new SecurityParameters(redirect.Seed, redirect.Salt);
            client.Server = this;
            lock (Generator.Random)
            {
                client.Aisling.Serial = Generator.GenerateNumber();
            }
            client.Aisling.Client = client;
            client.Aisling.LoggedIn = false;
            client.Aisling.LastLogged = new DateTime();

            client.Load();

            ServerContext.GlobalRedirects.Remove(redirect);
        }

        /// <summary>
        /// Leave Game
        /// </summary>
        private void LeaveGame(GameClient client, ClientFormat0B format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;
            #endregion

            if (format.Type == 0)
            {
                var redirect = new Redirect
                {
                    Serial = client.Serial,
                    Salt = client.Encryption.Parameters.Salt,
                    Seed = client.Encryption.Parameters.Seed,
                    Name = client.Aisling.Username,
                    Client = client,
                    Type = 2
                };

                client.Aisling.LoggedIn = false;

                ServerContext.GlobalRedirects.Add(redirect);
                client.Save();
                client.Aisling.Remove(true, true);


                client.Send(new ServerFormat03
                {
                    EndPoint = new IPEndPoint(Address, 2610),
                    Redirect = redirect
                });
                client.Send(new ServerFormat02(0x00, "\0"));
            }

            if (format.Type == 1)
            {
                client.Send(new ServerFormat4C());
            } else if (format.Type == 3)
            {
                client.LastSave = DateTime.UtcNow;
                client.Aisling.Remove();
            }
        }

        /// <summary>
        /// Request Map
        /// </summary>
        protected override void Format05Handler(GameClient client, ClientFormat05 format)
        {
            #region Sanity Checks

            #endregion

            if (client?.Aisling?.Map == null) return;

            if (client.ShouldUpdateMap)
            {
                new TaskFactory().StartNew(() =>
                {
                    SendMapData(client);
                });
            }
        }

        private static void SendMapData(GameClient client)
        {
            for (var i = 0; i < client.Aisling.Map.Rows; i++)
            {
                var response = new ServerFormat3C
                {
                    Line = (ushort)i,
                    Data = client.Aisling.Map.GetRowData(i)
                };
                client.Send(response);
            }

            client.Aisling.Map.OnLoaded();
            client.ShouldUpdateMap = false;
        }

        /// <summary>
        /// Sprite Walk
        /// </summary>
        protected override void Format06Handler(GameClient client, ClientFormat06 format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.IsRefreshing && !ServerContext.Config.LimitWalkingWhenRefreshing)
                return;

            if (ServerContext.Config.CancelCastingWhenWalking)
                CancelIfCasting(client);

            #endregion

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
            {
                client.SendLocation();
                client.UpdateDisplay();
                return;
            }

            if (!client.Aisling.Map.Ready)
                return;

            client.Aisling.Direction = format.Direction;

            if (client.Aisling.Walk())
            {
                client.Aisling.Map.Script?.OnStep(client);
            }
            else
            {
                if (ServerContext.Config.RefreshOnWalkCollision)
                    client.Refresh();
            }
        }

        /// <summary>
        /// Pickup Item / Gold (User Pressed B)
        /// </summary>
        protected override void Format07Handler(GameClient client, ClientFormat07 format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.IsDead())
                return;
            #endregion

            if (format.PickupType == 1 && format.Position.IsNextTo(client.Aisling.Position))
            {
                //get any item in front of user.
                var objectsInfront = client.Aisling.GetInfront(ServerContext.Config.AutoLootPickupDistance).ToArray();

                foreach (var obj in objectsInfront)
                {
                    if (obj?.CurrentMapId != client.Aisling.CurrentMapId)
                        continue;

                    if (obj is Money)
                    {
                        (obj as Money).GiveTo((obj as Money).Amount, client.Aisling);
                    }

                    if (obj is Item)
                    {
                        if ((obj as Item).GiveTo(client.Aisling))
                        {
                            obj.Remove<Item>();
                        }
                    }
                }
            }
            else
            {
                var objs = GetObjects(i => i.X == format.Position.X
                                           && i.Y == format.Position.Y, Get.Items | Get.Money);

                if (objs.Length <= 0) return;
                foreach (var obj in objs)
                {
                    if (obj?.CurrentMapId != client.Aisling.CurrentMapId)
                        continue;

                    if (!client.Aisling.WithinRangeOf(obj, ServerContext.Config.ClickLootDistance))
                        continue;

                    if (obj is Money)
                    {
                        (obj as Money).GiveTo((obj as Money).Amount, client.Aisling);
                    }

                    if (obj is Item)
                    {
                        if ((obj as Item).GiveTo(client.Aisling))
                        {
                            obj.Remove<Item>();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Use Item
        /// </summary>
        protected override void Format1CHandler(GameClient client, ClientFormat1C format)
        {
            #region Sanity Checks (alot can go wrong if you remove this)
            if (client == null || client.Aisling == null)
            {
                return;
            }

            if (client.Aisling.Map == null || !client.Aisling.Map.Ready)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.Aisling.Dead)
                return;
            #endregion

            var slot = format.Index;
            var item = client.Aisling.Inventory.Get(i => i != null && i.Slot == slot).FirstOrDefault();

            if (item == null)
                return;

            client.LastActivatedLost = slot;

            if (item.Template != null)
            {
                if (!string.IsNullOrEmpty(item.Template.ScriptName))
                    if (item.Script == null)
                        item.Script = ScriptManager.Load<ItemScript>(item.Template.ScriptName, item);


                if (item.Script == null)
                {
                    client.SendMessage(0x02, ServerContext.Config.CantUseThat);
                }
                else
                {
                    item.Script.OnUse(client.Aisling, slot);

                    if (item.Template.Flags.HasFlag(ItemFlags.Stackable))
                    {
                        //should we consume it?
                        if (item.Template.Flags.HasFlag(ItemFlags.Consumable))
                        {
                            var stack = item.Stacks - 1;

                            if (stack > 0)
                            {
                                //consume 1 unit, update stack and refresh item in inventory.
                                item.Stacks -= 1;
                                client.Aisling.Inventory.Set(item, false);

                                //send remove packet.
                                client.Send(new ServerFormat10(item.Slot));

                                //add it again with updated information.
                                client.Send(new ServerFormat0F(item));
                            }
                            else
                            {
                                //remove from inventory
                                client.Aisling.Inventory.Remove(item.Slot);
                                client.Send(new ServerFormat10(item.Slot));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Drop Item
        /// </summary>
        protected override void Format08Handler(GameClient client, ClientFormat08 format)
        {
            #region Sanity Checks (alot can go wrong if you remove this)
            if (client == null || client.Aisling == null)
            {
                return;
            }

            if (!client.Aisling.LoggedIn)
                return;

            if (client.Aisling.Map == null || !client.Aisling.Map.Ready)
                return;

            #endregion

            //do we have an item in this slot?
            var item = client.Aisling.Inventory.Get(i => i != null && i.Slot == format.ItemSlot).FirstOrDefault();
            if (item == null)
            {
                return;
            }

            //check if it's also a valid item?
            if (item.Template == null)
                return;


            //check if we can drop it?
            if (!item.Template.Flags.HasFlag(ItemFlags.Dropable))
            {
                client.SendMessage(Scope.Self, 0x02, ServerContext.Config.CantDropItemMsg);
                return;
            }

            var item_position = new Position(format.X, format.Y);

            if (client.Aisling.Position.DistanceFrom(item_position) > 2)
            {
                client.SendMessage(Scope.Self, 0x02, ServerContext.Config.CantDoThat);
                return;
            }

            //check position is available to drop.
            if (client.Aisling.Map.IsWall(client.Aisling, format.X, format.Y))
            {
                if (client.Aisling.X != format.X || client.Aisling.Y != format.Y)
                {
                    client.SendMessage(Scope.Self, 0x02, ServerContext.Config.CantDoThat);
                    return;
                }
            }

            //if this item a stackable item?
            if ((item.Template.Flags & ItemFlags.Stackable) == ItemFlags.Stackable)
            {
                //remaining?
                var remaining = item.Stacks - format.ItemAmount;

                if (remaining <= 0) //none remaining / remove the item from inventory.
                {
                    //remove from inventory
                    client.Aisling.Inventory.Remove(item.Slot);
                    client.Send(new ServerFormat10(item.Slot));

                    //release this item.
                    item.Release(client.Aisling, new Position(format.X, format.Y));

                    client.Aisling.CurrentWeight -= item.Template.Weight;

                    if (client.Aisling.CurrentWeight < 0)
                        client.Aisling.CurrentWeight = 0;

                    client.SendStats(StatusFlags.StructA);
                }
                else // some remain, update inventory item.
                {
                    //clone and release item
                    var nitem = Clone<Item>(item);
                    nitem.Stacks = (byte)format.ItemAmount;
                    nitem.Release(client.Aisling, new Position(format.X, format.Y));

                    item.Stacks = (byte)remaining;
                    client.Aisling.Inventory.Set(item, false);

                    //send remove packet.
                    client.Send(new ServerFormat10(item.Slot));

                    //add it again with updated information.
                    client.Send(new ServerFormat0F(item));
                }
            }
            else // not stackable.
            {
                //remove from inventory
                client.Aisling.Inventory.Remove(item.Slot);
                client.Send(new ServerFormat10(item.Slot));

                client.Aisling.CurrentWeight -= item.Template.Weight;

                if (client.Aisling.CurrentWeight < 0)
                    client.Aisling.CurrentWeight = 0;

                //release the item back to the world.
                item.Release(client.Aisling, new Position(format.X, format.Y));

                client.SendStats(StatusFlags.StructA);
            }
        }

        /// <summary>
        /// Log Out
        /// </summary>
        protected override void Format0BHandler(GameClient client, ClientFormat0B format)
        {
            LeaveGame(client, format);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="format"></param>
        protected override void Format0EHandler(GameClient client, ClientFormat0E format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;
            #endregion

            var response = new ServerFormat0D
            {
                Serial = client.Aisling.Serial,
                Type = format.Type,
                Text = ""
            };

            Aisling[] audience;

            switch (format.Type)
            {
                case 0x00:
                    response.Text = $"{client.Aisling.Username}: {format.Text}";
                    audience = client.GetObjects<Aisling>(n => client.Aisling.WithinRangeOf(n));
                    break;
                case 0x01:
                    response.Text = $"{client.Aisling.Username}! {format.Text}";
                    audience = client.GetObjects<Aisling>(n => client.Aisling.CurrentMapId == n.CurrentMapId);
                    break;
                case 0x02:
                    response.Text = format.Text;
                    audience = client.GetObjects<Aisling>(n => client.Aisling.WithinRangeOf(n));
                    break;
                default:
                    ClientDisconnected(client);
                    return;
            }

            client.Aisling.Show(Scope.DefinedAislings, response, audience);
        }

        /// <summary>
        /// Use Spell
        /// </summary>
        protected override void Format0FHandler(GameClient client, ClientFormat0F format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.IsDead())
                return;
            #endregion


            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
            {
                CancelIfCasting(client);
                return;
            }

            if (client.Aisling.ActiveSpellInfo != null)
            {
                client.Aisling.ActiveSpellInfo.Slot = format.Index;
                client.Aisling.ActiveSpellInfo.Target = format.Serial;
                client.Aisling.ActiveSpellInfo.Position = format.Point;

                var spell = client.Aisling.SpellBook.Get(i => i != null &&
                                                              i.Slot == client.Aisling.ActiveSpellInfo.Slot).FirstOrDefault();

                if (spell?.Script != null)
                {
                    client.Aisling.CastSpell(spell);
                    client.Aisling.IsCastingSpell = false;
                }

                client.Aisling.ActiveSpellInfo = null;
            }
            else
            {
                var spell = client.Aisling.SpellBook.Get(i =>
                    i != null
                    && i.Slot == format.Index).FirstOrDefault();

                if (spell?.Script == null)
                    return;
                if (spell.Template == null)
                    return;

                client.Aisling.IsCastingSpell = true;
                client.Aisling.CastSpell(spell);
                client.Aisling.IsCastingSpell = false;
            }
        }

        /// <summary>
        /// Enter Game
        /// </summary>
        protected override void Format10Handler(GameClient client, ClientFormat10 format)
        {
            #region Sanity Checks
            if (client == null)
                return;
            #endregion

            EnterGame(client, format);
        }

        /// <summary>
        /// Sprite Turn
        /// </summary>
        protected override void Format11Handler(GameClient client, ClientFormat11 format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.IsRefreshing && ServerContext.Config.DontTurnDuringRefresh)
                return;
            #endregion


            client.Aisling.Direction = format.Direction;

            client.Aisling.Show(Scope.NearbyAislings, new ServerFormat11
            {
                Direction = client.Aisling.Direction,
                Serial = client.Aisling.Serial
            });
        }

        /// <summary>
        /// SpaceBar
        /// </summary>
        protected override void Format13Handler(GameClient client, ClientFormat13 format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.IsDead())
                return;
            #endregion

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
            {
                client.Interupt();
                return;
            }

            ActivateAssails(client);
        }

        /// <summary>
        /// User List
        /// </summary>
        protected override void Format18Handler(GameClient client, ClientFormat18 format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.IsRefreshing)
                return;

            CancelIfCasting(client);
            #endregion

            client.Aisling.Show(Scope.Self, new ServerFormat36(client));
        }

        /// <summary>
        /// Emotions
        /// </summary>
        protected override void Format1DHandler(GameClient client, ClientFormat1D format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.IsRefreshing)
                return;

            CancelIfCasting(client);

            if (client.IsDead())
                return;
            #endregion

            var id = format.Number;

            if (id > 35)
                return;

            client.Aisling.Show(Scope.NearbyAislings,
                new ServerFormat1A(client.Aisling.Serial, (byte)(id + 9), 64));
        }

        /// <summary>
        /// Drop Gold
        /// </summary>
        protected override void Format24Handler(GameClient client, ClientFormat24 format)
        {
            if (client?.Aisling == null)
                return;

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
                return;

            if (client.Aisling.GoldPoints >= format.GoldAmount)
            {
                client.Aisling.GoldPoints -= format.GoldAmount;
                if (client.Aisling.GoldPoints <= 0)
                    client.Aisling.GoldPoints = 0;

                client.SendMessage(Scope.Self, 0x02, ServerContext.Config.YouDroppedGoldMsg);
                client.SendMessage(Scope.NearbyAislingsExludingSelf, 0x02, ServerContext.Config.UserDroppedGoldMsg.Replace("noname", client.Aisling.Username));
                Money.Create(client.Aisling, format.GoldAmount, new Position(format.X, format.Y));
                client.SendStats(StatusFlags.StructC);
            }
            else
            {
                client.SendMessage(0x02, ServerContext.Config.NotEnoughGoldToDropMsg);
            }
        }

        /// <summary>
        /// Get Profile
        /// </summary>
        protected override void Format2DHandler(GameClient client, ClientFormat2D format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;
            #endregion

            client.Send(new ServerFormat39(client.Aisling));
        }

        /// <summary>
        /// Grouping
        /// </summary>
        protected override void Format2EHandler(GameClient client, ClientFormat2E format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.IsRefreshing)
                return;

            CancelIfCasting(client);

            if (client.IsDead())
                return;
            #endregion

            if (format.Type != 0x02) return;
            //get aisling who i want to group, check if they are nearby.
            var player = GetObject<Aisling>(i => i.Username.ToLower() == format.Name
                                                 && i.WithinRangeOf(client.Aisling));

            if (player == null) return;
            {
                //does player have group open?
                if (player.PartyStatus != GroupStatus.AcceptingRequests)
                {
                    client.SendMessage(0x02, ServerContext.Config.GroupRequestDeclinedMsg.Replace("noname", player.Username));
                    return;
                }

                //is this user in another group already?, and I'm not already in it?
                if (player.GroupParty.Members.Count > 0
                    && !player.GroupParty.Has(client.Aisling))
                {
                    client.SendMessage(0x02, ServerContext.Config.GroupedAlreadyMsg.Replace("noname", player.Username));
                    return;
                }

                //is this a new group? or existing grop?
                var isNew = client.Aisling.GroupParty.Members.Count == 0;

                if (isNew)
                {
                    //add player to your group.
                    client.Aisling.GroupParty.Add(player, true); //leader is the creator.

                    //add you to players group.
                    player.GroupParty.Add(client.Aisling);
                }
                else
                {
                    //it's an existing group.
                    //am i the leader of this group?

                    var leader = client.Aisling.GroupParty.Members
                        .FirstOrDefault(i => i.Leader)
                        ?.Aisling;

                    //am i the leader?
                    if (leader != null && leader.Serial == client.Aisling.Serial)
                    {
                        //add player to group.
                        client.Aisling.GroupParty.Add(player);
                    }
                }

                //am i already in this players group?
                //if so, player is the leader, remove me.

                client.Aisling.GroupParty.Add(player);
            }
        }


        /// <summary>
        /// Moving Slot
        /// </summary>
        protected override void Format30Handler(GameClient client, ClientFormat30 format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.IsRefreshing)
                return;

            CancelIfCasting(client);

            if (client.IsDead())
                return;
            #endregion

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen || client.Aisling.IsCastingSpell)
            {
                client.Interupt();
                return;
            }
            switch (format.PaneType)
            {
                case Pane.Inventory:
                    {
                        if (format.MovingTo - 1 > client.Aisling.Inventory.Length)
                            return;
                        if (format.MovingFrom - 1 > client.Aisling.Inventory.Length)
                            return;
                        if (format.MovingTo - 1 < 0)
                            return;
                        if (format.MovingFrom - 1 < 0)
                            return;

                        var a = client.Aisling.Inventory.Remove(format.MovingFrom);
                        var b = client.Aisling.Inventory.Remove(format.MovingTo);

                        client.Send(new ServerFormat10(format.MovingFrom));
                        client.Send(new ServerFormat10(format.MovingTo));

                        if (a != null)
                        {
                            a.Slot = format.MovingTo;
                            client.Aisling.Inventory.Set(a, false);
                            client.Send(new ServerFormat0F(a));
                        }

                        if (b != null)
                        {
                            b.Slot = format.MovingFrom;
                            client.Aisling.Inventory.Set(b, false);
                            client.Send(new ServerFormat0F(b));
                        }
                        client.Save();
                    } break;
                case Pane.Skills:
                    {
                        if (format.MovingTo - 1 > client.Aisling.SkillBook.Length)
                            return;
                        if (format.MovingFrom - 1 > client.Aisling.SkillBook.Length)
                            return;
                        if (format.MovingTo - 1 < 0)
                            return;
                        if (format.MovingFrom - 1 < 0)
                            return;

                        var a = client.Aisling.SkillBook.Remove(format.MovingFrom);
                        var b = client.Aisling.SkillBook.Remove(format.MovingTo);
                        client.Send(new ServerFormat2D(format.MovingFrom));
                        client.Send(new ServerFormat2D(format.MovingTo));

                        if (a != null)
                        {
                            a.Slot = format.MovingTo;
                            client.Aisling.SkillBook.Set(a, false);
                            client.Send(new ServerFormat2C(a.Slot, a.Icon, a.Name));
                        }

                        if (b != null)
                        {
                            b.Slot = format.MovingFrom;
                            client.Aisling.SkillBook.Set(b, false);
                            client.Send(new ServerFormat2C(b.Slot, b.Icon, b.Name));
                        }
                        client.Save();
                    }
                    break;
                case Pane.Spells:
                    {
                        if (format.MovingTo - 1 > client.Aisling.SpellBook.Length)
                            return;
                        if (format.MovingFrom - 1 > client.Aisling.SpellBook.Length)
                            return;
                        if (format.MovingTo - 1 < 0)
                            return;
                        if (format.MovingFrom - 1 < 0)
                            return;

                        var a = client.Aisling.SpellBook.Remove(format.MovingFrom);
                        var b = client.Aisling.SpellBook.Remove(format.MovingTo);
                        client.Send(new ServerFormat18(format.MovingFrom));
                        client.Send(new ServerFormat18(format.MovingTo));

                        if (a != null)
                        {
                            a.Slot = format.MovingTo;
                            client.Aisling.SpellBook.Set(a, false);
                            client.Send(new ServerFormat17(a));
                        }

                        if (b != null)
                        {
                            b.Slot = format.MovingFrom;
                            client.Aisling.SpellBook.Set(b, false);
                            client.Send(new ServerFormat17(b));
                        }
                        client.Save();
                    } break;
                case Pane.Tools:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Refresh
        /// </summary>
        protected override void Format38Handler(GameClient client, ClientFormat38 format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.IsRefreshing)
                return;
            #endregion

            client.Refresh();
            client.LastClientRefresh = DateTime.Now;
        }

        /// <summary>
        /// Dialogs A
        /// </summary>
        protected override void Format39Handler(GameClient client, ClientFormat39 format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            CancelIfCasting(client);
            #endregion

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
            {
                client.Interupt();
                return;
            }


            var mundane = GetObject<Mundane>(i => i.Serial == format.Serial);

            mundane?.Script?.OnResponse(this, client, format.Step, format.Args);
        }

        /// <summary>
        /// Dialogs B
        /// </summary>
        protected override void Format3AHandler(GameClient client, ClientFormat3A format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            CancelIfCasting(client);
            #endregion

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
            {
                client.Interupt();
                return;
            }

            if (client.DlgSession != null)
            {
                client.DlgSession.Callback?.Invoke(this, client, format.Step, string.Empty);
            }
        }

        /// <summary>
        /// Use Skill
        /// </summary>
        protected override void Format3EHandler(GameClient client, ClientFormat3E format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            if (client.IsDead())
                return;
            #endregion

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
            {
                client.Interupt();
                return;
            }


            var skill = client.Aisling.SkillBook.Get(i => i.Slot == format.Index).FirstOrDefault();
            if (skill?.Template == null || skill.Script == null) return;
            if (skill.Template.Type == SkillScope.Assail)
            {
                ActivateAssails(client);
                return;
            }

            if (!skill.CanUse())
                return;

            skill.InUse = true;
            skill.Script.OnUse(client.Aisling);

            //define cooldown.
            if (skill.Template.Cooldown > 0)
                skill.NextAvailableUse = DateTime.UtcNow.AddSeconds(skill.Template.Cooldown);

            //no cooldown? aisling must wait atleast 450 to use it again.
            skill.NextAvailableUse = DateTime.UtcNow.AddMilliseconds(ServerContext.Config.GlobalBaseSkillDelay);

            skill.InUse = false;
        }

        /// <summary>
        /// Remove equipment
        /// </summary>
        protected override void Format44Handler(GameClient client, ClientFormat44 format)
        {
            #region Sanity Checks
            if (client == null || client.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            CancelIfCasting(client);

            if (client.Aisling.Dead)
                return;
            #endregion

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
                return;

            if (client.Aisling.EquipmentManager.Equipment.ContainsKey(format.Slot))
                client.Aisling.EquipmentManager?.RemoveFromExisting(format.Slot);
        }

        /// <summary>
        /// Mouse Click
        /// </summary>
        protected override void Format43Handler(GameClient client, ClientFormat43 format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            CancelIfCasting(client);
            #endregion

            if (format.Type == 1) // object clicked.
            {
                //only search for aislings/monsters/npcs for now.
                var obj = GetObject(i => i.Serial == format.Serial, Get.All);

                switch (obj)
                {
                    case null:
                        return;
                    case Aisling _:
                        client.Aisling.Show(Scope.Self, new ServerFormat34(obj as Aisling));
                        break;
                    case Monster _:
                        (obj as Monster)?.Script?.OnClick(client);
                        break;
                    case Mundane _:
                        (obj as Mundane)?.Script?.OnClick(this, client);
                        break;
                }
            }
        }
        
        /// <summary>
        /// Keep-Alive Ping Response
        /// </summary>
        protected override void Format45Handler(GameClient client, ClientFormat45 format)
        {
            client.LastPingResponse = DateTime.UtcNow;
            AutoSave(client);
        }
        
        /// <summary>
        /// Add Stat Point
        /// </summary>
        protected override void Format47Handler(GameClient client, ClientFormat47 format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.IsRefreshing)
                return;

            CancelIfCasting(client);
            #endregion

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
                return;

            var attribute = (Stat)format.Stat;

            if (client.Aisling.StatPoints == 0)
            {
                client.SendMessage(0x02, ServerContext.Config.CantDoThat);
                return;
            }

            if ((attribute & Stat.Str) == Stat.Str)
            {
                client.Aisling._Str++;
                client.SendMessage(0x02, ServerContext.Config.StrAddedMessage);
            }

            if ((attribute & Stat.Int) == Stat.Int)
            {
                client.Aisling._Int++;
                client.SendMessage(0x02, ServerContext.Config.IntAddedMessage);
            }

            if ((attribute & Stat.Wis) == Stat.Wis)
            {
                client.Aisling._Wis++;
                client.SendMessage(0x02, ServerContext.Config.WisAddedMessage);
            }

            if ((attribute & Stat.Con) == Stat.Con)
            {
                client.Aisling._Con++;
                client.SendMessage(0x02, ServerContext.Config.ConAddedMessage);
            }
            if ((attribute & Stat.Dex) == Stat.Dex)
            {
                client.Aisling._Dex++;
                client.SendMessage(0x02, ServerContext.Config.DexAddedMessage);
            }

            if (client.Aisling._Wis > ServerContext.Config.StatCap)
                client.Aisling._Wis = ServerContext.Config.StatCap;
            if (client.Aisling._Str > ServerContext.Config.StatCap)
                client.Aisling._Str = ServerContext.Config.StatCap;
            if (client.Aisling._Int > ServerContext.Config.StatCap)
                client.Aisling._Int = ServerContext.Config.StatCap;
            if (client.Aisling._Con > ServerContext.Config.StatCap)
                client.Aisling._Con = ServerContext.Config.StatCap;
            if (client.Aisling._Dex > ServerContext.Config.StatCap)
                client.Aisling._Dex = ServerContext.Config.StatCap;

            if (client.Aisling._Wis <= 0)
                client.Aisling._Wis = ServerContext.Config.StatCap;
            if (client.Aisling._Str <= 0)
                client.Aisling._Str = ServerContext.Config.StatCap;
            if (client.Aisling._Int <= 0)
                client.Aisling._Int = ServerContext.Config.StatCap;
            if (client.Aisling._Con <= 0)
                client.Aisling._Con = ServerContext.Config.StatCap;
            if (client.Aisling._Dex <= 0)
                client.Aisling._Dex = ServerContext.Config.StatCap;

            client.Aisling.StatPoints--;
            if (client.Aisling.StatPoints < 0)
                client.Aisling.StatPoints = 0;


            client.Aisling.MaximumWeight = (int)(client.Aisling.Str * 3.5);
            client.Aisling.Show(Scope.Self, new ServerFormat08(client.Aisling, StatusFlags.All));
        }
        
        /// <summary>
        /// Start Spell Cast
        /// </summary>
        protected override void Format4DHandler(GameClient client, ClientFormat4D format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.IsDead())
                return;
            #endregion

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
            {
                client.Interupt();
                return;
            }

            var lines = format.Lines;

            if (lines <= 0) return;

            if (client.Aisling.ActiveSpellInfo != null)
                client.Aisling.ActiveSpellInfo = null;

            client.Aisling.ActiveSpellInfo = new CastInfo
            {
                SpellLines = format.Lines,
                Started = DateTime.UtcNow
            };
            client.Aisling.IsCastingSpell = true;
        }

        /// <summary>
        /// Spell/Skill Chant Information
        /// </summary>
        protected override void Format4EHandler(GameClient client, ClientFormat4E format)
        {
            #region Sanity Checks
            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.IsDead())
                return;
            #endregion

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
            {
                client.Interupt();
                return;
            }



            var chant = format.Message;
            var subject = chant.IndexOf(" Lev", StringComparison.Ordinal);

            if (subject > 0)
            {
                var message = chant.Substring(subject, chant.Length - subject);
                client.Say(ServerContext.Config.ChantPrefix + chant.Replace(message, string.Empty).Trim() + ServerContext.Config.ChantSuffix, 0x02);
                return;
            }

            client.Say(chant, 0x02);
        }

        /// <summary>
        /// Profile Picture
        /// </summary>
        protected override void Format4FHandler(GameClient client, ClientFormat4F format)
        {
            client.Aisling.ProfileMessage = format.Words;
            client.Aisling.PictureData = format.Image;
        }

        /// <summary>
        /// Save
        /// </summary>
        protected override void Format75Handler(GameClient client, ClientFormat75 format)
        {
            AutoSave(client);
        }

        /// <summary>
        /// Meta data
        /// </summary>
        protected override void Format7BHandler(GameClient client, ClientFormat7B format)
        {
            if (format.Type == 0x00)
            {
                client.Send(new ServerFormat6F
                {
                    Type = 0x00,
                    Name = format.Name
                });
            }

            if (format.Type == 0x01)
            {
                client.Send(new ServerFormat6F
                {
                    Type = 0x01
                });
            }
        }        
    }
}