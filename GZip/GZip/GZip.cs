using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using Microsoft.VisualBasic.Devices;

namespace GZip
{
    public class GZip : IDisposable
    {
        private readonly Parameters _parameters;
        private FileStream _input;
        private FileStream _output;
        private IGZip _zip;

        private Thread[] _workThreads;
        private SimpleThreadSafeStack<Frame> _stackInput;
        private SimpleThreadSafeStack<Frame> _stackOutput;
        private readonly object _locker = new object();

        public GZip(Parameters parameters)
        {
            _parameters = parameters;
        }

        public event Message ShowMessage;

        protected virtual void OnShowMessage(MessageEventArgs args)
        {
            ShowMessage?.Invoke(this, args);
        }

        private bool IsMemoryEnough(Frame frame)
        {
            var len = frame.SizeOf();
            return (new ComputerInfo().AvailablePhysicalMemory) > (ulong) (_parameters.BlockLength * 3 + len);
        }

        public void Start()
        {
            lock (_locker)
            {

                if (_workThreads != null)
                {
                    OnShowMessage(new MessageEventArgs(Properties.Resources.CannNotStartProcessAgain));
                    return;
                }

                _stackInput = new SimpleThreadSafeStack<Frame>();
                _stackOutput = new SimpleThreadSafeStack<Frame>();

                _workThreads = new Thread[_parameters.CountOfThreads+2];
                _workThreads[0] = new Thread(ReadWork);
                _workThreads[1] = new Thread(WriteWork);
                for (int i = 2; i < _workThreads.Length; i++)
                {
                    _workThreads[i] = new Thread(ProcessWork);
                }

                try
                {
                    _output = new FileStream(_parameters.OutputFileName, FileMode.CreateNew);
                    _input = new FileStream(_parameters.InputFileName, FileMode.Open);

                    switch (_parameters.Operation)
                    {
                        case Parameters.OperationType.Compress:
                            _zip = new GZipCompress(_input, _output, _parameters.BlockLength, 0);
                            break;
                        case Parameters.OperationType.Decompress:
                            _zip = new GZipDecompress(_input, _output);
                            break;
                        default:
                            OnShowMessage(new MessageEventArgs(Properties.Resources.UnknownOperation));
                            break;
                    }
                }
                catch (Exception e)
                {
                    OnShowMessage(new MessageEventArgs(e.Message));
                }
            }
        }

        private void ProcessWork()
        {
            while (true)
            {
                var frame = _stackInput.Pop();
                _stackOutput.Push(_zip.Process(frame));
            }
        }

        private void WriteWork()
        {
            while (true)
            {
                _zip.Write(_stackOutput.Pop());
            }
        }

        private void ReadWork()
        {
            foreach (var frame in _zip.Read())
            {
                if (!IsMemoryEnough(frame))
                {
                    OnShowMessage(new MessageEventArgs(Properties.Resources.MemoryNoEnough));
                    OnShowMessage(new MessageEventArgs(Properties.Resources.OperationWasStopped));
                    return;
                }
                _stackInput.Push(frame);
            }
        }

        public void Stop()
        {
            lock (_locker)
            {
                if (_workThreads == null)
                {
                    OnShowMessage(new MessageEventArgs(Properties.Resources.OperationIsNotStarted));
                    return;
                }

                foreach (var workThread in _workThreads)
                {
                    if (workThread.IsAlive)
                        workThread.Abort();
                }
            }
        }

        public void Dispose()
        {
            Stop();
            _input?.Dispose();
            _output?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public delegate void Message(object sender, MessageEventArgs args);

    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    internal interface IGZip
    {
        IEnumerable<Frame> Read();
        void Write(Frame frame);
        Frame Process(Frame frame);
    }

    sealed class GZipCompress : IGZip
    {
        private readonly Stream _streamRead;
        private readonly Stream _streamWrite;
        private readonly int _frameLength;
        private readonly int _headerId;

        public GZipCompress(Stream read, Stream write, int frameLength, int headerId)
        {
            _streamRead = read;
            _streamWrite = write;
            _frameLength = frameLength;
            _headerId = headerId;

            int frameCount = (int)_streamWrite.Length / _frameLength + ((_streamWrite.Length % _frameLength) > 0 ? 1 : 0);
            var header = new WindowHeader(1, 1, _streamWrite.Length, frameCount);
            FrameHelper.WriteWindowHeaderToStream(_streamWrite, header);
        }

        public Frame Process(Frame frame)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var stream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    stream.Write(frame.Data, 0, frame.Data.Length);
                }

                var data = memoryStream.ToArray();
                return new Frame(new FrameHeader(frame.Header.HeaderId, frame.Header.Id, frame.Header.Position, data.Length), data);
            }
        }

        public void Write(Frame frame)
        {
            FrameHelper.WriteFrameToStream(_streamWrite, frame);
        }

        public IEnumerable<Frame> Read()
        {
            var frameCount = 0;
            while (_streamRead.Position < _streamRead.Length)
            {
                int needToRead = (int)(((_streamRead.Length - _streamRead.Position) > _frameLength) ? _frameLength : (_streamRead.Length - _streamRead.Position));
                yield return FrameHelper.CreateUncompressedFrameFromStream(_streamRead, needToRead, _headerId, frameCount++);
            }
        }
    }

    sealed class GZipDecompress : IGZip
    {
        private readonly Stream _streamRead;
        private readonly Stream _streamWrite;

        public GZipDecompress(Stream read, Stream write)
        {
            _streamRead = read;
            _streamWrite = write;

            var header = FrameHelper.ReadWindowHeaderFromCompressedStream(_streamRead);
            _streamWrite.SetLength(header.SourceLength);
        }

        public Frame Process(Frame frame)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var stream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    stream.Write(frame.Data, 0, frame.Data.Length);
                }

                var data = memoryStream.ToArray();
                return new Frame(new FrameHeader(frame.Header.HeaderId, frame.Header.Id, frame.Header.Position, data.Length), data);
            }
        }

        public void Write(Frame frame)
        {
            _streamWrite.Position = frame.Header.Position;
            _streamWrite.Write(frame.Data, 0, frame.Data.Length);
        }

        public IEnumerable<Frame> Read()
        {
            while (_streamRead.Position < _streamRead.Length)
            {
                yield return FrameHelper.ReadCompressedFrameFromStream(_streamRead);
            }
        }
    }
}