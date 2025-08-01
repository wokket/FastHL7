using System.Net;
using FastHl7;

namespace UnitTests;

public class EscapingTests
{
    [InlineData("This is fine", false)]
    [InlineData(@"Obstetrician \T\ Gynaecologist", true)]
    [InlineData(@"Cost 99\T\#162;", true)]
    [Theory]
    public void TestDeEscapingRequiredDefaultDelims(string input, bool required)
    {
        // Arrange
        var delimiters =
            new FastHl7.Delimiters(
                "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3");
        // Assert
        Assert.Equal(required, input.AsSpan().RequiresUnescaping(delimiters));
    }


    [InlineData("No escaping required!", "No escaping required!")]
    [InlineData(@"Obstetrician \T\ Gynaecologist", "Obstetrician & Gynaecologist")]
    [InlineData(@"Pierre DuRho\S\ne \T\ Cie", "Pierre DuRho^ne & Cie")] // insurance company
    [InlineData(@"c:\E\temp\E\file.txt", @"c:\temp\file.txt")]
    [InlineData(@"\ZCustomSequence\ Is Ignored", @"\ZCustomSequence\ Is Ignored")] // custom application escape sequence, not replaced
    [InlineData(@"This is \H\highlighted text\N\ and this isn't", @"This is \H\highlighted text\N\ and this isn't")] // highlighted text is a consuming application problem, so we don't replace it
    [InlineData(@"10\S\9/l", "10^9/l")]
    [InlineData(@"Trailing \E\", @"Trailing \")]
    [InlineData(@"\F\ Leading", @"| Leading")]
    [InlineData(@"T\XC3A4\glich 1 Tablette oral einnehmen\E\day \H\ONLY\N\ if tests > 10\S\9/l. ", @"Täglich 1 Tablette oral einnehmen\day \H\ONLY\N\ if tests > 10^9/l. ")]
    [InlineData(@"Cost 99\T\#162;", "Cost 99¢")] // Embedded Html Encoded char
    [Theory]
    public void TestDeEscapingDefaultDelims(string input, string expected)
    {
        // Arrange
        var delimiters =
            new Delimiters(
                "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3");

        // Act
        var result = input.AsSpan().Unescape(delimiters);
        result = WebUtility.HtmlDecode(result.ToString()); // not going to be part of this library, but demonstrating what a caller could do if they have embedded Html encoded chars
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void TestDeEscapingMultilinesCustomDelims()
    {
        // Arrange
        var delimiters = new Delimiters("MSH|^~G&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202310101010||ADT^A01|1234567890|P|2.3");

        // Act
        var result = "MultiG.brGLine".AsSpan().Unescape(delimiters);
        
        // Assert
        Assert.Equal($"Multi{Environment.NewLine}Line", result);
    }

    [InlineData("C3A4", "ä")]
    [InlineData("C3A5", "å")]
    [InlineData("A2", "¢")]
    [InlineData("00A2", "¢")]
    [InlineData("E6AF8F", "每")]
    [Theory]
    public void TestHexDecode(string input, string expected)
    {
        var actual = EscapeSequenceExtensions.DecodeHexString(input);
        Assert.Equal(expected, actual);
    }
}