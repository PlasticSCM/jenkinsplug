using System;
using System.IO;
using System.Reflection;

namespace JenkinsPlug
{
    internal static class IdMappings
    {
        internal static string GetIdMappingsFile(string jenkinsUrl)
        {
            string clearServerFileName = GetClearFileNameFor(jenkinsUrl);
            return Path.Combine(GetMappingsDirectory(), clearServerFileName);
        }

        static string GetClearFileNameFor(string jenkinsUrl)
        {
            return "jenkins_id_mappings_" + GetCleanNameForServer(jenkinsUrl) + ".conf";
        }

        static string GetCleanNameForServer(string jenkinsUrl)
        {
            string[] parts = jenkinsUrl.Split(
                new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts == null || parts.Length < 2)
                return NormalizeName(jenkinsUrl);

            return NormalizeName(parts[1]);
        }

        static string NormalizeName(string nameToNormalize)
        {
            char[] forbiddenChars = new char[] {'/', '\\', '<', '>', ':', '"', '|', '?', '*' };

            if (nameToNormalize.IndexOfAny(forbiddenChars) == -1)
                return nameToNormalize;

            foreach (char character in forbiddenChars)
                nameToNormalize = nameToNormalize.Replace(character, '_');

            return nameToNormalize;
        }

        static string GetMappingsDirectory()
        {
            string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(appPath, "mapping");
        }
    }
}
