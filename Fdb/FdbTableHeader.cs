using System.IO;

namespace Fdb
{
    public class FdbTableHeader : FdbData
    {
        public FdbTableHeader(BinaryReader reader, FdbFile file)
        {
            ColumnHeaders = new FdbColumnHeader[file.TableCount];
            RowTopHeaders = new FdbRowTopHeader[file.TableCount];

            for (var i = 0; i < file.TableCount; i++)
            {
                using (new FdbScope(reader))
                {
                    ColumnHeaders[i] = new FdbColumnHeader(reader);
                }

                using (new FdbScope(reader))
                {
                    RowTopHeaders[i] = new FdbRowTopHeader(reader);
                }
            }
        }

        public FdbColumnHeader[] ColumnHeaders { get; set; }

        public FdbRowTopHeader[] RowTopHeaders { get; set; }

        public override void Write(FdbFile writer)
        {
            writer.WriteObject(this);

            for (var i = 0; i < ColumnHeaders.Length; i++)
            {
                writer.WriteObject(ColumnHeaders[i]);
                writer.WriteObject(RowTopHeaders[i]);
            }

            for (var i = 0; i < ColumnHeaders.Length; i++)
            {
                ColumnHeaders[i].Write(writer);
                RowTopHeaders[i].Write(writer);
            }
        }
    }
}