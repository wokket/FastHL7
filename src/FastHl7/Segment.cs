using System.Diagnostics;

namespace FastHl7;

/// <summary>
/// Represents a single Segment(line) of a HL7 message.
/// You normally don't construct these yourself as a consumer of the library, but get access via the <see cref="Message"/> type.
/// </summary>
[DebuggerDisplay("{Value}")]
public readonly ref struct Segment
{
    private readonly Delimiters _delimiters;

    internal Segment(ReadOnlySpan<char> value, Delimiters delimiters)
    {
        Value = value;
        _delimiters = delimiters;
    }

    /// <summary>
    /// 
    /// </summary>
    public bool HasValue => !Value.IsEmpty;

    /// <summary>
    /// Gets the name (eg MSH) of this segment
    /// </summary>
    public ReadOnlySpan<char> Name => Value[..3];

    /// <summary>
    /// Gets the raw text content of this segment.
    /// </summary>
    public ReadOnlySpan<char> Value { get; }

    /// <summary>
    /// Gets the number of (possibly empty) fields in this segment
    /// </summary>
    public int FieldCount
    {
        get
        {
            var fieldCount = SplitHelper.CountParts(Value, _delimiters.FieldDelimiter);
            if (Name.Equals("MSH", StringComparison.OrdinalIgnoreCase))
            {
                // MSH is a special cat, field 1 is the field delim char, and field 2 the remainder of the encoding chars
                return fieldCount + 1; // offset by one
            }
            return fieldCount;
        }
    }

    /// <summary>
    /// Gets the field at the given index.  Index 0 is always the Segment name (eg MSH).
    /// If this field has multiple repeats, _all_ the repeats are in the returned field.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Field GetField(int i)
    {
        if (i < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(i), "Field index is out of range.");
        }
        
        if (Name.Equals("MSH", StringComparison.OrdinalIgnoreCase))
        {
            if (i == 1)
            {
                return new(Value.Slice(3, 1), _delimiters);
            }

            if (i >= 2)
            {
                if (!SplitHelper.TryGetPart(Value, _delimiters.FieldDelimiter, i - 1, out var mshField))
                {
                    throw new ArgumentOutOfRangeException(nameof(i), "Field index is out of range.");
                }

                return new(mshField, _delimiters);
            }
        }

        if (!SplitHelper.TryGetPart(Value, _delimiters.FieldDelimiter, i, out var field))
        {
            throw new ArgumentOutOfRangeException(nameof(i), "Field index is out of range.");
        }

        return new(field, _delimiters);
        
    }

    /// <summary>
    /// Gets the field at the given index.  Index 0 is always the Segment name (eg MSH).
    /// The returned field _only_ contains the information from the requested repeat(1-i based index), and no others.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="repeat">1-based index</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private Field GetField(int index, int repeat)
    {
        if (repeat <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(repeat), "Repeat must be positive");
        }

        var fieldValue = GetField(index).Value;
        if (!SplitHelper.TryGetPart(fieldValue, _delimiters.RepeatDelimiter, repeat - 1, out var repeatValue))
        {
            throw new ArgumentOutOfRangeException(nameof(repeat), "Asked for repeat field doesn't have");
        }

        return new(repeatValue, _delimiters);
    }

    /// <summary>
    /// This takes a dot-delimited query, and returns the raw message text for that query.
    /// eg "0" returns the content of the segment name, while "2.3" returns the content of the third component of the 2nd real field in the segment.
    /// </summary>
    public ReadOnlySpan<char> Query(ReadOnlySpan<char> query)
    {
        if (query.IsEmpty)
        {
            return null;
        }


        var separatorIndex = query.IndexOf('.');
        var fieldQuery = separatorIndex < 0 ? query : query[..separatorIndex];

        // first part has to be the field index (possibly with a repeat index??) 
        var repeatIndex = -1;
        int fieldIndex;
        var parenIndex = fieldQuery.IndexOf('(');
        if (parenIndex >= 0)
        {
            // we have a request for a repeat.
            if (!SplitHelper.TryGetIntBetweenParens(fieldQuery, out repeatIndex) || repeatIndex <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(query),
                    "Invalid field query format. Expected format is 'FIELD_INDEX' or 'FIELD_INDEX(repeat)'.");
            }

            if (!int.TryParse(fieldQuery[..parenIndex], out fieldIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(query),
                    "First part of query should be a numeric field index");
            }
        }
        else // no parens, simple parse
        {
            if (!int.TryParse(fieldQuery, out fieldIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(query),
                    "First part of query should be a numeric field index");
            }
        }

        // If requested a repeat you just get that.  If you asked for the whole field, well there ya go
        var field = repeatIndex > 0 ? GetField(fieldIndex, repeatIndex) : GetField(fieldIndex);

        return separatorIndex >= 0
            // Defer to the field's Query method for the rest of the query
            ? field.Query(query[(separatorIndex + 1)..])
            : field.Value;
    }
}