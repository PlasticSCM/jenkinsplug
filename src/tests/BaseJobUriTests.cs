using NUnit.Framework;

namespace JenkinsPlug.Tests
{
    [TestFixture]
    class BaseJobUriTests
    {
        [Test]
        public void TestUriForPlanName()
        {
            string planName = "plan name with spaces";

            string expectedUri = "job/plan name with spaces";

            Assert.AreEqual(expectedUri, BaseJobUri.Get(planName));
        }

        [Test]
        public void TestUriForPlanPath1Level()
        {
            string planName = "folder with spaces/plan with spaces";

            string expectedUri = "job/folder with spaces/job/plan with spaces";

            Assert.AreEqual(expectedUri, BaseJobUri.Get(planName));
        }

        [Test]
        public void TestUriForPlanPath2Levels()
        {
            string planName = "folder with spaces/subfolder/plan with spaces";

            string expectedUri = "job/folder with spaces/job/subfolder/job/plan with spaces";

            Assert.AreEqual(expectedUri, BaseJobUri.Get(planName));
        }
    }
}
