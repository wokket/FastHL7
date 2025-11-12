namespace UnitTests;

public class DelimiterTests
{
    [Fact]
    public void TestDelimiterInitialization()
    {
        // Arrange
        const string message = "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3\r\n";

        // Act
        var delimiters = new FastHl7.Delimiters(message);

        // Assert
        Assert.Equal('|', delimiters.FieldDelimiter);
        Assert.Equal('^', delimiters.ComponentDelimiter);
        Assert.Equal('~', delimiters.RepeatDelimiter);
        Assert.Equal('\\', delimiters.EscapeCharacter);
        Assert.Equal('&', delimiters.SubComponentDelimiter);
    }
    
    [Fact]
    public void Constructor_ThrowsArgumentException_WhenNotMSH()
    {
        const string message = "PID|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3\r\n";
        Assert.Throws<ArgumentException>(() => new FastHl7.Delimiters(message));
    }

    [Fact]
    public void Constructor_ThrowsIndexOutOfRange_WhenMessageTooShort()
    {
        // Length is 7; index 7 access should throw
        const string message = "MSH|^~\\";
        Assert.Throws<IndexOutOfRangeException>(() => new FastHl7.Delimiters(message));
    }
}