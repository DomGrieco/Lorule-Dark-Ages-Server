using Darkages.Network.ServerFormats;
using Darkages.Scripting;
using Darkages.Types;

namespace Darkages.Storage.locales.Scripts.Spells
{
    [Script("beag ioc", "Dean")]
    public class beagioc : SpellScript
    {
        public beagioc(Spell spell) : base(spell)
        {
        }

        public override void OnFailed(Sprite sprite, Sprite target)
        {
        }

        public override void OnSuccess(Sprite sprite, Sprite target)
        {
        }

        public override void OnUse(Sprite sprite, Sprite target)
        {
            if (target is Aisling && sprite is Aisling)
            {
                var client = (sprite as Aisling).Client;
                if (client.Aisling.CurrentMp >= Spell.Template.ManaCost)
                {
                    client.TrainSpell(Spell);

                    var action = new ServerFormat1A
                    {
                        Serial = target.Serial,
                        Number = 0x80,
                        Speed = 30
                    };

                    sprite.CurrentMp -= Spell.Template.ManaCost;
                    target.CurrentHp += (200 * ((Spell.Level + sprite.Wis) + 26));

                    if (target.CurrentHp > target.MaximumHp)
                        target.CurrentHp = target.MaximumHp;

                    if (client.Aisling.CurrentMp < 0)
                        client.Aisling.CurrentMp = 0;

                    if (target.CurrentHp > 0)
                    {
                        var hpbar = new ServerFormat13
                        {
                            Serial = target.Serial,
                            Health = (ushort)(100 * target.CurrentHp / target.MaximumHp),
                            Sound = 8
                        };
                        target.Show(Scope.NearbyAislings, hpbar);
                    }

                    sprite.Client.SendStats(StatusFlags.StructB);
                    target.Client.SendAnimation(0x04, target, client.Aisling);

                    client.Aisling.Show(Scope.NearbyAislings, action);
                    client.SendMessage(0x02, "you cast " + Spell.Template.Name + ".");
                    client.SendStats(StatusFlags.All);
                }
                else
                {
                    if (sprite is Aisling)
                    {
                        (sprite as Aisling).Client.SendMessage(0x02, ServerContext.Config.NoManaMessage);
                    }
                    return;

                }
            }
            else
            {
                var action = new ServerFormat1A
                {
                    Serial = sprite.Serial,
                    Number = 1,
                    Speed = 30
                };

                foreach (var s in sprite.AislingsNearby())
                {
                    if (s == null || !s.LoggedIn)
                        continue;

                    if (s.CurrentHp >= s.MaximumHp || s.Target != null)
                        continue;

                    s.CurrentHp = s.MaximumHp;

                    var hpbar = new ServerFormat13
                    {
                        Serial = s.Serial,
                        Health = (ushort)(100 * s.CurrentHp / s.MaximumHp),
                        Sound = 8
                    };

                    s.SendAnimation(0x04, s, sprite);
                    s.Show(Scope.NearbyAislings, hpbar);
                    s.Client.SendStats(StatusFlags.StructB);
                }
                sprite.Show(Scope.NearbyAislings, action);
            }
        }
    }
}

