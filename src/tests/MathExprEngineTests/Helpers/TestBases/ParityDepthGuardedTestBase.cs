namespace MathExprEngineTests.Helpers.TestBases
{
    public abstract class ParityDepthGuardedTestBase : DepthGuardedTestBase
    {
        protected override string DepthLeakedInMessage => "Depth leaked into this test (V-14).";

        protected override string DepthLeakedOutMessage => "Depth leaked out of this test (V-14).";
    }
}
