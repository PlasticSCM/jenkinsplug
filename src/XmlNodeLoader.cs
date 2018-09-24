using System;
using System.Xml;

namespace JenkinsPlug
{
    internal static class XmlNodeLoader
    {
        internal static string LoadValue(string rawXmlStr, string xmlNodePath)
        {
            if (string.IsNullOrEmpty(rawXmlStr))
                return null;

            string nodeValue = string.Empty;
            XmlDocument xmlOutput = new XmlDocument();
            xmlOutput.LoadXml(rawXmlStr);

            nodeValue = GetNodeValue(xmlOutput, xmlNodePath);
            return nodeValue;
        }

        internal static string GetNodeValue(XmlDocument xmlOutput, string nodePath)
        {
            if (xmlOutput == null)
                return null;

            XmlNode buildNode = xmlOutput.SelectSingleNode(nodePath);

            if (buildNode == null)
                return null;

            return buildNode.InnerText;
        }
    }
}
