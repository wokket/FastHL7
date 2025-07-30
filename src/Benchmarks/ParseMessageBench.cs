using BenchmarkDotNet.Attributes;
using NHapi.Base.Parser;
using NHapi.Model.V23.Message;

namespace Benchmarks;

/// <summary>
/// Benchmark to load and parse a message, then get values from it.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
[HideColumns("BuildConfiguration", "Error", "StdDev", "RatioSD")]
public class ParseMessageBench
{
    // This uses the same sample message I added to Hl7-V2 which is a very feature complete message (encodings, escape chars etc)
    // While this library is all about perf (and perf is a feature), this lib isn't yet anything like spec compliant, so take with a truck full of salt.

    // All benchmarks are flawed in some respect. I've tried to do like-for-like here, but if I'm being unfair somehow let me know.

/*
| Method  | Mean       | Ratio  | Gen0    | Gen1    | Allocated | Alloc Ratio |
|-------- |-----------:|-------:|--------:|--------:|----------:|------------:|
| FastHl7 |   1.422 us |   1.00 |  0.0095 |       - |     168 B |        1.00 |
| Hl7V2   |  24.513 us |  17.24 |  7.3242 |  1.0986 |  126368 B |      752.19 |
| NHapi   | 181.697 us | 127.78 | 29.2969 | 11.7188 |  510982 B |    3,041.56 |
 */

    private static readonly string _sampleMessage = File.ReadAllText("Sample-Orm.txt");

    [Benchmark(Baseline = true)]
    public void FastHl7()
    {
        var msg = new FastHl7.Message(_sampleMessage);
        var nte = msg.GetSegment("NTE(2)");

        var noteText = nte.GetField(3);
        if (!noteText.StartsWith("more text"))
        {
            throw new(); //sanity check
        }
    }

    [Benchmark]
    public void Hl7V2()
    {
        var msg = new Efferent.HL7.V2.Message(_sampleMessage);
        msg.ParseMessage(true);
        var nte = msg.Segments("NTE")[1];

        var noteText = nte.Fields(3);
        if (!noteText.Value.StartsWith("more text"))
        {
            throw new(); //sanity check
        }
    }

    [Benchmark]
    public void NHapi()
    {
        var parser = new PipeParser();
        var msg = parser.Parse(_sampleMessage) as ORM_O01; // there's normally more work involved in determining this
        var nte = msg?.ORDERs.First().ORDER_DETAIL.NTEs.ElementAt(1);
        var noteText = nte?.GetField(3);
        if (!(noteText?.GetValue(0)?.ToString()?.StartsWith("more text") ?? false))
        {
            throw new(); //sanity check
        }
    }
}