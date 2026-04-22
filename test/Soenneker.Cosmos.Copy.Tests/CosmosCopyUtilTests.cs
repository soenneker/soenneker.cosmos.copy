using Soenneker.Cosmos.Copy.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.Cosmos.Copy.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class CosmosCopyUtilTests : HostedUnitTest
{
    private readonly ICosmosCopyUtil _util;

    public CosmosCopyUtilTests(Host host) : base(host)
    {
        _util = Resolve<ICosmosCopyUtil>(true);
    }

    [Test]
    public void Default()
    {

    }
}
