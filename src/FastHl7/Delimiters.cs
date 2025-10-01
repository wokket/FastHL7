using System.Diagnostics;

namespace FastHl7;

/// <summary>
/// Helper for various delimiter characters used in a message.  The Spec allows for differing delimiters <em>per message</em>
/// but is generally a standard set  (|^~\&amp;) 
/// </summary>
public readonly struct Delimiters
{
    /// <summary>
    /// Get the delimiter field for the given message.
    /// The message must start with an MSH segment, and the delimiters are taken from the MSH-1 and MSH-2 fields.
    /// </summary>
    /// <param name="message"></param>
    /// <exception cref="ArgumentException"></exception>
    internal Delimiters(ReadOnlySpan<char> message)
    {
        if (message[..3] is not "MSH")
        {
            throw new ArgumentException("Message must start with MSH segment", nameof(message));
        }
        
        FieldDelimiter = message[3];
        ComponentDelimiter = message[4];
        RepeatDelimiter = message[5];
        EscapeCharacter = message[6];
        SubComponentDelimiter = message[7];
    }
    
    /// <summary>
    /// The sub-component delimiter, typically '&amp;'
    /// </summary>
    public char SubComponentDelimiter { get; }

    /// <summary>
    /// The escape character, typically '\'
    /// </summary>
    public char EscapeCharacter { get; }
    
    /// <summary>
    /// The repetition delimiter, typically '~'
    /// </summary>
    public char RepeatDelimiter { get; }

    /// <summary>
    /// The component delimiter, typically '^'
    /// </summary>
    public char ComponentDelimiter { get; }

    /// <summary>
    /// The field delimiter, typically '|'
    /// </summary>
    public char FieldDelimiter { get; }
}