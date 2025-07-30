namespace UnitTests;

public class DelimiterTests
{
    [Fact]
    public void TestDelimiterInitialization()
    {
        // Arrange
        var message = "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3\r\n";

        // Act
        var delimiters = new FastHl7.Delimiters(message);

        // Assert
        Assert.Equal('|', delimiters.FieldDelimiter);
        Assert.Equal('^', delimiters.ComponentDelimiter);
        Assert.Equal('~', delimiters.RepeatDelimiter);
        Assert.Equal('\\', delimiters.EscapeCharacter);
        Assert.Equal('&', delimiters.SubComponentDelimiter);
    }
}