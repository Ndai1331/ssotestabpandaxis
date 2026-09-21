using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HCS.CollaborationService.Contracts;
using Volo.Abp.Users;

namespace HCS.Blazor.Client.Collaboration;

/// <summary>
/// Resolves stored user ids to display names. Chat contact search excludes the
/// current user and only returns a page of people, so known members/assignees
/// must be loaded by id.
/// </summary>
internal static class ChatContactDirectory
{
    public static string Name(IEnumerable<ChatContactDto> contacts, Guid? userId)
    {
        if (userId is not { } id || id == Guid.Empty)
            return string.Empty;

        var contact = contacts.FirstOrDefault(item => item.Id == id);
        return UserDisplayNames.FromPerson(contact?.Surname, contact?.Name, contact?.UserName, contact?.DisplayName);
    }

    public static async Task EnsureAsync(
        CollaborationClient chat,
        List<ChatContactDto> contacts,
        IEnumerable<Guid> userIds,
        ICurrentUser? currentUser = null,
        CancellationToken cancellationToken = default)
    {
        var missing = userIds
            .Where(id => id != Guid.Empty && contacts.All(item => item.Id != id))
            .Distinct()
            .ToArray();
        if (missing.Length > 0)
        {
            try
            {
                foreach (var contact in await chat.GetContactsByIdsAsync(missing, cancellationToken))
                {
                    if (contacts.All(item => item.Id != contact.Id))
                        contacts.Add(contact);
                }
            }
            catch
            {
                // Picker search may still work; known ids fall back to current-user claims.
            }
        }

        if (currentUser is not null)
            EnsureCurrentUser(contacts, currentUser);
    }

    public static void EnsureCurrentUser(List<ChatContactDto> contacts, ICurrentUser currentUser)
    {
        if (currentUser.Id is not { } id || id == Guid.Empty)
            return;
        if (contacts.Any(item => item.Id == id))
            return;

        var display = UserDisplayNames.FromPerson(currentUser.SurName, currentUser.Name, currentUser.UserName);
        if (string.IsNullOrWhiteSpace(display))
            return;

        contacts.Add(new ChatContactDto(
            id,
            currentUser.UserName ?? display,
            display,
            true,
            currentUser.SurName,
            currentUser.Name));
    }

    public static IReadOnlyList<ChatContactDto> WithCurrentUser(
        IReadOnlyList<ChatContactDto> found,
        List<ChatContactDto> cache,
        ICurrentUser currentUser,
        string? term)
    {
        EnsureCurrentUser(cache, currentUser);
        if (currentUser.Id is not { } id)
            return found;

        var self = cache.FirstOrDefault(item => item.Id == id);
        if (self is null || !ChatContactSearch.Matches(self, term) || found.Any(item => item.Id == id))
            return found;

        var merged = new List<ChatContactDto>(found.Count + 1) { self };
        merged.AddRange(found);
        return merged;
    }
}
