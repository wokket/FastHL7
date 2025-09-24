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
        Assert.Equal("|", segment.GetField(1).Value);
        Assert.Equal("^~\\&", segment.GetField(2).Value);
        Assert.Equal("SendingApp", segment.GetField(3).Value);
        Assert.Equal("ReceivingFac", segment.GetField(6).Value);
        Assert.Equal("ADT^A01", segment.GetField(9).Value);
        Assert.Equal("2.3", segment.GetField(12).Value);
        Assert.True(segment.HasValue);
        Assert.Equal(13, segment.FieldCount);
    }

    [Fact]
    public void TestGetFieldOutOfRange()
    {
        // Arrange
        const string text =
            "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3";
        var delimiters = new Delimiters(text);
        var segment = new Segment(text.AsSpan(), delimiters);
        
        _ = segment.GetField(12); // this should work: MSH.12 is VersionId
        
        try
        {
            // Act & Assert
            _ = segment.GetField(13);
            Assert.Fail("We shouldn't have got here");
        }
        catch (ArgumentOutOfRangeException)
        {
            // we're good 
        }
        
        const string evnText =
            "EVN|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3"; // _Not_ an MSH, diff field count
        delimiters = new Delimiters(text);
        segment = new Segment(evnText.AsSpan(), delimiters);
        try
        {
            // Act & Assert
            _ = segment.GetField(12); // 12 worked above on an MSH, but an identical format non-MSH should fail
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