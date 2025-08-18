namespace FastHl7;

internal static class SplitHelper
{
    
    private static readonly string[] _segmentDelimiters = ["\r", "\n", "\r\n"];
    
    /// <summary>
    /// Splits a string into segments based on the HL7 segment delimiter.
    /// </summary>
    /// <param name="message">The HL7 message as a ReadOnlySpan of characters.</param>
    /// <returns>An array of segments.</returns>
    public static Range[] SplitSegments(ReadOnlySpan<char> message)
    {
        Span<Range> dest = stackalloc Range[1024];
        var segCount = message.SplitAny(dest, _segmentDelimiters, StringSplitOptions.RemoveEmptyEntries);
        return dest[..segCount].ToArray();
    }

    public static Range[] Split(ReadOnlySpan<char> value, char delimitersFieldDelimiter)
    {
        Span<Range> dest = stackalloc Range[1024];
        var itemCount = Split(value, delimitersFieldDelimiter, dest); // don't strip empty entries, we need the blanks for indexing
        return dest[..itemCount].ToArray();
    }
    
    /// <summary>
    /// Splits by delim into the dest, returning the item count in play
    /// </summary>
    /// <param name="value"></param>
    /// <param name="delimitersFieldDelimiter"></param>
    /// <param name="dest"></param>
    /// <returns></returns>
    public static int Split(ReadOnlySpan<char> value, char delimitersFieldDelimiter, Span<Range> dest)
    {
        return value.Split(dest, delimitersFieldDelimiter); // don't strip empty entries, we need the blanks for indexing
    }

    /// <summary>
    /// Find the int value nestled between parens
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool TryGetIntBetweenParens(ReadOnlySpan<char> input, out int value)
    {
        value = 0;
        var startIndex = input.IndexOf('(');
        if (startIndex < 0)
        {
            return false;
        }

        var endIndex = input.IndexOf(')');
        if (endIndex < 0 || endIndex <= startIndex + 1)
        {
            return false;
        }

        return int.TryParse(input.Slice(startIndex + 1, endIndex - startIndex - 1), out value);
    }
}