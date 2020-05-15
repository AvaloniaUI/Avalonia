using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Xml;

namespace Avalonia.Remote.Protocol.Designer
{
    [AvaloniaRemoteMessageGuid("9AEC9A2E-6315-4066-B4BA-E9A9EFD0F8CC")]
    public class UpdateXamlMessage
    {
        public string Xaml { get; set; }
        public string AssemblyPath { get; set; }
        public string XamlFileProjectPath { get; set; }
    }

    [AvaloniaRemoteMessageGuid("B7A70093-0C5D-47FD-9261-22086D43A2E2")]
    public class UpdateXamlResultMessage
    {
        public string Error { get; set; }
        public string Handle { get; set; }
        public ExceptionDetails Exception { get; set; }
    }

    [AvaloniaRemoteMessageGuid("854887CF-2694-4EB6-B499-7461B6FB96C7")]
    public class StartDesignerSessionMessage
    {
        public string SessionId { get; set; }
    }
    
    public class ExceptionDetails
    {
        public ExceptionDetails()
        {
        }

        public ExceptionDetails(Exception e)
        {
            if (e is TargetInvocationException)
            {
                e = e.InnerException;
            }

            ExceptionType = e.GetType().Name;
            Message = e.Message;

            if (e is XmlException xml)
            {
                LineNumber = xml.LineNumber;
                LinePosition = xml.LinePosition;
            }
        }

        public string ExceptionType { get; set; }
        public string Message { get; set; }
        public int? LineNumber { get; set; }
        public int? LinePosition { get; set; }
    }
}
