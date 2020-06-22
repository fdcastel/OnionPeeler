using System;
using System.IO;

/// <summary>
/// C# implementation of ASCII85 encoding. 
/// Based on C code from http://www.stillhq.com/cgi-bin/cvsweb/ascii85/
/// </summary>
/// <remarks>
/// Jeff Atwood
/// http://www.codinghorror.com/blog/archives/000410.html
/// </remarks>
class Ascii85
{
    /// <summary>
    /// Prefix mark that identifies an encoded ASCII85 string, traditionally '<~'
    /// </summary>
    public const string PrefixMark = "<~";

    /// <summary>
    /// Suffix mark that identifies an encoded ASCII85 string, traditionally '~>'
    /// </summary>
    public const string SuffixMark = "~>";

    /// <summary>
    /// Maximum line length for encoded ASCII85 string; 
    /// set to zero for one unbroken line.
    /// </summary>
    public const int LineLength = 75;

    private const int _asciiOffset = 33;
    private byte[] _encodedBlock = new byte[5];
    private byte[] _decodedBlock = new byte[4];
    private uint _tuple = 0;

    private uint[] pow85 = { 85 * 85 * 85 * 85, 85 * 85 * 85, 85 * 85, 85, 1 };

    /// <summary>
    /// Decodes an ASCII85 encoded string into the original binary data
    /// </summary>
    /// <param name="s">ASCII85 encoded string</param>
    /// <returns>byte array of decoded binary data</returns>
    public byte[] Decode(ReadOnlySpan<char> s)
    {
        // strip prefix and suffix if present
        if (s.StartsWith(PrefixMark))
        {
            s = s.Slice(PrefixMark.Length);
        }
        if (s.EndsWith(SuffixMark))
        {
            s = s.Slice(0, s.Length - SuffixMark.Length);
        }

        MemoryStream ms = new MemoryStream();
        int count = 0;
        bool processChar;

        foreach (char c in s)
        {
            switch (c)
            {
                case 'z':
                    if (count != 0)
                    {
                        throw new Exception("The character 'z' is invalid inside an ASCII85 block.");
                    }
                    _decodedBlock[0] = 0;
                    _decodedBlock[1] = 0;
                    _decodedBlock[2] = 0;
                    _decodedBlock[3] = 0;
                    ms.Write(_decodedBlock, 0, _decodedBlock.Length);
                    processChar = false;
                    break;

                case '\n':
                case '\r':
                case '\t':
                case '\0':
                case '\f':
                case '\b':
                    processChar = false;
                    break;

                default:
                    if (c < '!' || c > 'u')
                    {
                        throw new Exception("Bad character '" + c + "' found. ASCII85 only allows characters '!' to 'u'.");
                    }
                    processChar = true;
                    break;
            }

            if (processChar)
            {
                _tuple += ((uint)(c - _asciiOffset) * pow85[count]);
                count++;
                if (count == _encodedBlock.Length)
                {
                    DecodeBlock(_decodedBlock.Length);
                    ms.Write(_decodedBlock, 0, _decodedBlock.Length);
                    _tuple = 0;
                    count = 0;
                }
            }
        }

        // if we have some bytes left over at the end..
        if (count != 0)
        {
            if (count == 1)
            {
                throw new Exception("The last block of ASCII85 data cannot be a single byte.");
            }

            count--;
            _tuple += pow85[count];
            DecodeBlock(count);
            for (int i = 0; i < count; i++)
            {
                ms.WriteByte(_decodedBlock[i]);
            }
        }

        return ms.ToArray();
    }

    private void DecodeBlock(int bytes)
    {
        for (int i = 0; i < bytes; i++)
        {
            _decodedBlock[i] = (byte)(_tuple >> 24 - (i * 8));
        }
    }
}