namespace Phantonia.Historia.Build;

// adapted from https://stackoverflow.com/questions/3879152/how-do-i-concatenate-two-system-io-stream-instances-into-one
internal sealed class ConcatenatedStream(IEnumerable<Stream> streams) : Stream
{
    private readonly Queue<Stream> streams = new(streams);

    public override bool CanRead => true;

    public override int Read(byte[] buffer, int offset, int count)
    {
        int totalBytesRead = 0;

        while (count > 0 && streams.Count > 0)
        {
            int bytesRead = streams.Peek().Read(buffer, offset, count);
            if (bytesRead == 0)
            {
                streams.Dequeue().Dispose();
                continue;
            }

            totalBytesRead += bytesRead;
            offset += bytesRead;
            count -= bytesRead;
        }

        return totalBytesRead;
    }

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override long Length => throw new NotImplementedException();

    public override long Position
    {
        get
        {
            throw new NotImplementedException();
        }
        set
        {
            throw new NotImplementedException();
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    protected override void Dispose(bool disposing)
    {
        foreach (Stream stream in streams)
        {
            stream.Dispose();
        }
    }
}