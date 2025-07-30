using FastHl7;

namespace UnitTests;

public class DTM_Tests
{
    public static TheoryData<string?, DateTimeOffset?> DtData =>
        new()
        {
            { "2024", new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            { "202503", new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero) },
            { "20231011", new DateTimeOffset(2023, 10, 11, 0, 0, 0, TimeSpan.Zero) },
            { "2023101112", new DateTimeOffset(2023, 10, 11, 12, 0, 0, TimeSpan.Zero) },
            { "202310111213", new DateTimeOffset(2023, 10, 11, 12, 13, 0, TimeSpan.Zero) },
            { "20231011121314", new DateTimeOffset(2023, 10, 11, 12, 13, 14, TimeSpan.Zero) },
            { "20231011121314.5", new DateTimeOffset(2023, 10, 11, 12, 13, 14, TimeSpan.Zero).AddSeconds(0.5) },
            { "20231011121314.12", new DateTimeOffset(2023, 10, 11, 12, 13, 14, TimeSpan.Zero).AddSeconds(0.12) },
            { "20231011121314.123", new DateTimeOffset(2023, 10, 11, 12, 13, 14, TimeSpan.Zero).AddSeconds(0.123) },
            { "20231011121314.1234", new DateTimeOffset(2023, 10, 11, 12, 13, 14, TimeSpan.Zero).AddSeconds(0.1234) },
            { "20230102030405.5+0130", new DateTimeOffset(2023, 01, 02, 3, 4, 5, new (1, 30, 0)).AddSeconds(0.5) },
            { "20230102030405.1234-0130", new DateTimeOffset(2023, 01, 02, 3, 4, 5, new (-1, 30, 0)).AddSeconds(0.1234) },
            { null, null },
            { "", null }
        };

    [MemberData(nameof(DtData))]
    [Theory]
    public void TestSuccess(string? input, DateTimeOffset? expected)
    {
        var parsedDateTime = input.AsSpan().AsDateTime();
        Assert.Equal(expected, parsedDateTime);
    }
    
    [InlineData("abcd")]
    [InlineData("192")]
    [InlineData("202313")]
    [InlineData("20231011a121")]
    [InlineData("20230102030405.6789+10300")]
    [Theory]
    public void InvalidStringsThrow(string input)
    {
        var ex = Record.Exception(() => { input.AsSpan().AsDateTime(); });
        Assert.NotNull(ex);
    }
}