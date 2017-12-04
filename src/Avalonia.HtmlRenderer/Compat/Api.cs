using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net
{
    class WebException : Exception
    {
        public object Response { get; set; }
    }

    class HttpWebResponse
    {
        public HttpStatusCode StatusCode { get; set; }
    }
}
