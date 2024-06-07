namespace IntegrationTests.Monitoring;

using System.Diagnostics;
using System.Reflection;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true
)]
public sealed class TracePerTestRunAttribute : BaseTraceTestAttribute
{
    private Activity? activityForThisTest;

    public override void Before(MethodInfo methodUnderTest)
    {
        ArgumentNullException.ThrowIfNull(methodUnderTest);

        this.activityForThisTest = WebAppFixture.ActivitySource.StartActivity(
            methodUnderTest.Name,
            ActivityKind.Internal,
            WebAppFixture.ActivityForTestRun.Context
        );

        base.Before(methodUnderTest);
    }

    public override void After(MethodInfo methodUnderTest)
    {
        this.activityForThisTest?.Stop();
        base.After(methodUnderTest);
    }
}
