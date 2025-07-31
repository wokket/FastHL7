using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Efferent.HL7.V2;
using FastHl7;

namespace Benchmarks;

/*
| Method           | Mean       | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|------:|-------:|----------:|------------:|
| FastHL7_NoEscape |   9.117 ns |  1.00 | 0.0018 |      32 B |        1.00 |
| Hl7V2_NoEscape   |  14.312 ns |  1.57 | 0.0056 |      96 B |        3.00 |
| FastHL7_Escape   |  90.130 ns |  9.89 | 0.0259 |     448 B |       14.00 |
| Hl7V2_Escape     | 188.147 ns | 20.64 | 0.0625 |    1080 B |       33.75 |
 */

[MemoryDiagnoser]
[ShortRunJob]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
[HideColumns("BuildConfiguration", "Error", "StdDev", "RatioSD")]
public class EscapingBench
{
    private const string _escapeText = @"T\XC3A4\glich 1 Tablette oral einnehmen\E\day \H\ONLY\N\ if tests > 10\S\9/l. ";
    private const string _noEscapeText = "This document supersedes Release 1 and contains additional specifications to accommodate new features introduced beginning HL7 Version 2.3.1, e.g. the use of choices within message structures. As of the time of this writing the current version is v2.7. This document is valid for all v2.x versions which have passed ballot. Chapter 2 of the HL7 Version 2.3.1 and 2.7 [rfHL7v231, rfHL7v27] specifies standard message structures (syntax) and content (semantics), the message definitions. It also specifies an interchange format and management rules, the encoding rules for HL7 message instances (see Figure 1).";
    private const string _msh = "MSH|^~\\&|SendingApp|SendingFacility|ReceivingApp|ReceivingFacility|20250304132813.1234+0930||ORM^O01|1234567890|P|2.3|||AL|NE|AL|NE\r\n";

    [Benchmark(Baseline = true)]
    public void FastHL7_NoEscape()
    {
        var delims = new Delimiters(_msh); // the 32b we allocate
        
        if (_noEscapeText.AsSpan().RequiresUnescaping(delims)) // Kind of cheating, but we're pretty explicit to only call the Decode if it's required
        {
            var result = _noEscapeText.AsSpan().Unescape(delims);
        }
        
    }

    [Benchmark]
    public void Hl7V2_NoEscape()
    {
        var encoding = new HL7Encoding();
        var result = encoding.Decode(_noEscapeText);
    }
    
    
    [Benchmark]
    public void FastHL7_Escape()
    {
        var delims = new Delimiters(_msh); // the 32b we allocate
        var result = _escapeText.AsSpan().Unescape(delims);
    }

    [Benchmark]
    public void Hl7V2_Escape()
    {
        var encoding = new HL7Encoding();
        var result = encoding.Decode(_escapeText);
    }
    

}