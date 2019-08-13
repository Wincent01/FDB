using System.IO;

namespace Fdb
{
    public class FdbRowHeader : FdbData
    {
        public FdbRowHeader(BinaryReader reader, FdbRowTopHeader rowTopHeader)
        {
            RowInfos = new FdbRowInfo[rowTopHeader.RowCount];

            for (var i = 0; i < rowTopHeader.RowCount; i++)
                using (var s = new FdbScope(reader, true))
                {
                    if (s) RowInfos[i] = new FdbRowInfo(reader);
                }
        }

        public FdbRowInfo[] RowInfos { get; set; }

        public override void Write(FdbFile writer)
        {
            writer.WriteObject(this);
            foreach (var rowInfo in RowInfos) writer.WriteObject(rowInfo);

            foreach (var rowInfo in RowInfos) rowInfo?.Write(writer);
        }
    }
}