﻿using Darkages.Types;
using System.Collections.Generic;

namespace Darkages.Network.ServerFormats
{
    public class ServerFormat07 : NetworkFormat
    {
        public override bool Secured
        {
            get { return true; }
        }
        public override byte Command
        {
            get { return 0x07; }
        }

        public ushort Count
        {
            get { return 0; }
        }

        private List<Sprite> Sprites;

        public ServerFormat07()
        {
            Sprites = new List<Sprite>(GetObjects(i => true, Get.Monsters | Get.Mundanes | Get.Items));
        }

        public ServerFormat07(Sprite[] objectsToAdd)
        {
            Sprites = new List<Sprite>(objectsToAdd);
        }

        public override void Serialize(NetworkPacketReader reader)
        {

        }

        public override void Serialize(NetworkPacketWriter writer)
        {
            writer.Write((ushort)Sprites.Count);

            foreach (var sprite in Sprites)
            {
                if (sprite is Money || sprite is Item)
                {
                    if (sprite is Money)
                    {
                        writer.Write((ushort)sprite.X);
                        writer.Write((ushort)sprite.Y);
                        writer.Write((uint)sprite.Serial);
                        writer.Write((ushort)(sprite as Money).Image);
                        writer.Write(byte.MinValue);
                        writer.Write(byte.MinValue);
                        writer.Write(byte.MinValue);
                    }
                    if (sprite is Item)
                    {
                        writer.Write((ushort)sprite.X);
                        writer.Write((ushort)sprite.Y);
                        writer.Write((uint)sprite.Serial);
                        writer.Write((ushort)(sprite as Item).DisplayImage);
                        writer.Write(byte.MinValue);
                        writer.Write(byte.MinValue);
                        writer.Write(byte.MinValue);
                    }
                }

                if (sprite is Monster)
                {
                    writer.Write((ushort)sprite.X);
                    writer.Write((ushort)sprite.Y);
                    writer.Write((uint)sprite.Serial);
                    writer.Write((ushort)(sprite as Monster).Image);
                    writer.Write((uint)0x0); // NFI
                    writer.Write((byte)sprite.Direction);
                    writer.Write(byte.MinValue); // NFI
                    writer.Write(byte.MinValue); // Tint
                }

                if (sprite is Mundane)
                {
                    writer.Write((ushort)sprite.X);
                    writer.Write((ushort)sprite.Y);
                    writer.Write((uint)sprite.Serial);
                    writer.Write((ushort)(sprite as Mundane).Template.Image);
                    writer.Write(uint.MinValue); // NFI
                    writer.Write((byte)sprite.Direction);
                    writer.Write((byte)byte.MinValue); // NFI

                    writer.Write((byte)0x02); // Type
                    writer.WriteStringA((sprite as Mundane).Template.Name);
                }              
            }
        }
    }
}
