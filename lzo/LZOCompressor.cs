﻿using System;
using System.Runtime.InteropServices;

namespace TesteRedis
{
    public class LZOCompressor
    {
        private const string LzoDll32Bit = @"lzo\lib32\lzo2.dll";
        private const string LzoDll64Bit = @"lzo\lib64\lzo2_64.dll";
        private byte[] _workMemory = new byte[16384L * 4];

        #region Dll-Imports

        [DllImport(LzoDll64Bit)]
        private static extern int __lzo_init_v2(uint v, int s1, int s2, int s3, int s4, int s5, int s6, int s7, int s8, int s9);
        [DllImport(LzoDll64Bit)]
        private static extern IntPtr lzo_version_string();
        [DllImport(LzoDll32Bit, EntryPoint = "lzo_version_string")]
        private static extern IntPtr lzo_version_string32();
        [DllImport(LzoDll64Bit)]
        private static extern string lzo_version_date();
        [DllImport(LzoDll64Bit)]
        private static extern int lzo1x_1_compress(byte[] src, int src_len, byte[] dst, ref int dst_len, byte[] wrkmem);
        [DllImport(LzoDll64Bit)]
        private static extern int lzo1x_decompress(byte[] src, int src_len, byte[] dst, ref int dst_len, byte[] wrkmem);
        [DllImport(LzoDll32Bit, EntryPoint = "lzo1x_1_compress", CallingConvention = CallingConvention.Cdecl)]
        private static extern int lzo1x_1_compress32(byte[] src, int src_len, byte[] dst, ref int dst_len, byte[] wrkmem);
        [DllImport(LzoDll32Bit, EntryPoint = "lzo1x_decompress", CallingConvention = CallingConvention.Cdecl)]
        private static extern int lzo1x_decompress32(byte[] src, int src_len, byte[] dst, ref int dst_len, byte[] wrkmem);
        [DllImport(LzoDll32Bit, EntryPoint = "__lzo_init_v2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int __lzo_init_v2_32(uint v, int s1, int s2, int s3, int s4, int s5, int s6, int s7, int s8, int s9);

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public LZOCompressor()
        {
            int init = 0;
            if (Is64Bit())
                init = __lzo_init_v2(1, -1, -1, -1, -1, -1, -1, -1, -1, -1);
            else
                init = __lzo_init_v2_32(1, -1, -1, -1, -1, -1, -1, -1, -1, -1);

            if (init != 0)
            {
                throw new Exception("Initialization of LZO-Compressor failed !");
            }
        }

        /// <summary>
        /// Version string of the compression library.
        /// </summary>
        public string Version
        {
            get
            {
                IntPtr strPtr;
                if (Is64Bit())
                    strPtr = lzo_version_string();
                else
                    strPtr = lzo_version_string32();

                string version = Marshal.PtrToStringAnsi(strPtr);
                return version;
            }
        }

        /// <summary>
        /// Version date of the compression library
        /// </summary>
        public string VersionDate
        {
            get
            {
                return lzo_version_date();
            }
        }

        /// <summary>
        /// Compresses a byte array and returns the compressed data in a new
        /// array. You need the original length of the array to decompress it.
        /// </summary>
        /// <param name="src">Source array for compression</param>
        /// <returns>Byte array containing the compressed data</returns>
        public byte[] Compress(byte[] src)
        {
            byte[] dst = new byte[src.Length + src.Length / 64 + 16 + 3 + 4];
            int outlen = 0;
            if (Is64Bit())
                lzo1x_1_compress(src, src.Length, dst, ref outlen, _workMemory);
            else
                lzo1x_1_compress32(src, src.Length, dst, ref outlen, _workMemory);

            byte[] ret = new byte[outlen + 4];
            Array.Copy(dst, 0, ret, 0, outlen);
            byte[] outlenarr = BitConverter.GetBytes(src.Length);
            Array.Copy(outlenarr, 0, ret, outlen, 4);
            return ret;
        }

        private bool _calculated;
        private bool _is64bit;
        private bool Is64Bit()
        {
            if (!_calculated)
            {
                _is64bit = IntPtr.Size == 8;
                _calculated = true;
            }
            return _is64bit;
        }

        /// <summary>
        /// Decompresses compressed data to its original state.
        /// </summary>
        /// <param name="src">Source array to be decompressed</param>
        /// <returns>Decompressed data</returns>
        public byte[] Decompress(byte[] src)
        {
            int origlen = BitConverter.ToInt32(src, src.Length - 4);
            byte[] dst = new byte[origlen];
            int outlen = origlen;
            if (Is64Bit())
                lzo1x_decompress(src, src.Length - 4, dst, ref outlen, _workMemory);
            else
                lzo1x_decompress32(src, src.Length - 4, dst, ref outlen, _workMemory);

            return dst;
        }
    }
}
