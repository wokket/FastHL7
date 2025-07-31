using System.Text;

namespace FastHl7;

/// <summary>
/// Extensions for dealing with HL7 escape sequences (eg `\T\` etc), as well as embedded special chars (`\XC3A4\`) and html encoded chars (`\T\#162;`).
/// </summary>
/// <remarks>See also https://hl7.org.au/archive/hl7v2wg/1278287.html#Appendix1ParsingHL7v2(Informative)-6Dealingwithreservedcharactersanddelimiters and
/// https://docs.intersystems.com/latest/csp/docbook/DocBook.UI.Page.cls?KEY=EHL72_escape_sequences
/// </remarks>
public static class EscapeSequenceExtensions
{
    /// <summary>
    /// Gets whether this value contains any special chars that require de-escaping/decoding etc
    /// </summary>
    /// <remarks>This method does not allocate.</remarks>
    /// <param name="value"></param>
    /// <param name="delimiters"></param>
    /// <returns></returns>
    public static bool RequiresUnescaping(this ReadOnlySpan<char> value, Delimiters delimiters)
    {
        return !value.IsEmpty && value.Contains(delimiters.EscapeCharacter);
    }


    /// <summary>
    /// This decoder will replace some, **but not all** of the standard HL7 escape sequences.
    /// - `\E\`,`\F\`, '\R\`, `\S\`, `\T\` are all handled, and replaced with the Escape, Field, Repeat, Component and Sub-Component separator chars respectively
    /// - `\X..\` hexadecimal escape sequences are supported (2 hex digits per char)
    ///
    /// The following sequences are **NOT** replaced by design and will be left in the string:
    /// - `\H\` Indicates the start of highlighted text, this is a consuming application problem and will not be replaced.
    /// - `\N\` Indicates the end of highlighted text and resumption of normal text.  This is a consuming application problem and will not be replaced.
    /// - `\Z...\` Custom application escape sequences, these are custom (as are most `Z` items in HL7) and will not be replaced.
    ///
    /// Also, not all of the sequences that _should_ be replaced are currently being handled, specifically:
    /// /// - `\Cxxyy\`, '\Mxxyyzz\ arguably _should_ be handled, but aren't currently.  There's [some suggestion](https://confluence.hl7australia.com/display/OOADRM20181/Appendix+1+Parsing+HL7v2#Appendix1ParsingHL7v2-Unicodecharacters) that these are discouraged in lieu of html-escaped values
    ///
    /// </summary>
    /// <remarks>Note that this method has to allocate a new buffer for the content, so only call it if <see cref="RequiresUnescaping"/> returns true;</remarks>
    /// <param name="value"></param>
    /// <param name="delimiters"></param>
    /// <returns></returns>
    public static ReadOnlySpan<char> Unescape(this ReadOnlySpan<char> value, Delimiters delimiters)
    {
        // broadly based on both the Rust-HL7 and HL7V2 implementations

        if (!value.RequiresUnescaping(delimiters))
        {
            // TODO: Log that the caller should have called RequiresUnescaping first for efficiency??
            return value; // no escaping required, just return the original string
        }

        // we're going to be replacing (mainly) 3-char sequences with single characters, so this should be a reducing operation wrt the length of the content
        var sb = new StringBuilder(value.Length); // by using StringBuilder we don't need to track the output index
        var buffer = value; // the buffer we're working on for each iteration, we re-window this as we process

        while (true)
        {
            // get the next escape char
            var escapeIndex = buffer.IndexOf(delimiters.EscapeCharacter);

            // everything before the escape char is just copied as-is
            if (escapeIndex < 0) // no more escape chars found
            {
                sb.Append(buffer);
                break; // we're done
            }

            sb.Append(buffer[..escapeIndex]); // append the part before the escape character
            buffer = buffer[(escapeIndex + 1)..]; //and rebuild the buffer to start at the start of the escape sequence

            // we need to get the sequence between the current escape char and the next one (or the end of the string)
            var sequenceEndIndex = buffer.IndexOf(delimiters.EscapeCharacter);
            if (sequenceEndIndex < 0) // no more escape chars found, take the rest of the string
            {
                // We have an unterminated escape sequence which is against the spec, but we'll follow Postel's Law and just append the rest of the string
                sb.Append(buffer); // HL7V2 and Rust-Hl7 also just ignore this situation.... 
                break; // we're done
            }

            var sequence = buffer[..sequenceEndIndex]; // get the sequence between the escape characters

            switch (sequence)
            {
                case "F": // field separator
                    sb.Append(delimiters.FieldDelimiter);
                    break;
                case "S": // component separator
                    sb.Append(delimiters.ComponentDelimiter);
                    break;
                case "T": // subcomponent separator
                    sb.Append(delimiters.SubComponentDelimiter);
                    break;
                case "R": // repetition separator
                    sb.Append(delimiters.RepeatDelimiter);
                    break;
                case "E": // escape character
                    sb.Append(delimiters.EscapeCharacter);
                    break;
                case ".br": // TODO: Real?
                    sb.Append("<BR>");
                    break;

                default:

                    switch (sequence[0])
                    {
                        case 'X':
                            sb.Append(DecodeHexString(sequence[1..])); // decode hex string, skipping the 'X'
                            break;
                        case 'H' or 'N' or 'Z':
                            // These are left alone, not changed
                            sb.Append(delimiters.EscapeCharacter);
                            sb.Append(sequence);
                            sb.Append(delimiters.EscapeCharacter);
                            break;
                        default:
                            sb.Append(sequence);
                            break;
                    }

                    break;
            }

            buffer = buffer[(sequenceEndIndex + 1)..]; // move past the end of the sequence
        }

        return
            sb.ToString(); // TODO: IS there any way we can do this without allocating a new string?  Accept a buffer in as an overload ??
    }


    internal static ReadOnlySpan<char> DecodeHexString(ReadOnlySpan<char> input)
    {
        var byteCount = input.Length / 2;
        Span<byte> bytes = stackalloc byte[byteCount];

        for (var i = 0; i < byteCount; i++)
        {
            var hi = HexCharToInt(input[i * 2]);
            var lo = HexCharToInt(input[i * 2 + 1]);
            bytes[i] = (byte)((hi << 4) | lo);
        }

        if (bytes.Length == 1) //A2
        {
            return char.ConvertFromUtf32(bytes[0]);
        }

        if (bytes.Length == 2 && bytes[0] == 0) // 00A2
        {
            return char.ConvertFromUtf32(bytes[1]);
        }

        // we need a temp buffer for the UTF-8 conversion
        Span<char> chars = stackalloc char[bytes.Length]; // plenty
        var count = Encoding.UTF8.GetChars(bytes, chars);

        return count == 1
            ? chars[0].ToString()
            : // char ToString() is pretty optimised if we only have the one char
            chars[..count].ToArray();

        static int HexCharToInt(char c)
        {
            return c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'A' and <= 'F' => c - 'A' + 10,
                >= 'a' and <= 'f' => c - 'a' + 10,
                _ => throw new ArgumentException("Invalid hex character", nameof(input))
            };
        }
    }
}