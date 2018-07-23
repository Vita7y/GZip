using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using GZip.Properties;
using Microsoft.VisualBasic.Devices;

namespace GZip
{
    public class GZip : IDisposable
    {
        private readonly object _locker = new object();
        private readonly Parameters _parameters;
        private FileStream _input;
        private FileStream _output;
        private SimpleThreadSafeQueue<Frame> _queueInput;
        private SimpleThreadSafeQueue<Frame> _queueOutput;
        private EventWaitHandle _whaitHandle;
        private EventWaitHandle _whaitMemoryHandle;
        private Thread[] _workThreads;
        private IGZip _zip;

        public GZip(Parameters parameters)
        {
            _parameters = parameters;
        }

        ~GZip()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                Stop();
            _input?.Dispose();
            _output?.Dispose();
            _whaitHandle?.Dispose();
            _whaitMemoryHandle?.Dispose();
        }

        public event Message ShowMessage;

        public bool Start()
        {
            lock (_locker)
            {
                if (_workThreads != null)
                {
                    OnShowMessage(new MessageEventArgs(Resources.OperationWasStarted));
                    return false;
                }

                try
                {
                    _input = new FileStream(_parameters.InputFileName, FileMode.Open, FileAccess.Read);
                    if (_input.Length == 0)
                    {
                        OnShowMessage(new MessageEventArgs(Resources.InputFileIsEmpty));
                        return false;
                    }
                    _output = new FileStream(_parameters.OutputFileName, FileMode.CreateNew);

                    _zip = SelectOperationType(_parameters.Operation, _input, _output, _parameters.BlockLength);
                    if (_zip == null)
                    {
                        OnShowMessage(new MessageEventArgs(Resources.UnknownOperation));
                        return false;
                    }

                    _queueInput = new SimpleThreadSafeQueue<Frame>();
                    _queueOutput = new SimpleThreadSafeQueue<Frame>();

                    _workThreads = new Thread[_parameters.CountOfThreads + 2];
                    _workThreads[0] = new Thread(ReadWork) {Name = "ReadDataThread"};
                    _workThreads[1] = new Thread(WriteWork) {Name = "WriteDataThread"};

                    _whaitHandle = new AutoResetEvent(false);
                    _whaitMemoryHandle = new AutoResetEvent(false);
                    for (var i = 0; i < _workThreads.Length; i++)
                    {
                        if (_workThreads[i] == null)
                            _workThreads[i] = new Thread(ProcessWork) {Name = "WorkThread" + i};
                        _workThreads[i].Start();
                    }

                    OnShowMessage(new MessageEventArgs(Resources.OperationWasStarted));
                    return true;
                }
                catch (Exception e)
                {
                    OnShowMessage(new MessageEventArgs(e.Message));
                    _whaitHandle?.Set();
                }

                return false;
            }
        }

        public void Stop()
        {
            lock (_locker)
            {
                try
                {
                    if (_workThreads == null)
                    {
                        _whaitHandle?.Set();
                        OnShowMessage(new MessageEventArgs(Resources.OperationIsNotStarted));
                        return;
                    }

                    _output.Flush();
                    _zip.Cancel();
                    _whaitMemoryHandle.Set();

                    foreach (var workThread in _workThreads)
                        if (workThread.IsAlive)
                            workThread.Abort();

                    OnShowMessage(new MessageEventArgs(Resources.WorkIsDone));
                    _whaitHandle?.Set();
                }
                catch (Exception e)
                {
                    OnShowMessage(new MessageEventArgs(e.Message));
                }
            }
        }

        public void WhaitToEnd()
        {
            _whaitHandle?.WaitOne();
        }

        protected virtual void OnShowMessage(MessageEventArgs args)
        {
            ShowMessage?.Invoke(this, args);
        }

        private bool IsMemoryEnough()
        {
            var freeMemry = new ComputerInfo().AvailablePhysicalMemory;
            var minLevelMemory = (ulong) (_parameters.BlockLength * 10);
            if (IntPtr.Size == 4)
            {
                // 32-bit
                var memory = GC.GetTotalMemory(true);
                freeMemry = (freeMemry > (ulong) (int.MaxValue - memory)) ? (ulong) (int.MaxValue - memory) : freeMemry;
            }
            return freeMemry > minLevelMemory;
        }

        private void ReadWork()
        {
            if (!IsMemoryEnough())
            {
                _zip.Cancel();
                _queueInput.Enqueue(default(Frame));
                OnShowMessage(new MessageEventArgs(Resources.ErrorMemoryNotEnough));
                OnShowMessage(new MessageEventArgs(Resources.OperationWasStopped));
                return;
            }

            try
            {
                foreach (var frame in _zip.Read())
                {
                    if (!IsMemoryEnough())
                        _whaitMemoryHandle.WaitOne();
                    _queueInput.Enqueue(frame);
                    OnShowMessage(new MessageEventArgs($"Statistics: Count element in input queue {_queueInput.Count}, in output queue {_queueOutput.Count}"));
                }
            }
            catch (Exception e)
            {
                OnShowMessage(new MessageEventArgs(e.Message));
                _queueInput.Enqueue(default(Frame));
            }
        }

        private void ProcessWork()
        {
            try
            {
                while (true)
                {
                    var frame = _queueInput.Dequeue();
                    if (frame.Data == null)
                    {
                        _queueInput.Enqueue(frame);
                        return;
                    }
                    _queueOutput.Enqueue(_zip.Process(frame));
                }
            }
            catch (Exception e)
            {
                OnShowMessage(new MessageEventArgs(e.Message));
                _queueInput.Enqueue(default(Frame));
            }
        }

        private void WriteWork()
        {
            var writeFrameCount = 0;
            try
            {
                while (true)
                {
                    var frame = _queueOutput.Dequeue();
                    _zip.Write(frame);
                    writeFrameCount++;
                    if (writeFrameCount == _zip.FramesCount)
                        return;
                    if (IsMemoryEnough())
                        _whaitMemoryHandle.Set();
                }
            }
            catch (Exception e)
            {
                OnShowMessage(new MessageEventArgs(e.Message));
            }
            finally
            {
                _whaitHandle.Set();
            }
        }

        private static IGZip SelectOperationType(Parameters.OperationType operation, Stream read, Stream write, int blockLength)
        {
            switch (operation)
            {
                case Parameters.OperationType.COMPRESS:
                    return new GZipCompress(read, write, blockLength, 0);
                case Parameters.OperationType.DECOMPRESS:
                    return new GZipDecompress(read, write);
                default:
                    return null;
            }
        }
    }

    public delegate void Message(object sender, MessageEventArgs e);

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
        long FramesCount { get; }
        long ReadFramesCount { get; }
        IEnumerable<Frame> Read();
        void Write(Frame frame);
        Frame Process(Frame frame);
        void Cancel();
    }

    internal sealed class GZipCompress : IGZip
    {
        private readonly int _frameLength;
        private readonly int _headerId;
        private readonly Stream _streamToRead;
        private readonly Stream _streamToWrite;

        public GZipCompress(Stream toRead, Stream toWrite, int frameLength, int headerId)
        {
            _streamToRead = toRead;
            _streamToWrite = toWrite;
            _frameLength = frameLength;
            _headerId = headerId;

            FramesCount = _streamToRead.Length / _frameLength + (_streamToRead.Length % _frameLength > 0 ? 1 : 0);
            var header = new WindowHeader(1, 1, _streamToRead.Length, FramesCount);
            FrameHelper.WriteWindowHeaderToStream(_streamToWrite, header);
        }

        public long ReadFramesCount { get; private set; }

        public long FramesCount { get; private set; }

        public void Cancel()
        {
            FramesCount = ReadFramesCount - 1;
        }

        public Frame Process(Frame frame)
        {
            byte[] data;
            MemoryStream memoryStream = null;
            try
            {
                memoryStream = new MemoryStream();
                using (var stream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    stream.Write(frame.Data, 0, frame.Data.Length);
                }
                data = memoryStream.ToArray();
            }
            finally
            {
                memoryStream?.Dispose();
            }

            var header = new FrameHeader(frame.Header.HeaderId, frame.Header.Id, frame.Header.Position, data.Length);
            return new Frame(header, data);
        }

        public void Write(Frame frame)
        {
            FrameHelper.WriteFrameToStream(_streamToWrite, frame);
        }

        public IEnumerable<Frame> Read()
        {
            while (_streamToRead.Position < _streamToRead.Length)
            {
                if (ReadFramesCount >= FramesCount)
                    break;
                var needToRead = (int) (((_streamToRead.Length - _streamToRead.Position) > _frameLength)
                    ? _frameLength
                    : _streamToRead.Length - _streamToRead.Position);
                yield return FrameHelper.CreateUncompressedFrameFromStream(_streamToRead, needToRead, _headerId, ReadFramesCount++);
            }
        }
    }

    internal sealed class GZipDecompress : IGZip
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

        public long FramesCount { get; private set; }

        public long ReadFramesCount { get; private set; }

        public Frame Process(Frame frame)
        {
            byte[] data;
            MemoryStream memoryCompressedStream = null;
            MemoryStream memoryDecompressedStream = null;
            try
            {
                memoryCompressedStream = new MemoryStream(frame.Data);
                memoryDecompressedStream = new MemoryStream();
                using (var stream = new GZipStream(memoryCompressedStream, CompressionMode.Decompress))
                {
                    stream.CopyTo(memoryDecompressedStream);
                }
                data = memoryDecompressedStream.ToArray();
            }
            finally
            {
                memoryCompressedStream?.Dispose();
                memoryDecompressedStream?.Dispose();
            }

            var header = new FrameHeader(frame.Header.HeaderId, frame.Header.Id, frame.Header.Position, data.Length);
            return new Frame(header, data);
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
                if (ReadFramesCount >= FramesCount)
                    break;
                var frame = FrameHelper.ReadCompressedFrameFromStream(_streamRead);
                ReadFramesCount++;
                yield return frame;
            }
        }

        public void Cancel()
        {
            FramesCount = ReadFramesCount - 1;
        }
    }
}