using HCS;
using Xunit;

namespace HCS.EntityFrameworkCore.Applications;

[Collection(HCSTestConsts.CollectionDefinitionName)]
public class EfCoreOrganizationUnitLookupAppServiceTests
    : OrganizationUnitLookupAppServiceTests<HCSEntityFrameworkCoreTestModule>
{
}
