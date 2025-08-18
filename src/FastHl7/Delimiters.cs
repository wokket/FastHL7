namespace FastHl7;

/// <summary>
/// Helper for various delimiter characters used in a message.  The Spec allows for differing delimiters _per message_
/// but is generally a standard set (|^~\&) 
/// </summary>
public struct Delimiters
{
    
    //public static Delimiters Default { get; } = new("MSH|^~\\&");
    
    public Delimiters(ReadOnlySpan<char> message)
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

    public char SubComponentDelimiter { get; }

    public char EscapeCharacter { get; }

    public char RepeatDelimiter { get; }

    public char ComponentDelimiter { get; }

    public char FieldDelimiter { get; }
}