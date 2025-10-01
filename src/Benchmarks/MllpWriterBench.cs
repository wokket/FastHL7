using BenchmarkDotNet.Attributes;
using FastHl7;

namespace Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class MllpWriterBench
{
    
    // nothing to compare to, just a benchmark to see how fast it is (and ensure it's 0 alloc)
    
/*
| Method       | Mean     | Error    | StdDev   | Allocated |
|------------- |---------:|---------:|---------:|----------:|
| WriteMessage | 97.04 ns | 7.957 ns | 0.436 ns |         - 
 */
 
    private readonly MemoryStream _memStream = new();
    private readonly MllpWriter _writer;
    private readonly string _message = File.ReadAllText("Sample-ORM.txt");
   
    public  MllpWriterBench()
    {
        _writer = new(_memStream);
    }
    
    
    [Benchmark]
    public async Task WriteMessage()
    {
        _memStream.SetLength(0); // reset
        await _writer.Send(_message);
    }
    
}