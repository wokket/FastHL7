using System.Buffers;
using System.Text;
using System.IO.Pipelines;

namespace FastHl7;

/// <summary>
/// Helper to write Mllp-framed messages to a stream or PipeWriter
/// </summary>
public class MllpWriter : IAsyncDisposable
{
    private readonly bool _ourPipeWriter;
    private readonly PipeWriter _writer;

    // MLLP framing characters
    private const byte _headerBytes = 0x0B; // Vertical Tab - Start of message
    private static readonly byte[] _footerBytes = [0x1C, 0x0D]; // File Separator, Carriage Return
    
    /// <summary>
    /// Create a new writer against the given stream.  The stream is not disposed by this class but the underlying PipeWriter is completed.
    /// </summary>
    /// <param name="stream"></param>
    public MllpWriter(Stream stream)
    {
        _writer = PipeWriter.Create(stream);
        _ourPipeWriter = true;
    }

    /// <summary>
    /// Create a new writer against the given PipeWriter.  The PipeWriter is not completed by this class.
    /// </summary>
    /// <param name="writer"></param>
    public MllpWriter(PipeWriter writer)
    {
        _writer = writer;
        _ourPipeWriter = false;
    }

    /// <summary>
    /// Send the given message, framed with MLLP characters.
    /// </summary>
    /// <param name="message"></param>
    /// <returns>true if we're good to continue writing, or false if the underlying reader has terminated and we should stop writing.</returns>
    public async Task<bool> Send(ReadOnlySequence<char> message)
    {
        _writer.Write([_headerBytes]);
        Encoding.UTF8.GetBytes(message, _writer); // write the conversion directly into the PipeWriter, no allocs
        _writer.Write(_footerBytes);
        var flushResult = await _writer.FlushAsync();
        return !flushResult.IsCompleted;
    }
    
    /// <summary>
    /// Send the given message, framed with MLLP characters.
    /// </summary>
    /// <param name="message"></param>
    /// <returns>true if we're good to continue writing, or false if the underlying reader has terminated and we should stop writing.</returns>
    public async Task<bool> Send(string message)
    {
        _writer.Write([_headerBytes]);
        Encoding.UTF8.GetBytes(message, _writer); // write the conversion directly into the PipeWriter, no allocs
        _writer.Write(_footerBytes);
        
        var flushResult = await _writer.FlushAsync();
        return !flushResult.IsCompleted;
    }

    private static readonly byte[] _simpleAck = [0x0B,0x06,0x1C,0x0D];
    private static readonly byte[] _simpleNak = [0x0B,0x15,0x1C,0x0D];
    /// <summary>
    /// Sends a MllpV2 simple ACK (0x06)
    /// </summary>
    /// <returns></returns>
    public async Task<bool> SendSimpleAck()
    {
        var result = await _writer.WriteAsync(_simpleAck);
        return !result.IsCompleted;
    }
    
    /// <summary>
    /// Sends a MllpV2 simple NAK (0x15)
    /// </summary>
    /// <returns></returns>
    public async Task<bool> SendSimpleNak()
    {
        var result = await _writer.WriteAsync(_simpleAck);
        return !result.IsCompleted;
    }

    /// <summary>
    /// Asynchronously dispose of this writer, completing the underlying PipeWriter if needed.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        return _ourPipeWriter ? _writer.CompleteAsync() : ValueTask.CompletedTask;
    }
}