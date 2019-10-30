using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using log4net;

namespace JenkinsPlug
{
    internal class WebSocketRequest
    {
        internal WebSocketRequest(
            HttpClient httpClient,
            JenkinsQueueToBuildMapper jenkinsIdMapper)
        {
            mHttpClient = httpClient;
            mJenkinsIdMapper = jenkinsIdMapper;
        }

        internal async Task<string> ProcessMessage(string message)
        {
            string requestId = Messages.GetRequestId(message);
            string type = string.Empty;
            try
            {
                type = Messages.GetActionType(message);
                switch (type)
                {
                    case "launchplan":
                        return await ProcessLaunchPlanMessage(
                            requestId,
                            Messages.ReadLaunchPlanMessage(message),
                            mJenkinsIdMapper,
                            mHttpClient);

                    case "getstatus":
                        return await ProcessGetStatusMessage(
                            requestId,
                            Messages.ReadGetStatusMessage(message),
                            mJenkinsIdMapper,
                            mHttpClient);

                    default:
                        return Messages.BuildErrorResponse(requestId,
                            string.Format("The action '{0}' is not supported", type));
                }
            }
            catch (Exception ex)
            {
                mLog.ErrorFormat("Error processing message {0}: \nMessage:{1}. Error: {2}",
                    type, message, ex.Message);
                ExceptionLogger.Log(ex);
                return Messages.BuildErrorResponse(requestId, ex.Message);
            }
        }

        internal static async Task<string> ProcessLaunchPlanMessage(
            string requestId,
            LaunchPlanMessage message,
            JenkinsQueueToBuildMapper jenkinsIdMapper,
            HttpClient jenkinsHttpClient)
        {
            LogLaunchPlanMessage(message);

            string jenkinsQueuedItemId = await JenkinsBuild.QueueBuildAsync(
                message.PlanName,
                message.ObjectSpec,
                message.Comment,
                message.Properties,
                jenkinsHttpClient);

            jenkinsIdMapper.SetAsPendingToResolve(jenkinsQueuedItemId);

            return Messages.BuildLaunchPlanResponse(requestId, jenkinsQueuedItemId);
        }

        internal static async Task<string> ProcessGetStatusMessage(
            string requestId,
            GetStatusMessage message,
            JenkinsQueueToBuildMapper jenkinsIdMapper,
            HttpClient jenkinsHttpClient)
        {
            LogGetStatusMessage(message);

            BuildStatus status = await JenkinsBuild.QueryStatusAsync(
                message.ExecutionId,
                jenkinsIdMapper,
                jenkinsHttpClient);

            bool bIsFinished;
            bool bIsSuccessful;
            ParseStatus(status, out bIsFinished, out bIsSuccessful);

            if (bIsFinished)
                jenkinsIdMapper.Clear(message.ExecutionId);

#warning jenkins API wrapper does not retrieve an explanation yet.
            return Messages.BuildGetStatusResponse(
                requestId, bIsFinished, bIsSuccessful, string.Empty);
        }

        static void ParseStatus(BuildStatus status, out bool bIsFinished, out bool bIsSuccessful)
        {
            if (status == null)
            {
                bIsFinished = true;
                bIsSuccessful = false;
                return;
            }

            if (string.IsNullOrEmpty(status.BuildResult))
            {
                bIsFinished = false;
                bIsSuccessful = false;
                return;
            }

            bIsFinished = status.Progress.Equals(
                JenkinsBuild.FINISHED_BUILD_TAG, StringComparison.InvariantCultureIgnoreCase);

            bIsSuccessful = status.BuildResult.Equals(
                JenkinsBuild.SUCESSFUL_BUILD_TAG, StringComparison.InvariantCultureIgnoreCase);
        }

        static void LogLaunchPlanMessage(LaunchPlanMessage message)
        {
            mLog.InfoFormat("Launch plan was requested. Fields:");
            mLog.InfoFormat("\tPlanName: " + message.PlanName);
            mLog.InfoFormat("\tObjectSpec: " + message.ObjectSpec);
            mLog.InfoFormat("\tComment: " + message.Comment);
            mLog.InfoFormat("\tProperties:");

            foreach (KeyValuePair<string, string> pair in message.Properties)
                mLog.InfoFormat("\t\t{0}: {1}", pair.Key, pair.Value);
        }

        static void LogGetStatusMessage(GetStatusMessage message)
        {
            mLog.InfoFormat("Plan status requested. Fields:");
            mLog.InfoFormat("\tPlanName: " + message.PlanName);
            mLog.InfoFormat("\tExecutionId: " + message.ExecutionId);
        }

        readonly HttpClient mHttpClient;
        readonly JenkinsQueueToBuildMapper mJenkinsIdMapper;

        static readonly ILog mLog = LogManager.GetLogger("jenkinsplug");
    }
}
