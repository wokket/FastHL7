using FastHl7;

namespace UnitTests;

public class SegmentTests
{
    [Fact]
    public void TestSegmentConstruction()
    {
        // Arrange
        const string text =
            "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3";
        var delimiters = new Delimiters(text);
        var segment = new Segment(text.AsSpan(), delimiters);

        // Act & Assert
        Assert.Equal("MSH", segment.Name);
        Assert.Equal("MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3",
            segment.Value);
        Assert.Equal("SendingApp", segment.GetField(2));
        Assert.Equal("ReceivingFac", segment.GetField(5));
        Assert.Equal("ADT^A01", segment.GetField(8));
        Assert.Equal("2.3", segment.GetField(11));
        Assert.True(segment.HasValue);
    }

    [Fact]
    public void TestGetFieldOutOfRange()
    {
        // Arrange
        const string text =
            "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3";
        var delimiters = new Delimiters(text);
        var segment = new Segment(text.AsSpan(), delimiters);
        try
        {
            // Act & Assert
            _ = segment.GetField(12);
            Assert.Fail("We shouldn't have got here");
        }
        catch (ArgumentOutOfRangeException)
        {
            // we're good 
        }

        segment = new(text.AsSpan(), delimiters);
        try
        {
            // Act & Assert
            _ = segment.GetField(-1);
            Assert.Fail("We shouldn't have got here");
        }
        catch (ArgumentOutOfRangeException)
        {
            // we're good 
        }
    }

    [Fact]
    public void MustInitFromMSH()
    {
        // Arrange
        const string text = "EVN|A01|202310101010";
        try
        {
            // Act & Assert
            _ = new Delimiters(text);
            Assert.Fail("We shouldn't have got here");
        }
        catch (ArgumentException e)
        {
            Assert.Equal("Message must start with MSH segment (Parameter 'message')", e.Message);
        }
        
    }

}