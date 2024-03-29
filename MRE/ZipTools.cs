using System.Text;

namespace MRE;

public static class ZipTools
{
    private const int ValidZipDate_YearMin = 1980;
    private const int ValidZipDate_YearMax = 2107;
    public static byte[] GetLocalFileHeaderEntry(string fileName)
    {
        var header = new List<byte>();
        // header 4 bytes
        header.AddRange(Constants.LocalFileHeaderSignature);
        // extract versiom zip64 2 bytes
        header.AddRange(BitConverter.GetBytes((ushort)45));
        // GeneralPurposeBitFlag 2 bytes
        header.AddRange(BitConverter.GetBytes((ushort)0));
        // compression method = deflate 2 bytes
        header.AddRange(BitConverter.GetBytes((ushort)8));
        // last mod date and time 2 bytes + 2 bytes
        header.AddRange(BitConverter.GetBytes(DateTimeToDosTime(DateTime.UtcNow)));
        // crc32 4bytes
        header.AddRange(BitConverter.GetBytes(0));
        // uncompressedSize 4bytes
        header.AddRange(BitConverter.GetBytes(-1));
        // compressedSize 4bytes
        header.AddRange(BitConverter.GetBytes(-1));
        // fileNameLength 2bytes
        var byteFileName = Encoding.UTF8.GetBytes(fileName);
        byte[] fileNameLength;
        if (byteFileName.Length > 255)
        {

            var oddment = byteFileName.Length - 255;
            if (oddment > 255)
                throw new Exception("File name is too long");
            fileNameLength = new byte[] { 255, BitConverter.GetBytes(oddment).First() };
        }
        else
        {
            fileNameLength = new byte[] { BitConverter.GetBytes(byteFileName.Length).First(), 0};
        }
        header.AddRange(fileNameLength);
        // extraFieldLength 2bytes
        var extraInfo = GetLfhExtraInfo();
        header.AddRange(BitConverter.GetBytes((ushort) extraInfo.Length));
        // Имя файла. произвольный размер
        header.AddRange(byteFileName);
        // extraFields
        header.AddRange(extraInfo);
        return header.ToArray();
    }

    public static byte[] GetCentralDirectoryEntry(
        string fileName,
        uint crc32,
        ulong localFileHeaderLength,
        ulong compressedSize,
        ulong uncompressedSize)
    {
        var entry = new List<byte>();
        entry.AddRange(Constants.CentralFileHeaderSignature);
        // CreateVersion. for zip64 2 bytes
        entry.AddRange(BitConverter.GetBytes((ushort)45));
        // ExtractVersion. for zip64 2 bytes
        entry.AddRange(BitConverter.GetBytes((ushort)45));
        // GeneralPurposeBitFlag 2 bytes
        entry.AddRange(BitConverter.GetBytes((ushort)0));
        // compression method = deflate 2 bytes
        entry.AddRange(BitConverter.GetBytes((ushort)8));
        // last mod date and time 2 bytes + 2 bytes
        entry.AddRange(BitConverter.GetBytes(DateTimeToDosTime(DateTime.UtcNow)));
        // crc32 4bytes
        entry.AddRange(BitConverter.GetBytes(crc32));
        // uncompressedSize 4bytes
        entry.AddRange(BitConverter.GetBytes(-1));
        // compressedSize 4bytes
        entry.AddRange(BitConverter.GetBytes(-1));

        // fileNameLength 2bytes
        var byteFileName = Encoding.UTF8.GetBytes(fileName);
        byte[] fileNameLength;
        if (byteFileName.Length > 255)
        {

            var oddment = byteFileName.Length - 255;
            if (oddment > 255)
                throw new Exception("File name is too long");
            fileNameLength = new byte[] { 255, BitConverter.GetBytes(oddment).First() };
        }
        else
        {
            fileNameLength = new byte[] { BitConverter.GetBytes(byteFileName.Length).First(), 0};
        }
        entry.AddRange(fileNameLength);
        var zip64ExtraInfo = GetZip64CdExtraInfo(compressedSize, uncompressedSize);
        entry.AddRange(BitConverter.GetBytes((ushort) zip64ExtraInfo.Length));
        // filecomment length 2 bytes
        entry.AddRange(BitConverter.GetBytes((ushort)0));
        // disknumber 2 bytes
        entry.AddRange(BitConverter.GetBytes((ushort)0));
        // internal attributes 2 bytes
        entry.AddRange(BitConverter.GetBytes((ushort)0));
        // external attributes 4 bytes
        entry.AddRange(BitConverter.GetBytes(0));
        // lfh offset 4 bytes
        entry.AddRange(BitConverter.GetBytes(-1));
        // filename
        entry.AddRange(byteFileName);
        // extrainfo
        entry.AddRange(zip64ExtraInfo);

        var sizeOfCd = entry.Count;
        //zip64 part
        entry.AddRange(GetZip64EoCDRecord((ulong) entry.Count, localFileHeaderLength, 1));
        // eocdrecord
        entry.AddRange(GetEoCdRecord(sizeOfCd, (int) localFileHeaderLength));
        return entry.ToArray();
    }

    /// <summary>
    /// create zip64eocdrecord
    /// </summary>
    /// <param name="cdFhLength">size of centraldirectory file header with data. from signature 0x02014b50 till 0ч06064b50</param>
    /// <param name="dataSize">size of LFH + compressed data - from start till 0x02014b50</param>
    /// <param name="numberOfEntries">Number of entries</param>
    /// <returns>zip64eocdrecord in bytes</returns>
    public static byte[] GetZip64EoCDRecord(ulong cdFhLength, ulong dataSize, int numberOfEntries)
    {
        var record = new List<byte>();
        // signature 4bytes
        record.AddRange(Constants.Zip64EndOfCentralDirectorySignature);
        // size without first 12 bytes (fixed) 8bytes
        record.AddRange(BitConverter.GetBytes((long) Constants.Zip64EndOfCentralDirectoryByteLenghtWithoutUncountableBytes));
        // Версия для создания. Ставим сразу же версию для zip64 2 bytes
        record.AddRange(BitConverter.GetBytes((ushort)45));
        // Версия для извлечения. Ставим сразу же версию для zip64 2 bytes
        record.AddRange(BitConverter.GetBytes((ushort)45));
        // diskNumber 4 bytes
        record.AddRange(BitConverter.GetBytes(0));
        // diskNumber with cd start 4 bytes
        record.AddRange(BitConverter.GetBytes(0));
        // total number of entries in cd on this disk 8 bytes
        record.AddRange(BitConverter.GetBytes((ulong) numberOfEntries));
        // total number of entries in cd 8 bytes
        record.AddRange(BitConverter.GetBytes((ulong) numberOfEntries));
        // size of cd 8 bytes
        record.AddRange(BitConverter.GetBytes(cdFhLength));
        // offset from start file 8 bytes
        record.AddRange(BitConverter.GetBytes(dataSize));
        // add zip64eocdlocator
        record.AddRange(GetZip64EoCDLocator(dataSize + cdFhLength));
        return record.ToArray();
    }

    /// <summary>
    /// creating zip64eocdlocator
    /// </summary>
    /// <param name="offsetOfZip64EoCdRecord">offset from start till 0x06064b50</param>
    /// <returns>zip64eocdlocator in bytes</returns>
    public static byte[] GetZip64EoCDLocator(ulong offsetOfZip64EoCdRecord)
    {
        var record = new List<byte>();
        // signature 4bytes
        record.AddRange(Constants.Zip64EndOfCentralDirectoryLocatorSignature);
        // number of the disk with the start zip64eocd 4bytes
        record.AddRange(BitConverter.GetBytes(0));
        // relative offset of the zip64 end of central directory record 8 bytes (files + regular cd)
        record.AddRange(BitConverter.GetBytes(offsetOfZip64EoCdRecord));
        // total number of disks 4 bytes
        record.AddRange(BitConverter.GetBytes(1));
        return record.ToArray();
    }

    private static byte[] GetZip64CdExtraInfo(
        ulong compressedSize,
        ulong uncompressedSize)
    {
        var info = new List<byte>();
        // zip64 extra signature
        info.AddRange(Constants.Zip64LFHSignature);
        // zip64 extra block size (fixed) 2 bytes
        info.AddRange(BitConverter.GetBytes((ushort) 28));
        // original size 8bytes
        info.AddRange(BitConverter.GetBytes(uncompressedSize));
        // compressed size 8 bytes
        info.AddRange(BitConverter.GetBytes(compressedSize));
        // relative header offset 8 bytes
        info.AddRange(BitConverter.GetBytes(0ul));
        // disk number start 4 bytes
        info.AddRange(BitConverter.GetBytes(0));

        // NTFS section 2 bytes
        info.AddRange(Constants.Zip64NTFSSignature);
        // NTFS section size (fixed) 2 bytes
        info.AddRange(BitConverter.GetBytes((ushort) 32));
        // NTFS reserved bytes 4 bytes
        info.AddRange(BitConverter.GetBytes(0));
        // tag 2bytes
        info.AddRange(Constants.Zip64NTFSTagSignature);
        // tag info length
        info.AddRange(Constants.Zip64NTFSTagLength);
        // time info (modified time, access time, create time) 24 bytes
        info.AddRange(BitConverter.GetBytes((ulong) DateTimeToDosTime(DateTime.UtcNow)));
        info.AddRange(BitConverter.GetBytes((ulong) DateTimeToDosTime(DateTime.UtcNow)));
        info.AddRange(BitConverter.GetBytes((ulong) DateTimeToDosTime(DateTime.UtcNow)));
        return info.ToArray();
    }

    private static byte[] GetEoCdRecord(int sizeOfCd, int startOfCdOffset)
    {
        var record = new List<byte>();
        // signature 4bytes
        record.AddRange(Constants.EndOfCentralDirectorySignature);
        // number of the disk 2 bytes
        record.AddRange(BitConverter.GetBytes((ushort) 0));
        // number of the disk with the start cd 2 bytes
        record.AddRange(BitConverter.GetBytes((ushort) 0));
        // total number of entries in the cd on this disk 2 bytes
        record.AddRange(BitConverter.GetBytes(ushort.MaxValue));
        // total number of entries in the cd 2 bytes
        record.AddRange(BitConverter.GetBytes(ushort.MaxValue));
        // size of cd 4 bytes
        record.AddRange(BitConverter.GetBytes(sizeOfCd));
        // offset of start cd 4 bytes
        record.AddRange(BitConverter.GetBytes(startOfCdOffset));
        // comment length 2 bytes
        record.AddRange(BitConverter.GetBytes((ushort) 0));
        return record.ToArray();
    }

    private static byte[] GetLfhExtraInfo()
    {
        var info = new List<byte>();
        // extraFields
        info.AddRange(Constants.Zip64LFHSignature); // signature zip64 lfh
        info.AddRange(Constants.Zip64LFHSize); // size of zip64 lfh fields AFTER those bytes
        info.AddRange(Constants.Zip64EmptySize); // uncompressedSize - zeros because we have it in cd
        info.AddRange(Constants.Zip64EmptySize); // compressedSize - zeros because we have it in cd

        // NTFS section 2 bytes
        info.AddRange(Constants.Zip64NTFSSignature);
        // NTFS section size (fixed) 2 bytes
        info.AddRange(BitConverter.GetBytes((ushort) 32));
        // NTFS reserved bytes 4 bytes
        info.AddRange(BitConverter.GetBytes(0));
        // tag 2bytes
        info.AddRange(Constants.Zip64NTFSTagSignature);
        // tag info length
        info.AddRange(Constants.Zip64NTFSTagLength);
        // time info (modified time, access time, create time) 24 bytes
        info.AddRange(BitConverter.GetBytes((ulong) DateTimeToDosTime(DateTime.UtcNow)));
        info.AddRange(BitConverter.GetBytes((ulong) DateTimeToDosTime(DateTime.UtcNow)));
        info.AddRange(BitConverter.GetBytes((ulong) DateTimeToDosTime(DateTime.UtcNow)));
        return info.ToArray();
    }

    private static uint DateTimeToDosTime(DateTime dateTime)
    {
        // DateTime must be Convertible to DosTime:
        if(dateTime.Year is <= ValidZipDate_YearMin or >= ValidZipDate_YearMax)
        {
            throw new Exception("Date is not valid");
        }

        var ret = ((dateTime.Year - ValidZipDate_YearMin) & 0x7F);
        ret = (ret << 4) + dateTime.Month;
        ret = (ret << 5) + dateTime.Day;
        ret = (ret << 5) + dateTime.Hour;
        ret = (ret << 6) + dateTime.Minute;
        ret = (ret << 5) + (dateTime.Second / 2); // only 5 bits for second, so we only have a granularity of 2 sec.
        return (uint)ret;
    }
}