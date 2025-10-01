using BenchmarkDotNet.Attributes;
using FastHl7;

namespace Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
[HideColumns("BuildConfiguration", "Error", "StdDev", "RatioSD")]
public class MllpReaderBench
{
    // The Setup just news up the objects, the actual read perf is the diff between the baseline and the ReadMessages bench. 
    // We read a stream with 100 copies of the same message in it so our amortised allocs are about 14 bytes per message if we
    // can get away with a synchronous handler.  If we have to allocate a string for an async handler then that's the price we pay.

/*
| Method            | Mean         | Ratio  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-------:|-------:|-------:|----------:|------------:|
| Setup             |     27.30 ns |   1.00 | 0.0162 |      - |     280 B |        1.00 |                                                                                                                                                                                         
| ReadMessagesSync  |  8,453.29 ns | 309.87 | 0.0916 |      - |    1672 B |        5.97 |
| ReadMessagesAsync | 14,593.32 ns | 534.94 | 9.5978 | 0.0153 |  165672 B |      591.69 |
 */


    private MemoryStream _memStream = new();
    private readonly string _message = File.ReadAllText("Sample-ORM.txt");
    private byte[] _bytesToRead = [];
    private MllpReader _reader = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _memStream = new();
        //let's put a bunch of copies of the message in the stream
        for (var i = 0; i < 100; i++)
        {
            var writer = new MllpWriter(_memStream);
            if (!await writer.Send(_message))
            {
                throw new("Failed to Flush"); // should never happen
            }
        }

        _bytesToRead = _memStream.ToArray();
    }

    [Benchmark(Baseline = true)]
    public async Task Setup()
    {
        _memStream = new(_bytesToRead);
        _reader = new(_memStream);
        // just don't read
    }
    
    [Benchmark] // the actual read perf is the diff between this and the baseline
    public async Task ReadMessagesSync()
    {
        _memStream = new(_bytesToRead);
        _reader = new(_memStream);
        await _reader.ReadMessagesAsync(c => { }); // note the ReadOnlySpan overload because the handler is synchronous
    }

    [Benchmark] // the actual read perf is the diff between this and the baseline
    public async Task ReadMessagesAsync()
    {
        _memStream = new(_bytesToRead);
        _reader = new(_memStream);
        await _reader.ReadMessagesAsync(c => Task.CompletedTask); // async handler gets a string
    }
    

}