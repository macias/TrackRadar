using System;

namespace TrackRadar
{
    public sealed class ProgressEventArgs : EventArgs
    {
        public string Message { get; }
        public double Progress { get; }

        public ProgressEventArgs(string message, double progress)
        {
            this.Message = message;
            this.Progress = progress;
        }
    }
}