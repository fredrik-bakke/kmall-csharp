using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Freidrich.Utils
{
    public static class BinaryUtils
    {
        // Safe version:
        ///// <summary>
        ///// Casts a byte array to the structure <typeparamref name="T"/> using the structure's byte-order.
        ///// <see href="https://stackoverflow.com/a/2887"/>
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="bytes"></param>
        ///// <returns></returns>
        //public static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        //{
        //    GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        //    try
        //    {
        //        return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        //    }
        //    finally
        //    {
        //        handle.Free();
        //    }
        //}

        /// <summary>
        /// Casts a byte array to the structure <typeparamref name="T"/> using the structure's byte-order.
        /// <see href="https://stackoverflow.com/a/2887"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static unsafe T ByteArrayToStructure<T>(byte[] bytes, int start = 0) where T : struct
        {
            fixed(byte* ptr = &bytes[start])
            {
                return (T)Marshal.PtrToStructure((IntPtr)ptr, typeof(T));
            }
        }

        //Safe version:
        /// <summary>
        /// Casts a structure <typeparamref name="T"/> to a byte array using the structure's byte-order.
        /// <see href="https://stackoverflow.com/a/2887"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="byteSize">Optional argument of size of byte-array.
        /// If <paramref name="byteSize"/> is smaller than the byte size of <typeparamref name="T"/> the data will be truncated. If <paramref name="byteSize"/> is larger, the array will be padded with zeroes.</param>
        /// <returns></returns>
        public static byte[] StructureToByteArray<T>(T source, int byteSize = int.MaxValue) where T : struct
        {
            int TSize = Marshal.SizeOf<T>();
            if (byteSize == int.MaxValue) byteSize = TSize;

            byte[] bytes = new byte[byteSize];

            IntPtr handle = Marshal.AllocHGlobal(TSize);

            try
            {
                Marshal.StructureToPtr(source, handle, true);
                Marshal.Copy(handle, bytes, 0, Math.Min(byteSize, TSize));

                return bytes;
            }
            finally
            {
                Marshal.FreeHGlobal(handle);
            }
        }


        public static T[] ByteArrayToStructureArray<T>(byte[] source, int start = 0, int length = int.MaxValue) where T : struct
        {
            int end = Math.Min(source.Length - start, length) + start;
            length = end - start;

            T[] res = new T[length / Marshal.SizeOf<T>()];

            GCHandle handle = GCHandle.Alloc(res, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(source, start, handle.AddrOfPinnedObject(), length);
            }
            finally
            {
                handle.Free();
            }

            return res;
        }

        public static byte[] StructureArrayToByteArray<T>(T[] source, int start = 0, int length = int.MaxValue) where T : struct
        {
            int end = Math.Min(source.Length - start, length) + start;
            length = end - start;

            byte[] bytes = new byte[length * Marshal.SizeOf<T>()];

            GCHandle handle = GCHandle.Alloc(source, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(handle.AddrOfPinnedObject(), bytes, 0, bytes.Length);
            }
            finally
            {
                handle.Free();
            }

            return bytes;
        }


        /**************** READ ****************/

        /// <summary>
        /// Reads bytes from <paramref name="reader"/> and casts to a structure.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static T ReadStructure<T>(this BinaryReader reader) where T : struct => ByteArrayToStructure<T>(reader.ReadBytes(Marshal.SizeOf<T>()));

        ///// <summary>
        ///// Reads bytes from <paramref name="stream"/> and casts to a structure.
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="stream"></param>
        ///// <returns></returns>
        //public static T ReadStructure<T>(this Stream stream) where T : struct
        //{
        //    int TSize = Marshal.SizeOf<T>();
        //    byte[] buffer = new byte[TSize];
        //    stream.Read(buffer, 0, TSize);
        //    return ByteArrayToStructure<T>(buffer);
        //}

        /// <summary>
        /// Reads bytes from <paramref name="reader"/> and casts to a structure.
        /// Then seeks to the end of the structure data using byte size specified by the stream/object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <param name="GetByteSizeFromObj">Called once on the resulting object from casting to structure.</param>
        /// <returns></returns>
        public static T ReadStructureAndSeek<T>(this BinaryReader reader, Func<T, int> GetByteSizeFromObj) where T : struct
        {
            int TSize = Marshal.SizeOf<T>();

            T res = ByteArrayToStructure<T>(reader.ReadBytes(TSize));

            // Seek/skip to end.
            int intendedByteSize = GetByteSizeFromObj(res);
            long byteDelta = intendedByteSize - TSize;
            if (byteDelta != 0) reader.BaseStream.Seek(byteDelta, SeekOrigin.Current);
            //if (skipBytes > 0)
            //  reader.ReadBytes(skipBytes);
            //else if (skipBytes < 0)
            //  throw new IndexOutOfRangeException($"Byte size of struct ({structByteSize} B) is larger than the byte size specified by the stream/object ({intendedByteSize} B).");

            return res;
        }

        ///// <summary>
        ///// Reads bytes from <paramref name="stream"/> and casts to a structure.
        ///// Then seeks to the end of the structure data using byte size specified by the stream/object.
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="stream"></param>
        ///// <param name="GetByteSizeFromObj">Called once on the resulting object from casting to structure.</param>
        ///// <returns></returns>
        //public static T ReadStructureAndSeek<T>(this Stream stream, Func<T, int> GetByteSizeFromObj) where T : struct
        //{
        //    int TSize = Marshal.SizeOf<T>();
        //    byte[] buffer = new byte[TSize];
        //    stream.Read(buffer, 0, TSize);
        //    T res = ByteArrayToStructure<T>(buffer);

        //    // Seek/skip to end.
        //    int intendedByteSize = GetByteSizeFromObj(res);
        //    long byteDelta = intendedByteSize - TSize;
        //    if (byteDelta != 0) stream.Seek(byteDelta, SeekOrigin.Current);
        //    //if (skipBytes > 0)
        //    //  reader.ReadBytes(skipBytes);
        //    //else if (skipBytes < 0)
        //    //  throw new IndexOutOfRangeException($"Byte size of struct ({structByteSize} B) is larger than the byte size specified by the stream/object ({intendedByteSize} B).");

        //    return res;
        //}

        public static T[] ReadStructureArray<T>(this BinaryReader reader, int count) where T : struct
        {
            byte[] bytes = new byte[count * Marshal.SizeOf<T>()];
            reader.Read(bytes, 0, bytes.Length);
            return ByteArrayToStructureArray<T>(bytes);
        }

        //public static T[] ReadStructureArray<T>(this Stream stream, int count) where T : struct
        //{
        //    byte[] bytes = new byte[count * Marshal.SizeOf<T>()];
        //    stream.Read(bytes, 0, bytes.Length);
        //    return ByteArrayToStructureArray<T>(bytes);
        //}


        //public static char[] ReadChars(this Stream stream, int length)
        //{
        //    StreamReader reader = new StreamReader(stream);
        //    char[] buffer = new char[length];
        //    reader.Read(buffer, 0, length);
        //    return buffer;
        //}


        /**************** Write ****************/

        /// <summary>
        /// Reads bytes from <paramref name="writer"/> and casts to a structure.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="writer"></param>
        /// <returns></returns>
        public static void WriteStructure<T>(this BinaryWriter writer, T source) where T : struct => writer.Write(StructureToByteArray<T>(source));

        ///// <summary>
        ///// Reads bytes from <paramref name="stream"/> and casts to a structure.
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="stream"></param>
        ///// <returns></returns>
        //public static void WriteStructure<T>(this Stream stream, T source) where T : struct
        //{
        //    byte[] bytes = StructureToByteArray<T>(source);
        //    stream.Write(StructureToByteArray<T>(source), 0, bytes.Length);
        //}

        /// <summary>
        /// Reads bytes from <paramref name="writer"/> and casts to a structure.
        /// Then seeks to the end of the structure data using byte size specified by the stream/object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="writer"></param>
        /// <param name="GetByteSizeFromObj">Called once on the resulting object from casting to structure.</param>
        /// <returns></returns>
        public static void WriteStructureAndPadOrTruncate<T>(this BinaryWriter writer, T source, int inputByteSize) where T : struct
        {
            // Padded/truncated data. 
            byte[] bytes = StructureToByteArray<T>(source, inputByteSize);

            writer.Write(bytes);

            // No need to seek as the data is padded/truncated already.
            // Seek/skip to end.
            //int byteDelta = inputByteSize - Marshal.SizeOf<T>();
            //if (byteDelta != 0) writer.Seek(byteDelta, SeekOrigin.Current);
        }

        ///// <summary>
        ///// Reads bytes from <paramref name="stream"/> and casts to a structure.
        ///// Then seeks to the end of the structure data using byte size specified by the stream/object.
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="stream"></param>
        ///// <param name="GetByteSizeFromObj">Called once on the resulting object from casting to structure.</param>
        ///// <returns></returns>
        //public static void WriteStructureAndSeek<T>(this Stream stream, T source, int inputByteSize) where T : struct
        //{
        //    byte[] bytes = StructureToByteArray<T>(source);
        //    stream.Write(bytes, 0, bytes.Length);

        //    // Seek/skip to end.
        //    long byteDelta = inputByteSize - Marshal.SizeOf<T>();
        //    if (byteDelta != 0) stream.Seek(byteDelta, SeekOrigin.Current);
        //}

        public static void WriteStructureArray<T>(this BinaryWriter writer, T[] source, int start = 0, int length = int.MaxValue) where T : struct
        {
            int end = Math.Min(source.Length - start, length) + start;
            length = end - start;

            byte[] bytes = StructureArrayToByteArray<T>(source, start, length);
            writer.Write(bytes, 0, bytes.Length);
        }

        //public static void WriteStructureArray<T>(this Stream stream, T[] source, int start = 0, int length = int.MaxValue) where T : struct
        //{
        //    int end = Math.Min(source.Length - start, length) + start;
        //    length = end - start;

        //    byte[] bytes = StructureArrayToByteArray<T>(source, start, length);
        //    stream.Write(bytes, 0, bytes.Length);
        //}



        /**************** Little/Big-endian correction ****************/

        /// <summary>
        /// Double/UInt64 union.
        /// Enables us to convert between a double and its bit representation without any kind of type casting.
        /// <see href="https://stackoverflow.com/a/14709081"/>
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct DoubleUInt64Union
        {
            [FieldOffset(0)] public Double Double;
            [FieldOffset(0)] public UInt64 UInt64;
            //[FieldOffset(0)] public UInt32 UInt32Lower;
            //[FieldOffset(4)] public UInt32 UInt32Upper;

            public DoubleUInt64Union(Double @double) : this() => Double = @double;
            public DoubleUInt64Union(UInt64 uint64) : this() => UInt64 = uint64;
        }

        /// <summary>
        /// Swaps the lower and upper halves of the binary encoding of <paramref name="n"/>.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static UInt64 SwapBitHalves(UInt64 n) => ((n & 0x0000_0000_FFFF_FFFFUL) << 32) | ((n & 0xFFFF_FFFF_0000_0000UL) >> 32);

        /// <summary>
        /// Swaps the lower and upper halves of the binary encoding of <paramref name="x"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static double SwapBitHalves(double x)
        {
            DoubleUInt64Union n = new DoubleUInt64Union(x);
            n.UInt64 = SwapBitHalves(n.UInt64);
            return n.Double;
        }

    }
}
