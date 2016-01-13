using System;

namespace ServiceLibrary
{
    public class StatusEventArgs : EventArgs
    {
        public StatusEventArgs(string s)
        {
            Message = s;
        }

        public string Message { get; }
    }
}