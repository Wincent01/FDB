using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Fdb
{
    public class FdbFile : FdbData
    {
        public FdbFile(string path)
        {
            var reader = new BinaryReader(File.OpenRead(path));

            TableCount = reader.ReadUInt32();

            using (new FdbScope(reader))
            {
                TableHeader = new FdbTableHeader(reader, this);
            }
        }

        public uint TableCount { get; set; }

        public FdbTableHeader TableHeader { get; set; }

        public List<object> Structure { get; set; } = new List<object>();

        public void WriteObject(object obj)
        {
            Structure.Add(obj);
        }

        public byte[] Complete()
        {
            var fdb = new List<byte>();
            var pointers = new List<(FdbData, int)>();
            
            foreach (var obj in Structure)
            {
                switch (obj)
                {
                    case FdbColumnHeader header:
                        Console.WriteLine($"\nWriting {header.TableName.Value} ...");
                        break;
                    case FdbRowData _:
                        Console.Write('.');
                        break;
                }

                switch (obj)
                {
                    case null:
                        fdb.AddRange(ToBytes(-1));
                        break;
                    case FdbData data:
                    {
                        var pointer = pointers.Where(p => p.Item1 == data).ToArray();
                        if (pointer.Any())
                        {
                            var bytes = ToBytes(fdb.Count);

                            foreach (var tuple in pointer)
                            {
                                pointers.Remove(tuple);
                                for (var j = 0; j < 4; j++) fdb[tuple.Item2 + j] = bytes[j];
                            }
                        }
                        else
                        {
                            // Save pointers for last
                            pointers.Add((data, fdb.Count));

                            // Reserve space for pointer
                            fdb.AddRange(new byte[4]);
                        }

                        break;
                    }
                    default:
                        fdb.AddRange(ToBytes(obj));
                        break;
                }
            }

            return fdb.ToArray();
        }

        public static byte[] ToBytes(object obj)
        {
            var size = Marshal.SizeOf(obj);
            var buf = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);

            Marshal.StructureToPtr(obj, ptr, false);
            Marshal.Copy(ptr, buf, 0, size);

            return buf;
        }

        public override void Write(FdbFile writer)
        {
            writer.WriteObject(TableCount);
            writer.WriteObject(TableHeader);

            TableHeader.Write(writer);
        }
    }
}