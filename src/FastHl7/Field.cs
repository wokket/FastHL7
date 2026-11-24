using System.Diagnostics;

namespace FastHl7;

/// <summary>
/// Fields can contain repeats, components, and sub-components.
/// See [the spec](http://www.hl7.eu/HL7v2x/v251/std251/ch02.html#Heading13) for more info
/// </summary>
[DebuggerDisplay("{Value}")]
public ref struct Field
{
    private readonly Delimiters _delimiters;

    /// <summary>
    /// Construct a new field from the given value and delimiters.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="delimiters"></param>
    internal Field(ReadOnlySpan<char> value, Delimiters delimiters)
    {
        Value = value;
        _delimiters = delimiters;
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
            var value = FirstRepeat;
            return SplitHelper.CountParts(value, _delimiters.ComponentDelimiter);
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
        if (i < 1 || !SplitHelper.TryGetPart(FirstRepeat, _delimiters.ComponentDelimiter, i - 1, out var component))
        {
            throw new ArgumentOutOfRangeException(nameof(i), "Component index is out of range.");
        }
    
        return component;
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

        var separatorIndex = query.IndexOf('.');
        var componentQuery = separatorIndex < 0 ? query : query[..separatorIndex];
        if (!int.TryParse(componentQuery, out var componentIndex) || componentIndex < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Unable to parse componentIndex");
        }

        if (!SplitHelper.TryGetPart(FirstRepeat, _delimiters.ComponentDelimiter, componentIndex - 1, out var valueToReturn))
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Requested component not found in field");
        }

        if (separatorIndex < 0)
        {
            return valueToReturn; // nothing else to do
        }

        // we have a subcomponent
        var subComponentQuery = query[(separatorIndex + 1)..];
        var nextSeparatorIndex = subComponentQuery.IndexOf('.');
        if (nextSeparatorIndex >= 0)
        {
            subComponentQuery = subComponentQuery[..nextSeparatorIndex];
        }

        if (!int.TryParse(subComponentQuery, out var subComponentIndex) || subComponentIndex < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Unable to parse subComponentIndex");
        }

        if (!SplitHelper.TryGetPart(valueToReturn, _delimiters.SubComponentDelimiter, subComponentIndex - 1, out var subComponent))
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Requested subcomponent not found in component");
        }

        return subComponent;
    }

    private ReadOnlySpan<char> FirstRepeat
    {
        get
        {
            var repeatIndex = Value.IndexOf(_delimiters.RepeatDelimiter);
            return repeatIndex < 0 ? Value : Value[..repeatIndex];
        }
    }
}