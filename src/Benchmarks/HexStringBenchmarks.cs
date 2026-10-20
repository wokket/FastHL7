using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Efferent.HL7.V2;
using FastHl7;

namespace Benchmarks;


/*
| Method            | Mean     | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |---------:|------:|-------:|----------:|------------:|
| FastHL7_HexString | 20.07 ns |  1.00 | 0.0076 |      32 B |        1.00 |                                                                                                                                                                              
| Hl7V2_HexString   | 31.77 ns |  1.58 | 0.0459 |     192 B |        6.00 |
 */

[MemoryDiagnoser]
//[ShortRunJob]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
[HideColumns("BuildConfiguration", "Error", "StdDev", "RatioSD")]
public class HexStringBenchmarks
{
    private const string _input = "C3A4C3A4"; // "ä" in UTF-8 hex encoding

    [Benchmark(Baseline = true)]
    public void FastHL7_HexString()
    {
        var result = EscapeSequenceExtensions.DecodeHexString(_input);
        if (result is not "ää") // sanity check
        {
            throw new();
        }
    }

    [Benchmark]
    public void Hl7V2_HexString()
    {
        var result = HL7Encoding.DecodeHexString(_input);
        if (result is not "ää") // sanity check
        {
            throw new();
        }
    }
}