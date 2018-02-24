﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darkages.Network.ServerFormats;
using Darkages.Scripting;
using Darkages.Types;

namespace Darkages.Storage.locales.Scripts.Spells
{
    [Script("Generic Elemental Mass", "Dean")]
    public class Generic_Elemental_Mass : SpellScript
    {
        private readonly Random rand = new Random();

        public Generic_Elemental_Mass(Spell spell) : base(spell)
        {
        }

        public override void OnFailed(Sprite sprite, Sprite target)
        {
            if (sprite is Aisling)
            {
                (sprite as Aisling)
                    .Client
                    .SendMessage(0x02, "Your spell has been deflected.");
                (sprite as Aisling)
                    .Client
                    .SendAnimation(33, target, sprite);
            }
            else
            {
                if (sprite is Monster)
                    (sprite.Target as Aisling)?
                        .Client
                        .SendAnimation(33, sprite, target);
            }
        }

        public override void OnSuccess(Sprite sprite, Sprite target)
        {
            if (sprite is Aisling)
            {
                var d = sprite.Int * Spell.Level / 100;
                var dmg = (int)(sprite.Int * d);

                target.ApplyDamage(sprite, dmg, Spell.Template.ElementalProperty, Spell.Template.Sound);
            }
            else
            {
                if (!(target is Aisling))
                    return;

                var client = (target as Aisling).Client;

                var dmg = (int)(sprite.Int * Spell.Template.DamageExponent * Spell.Level) * 100;
                target.ApplyDamage(sprite, dmg, Spell.Template.ElementalProperty, Spell.Template.Sound);

                (target as Aisling).Client
                    .SendMessage(0x02, string.Format("{0} Attacks you with {1}.",
                        (sprite is Monster
                            ? (sprite as Monster).Template.Name
                            : (sprite as Mundane).Template.Name) ?? "Monster",
                        Spell.Template.Name));

                client.SendAnimation(Spell.Template.Animation, target, sprite);

                var action = new ServerFormat1A
                {
                    Serial = sprite.Serial,
                    Number = 0x80,
                    Speed = 30
                };

                client.Aisling.Show(Scope.NearbyAislings, action);
            }
        }

        public override void OnUse(Sprite sprite, Sprite target)
        {
            if (sprite is Aisling)
            {
                if (sprite.CurrentMp - Spell.Template.ManaCost > 0)
                    sprite.CurrentMp -= Spell.Template.ManaCost;
                else
                {
                    if (sprite is Aisling)
                    {
                        (sprite as Aisling).Client.SendMessage(0x02, ServerContext.Config.NoManaMessage);
                    }
                    return;

                }

                if (sprite.CurrentMp < 0)
                {
                    sprite.CurrentMp = 0;
                }
                var targets = GetObjects(i => i.WithinRangeOf(sprite), Get.Aislings | Get.Monsters | Get.Mundanes);
                var client = (sprite as Aisling).Client;
                client.TrainSpell(Spell);

                foreach (var t in targets)
                {
                    if (t.Serial == sprite.Serial)
                        continue;

                    if (t.CurrentHp == 0)
                        continue;

                    client.SendAnimation(Spell.Template.Animation, t, sprite);

                    lock (rand)
                    {
                        if (rand.Next(0, 100) > t.Mr + (int)(t.Position.DistanceFrom(sprite.Position) * 10))
                        {
                            OnSuccess(sprite, t);

                            if (t is Aisling)
                                (t as Aisling).Client
                                    .SendMessage(0x02,
                                        string.Format("{0} Attacks you with {1}.", client.Aisling.Username,
                                            Spell.Template.Name));


                        }
                        else
                            OnFailed(sprite, t);
                    }

                    Thread.Yield();
                }

                client.SendMessage(0x02, string.Format("you cast {0}", Spell.Template.Name));

                var action = new ServerFormat1A
                {
                    Serial = sprite.Serial,
                    Number = 0x80,
                    Speed = 30
                };

                client.Aisling.Show(Scope.NearbyAislings, action);

                if (sprite is Aisling)
                    (sprite as Aisling)
                        .Client
                        .SendStats(StatusFlags.StructB);
            }
            else
            {
                var targets = GetObjects(i => i.WithinRangeOf(sprite), Get.Monsters);

                foreach (var t in targets)
                {
                    if (t.Serial == sprite.Serial)
                        continue;

                    if (t.CurrentHp == 0)
                        continue;


                    var dmg = (int)(sprite.Int * Spell.Template.DamageExponent) * (5 + Spell.Level * 10 / 100);

                    t.ApplyDamage(sprite, dmg, Spell.Template.ElementalProperty, Spell.Template.Sound);
                    t.SendAnimation(Spell.Template.Animation, t, sprite);
                    t.Target = sprite;


                    var action = new ServerFormat1A
                    {
                        Serial = sprite.Serial,
                        Number = 0x80,
                        Speed = 30
                    };


                    var hpbar = new ServerFormat13
                    {
                        Serial = t.Serial,
                        Health = (ushort)(100 * t.CurrentHp / t.MaximumHp),
                        Sound = Spell.Template.Sound
                    };

                    t.Show(Scope.NearbyAislings, hpbar);
                    sprite.Show(Scope.NearbyAislings, action);
                }
            }
        }
    }
}
