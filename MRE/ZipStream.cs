using System.IO.Compression;

namespace MRE;


// Dear folks. Uncommented code is working, but only when we have all file data in memory. The problem is in commented code.

public class ZipStream
{
    private ulong _originalSize;
    private ulong _compressedSize;
    public List<byte> testResult = new List<byte>();
    private uint _crc32;

    private MemoryStream? _compressStream;
    private DeflateStream? _deflateStream;

    public ZipStream()
    {
        _compressStream = new MemoryStream();
        _deflateStream = new DeflateStream(_compressStream, CompressionMode.Compress, true);
    }

    // this is how it should work but it doesn't
    // public async Task<byte[]> Compress(string fileName, IAsyncEnumerable<byte[]> data)
    // {
    //     var crc32Helper = new System.IO.Hashing.Crc32();
    //     var lfh = ZipTools.GetLocalFileHeaderEntry(fileName);
    //     testResult.AddRange(lfh);
    //     var bytearray = new List<byte>();
    //     await foreach (var chunk in data)
    //     {
    //         _originalSize += (ulong) chunk.Length;
    //         var compressedData = Compress(chunk);
    //
    //         var buffer = _compressStream!.GetBuffer();
    //         Array.Clear(buffer, 0, buffer.Length);
    //         _compressStream!.Position = 0;
    //         _compressStream!.SetLength(0);
    //
    //         _compressedSize += (ulong) compressedData.Length;
    //         crc32Helper.Append(chunk);
    //         testResult.AddRange(compressedData);
    //     }
    //     _originalSize += (ulong) bytearray.Count;
    //
    //     _crc32 = crc32Helper.GetCurrentHashAsUInt32();
    //     var cd = ZipTools.GetCentralDirectoryEntry(
    //         fileName,
    //         _crc32,
    //         (ulong) lfh.Length + _compressedSize,
    //         _compressedSize,
    //         _originalSize);
    //     testResult.AddRange(cd);
    //     return testResult.ToArray();
    // }
    
    
    
    // That works but it requires to store all data in memory
    public async Task<byte[]> Compress(string fileName, IAsyncEnumerable<byte[]> data)
    {
        var crc32Helper = new System.IO.Hashing.Crc32();
        var lfh = ZipTools.GetLocalFileHeaderEntry(fileName);
        testResult.AddRange(lfh);
        var byteList = new List<byte>();
        await foreach (var chunk in data)
        {
            byteList.AddRange(chunk);
        }

        var compressedData = Compress(byteList.ToArray());
        _compressedSize += (ulong)compressedData.Length;
        crc32Helper.Append(byteList.ToArray());
        _originalSize += (ulong) byteList.Count;
        testResult.AddRange(compressedData);
    
        _crc32 = crc32Helper.GetCurrentHashAsUInt32();
        var cd = ZipTools.GetCentralDirectoryEntry(
            fileName,
            _crc32,
            (ulong) lfh.Length + _compressedSize,
            _compressedSize,
            _originalSize);
        testResult.AddRange(cd);
        return testResult.ToArray();
    }

    // works only when we have all data in memory which is not good
    public byte[] Compress(byte[] data)
    {
        byte[] retVal;
        using (MemoryStream compressedMemoryStream = new MemoryStream())
        {
            DeflateStream compressStream = new DeflateStream(compressedMemoryStream, CompressionMode.Compress, true);
            compressStream.Write(data, 0, data.Length);
            compressStream.Close();
            retVal = new byte[compressedMemoryStream.Length];
            compressedMemoryStream.Position = 0L;
            compressedMemoryStream.Read(retVal, 0, retVal.Length);
            compressedMemoryStream.Close();
            compressStream.Close();
        }
        return retVal;
    }

    // not working compress with single deflater
    // public byte[] Compress(byte[] data)
    // {
    //     using var input = new MemoryStream(data);
    //     input.CopyTo(_deflateStream!);
    //     _deflateStream?.Flush();
    //
    //     return _compressStream!.ToArray();
    // }
}