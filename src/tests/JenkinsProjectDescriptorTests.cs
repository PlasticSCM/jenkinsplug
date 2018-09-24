using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace JenkinsPlug.Tests
{
    [TestFixture]
    public class JenkinsProjectDescriptorTests
    {
        [Test]
        public void ParseAuthTokenTest()
        {
            XmlDocument descriptor = ProjectDescriptor.Parse(XML_WITH_PROPERTY);
            Assert.AreEqual("01234", ProjectDescriptor.GetAuthToken(descriptor),
                "Unexpected value of auth token");
        }

        [Test]
        public void ParseEmptyAuthTokenTest()
        {
            XmlDocument descriptor = ProjectDescriptor.Parse(XML_NO_AUTH_TOKEN);
            Assert.IsNull(
                ProjectDescriptor.GetAuthToken(descriptor),
                "Unexpected value of auth token");
        }

        [Test]
        public void ParsePendingParametersNonParametrizedProjectTest()
        {
            List<BuildProperty> props = new List<BuildProperty>();
            props.Add(new BuildProperty("plasticscm.mergebot.update.spec", "cs:0"));
            props.Add(new BuildProperty("anotheParam", "paramValue"));

            XmlDocument descriptor = ProjectDescriptor.Parse(XML_WITHOUT_PROPERTIES);

            List<string> missingParams =
                ProjectDescriptor.GetMissingParameters(descriptor, props);

            Assert.AreEqual(2, missingParams.Count,
                "unexpected number of calculated missing parameters for project descriptor");

            Assert.Contains("plasticscm.mergebot.update.spec", missingParams,
                "missing parameter for project descriptor not included.");

            Assert.Contains("anotheParam", missingParams,
                "missing parameter for project descriptor not included.");
        }

        [Test]
        public void ParsePendingParametersAlreadyExistsOneTest()
        {
            List<BuildProperty> props = new List<BuildProperty>();
            props.Add(new BuildProperty("plasticscm.mergebot.update.spec", "cs:0"));
            props.Add(new BuildProperty("anotheParam", "paramValue"));

            XmlDocument descriptor = ProjectDescriptor.Parse(XML_WITH_PROPERTY);

            List<string> missingParams =
                ProjectDescriptor.GetMissingParameters(descriptor, props);

            Assert.AreEqual(1, missingParams.Count,
                "unexpected number of calculated missing parameters for project descriptor");

            Assert.AreEqual("anotheParam", missingParams[0],
                "unexpected name of calculated missing parameter for project descriptor");
        }

        [Test]
        public void AddMissingParametersEmptyProjectTest()
        {
            XmlDocument descriptor = ProjectDescriptor.Parse(XML_WITHOUT_PROPERTIES);

            List<string> missingParams = new List<string>();
            missingParams.Add("plasticscm.mergebot.update.spec");

            string outputFile = Path.GetTempFileName();

            try
            {
                ProjectDescriptor.AddMissingParameters(descriptor, missingParams, outputFile);
                XmlDocument descriptorModified = ProjectDescriptor.Parse(File.ReadAllText(outputFile));

                XmlNodeList existingParams = descriptorModified.SelectNodes(
                    "/*/properties/hudson.model.ParametersDefinitionProperty" +
                    "/parameterDefinitions/*/name");

                Assert.AreEqual(1, existingParams.Count,
                    "Unexpected number of parameters after adding required missing parameters");

                Assert.AreEqual("plasticscm.mergebot.update.spec", existingParams[0].InnerText,
                    "Unexpected name of parameter after adding required missing parameters");

            }
            finally
            {
                if (File.Exists(outputFile))
                    File.Delete(outputFile);
            }
        }

        [Test]
        public void AddMissingParametersEmptyParamDefinitionsTest()
        {
            XmlDocument descriptor = ProjectDescriptor.Parse(XML_WITHOUT_PARAMETERS_DEFINITIONS);

            List<string> missingParams = new List<string>();
            missingParams.Add("plasticscm.mergebot.update.spec");

            string outputFile = Path.GetTempFileName();

            try
            {
                ProjectDescriptor.AddMissingParameters(descriptor, missingParams, outputFile);
                XmlDocument descriptorModified = ProjectDescriptor.Parse(File.ReadAllText(outputFile));

                XmlNodeList existingParams = descriptorModified.SelectNodes(
                    "/*/properties/hudson.model.ParametersDefinitionProperty" +
                    "/parameterDefinitions/*/name");

                Assert.AreEqual(1, existingParams.Count,
                    "Unexpected number of parameters after adding required missing parameters");

                Assert.AreEqual("plasticscm.mergebot.update.spec", existingParams[0].InnerText,
                    "Unexpected name of parameter after adding required missing parameters");

            }
            finally
            {
                if (File.Exists(outputFile))
                    File.Delete(outputFile);
            }
        }

        [Test]
        public void AddMissingParametersEmptyParamDefinitionPropertyTest()
        {
            XmlDocument descriptor = ProjectDescriptor.Parse(XML_WITH_EMPTY_PARAMETERS_DEFINITION_PROPERTY);

            List<string> missingParams = new List<string>();
            missingParams.Add("plasticscm.mergebot.update.spec");

            string outputFile = Path.GetTempFileName();

            try
            {
                ProjectDescriptor.AddMissingParameters(descriptor, missingParams, outputFile);
                XmlDocument descriptorModified = ProjectDescriptor.Parse(File.ReadAllText(outputFile));

                XmlNodeList existingParams = descriptorModified.SelectNodes(
                    "/*/properties/hudson.model.ParametersDefinitionProperty" +
                    "/parameterDefinitions/*/name");

                Assert.AreEqual(1, existingParams.Count,
                    "Unexpected number of parameters after adding required missing parameters");

                Assert.AreEqual("plasticscm.mergebot.update.spec", existingParams[0].InnerText,
                    "Unexpected name of parameter after adding required missing parameters");

            }
            finally
            {
                if (File.Exists(outputFile))
                    File.Delete(outputFile);
            }
        }

        [Test]
        public void AddMissingParametersOnAlreadyParametrizedProjectTest()
        {
            XmlDocument descriptor = ProjectDescriptor.Parse(XML_WITH_PROPERTY);

            List<string> missingParams = new List<string>();
            missingParams.Add("plasticscm.mergebot.branchName");

            string outputFile = Path.GetTempFileName();

            try
            {
                ProjectDescriptor.AddMissingParameters(descriptor, missingParams, outputFile);
                XmlDocument descriptorModified = ProjectDescriptor.Parse(File.ReadAllText(outputFile));

                XmlNodeList existingParams = descriptorModified.SelectNodes(
                    "/*/properties/hudson.model.ParametersDefinitionProperty" +
                    "/parameterDefinitions/*/name");

                Assert.AreEqual(2, existingParams.Count,
                    "Unexpected number of parameters after adding required missing parameters");

                Assert.AreEqual("plasticscm.mergebot.update.spec", existingParams[0].InnerText,
                    "Unexpected name of parameter after adding required missing parameters");

                Assert.AreEqual("plasticscm.mergebot.branchName", existingParams[1].InnerText,
                    "Unexpected name of parameter after adding required missing parameters");

            }
            finally
            {
                if (File.Exists(outputFile))
                    File.Delete(outputFile);
            }
        }


        [Test]
        public void AddEmptyMissingParametersDontFailTest()
        {
            XmlDocument descriptor = ProjectDescriptor.Parse(XML_WITH_PROPERTY);

            List<string> missingParams = new List<string>();

            string outputFile = Path.GetTempFileName();

            try
            {
                ProjectDescriptor.AddMissingParameters(descriptor, missingParams, outputFile);
                XmlDocument descriptorModified = ProjectDescriptor.Parse(File.ReadAllText(outputFile));

                XmlNodeList existingParams = descriptorModified.SelectNodes(
                    "/*/properties/hudson.model.ParametersDefinitionProperty" +
                    "/parameterDefinitions/*/name");

                Assert.AreEqual(1, existingParams.Count,
                    "Unexpected number of parameters after adding required missing parameters");

                Assert.AreEqual("plasticscm.mergebot.update.spec", existingParams[0].InnerText,
                    "Unexpected name of parameter after adding required missing parameters");

            }
            finally
            {
                if (File.Exists(outputFile))
                    File.Delete(outputFile);
            }
        }

        const string XML_WITH_PROPERTY = @"
<project>
  <actions/>
  <description></description>
  <keepDependencies>false</keepDependencies>
  <properties>
    <hudson.model.ParametersDefinitionProperty>
      <parameterDefinitions>
        <hudson.model.StringParameterDefinition>
          <name>plasticscm.mergebot.update.spec</name>
          <description></description>
          <defaultValue></defaultValue>
          <trim>false</trim>
        </hudson.model.StringParameterDefinition>
      </parameterDefinitions>
    </hudson.model.ParametersDefinitionProperty>
  </properties>
  <canRoam>true</canRoam>
  <disabled>false</disabled>
  <blockBuildWhenDownstreamBuilding>false</blockBuildWhenDownstreamBuilding>
  <blockBuildWhenUpstreamBuilding>false</blockBuildWhenUpstreamBuilding>
  <authToken>01234</authToken>
  <triggers/>
  <concurrentBuild>false</concurrentBuild>
  <builders>
    <hudson.tasks.BatchFile>
      <command>@echo off&#xd;

echo done!</command>
    </hudson.tasks.BatchFile>
  </builders>
  <publishers/>
  <buildWrappers/>
</project>";

        const string XML_WITHOUT_PROPERTIES = @"
<project>
  <actions/>
  <description></description>
  <keepDependencies>false</keepDependencies>
  <properties/>
  <canRoam>true</canRoam>
  <disabled>false</disabled>
  <blockBuildWhenDownstreamBuilding>false</blockBuildWhenDownstreamBuilding>
  <blockBuildWhenUpstreamBuilding>false</blockBuildWhenUpstreamBuilding>
  <authToken>01234</authToken>
  <triggers/>
  <concurrentBuild>false</concurrentBuild>
  <builders>
    <hudson.tasks.BatchFile>
      <command>@echo off&#xd;

echo done!</command>
    </hudson.tasks.BatchFile>
  </builders>
  <publishers/>
  <buildWrappers/>
</project>";

        const string XML_WITHOUT_PARAMETERS_DEFINITIONS = @"
<project>
  <actions/>
  <description></description>
  <keepDependencies>false</keepDependencies>
  <properties>
    <hudson.model.ParametersDefinitionProperty>
      <parameterDefinitions>
      </parameterDefinitions>
    </hudson.model.ParametersDefinitionProperty>
  </properties>
  <canRoam>true</canRoam>
  <disabled>false</disabled>
  <blockBuildWhenDownstreamBuilding>false</blockBuildWhenDownstreamBuilding>
  <blockBuildWhenUpstreamBuilding>false</blockBuildWhenUpstreamBuilding>
  <authToken>01234</authToken>
  <triggers/>
  <concurrentBuild>false</concurrentBuild>
  <builders>
    <hudson.tasks.BatchFile>
      <command>@echo off&#xd;

echo done!</command>
    </hudson.tasks.BatchFile>
  </builders>
  <publishers/>
  <buildWrappers/>
</project>";

        const string XML_WITH_EMPTY_PARAMETERS_DEFINITION_PROPERTY = @"
<project>
  <actions/>
  <description></description>
  <keepDependencies>false</keepDependencies>
  <properties>
    <hudson.model.ParametersDefinitionProperty>
    </hudson.model.ParametersDefinitionProperty>
  </properties>
  <canRoam>true</canRoam>
  <disabled>false</disabled>
  <blockBuildWhenDownstreamBuilding>false</blockBuildWhenDownstreamBuilding>
  <blockBuildWhenUpstreamBuilding>false</blockBuildWhenUpstreamBuilding>
  <authToken>01234</authToken>
  <triggers/>
  <concurrentBuild>false</concurrentBuild>
  <builders>
    <hudson.tasks.BatchFile>
      <command>@echo off&#xd;

echo done!</command>
    </hudson.tasks.BatchFile>
  </builders>
  <publishers/>
  <buildWrappers/>
</project>";

        const string XML_NO_AUTH_TOKEN = @"
<project>
  <actions/>
  <description></description>
  <keepDependencies>false</keepDependencies>
  <properties/>
  <canRoam>true</canRoam>
  <disabled>false</disabled>
  <blockBuildWhenDownstreamBuilding>false</blockBuildWhenDownstreamBuilding>
  <blockBuildWhenUpstreamBuilding>false</blockBuildWhenUpstreamBuilding>
  <triggers/>
  <concurrentBuild>false</concurrentBuild>
  <builders>
    <hudson.tasks.BatchFile>
      <command>@echo off&#xd;

echo done!</command>
    </hudson.tasks.BatchFile>
  </builders>
  <publishers/>
  <buildWrappers/>
</project>";
    }
}
