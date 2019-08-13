using System.IO;

namespace Fdb
{
    public class FdbRowInfo : FdbData
    {
        public FdbRowInfo(BinaryReader reader)
        {
            using (var s = new FdbScope(reader, true))
            {
                if (s) DataHeader = new FdbRowDataHeader(reader);
            }

            using (var s = new FdbScope(reader, true))
            {
                if (s) Linked = new FdbRowInfo(reader);
            }
        }

        public FdbRowDataHeader DataHeader { get; set; }

        public FdbRowInfo Linked { get; set; }

        public override void Write(FdbFile writer)
        {
            writer.WriteObject(this);
            writer.WriteObject(DataHeader);
            writer.WriteObject(Linked);

            DataHeader?.Write(writer);
            ;
            Linked?.Write(writer);
        }
    }
}