using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace GZip
{
    public delegate void Message(object sender, MessageEventArgs args);

    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    public class GZip: IDisposable
    {
        private Parameters _parameters;
        private FileStream _input;
        private FileStream _output;
        private ThreadsManager _threadsManager;

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
            if (_threadsManager != null)
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
                    case Parameters.OperationType.Compress:
                        break;
                    case Parameters.OperationType.Decompress:
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

        public void Cancel()
        {
            if (_threadsManager == null || _threadsManager.IsWork)
            {
                OnShowMessage(new MessageEventArgs(Properties.Resources.OperationIsNotStarted));
                return;
            }
            _threadsManager.Stop();
        }

        protected static WindowHeader StartCompress(Stream stream)
        {
            var header = new WindowHeader(1, 1, stream.Length);
            FrameHelper.WriteWindowHeader(stream, header);
            return header;
        }


        protected static Frame ReadCompressedFrame(Stream stream)
        {

            var buf = new byte[lengthRead];
            var position = stream.Position;
            stream.Read(buf, (int)stream.Position, lengthRead);
            return new Frame(new FrameHeader(headerId, frameId, position, buf.Length), buf);
        }

        protected static Frame Compress(Frame input)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var stream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    stream.Write(input.Data, 0, input.Data.Length);
                }
                var data = memoryStream.ToArray();
                return new Frame(new FrameHeader( input.Header.HeaderId, input.Header.Id, input.Header.Point, data.Length), data);
            }
        }

        protected static Frame Decompress(Frame input)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var stream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    stream.Write(input.Data, 0, input.Data.Length);
                }
                var data = memoryStream.ToArray();
                return new Frame(new FrameHeader(input.Header.HeaderId, input.Header.Id, input.Header.Point, data.Length), data);
            }
        }

        public void Dispose()
        {
            _input?.Dispose();
            _output?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}