using System;

using log4net;

namespace JenkinsPlug
{
    internal static class ExceptionLogger
    {
        internal static void Log(Exception ex)
        {
            string messageToLog = GetMessageToLog(ex);

            mLog.Error(messageToLog);

            mLog.Debug(ex.StackTrace);
        }

        internal static string GetMessageToLog(Exception ex)
        {
            string exceptionErrorMsg = GetErrorMessage(ex);
            string innerExceptionErrorMsg = GetErrorMessage(ex == null ? null : ex.InnerException);

            bool bHasInnerEx = !string.IsNullOrEmpty(innerExceptionErrorMsg);

            return string.Format("{0}{1}{2}{3}",
                exceptionErrorMsg,
                bHasInnerEx ? " - [" : string.Empty,
                innerExceptionErrorMsg,
                bHasInnerEx ? "]" : string.Empty);
        }

        static string GetErrorMessage(Exception ex)
        {
            if (ex == null || string.IsNullOrEmpty(ex.Message))
                return string.Empty;

            return ex.Message;
        }

        static readonly ILog mLog = LogManager.GetLogger("jenkinsplug");
    }
}
