﻿namespace Darkages.Network.ServerFormats
{
    public class ServerFormat34 : NetworkFormat
    {
        public ServerFormat34(Aisling aisling)
        {
            Aisling = aisling;
        }

        public override bool Secured => true;

        public override byte Command => 0x34;

        public Aisling Aisling { get; set; }

        public override void Serialize(NetworkPacketReader reader)
        {
        }


        public override void Serialize(NetworkPacketWriter writer)
        {
            writer.Write((uint) Aisling.Serial);

            BuildEquipment(writer);

            writer.Write((byte) Aisling.ActiveStatus);
            writer.WriteStringA(Aisling.Username);
            writer.Write(Aisling.Nation);
            writer.WriteStringA(Aisling.Stage.ToString());
            writer.Write((byte) Aisling.PartyStatus);

            writer.WriteStringA(Aisling.ClanTitle);
            writer.WriteStringA(Aisling.Path.ToString());
            writer.WriteStringA(string.Empty);


            writer.Write((byte) Aisling.LegendBook.LegendMarks.Count);
            foreach (var mark in Aisling.LegendBook.LegendMarks)
            {
                writer.Write(mark.Icon);
                writer.Write(mark.Color);
                writer.WriteStringA(mark.Category);
                writer.WriteStringA(mark.Value);
            }

            writer.Write((ushort) (Aisling.PictureData.Length + Aisling.ProfileMessage.Length + 4));
            writer.Write((ushort) Aisling.PictureData.Length);
            writer.Write(Aisling.PictureData ?? new byte[] {0x00});
            writer.WriteStringB(Aisling.ProfileMessage ?? string.Empty);
        }

        private void BuildEquipment(NetworkPacketWriter writer)
        {
            //1
            if (Aisling.EquipmentManager.Weapon != null)
            {
                writer.Write((ushort) Aisling.EquipmentManager.Weapon.Item.DisplayImage);
                writer.Write(Aisling.EquipmentManager.Weapon.Item.Color);
            }
            else
            {
                writer.Write(ushort.MinValue);
                writer.Write((byte) 0x00);
            }

            //2
            if (Aisling.EquipmentManager.Armor != null)
            {
                writer.Write((ushort) Aisling.EquipmentManager.Armor.Item.DisplayImage);
                writer.Write(Aisling.EquipmentManager.Armor.Item.Color);
            }
            else
            {
                writer.Write(ushort.MinValue);
                writer.Write((byte) 0x00);
            }

            //3
            if (Aisling.EquipmentManager.Shield != null)
            {
                writer.Write((ushort) Aisling.EquipmentManager.Shield.Item.DisplayImage);
                writer.Write(Aisling.EquipmentManager.Shield.Item.Color);
            }
            else
            {
                writer.Write(ushort.MinValue);
                writer.Write((byte) 0x00);
            }

            //4
            if (Aisling.EquipmentManager.DisplayHelm != null)
            {
                writer.Write((ushort) Aisling.EquipmentManager.DisplayHelm.Item.DisplayImage);
                writer.Write(Aisling.EquipmentManager.DisplayHelm.Item.Color);
            }
            else
            {
                writer.Write(ushort.MinValue);
                writer.Write((byte) 0x00);
            }

            //5
            if (Aisling.EquipmentManager.Earring != null)
            {
                writer.Write((ushort) Aisling.EquipmentManager.Earring.Item.DisplayImage);
                writer.Write(Aisling.EquipmentManager.Earring.Item.Color);
            }
            else
            {
                writer.Write(ushort.MinValue);
                writer.Write((byte) 0x00);
            }

            //6
            if (Aisling.EquipmentManager.Necklace != null)
            {
                writer.Write((ushort) Aisling.EquipmentManager.Necklace.Item.DisplayImage);
                writer.Write(Aisling.EquipmentManager.Necklace.Item.Color);
            }
            else
            {
                writer.Write(ushort.MinValue);
                writer.Write((byte) 0x00);
            }

            //7
            if (Aisling.EquipmentManager.LRing != null)
            {
                writer.Write((ushort) Aisling.EquipmentManager.LRing.Item.DisplayImage);
                writer.Write(Aisling.EquipmentManager.LRing.Item.Color);
            }
            else
            {
                writer.Write(ushort.MinValue);
                writer.Write((byte) 0x00);
            }

            //8
            if (Aisling.EquipmentManager.RRing != null)
            {
                writer.Write((ushort) Aisling.EquipmentManager.RRing.Item.DisplayImage);
                writer.Write(Aisling.EquipmentManager.RRing.Item.Color);
            }
            else
            {
                writer.Write(ushort.MinValue);
                writer.Write((byte) 0x00);
            }

            //9
            if (Aisling.EquipmentManager.LGauntlet != null)
            {
                writer.Write((ushort) Aisling.EquipmentManager.LGauntlet.Item.DisplayImage);
                writer.Write(Aisling.EquipmentManager.LGauntlet.Item.Color);
            }
            else
            {
                writer.Write(ushort.MinValue);
                writer.Write((byte) 0x00);
            }

            //10
            if (Aisling.EquipmentManager.RGauntlet != null)
            {
                writer.Write((ushort) Aisling.EquipmentManager.RGauntlet.Item.DisplayImage);
                writer.Write(Aisling.EquipmentManager.RGauntlet.Item.Color);
            }
            else
            {
                writer.Write(ushort.MinValue);
                writer.Write((byte) 0x00);
            }

            //11
            if (Aisling.EquipmentManager.Belt != null)
            {
                writer.Write((ushort) Aisling.EquipmentManager.Belt.Item.DisplayImage);
                writer.Write(Aisling.EquipmentManager.Belt.Item.Color);
            }
            else
            {
                writer.Write(ushort.MinValue);
                writer.Write((byte) 0x00);
            }

            //12
            if (Aisling.EquipmentManager.Greaves != null)
            {
                writer.Write((ushort) Aisling.EquipmentManager.Greaves.Item.DisplayImage);
                writer.Write(Aisling.EquipmentManager.Greaves.Item.Color);
            }
            else
            {
                writer.Write(ushort.MinValue);
                writer.Write((byte) 0x00);
            }

            //13
            if (Aisling.EquipmentManager.FirstAcc != null)
            {
                writer.Write((ushort) Aisling.EquipmentManager.FirstAcc.Item.DisplayImage);
                writer.Write(Aisling.EquipmentManager.FirstAcc.Item.Color);
            }
            else
            {
                writer.Write(ushort.MinValue);
                writer.Write((byte) 0x00);
            }

            if (Aisling.EquipmentManager.Boots != null)
            {
                writer.Write((ushort) Aisling.EquipmentManager.Boots.Item.DisplayImage);
                writer.Write(Aisling.EquipmentManager.Boots.Item.Color);
            }
            else
            {
                writer.Write(ushort.MinValue);
                writer.Write((byte) 0x00);
            }

            //15
            if (Aisling.EquipmentManager.Overcoat != null)
            {
                writer.Write((ushort) Aisling.EquipmentManager.Overcoat.Item.DisplayImage);
                writer.Write(Aisling.EquipmentManager.Overcoat.Item.Color);
            }
            else
            {
                writer.Write(ushort.MinValue);
                writer.Write((byte) 0x00);
            }

            //16
            if (Aisling.EquipmentManager.Helmet != null)
            {
                writer.Write((ushort) Aisling.EquipmentManager.Helmet.Item.DisplayImage);
                writer.Write(Aisling.EquipmentManager.Helmet.Item.Color);
            }
            else
            {
                writer.Write(ushort.MinValue);
                writer.Write((byte) 0x00);
            }

            //17
            if (Aisling.EquipmentManager.SecondAcc != null)
            {
                writer.Write((ushort) Aisling.EquipmentManager.SecondAcc.Item.DisplayImage);
                writer.Write(Aisling.EquipmentManager.SecondAcc.Item.Color);
            }
            else
            {
                writer.Write(ushort.MinValue);
                writer.Write((byte) 0x00);
            }
        }
    }
}