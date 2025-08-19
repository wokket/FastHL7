using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Efferent.HL7.V2;
using FastHl7;

namespace Benchmarks;

/*
| Method           | Mean      | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|------:|-------:|----------:|------------:|
| FastHL7_NoEscape |  11.32 ns |  1.00 |      - |         - |          NA |
| Hl7V2_NoEscape   |  10.93 ns |  0.97 |      - |         - |          NA |
| FastHL7_Escape   | 112.39 ns |  9.93 | 0.0122 |     192 B |          NA |
| Hl7V2_Escape     | 207.79 ns | 18.35 | 0.0677 |    1064 B |          NA |
 */

[MemoryDiagnoser]
//[ShortRunJob]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
[HideColumns("BuildConfiguration", "Error", "StdDev", "RatioSD")]
public class EscapingBench
{
    private const string _escapeText =
        @"T\XC3A4\glich 1 Tablette oral einnehmen\E\day \H\ONLY\N\ if tests > 10\S\9/l. \.br\\.br\";

    private const string _noEscapeText =
        "This document supersedes Release 1 and contains additional specifications to accommodate new features introduced beginning HL7 Version 2.3.1, e.g. the use of choices within message structures. As of the time of this writing the current version is v2.7. This document is valid for all v2.x versions which have passed ballot. Chapter 2 of the HL7 Version 2.3.1 and 2.7 [rfHL7v231, rfHL7v27] specifies standard message structures (syntax) and content (semantics), the message definitions. It also specifies an interchange format and management rules, the encoding rules for HL7 message instances (see Figure 1).";

    private const string _msh =
        "MSH|^~\\&|SendingApp|SendingFacility|ReceivingApp|ReceivingFacility|20250304132813.1234+0930||ORM^O01|1234567890|P|2.3|||AL|NE|AL|NE\r\n";

    private readonly Delimiters _delims = new(_msh);
    private readonly HL7Encoding _encoding = new();
    
    [Benchmark(Baseline = true)]
    public void FastHL7_NoEscape()
    {
        var result = _noEscapeText.AsSpan().Unescape(_delims);
    }

    [Benchmark]
    public void Hl7V2_NoEscape()
    {
        var result = _encoding.Decode(_noEscapeText);
    }


    [Benchmark]
    public void FastHL7_Escape()
    {
        var result = _escapeText.AsSpan().Unescape(_delims);
    }

    [Benchmark]
    public void Hl7V2_Escape()
    {
        var result = _encoding.Decode(_escapeText);
    }
}