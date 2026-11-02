using MathExprEngine.Models;

namespace MathExprEngineTests.Helpers.TestBases
{
    public abstract class DepthGuardedTestBase
    {
        protected virtual string DepthLeakedInMessage => "Thread-static evaluation depth leaked into this test.";

        protected virtual string DepthLeakedOutMessage => "This test leaked thread-static evaluation depth.";

        [TestInitialize]
        public void VerifyDepthNotLeakedIn() => Assert.AreEqual(0, EvaluationContext.CurrentDepth, DepthLeakedInMessage);

        [TestCleanup]
        public void VerifyDepthNotLeakedOut() => Assert.AreEqual(0, EvaluationContext.CurrentDepth, DepthLeakedOutMessage);
    }
}
