using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Efferent.HL7.V2;
using FastHl7;
using NHapi.Base.Model.Primitive;

namespace Benchmarks;

[MemoryDiagnoser]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
[HideColumns("BuildConfiguration", "Error", "StdDev", "RatioSD", "Gen0", "Gen1")]
public class DateTimeParseBench
{ 

/*
| Method                  | Runtime   | Mean      | Allocated | Alloc Ratio |
|------------------------ |---------- |----------:|----------:|------------:|
| FastHL7_ParseDateValues | .NET 10.0 |  44.84 ns |         - |          NA |                                                                                                                               
| HL7V2_ParseDateValues   | .NET 10.0 | 894.41 ns |    2824 B |          NA |
| NHapi_ParseDateValues   | .NET 10.0 | 157.23 ns |     768 B |          NA |
| FastHL7_ParseDateValues | .NET 9.0  |  52.55 ns |         - |          NA |
| HL7V2_ParseDateValues   | .NET 9.0  | 894.82 ns |    2824 B |          NA |
| NHapi_ParseDateValues   | .NET 9.0  | 169.71 ns |     768 B |          NA |
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