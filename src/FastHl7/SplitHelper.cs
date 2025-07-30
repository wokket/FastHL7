namespace FastHl7;

public static class SplitHelper
{
    
    private static readonly string[] SegmentDelimiters = ["\r", "\n", "\r\n"];
    
    /// <summary>
    /// Splits a string into segments based on the HL7 segment delimiter.
    /// </summary>
    /// <param name="message">The HL7 message as a ReadOnlySpan of characters.</param>
    /// <returns>An array of segments.</returns>
    public static Range[] SplitSegments(ReadOnlySpan<char> message)
    {
        Span<Range> dest = stackalloc Range[1024];
        var segCount = message.SplitAny(dest, SegmentDelimiters, StringSplitOptions.RemoveEmptyEntries);
        return dest[..segCount].ToArray();
    }

    public static Range[] SplitFields(ReadOnlySpan<char> value, char delimitersFieldDelimiter)
    {
        Span<Range> dest = stackalloc Range[1024];
        var fieldCount = value.SplitAny(dest, new[] { delimitersFieldDelimiter }); // don't strip empty entries, we need the blanks for indexing
        return dest[..fieldCount].ToArray();
    }
}