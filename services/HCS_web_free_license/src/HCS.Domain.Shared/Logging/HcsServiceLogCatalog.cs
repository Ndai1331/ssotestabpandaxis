using System;
using System.Collections.Generic;
using System.Linq;

namespace HCS.Logging;

public static class HcsServiceLogCatalog
{
    public static class Applications
    {
        public const string AuthServer = "HCS.AuthServer";
        public const string WebGateway = "HCS.WebGateway";
        public const string Blazor = "HCS.Blazor";
        public const string PlatformService = "HCS.PlatformService";
        public const string OrganizationService = "HCS.OrganizationService";
        public const string DocumentService = "HCS.DocumentService";
        public const string WorkManagementService = "HCS.WorkManagementService";
        public const string CollaborationService = "HCS.CollaborationService";

        public static readonly IReadOnlyList<string> All =
        [
            AuthServer,
            WebGateway,
            Blazor,
            PlatformService,
            OrganizationService,
            DocumentService,
            WorkManagementService,
            CollaborationService
        ];

        public static bool IsKnown(string? application) =>
            !string.IsNullOrWhiteSpace(application) &&
            All.Contains(application.Trim(), StringComparer.Ordinal);
    }

    public static class Levels
    {
        public const string Error = "Error";
        public const string Warning = "Warning";
        public const string Information = "Information";

        public static readonly IReadOnlyList<string> Allowed =
        [
            Error,
            Warning,
            Information
        ];

        public static readonly IReadOnlyList<string> Default =
        [
            Error,
            Warning
        ];

        public static bool IsAllowed(string? level) =>
            !string.IsNullOrWhiteSpace(level) &&
            Allowed.Contains(level.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
