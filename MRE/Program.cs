// See https://aka.ms/new-console-template for more information

using System.IO.Compression;
using MRE;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var arr = new List<byte>();
        var zipStream = new ZipStream();
        var bytes = await zipStream.Compress("test2.csv", ReadChunks("../../../../MRE/files/test1.csv"));
        await File.WriteAllBytesAsync("../../../../MRE/files/compress_test.zip", bytes);
    }
    private static async IAsyncEnumerable<byte[]> ReadChunks(string path)
    {
        var chunkSize = 100;
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var bytesRemaining = stream.Length;
            while (bytesRemaining > 0)
            {
                var size = Math.Min((int)bytesRemaining, chunkSize);
                var buffer = new byte[size];
                var bytesRead = stream.Read(buffer, 0, size);
                if (bytesRead <= 0)
                    break;
                yield return buffer;
                bytesRemaining -= bytesRead;
            }
        }
    }
}