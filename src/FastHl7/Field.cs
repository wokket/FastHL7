namespace FastHl7;

/// <summary>
/// Fields can contain repeats, components, and sub-components.
/// See [the spec](http://www.hl7.eu/HL7v2x/v251/std251/ch02.html#Heading13) for more info
/// </summary>
public ref struct Field
{
    private readonly Delimiters _delimiters;
    private Range[]? _components;

    public Field(ReadOnlySpan<char> value, Delimiters delimiters)
    {
        Value = value;
        _delimiters = delimiters;
    }

    private Range[] Components
    {
        get
        {
            _components ??= SplitHelper.Split(Value, _delimiters.ComponentDelimiter);
            return _components;
        }
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
    public bool HasRepeats
    {
        get { return Value.Contains(_delimiters.RepeatDelimiter); }
    }

    /// <summary>
    /// Gets the number of (possibly empty) components in this field (first/only repeat). Subsequent repeats are not counted.
    /// </summary>
    public int ComponentCount
    {
        get
        {
            
            return Components.Length;
        }
    }


    /// <summary>
    /// Gets the component at the given index (1-based).
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public ReadOnlySpan<char> GetComponent(int i)
    {
        if (Components.Length < i || i < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(i), "Component index is out of range.");
        }
    
        return Value[Components[i - 1]];
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public ReadOnlySpan<char> Query(ReadOnlySpan<char> query)
    {
        // we're expecting query to be in the format : 
        //"componentIndex[.sub-component]"
        
        //if this field has multiple repeats, and you query for things out of range, life might get weird....

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

        if (!int.TryParse(query[queryParts[0]], out var componentIndex))
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Unable to parse componentIndex");
        }

        var valueToQuery = Value;
        if (Value.Contains(_delimiters.RepeatDelimiter)) // Field has multiple repeats but user didn't query a specific one - default to first repeat
        {
            valueToQuery = valueToQuery[..valueToQuery.IndexOf(_delimiters.RepeatDelimiter)];
        }

        Span<Range> components = stackalloc Range[10];
        var componentCount = SplitHelper.Split(valueToQuery, _delimiters.ComponentDelimiter, components);
        

        var valueToReturn = Value[components[componentIndex - 1]]; // -1 for 1-based indexing like Hl7V2

        if (queryPartsCount == 1)
        {
            return valueToReturn; // nothing else to do
        }

        // we have a subcomponent
        if (!int.TryParse(query[queryParts[1]], out var subComponentIndex))
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Unable to parse subComponentIndex");
        }

        Span<Range> subComps = stackalloc Range[10];
        var subCompCount = SplitHelper.Split(valueToReturn, _delimiters.SubComponentDelimiter, subComps);

        if (subCompCount < subComponentIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Requested subcomponent not found in component");
        }

        valueToReturn = valueToReturn[subComps[subComponentIndex - 1]];

        return valueToReturn;
    }
}