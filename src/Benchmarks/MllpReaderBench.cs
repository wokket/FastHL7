using BenchmarkDotNet.Attributes;
using FastHl7;

namespace Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
[HideColumns("BuildConfiguration", "Error", "StdDev", "RatioSD")]
public class MllpReaderBench
{
    // The Setup just news up the objects, the actual read perf is the diff between the baseline and the ReadMessages bench. 
    // We read a stream with 100 copies of the same message in it so our amortised allocs are about 13 bytes per message.

/*
| Method       | Mean        | Ratio  | Gen0   | Allocated | Alloc Ratio |
|------------- |------------:|-------:|-------:|----------:|------------:|
| Setup        |    24.24 ns |   1.00 | 0.0162 |     280 B |        1.00 |
| ReadMessages | 8,279.52 ns | 341.57 | 0.0916 |    1584 B |        5.66 |
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
    public async Task ReadMessages()
    {
        _memStream = new(_bytesToRead);
        _reader = new(_memStream);
        await _reader.ReadMessagesAsync(c => Task.CompletedTask);
    }
}