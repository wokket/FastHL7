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
| FastHL7_ParseDateValues |  51.63 ns |  1.00 |      - |      - |         - |          NA |                                                                                                                                            
| HL7V2_ParseDateValues   | 866.87 ns | 16.79 | 0.1631 | 0.0010 |    2824 B |          NA |
| NHapi_ParseDateValues   | 169.98 ns |  3.29 | 0.0443 |      - |     768 B |          NA |
*/

    // Again, all tests are flawed, but let's try as best we can

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
        var expected = new DateTimeOffset(2025, 03, 04, 13, 28, 13, new(9, 30, 0))
            .AddSeconds(0.1234);
        if (ts != expected) //sanity check
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

        var expected = new DateTime(2025, 03, 04, 13, 28, 13, DateTimeKind.Utc).Subtract(new TimeSpan(9, 30, 0))
            .AddSeconds(0.1234);
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

        
        var tsField = new CommonTS(_inputTS);
        var dtm = new DateTimeOffset(tsField.Year, tsField.Month, tsField.Day, tsField.Hour, tsField.Minute, tsField.Second,  TimeSpan.FromMinutes(570)); // +9:30 offset, I'm not smart enough to get it from ts
        var expected = new DateTimeOffset(2025, 03, 04, 13, 28, 13, TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(30)));
        if (dtm != expected) //sanity check
        {
            throw new("Invalid date value");
        }
    }
}