using FastHl7;

namespace UnitTests;

public class SplitTests
{

    [Fact]
    public void TestLLFSegmentSplitting()
    {
    
        var message = "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3\rEVN|A01|202310101010\rPID|1||123456^^^MRN||Doe^John||19800101|M|||123 Main St^^Anytown^CA^12345||(555)555-5555|||||||987654321\r";
        var segments = SplitHelper.SplitSegments(message);

        Assert.Equal(3, segments.Length);
        Assert.Equal("MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3", message[segments[0]]);
        Assert.Equal("EVN|A01|202310101010", message[segments[1]]);
        Assert.Equal("PID|1||123456^^^MRN||Doe^John||19800101|M|||123 Main St^^Anytown^CA^12345||(555)555-5555|||||||987654321", message[segments[2]]);
    }
    
    [Fact]
    public void TestCRSegmentSplitting()
    {
    
        var message = "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3\nEVN|A01|202310101010\nPID|1||123456^^^MRN||Doe^John||19800101|M|||123 Main St^^Anytown^CA^12345||(555)555-5555|||||||987654321\n\n\n";
        var segments = SplitHelper.SplitSegments(message);

        Assert.Equal(3, segments.Length);
        Assert.Equal("MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3", message[segments[0]]);
        Assert.Equal("EVN|A01|202310101010", message[segments[1]]);
        Assert.Equal("PID|1||123456^^^MRN||Doe^John||19800101|M|||123 Main St^^Anytown^CA^12345||(555)555-5555|||||||987654321", message[segments[2]]);
    }
    
    [Fact]
    public void TestCRLFSegmentSplitting()
    {
    
        var message = "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3\r\nEVN|A01|202310101010\r\nPID|1||123456^^^MRN||Doe^John||19800101|M|||123 Main St^^Anytown^CA^12345||(555)555-5555|||||||987654321\r\n\r\n\r\n";
        var segments = SplitHelper.SplitSegments(message);

        Assert.Equal(3, segments.Length);
        Assert.Equal("MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3", message[segments[0]]);
        Assert.Equal("EVN|A01|202310101010", message[segments[1]]);
        Assert.Equal("PID|1||123456^^^MRN||Doe^John||19800101|M|||123 Main St^^Anytown^CA^12345||(555)555-5555|||||||987654321", message[segments[2]]);
    }
}