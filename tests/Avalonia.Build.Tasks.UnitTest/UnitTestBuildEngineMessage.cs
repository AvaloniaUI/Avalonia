using Microsoft.Build.Framework;

namespace Avalonia.Build.Tasks.UnitTest;

enum MessageSource
{
    Unknown,
    ErrorEvent,
    MessageEvent,
    CustomEvent,
    WarningEvent
}

record class UnitTestBuildEngineMessage
{
    private UnitTestBuildEngineMessage(MessageSource Type, LazyFormattedBuildEventArgs Source)
    {
        this.Type = Type;
        this.Source = Source;
        Message = Source.Message;
    }

    public MessageSource Type { get; }
    public LazyFormattedBuildEventArgs Source { get; }
    public string Message { get; }

    public static UnitTestBuildEngineMessage From(BuildWarningEventArgs buildWarning) =>
        new UnitTestBuildEngineMessage(MessageSource.WarningEvent, buildWarning);

    public static UnitTestBuildEngineMessage From(BuildMessageEventArgs buildMessage) =>
        new UnitTestBuildEngineMessage(MessageSource.MessageEvent, buildMessage);

    public static UnitTestBuildEngineMessage From(BuildErrorEventArgs buildError) =>
        new UnitTestBuildEngineMessage(MessageSource.ErrorEvent, buildError);

    public static UnitTestBuildEngineMessage From(CustomBuildEventArgs customBuild) =>
        new UnitTestBuildEngineMessage(MessageSource.CustomEvent, customBuild);

}
