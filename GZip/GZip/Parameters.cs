using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZip
{
    public class Parameters
    {
        public Parameters()
        {
            BlockLength = 1048576;
            CountOfThreads = Environment.ProcessorCount;
        }

        public enum OperationType
        {
            Unknown,
            Compress,
            Decompress
        }

        public string InputFileName { get; set; }

        public string OutputFileName { get; set; }

        public OperationType Operation { get; set; }

        public int BlockLength { get; set; }

        public int CountOfThreads { get; set; }
    }
}
