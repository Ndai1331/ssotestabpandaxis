using System;
using HCS.CollaborationService.Contracts;
using Microsoft.Extensions.Localization;

namespace HCS.Blazor.Client.Collaboration;

public static class NotificationText
{
    public static string Localize(IStringLocalizer localizer, string stored) =>
        NotificationLocalization.Format(
            stored,
            key => localizer[key],
            (key, args) => localizer[key, args]);

    public static string ResolveLink(NotificationDto item)
    {
        var link = item.Link ?? "";
        if (TryInboxDocumentPreview(item, link, out var preview))
            return preview;
        if (link.Contains("workflow", StringComparison.OrdinalIgnoreCase)
            || link.Contains("signing", StringComparison.OrdinalIgnoreCase)
            || item.Title.Contains("WORKFLOW", StringComparison.OrdinalIgnoreCase)
            || item.Body.Contains("WORKFLOW", StringComparison.OrdinalIgnoreCase))
            return "/document-signing";
        if (link.StartsWith('/')) return link;
        return "/workspace";
    }

    private static bool TryInboxDocumentPreview(NotificationDto item, string link, out string preview)
    {
        preview = "";
        if (!IsDocumentSentNotification(item, link))
            return false;
        if (TryDocumentIdFromInboxLink(link, out var documentId))
        {
            preview = $"/manage-documents?sourceType=2&preview={documentId:D}";
            return true;
        }
        return false;
    }

    private static bool IsDocumentSentNotification(NotificationDto item, string link) =>
        string.Equals(item.Title, NotificationLocalization.DocumentSentTitle, StringComparison.OrdinalIgnoreCase)
        || link.Contains("sourceType=2", StringComparison.OrdinalIgnoreCase)
        || link.Contains("preview=", StringComparison.OrdinalIgnoreCase);

    private static bool TryDocumentIdFromInboxLink(string link, out Guid documentId)
    {
        documentId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(link)) return false;
        const string previewKey = "preview=";
        var previewIndex = link.IndexOf(previewKey, StringComparison.OrdinalIgnoreCase);
        if (previewIndex >= 0)
        {
            var value = link[(previewIndex + previewKey.Length)..];
            var end = value.IndexOfAny(['&', '#']);
            if (end >= 0) value = value[..end];
            return Guid.TryParse(value, out documentId);
        }

        const string detailPrefix = "/document-detail/";
        var detailIndex = link.IndexOf(detailPrefix, StringComparison.OrdinalIgnoreCase);
        if (detailIndex < 0) return false;
        var idPart = link[(detailIndex + detailPrefix.Length)..];
        var query = idPart.IndexOfAny(['?', '#']);
        if (query >= 0) idPart = idPart[..query];
        return Guid.TryParse(idPart, out documentId);
    }

    public static string ResolveKind(NotificationDto item)
    {
        var link = item.Link ?? "";
        var haystack = $"{item.Title} {item.Body} {link}";
        if (ChatNotificationRules.IsChatLink(link) || haystack.Contains("chat", StringComparison.OrdinalIgnoreCase))
            return "chat";
        if (link.Contains("signing", StringComparison.OrdinalIgnoreCase)
            || haystack.Contains("WORKFLOW", StringComparison.OrdinalIgnoreCase)
            || haystack.Contains("ký", StringComparison.OrdinalIgnoreCase)
            || haystack.Contains("sign", StringComparison.OrdinalIgnoreCase))
            return "sign";
        if (link.Contains("event", StringComparison.OrdinalIgnoreCase)
            || haystack.Contains("event", StringComparison.OrdinalIgnoreCase)
            || haystack.Contains("sự kiện", StringComparison.OrdinalIgnoreCase))
            return "event";
        if (link.Contains("social", StringComparison.OrdinalIgnoreCase)
            || haystack.Contains("social", StringComparison.OrdinalIgnoreCase))
            return "social";
        return "doc";
    }

    public static string FormatLocal(DateTime value, string format)
    {
        if (value.Year < 2000) return "—";
        var local = value.Kind == DateTimeKind.Utc ? value.ToLocalTime()
            : value.Kind == DateTimeKind.Local ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime();
        return local.ToString(format);
    }
}
