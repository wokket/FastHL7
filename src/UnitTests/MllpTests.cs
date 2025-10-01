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
            return Task.CompletedTask;
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
            results.Add(c.ToString());
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