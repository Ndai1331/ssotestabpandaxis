using System;
using System.Collections.Generic;
using System.Linq;
using HCS.Blazor.Client.Components;
using HCS.CollaborationService.Contracts;

namespace HCS.Blazor.Client.Work;

internal static class ProjectParticipantPicker
{
    public static HashSet<Guid> Ids(ProjectDetailDto detail) =>
        detail.Members
            .Where(member => member.IsActive)
            .Select(member => member.UserId)
            .Append(detail.Project.OwnerUserId)
            .Where(id => id != Guid.Empty)
            .ToHashSet();

    public static CatalogSelect2SearchResponse Filter(
        IEnumerable<ChatContactDto> contacts,
        IReadOnlySet<Guid> participantIds,
        string? term)
    {
        var found = contacts
            .Where(contact => participantIds.Contains(contact.Id) && ChatContactSearch.Matches(contact, term))
            .GroupBy(contact => contact.Id)
            .Select(group => group.First())
            .ToList();
        return new CatalogSelect2SearchResponse(found.Select(CatalogSelect2Cache.UserItem).ToList(), false);
    }
}
