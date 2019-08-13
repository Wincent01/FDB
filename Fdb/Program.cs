using System.IO;

namespace Fdb
{
    internal static class FdbProgram
    {
        private static void Main(string[] args)
        {
            var fdb = new FdbFile(args[0]);

            fdb.Write(fdb);

            if (!File.Exists("./cdclient.fdb")) File.Create("./cdclient.fdb").Dispose();

            var bytes = fdb.Complete();

            File.WriteAllBytes("./cdclient.fdb", bytes);
        }
    }
}