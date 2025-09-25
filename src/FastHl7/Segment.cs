namespace FastHl7;

/// <summary>
/// Represents a single Segment(line) of a HL7 message.
/// You normally don't construct these yourself as a consumer of the library, but get access via the <see cref="Message"/> type.
/// </summary>
public readonly ref struct Segment
{
    private readonly Delimiters _delimiters;
    private readonly Range[] _fields;

    internal Segment(ReadOnlySpan<char> value, Delimiters delimiters)
    {
        Value = value;
        _delimiters = delimiters;

        _fields = SplitHelper.Split(value, _delimiters.FieldDelimiter);
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
            if (Name.Equals("MSH", StringComparison.OrdinalIgnoreCase))
            {
                // MSH is a special cat, field 1 is the field delim char, and field 2 the remainder of the encoding chars
                return _fields.Length + 1; // offset by one
            }
            return _fields.Length;
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
        if (FieldCount <= i || i < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(i), "Field index is out of range.");
        }
        
        if (!Name.Equals("MSH", StringComparison.OrdinalIgnoreCase))
        {
            return new(Value[_fields[i]], _delimiters);
        }

        // MSH is a special cat, field 1 is the field delim char, and field 2 the remainder of the encoding chars
        switch (i)
        {
            case 1:
                return new(new[] { _delimiters.FieldDelimiter }, _delimiters);
            case 2:
            {
                // field 2 is the rest of field 1
                var encodingChars = Value[_fields[1]]; // Everything but first char
                return new(encodingChars, _delimiters);
            }
            default:
                return new(Value[_fields[i-1]], _delimiters); // all the others are offset now
        }
        
    }

    /// <summary>
    /// Gets the field at the given index.  Index 0 is always the Segment name (eg MSH).
    /// The returned field _only_ contains the information from the requested repeat(1-i based index), and no others.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="repeat">1-based index</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    internal Field GetField(int index, int repeat)
    {
        if (_fields.Length <= index || index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Field index is out of range.");
        }

        if (repeat <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(repeat), "Repeat must be positive");
        }

        var fieldValue = Value[_fields[index]];

        Span<Range> repeats = stackalloc Range[20];
        var repeatCount = SplitHelper.Split(fieldValue, _delimiters.RepeatDelimiter, repeats);
        if (repeat > repeatCount)
        {
            throw new ArgumentOutOfRangeException(nameof(repeat), "Asked for repeat field doesn't have");
        }

        fieldValue = fieldValue[repeats[repeat - 1]];

        return new(fieldValue, _delimiters);
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


        Span<Range> queryParts = stackalloc Range[10];
        var queryPartsCount = SplitHelper.Split(query, '.', queryParts);

        if (queryPartsCount == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Query should have at least one part");
        }

        var fieldQuery = query[queryParts[0]];

        // first part has to be the field index (possibly with a repeat index??) 
        var repeatIndex = -1;
        int fieldIndex;
        var parenIndex = fieldQuery.IndexOf('(');
        if (parenIndex >= 0)
        {
            // we have a request for a repeat.
            if (!SplitHelper.TryGetIntBetweenParens(fieldQuery, out repeatIndex))
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

        return queryPartsCount > 1
            ?
            // Defer to the field's Query method for the rest of the query
            field.Query(query[queryParts[1].Start..])
            : field.Value;
    }
}