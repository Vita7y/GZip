namespace GZip
{
    public struct Frame
    {
        public Frame(int id, byte[] buf)
        {
            Id = id;
            Input = buf;
            Out = null;
        }

        public int Id { get; }

        public byte[] Input { get; }

        public byte[] Out { get; private set; }

        public void SetOutBuf(byte[] buf)
        {
            Out = buf;
        }
    }
}