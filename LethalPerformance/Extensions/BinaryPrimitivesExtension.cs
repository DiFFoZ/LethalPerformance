using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace LethalPerformance.Extensions;
internal static class BinaryPrimitivesExtension
{
    // In .netstandard2.1 BinaryPrimitives.WriteSingleLittleEndian doesn't exists, using code from dotnet runtime
    public static void WriteSingleLittleEndian(Span<byte> destination, float value)
    {
        if (BitConverter.IsLittleEndian)
        {
            MemoryMarshal.Write(destination, ref value);
        }
        else
        {
            var temp = BinaryPrimitives.ReverseEndianness(BitConverter.SingleToInt32Bits(value));

            MemoryMarshal.Write(destination, ref temp);
        }
    }
}
