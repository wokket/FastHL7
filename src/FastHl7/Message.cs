namespace FastHl7;

public readonly ref struct Message
{
    private readonly Span<Range> _segments;
    private readonly Delimiters _delimiters;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message">This should be the only place we alloc a string in the lib!</param>
    public Message(string message)
    {
        MessageText = message;
        _delimiters = new(MessageText.AsSpan());

        _segments = SplitHelper.SplitSegments(MessageText);
    }

    /// <summary>
    /// The raw HL7 message content underlying this object.
    /// </summary>
    private string MessageText { get; }

    /// <summary>
    /// Gets the segment at the requested 0-based index (MSH is effectively always 0)
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public ReadOnlySpan<char> GetSegment(int i)
    {
        if (_segments.Length <= i || i < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(i), "Segment index is out of range.");
        }

        return MessageText.AsSpan()[_segments[i]];
    }

    /// <summary>
    /// Accessor to get a segment by name.
    /// If given the raw name (`PID`) it will return the first segment of that type.
    /// If given a repeat name (`PID(2)`) it will return that repeat segment, 1 based.
    /// </summary>
    /// <param name="name"></param>
    /// <returns>An empty span if not found</returns>
    public Segment GetSegment(ReadOnlySpan<char> name)
    {
        var segmentName = name;
        var repeat = 1; // default to 1, so PID(1) is the same as PID

        var parenIndex = name.IndexOf('(');
        if (parenIndex >= 0)
        {
            var lastIndex = name.IndexOf(')');
            if (lastIndex < 0 ||
                lastIndex <= parenIndex ||
                !int.TryParse(name.Slice(parenIndex + 1, lastIndex - parenIndex - 1), out repeat)
               )
            {
                throw new ArgumentOutOfRangeException(nameof(name),
                    "Invalid segment name format. Expected format is 'SEGMENT_NAME' or 'SEGMENT_NAME(index)'.");
            }

            segmentName = name[..parenIndex];
        }

        var foundCount = 0;
        foreach (var segment in _segments)
        {
            if (!MessageText.AsSpan()[segment].StartsWith(segmentName, StringComparison.OrdinalIgnoreCase))
                continue; // not our segment name

            if (foundCount + 1 == repeat) // +1 so we get 1-based indexing
            {
                return new Segment(MessageText.AsSpan()[segment], _delimiters);
            }

            foundCount++;
        }

        return new(); // not found, return empty segment
    }
}