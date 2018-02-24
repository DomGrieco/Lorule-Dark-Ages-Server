using System;
using System.Collections.Generic;
using Darkages.Network.Game;
using Darkages.Network.ServerFormats;
using Darkages.Scripting;
using Darkages.Types;

namespace Darkages.Storage.locales.Scripts.Mundanes
{
    [Script("Class Chooser")]
    public class ClassChooser : MundaneScript
    {
        public ClassChooser(GameServer server, Mundane mundane)
            : base(server, mundane)
        {
        }

        public override void OnGossip(GameServer server, GameClient client, string message)
        {
        }

        public override void TargetAcquired(Sprite Target)
        {
        }


        public override void OnClick(GameServer server, GameClient client)
        {
            if (client.Aisling.ClassID == 0)
            {
                var options = new List<OptionsDataItem>();
                options.Add(new OptionsDataItem(0x01, "Warrior"));
                options.Add(new OptionsDataItem(0x02, "Rogue"));
                options.Add(new OptionsDataItem(0x03, "Wizard"));
                options.Add(new OptionsDataItem(0x04, "Priest"));
                options.Add(new OptionsDataItem(0x05, "Monk"));

                client.SendOptionsDialog(Mundane, "What path will you walk?", options.ToArray());
            }
            else
            {
                client.SendOptionsDialog(Mundane, "You have already chosen your path.");
            }
        }

        public override void OnResponse(GameServer server, GameClient client, ushort responseID, string args)
        {
            if (responseID < 0x0001 ||
                responseID > 0x0005)
                return;

            client.Aisling.ClassID = responseID;
            client.Aisling.Path = (Class) responseID;

            client.SendOptionsDialog(Mundane, string.Format("You are now a {0}",
                Convert.ToString(client.Aisling.Path)));

            if (client.Aisling.Path == Class.Priest)
            {
                Spell.GiveTo(client.Aisling, "deo saighead");
                Spell.GiveTo(client.Aisling, "deo saighead lamh");
            }
            if (client.Aisling.Path == Class.Wizard)
            {
                Spell.GiveTo(client.Aisling, "fas nadur");
                Spell.GiveTo(client.Aisling, "beag srad");
                Spell.GiveTo(client.Aisling, "beag sal");
                Spell.GiveTo(client.Aisling, "mor strioch pian gar");
            }

            client.Aisling.LegendBook.AddLegend(new Legend.LegendItem
            {
                Category = "Class",
                Color = (byte)LegendColor.Blue,
                Icon = (byte)LegendIcon.Victory,
                Value = string.Format("Walks the path of the {0} ", Convert.ToString(client.Aisling.Path))
            });
        }
    }
}