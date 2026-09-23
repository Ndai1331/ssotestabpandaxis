using HCS;
using Xunit;

namespace HCS.EntityFrameworkCore.Applications;

[Collection(HCSTestConsts.CollectionDefinitionName)]
public class EfCoreIdentityUserListTests : IdentityUserListTests<HCSEntityFrameworkCoreTestModule>
{
}
