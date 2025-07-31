using FastHl7;

namespace UnitTests;

public class FieldTests
{
    
    [Fact]
    public void TestFieldConstruction()
    {
        // Arrange
        const string text = "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3";
        var delimiters = new Delimiters(text);
        var field = new Field("ADT^A01", delimiters);

        // Act & Assert
        Assert.Equal("ADT^A01", field.Value.ToString());
        Assert.True(field.HasValue);
        Assert.Equal(2, field.ComponentCount);
    }
}