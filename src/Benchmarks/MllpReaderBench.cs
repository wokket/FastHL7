using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Text;
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
| Method                   | Mean         | Ratio  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------- |-------------:|-------:|-------:|-------:|----------:|------------:|
| Setup                    |     23.84 ns |   1.00 | 0.0162 |      - |     280 B |        1.00 |                                                                                                                                                                                  
| ReadMessagesSync         |  8,088.34 ns | 339.33 | 0.0916 |      - |    1672 B |        5.97 |
| ReadMessagesAsync        | 14,431.80 ns | 605.46 | 9.5978 | 0.0153 |  165672 B |      591.69 |
| ReadMessagesSyncSequence |  8,668.72 ns | 363.68 | 0.0916 |      - |    1672 B |        5.97 |
 */


    private MemoryStream _memStream = new();
    private readonly string _message = File.ReadAllText("Sample-ORM.txt");
    private byte[] _bytesToRead = [];
    private MllpReader _reader = null!;
    private MllpSequenceReader _sequenceReader = null!;

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
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task Setup()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
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
    
    [Benchmark] // the actual read perf is the diff between this and the baseline
    public async Task ReadMessagesSyncSequence()
    {
        _memStream = new(_bytesToRead);
        _sequenceReader = new(_memStream);
        await _sequenceReader.ReadMessagesAsync(c => { }); // note the ReadOnlySpan overload because the handler is synchronous
    }
    
    
    
    
/// <summary>
/// A version that uses SequenceReader 
/// </summary>
internal class MllpSequenceReader : IAsyncDisposable
{
    private readonly bool _ourOwnPipeReader;
    private readonly PipeReader _reader;

    // MLLP framing characters
    private const byte _vt = 0x0B; // Start of message
    private const byte _fs = 0x1C; // End of message
    private const byte _cr = 0x0D; // Carriage return

    /// <summary>
    /// Creates a new reader against the given stream.  The stream is not disposed by this class.
    /// </summary>
    /// <param name="stream"></param>
    public MllpSequenceReader(Stream stream)
    {
        _reader = PipeReader.Create(stream);
        _ourOwnPipeReader = true;
    }

    /// <summary>
    /// Creates a new reader against the given PipeReader.  The PipeReader is not completed by this class.
    /// </summary>
    /// <param name="pipeReader"></param>
    public MllpSequenceReader(PipeReader pipeReader)
    {
        _reader = pipeReader;
        _ourOwnPipeReader = false;
    }

    /// <summary>
    /// Long-lived loop to read messages from the stream.  This will continue until the stream is closed or the cancellation token is cancelled.
    /// Each message found will be passed to the given messageHandler delegate for processing.  Note this overload does NOT accept an async handler as the lightweight ReadOnlySpan cannot survive calls to await.
    /// If you require an async handler use the overload that returns a String instead, and pay the allocation penalty.
    /// </summary>
    /// <param name="messageHandler">Delegate to a handler for each message.  The given message must be completely processed prior to returning as buffers may be reused.  </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task ReadMessagesAsync(Action<ReadOnlySpan<char>> messageHandler,
        CancellationToken cancellationToken = default)
    {
        Task LocalMessageHandler(char[] msg, int count)
        {
            messageHandler(msg.AsSpan()[..count]); // synchronous handler
            return Task.CompletedTask;
        }


        await ReadMessagesAsyncInternal(LocalMessageHandler, cancellationToken);
    }

    /// <summary>
    /// Long-lived loop to read messages from the stream.  This will continue until the stream is closed or the cancellation token is cancelled.
    /// Each message found will be passed to the given messageHandler delegate for processing.  Note this overload does NOT accept an async handler as the lightweight ReadOnlySpan cannot survive calls to await.
    /// If you require an async handler use the overload that returns a String instead, and pay the allocation penalty.
    /// </summary>
    /// <param name="messageHandler">Delegate to a handler for each message.  The given message must be completely processed prior to returning as buffers may be reused.  </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task ReadMessagesAsync(Func<string, Task> messageHandler,
        CancellationToken cancellationToken = default)
    {
        Task LocalMessageHandler(char[] msg, int count)
        {
            var msgAsString = new String(msg.AsSpan()[..count]);
            return messageHandler(msgAsString); // asynchronous handler
        }

        await ReadMessagesAsyncInternal(LocalMessageHandler, cancellationToken);
    }


    private async Task ReadMessagesAsyncInternal(Func<char[], int, Task> handler,
        CancellationToken cancellationToken = default)
    {
        try
        {
            while (true)
            {
                var result = await _reader.ReadAsync(cancellationToken);
                var buffer = result.Buffer;

                try
                {
                    if (result.IsCanceled)
                    {
                        break; // The read was canceled. We can quit without reading the existing data.
                    }

                    // Process all messages from the buffer, modifying the input buffer on each iteration.
                    while (TryParseMessage(ref buffer, out var messageBytes))
                    {
                        var charBuffer =
                            ArrayPool<char>.Shared.Rent((int)messageBytes.Length); // can only be a narrowing operation
                        var count = Encoding.UTF8.GetChars(messageBytes, charBuffer);

                        try
                        {
                            await handler(charBuffer, count);
                        }
                        finally
                        {
                            ArrayPool<char>.Shared.Return(charBuffer);
                        }
                    }

                    // There's no more data to be processed, socket is closed.
                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
                finally
                {
                    // Since all messages in the buffer are being processed, we can use the
                    // remaining buffer's Start and End position to determine consumed and examined.
                    _reader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
        }
        finally
        {
            await _reader.CompleteAsync();
        }
    }

    private static bool TryParseMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> message)
    {
        // Look for the header in the buffer, it should be the first char in the buffer, otherwise things are whacky
        var reader = new SequenceReader<byte>(buffer);
        if (!reader.TryRead(out var firstByte)) //empty buffer
        {
            message = default;
            return false;
        }
        
        
        if (firstByte != _vt) {
            throw new ProtocolViolationException("MLLP stream does not start with VT character");
        }


        if (!reader.TryReadTo(out message, _fs))
        {
            // we have a partial message on the buffer, wait for more data
            message = default;
            return false;
        }
        
        
        // ensure the next char after the FS is CR
        if (!reader.TryRead(out var crByte) || crByte != _cr)
        {
            throw new ProtocolViolationException("MLLP stream does not have CR character after FS");
        }
        
        //if here we successfully read a whole MLLP frame, advance the buffer to after the CR
        buffer = buffer.Slice(reader.Position);

        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        return _ourOwnPipeReader ? _reader.CompleteAsync() : ValueTask.CompletedTask;
    }
}
    
    

}