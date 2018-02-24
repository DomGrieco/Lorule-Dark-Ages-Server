﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;
using Darkages.Compression;
using Darkages.IO;

namespace Darkages.Types
{
    public class MServerTable : CompressableObject
    {
        public MServerTable()
        {
            Servers = new Collection<MServer>();
        }

        public Collection<MServer> Servers { get; set; }

        [XmlIgnore] public ushort Size => (ushort) DeflatedData.Length;

        [XmlIgnore] public byte[] Data => DeflatedData;

        [XmlIgnore] public uint Hash { get; set; }

        public static MServerTable FromFile(string filename)
        {
            MServerTable result;

            using (var stream = File.OpenRead(filename))
            {
                result = new XmlSerializer(typeof(MServerTable)).Deserialize(stream) as MServerTable;
            }

            using (var stream = new MemoryStream())
            {
                result.Save(stream);
                result.InflatedData = stream.ToArray();
            }

            result.Hash = Crc32Provider.ComputeChecksum(result.InflatedData);
            result.Deflate();

            return result;
        }

        public override void Load(MemoryStream stream)
        {
            using (var reader = new BufferReader(stream))
            {
                var count = reader.ReadByte();

                for (var i = 0; i < count; i++)
                {
                    var server = new MServer();

                    server.Guid = reader.ReadByte();
                    server.Address = reader.ReadIPAddress();
                    server.Port = reader.ReadUInt16();

                    var text = reader.ReadString().Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);

                    server.Name = text[0];
                    server.Description = text[1];

                    var id = reader.ReadByte();

                    Servers.Add(server);
                }
            }
        }

        public override void Save(MemoryStream stream)
        {
            using (var writer = new BufferWriter(stream))
            {
                writer.Write(
                    (byte) Servers.Count);

                foreach (var server in Servers)
                {
                    writer.Write((byte)server.Guid);
                    writer.Write(server.Address);
                    writer.Write((ushort)server.Port);
                    writer.Write(server.Name + ";" + server.Description);
                    writer.Write((byte)server.ID);
                }
            }
        }
    }
}