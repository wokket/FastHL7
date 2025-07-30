using FastHl7;

namespace UnitTests;

public class DT_Tests
{
    public static TheoryData<string?, DateOnly?> DtData =>
        new()
        {
            { "20231011", new DateOnly(2023, 10, 11) },
            { "202503", new DateOnly(2025, 3, 1) },
            { "2024", new DateOnly(2024, 1, 1) },
            { null, null },
            { "", null }
        };

    [MemberData(nameof(DtData))]
    [Theory]
    public void TestParseYearOnly(string? input, DateOnly? expected)
    {
        var parsedDateTime = input.AsSpan().AsDtValue(true);
        Assert.Equal(expected, parsedDateTime);
    }
    
    [InlineData("abcd")]
    [InlineData("192")]
    [InlineData("202313")]
    [InlineData("20231011121")]
    [Theory]
    public void InvalidStringsThrow(string input)
    {
        var ex = Record.Exception(() => { input.AsSpan().AsDtValue(); });
        Assert.NotNull(ex);
    }
}