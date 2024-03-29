namespace MRE;

public static class Constants
{
    public static readonly byte[] LocalFileHeaderSignature = { 0x50, 0x4b, 0x03, 0x04 };
    public static readonly byte[] CentralFileHeaderSignature = { 0x50, 0x4b, 0x01, 0x02 };
    public static readonly byte[] EndOfCentralDirectorySignature = { 0x50, 0x4b, 0x05, 0x06 };
    public static readonly byte[] Zip64EndOfCentralDirectorySignature = { 0x50, 0x4b, 0x06, 0x06 };
    public static readonly byte[] Zip64EndOfCentralDirectoryLocatorSignature = { 0x50, 0x4b, 0x06, 0x07 };
    public static readonly byte[] Zip64EmptySize = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
    public static readonly byte[] Zip64LFHSignature = { 0x01, 0x00 };
    public static readonly byte[] Zip64LFHSize = { 0x10, 0x00 };
    public static readonly byte[] Zip64NTFSSignature = { 0x0a, 0x00 };
    public static readonly byte[] Zip64NTFSTagSignature = { 0x01, 0x00 };
    public static readonly byte[] Zip64NTFSTagLength = { 0x18, 0x00 };

    public const int SignatureStart = 80;
    public const int Zip64EndOfCentralDirectoryByteLenght = 56;
    public const int Zip64EndOfCentralDirectoryByteLenghtWithoutUncountableBytes = 44;
    public const int Zip64EndOfCentralDirectoryLocatorByteLength = 20;
    public const int EndOfCentralDirectoryRecordByteLength = 22;

    public const int ZipInternalBufferSize = 131_072;
    public const int ZipBufferSize = 262_144;
}