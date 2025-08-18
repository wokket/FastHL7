using BenchmarkDotNet.Attributes;

namespace Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
[HideColumns("BuildConfiguration", "Error", "StdDev", "RatioSD")]
public class QueryBench
{
    
/*
| Method  | Mean       | Ratio | Gen0   | Allocated | Alloc Ratio |
|-------- |-----------:|------:|-------:|----------:|------------:|
| FastHl7 | 4,389.5 ns |  1.00 | 0.0305 |     568 B |        1.00 |
| Hl7V2   |   800.9 ns |  0.18 | 0.1059 |    1664 B |        2.93 |
 */
    
    
    private static readonly Efferent.HL7.V2.Message _hl7V2Message = new(_message);

    static QueryBench()
    {
        _hl7V2Message.ParseMessage(true);
    }

    [Benchmark(Baseline = true)]
    public void FastHl7()
    {
        // we'll give HL7V2 a headstart by building our ref struct inside the benchmark, not pre-loading it!
        var msg = new FastHl7.Message(_message);
        var result = msg.Query("OBX(14).16.2");
        if (result is not "XYZ LAB")
        {
            throw new();
        }
    }

    [Benchmark]
    public void Hl7V2()
    {
        var result = _hl7V2Message.GetValue("OBX(14).16.2");
        if (result is not "XYZ LAB")
        {
            throw new();
        }
    }
    
    
    
        private const string _message =
        """
        MSH|^~\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|20120411070545||ORU^R01|59689|P|2.3
        PID|1|12345|12345^^^MIE&1.2.840.114398.1.100&ISO^MR||MOUSE^MINNIE^S||19240101|F|||123 MOUSEHOLE LN^^FORT WAYNE^IN^46808|||||||||||||||||||
        PV1|1|I||EL|||00976^PHYSICIAN^DAVID^G|976^PHYSICIAN^DAVID^G|01055^PHYSICIAN^RUTH^K~02807^PHYSICIAN^ERIC^LEE~07019^GI^ASSOCIATES~01255^PHYSICIAN^ADAM^I~02084^PHYSICIAN^SAYED~01116^PHYSICIAN^NURUDEEN^A~01434^PHYSICIAN^DONNA^K~02991^PHYSICIAN^NICOLE|MED||||7|||00976^PHYSICIAN^DAVID^G||^^^Chart ID^Vis|||||||||||||||||||||||||20120127204900
        ORC|RE||12376|||||||100^DUCK^DASIY||71^DUCK^DONALD|^^^||20120411070545|||||
        OBR|1||12376|cbc^CBC|R||20120410160227|||22^GOOF^GOOFY|||Fasting: No|201204101625||71^DUCK^DONALD||||||201204101630|||F||^^^^^R|||||||||||||||||85025|
        OBX|1|NM|wbc^Wbc^Local^6690-2^Wbc^LN||7.0|/nl|3.8-11.0||||F|||20120410160227|lab|12^XYZ LAB|
        OBX|2|NM|neutros^Neutros^Local^770-8^Neutros^LN||68|%|40-82||||F|||20120410160227|lab|12^XYZ LAB|
        OBX|3|NM|lymphs^Lymphs^Local^736-9^Lymphs^LN||20|%|11-47||||F|||20120410160227|lab|12^XYZ LAB|
        OBX|4|NM|monos^Monos^Local^5905-5^Monos^LN||16|%|4-15|H|||F|||20120410160227|lab|12^XYZ LAB|
        OBX|5|NM|eo^Eos^Local^713-8^Eos^LN||3|%|0-8||||F|||20120410160227|lab|12^XYZ LAB|
        OBX|6|NM|baso^Baso^Local^706-2^Baso^LN||0|%|0-1||||F|||20120410160227|lab|12^XYZ LAB|
        OBX|7|NM|ig^Imm Gran^Local^38518-7^Imm Gran^LN||0|%|0-2||||F|||20120410160227|lab|12^XYZ LAB|
        OBX|8|NM|rbc^Rbc^Local^789-8^Rbc^LN||4.02|/pl|4.07-4.92|L|||F|||20120410160227|lab|12^XYZ LAB|
        OBX|9|NM|hgb^Hgb^Local^718-7^Hgb^LN||13.7|g/dl|12.0-14.1||||F|||20120410160227|lab|12^XYZ LAB|
        OBX|10|NM|hct^Hct^Local^4544-3^Hct^LN||40|%|34-43||||F|||20120410160227|lab|12^XYZ LAB|
        OBX|11|NM|mcv^Mcv^Local^787-2^Mcv^LN||80|fl|77-98||||F|||20120410160227|lab|12^XYZ LAB|
        OBX|12|NM|mch^Mch||30|pg|27-35||||F|||20120410160227|lab|12^XYZ LAB|
        OBX|13|NM|mchc^Mchc||32|g/dl|32-35||||F|||20120410160227|lab|12^XYZ LAB|
        OBX|14|NM|plt^Platelets||221|/nl|140-400||||F|||20120410160227|lab|12^XYZ LAB|
        """;

}