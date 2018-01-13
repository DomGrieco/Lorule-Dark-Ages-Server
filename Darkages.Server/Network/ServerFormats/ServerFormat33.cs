﻿using Darkages.Network.Game;
using Darkages.Types;

namespace Darkages.Network.ServerFormats
{
    public class ServerFormat33 : NetworkFormat
    {
        public static int x = 0;

        public ServerFormat33(GameClient client, Aisling aisling)
        {
            Client = client;
            Aisling = aisling;
        }

        public override bool Secured => true;

        public override byte Command => 0x33;

        public Aisling Aisling { get; set; }
        public GameClient Client { get; set; }

        public override void Serialize(NetworkPacketReader reader)
        {
        }

        public override void Serialize(NetworkPacketWriter writer)
        {
            if (Aisling.Dead && !Client.CanSeeGhosts())
                return;

            if (Client.Aisling.Serial != Aisling.Serial)
                if (Aisling.Invisible && !Client.CanSeeHidden())
                    return;

            writer.Write((ushort) Aisling.X);
            writer.Write((ushort) Aisling.Y);
            writer.Write(Aisling.Direction);
            writer.Write((uint) Aisling.Serial);

            var displayFlag = Aisling.Gender == Gender.Male ? 0x10 : 0x20;

            if (Aisling.Dead)
                displayFlag += 0x20;
            else if (Aisling.Invisible)
                displayFlag += Aisling.Gender == Gender.Male ? 0x40 : 0x30;
            else
                displayFlag = Aisling.Gender == Gender.Male ? 0x10 : 0x20;


            if (displayFlag == 0x10)
                if (Aisling.Helmet > 100)
                    writer.Write((ushort) Aisling.Helmet);
                else
                    writer.Write((ushort) Aisling.HairStyle);
            else if (displayFlag == 0x20)
                if (Aisling.Helmet > 100)
                    writer.Write((ushort) Aisling.Helmet);
                else
                    writer.Write((ushort) Aisling.HairStyle);
            else
                writer.Write((ushort) 0x00);

            writer.Write((byte) (Aisling.Dead || Aisling.Invisible
                ? displayFlag
                : (byte) (Aisling.Display + Aisling.Pants)));


            if (!Aisling.Dead && !Aisling.Invisible)
            {
                writer.Write(Aisling.Armor);
                writer.Write(Aisling.Boots);
                writer.Write(Aisling.Armor);
                writer.Write(Aisling.Shield);
                writer.Write((byte) Aisling.Weapon);
                writer.Write(Aisling.HairColor);
                writer.Write(Aisling.BootColor);
                writer.Write((ushort) Aisling.HeadAccessory1);
                writer.Write((byte) 0);
                writer.Write((ushort) Aisling.HeadAccessory2);
                writer.Write((byte) 0);
                writer.Write(Aisling.Resting);
                writer.Write((ushort) Aisling.OverCoat);
            }
            else
            {
                writer.Write((ushort) 0);
                writer.Write((byte) 0);
                writer.Write((ushort) 0);
                writer.Write((byte) 0);
                writer.Write((byte) 0);
                writer.Write(Aisling.HairColor);
                writer.Write((byte) 0);
                writer.Write((ushort) 0);
                writer.Write((byte) 0);
                writer.Write((ushort) 0);
                writer.Write((byte) 0);
                writer.Write((byte) 0);
                writer.Write((ushort) 0);
            }

            if (Aisling.Map != null && Aisling.Map.Ready && Aisling.LoggedIn)
                writer.Write((byte) (Aisling.Map.Flags.HasFlag(MapFlags.PlayerKill) ? 1 : 0));
            else
                writer.Write((byte) 0);

            writer.WriteStringA(Aisling.Username ?? string.Empty);
            writer.WriteStringA(Aisling.GroupParty.LengthExcludingSelf > 0
                ? Aisling.GroupParty.Name ?? string.Empty
                : string.Empty);
        }
    }
}