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
        var itemCount = value.Split(dest, delimitersFieldDelimiter); // don't strip empty entries, we need the blanks for indexing
        return dest[..itemCount].ToArray(); //TODO: Can we do this any better?
    }
}