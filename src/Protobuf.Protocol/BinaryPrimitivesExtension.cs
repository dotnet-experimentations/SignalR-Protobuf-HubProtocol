using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

internal static class BinaryPrimitivesExtensions
{
      public static int ReadInt32(ReadOnlySpan<byte> source)
      {
            if (BitConverter.IsLittleEndian)
            {
                  return BinaryPrimitives.ReadInt32LittleEndian(source);
            }
            return BinaryPrimitives.ReadInt32BigEndian(source);
      }

      public static byte ReadByte(ReadOnlySpan<byte> source)
      {
            return MemoryMarshal.Read<byte>(source);
      }

      public static double ReadDouble(ReadOnlySpan<byte> source)
      {
            return MemoryMarshal.Read<double>(source);
      }

      public static void WriteInt32(Span<byte> destination, int value)
      {
            if (BitConverter.IsLittleEndian)
            {
                  BinaryPrimitives.WriteInt32LittleEndian(destination, value);

            }
            else
            {
                  BinaryPrimitives.WriteInt32BigEndian(destination, value);
            }      
      }
}