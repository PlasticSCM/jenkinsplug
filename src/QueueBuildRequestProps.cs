using System.Collections.Generic;

namespace JenkinsPlug
{
    internal static class QueueBuildRequestProps
    {
        internal static List<BuildProperty> Create(
            string plasticUpdateToSpec,
            Dictionary<string, string> customBotProperties)
        {
            List<BuildProperty> result = new List<BuildProperty>();
            result.Add(new BuildProperty(PLASTIC_PROPERTY_UPDATE_SPEC, plasticUpdateToSpec));

            AddCustomProperties(customBotProperties, result);
            return result;
        }

        static void AddCustomProperties(
           Dictionary<string, string> customBotProperties,
           List<BuildProperty> result)
        {
            if (customBotProperties == null || customBotProperties.Count == 0)
                return;

            BuildProperty customProperty = null;

            foreach (string key in customBotProperties.Keys)
            {
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(customBotProperties[key]))
                    continue;

                string jenkinsKey = BOT_BUILD_PROPERTY_PREFIX
                    + key.Replace(".", "_").ToUpperInvariant();
                customProperty = new BuildProperty(
                    jenkinsKey, customBotProperties[key]);

                result.Add(customProperty);
            }
        }

        const string BOT_BUILD_PROPERTY_PREFIX = "PLASTICSCM_MERGEBOT_";
        const string PLASTIC_PROPERTY_UPDATE_SPEC = BOT_BUILD_PROPERTY_PREFIX + "UPDATE_SPEC";
    }
}
