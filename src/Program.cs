using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using log4net;
using log4net.Config;

namespace JenkinsPlug
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                PlugArguments plugArgs = new PlugArguments(args);

                bool bValidArgs = plugArgs.Parse();

                ConfigureLogging(plugArgs.BotName);

                mLog.InfoFormat("JenkinsPlug [{0}] started. Version [{1}]",
                    plugArgs.BotName,
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

                string argsStr = args == null ? string.Empty : string.Join(" ", args);
                mLog.DebugFormat("Args: [{0}]. Are valid args?: [{1}]", argsStr, bValidArgs);

                if (!bValidArgs || plugArgs.ShowUsage)
                {
                    PrintUsage();
                    return 0;
                }

                CheckArguments(plugArgs);

                Config config = ReadConfigFile(plugArgs.ConfigFilePath);

                string authToken = HttpClientBuilder.GetAuthToken(config.User, config.Password);
                using (HttpClient httpClient = HttpClientBuilder.Build(config.Url, authToken))
                {
                    CheckConnection(httpClient, config.Url);

                    string jenkinsIdMapStorageFile = IdMappings.GetIdMappingsFile(config.Url);

                    //if cannot create, fail on app startup, not later!
                    if (!File.Exists(jenkinsIdMapStorageFile))
                    {
                        string parentDir = Path.GetDirectoryName(jenkinsIdMapStorageFile);
                        if (!Directory.Exists(parentDir))
                            Directory.CreateDirectory(parentDir);
                        File.CreateText(jenkinsIdMapStorageFile).Close();
                    }

                    JenkinsQueueToBuildMapper jenkinsIdMapper = new JenkinsQueueToBuildMapper(
                        httpClient,
                        jenkinsIdMapStorageFile);

                    LaunchJenkinsPlug(plugArgs.WebSocketUrl, jenkinsIdMapper, httpClient,
                        plugArgs.BotName, plugArgs.ApiKey);
                }

                return 0;
            }
            catch (Exception e)
            {
                mLog.ErrorFormat("Error: {0}", e.Message);
                mLog.DebugFormat("StackTrace: {0}", e.StackTrace);
                return 1;
            }
        }

        static void CheckArguments(PlugArguments plugArgs)
        {
            CheckAgumentIsNotEmpty(
                "Plastic web socket url endpoint",
                plugArgs.WebSocketUrl,
                "web socket url",
                "--server wss://localhost:7111/plug");

            CheckAgumentIsNotEmpty("name for this bot", plugArgs.BotName, "name",
                "--name jenkis-main");
            CheckAgumentIsNotEmpty("connection API key", plugArgs.ApiKey, "api key",
                "--apikey 014B6147A6391E9F4F9AE67501ED690DC2D814FECBA0C1687D016575D4673EE3");
            CheckAgumentIsNotEmpty("JSON config file", plugArgs.ConfigFilePath, "file path",
                "--config jenkis-main.conf");
        }

        static void CheckConnection(HttpClient httpClient, string url)
        {
            if (JenkinsBuild.CheckConnection(httpClient))
                return;

            throw new Exception(string.Format(
                "Unable to contact jenkins server '{0}' with specified credentials.", url));
        }

        static void LaunchJenkinsPlug(
            string serverUrl,
            JenkinsQueueToBuildMapper jenkinsIdMapper,
            HttpClient httpClient,
            string name,
            string apiKey)
        {
            jenkinsIdMapper.Start();

            WebSocketClient ws = new WebSocketClient(
                serverUrl,
                "ciPlug",
                name,
                apiKey,
                new WebSocketRequest(httpClient, jenkinsIdMapper).ProcessMessage);

            ws.ConnectWithRetries();

            Task.Delay(-1).Wait();
        }

        static Config ReadConfigFile(string botFilePath)
        {
            try
            {
                string fileContent = System.IO.File.ReadAllText(botFilePath);
                Config result = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(fileContent);

                if (result == null)
                {
                    throw new Exception(
                        string.Format("Config file '{0}' is not valid", botFilePath));
                }

                CheckFieldIsNotEmpty("url", result.Url);
                CheckFieldIsNotEmpty("user", result.User);
                CheckFieldIsNotEmpty("password", result.Password);

                return result;
            }
            catch (Exception e)
            {
                throw new Exception("The config cannot be loaded. Error: " + e.Message);
            }
        }

        static void CheckAgumentIsNotEmpty(
            string fielName, string fieldValue, string type, string example)
        {
            if (!string.IsNullOrEmpty(fieldValue))
                return;
            string message = string.Format("jenkinsplug can't start without specifying a {0}.{1}" +
                "Please type a valid {2}. Example:  \"{3}\"",
                fielName, Environment.NewLine, type, example);
            throw new Exception(message);
        }

        static void CheckFieldIsNotEmpty(string fieldName, string fieldValue)
        {
            if (!string.IsNullOrEmpty(fieldValue))
                return;

            throw new Exception(string.Format(
                "The field '{0}' must be defined in the config", fieldName));
        }

        static void ConfigureLogging(string plugName)
        {
            if (string.IsNullOrEmpty(plugName))
                plugName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm");

            try
            {
                string log4netpath = LogConfig.GetLogConfigFile();
                log4net.GlobalContext.Properties["Name"] = plugName;
                XmlConfigurator.Configure(new FileInfo(log4netpath));
            }
            catch
            {
                //it failed configuring the logging info; nothing to do.
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("\tjenkinsplug.exe --server <WEB_SOCKET_URL> --config <JSON_CONFIG_FILE_PATH>");
            Console.WriteLine("\t                --apikey <WEB_SOCKET_CONN_KEY> --name <PLUG_NAME>");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("\tjenkinsplug.exe --server wss://localhost:7111/plug --name jenkis-main ");
            Console.WriteLine("\t                --apikey 014B6147A6391E9F4F9AE67501ED690DC2D814FECBA0C1687D016575D4673EE3");
            Console.WriteLine("\t                --config jenkis-main.conf");
            Console.WriteLine();
        }

        static class HttpClientBuilder
        {
            internal static string GetAuthToken(string user, string password)
            {
                return Convert.ToBase64String(
                    System.Text.ASCIIEncoding.ASCII.GetBytes(user + ":" + password));
            }

            internal static HttpClient Build(string host, string authHeader)
            {
                HttpClient httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(host);

                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

                httpClient.DefaultRequestHeaders.ConnectionClose = false;
                return httpClient;
            }
        }

        static readonly ILog mLog = LogManager.GetLogger("jenkinsplug");
    }
}
