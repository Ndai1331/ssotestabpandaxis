using HCS.DocumentService.Storage;

namespace HCS.DocumentService.Tests;

public sealed class DocumentBlobCleanupQueueTests
{
    [Fact]
    public async Task Enqueue_skips_blank_names_and_can_be_read()
    {
        var queue = new DocumentBlobCleanupQueue();
        queue.Enqueue(["documents/a", "", "documents/a"], ["signing/b", "  "]);

        Assert.True(queue.Reader.TryRead(out var work));
        Assert.Equal(["documents/a"], work.DocumentBlobNames);
        Assert.Equal(["signing/b"], work.SigningBlobNames);
        Assert.False(queue.Reader.TryRead(out _));
        await Task.CompletedTask;
    }

    [Fact]
    public void Enqueue_ignores_empty_work()
    {
        var queue = new DocumentBlobCleanupQueue();
        queue.Enqueue(["", " "], Array.Empty<string>());
        Assert.False(queue.Reader.TryRead(out _));
    }
}
