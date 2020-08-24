using System;
using System.IO;
using System.Text;

namespace OnionPeeler.Tomtel
{
    public class Assembler
    {
        public static byte[] Assemble(string source)
        {
            using var result = new MemoryStream();
            using var writer = new BinaryWriter(result, Encoding.ASCII);

            using var reader = new StringReader(source);

            var line = reader.ReadLine();
            while (line != null)
            {
                // Strip comments
                var pComment = line.IndexOf('#');
                if (pComment != -1)
                {
                    line = line.Substring(0, pComment - 1);
                }

                // Split instructions
                var instructions = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var i in instructions)
                {
                    writer.Write(Convert.ToByte(i, 16));
                }
                line = reader.ReadLine();
            }

            return result.ToArray();
        }
    }
}
