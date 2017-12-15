﻿using Darkages.Network.Game;
using Darkages.Types;

namespace Darkages.Network.ServerFormats
{
    public class ServerFormat33 : NetworkFormat
    {
        public override bool Secured
        {
            get
            {
                return true;
            }
        }
        public override byte Command
        {
            get
            {
                return 0x33;
            }
        }

        public Aisling Aisling { get; set; }
        public GameClient Client { get; set; }

        public ServerFormat33(GameClient client, Aisling aisling)
        {
            Client = client;
            Aisling = aisling;
        }

        public override void Serialize(NetworkPacketReader reader)
        {

        }

        public override void Serialize(NetworkPacketWriter writer)
        {
            if (Aisling.Dead && !Client.CanSeeGhosts())
                return;

            if (Client.Aisling.Serial != Aisling.Serial)
            {
                if (Aisling.Invisible && !Client.CanSeeHidden())
                    return;
            }

            writer.Write((ushort)Aisling.X);
            writer.Write((ushort)Aisling.Y);
            writer.Write((byte)Aisling.Direction);
            writer.Write((uint)Aisling.Serial);

            var displayFlag = ((Aisling.Gender == Gender.Male) ? 0x10 : 0x20);

            if (Aisling.Dead)
            {
                displayFlag += 0x20;
            }
            else if (Aisling.Invisible)
            {
                displayFlag += (Aisling.Gender == Gender.Male) ? 0x40 : 0x30;
            }
            else
            {
                displayFlag = (Aisling.Gender == Gender.Male) ? 0x10 : 0x20;
            }

            if (displayFlag == 0x10)
            {
                writer.Write((byte)0x00);

                if (Aisling.Helmet > 0)
                {
                    writer.Write((byte)Aisling.Helmet);
                }
                else
                {
                    writer.Write((byte)Aisling.HairStyle);
                }
            }
            else if (displayFlag == 0x20)
            {
                writer.Write((byte)0x00);

                if (Aisling.Helmet > 0)
                {
                    writer.Write((byte)Aisling.Helmet);
                }
                else
                {
                    writer.Write((byte)Aisling.HairStyle);
                }
            }
            else
            {
                writer.Write((byte)0x00);
                writer.Write((byte)0x00);

            }

            writer.Write((byte)(Aisling.Dead || Aisling.Invisible 
                ? displayFlag : (byte)(Aisling.Display + Aisling.Pants)));

            if (!Aisling.Dead && !Aisling.Invisible)
            {
                writer.Write((ushort)Aisling.Armor);
                writer.Write((byte)Aisling.Boots);
                writer.Write((ushort)Aisling.Armor);
                writer.Write((byte)Aisling.Shield);
                writer.Write((byte)Aisling.Weapon);
                writer.Write((short)Aisling.HairColor);
            }
            else
            {
                writer.Write((ushort)0x00);
                writer.Write((byte)0);
                writer.Write((ushort)0);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((byte)0);
            }
            writer.Write((byte)0);
            writer.Write((byte)Aisling.HeadAccessory1);
            writer.Write((byte)Aisling.Blind);

            writer.Write((byte)Aisling.HeadAccessory2);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);

            if (!Aisling.Dead)
            {
                writer.Write((byte)Aisling.OverCoat);
            }
            else
            {
                writer.Write((byte)0);
            }

            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);

            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);

            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.WriteStringA(Aisling.Username);
            writer.WriteStringA(string.Empty);
        }
    }
}