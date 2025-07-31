using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Efferent.HL7.V2;
using FastHl7;
using NHapi.Base.Model.Primitive;

namespace Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
[HideColumns("BuildConfiguration", "Error", "StdDev", "RatioSD")]
public class DateTimeParseBench
{
    /*
| Method                  | Mean      | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------:|------:|-------:|-------:|----------:|------------:|
| FastHL7_ParseDateValues |  52.17 ns |  1.00 |      - |      - |         - |          NA |
| HL7V2_ParseDateValues   | 889.60 ns | 17.05 | 0.1631 | 0.0010 |    2824 B |          NA |
| NHapi_ParseDateValues   |  18.11 ns |  0.35 | 0.0060 |      - |     104 B |          NA |
     */

    // Again, all tests are flawed, but lets try as best we can

    private const string _inputDT = "202503"; // March 2025
    private const string _inputTS = "20250304132813.1234+0930"; // March 4th, 2025, 1:28:13 PM, with offset +9:30

    [Benchmark(Baseline = true)]
    public void FastHL7_ParseDateValues()
    {
        var dt = _inputDT.AsSpan().AsDate();
        if (dt != new DateOnly(2025, 03, 01)) //sanity check
        {
            throw new("Invalid DT value");
        }

        var ts = _inputTS.AsSpan().AsDateTime();
        if (ts != new DateTimeOffset(2025, 03, 04, 13, 28, 13, new(9, 30, 0)).AddSeconds(0.1234)) //sanity check
        {
            throw new("Invalid TS value");
        }
    }

    [Benchmark]
    public void HL7V2_ParseDateValues()
    {
        var dt = MessageHelper.ParseDateTime(_inputDT);
        if (dt != new DateTime(2025, 03, 01)) //sanity check
        {
            throw new("Invalid DT value");
        }

        var ts = MessageHelper.ParseDateTime(_inputTS);

        // HL7V2 seems to lose the microsecond???  Logged at https://github.com/Efferent-Health/HL7-V2/issues/12
        var expected = new DateTime(2025, 03, 04, 13, 28, 13, DateTimeKind.Utc).Subtract(new TimeSpan(9, 30, 0))
            .AddSeconds(0.123);
        if (ts!.Value != expected) //sanity check
        {
            throw new("Invalid TS value");
        }
    }

    [Benchmark]
    public void NHapi_ParseDateValues()
    {
        var dtField = new CommonDT(_inputDT);
        var dt = new DateOnly(dtField.Year, Math.Max(dtField.Month, 1), Math.Max(dtField.Day, 1));
        if (dt != new DateOnly(2025, 03, 01)) //sanity check
        {
            throw new("Invalid DT value");
        }

        // I'm not smart enough to figure out how to do this
        // var tsField = new CommonTS(_inputDTM);
        // var dtm = new DateTimeOffset(tsField.Year, tsField.Month, tsField.Day, tsField.Hour, tsField.Minute, tsField.Second, TimeSpan.FromMinutes(tsField.GMTOffset));
        // if (dtm != new DateTimeOffset(2025, 03, 04, 13, 28 , 13, TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(30)))) //sanity check
        // {
        //     throw new Exception("Invalid date value");
        // }
    }
}