using System;
using System.Collections.Generic;

namespace HCS.Identity;

public sealed record UserRoleLookupDto(Guid UserId, IReadOnlyList<string> RoleNames);
