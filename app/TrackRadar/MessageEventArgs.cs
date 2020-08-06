using System;

namespace TrackRadar
{
    public sealed class MessageEventArgs : EventArgs
    {
        public string Message { get; }

        public MessageEventArgs(string message)
        {
            this.Message = message;
        }
    }
}