using System.Collections.Generic;
using System.IO;

namespace JenkinsPlug
{
    public class IdMappingsStorage
    {
        public static void Load(
            string mapStorageFile,
            Dictionary<string, string> resolvedIdsCache)
        {
            if (!File.Exists(mapStorageFile))
                return;

            string storedId, storedResolvedUrl;
            foreach (string line in File.ReadAllLines(mapStorageFile))
            {
                if (!IdLineParser.TryParse(line, out storedId, out storedResolvedUrl))
                    continue;

                resolvedIdsCache[storedId] = storedResolvedUrl;
            }
        }

        public static void Save(
            string mapStorageFile,
            Dictionary<string, string> resolvedIdsCache)
        {
            if (!File.Exists(mapStorageFile))
                return;

            if (resolvedIdsCache == null || resolvedIdsCache.Count == 0)
                return;

            using (StreamWriter sw = new StreamWriter(mapStorageFile, false))
            {
                foreach (string id in resolvedIdsCache.Keys)
                {
                    sw.WriteLine(string.Format("{0}{1}{2}",
                        id, IdLineParser.SEPARATOR, resolvedIdsCache[id]));
                }
            }
        }

        public class IdLineParser
        {
            public static bool TryParse(
                string line,
                out string storedQueuedId,
                out string storedResolvedBuildUrl)
            {
                storedQueuedId = string.Empty;
                storedResolvedBuildUrl = string.Empty;

                if (string.IsNullOrEmpty(line))
                    return false;

                line = line.Trim();

                int separatorIndex = line.IndexOf(SEPARATOR);

                if (separatorIndex < 0)
                    return false;

                storedQueuedId = line.Substring(0, separatorIndex).Trim();

                int parsedId;
                if (!int.TryParse(storedQueuedId, out parsedId))
                    return false;

                if (separatorIndex == line.Length - 1)
                    return false;

                storedResolvedBuildUrl = line.Substring(separatorIndex + 1).Trim();
                return true;
            }

            internal const string SEPARATOR = "=";
        }
    }
}
