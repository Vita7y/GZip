using System.Runtime.InteropServices;

namespace GZip
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FrameHeader
    {
        public FrameHeader(int headerId, int frameId, long position, int dataLegth)
        {
            HeaderId = headerId;
            Id = frameId;
            Position = position;
            DataLength = dataLegth;
        }

        public int HeaderId { get; }

        public int Id { get; }

        public long Position { get; }

        public int DataLength { get; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Frame
    {
        public Frame(FrameHeader header, byte[] buf)
        {
            Header = header;
            Data = buf;
        }

        public FrameHeader Header { get; }

        public byte[] Data { get; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowHeader
    {
        public WindowHeader(int version, int id, long sourceLength, int framesCount)
        {
            Version = version;
            Id = id;
            SourceLength = sourceLength;
            FramesCount = framesCount;
        }
        public int Id { get; }

        public long SourceLength { get; }

        public int Version { get; }

        public int FramesCount { get; }
    }
}