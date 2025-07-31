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
}