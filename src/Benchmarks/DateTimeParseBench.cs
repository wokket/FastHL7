using BenchmarkDotNet.Attributes;
using Efferent.HL7.V2;
using FastHl7;

namespace Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class DateTimeParseBench
{
    
    // Again, all tests are flawed, but lets try as best we can

    private const string _input = "202503"; // March 2025
    
    [Benchmark(Baseline = true)]
    public void FastHL7_ParseDTValue()
    {
        var output = _input.AsSpan().AsDtValue(true);
    }

    [Benchmark]
    public void HL7V2_ParseDTValue()
    {
        var dt = MessageHelper.ParseDateTime(_input);
    }

    // [Benchmark]
    // public void NHapi_ParseDTValue()
    // {
    //     var dt = NHapi.Base.Validation.
    // }
    
}