using Numerge;
using static Serilog.Log;

public partial class Build
{
    class NumergeNukeLogger : INumergeLogger
    {
        public void Log(NumergeLogLevel level, string message)
        {
            if(level == NumergeLogLevel.Error)
                Error(message);
            else if (level == NumergeLogLevel.Warning)
                Warning(message);
            else
                Information(message);
        }
    }
}
