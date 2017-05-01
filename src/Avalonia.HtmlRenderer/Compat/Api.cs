using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net
{
    internal class AsyncCompletedEventArgs
    {
        public object UserState { get; set; }
        public Exception Error { get; set; }
        public bool Cancelled { get; set; }

        public AsyncCompletedEventArgs(Exception error, bool cancelled, object userState)
        {

        }
    }

    class WebException : Exception
    {
        public object Response { get; set; }
    }

    class HttpWebResponse
    {
        public HttpStatusCode StatusCode { get; set; }
    }
}
