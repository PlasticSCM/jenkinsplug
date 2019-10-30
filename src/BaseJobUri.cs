namespace JenkinsPlug
{
    public static class BaseJobUri
    {
        public static string Get(string projectNameOrPath)
        {
            return "job/" + projectNameOrPath.Replace("/", "/job/");
        }
    }
}
