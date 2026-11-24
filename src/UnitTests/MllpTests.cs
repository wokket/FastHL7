using System.IO.Pipelines;
using FastHl7;

namespace UnitTests;

public class MllpTests
{
    [Fact]
    public async Task ReadSingleSimple()
    {
        // super simple, perfect format
        var bytes = "\vTest Message\u001c\r".Select(c => (byte)c).ToArray();

        var memStream = new MemoryStream(bytes);

        var reader = new MllpReader(memStream);

        var token = TestContext.Current.CancellationToken;
        
        var results = new List<string>();
        await reader.ReadMessagesAsync(c =>
        {
            results.Add(c.ToString());
            return Task.CompletedTask;
        }, token);

        Assert.Single(results);
        Assert.Equal("Test Message", results[0]);
    }

    [Fact]
    public async Task ReadMultipleSimple()
    {
        // super simple, perfect format
        var bytes = "\vTest Message\u001c\r\vAnother Message\u001c\r".Select(c => (byte)c).ToArray();

        var memStream = new MemoryStream(bytes);

        var reader = new MllpReader(memStream);

        var token = TestContext.Current.CancellationToken;
        
        var results = new List<string>();
        await reader.ReadMessagesAsync(c =>
        {
            results.Add(c.ToString());
            return Task.CompletedTask;
        }, token);

        Assert.Equal(2, results.Count);
        Assert.Equal("Test Message", results[0]);
        Assert.Equal("Another Message", results[1]);
    }

    [Fact]
    public async Task ReadMessagesAcceptsPayloadAtConfiguredLimit()
    {
        var reader = new MllpReader(new MemoryStream(CreateFrame("12345"u8.ToArray())), 5);
        var results = new List<string>();

        await reader.ReadMessagesAsync(message =>
        {
            results.Add(message);
            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken);

        Assert.Equal("12345", Assert.Single(results));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadMessagesRejectsPayloadAboveConfiguredLimit(bool usePipeReader)
    {
        var bytes = CreateFrame("123456"u8.ToArray());
        var reader = await CreateReader(usePipeReader, bytes, 5);
        var handlerCalled = false;

        await Assert.ThrowsAsync<System.Net.ProtocolViolationException>(() => reader.ReadMessagesAsync(_ =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken));

        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task ReadMessagesRejectsFragmentedUnterminatedPayloadAboveConfiguredLimit()
    {
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(new byte[] { 0x0B, (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5' },
            TestContext.Current.CancellationToken);
        var reader = new MllpReader(pipe.Reader, 5);
        var readTask = reader.ReadMessagesAsync(_ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await pipe.Writer.WriteAsync(new byte[] { (byte)'6' }, TestContext.Current.CancellationToken);
        await pipe.Writer.CompleteAsync();

        await Assert.ThrowsAsync<System.Net.ProtocolViolationException>(() => readTask);
    }

    [Fact]
    public async Task ReadMessagesRejectsPayloadBeforeSynchronousHandler()
    {
        var reader = new MllpReader(new MemoryStream(CreateFrame("123456"u8.ToArray())), 5);
        var handlerCalled = false;

        await Assert.ThrowsAsync<System.Net.ProtocolViolationException>(() =>
            reader.ReadMessagesAsync((ReadOnlySpan<char> _) => handlerCalled = true,
                TestContext.Current.CancellationToken));

        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task ReadMessagesRejectsPayloadAboveDefaultLimit()
    {
        var payload = new byte[10 * 1024 * 1024 + 1];
        Array.Fill(payload, (byte)'x');
        var reader = new MllpReader(new MemoryStream(CreateFrame(payload)));

        await Assert.ThrowsAsync<System.Net.ProtocolViolationException>(() =>
            reader.ReadMessagesAsync(_ => Task.CompletedTask, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void ConstructorRejectsNegativeMaximumMessageSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MllpReader(new MemoryStream(), -1));
    }

    private static async Task<MllpReader> CreateReader(bool usePipeReader, byte[] bytes, int maxMessageSizeBytes)
    {
        if (!usePipeReader)
        {
            return new MllpReader(new MemoryStream(bytes), maxMessageSizeBytes);
        }

        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(bytes, TestContext.Current.CancellationToken);
        await pipe.Writer.CompleteAsync();
        return new MllpReader(pipe.Reader, maxMessageSizeBytes);
    }

    private static byte[] CreateFrame(byte[] payload)
    {
        var frame = new byte[1 + payload.Length + 2];
        frame[0] = 0x0B;
        payload.CopyTo(frame, 1);
        frame[^2] = 0x1C;
        frame[^1] = 0x0D;

        return frame;
    }
   

    [Fact]
    public async Task ReadSimpleAcrossTwoPackets() // emulate a partial write while reading
    {
        var pipe = new Pipe();
        var token = TestContext.Current.CancellationToken;

        var writerTask = Write(pipe.Writer);

        var reader = new MllpReader(pipe.Reader);

        // simulate a partial read, then write the rest of the message

        var results = new List<string>();
        var readerTask = reader.ReadMessagesAsync(c =>
        {
            results.Add(c.ToString());
            return Task.CompletedTask;
        }, token);

        await Task.WhenAll(readerTask, writerTask);
        
        Assert.Single(results);
        Assert.Equal("Test Message", results[0]);
        return;

        async Task Write(PipeWriter writer)
        {
            
            var stream = writer.AsStream();
            var streamWriter = new StreamWriter(stream);

            await streamWriter.WriteAsync("\vTest "); // start the message, emulate a first packet
            await streamWriter.FlushAsync(token);
            await Task.Delay(100, token); // wait a moment before finishing the message
            await streamWriter.WriteAsync("Message\u001c\r"); // finish the message, emulate a second packet
            
            await streamWriter.FlushAsync(token);
            await writer.CompleteAsync(); // so the reader loop completes
        }
    }
    
    [Fact]
    public async Task SendSimple()
    {
        var memStream = new MemoryStream(); // so we can inspect the output

        var writer = new MllpWriter(memStream);
        await writer.Send("Test Message");
        var content = memStream.ToArray();

        Assert.Equal("Test Message".Length + 3, memStream.Position);
        Assert.Equal("\vTest Message\u001c\r", System.Text.Encoding.UTF8.GetString(content));
    }

    [Theory]
    [InlineData(true, 0x06)]
    [InlineData(false, 0x15)]
    public async Task SendSimpleAcknowledgementWritesFrame(bool ack, byte acknowledgement)
    {
        using var stream = new MemoryStream();
        await using var writer = new MllpWriter(stream);

        var canContinue = ack ? await writer.SendSimpleAck() : await writer.SendSimpleNak();

        Assert.True(canContinue);
        Assert.Equal(new byte[] { 0x0B, acknowledgement, 0x1C, 0x0D }, stream.ToArray());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendSimpleAcknowledgementReturnsFalseWhenReaderCompleted(bool ack)
    {
        var pipe = new Pipe();
        await pipe.Reader.CompleteAsync();
        await using var writer = new MllpWriter(pipe.Writer);

        var canContinue = ack ? await writer.SendSimpleAck() : await writer.SendSimpleNak();

        Assert.False(canContinue);
        await pipe.Writer.CompleteAsync();
    }
    
    [Fact]
    public async Task ReadAndWriteSimple()
    {
        var pipe = new Pipe();

        var writer = new MllpWriter(pipe.Writer);
        await writer.Send("Test Message");

        var reader = new MllpReader(pipe.Reader);

        var token = TestContext.Current.CancellationToken;

        var results = new List<string>();
        var readTask = reader.ReadMessagesAsync(c =>
        {
            results.Add(c.ToString());
        }, token);

        await pipe.Writer.CompleteAsync(); // so the reader loop completes
        await readTask;


        Assert.Single(results);
        Assert.Equal("Test Message", results[0]);
    }


    [Fact]
    public async Task ReadAndWriteMultiple()
    {
        var pipe = new Pipe();

        var writer = new MllpWriter(pipe.Writer);
        await writer.Send("Test Message");

        var reader = new MllpReader(pipe.Reader);

        var token = TestContext.Current.CancellationToken;

        var results = new List<string>();
        var readTask = reader.ReadMessagesAsync(c =>
        {
            results.Add(c);
            return Task.CompletedTask;
        }, token);

        await writer.Send("Another Message"); // after reading has started

        await pipe.Writer.CompleteAsync(); // so the reader loop completes
        await readTask;


        Assert.Equal(2, results.Count);
        Assert.Equal("Test Message", results[0]);
        Assert.Equal("Another Message", results[1]);
    }
}