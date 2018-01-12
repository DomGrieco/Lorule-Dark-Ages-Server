﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using Darkages.Common;
using Darkages.Network.Game;
using Darkages.Network.Object;
using Darkages.Network.ServerFormats;
using Darkages.Security;
using static System.Threading.Tasks.Parallel;

namespace Darkages.Network
{
    [Serializable]
    public class NetworkClient : ObjectManager
    {
        public int Errors;
        private byte _lastFormat;
        private int _matches;
        private bool _sending;

        private readonly Queue<NetworkFormat> _sendBuffers = new Queue<NetworkFormat>();
        private readonly Throttler _throttler;

        public NetworkClient()
        {
            Reader = new NetworkPacketReader();
            Writer = new NetworkPacketWriter();
            Encryption = new SecurityProvider();

            _throttler = new Throttler(0x4096, TimeSpan.FromSeconds(1), 1024);
        }

        public NetworkPacketReader Reader { get; set; }
        public NetworkPacketWriter Writer { get; set; }
        public NetworkSocket Socket { get; set; }
        public SecurityProvider Encryption { get; set; }
        public byte Ordinal { get; set; }
        public int Serial { get; set; }

        private static byte P(NetworkPacket value)
        {
            return (byte)(value.Data[1] ^ (byte)(value.Data[0] - 0x2D));
        }

        private static void TransFormDialog(NetworkPacket value)
        {
            value.Data[2] ^= (byte)(P(value) + 0x72);
            value.Data[3] ^= (byte)(((byte)(P(value) + 0x72) + 1) % 256);
            value.Data[4] ^= (byte)(P(value) + 0x28);
            value.Data[5] ^= (byte)(((byte)(P(value) + 0x28) + 1) % 256);

            For(0, value.Data.Length - 6, i => value.Data[6 + i] ^= (byte)(((byte)(P(value) + 0x28) + i + 2) % 256));
        }


        public virtual void Read(NetworkPacket packet, NetworkFormat format)
        {
            if (format.Secured)
            {
                Encryption.Transform(packet);

                if (format.Command == 0x39 || format.Command == 0x3A)
                {
                    TransFormDialog(packet);
                    Reader.Position = 6;
                }
                else
                {
                    Reader.Position = 0;
                }
            }
            else
            {
                Reader.Position = -1;
            }

            if (ServerContext.Config?.LogRecvPackets ?? false)
                Console.WriteLine("r: {0}", packet);


            Reader.Packet = packet;
            format.Serialize(Reader);
        }

        public void SendAsync(NetworkFormat format)
        {
            lock (_sendBuffers)
            {
                _sendBuffers.Enqueue(format);

                if (_sending)
                    return;

                _sending = true;
                ThreadPool.QueueUserWorkItem(SendBuffers);
            }
        }

        private void SendBuffers(object state)
        {
            while (true)
            {
                NetworkFormat format;

                lock (_sendBuffers)
                {
                    if (_sendBuffers.Count == 0)
                    {
                        _sending = false;
                        return;
                    }

                    format = _sendBuffers.Dequeue();
                }

                SendFormat(format);
            }
        }

        public void SendPacket(byte[] data)
        {
            lock (Writer)
            {
                Writer.Position = 0;
                Writer.Write(data);

                var packet = Writer.ToPacket();
                {
                    Encryption.Transform(packet);
                    Socket.Send(packet.ToArray());
                }
            }
        }

        private void SendFormat(NetworkFormat format)
        {
            if (format == null)
                return;

            lock (Writer)
            {
                if (!GetPacket(format))
                    return;

                var packet = Writer.ToPacket();
                {
                    if (ServerContext.Config.LogSentPackets)
                        if (this is GameClient)
                            Console.WriteLine("{0}: {1}", (this as GameClient)?.Aisling?.Username, packet);

                    if (format.Secured)
                        Encryption.Transform(packet);

                    var buffer = packet.ToArray();

                    _throttler.ThrottledWait(buffer.Length);
                    Socket.BeginSend(buffer, 0, buffer.Length, 0, SendCallback, Socket);
                }
            }
        }

        private bool GetPacket(NetworkFormat format)
        {
            Writer.Position = 0;
            Writer.Write(format.Command);

            if (format.Secured)
                Writer.Write(Ordinal++);

            format.Serialize(Writer);

            if (_lastFormat == format.Command)
            {
                ++_matches;
            }
            else
            {
                _lastFormat = format.Command;
                _matches = 0;
            }

            return _matches < (format is ServerFormat3C
                             ? ServerContext.Config.PacketOverflowLimit
                             : ServerContext.Config.ServerOverflowTolerate);
        }

        private void SendCallback(IAsyncResult ar)
        {
            var client = (Socket)ar.AsyncState;
            {
                client.EndSend(ar);
            }
        }


        List<NetworkFormat> formatBatch = new List<NetworkFormat>();

        public void Send(NetworkFormat format)
        {
            SendAsync(format);
        }
    


        public void SendMessageBox(byte code, string text)
        {
            Send(new ServerFormat02(code, text));
        }
    }
}