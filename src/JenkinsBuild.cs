using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace JenkinsPlug
{
    internal static class JenkinsBuild
    {
        internal class Crumb
        {
            internal string FieldName;
            internal string Value;
        }

        internal static Crumb GetCrumb(HttpClient httpClient)
        {
            string endPoint = string.Format(GET_CRUMB_URI);

            string response = GetStringResponseAsync(endPoint, httpClient).Result;

            if (string.IsNullOrEmpty(response))
                return null;

            Crumb crumb = new Crumb();

            crumb.FieldName = XmlNodeLoader.LoadValue(
                response, "/defaultCrumbIssuer/crumbRequestField");
            crumb.Value = XmlNodeLoader.LoadValue(
                response, "/defaultCrumbIssuer/crumb");

            return crumb;
        }

        internal static bool CheckConnection(HttpClient httpClient)
        {
            HttpResponseMessage response = null;
            try
            {
                response = httpClient.GetAsync(GET_QUEUE_URI).Result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.Log(ex);
                return false;
            }

            return response.IsSuccessStatusCode;
        }

        internal static async Task<string> QueueBuildAsync(
            string projectName,
            string plasticUpdateToSpec,
            string buildComment,
            Dictionary<string, string> botRequestProperties,
            HttpClient httpClient)
        {
            XmlDocument projectDescriptor = null;
            bool bXmlVersionChanged = false;

            string projectDescriptorContents = await GetProjectDescriptorAsync(projectName, httpClient);

            projectDescriptorContents = ProjectXmlVersionFix.EnsureV1_0(
                projectDescriptorContents, out bXmlVersionChanged);

            projectDescriptor = ProjectDescriptor.Parse(projectDescriptorContents);

            List<BuildProperty> requestProperties = QueueBuildRequestProps.Create(plasticUpdateToSpec, botRequestProperties);
            string projectAuthToken = string.Empty;

            projectAuthToken = ProjectDescriptor.GetAuthToken(projectDescriptor);

            List<string> pendingParametersToConfigure =
                ProjectDescriptor.GetMissingParameters(projectDescriptor, requestProperties);

            if (pendingParametersToConfigure.Count > 0)
                await ModifyJenkinsProjectAsync(
                    httpClient,
                    projectName,
                    projectDescriptor,
                    pendingParametersToConfigure,
                    bXmlVersionChanged);

            if (!string.IsNullOrEmpty(projectAuthToken))
                requestProperties.Add(new BuildProperty("token", projectAuthToken));

            if (!string.IsNullOrEmpty(buildComment))
                requestProperties.Add(new BuildProperty("cause", buildComment));

            string endPoint = Uri.EscapeUriString(
                string.Format(
                    QUEUE_BUILD_URI_FORMAT,
                    projectName,
                    BuildPropertiesUri(requestProperties)));

            HttpResponseMessage response = await httpClient.PostAsync(endPoint, null);

            if (!response.IsSuccessStatusCode)
                return string.Empty;

            if (response.Headers.Location != null)
                return ParseBuildNumberFromLocationPathHeader(
                    response.Headers.Location.AbsolutePath);

            return string.Empty;
        }

        internal static async Task<BuildStatus> QueryStatusAsync(
            string projectName,
            string queuedItemId,
            JenkinsQueueToBuildMapper jenkinsIdMapper,
            HttpClient httpClient)
        {
            if (jenkinsIdMapper.IsPendingToResolve(queuedItemId))
            {
                return QueuedStatus;
            }

            string buildIdUrl = jenkinsIdMapper.GetBuildUrl(queuedItemId);

            if (string.IsNullOrEmpty(buildIdUrl))
                return null;

            string buildIdEndpoint = string.Format("{0}{1}api/xml",
                buildIdUrl,
                buildIdUrl.EndsWith("/") ? string.Empty : "/");

            HttpResponseMessage response = await httpClient.GetAsync(buildIdEndpoint);

            if (!response.IsSuccessStatusCode)
                return null;

            string responseStr = await response.Content.ReadAsStringAsync();

            BuildStatus status = new BuildStatus();
            status.BuildResult = XmlNodeLoader.LoadValue(responseStr, "/*/result");
            status.Progress = ParseBuildingTag(XmlNodeLoader.LoadValue(responseStr, "/*/building"));

            return status;
        }

        internal static async Task<string> GetProjectDescriptorAsync(
            string projectName,
            HttpClient httpClient)
        {
            string endPoint = string.Format(GET_JOB_CONFIG_URI, projectName);
            return await GetStringResponseAsync(endPoint, httpClient);
        }

        static async Task<string> GetStringResponseAsync(string endPoint, HttpClient httpClient)
        {
            HttpResponseMessage response = await httpClient.GetAsync(endPoint);

            if (!response.IsSuccessStatusCode)
                return string.Empty;

            return await response.Content.ReadAsStringAsync();
        }

        static async Task ModifyJenkinsProjectAsync(
            HttpClient httpClient,
            string projectName,
            XmlDocument projectDescriptor,
            List<string> pendingParametersToConfigure,
            bool bXmlVersionChanged)
        {
            string modifiedXmlProject = Path.GetTempFileName();

            try
            {
                ProjectDescriptor.AddMissingParameters(
                    projectDescriptor,
                    pendingParametersToConfigure,
                    modifiedXmlProject);

                string payLoadStr = File.ReadAllText(modifiedXmlProject);
                if (bXmlVersionChanged)
                    payLoadStr = ProjectXmlVersionFix.RestoreToV1_1(payLoadStr);

                var payLoad = new StringContent(
                    payLoadStr,
                    System.Text.Encoding.UTF8,
                    "application/xml");

                string endPoint = string.Format(GET_JOB_CONFIG_URI, projectName);
                HttpResponseMessage response = await httpClient.PostAsync(endPoint, payLoad);

                if (response.IsSuccessStatusCode)
                    return;

                throw new InvalidOperationException(string.Format(
                    "Unable to update config.xml file for project [{0}] " +
                    "in order to setup required build parameters: {1}",
                    projectName, response.ReasonPhrase));
            }
            finally
            {
                if (File.Exists(modifiedXmlProject))
                    File.Delete(modifiedXmlProject);
            }
        }

        static string BuildPropertiesUri(List<BuildProperty> payloadProps)
        {
            string result = string.Empty;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < payloadProps.Count; i++)
            {
                sb.AppendFormat("{0}{1}={2}",
                    i == 0 ? string.Empty : "&",
                    payloadProps[i].Name,
                    payloadProps[i].Value);
            }

            return sb.ToString();
        }

        static string ParseBuildNumberFromLocationPathHeader(string absolutePath)
        {
            return absolutePath.
                Replace("queue", string.Empty).
                Replace("item", string.Empty).
                Replace("/", string.Empty).
                Trim();
        }

        static string ParseBuildingTag(string buildingTagValue)
        {
            if (string.IsNullOrEmpty(buildingTagValue))
                return UNDEFINED_BUILD_TAG;

            if (buildingTagValue.ToLower().Trim().Equals("false"))
                return FINISHED_BUILD_TAG;

            if (buildingTagValue.ToLower().Trim().Equals("true"))
                return INPROGRESS_BUILD_TAG;

            //should not happen...
            return UNDEFINED_BUILD_TAG;
        }

        internal const string SUCESSFUL_BUILD_TAG = "SUCCESS";
        internal const string FINISHED_BUILD_TAG = "finished";
        const string QUEUED_BUILD_TAG = "queued";
        const string UNDEFINED_BUILD_TAG = "undefined";
        const string INPROGRESS_BUILD_TAG = "in_progress";

        const string GET_CRUMB_URI = "crumbIssuer/api/xml";
        const string GET_JOB_CONFIG_URI = "job/{0}/config.xml";

        const string QUEUE_BUILD_URI_FORMAT = "job/{0}/buildWithParameters?{1}";

        const string GET_QUEUE_URI = "queue/api/xml";

        const string QUERY_BUILD_URI_FORMAT = "job/{0}/{1}/api/xml";

        static readonly BuildStatus QueuedStatus = new BuildStatus()
            { Progress = QUEUED_BUILD_TAG, BuildResult = string.Empty };
    }
}
