namespace FastHl7;

public readonly ref struct Field
{
    private readonly Delimiters _delimiters;
    private readonly Range[] _components;

    public Field(ReadOnlySpan<char> value, Delimiters delimiters)
    {
        Value = value;
        _delimiters = delimiters;

        _components = SplitHelper.Split(value, _delimiters.ComponentDelimiter);
    }

    /// <summary>
    /// The raw string value of this field.
    /// </summary>
    public ReadOnlySpan<char> Value { get; }

    /// <summary>
    /// Whether this field contains a value.
    /// </summary>
    /// <remarks>Open Q: Does a field consisting of nothing but empty components (`"^^^^"`)count as having a value?</remarks>
    public bool HasValue => !Value.IsEmpty;

    /// <summary>
    /// Gets the number of (possibly empty) components in this field.
    /// </summary>
    public int ComponentCount => _components?.Length ?? 0;

    public ReadOnlySpan<char> GetComponent(int i)
    {
        if (_components.Length <= i || i < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(i), "Component index is out of range.");
        }

        return Value[_components[i]];
    }
}