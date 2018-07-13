using System.Runtime.InteropServices;

namespace GZip
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FrameHeader
    {
        public FrameHeader(int headerId, long frameId, long position, long dataLegth)
        {
            HeaderId = headerId;
            Id = frameId;
            Position = position;
            DataLength = dataLegth;
        }

        public int HeaderId { get; }

        public long Id { get; }

        public long Position { get; }

        public long DataLength { get; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Frame
    {
        public Frame(FrameHeader header, byte[] buf)
        {
            Header = header;
            Data = buf;
        }

        public FrameHeader Header;

        public byte[] Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowHeader
    {
        public WindowHeader(int version, int headerId, long sourceLength, long framesCount)
        {
            Version = version;
            HeaderId = headerId;
            SourceLength = sourceLength;
            FramesCount = framesCount;
        }

        public int HeaderId { get; }

        public long SourceLength { get; }

        public int Version { get; }

        public long FramesCount { get; }
    }
}