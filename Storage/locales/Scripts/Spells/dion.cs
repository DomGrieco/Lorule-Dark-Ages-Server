﻿using Darkages.Network.ServerFormats;
using Darkages.Scripting;
using Darkages.Types;
using System;
using System.Linq;

namespace Darkages.Storage.locales.Scripts.Spells
{
    [Script("dion", "Dean")]
    public class dion : SpellScript
    {
        Random rand = new Random();

        public dion(Spell spell) : base(spell)
        {

        }

        public override void OnFailed(Sprite sprite, Sprite target)
        {
            if (sprite is Aisling)
            {
                var client = (sprite as Aisling).Client;
                client.SendMessage(0x02, "something went wrong.");
            }
        }

        public override void OnSuccess(Sprite sprite, Sprite target)
        {
            if (sprite is Aisling)
            {
                var client = (sprite as Aisling).Client;
                var buff   = Clone<Buff>(Spell.Template.Buff);

                if (sprite.Buffs.FirstOrDefault(i => i.Name == buff.Name) == null)
                {
                    buff.OnApplied(sprite, buff);

                    var action = new ServerFormat1A
                    {
                        Serial = client.Aisling.Serial,
                        Number = 0x80,
                        Speed = 30
                    };

                    client.SendAnimation(244, client.Aisling, client.Aisling);
                    client.Aisling.Show(Scope.NearbyAislings, action);
                    client.SendMessage(0x02, "you cast " + Spell.Template.Name + ".");
                    client.SendStats(StatusFlags.All);
                }
                else
                {
                    client.SendMessage(0x02, "Your skin is already like stone.");
                }
            }
        }

        public override void OnUse(Sprite sprite, Sprite target)
        {
            if (sprite is Aisling)
            {
                var client = (sprite as Aisling).Client;
                if (client.Aisling.CurrentMp >= Spell.Template.ManaCost)
                {
                    client.Aisling.CurrentMp -= Spell.Template.ManaCost;
                    if (client.Aisling.CurrentMp < 0)
                        client.Aisling.CurrentMp = 0;

                    if (rand.Next(1, 101) >= 25)
                    {
                        OnSuccess(sprite, target);
                    }
                    else
                    {
                        OnFailed(sprite, target);
                    }
                }
                else
                {
                    client.SendMessage(0x02, "your will is to weak.");
                }
                client.SendStats(StatusFlags.StructB);
            }
        }
    }
}
