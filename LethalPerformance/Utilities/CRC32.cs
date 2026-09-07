namespace LethalPerformance.Utilities;
internal static class CRC32
{
    private static readonly uint[] s_CrcTable = CreateCrcTable();

    public static uint Crc32Ascii(string value)
    {
        uint crc = 0xFFFFFFFF;
        for (var i = 0; i < value.Length; i++)
        {
            crc = s_CrcTable[(crc ^ (byte)value[i]) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ 0xFFFFFFFF;
    }

    // audio::mixer::GetExposedPropertyIndex 
    // or zlib crc32
    private static uint[] CreateCrcTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var crc = i;
            for (var b = 0; b < 8; b++)
            {
                crc = (crc & 1) != 0
                    ? (crc >> 1) ^ 0xEDB88320u
                    : crc >> 1;
            }

            table[i] = crc;
        }

        return table;
    }
}
