using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Text;

namespace FastHl7;

/// <summary>
/// Helper class to parse MLLP-framed messages from a stream or PipeReader.
/// </summary>
public class MllpReader: IAsyncDisposable
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
    public MllpReader(Stream stream)
    {
        _reader = PipeReader.Create(stream);
        _ourOwnPipeReader = true;
    }

    /// <summary>
    /// Creates a new reader against the given PipeReader.  The PipeReader is not completed by this class.
    /// </summary>
    /// <param name="pipeReader"></param>
    public MllpReader(PipeReader pipeReader)
    {
        _reader = pipeReader;
        _ourOwnPipeReader = false;
    }

    /// <summary>
    /// Long-lived loop to read messages from the stream.  This will continue until the stream is closed or the cancellation token is cancelled.
    /// Each message found will be passed to the given messageHandler delegate for processing.
    /// </summary>
    /// <param name="messageHandler">Delegate to a handler for each message.  The given message must be completely processed prior to returning as buffers may be reused</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task ReadMessagesAsync( Func<ReadOnlySpan<char>, Task> messageHandler, CancellationToken cancellationToken = default)
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
                        var charBuffer = ArrayPool<char>.Shared.Rent((int)messageBytes.Length); // can only be a narrowing operation
                        var count = Encoding.UTF8.GetChars(messageBytes, charBuffer);

                        try
                        {
                            await messageHandler(charBuffer.AsSpan()[..count]);
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
        if (buffer.IsEmpty)
        {
            message = default;
            return false;
        }

        if (buffer.First.Span[0] != _vt)
        {
            throw new ProtocolViolationException("MLLP stream does not start with VT character");
        }

        var fsPosition = buffer.PositionOf(_fs); // FS, the end of message character

        if (fsPosition == null)
        {
            // we have a partial message on the buffer, wait for more data
            message = default;
            return false;
        }

        // ensure the next char after the FS is CR
        var crPosition = buffer.GetPosition(1, fsPosition.Value);
        if (crPosition.Equals(buffer.End) || buffer.Slice(crPosition).First.Span[0] != _cr)
        {
            throw new ProtocolViolationException("MLLP stream does not have CR character after FS");
        }
        
        message = buffer.Slice(1, fsPosition.Value);
        
        // Skip the header, message and the two-byte footer
        buffer = buffer.Slice(buffer.GetPosition(1, crPosition));

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