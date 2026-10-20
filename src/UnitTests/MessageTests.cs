using FastHl7;
using JetBrains.dotMemoryUnit;

[assembly: DotMemoryUnit(FailIfRunWithoutSupport = false)]

namespace UnitTests;

public class MessageTests
{
    public MessageTests(ITestOutputHelper output)
    {
        DotMemoryUnitTestOutput.SetOutputMethod(output.WriteLine);
    }


    [Fact]
    public void TestMessageConstruction()
    {
        // Arrange
        const string text = """
                            MSH|^~\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3
                            EVN|A01|202310101010
                            PID|1||123456^^^MRN||Doe^John||19800101|M|||123 Main St^^Anytown^CA^12345||(555)555-5555|||||||987654321
                            """;

        // Act
        var hl7Message = new Message(text);
        var mshSegment = hl7Message.GetSegment(0);

        // Assert
        Assert.Equal("MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3",
            mshSegment.Value);
        Assert.Equal("EVN|A01|202310101010", hl7Message.GetSegment(1).Value); // EVN segment
        Assert.Equal(
            "PID|1||123456^^^MRN||Doe^John||19800101|M|||123 Main St^^Anytown^CA^12345||(555)555-5555|||||||987654321",
            hl7Message.GetSegment(2).Value); // PID segment
    }

    [Fact]
    public void TestGetSegmentOutOfRange()
    {
        // Arrange
        const string text = """
                            MSH|^~\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3
                            EVN|A01|202310101010
                            PID|1||123456^^^MRN||Doe^John||19800101|M|||123 Main St^^Anytown^CA^12345||(555)555-5555|||||||987654321
                            """;
        var hl7Message = new Message(text);
        try
        {
            // Act & Assert
            _ = hl7Message.GetSegment(3);
            Assert.Fail("We shouldn't have got here");
        }
        catch (ArgumentOutOfRangeException)
        {
            // we're good 
        }
    }

    [Fact]
    public void TestGetSegmentByName()
    {
        // Arrange
        const string text = """
                            MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3
                            EVN|A01|202310101010
                            PID|1||123456^^^MRN||Doe^John||19800101|M|||123 Main St^^Anytown^CA^12345||(555)555-5555|||||||987654321
                            """;
        var hl7Message = new Message(text);
        // Act
        var pidSegment = hl7Message.GetSegment("PID");
        var evnSegment = hl7Message.GetSegment("EVN");
        var missingSegment = hl7Message.GetSegment("XYZ"); // This should return an empty segment
        // Assert

        Assert.True(pidSegment.HasValue);
        Assert.True(evnSegment.HasValue);
        Assert.False(missingSegment.HasValue);

        Assert.Equal(
            "PID|1||123456^^^MRN||Doe^John||19800101|M|||123 Main St^^Anytown^CA^12345||(555)555-5555|||||||987654321",
            pidSegment.Value);
        Assert.Equal("EVN|A01|202310101010", evnSegment.Value);
    }

    [Fact]
    public void TestGetSegmentByNameWithRepeat()
    {
        // Arrange
        var text =
            """
            MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3
            EVN|A01|202310101010
            PID|1||123456^^^MRN||Doe^John||19800101|M|||123 Main St^^Anytown^CA^12345||(555)555-5555|||||||987654321
            PID|2||654321^^^MRN||Smith^Jane||19850101|F|||456 Elm St^^Othertown^CA^54321||(555) 555-1234|||||||123456789
            """;
        var hl7Message = new Message(text);
        // Act
        var pidSegment1 = hl7Message.GetSegment("PID(1)");
        var pidSegment2 = hl7Message.GetSegment("PID(2)");
        // Assert
        Assert.Equal(
            "PID|1||123456^^^MRN||Doe^John||19800101|M|||123 Main St^^Anytown^CA^12345||(555)555-5555|||||||987654321",
            pidSegment1.Value);
        Assert.Equal(
            "PID|2||654321^^^MRN||Smith^Jane||19850101|F|||456 Elm St^^Othertown^CA^54321||(555) 555-1234|||||||123456789",
            pidSegment2.Value);
    }

    [Fact]
    public void TestGetSegmentByNameWithInvalidRepeat()
    {
        // Arrange
        const string text = """
                            MSH|^~\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3
                            EVN|A01|202310101010
                            PID|1||123456^^^MRN||Doe^John||19800101|M|||123 Main St^^Anytown^CA^12345||(555)555-5555|||||||987654321
                            """;
        var hl7Message = new Message(text);
        // Act & Assert
        try
        {
            _ = hl7Message.GetSegment("PID)(a"); // 0 is not a valid repeat index
            Assert.Fail("We shouldn't have got here");
        }
        catch (ArgumentOutOfRangeException)
        {
            // we're good 
        }
    }

    [Fact]
    public void TestCustomDelimiters()
    {
        const string text = """
                            MSH123451SendingApp1SendingFac2Comp21ReceivingApp1ReceivingFac111ADT2A0811P12.3
                            EVN1A081abcdef
                            """;
        var hl7Message = new Message(text);
        var sendingFac = hl7Message.GetSegment("MSH").GetField(3); //.GetComponent(2);
        //Assert.Equal("Comp", sendingFac);
    }

    [Fact]
    public void TestEpicNewLine_HL7V2_46()
    {
        //EPIC encodes newlines in an interesting way
        const string input = @"MSH|^~\&|EPIC|StJohn|CATH|StJohn|20061019172719||ADT^A08|MSGID12349876|P|2.3
PID||0493575^^^2^ID 1|454721||DOE^JOHN^^^^|DOE^JOHN^^^^|19480203|M||B|254 MYSTREET AVE^^MYTOWN^OH^44123^USA||(216)123-4567|||M|NON|400003403~1129086|
NK1||ROE^MARIE^^^^|SPO||(216)123-4567||EC|||||||||||||||||||||||||||
NK1||DOE^JHON^^^^|FTH||(216)123-4567||EC|||||||||||||||||||||||||||

PV1||O|OP^PAREG^^^^^^^^||||277^MYLASTNAME^BONNIE^^^^|||||||||| ||2688684|||||||||||||||||||||||||199912271408||||||002376853
GT1|1|123456789|DOE^JOHN^^^^^||254 MYSTREET AVE^APT #204 - P: 555-555-1234\X0D0A\254 MYSTREET AVE S^MYTOWN^OH^44123^US^^^MYTOWN|(555)555-1234^^7^^^123^1234567~(555)555-1234^^1^^^555^1234567||19550101|F|P/|15|555-55-5555|||||^^^^^US|||Re";

        var message = new Message(input);
        var field = message.Query("GT1.5.2");

        var decoded = field.Unescape(message.Delimiters);

        Assert.Equal("APT #204 - P: 555-555-1234\r\n254 MYSTREET AVE S", decoded);
    }
}