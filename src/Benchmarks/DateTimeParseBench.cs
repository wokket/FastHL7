using BenchmarkDotNet.Attributes;
using Efferent.HL7.V2;
using FastHl7;

namespace Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class DateTimeParseBench
{
    
    /*
| Method               | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |----------:|-----------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| FastHL7_ParseDTValue |  49.34 ns |   0.575 ns | 0.031 ns |  1.00 |    0.00 |      - |      - |         - |          NA |
| HL7V2_ParseDTValue   | 861.43 ns | 135.225 ns | 7.412 ns | 17.46 |    0.13 | 0.1631 | 0.0010 |    2824 B |          NA |
     */
    
    // Again, all tests are flawed, but lets try as best we can

    private const string _inputDT = "202503"; // March 2025
    private const string _inputDTM = "20250304132813.1234+0930"; // March 4th, 2025, 1:28:13 PM, with offset +9:30
    
    [Benchmark(Baseline = true)]
    public void FastHL7_ParseDTValue()
    {
        var dt = _inputDT.AsSpan().AsDate(true);
        var dtm = _inputDTM.AsSpan().AsDateTime();
    }

    [Benchmark]
    public void HL7V2_ParseDTValue()
    {
        var dt = MessageHelper.ParseDateTime(_inputDT);
        var dtm = MessageHelper.ParseDateTime(_inputDTM);
    }

    // [Benchmark]
    // public void NHapi_ParseDTValue()
    // {
    //     var dt = NHapi.Base.Parser
    // }
    
}