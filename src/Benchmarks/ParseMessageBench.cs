using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using NHapi.Base.Parser;
using NHapi.Base.Util;
using NHapi.Model.V23.Message;

namespace Benchmarks;

/// <summary>
/// Benchmark to load and parse a message, then get values from it.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
[HideColumns("BuildConfiguration", "Error", "StdDev", "RatioSD")]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class ParseMessageBench
{
    // This uses the same sample message I added to Hl7-V2 which is a very feature complete message (encodings, escape chars etc)
    // While this library is all about perf (and perf is a feature), this lib has not been battle tested in production so take with a truck full of salt.

    // All benchmarks are flawed in some respect. I've tried to do like-for-like as best I can here, but if I'm being unfair somehow let me know.

/*
| Method           | Mean         | Ratio  | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------------- |-------------:|-------:|--------:|--------:|----------:|------------:|
| FastHl7          |   1,829.1 ns |   1.00 |  0.0286 |       - |     496 B |        1.00 |                                                                                                                                             
| Hl7V2            |  26,094.4 ns |  14.27 |  7.5378 |  1.2817 |  130216 B |      262.53 |
| NHapi_Parser     | 183,373.6 ns | 100.26 | 29.2969 | 12.6953 |  518875 B |    1,046.12 |
| NaiveStringManip |     426.3 ns |   0.23 |  0.2146 |  0.0010 |    3704 B |        7.47 |
*/

    private static readonly string _sampleMessage = File.ReadAllText("Sample-Orm.txt");

    [Benchmark(Baseline = true)]
    public void FastHl7()
    {
        var msg = new FastHl7.Message(_sampleMessage);
        var nte = msg.GetSegment("NTE(2)");
        
        var noteText = nte.GetField(3);
        if (!noteText.Value.StartsWith("more text"))
        {
            throw new(); //sanity check
        }
        
        var receivingApp = msg.Query("MSH.5");
        if (!receivingApp.Equals("ReceivingApp", StringComparison.OrdinalIgnoreCase))
        {
            throw new($"'{receivingApp}' is not expected value"); //sanity check
        }
        
        var patientFirstName = msg.Query("PID.5.2");
        if (!patientFirstName.Equals("Jane", StringComparison.OrdinalIgnoreCase))
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
        
        var receivingApp = msg.GetValue("MSH.5");
        if (!receivingApp.Equals("ReceivingApp", StringComparison.OrdinalIgnoreCase))
        {
            throw new(); //sanity check
        }
        
        var patientFirstName = msg.GetValue("PID.5.2");
        if (!patientFirstName.Equals("Jane", StringComparison.OrdinalIgnoreCase))
        {
            throw new(); //sanity check 
        }
    }
    
    [Benchmark]
    public void NHapi_Parser()
    {
        var parser = new PipeParser();
        var msg = (parser.Parse(_sampleMessage) as ORM_O01)!; // there's normally more work involved in determining this
        var nte = msg.ORDERs.First().ORDER_DETAIL.NTEs.ElementAt(1);
        var noteText = nte?.GetField(3);
        if (!(noteText?.GetValue(0)?.ToString()?.StartsWith("more text") ?? false))
        {
            throw new(); //sanity check
        }
        
        var receivingApp = msg.MSH.ReceivingApplication;
        if (!receivingApp.NamespaceID.Value.Equals("ReceivingApp", StringComparison.OrdinalIgnoreCase))
        {
            throw new(); //sanity check
        }
        
        var patientFirstName = msg.PATIENT.PID.GetPatientName(0).GivenName.Value;
        if (!patientFirstName.Equals("Jane", StringComparison.OrdinalIgnoreCase))
        {
            throw new();
        }
    }
  
    // this is no faster, and I'm not smart enough to make it work properly
    // [Benchmark]
    // public void NHapi_Terser()
    // {
    //     var parser = new PipeParser();
    //     var msg = parser.Parse(_sampleMessage) as ORM_O01; // there's normally more work involved in determining this
    //     var terser = new Terser(msg);
    //
    //     var noteText = terser.Get("/ORDER/ORDER_DETAIL/NTE-3"); //I'm not smart enough to work out how to get the 2nd NTE
    //     
    //     if (!(noteText?.StartsWith("some text") ?? false))
    //     {
    //         throw new(); //sanity check
    //     }
    // }

    [Benchmark]
    public void NaiveStringManip()
    {
        // assuming you just did the simplest thing that could possibly work, rather than using a library, just for giggles
        // no-one would _actually_ do this in production of course...
        var segments = _sampleMessage.Split('\n');

        var nte = segments[6];
        var fields = nte.Split('|');
        var noteText = fields[3];
        if (!noteText.StartsWith("more text"))
        {
            throw new(); //sanity check
        }
        
        var msh = segments[0];
        fields = msh.Split('|');
        var receivingApp = fields[4];
        if (!receivingApp.Equals("ReceivingApp", StringComparison.OrdinalIgnoreCase))
        {
            throw new(); //sanity check
        }

        var pid = segments[1];
        fields = pid.Split('|');
        var patientName = fields[5];
        var nameParts = patientName.Split('^');
        var patientFirstName = nameParts[1];
        if (!patientFirstName.Equals("Jane", StringComparison.OrdinalIgnoreCase))
        {
            throw new(); //sanity check 
        }
    }
}