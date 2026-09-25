using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCS.Blazor.Client.Documents;
using HCS.Blazor.Client.Services;
using HCS.Blazor.Client.Work;
using HCS.CollaborationService.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace HCS.Blazor.Client.Pages;

public partial class ChatWorkspace
{
    private const string MessagesPaneId = "hcs-chat-messages";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (scrollToBottomAfterRender)
        {
            scrollToBottomAfterRender = false;
            await ScrollMessagesAsync(toBottom: true);
            allowOlderScroll = true;
        }

        if (jumpToMessageIdAfterRender is { } messageId)
        {
            jumpToMessageIdAfterRender = null;
            highlightedMessageId = messageId;
            await TryChatScriptAsync(() => Js.InvokeVoidAsync("hcsChat.scrollToMessage", MessageElementId(messageId)));
            allowOlderScroll = true;
            StateHasChanged();
        }

        if (focusRenameAfterRender)
        {
            focusRenameAfterRender = false;
            try
            {
                await renameInputRef.FocusAsync();
            }
            catch (Exception)
            {
            }
        }

        if (messageMenuId is { } menuId)
        {
            await TryChatScriptAsync(() => Js.InvokeVoidAsync(
                "hcsChat.positionMenu",
                "chat-msg-menu",
                $"[data-msg-more='{menuId:D}']"));
        }
    }

    private async Task LoadOlderMessagesAsync()
    {
        if (selected is null || isLoadingMessages || messages.Count == 0 || messages.Count >= totalMessageCount)
        {
            return;
        }

        var previousHeight = await TryChatScriptAsync(() => Js.InvokeAsync<double>("hcsChat.scrollHeight", MessagesPaneId));
        var oldest = messages[0];
        isLoadingMessages = true;
        messageError = null;
        try
        {
            var context = await Client.GetMessageContextAsync(selected.Id, oldest.Id, before: 50, after: 0);
            foreach (var item in context.Before)
            {
                if (messages.All(message => message.Id != item.Id))
                {
                    messages.Insert(0, item);
                }
            }

            if (!context.HasMoreBefore)
            {
                totalMessageCount = messages.Count;
            }

            messageSkip = messages.Count;
            if (previousHeight is { } height)
            {
                await TryChatScriptAsync(() => Js.InvokeVoidAsync("hcsChat.preserveScrollAfterPrepend", MessagesPaneId, height));
            }

            await ResolveUserNamesAsync(context.Before.SelectMany(MessageUserIds));
        }
        catch (Exception exception)
        {
            messageError = MapChatError(exception, "Chat:MessagesError");
        }
        finally
        {
            isLoadingMessages = false;
        }
    }

    private async Task OnMessagesScrollAsync()
    {
        if (messageMenuId is not null)
        {
            CloseMessageMenu();
        }

        if (!allowOlderScroll || selected is null || isLoadingMessages || messages.Count >= totalMessageCount)
        {
            return;
        }

        if (await TryChatScriptAsync(() => Js.InvokeAsync<bool>("hcsChat.isNearTop", MessagesPaneId)) == true)
        {
            await LoadOlderMessagesAsync();
        }
    }

    private void StartReply(ChatMessageDto message)
    {
        replyingTo = message;
        forwardingMessage = null;
    }

    private void CancelReply() => replyingTo = null;

    private void CloseMessageMenu() => messageMenuId = null;

    private void ToggleMessageMenu(Guid messageId) =>
        messageMenuId = messageMenuId == messageId ? null : messageId;

    private void ReplyFromMenu(ChatMessageDto message)
    {
        CloseMessageMenu();
        StartReply(message);
    }

    private void ForwardFromMenu(ChatMessageDto message)
    {
        CloseMessageMenu();
        OpenForward(message);
    }

    private Task OpenAssignTaskFromMenuAsync(ChatMessageDto message)
    {
        CloseMessageMenu();
        return OpenAssignTaskAsync(message);
    }

    private bool NeedsTaskProjectPicker => selected?.ProjectId is null;

    private Guid? PickedTaskProjectId =>
        selected?.ProjectId
        ?? (Guid.TryParse(taskProjectKey, out var projectId) && projectId != Guid.Empty ? projectId : null);

    private Task OnTaskProjectChangedAsync() => LoadTaskAssigneesAsync();

    private async Task LoadTaskAssigneesAsync()
    {
        var version = ++taskAssigneeLoadVersion;
        var previous = taskAssigneeKey;
        taskAssigneeIds.Clear();
        taskAssigneeKey = string.Empty;
        if (PickedTaskProjectId is not { } projectId)
        {
            return;
        }

        isLoadingTaskAssignees = true;
        try
        {
            var detail = await Work.GetProjectAsync(projectId);
            if (version != taskAssigneeLoadVersion)
            {
                return;
            }

            var ids = detail.Members
                .Where(member => member.IsActive)
                .Select(member => member.UserId)
                .Append(detail.Project.OwnerUserId)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();
            await ResolveUserNamesAsync(ids);
            if (version != taskAssigneeLoadVersion)
            {
                return;
            }

            taskAssigneeIds.AddRange(ids
                .OrderBy(id => id == currentUserId)
                .ThenBy(MemberName, StringComparer.CurrentCultureIgnoreCase));

            if (!string.IsNullOrEmpty(previous) && taskAssigneeIds.Any(id => id.ToString("D") == previous))
            {
                taskAssigneeKey = previous;
            }
            else if (selected is not null
                     && DirectConversationUserId(selected) is { } counterpart
                     && taskAssigneeIds.Contains(counterpart))
            {
                taskAssigneeKey = counterpart.ToString("D");
            }
            else
            {
                var others = taskAssigneeIds.Where(id => id != currentUserId).ToList();
                if (others.Count == 1)
                {
                    taskAssigneeKey = others[0].ToString("D");
                }
            }
        }
        catch (Exception exception)
        {
            assignTaskError = BffErrorMapper.From(Localizer, exception, BffErrorKind.Load);
        }
        finally
        {
            if (version == taskAssigneeLoadVersion)
            {
                isLoadingTaskAssignees = false;
            }
        }
    }

    private async Task OpenAssignTaskAsync(ChatMessageDto? source)
    {
        if (selected is null || permissions?.CanSend != true)
        {
            return;
        }

        assignTaskSource = source;
        assignTaskOpen = true;
        assignTaskError = null;
        isAssigningTask = false;
        isLoadingTaskAssignees = false;
        taskAssigneeIds.Clear();
        taskTitle = TruncateTaskTitle(source is null ? string.Empty : ChatTaskCardMessage.Preview(source.Text));
        if (string.IsNullOrWhiteSpace(taskTitle) && source is { Attachments.Count: > 0 })
        {
            taskTitle = TruncateTaskTitle(TitleFromFileName(source.Attachments[0].FileName));
        }
        taskNote = string.Empty;
        taskDue = DateTime.Today.AddDays(7);
        taskAssigneeKey = string.Empty;
        taskProjectKey = selected.ProjectId?.ToString("D") ?? string.Empty;
        taskProjects.Clear();
        try
        {
            var page = await Work.GetProjectsAsync(new WorkListQuery(null, null, 0, 100));
            taskProjects.AddRange(page.Items);
            if (string.IsNullOrEmpty(taskProjectKey) && taskProjects.Count == 1)
            {
                taskProjectKey = taskProjects[0].Id.ToString("D");
            }
        }
        catch (Exception exception)
        {
            assignTaskError = BffErrorMapper.From(Localizer, exception, BffErrorKind.Load);
        }

        await LoadTaskAssigneesAsync();
    }

    private void CloseAssignTask()
    {
        assignTaskOpen = false;
        isAssigningTask = false;
        isLoadingTaskAssignees = false;
        assignTaskError = null;
        assignTaskSource = null;
        taskTitle = string.Empty;
        taskNote = string.Empty;
        taskAssigneeKey = string.Empty;
        taskProjectKey = string.Empty;
        taskProjects.Clear();
        taskAssigneeIds.Clear();
    }

    private async Task OpenTaskViewAsync(Guid taskId)
    {
        if (taskId == Guid.Empty)
        {
            return;
        }

        try
        {
            if (taskViewModal is not null)
            {
                await taskViewModal.ShowAsync(taskId);
            }
        }
        catch (Exception exception)
        {
            await UiMessageService.Error(BffErrorMapper.From(Localizer, exception, BffErrorKind.Load));
        }
    }

    private async Task SubmitAssignTaskAsync()
    {
        if (selected is null || isAssigningTask)
        {
            return;
        }

        var title = taskTitle.Trim();
        if (string.IsNullOrWhiteSpace(title)
            || !Guid.TryParse(taskAssigneeKey, out var assigneeId)
            || assigneeId == Guid.Empty)
        {
            assignTaskError = T("Chat:TaskNeed");
            return;
        }

        var projectId = selected.ProjectId
            ?? (Guid.TryParse(taskProjectKey, out var pickedProject) ? pickedProject : null);
        if (projectId is not { } project)
        {
            assignTaskError = T("Chat:TaskNeedProject");
            return;
        }

        isAssigningTask = true;
        assignTaskError = null;
        try
        {
            var start = DateTime.Today;
            var due = taskDue.Date < start ? start : taskDue.Date;
            var created = await Work.CreateTaskAsync(new CreateProjectTaskRequest(
                project,
                selected.Type == ConversationType.Task ? selected.TaskId : null,
                null,
                title,
                string.IsNullOrWhiteSpace(taskNote) ? null : taskNote.Trim(),
                start,
                due,
                "Normal",
                "New",
                0));
            try
            {
                await Work.AddAssignmentAsync(created.Id, new AddTaskAssignmentRequest(assigneeId, "Member"));
            }
            catch (Exception assignmentException)
            {
                assignTaskError = BffErrorMapper.From(Localizer, assignmentException, BffErrorKind.Save);
            }

            var fileFailures = await AttachMessageFilesToTaskAsync(created.Id, assignTaskSource);

            var sent = await Client.SendMessageAsync(new SendMessageInput
            {
                ConversationId = selected.Id,
                Text = ChatTaskCardMessage.Format(new ChatTaskCardMessage.Payload(
                    created.Id,
                    title,
                    assigneeId,
                    MemberName(assigneeId),
                    DateOnly.FromDateTime(due),
                    string.IsNullOrWhiteSpace(taskNote) ? null : taskNote.Trim())),
                ClientMessageId = Guid.NewGuid(),
                ReplyToMessageId = assignTaskSource?.Id
            });
            UpsertMessage(sent);
            messageSkip = messages.Count;
            totalMessageCount = Math.Max(totalMessageCount, messages.Count);
            scrollToBottomAfterRender = true;
            await Client.MarkReadAsync(selected.Id);
            await PatchConversationFromMessageAsync(sent);
            var assignmentWarning = assignTaskError;
            CloseAssignTask();
            if (!string.IsNullOrWhiteSpace(assignmentWarning))
            {
                await UiMessageService.Warn(assignmentWarning);
            }

            if (fileFailures > 0)
            {
                await UiMessageService.Warn(T("Chat:TaskAttachFromMessageFailed"));
            }

            await UiMessageService.Success(T("Chat:TaskCreated"));
        }
        catch (Exception exception)
        {
            assignTaskError = BffErrorMapper.From(Localizer, exception, BffErrorKind.Save);
            if (string.Equals(assignTaskError, Localizer["Catalog:ValidationError"].Value, StringComparison.Ordinal)
                || string.Equals(assignTaskError, Localizer["Catalog:SaveError"].Value, StringComparison.Ordinal))
            {
                assignTaskError = T("Chat:TaskCreateError");
            }
        }
        finally
        {
            isAssigningTask = false;
        }
    }

    private static string TruncateTaskTitle(string? text)
    {
        var value = (text ?? string.Empty).Trim();
        if (value.Length == 0 || value == "…")
        {
            return string.Empty;
        }

        return value.Length <= 256 ? value : value[..256];
    }

    private async Task<int> AttachMessageFilesToTaskAsync(Guid taskId, ChatMessageDto? source)
    {
        if (source is null || source.IsDeleted || source.Attachments.Count == 0)
        {
            return 0;
        }

        var failed = 0;
        foreach (var attachment in source.Attachments)
        {
            try
            {
                if (attachment.Id == Guid.Empty || attachment.Size > DocumentClient.MaxUploadBytes)
                {
                    failed++;
                    continue;
                }

                var downloaded = await Client.DownloadAttachmentAsync(attachment.Id);
                var fileName = string.IsNullOrWhiteSpace(attachment.FileName) ? downloaded.FileName : attachment.FileName;
                var contentType = string.IsNullOrWhiteSpace(attachment.ContentType)
                    ? downloaded.ContentType
                    : attachment.ContentType;
                var created = await Documents.CreateDocumentAsync(new CreateDocumentRequest(
                    null,
                    TitleFromFileName(fileName),
                    null,
                    null,
                    null,
                    null,
                    null,
                    DocumentSourceType.Personal));
                try
                {
                    await Documents.UploadFileAsync(created.Id, downloaded.Bytes, fileName, contentType);
                }
                catch
                {
                    try { await Documents.DeleteDocumentAsync(created.Id); }
                    catch { }
                    throw;
                }

                await Work.AddTaskDocumentAsync(taskId, new AddTaskDocumentRequest(created.Id, created.Number));
            }
            catch (Exception exception)
            {
                LogChatError(exception, "Chat:TaskAttachFromMessageFailed");
                failed++;
            }
        }

        return failed;
    }

    private static string TitleFromFileName(string? fileName)
    {
        var name = (fileName ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            return "file";
        }

        var title = Path.GetFileNameWithoutExtension(name);
        return string.IsNullOrWhiteSpace(title) ? name : title;
    }

    private async Task DeleteFromMenuAsync(ChatMessageDto message)
    {
        CloseMessageMenu();
        await DeleteMessageAsync(message);
    }

    private void TogglePinnedPanel() => pinnedPanelOpen = !pinnedPanelOpen;

    private async Task OpenPinnedMessageAsync(ChatMessageDto message)
    {
        pinnedPanelOpen = false;
        await JumpToMessageAsync(message.Id);
    }

    private async Task LoadPinnedMessagesAsync()
    {
        if (selected is null)
        {
            return;
        }

        try
        {
            var response = await Client.GetPinnedMessagesAsync(selected.Id);
            pinnedMessages.Clear();
            pinnedMessages.AddRange(response.Items.OrderByDescending(item => item.CreatedAt));
            await ResolveUserNamesAsync(pinnedMessages.SelectMany(MessageUserIds));
            if (pinnedMessages.Count == 0)
            {
                pinnedPanelOpen = false;
            }
        }
        catch (Exception)
        {
            pinnedMessages.Clear();
        }
    }

    private async Task ToggleMessagePinnedAsync(ChatMessageDto message)
    {
        if (isPinningMessage)
        {
            return;
        }

        isPinningMessage = true;
        CloseMessageMenu();
        try
        {
            var pinned = !message.IsPinned;
            await Client.SetMessagePinnedAsync(message.Id, pinned);
            ApplyMessagePinned(message.Id, pinned);
            await LoadPinnedMessagesAsync();
        }
        catch (Exception exception)
        {
            messageError = MapChatError(exception, "Chat:PinError");
        }
        finally
        {
            isPinningMessage = false;
        }
    }

    private void ApplyMessagePinned(Guid messageId, bool pinned)
    {
        var index = messages.FindIndex(item => item.Id == messageId);
        if (index >= 0)
        {
            messages[index] = messages[index] with { IsPinned = pinned };
        }
    }

    private void OpenForward(ChatMessageDto message)
    {
        forwardingMessage = message;
        forwardComment = string.Empty;
        forwardSearch = string.Empty;
        forwardTargetId = null;
    }

    private void CloseForward()
    {
        forwardingMessage = null;
        forwardComment = string.Empty;
        forwardTargetId = null;
    }

    private async Task ForwardAsync()
    {
        if (forwardingMessage is null || forwardTargetId is not { } targetId || isForwarding)
        {
            return;
        }

        isForwarding = true;
        messageError = null;
        try
        {
            var sent = await Client.ForwardMessageAsync(forwardingMessage.Id, targetId, forwardComment);
            if (selected?.Id == targetId)
            {
                UpsertMessage(sent);
                scrollToBottomAfterRender = true;
            }

            CloseForward();
            await PatchConversationFromMessageAsync(sent);
        }
        catch (Exception exception)
        {
            messageError = MapChatError(exception, "Chat:ForwardError");
        }
        finally
        {
            isForwarding = false;
        }
    }

    private async Task DeleteMessageAsync(ChatMessageDto message)
    {
        if (!CanDeleteMessage(message) || isDeletingMessage || !await UiMessageService.Confirm(T("Chat:DeleteConfirm")))
        {
            return;
        }

        isDeletingMessage = true;
        try
        {
            await Client.DeleteMessageAsync(message.Id);
            MarkDeleted(message.Id);
            PatchConversationAfterDelete(message.ConversationId);
        }
        catch (Exception exception)
        {
            messageError = MapChatError(exception, "Chat:DeleteError");
        }
        finally
        {
            isDeletingMessage = false;
        }
    }

    private async Task JumpToMessageAsync(Guid messageId)
    {
        if (selected is null || messageId == Guid.Empty)
        {
            return;
        }

        if (messages.Any(item => item.Id == messageId))
        {
            jumpToMessageIdAfterRender = messageId;
            return;
        }

        try
        {
            var context = await Client.GetMessageContextAsync(selected.Id, messageId);
            var merged = messages
                .Concat(context.Before)
                .Concat([context.Target])
                .Concat(context.After)
                .GroupBy(item => item.Id)
                .Select(group => group.First())
                .OrderBy(item => item.CreatedAt)
                .ThenBy(item => item.Id)
                .ToList();
            messages.Clear();
            messages.AddRange(merged);
            messageSkip = messages.Count;
            totalMessageCount = Math.Max(totalMessageCount, messages.Count);
            await ResolveUserNamesAsync(merged.SelectMany(MessageUserIds));
            jumpToMessageIdAfterRender = messageId;
        }
        catch (Exception exception)
        {
            messageError = MapChatError(exception, "Chat:MessagesError");
        }
    }

    private async Task ConfirmRemoveMemberAsync()
    {
        if (selected is null || memberPendingRemoval is null || isRemovingMember)
        {
            return;
        }

        isRemovingMember = true;
        try
        {
            await Client.RemoveMemberAsync(selected.Id, memberPendingRemoval.UserId);
            memberPendingRemoval = null;
            selected = await Client.GetConversationAsync(selected.Id);
            permissions = await Client.GetPermissionsAsync(selected.Id);
            await ResolveUserNamesAsync(selected.Members.Select(member => member.UserId));
            PatchConversation(selected);
        }
        catch (Exception exception)
        {
            messageError = MapChatError(exception, "Chat:RemoveMemberError");
        }
        finally
        {
            isRemovingMember = false;
        }
    }

    private async Task HandleMessageReceivedAsync(ChatMessageDto message)
    {
        await InvokeAsync(async () =>
        {
            if (selected?.Id == message.ConversationId)
            {
                UpsertMessage(message);
                scrollToBottomAfterRender = true;
            }

            await ResolveUserNamesAsync(MessageUserIds(message));
            await PatchConversationFromMessageAsync(message);
            StateHasChanged();
        });
    }

    private async Task HandleMessageDeletedAsync(Guid conversationId, Guid messageId)
    {
        await InvokeAsync(() =>
        {
            if (selected?.Id == conversationId)
            {
                MarkDeleted(messageId);
            }

            PatchConversationAfterDelete(conversationId);
            StateHasChanged();
            return Task.CompletedTask;
        });
    }

    private void PatchConversationAfterDelete(Guid conversationId)
    {
        var existing = conversations.FirstOrDefault(item => item.Id == conversationId);
        if (existing is null)
            return;

        if (selected?.Id != conversationId)
            return;

        var last = messages.LastOrDefault(item => !item.IsDeleted);
        PatchConversation(existing with
        {
            LastMessage = last is null ? null : LastMessagePreview(last),
            LastMessageAt = last?.CreatedAt ?? existing.LastMessageAt
        });
    }

    private bool CanDeleteMessage(ChatMessageDto message) =>
        !message.IsDeleted &&
        currentUserId is { } userId &&
        ChatModerationRules.CanDeleteMessage(
            userId,
            message.SenderUserId,
            permissions?.CanModerateMessages == true,
            selected?.Members.FirstOrDefault(member => member.UserId == userId)?.Role ?? ConversationMemberRole.Member);

    private bool CanRemoveMember(ConversationMemberDto member) =>
        selected is not null &&
        permissions?.CanManageMembers == true &&
        selected.Type != ConversationType.User &&
        member.UserId != currentUserId;

    private void UpsertMessage(ChatMessageDto message)
    {
        var index = messages.FindIndex(item => item.Id == message.Id);
        if (index >= 0)
        {
            messages[index] = message;
            return;
        }

        messages.Add(message);
        messageSkip = messages.Count;
        totalMessageCount = Math.Max(totalMessageCount, messages.Count);
    }

    private void MarkDeleted(Guid messageId)
    {
        var index = messages.FindIndex(item => item.Id == messageId);
        if (index < 0)
        {
            return;
        }

        var current = messages[index];
        messages[index] = current with { IsDeleted = true, Text = string.Empty, Attachments = [], IsPinned = false };
        pinnedMessages.RemoveAll(item => item.Id == messageId);
        if (pinnedMessages.Count == 0)
        {
            pinnedPanelOpen = false;
        }
        if (replyingTo?.Id == messageId)
        {
            replyingTo = null;
        }
    }

    private async Task ScrollMessagesAsync(bool toBottom)
    {
        if (toBottom)
        {
            await TryChatScriptAsync(() => Js.InvokeVoidAsync("hcsChat.scrollToBottom", MessagesPaneId));
        }
    }

    private static async Task TryChatScriptAsync(Func<ValueTask> action)
    {
        try
        {
            await action();
        }
        catch (JSDisconnectedException)
        {
        }
        catch (JSException)
        {
        }
    }

    private static async Task<T?> TryChatScriptAsync<T>(Func<ValueTask<T>> action)
    {
        try
        {
            return await action();
        }
        catch (JSDisconnectedException)
        {
            return default;
        }
        catch (JSException)
        {
            return default;
        }
    }

    private static string MessageElementId(Guid id) => $"hcs-chat-msg-{id:N}";

    private static string PreviewText(string? text, string? fallback = null)
    {
        var value = string.IsNullOrWhiteSpace(text) ? fallback : ChatTaskCardMessage.Preview(text);
        if (string.IsNullOrWhiteSpace(value) || value == ChatModerationRules.ForwardedPlaceholder)
        {
            return "…";
        }

        return value.Length <= 140 ? value : $"{value[..140]}…";
    }
}
