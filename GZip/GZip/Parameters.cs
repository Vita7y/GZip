using System;

namespace GZip
{
    public class Parameters
    {
        public enum OperationType
        {
            UNKNOWN,
            COMPRESS,
            DECOMPRESS
        }

        public Parameters()
        {
            BlockLength = 1048576;
            CountOfThreads = Environment.ProcessorCount;
        }

        public string InputFileName { get; set; }

        public string OutputFileName { get; set; }

        public OperationType Operation { get; set; }

        public int BlockLength { get; set; }

        public int CountOfThreads { get; set; }
    }
}