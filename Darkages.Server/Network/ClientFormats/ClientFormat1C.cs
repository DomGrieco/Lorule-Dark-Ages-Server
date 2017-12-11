﻿namespace Darkages.Network.ClientFormats
{
    public class ClientFormat1C : NetworkFormat
    {
        public override bool Secured
        {
            get { return true; }
        }
        public override byte Command
        {
            get { return 0x1C; }
        }

        public byte Index { get; private set; }

        public override void Serialize(NetworkPacketReader reader)
        {
            this.Index = reader.ReadByte();
        }
        public override void Serialize(NetworkPacketWriter writer)
        {
        }
    }
}