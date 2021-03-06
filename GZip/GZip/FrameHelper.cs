﻿using System;
using System.IO;
using System.Runtime.InteropServices;

namespace GZip
{
    public static class FrameHelper
    {
        public static T ReadStruct<T>(this byte[] inputBuff) where T : struct
        {
            var buffer = new byte[Marshal.SizeOf(typeof(T))];
            Array.Copy(inputBuff, 0, buffer, 0, buffer.Length);
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            var temp = (T) Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return temp;
        }

        public static byte[] StructToByteArray(this Frame inputType)
        {
            var len = Marshal.SizeOf(inputType.Header);
            var buffer = new byte[len + inputType.Data.Length];
            var ptr = Marshal.AllocHGlobal(buffer.Length);
            Marshal.StructureToPtr(inputType.Header, ptr, true);
            Marshal.Copy(ptr, buffer, 0, buffer.Length);
            Marshal.FreeHGlobal(ptr);
            Array.Copy(inputType.Data, 0, buffer, len, inputType.Data.Length);
            return buffer;
        }

        public static byte[] StructToByteArray<T>(this T inputType) where T : struct
        {
            var len = Marshal.SizeOf(inputType);
            var buffer = new byte[len];
            var ptr = Marshal.AllocHGlobal(len);
            Marshal.StructureToPtr(inputType, ptr, true);
            Marshal.Copy(ptr, buffer, 0, len);
            Marshal.FreeHGlobal(ptr);
            return buffer;
        }

        public static WindowHeader ReadWindowHeaderFromCompressedStream(Stream stream)
        {
            var sizeOfFrameHeader = Marshal.SizeOf(typeof(WindowHeader));
            var frameHeaderBuf = new byte[sizeOfFrameHeader];
            stream.Read(frameHeaderBuf, 0, sizeOfFrameHeader);
            return frameHeaderBuf.ReadStruct<WindowHeader>();
        }

        public static void WriteWindowHeaderToStream(Stream stream, WindowHeader header)
        {
            var bufWindowHeader = header.StructToByteArray();
            stream.Write(bufWindowHeader, 0, bufWindowHeader.Length);
        }

        public static Frame CreateUncompressedFrameFromStream(Stream stream, int lengthRead, int headerId, long frameId)
        {
            var buf = new byte[lengthRead];
            var position = stream.Position;
            stream.Read(buf, 0, lengthRead);
            return new Frame(new FrameHeader(headerId, frameId, position, buf.Length), buf);
        }

        public static Frame ReadCompressedFrameFromStream(Stream stream)
        {
            var sizeOfFrameHeader = Marshal.SizeOf(typeof(FrameHeader));
            var frameHeaderBuf = new byte[sizeOfFrameHeader];
            stream.Read(frameHeaderBuf, 0, sizeOfFrameHeader);
            var frameHeader = frameHeaderBuf.ReadStruct<FrameHeader>();
            var frameDataBuf = new byte[frameHeader.DataLength];
            stream.Read(frameDataBuf, 0, frameDataBuf.Length);
            return new Frame(frameHeader, frameDataBuf);
        }

        public static void WriteFrameToStream(Stream stream, Frame frame)
        {
            var bufFrame = frame.StructToByteArray();
            stream.Write(bufFrame, 0, bufFrame.Length);
        }
    }
}