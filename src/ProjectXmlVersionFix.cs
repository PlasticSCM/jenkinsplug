namespace JenkinsPlug
{
    internal static class ProjectXmlVersionFix
    {
        internal static string EnsureV1_0(string projectDescriptorContents, out bool bXmlVersionChanged)
        {
            //The config.xml file of the jenkins project initially comes with xml version 1.1
            //Hack to avoid XmlDocument.LoadXml to fail, as .Net does not support xml version 1.1.
            //We don't care about xml 1.1 to manage the required node values, so we override the
            //version to avoid header check to fail when loading the xml document.

            bXmlVersionChanged = false;
            bool tagFound = projectDescriptorContents.IndexOf(XML_VERSION_1_1_SINGLE_QUOTE) != -1;

            if (tagFound)
            {
                bXmlVersionChanged = true;
                return projectDescriptorContents.Replace(XML_VERSION_1_1_SINGLE_QUOTE, XML_VERSION_1_0);
            }

            tagFound = projectDescriptorContents.IndexOf(XML_VERSION_1_1_DOUBLE_QUOTE) != -1;
            if (tagFound)
            {
                bXmlVersionChanged = true;
                return projectDescriptorContents.Replace(XML_VERSION_1_1_DOUBLE_QUOTE, XML_VERSION_1_0);
            }

            return projectDescriptorContents;
        }

        internal static string RestoreToV1_1(string projectDescriptorContents)
        {
            return projectDescriptorContents.Replace(XML_VERSION_1_0, XML_VERSION_1_1_SINGLE_QUOTE);
        }

        const string XML_VERSION_1_1_SINGLE_QUOTE = "?xml version='1.1'"; //the most common
        const string XML_VERSION_1_1_DOUBLE_QUOTE = "?xml version=\"1.1\"";

        const string XML_VERSION_1_0 = "?xml version=\"1.0\"";
    }
}
