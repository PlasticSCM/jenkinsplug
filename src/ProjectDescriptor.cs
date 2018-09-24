using System.Collections.Generic;
using System.Xml;

namespace JenkinsPlug
{
    public static class ProjectDescriptor
    {
        public static XmlDocument Parse(string projectDescriptorContents)
        {
            XmlDocument xmlOutput = new XmlDocument();
            xmlOutput.LoadXml(projectDescriptorContents);

            return xmlOutput;
        }

        public static string GetAuthToken(XmlDocument projectDescriptor)
        {
            return XmlNodeLoader.GetNodeValue(projectDescriptor, "/*/authToken");
        }

        public static List<string> GetMissingParameters(
            XmlDocument projectDescriptor,
            List<BuildProperty> requestedProperties)
        {
            List<string> missingParamsToCreate = new List<string>();

            foreach (BuildProperty property in requestedProperties)
                missingParamsToCreate.Add(property.Name);

            XmlNodeList existingParams = projectDescriptor.SelectNodes(
                "/*/properties/hudson.model.ParametersDefinitionProperty" +
                "/parameterDefinitions/*/name");

            foreach (XmlNode node in existingParams)
            {
                if (!missingParamsToCreate.Contains(node.InnerText))
                    continue;

                missingParamsToCreate.Remove(node.InnerText);
            }

            return missingParamsToCreate;
        }

        public static void AddMissingParameters(
            XmlDocument projectDescriptor,
            List<string> missingParameters,
            string outputXmlFile)
        {
            if (missingParameters == null || missingParameters.Count == 0)
            {
                projectDescriptor.Save(outputXmlFile);
                return;
            }

            bool bIsEmptyParametersProject = IsNotParametrizedProject(projectDescriptor);

            ModifyProperties(projectDescriptor, bIsEmptyParametersProject, missingParameters);

            projectDescriptor.Save(outputXmlFile);
        }

        static bool IsNotParametrizedProject(XmlDocument projectDescriptor)
        {
            XmlNodeList parameterDefinitionMainNode = projectDescriptor.SelectNodes(
                "/*/properties/hudson.model.ParametersDefinitionProperty");

            if (parameterDefinitionMainNode == null || parameterDefinitionMainNode.Count == 0)
                return true;

            XmlNodeList parameterDefinitionsNodes = projectDescriptor.SelectNodes(
                "/*/properties/hudson.model.ParametersDefinitionProperty/parameterDefinitions");

            if (parameterDefinitionsNodes == null || parameterDefinitionsNodes.Count == 0)
                return true;

            return false;
        }

        static void ModifyProperties(
            XmlDocument projectDescriptor,
            bool bIsEmptyParametersProject,
            List<string> missingParameters)
        {
            XmlNode propertiesMainNode = projectDescriptor.SelectSingleNode("/*/properties");
            if (propertiesMainNode == null)
                return;

            if (bIsEmptyParametersProject)
            {
                propertiesMainNode.RemoveAll();
                AddParametersStructure(projectDescriptor, propertiesMainNode);
            }

            XmlNode parameterDefinitionsNode = projectDescriptor.SelectSingleNode(
                "/*/properties/hudson.model.ParametersDefinitionProperty/parameterDefinitions");

            foreach (string missingParam in missingParameters)
                AddStringParameter(projectDescriptor, parameterDefinitionsNode, missingParam);
        }

        static void AddParametersStructure(XmlDocument projectDescriptor, XmlNode propertiesMainNode)
        {
            XmlElement parametersDefinitionProperty = projectDescriptor.CreateElement(
                "hudson.model.ParametersDefinitionProperty");
            propertiesMainNode.AppendChild(parametersDefinitionProperty);

            XmlElement parameterDefinitions = projectDescriptor.CreateElement(
                "parameterDefinitions");

            parametersDefinitionProperty.AppendChild(parameterDefinitions);
        }

        static void AddStringParameter(
            XmlDocument projectDescriptor,
            XmlNode parameterDefinitionsNode,
            string missingParam)
        {
            XmlElement name = projectDescriptor.CreateElement("name");
            XmlText nameValue = projectDescriptor.CreateTextNode(missingParam);
            name.AppendChild(nameValue);

            XmlElement description = projectDescriptor.CreateElement("description");
            XmlElement defaultValue = projectDescriptor.CreateElement("defaultValue");
            

            XmlElement trim = projectDescriptor.CreateElement("trim");
            XmlText trimValue = projectDescriptor.CreateTextNode("false");
            trim.AppendChild(trimValue);

            XmlElement stringParameterDefinition = projectDescriptor.CreateElement(
               "hudson.model.StringParameterDefinition");

            stringParameterDefinition.AppendChild(name);
            stringParameterDefinition.AppendChild(description);
            stringParameterDefinition.AppendChild(defaultValue);
            stringParameterDefinition.AppendChild(trim);

            parameterDefinitionsNode.AppendChild(stringParameterDefinition);
        }
    }
}
