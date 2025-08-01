namespace FastHl7;

public readonly ref struct Segment
{
    private readonly Delimiters _delimiters;
    private readonly Range[] _fields;

    public Segment(ReadOnlySpan<char> value, Delimiters delimiters)
    {
        Value = value;
        _delimiters = delimiters;

        _fields = SplitHelper.Split(value, _delimiters.FieldDelimiter);
    }

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
    public int FieldCount => _fields.Length;

    /// <summary>
    /// Gets the field at the given index.  Index 0 is always the Segment name (eg MSH).
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Field GetField(int i)
    {
        if (_fields.Length <= i || i < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(i), "Field index is out of range.");
        }

        return new(Value[_fields[i]], _delimiters);
    }

    /// <summary>
    /// This takes a dot-delimited query, and returns the raw message text for that query.
    /// eg "0" returns the content of the segment name, while "2.3" returns the content of the fourth component of the 2nd real field in the segment.
    /// </summary>
    public ReadOnlySpan<char> Query(ReadOnlySpan<char> query)
    {
        if (query.IsEmpty)
        {
            return null;
        }
        
        Span<Range> queryParts = SplitHelper.Split(query, '.');

        if (queryParts.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Query should have at least one part");
        }
        
        // first part has to be the field index (possibly with a repeat index??) 
        if (query[queryParts[0]].Contains('('))
        {
            throw new NotImplementedException("Repeating fields not yet supported");
        }
        
        if (! int.TryParse(query[queryParts[0]], out var fieldIndex))
        {
            throw new ArgumentOutOfRangeException(nameof(query), "First part of query should be a numeric field index");
        }
        var field = GetField(fieldIndex);

        return field.Value;
        // if (queryParts.Length > 1)
        // {
        //     // TODO: Handle components and sub-components
        // }
        
        
    }
}