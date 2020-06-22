﻿using System.IO;

namespace OnionPeeler
{
    /// <summary>
    /// https://en.wikipedia.org/wiki/IPv4#Header
    /// </summary>
    public class Ipv4Header
    {
        public byte Version;

        /// <summary>
        /// The IHL field contains the size of the IPv4 header, it has 4 bits that specify the number of 32-bit words in the header. (Wikipedia)
        /// </summary>
        public byte Ihl;

        public byte Dscp;
        public byte Ecn;

        /// <summary>
        /// This 16-bit field defines the entire packet size in bytes, including header and data. (Wikipedia)
        /// </summary>
        public ushort TotalLength;

        public ushort Identification;
        public byte Flags;
        public ushort FragmentOffset;
        public byte Ttl;
        public byte Protocol;
        public ushort HeaderChecksum;
        public uint SourceIpAddress;
        public uint DestinationIpAddress;

        public bool IsHeaderChecksumCorrect;

        public static Ipv4Header ReadFromStream(Stream stream)
        {
            // In this challenge all IPv4 headers are 20 bytes.

            // The fields in the header are packed with the most significant byte first (big endian). (Wikipedia)

            var version_ihl_dscp_ecn = stream.ReadUInt16BigEndian();
            var totalLength = stream.ReadUInt16BigEndian();
            var identification = stream.ReadUInt16BigEndian();
            var flags_fragmentOffset = stream.ReadUInt16BigEndian();
            var ttl_protocol = stream.ReadUInt16BigEndian();
            var headerChecksum = stream.ReadUInt16BigEndian();
            var sourceIpHi = stream.ReadUInt16BigEndian();
            var sourceIpLo = stream.ReadUInt16BigEndian();
            var destinationIpHi = stream.ReadUInt16BigEndian();
            var destinationIpLo = stream.ReadUInt16BigEndian();

            // https://en.wikipedia.org/wiki/IPv4_header_checksum
            var checksum = version_ihl_dscp_ecn +
                totalLength +
                identification +
                flags_fragmentOffset +
                ttl_protocol +
                headerChecksum +
                sourceIpHi +
                sourceIpLo +
                destinationIpHi +
                destinationIpLo;

            // For one's complement addition, each time a carry occurs, we must add a 1 to the sum. 
            checksum = (checksum & 0x_ffff) + (checksum >> 16);

            // If another carry is generated by the correction, another 1 is added to the sum.
            checksum = (checksum & 0x_ffff) + (checksum >> 16);

            return new Ipv4Header()
            {
                Version = (byte)(version_ihl_dscp_ecn >> 12),
                Ihl = (byte)(version_ihl_dscp_ecn >> 8 & 0b_0000_1111),

                Dscp = (byte)(version_ihl_dscp_ecn >> 2 & 0b_0011_1111),
                Ecn = (byte)(version_ihl_dscp_ecn & 0b_0000_0011),

                TotalLength = totalLength,

                Identification = identification,

                Flags = (byte)(flags_fragmentOffset >> 13),
                FragmentOffset = (ushort)(flags_fragmentOffset & 0b_0001_1111_1111_1111),

                Ttl = (byte)(ttl_protocol >> 8),
                Protocol = (byte)(ttl_protocol & 0b_1111_1111),

                HeaderChecksum = headerChecksum,

                SourceIpAddress = (uint)(sourceIpHi << 16 | sourceIpLo),

                DestinationIpAddress = (uint)(destinationIpHi << 16 | destinationIpLo),

                IsHeaderChecksumCorrect = (checksum == 0x_ffff)
            };
        }
    }
}
