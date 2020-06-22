using System.IO;

namespace OnionPeeler
{
    public static class StreamExtensions
    {
        public static ushort ReadUInt16BigEndian(this Stream stream)
        {
            var data = new byte[2];
            stream.Read(data);
            return (ushort)(data[0] << 8 | data[1]);
        }
    }
}
