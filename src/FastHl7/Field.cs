namespace FastHl7;


/// <summary>
/// Fields can contain repeats, components, and sub-components.
/// See [the spec](http://www.hl7.eu/HL7v2x/v251/std251/ch02.html#Heading13) for more info
/// </summary>
public readonly ref struct Field
{
    private readonly Delimiters _delimiters;
    private readonly Range[] _repeats;
    private readonly Range[] _repeat0Components;

    public Field(ReadOnlySpan<char> value, Delimiters delimiters)
    {
        Value = value;
        _delimiters = delimiters;
        _repeats = SplitHelper.Split(value, _delimiters.RepeatDelimiter);
        _repeat0Components = SplitHelper.Split(value[_repeats[0]], _delimiters.ComponentDelimiter);
    }

    /// <summary>
    /// The raw string value of this field.
    /// </summary>
    public ReadOnlySpan<char> Value { get; }

    /// <summary>
    /// Whether this field contains a value.
    /// </summary>
    /// <remarks>Open Q: Does a field consisting of nothing but empty repeats/components (`"^^~^^"`)count as having a value?</remarks>
    public bool HasValue => !Value.IsEmpty;

    /// <summary>
    /// Whether this field has multiple repeats
    /// </summary>
    public bool HasRepeats => _repeats.Length > 1;

    /// <summary>
    /// Gets the number of (possibly empty) components in this field (first/only repeat). Subsequent repeats are not counted.
    /// </summary>
    public int ComponentCount => _repeat0Components?.Length ?? 0;
    

    /// <summary>
    /// Gets the component (in the first/only repeat) at the given index (0-based).
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public ReadOnlySpan<char> GetComponent(int i)
    {
        if (_repeat0Components.Length <= i || i < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(i), "Component index is out of range.");
        }

        return Value[_repeat0Components[i]];
    }


    public ReadOnlySpan<char> Query(ReadOnlySpan<char> query)
    {
        throw new NotImplementedException();
    }
}