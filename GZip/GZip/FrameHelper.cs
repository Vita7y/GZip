using System;
using System.IO;
using System.Runtime.InteropServices;

namespace GZip
{
    public static class FrameHelper
    {
        public static int SizeOf<T>(this T inputType) where T : struct
        {
            return Marshal.SizeOf(inputType);
        }

        /// <summary>
        /// Получение структуры из буфера
        /// </summary>
        /// <param name="inputBuff"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T ReadStruct<T>(this byte[] inputBuff) where T: struct 
        {
            byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];
            Array.Copy(inputBuff, 0, buffer, 0, buffer.Length);
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            T temp = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return temp;
        }

        public static byte[] StructToByteArray<T>(this T inputType) where T: struct 
        {
            byte[] buffer = new byte[Marshal.SizeOf(inputType)];
            IntPtr ptr = Marshal.AllocHGlobal(buffer.Length);
            Marshal.StructureToPtr(inputType, ptr, true);
            Marshal.Copy(ptr, buffer, 0, buffer.Length);
            Marshal.FreeHGlobal(ptr);
            return buffer;
        }

        public static WindowHeader ReadWindowHeaderFromCompressedStream(Stream stream)
        {
            var sizeOfFrameHeader = Marshal.SizeOf(typeof(WindowHeader));
            var frameHeaderBuf = new byte[sizeOfFrameHeader];
            stream.Read(frameHeaderBuf, (int)stream.Position, sizeOfFrameHeader);
            return frameHeaderBuf.ReadStruct<WindowHeader>();
        }

        public static Frame CreateUncompressedFrameFromStream(Stream stream, int lengthRead, int headerId, int frameId)
        {
            var buf = new byte[lengthRead];
            var position = stream.Position;
            stream.Read(buf, (int)stream.Position, lengthRead);
            return new Frame(new FrameHeader(headerId, frameId, position, buf.Length), buf);
        }


        public static Frame ReadCompressedFrameFromStream(Stream stream)
        {
            var sizeOfFrameHeader = Marshal.SizeOf(typeof(FrameHeader));
            var frameHeaderBuf = new byte[sizeOfFrameHeader];
            stream.Read(frameHeaderBuf, (int)stream.Position, sizeOfFrameHeader);
            var frameHeader = frameHeaderBuf.ReadStruct<FrameHeader>();
            var frameDataBuf = new byte[frameHeader.DataLength];
            stream.Read(frameDataBuf, (int)stream.Position, frameDataBuf.Length);
            return new Frame(frameHeader, frameDataBuf);
        }

        public static void WriteWindowHeaderFromStream(Stream stream, WindowHeader header)
        {
            var bufWindowHeader = header.StructToByteArray();
            stream.Write(bufWindowHeader, (int)stream.Position, bufWindowHeader.Length);
        }

        public static void WriteFrameToStream(Stream stream, Frame frame)
        {
            var bufFrame = frame.StructToByteArray();
            stream.Write(bufFrame, (int)stream.Position, bufFrame.Length);
        }


    }
}