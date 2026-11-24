namespace FastHl7;

internal static class SplitHelper
{

    /// <summary>
    /// Splits a string into segments based on the HL7 segment delimiter.
    /// </summary>
    /// <param name="message">The HL7 message as a ReadOnlySpan of characters.</param>
    /// <returns>An array of segments.</returns>
    public static Range[] SplitSegments(ReadOnlySpan<char> message)
    {
        var segments = new List<Range>();
        var position = 0;
        while (TryGetNextSegment(message, ref position, out var segment))
        {
            segments.Add(segment);
        }

        return segments.ToArray();
    }

    public static Range[] Split(ReadOnlySpan<char> value, char delimitersFieldDelimiter)
    {
        var parts = new List<Range>();
        var start = 0;
        while (true)
        {
            var delimiterIndex = value[start..].IndexOf(delimitersFieldDelimiter);
            if (delimiterIndex < 0)
            {
                parts.Add(start..value.Length);
                break;
            }

            var end = start + delimiterIndex;
            parts.Add(start..end);
            start = end + 1;
        }

        return parts.ToArray();
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

    public static bool TryGetNextSegment(ReadOnlySpan<char> message, ref int position, out Range segment)
    {
        while (position < message.Length && (message[position] == '\r' || message[position] == '\n'))
        {
            position++;
        }

        if (position == message.Length)
        {
            segment = default;
            return false;
        }

        var start = position;
        while (position < message.Length && message[position] != '\r' && message[position] != '\n')
        {
            position++;
        }

        segment = start..position;
        return true;
    }

    public static int CountParts(ReadOnlySpan<char> value, char delimiter)
    {
        var count = 1;
        foreach (var character in value)
        {
            if (character == delimiter)
            {
                count++;
            }
        }

        return count;
    }

    public static bool TryGetPart(ReadOnlySpan<char> value, char delimiter, int index, out ReadOnlySpan<char> part)
    {
        if (index < 0)
        {
            part = default;
            return false;
        }

        var start = 0;
        while (index > 0)
        {
            var delimiterIndex = value[start..].IndexOf(delimiter);
            if (delimiterIndex < 0)
            {
                part = default;
                return false;
            }

            start += delimiterIndex + 1;
            index--;
        }

        var end = value[start..].IndexOf(delimiter);
        part = end < 0 ? value[start..] : value.Slice(start, end);
        return true;
    }

    /// <summary>
    /// Find the int value nestled between parens
    /// </summary>
    /// <param name="input"></param>
    /// <param name="value"></param>
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