using System;

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

    public class GZip
    {
        private Parameters _parameters;

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
            throw new NotImplementedException();
        }

        public void Cancel()
        {
            throw new NotImplementedException();
        }
    }
}