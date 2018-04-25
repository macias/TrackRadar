using System;

namespace TrackRadar
{
    public sealed class MessageEventArgs : EventArgs
    {
        public string Message { get; }

        public MessageEventArgs(string s)
        {
            this.Message = s;
        }
    }
}