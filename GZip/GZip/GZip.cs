using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using GZip.Properties;
using Microsoft.VisualBasic.Devices;

namespace GZip
{
    public class GZip : IDisposable
    {
        private readonly Parameters _parameters;
        private FileStream _input;
        private FileStream _output;
        private BlockingCollection<Frame> _queueInput;
        private BlockingCollection<Frame> _queueOutput;
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

        public void Start()
        {
            if (_workThreads != null)
            {
                OnShowMessage(new MessageEventArgs(Resources.OperationWasStarted));
                return;
            }

            try
            {
                if (!OpenStreams())
                    return;

                _zip = SelectOperationType(_parameters.Operation, _input, _output, _parameters.BlockLength);
                if (_zip == null)
                {
                    OnShowMessage(new MessageEventArgs(Resources.UnknownOperation));
                    return;
                }

                var workTime = new Stopwatch();
                workTime.Start();

                CreateWorkThreads();
                OnShowMessage(new MessageEventArgs(Resources.OperationWasStarted));
                WhaitToEnd();

                workTime.Stop();
                ShowWorkInfo(workTime);
            }
            catch (Exception e)
            {
                OnShowMessage(new MessageEventArgs($"Error on start: {e.Message}"));
            }
        }

        private void CreateWorkThreads()
        {
            _workThreads = new Thread[_parameters.CountOfThreads + 2];
            _workThreads[0] = new Thread(ReadWork) {Name = "ReadDataThread"};
            _workThreads[1] = new Thread(WriteWork) {Name = "WriteDataThread"};

            _queueInput = new BlockingCollection<Frame>(new ConcurrentQueue<Frame>(), _workThreads.Length);
            _queueOutput = new BlockingCollection<Frame>(new ConcurrentQueue<Frame>(), _workThreads.Length);

            _whaitHandle = new AutoResetEvent(false);
            _whaitMemoryHandle = new AutoResetEvent(false);

            for (var i = 0; i < _workThreads.Length; i++)
            {
                if (_workThreads[i] == null)
                    _workThreads[i] = new Thread(ProcessWork) {Name = "WorkThread" + i};
                _workThreads[i].Start();
            }
        }

        private void ShowWorkInfo(Stopwatch workTime)
        {
            if (_zip.ReadFramesCount == _zip.FramesCount)
            {
                OnShowMessage(new MessageEventArgs(Resources.WorkIsDone));
                OnShowMessage(new MessageEventArgs("Input file            :" + _parameters.InputFileName));
                OnShowMessage(new MessageEventArgs("Output file           :" + _parameters.OutputFileName));
                OnShowMessage(new MessageEventArgs("Executed operation    :" + _parameters.Operation));
                OnShowMessage(new MessageEventArgs("Count of work threads :" + _parameters.CountOfThreads));
                OnShowMessage(new MessageEventArgs("Work time             :" + workTime.Elapsed));
            }
        }

        private bool OpenStreams()
        {
            _input = new FileStream(_parameters.InputFileName, FileMode.Open, FileAccess.Read);
            if (_input.Length == 0)
            {
                OnShowMessage(new MessageEventArgs(Resources.InputFileIsEmpty));
                return false;
            }

            _output = new FileStream(_parameters.OutputFileName, FileMode.CreateNew);
            return true;
        }

        public void Stop()
        {
            try
            {
                if (_workThreads == null)
                {
                    OnShowMessage(new MessageEventArgs(Resources.OperationIsNotStarted));
                    return;
                }

                _queueInput?.CompleteAdding();
                _queueOutput?.CompleteAdding();
                _whaitMemoryHandle?.Set();
                _output?.Flush();
                _zip?.Cancel();
            }
            catch (Exception e)
            {
                OnShowMessage(new MessageEventArgs($"Error on stop: {e.Message}"));
            }
        }

        private void WhaitToEnd()
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
                _queueInput.Add(default(Frame));
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
                    if (_queueInput.IsCompleted)
                        return;
                    _queueInput.Add(frame);
                }
            }
            catch (InvalidOperationException ioe)
            {
                OnShowMessage(new MessageEventArgs($"Process {Thread.CurrentThread.Name} was stopped."));
            }
            catch (Exception e)
            {
                OnShowMessage(new MessageEventArgs($"Error on read: {e.Message}"));
            }
            _queueInput.CompleteAdding();
        }

        private void ProcessWork()
        {
            try
            {
                while (true)
                {
                    Frame frame;
                    if (!_queueInput.TryTake(out frame, Timeout.Infinite))
                        return;
                    _queueOutput.Add(_zip.Process(frame));
                }
            }
            catch (InvalidOperationException ioe)
            {
                OnShowMessage(new MessageEventArgs($"Work process {Thread.CurrentThread.Name} was stopped."));
            }
            catch (Exception e)
            {
                OnShowMessage(new MessageEventArgs($"Error in process {Thread.CurrentThread.Name}: {e.Message}"));
            }
        }

        private void WriteWork()
        {
            var writeFrameCount = 0;
            try
            {
                while (true)
                {
                    Frame frame;
                    if (!_queueOutput.TryTake(out frame, Timeout.Infinite))
                        return;
                    _zip.Write(frame);
                    writeFrameCount++;

                    if (writeFrameCount == _zip.FramesCount)
                    {
                        _queueOutput.CompleteAdding();
                        return;
                    }
                }
            }
            catch (InvalidOperationException ioe)
            {
                OnShowMessage(new MessageEventArgs($"Process {Thread.CurrentThread.Name} was stopped."));
            }
            catch (Exception e)
            {
                OnShowMessage(new MessageEventArgs($"Error on write: {e.Message}"));
            }
            finally
            {
                _whaitHandle?.Set();
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