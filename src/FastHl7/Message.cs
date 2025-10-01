using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnitTests")]
[assembly: InternalsVisibleTo("Benchmarks")]

namespace FastHl7;

/// <summary>
/// The top-level construct in this library, representing a full HL7 message.
/// You generally want to create this from a string containing the full message text, and then perform operations
/// on it to extract/query segments, fields, components etc.
/// </summary>
/// <remarks>
/// Note that this is a ref struct, and so is stack-only.  This is by design to avoid heap allocations and GC pressure.
/// 
/// If you need to hold onto a message for longer than the current stack frame, constructing a new <see cref="Message" /> from the
/// source string is so fast that you should probably store the original string instead.
/// </remarks>
[DebuggerDisplay("{MessageText}")]
public readonly ref struct Message
{
    private readonly Span<Range> _segments;
    private readonly Delimiters _delimiters;
/*
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message">This should be the only place we alloc a string in the lib!</param>
    public Message(string message)
    {
        MessageText = message;
        _delimiters = new(MessageText);
        _segments = SplitHelper.SplitSegments(MessageText);
    }
    */


    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    public Message(ReadOnlySpan<char> message)
    {
        MessageText = message;
        _delimiters = new(MessageText);
        _segments = SplitHelper.SplitSegments(MessageText);
    }

    /// <summary>
    /// The raw HL7 message content underlying this object.
    /// </summary>
    private ReadOnlySpan<char> MessageText { get; }

    /// <summary>
    /// Gets the segment at the requested 0-based index (MSH is effectively always 0)
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Segment GetSegment(int i)
    {
        if (_segments.Length <= i || i < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(i), "Segment index is out of range.");
        }

        return new(MessageText[_segments[i]], _delimiters);
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
        if (parenIndex >= 0 )
        {
            segmentName = name[..parenIndex];

            if (!SplitHelper.TryGetIntBetweenParens(name, out repeat))
            {
                throw new ArgumentOutOfRangeException(nameof(name),
                    "Invalid segment name format. Expected format is 'SEGMENT_NAME' or 'SEGMENT_NAME(index)'.");
            }
        }

        var foundCount = 0;
        foreach (var segment in _segments)
        {
            if (!MessageText[segment].StartsWith(segmentName, StringComparison.OrdinalIgnoreCase))
                continue; // not our segment name

            if (foundCount + 1 == repeat) // +1 so we get 1-based indexing
            {
                return new(MessageText[segment], _delimiters);
            }

            foundCount++;
        }

        return new(); // not found, return empty segment
    }
    
    
    /// <summary>
    /// This takes a dot-delimited query, and returns the raw message text for that query.
    /// eg, Query("MSH") will return the MSH segment text, while Query("PID(2).3.1") will return the 1st component of the third field of the 2nd PID segment.
    /// Fields are effectively 1-based indexing per HL7 spec.
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public ReadOnlySpan<char> Query(ReadOnlySpan<char> query)
    {
        if (query.IsEmpty)
        {
            return null;
        }
        
        Span<Range> queryParts = stackalloc Range[10];
        var queryPartsCount = SplitHelper.Split(query, '.', queryParts);

        if (queryPartsCount == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Query should have at least one part");
        }

        
        // The first part of the query is the segment, possibly with a repeat index
        
        var segment = GetSegment(query[queryParts[0]]);
        
        // just want the segment text, or we couldn't find the segment asked for
        if (queryPartsCount == 1 || !segment.HasValue) 
        {
            return segment.Value;
        }
        
        // Defer to the segment for the remainder of the query
        return segment.Query(query[queryParts[1].Start..]);
    }
    
}