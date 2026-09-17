using System;
using System.Threading.Tasks;
using HCS.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Volo.Abp;
using Volo.Abp.SettingManagement;

namespace HCS.Settings;

[RemoteService(IsEnabled = false)]
[Authorize(HCSPermissions.SystemBranding.Update)]
public class LegacySigningReportSettingsAppService(
    ISettingManager settingManager) : HCSAppService, ILegacySigningReportSettingsAppService
{
    public async Task<LegacySigningReportSettingsDto> GetAsync()
    {
        var enabled = await settingManager.GetOrNullGlobalAsync(HCSSettings.LegacySigningReportEnabled);
        var value = await settingManager.GetOrNullGlobalAsync(HCSSettings.LegacySigningReportSqlServerConnectionString);
        return new LegacySigningReportSettingsDto
        {
            Enabled = HCSSettings.IsEnabledOrDefault(enabled),
            HasConnectionString = !string.IsNullOrWhiteSpace(value),
            ConnectionString = value
        };
    }

    public async Task UpdateAsync(UpdateLegacySigningReportSettingsDto input)
    {
        await settingManager.SetGlobalAsync(
            HCSSettings.LegacySigningReportEnabled,
            input.Enabled ? "true" : "false");

        if (input.Enabled)
        {
            Check.NotNullOrWhiteSpace(input.ConnectionString, nameof(input.ConnectionString));
        }

        if (!string.IsNullOrWhiteSpace(input.ConnectionString))
        {
            await settingManager.SetGlobalAsync(
                HCSSettings.LegacySigningReportSqlServerConnectionString,
                input.ConnectionString.Trim());
        }
    }

    public async Task<LegacySigningReportConnectionTestResultDto> TestAsync(UpdateLegacySigningReportSettingsDto input)
    {
        Check.NotNullOrWhiteSpace(input.ConnectionString, nameof(input.ConnectionString));
        var (success, message) = await TestConnectionAsync(input.ConnectionString.Trim());
        return new LegacySigningReportConnectionTestResultDto { Success = success, Message = message };
    }

    internal static async Task<(bool Success, string Message)> TestConnectionAsync(string connectionString)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 30;
            await command.ExecuteScalarAsync();
            return (true, "Connected successfully.");
        }
        catch (Exception exception)
        {
            var message = exception.Message;
            if (message.Contains("certificate", StringComparison.OrdinalIgnoreCase)
                || message.Contains("SSL", StringComparison.OrdinalIgnoreCase)
                || message.Contains("encrypt", StringComparison.OrdinalIgnoreCase))
            {
                message += " Try adding Encrypt=False;TrustServerCertificate=True;";
            }

            return (false, message);
        }
    }
}
