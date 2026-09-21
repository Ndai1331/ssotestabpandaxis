using System;
using System.Collections.Generic;
using System.Linq;

namespace HCS.Blazor.Client.Documents;

public sealed record SigningStatusStyle(string Key, string TextKey, string Css, string Color);

public static class SigningStatusDisplay
{
    public static IReadOnlyList<SigningStatusStyle> Styles { get; } =
    [
        new("InProgress", "Work:Status.InProgress", "badge badge-info", "#2563EB"),
        new("Overdue", "Work:Overdue", "badge badge-overdue", "#DB2777"),
        new("Approved", "Work:ApprovalStatus.Approved", "badge hcs-status-badge--success", "#12634f"),
        new("Rejected", "Work:ApprovalStatus.Rejected", "badge hcs-status-badge--danger", "#9b2118"),
        new("Returned", "Work:ApprovalStatus.Returned", "badge badge-warn", "#7C3AED"),
        new("Cancelled", "Work:ApprovalStatus.Cancelled", "badge badge-muted", "var(--color-gray-600, #64748b)"),
        new("Completed", "Work:Status.Completed", "badge hcs-status-badge--success", "#12634f")
    ];

    public static SigningStatusStyle ForKey(string key) => Styles.FirstOrDefault(x => x.Key == key) ?? Styles[0];

    public static SigningStatusStyle ForWorkflow(WorkflowInstanceDto instance) => ForKey(instance.Status switch
    {
        WorkflowInstanceStatus.Completed => "Approved",
        WorkflowInstanceStatus.Rejected => "Rejected",
        WorkflowInstanceStatus.Returned => "Returned",
        WorkflowInstanceStatus.Cancelled => "Cancelled",
        _ => instance.Tasks.Any(task => task.Status == ApprovalTaskStatus.Pending
            && task.DueAt is { } due && due < DateTime.UtcNow) ? "Overdue" : "InProgress"
    });
}
