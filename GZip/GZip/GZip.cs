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
        private SimpleThreadSafeQueue<Frame> _queueInput;
        private SimpleThreadSafeQueue<Frame> _queueOutput;
        private readonly object _locker = new object();
        private EventWaitHandle _whaitHandle;

        public GZip(Parameters parameters)
        {
            _parameters = parameters;
        }

        public event Message ShowMessage;

        protected virtual void OnShowMessage(MessageEventArgs args)
        {
            ShowMessage?.Invoke(this, args);
        }

        public void Start()
        {
            lock (_locker)
            {
                _whaitHandle = new AutoResetEvent(false);
                if (_workThreads != null)
                {
                    OnShowMessage(new MessageEventArgs(Properties.Resources.CannNotStartProcessAgain));
                    return;
                }

                try
                {
                    _output = new FileStream(_parameters.OutputFileName, FileMode.CreateNew);
                    _input = new FileStream(_parameters.InputFileName, FileMode.Open);

                    switch (_parameters.Operation)
                    {
                        case Parameters.OperationType.COMPRESS:
                            _zip = new GZipCompress(_input, _output, _parameters.BlockLength, 0);
                            break;
                        case Parameters.OperationType.DECOMPRESS:
                            _zip = new GZipDecompress(_input, _output);
                            break;
                        default:
                            OnShowMessage(new MessageEventArgs(Properties.Resources.UnknownOperation));
                            break;
                    }

                    _queueInput = new SimpleThreadSafeQueue<Frame>();
                    _queueOutput = new SimpleThreadSafeQueue<Frame>();

                    _workThreads = new Thread[_parameters.CountOfThreads + 2];
                    _workThreads[0] = new Thread(ReadWork);
                    _workThreads[1] = new Thread(WriteWork);
                    for (int i = 0; i < _workThreads.Length; i++)
                    {
                        if (_workThreads[i] == null)
                            _workThreads[i] = new Thread(ProcessWork);
                        _workThreads[i].Start();
                    }
                }
                catch (Exception e)
                {
                    OnShowMessage(new MessageEventArgs(e.Message));
                }
            }
        }

        public void WhaitToEnd()
        {
            _whaitHandle.WaitOne();
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

                _output.Flush();

                foreach (var workThread in _workThreads)
                {
                    if (workThread.IsAlive)
                        workThread.Abort();
                }

                _whaitHandle.Set();
            }
        }

        public void Dispose()
        {
            Stop();
            _input?.Dispose();
            _output?.Dispose();
            _whaitHandle.Dispose();
            GC.SuppressFinalize(this);
        }

        private bool IsMemoryEnough(Frame frame)
        {
            var len = frame.SizeOf();
            return (new ComputerInfo().AvailablePhysicalMemory) > (ulong)(_parameters.BlockLength * 3 + len);
        }

        private void ProcessWork()
        {
            while (true)
            {
                var frame = _queueInput.Pop();
                if (frame.Data == null)
                {
                    _queueInput.Push(frame);
                    return;
                }
                _queueOutput.Push(_zip.Process(frame));
            }
        }

        private void WriteWork()
        {
            int writeFrameCount = 0;
            while (true)
            {
                var frame = _queueOutput.Pop();
                _zip.Write(frame);
                writeFrameCount++;
                if (writeFrameCount == _zip.FramesCount)
                {
                    _whaitHandle.Set();
                    return;
                }
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
                _queueInput.Push(frame);
            }
            _queueInput.Push(default(Frame));
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
        int FramesCount { get; }
        IEnumerable<Frame> Read();
        void Write(Frame frame);
        Frame Process(Frame frame);
    }

    sealed class GZipCompress : IGZip
    {
        private readonly Stream _streamToRead;
        private readonly Stream _streamToWrite;
        private readonly int _frameLength;
        private readonly int _headerId;

        public GZipCompress(Stream toRead, Stream toWrite, int frameLength, int headerId)
        {
            _streamToRead = toRead;
            _streamToWrite = toWrite;
            _frameLength = frameLength;
            _headerId = headerId;

            FramesCount = (int)_streamToRead.Length / _frameLength + ((_streamToRead.Length % _frameLength) > 0 ? 1 : 0);
            var header = new WindowHeader(1, 1, _streamToRead.Length, FramesCount);
            FrameHelper.WriteWindowHeaderToStream(_streamToWrite, header);
        }

        public int FramesCount { get; }
        public int ReadFramesCount { get; private set; }

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
            FrameHelper.WriteFrameToStream(_streamToWrite, frame);
        }

        public IEnumerable<Frame> Read()
        {
            while (_streamToRead.Position < _streamToRead.Length)
            {
                int needToRead = (int)(((_streamToRead.Length - _streamToRead.Position) > _frameLength) ? _frameLength : (_streamToRead.Length - _streamToRead.Position));
                yield return FrameHelper.CreateUncompressedFrameFromStream(_streamToRead, needToRead, _headerId, ReadFramesCount++);
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
            FramesCount = header.FramesCount;
            _streamWrite.SetLength(header.SourceLength);
        }

        public int FramesCount { get; private set; }

        public Frame Process(Frame frame)
        {
            using (var memoryCompressedStream = new MemoryStream(frame.Data))
            {
                using (var memoryDecompressedStream = new MemoryStream())
                {
                    using (var stream = new GZipStream(memoryCompressedStream, CompressionMode.Decompress))
                    {
                        stream.CopyTo(memoryDecompressedStream);
                    }

                    var data = memoryDecompressedStream.ToArray();
                    return new Frame(new FrameHeader(frame.Header.HeaderId, frame.Header.Id, frame.Header.Position, data.Length), data);
                }
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