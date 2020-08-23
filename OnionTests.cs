using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using System.Text;

namespace OnionPeeler
{
    [TestClass]
    public class OnionTests
    {
        public const string DataFolder = "../../../data/";

        [TestMethod]
        public void DecodeLayer0()
        {
            var source = File.ReadAllText(DataFolder + "original.txt");
            var payload = ExtractPayload(source);
            var decoded = DecodeAscii85(payload);

            /* This payload has been encoded with Adobe-flavoured ASCII85. */

            const string outputFile = DataFolder + "layer1.txt";
            File.WriteAllBytes(outputFile, decoded);
            Assert.IsTrue(File.ReadAllText(outputFile).StartsWith("==["));
        }

        [TestMethod]
        public void DecodeLayer1()
        {
            var source = File.ReadAllText(DataFolder + "layer1.txt", Encoding.ASCII);
            var payload = ExtractPayload(source);
            var decoded = DecodeAscii85(payload);

            /* Apply the following operations to each byte of the payload:
             * 
             *   1. Flip every second bit
             *   2. Rotate the bits one position to the right
             */

            const string outputFile = DataFolder + "layer2.txt";
            using (var outputStream = new FileStream(outputFile, FileMode.Create))
            {
                const byte oddBits = 0b_1010_1010;
                const byte evenBits = 0b_0101_0101;

                foreach (var original in decoded)
                {
                    var flipped = (byte)(original ^ 0b_1111_1111);
                    var result = (byte)((original & oddBits) | (flipped & evenBits));
                    outputStream.WriteByte((byte)((result << 7) | (result >> 1)));
                }
            }
            Assert.IsTrue(File.ReadAllText(outputFile).StartsWith("==["));
        }

        [TestMethod]
        public void DecodeLayer2()
        {
            var source = File.ReadAllText(DataFolder + "layer2.txt");
            var payload = ExtractPayload(source);
            var decoded = DecodeAscii85(payload);

            /* For each byte of the payload, the seven most significant
             * bits carry data, and the least significant bit is the parity
             * bit. Combine the seven data bits from each byte where the
             * parity bit is correct, discarding bytes where the parity bit
             * is incorrect. 
             */

            using var parityStream = new MemoryStream();
            foreach (var original in decoded)
            {
                if (IsParityCorrect(original))
                {
                    parityStream.WriteByte(original);
                }
            }
            var bytesWritten = parityStream.Position;
            var parityData = parityStream.GetBuffer();

            const string outputFile = DataFolder + "layer3.txt";
            using (var outputStream = new FileStream(outputFile, FileMode.Create))
            {
                /* aaaaaaap bbbbbbbp cccccccp dddddddp eeeeeeep fffffffp gggggggp hhhhhhhp
                 * 
                 * aaaaaaab bbbbbbcc cccccddd ddddeeee eeefffff ffgggggg ghhhhhhh
                 */

                for (var i = 0; i < bytesWritten; i += 8)
                {
                    outputStream.WriteByte((byte)(((parityData[i + 0] << 0) & 0b_1111_1110) | (parityData[i + 1] >> 7)));
                    outputStream.WriteByte((byte)(((parityData[i + 1] << 1) & 0b_1111_1100) | (parityData[i + 2] >> 6)));
                    outputStream.WriteByte((byte)(((parityData[i + 2] << 2) & 0b_1111_1000) | (parityData[i + 3] >> 5)));
                    outputStream.WriteByte((byte)(((parityData[i + 3] << 3) & 0b_1111_0000) | (parityData[i + 4] >> 4)));
                    outputStream.WriteByte((byte)(((parityData[i + 4] << 4) & 0b_1110_0000) | (parityData[i + 5] >> 3)));
                    outputStream.WriteByte((byte)(((parityData[i + 5] << 5) & 0b_1100_0000) | (parityData[i + 6] >> 2)));
                    outputStream.WriteByte((byte)(((parityData[i + 6] << 6) & 0b_1000_0000) | (parityData[i + 7] >> 1)));
                }
            }
            Assert.IsTrue(File.ReadAllText(outputFile).StartsWith("==["));
        }

        [TestMethod]
        public void DecodeLayer3()
        {
            var source = File.ReadAllText(DataFolder + "layer3.txt");
            var payload = ExtractPayload(source);
            var decoded = DecodeAscii85(payload);

            /* The payload has been encrypted by XOR'ing each byte with a
             * secret, cycling key. The key is 32 bytes of random data,
             * which I'm not going to give you.
             */

            const string knownText =
              "==[ Layer 4/6: ????????????????????????????????=============\n\nWh";
            // 012345678901234567890123456789012345678901234567890123456789 0 123
            //           1         2         3         4         5          6

            var aKnownText = Encoding.ASCII.GetBytes(knownText);

            var aKey = new byte[32];
            aKey[0] = (byte)(decoded[0] ^ aKnownText[0]);
            aKey[1] = (byte)(decoded[1] ^ aKnownText[1]);
            aKey[2] = (byte)(decoded[2] ^ aKnownText[2]);
            aKey[3] = (byte)(decoded[3] ^ aKnownText[3]);
            aKey[4] = (byte)(decoded[4] ^ aKnownText[4]);
            aKey[5] = (byte)(decoded[5] ^ aKnownText[5]);
            aKey[6] = (byte)(decoded[6] ^ aKnownText[6]);
            aKey[7] = (byte)(decoded[7] ^ aKnownText[7]);
            aKey[8] = (byte)(decoded[8] ^ aKnownText[8]);
            aKey[9] = (byte)(decoded[9] ^ aKnownText[9]);
            aKey[10] = (byte)(decoded[10] ^ aKnownText[10]);
            aKey[11] = (byte)(decoded[11] ^ aKnownText[11]);
            aKey[12] = (byte)(decoded[12] ^ aKnownText[12]);
            aKey[13] = (byte)(decoded[13] ^ aKnownText[13]);
            aKey[14] = (byte)(decoded[14] ^ aKnownText[14]);

            aKey[15] = (byte)(decoded[47] ^ aKnownText[47]);
            aKey[16] = (byte)(decoded[48] ^ aKnownText[48]);
            aKey[17] = (byte)(decoded[49] ^ aKnownText[49]);
            aKey[18] = (byte)(decoded[50] ^ aKnownText[50]);
            aKey[19] = (byte)(decoded[51] ^ aKnownText[51]);
            aKey[20] = (byte)(decoded[52] ^ aKnownText[52]);
            aKey[21] = (byte)(decoded[53] ^ aKnownText[53]);
            aKey[22] = (byte)(decoded[54] ^ aKnownText[54]);
            aKey[23] = (byte)(decoded[55] ^ aKnownText[55]);
            aKey[24] = (byte)(decoded[56] ^ aKnownText[56]);
            aKey[25] = (byte)(decoded[57] ^ aKnownText[57]);
            aKey[26] = (byte)(decoded[58] ^ aKnownText[58]);
            aKey[27] = (byte)(decoded[59] ^ aKnownText[59]);
            aKey[28] = (byte)(decoded[60] ^ aKnownText[60]);
            aKey[29] = (byte)(decoded[61] ^ aKnownText[61]);
            aKey[30] = (byte)(decoded[62] ^ aKnownText[62]);
            aKey[31] = (byte)(decoded[63] ^ aKnownText[63]);

            const string outputFile = DataFolder + "layer4.txt";
            using (var outputStream = new FileStream(outputFile, FileMode.Create))
            {
                for (var i = 0; i < decoded.Length; i++)
                {
                    outputStream.WriteByte((byte)(decoded[i] ^ aKey[i % 32]));
                }
            }
            Assert.IsTrue(File.ReadAllText(outputFile).StartsWith("==["));
        }

        [TestMethod]
        public void DecodeLayer4()
        {
            var source = File.ReadAllText(DataFolder + "layer4.txt", Encoding.ASCII);
            var payload = ExtractPayload(source);
            var decoded = DecodeAscii85(payload);

            /* The payload for this layer is encoded as a stream of raw
             * network data, as if the solution was being received over the
             * internet. The data is a series of IPv4 packets with User
             * Datagram Protocol (UDP) inside. Extract the payload data
             * from inside each packet, and combine them together to form
             * the solution.
             * 
             * Each packet has three segments: the IPv4 header, the UDP
             * header, and the data section. So the first 20 bytes of the
             * payload will be the IPv4 header of the first packet. The
             * next 8 bytes will be the UDP header of the first packet.
             * This is followed by a variable-length data section for the
             * first packet. After the data section you will find the
             * second packet, starting with another 20 byte IPv4 header,
             * and so on.
             * 
             * However, the payload contains extra packets that are not
             * part of the solution. Discard these corrupted and irrelevant
             * packets when forming the solution.
             * 
             * Each valid packet of the solution has the following
             * properties. Discard packets that do not have all of these
             * properties.
             * 
             * - The packet was sent FROM any port of 10.1.1.10
             * - The packet was sent TO port 42069 of 10.1.1.200
             * - The IPv4 header checksum is correct
             * - The UDP header checksum is correct
             */

            using var ipv4Stream = new MemoryStream(decoded);

            const byte udp = 17;
            const uint sourceIp = 10 << 24 | 1 << 16 | 1 << 8 | 10;
            const uint destinationIp = 10 << 24 | 1 << 16 | 1 << 8 | 200;
            const ushort destinationPort = 42069;

            const string outputFile = DataFolder + "layer5.txt";
            using (var outputStream = new FileStream(outputFile, FileMode.Create))
            {
                while (ipv4Stream.Position != ipv4Stream.Length)
                {
                    // Read IPv4 packet
                    var ipv4Header = Ipv4Header.ReadFromStream(ipv4Stream);

                    var headerLength = ipv4Header.Ihl * 4;
                    var ipv4Data = new byte[ipv4Header.TotalLength - headerLength];
                    ipv4Stream.Read(ipv4Data);

                    if (ipv4Header.Protocol == udp &&
                        ipv4Header.IsHeaderChecksumCorrect &&
                        ipv4Header.SourceIpAddress == sourceIp &&
                        ipv4Header.DestinationIpAddress == destinationIp)
                    {
                        // Read UDP packet
                        using var udpStream = new MemoryStream(ipv4Data);

                        var udp_source_port = udpStream.ReadUInt16BigEndian();
                        var udp_destination_port = udpStream.ReadUInt16BigEndian();
                        var udp_length = udpStream.ReadUInt16BigEndian();
                        var udp_checksum = udpStream.ReadUInt16BigEndian();

                        // In this challenge all UDP headers are 8 bytes.
                        var udpData = new byte[udp_length - 8];
                        udpStream.Read(udpData);

                        /* The pseudo  header  conceptually prefixed to the UDP header contains the
                         * source  address,  the destination  address,  the protocol,  and the  UDP
                         * length.   This information gives protection against misrouted datagrams.
                         * This checksum procedure is the same as is used in TCP. (RFC 768)
                         */
                        var udpChecksum = (ushort)ipv4Header.SourceIpAddress +
                            (ushort)(ipv4Header.SourceIpAddress >> 16) +
                            (ushort)(ipv4Header.DestinationIpAddress) +
                            (ushort)(ipv4Header.DestinationIpAddress >> 16) +
                            ipv4Header.Protocol +
                            udp_length +
                            udp_source_port +
                            udp_destination_port +
                            udp_length;

                        for (var i = 0; i < udpData.Length; i += 2)
                        {
                            var hi = udpData[i];
                            var lo = i + 1 >= udpData.Length ? 0 : udpData[i + 1];

                            udpChecksum += (ushort)(hi << 8 | lo);
                        }
                        udpChecksum = (udpChecksum & 0x_ffff) + (udpChecksum >> 16);
                        udpChecksum = (udpChecksum & 0x_ffff) + (udpChecksum >> 16);

                        var isUdpChecksumCorrect = udp_checksum == (ushort)~udpChecksum;
                        if (isUdpChecksumCorrect &&
                            udp_destination_port == destinationPort)
                        {
                            outputStream.Write(udpData);
                        }
                    }
                }
            }
            Assert.IsTrue(File.ReadAllText(outputFile).StartsWith("==["));
        }

        [TestMethod]
        public void DecodeLayer5()
        {
            var source = File.ReadAllText(DataFolder + "layer5.txt", Encoding.ASCII);
            var payload = ExtractPayload(source);
            var decoded = DecodeAscii85(payload);

            /* The payload is structured like this:
             * 
             * - First 32 bytes: The 256-bit key encrypting key (KEK).
             * - Next 8 bytes: The 64-bit initialization vector (IV) for the wrapped key.
             * - Next 40 bytes: The wrapped (encrypted) key. When decrypted, this will become the 256-bit encryption key.
             * - Next 16 bytes: The 128-bit initialization vector (IV) for the encrypted payload.
             * - All remaining bytes: The encrypted payload.
             */

            using var payloadStream = new MemoryStream(decoded);
            using var payloadReader = new BinaryReader(payloadStream, Encoding.ASCII);

            var kek = payloadReader.ReadBytes(32);
            var ivWrappedKey = payloadReader.ReadBytes(8);
            var wrappedKey = payloadReader.ReadBytes(40);

            var ivEncryptedPayload = payloadReader.ReadBytes(16);
            var encryptedPayload = payloadReader.ReadBytes((int)(payloadReader.BaseStream.Length - payloadReader.BaseStream.Position));

            // Setup AES wrap engine
            var aesWrapEngine = new AesWrapEngine();
            aesWrapEngine.Init(false, new ParametersWithIV(new KeyParameter(kek), ivWrappedKey));
            var unwrappedKey = aesWrapEngine.Unwrap(wrappedKey, 0, wrappedKey.Length);

            // Setup AES cipher in CTR mode -- https://stackoverflow.com/a/38482749/33244
            var cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
            cipher.Init(false, new ParametersWithIV(new KeyParameter(unwrappedKey), ivEncryptedPayload));

            var clearText = new byte[cipher.GetOutputSize(encryptedPayload.Length)];
            var len = cipher.ProcessBytes(encryptedPayload, 0, encryptedPayload.Length, clearText, 0);
            cipher.DoFinal(clearText, len);

            const string outputFile = DataFolder + "layer6.txt";
            using (var outputStream = new FileStream(outputFile, FileMode.Create))
            {
                outputStream.Write(clearText);
            }
            Assert.IsTrue(File.ReadAllText(outputFile).StartsWith("==["));
        }

        private static ReadOnlySpan<char> ExtractPayload(ReadOnlySpan<char> content)
        {
            int begin = content.IndexOf(Ascii85.PrefixMark.AsSpan());
            int end = content.LastIndexOf(Ascii85.SuffixMark.AsSpan());
            return content.Slice(begin, end - begin + Ascii85.SuffixMark.Length);
        }

        private static byte[] DecodeAscii85(ReadOnlySpan<char> payload)
        {
            var ascii85 = new Ascii85();
            return ascii85.Decode(payload);
        }

        private static bool IsParityCorrect(byte value)
        {
            var parityBit = value & 1;

            var bitsSet = 0;
            bitsSet += (value >> 1) & 1;
            bitsSet += (value >> 2) & 1;
            bitsSet += (value >> 3) & 1;
            bitsSet += (value >> 4) & 1;
            bitsSet += (value >> 5) & 1;
            bitsSet += (value >> 6) & 1;
            bitsSet += (value >> 7) & 1;

            return (bitsSet + parityBit) % 2 == 0;
        }
    }
}
