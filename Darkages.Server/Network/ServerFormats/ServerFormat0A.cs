﻿namespace Darkages.Network.ServerFormats
{
    public class ServerFormat0A : NetworkFormat
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
                return 0x0A;
            }
        }

        public byte Type { get; set; }
        public string Text { get; set; }

        public ServerFormat0A(byte type, string text)
        {
            Type = type;
            Text = text;
        }

        public enum MsgType
        {
            Action = 2,
            Board = 10,
            Dialog = 9,
            Global = 3,
            Guild = 12,
            Message = 1,
            Party = 11,
            Whisper = 0,
        }

        public override void Serialize(NetworkPacketReader reader)
        {
        }
        public override void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(Type);
            writer.WriteStringB(Text);
        }
    }
}