using System;
using System.Threading.Tasks;

namespace OutsideTheBox
{
    /// <summary>Serializes snapshots and refuses writes until this account's download has been applied.</summary>
    public sealed class CloudProgressWriteQueue
    {
        private readonly Func<string, string, Task> write;
        private Task drain;
        private string pending;
        private Exception failure;
        public string Owner { get; private set; }
        public bool IsReady { get; private set; }

        public CloudProgressWriteQueue(Func<string, string, Task> writer) =>
            write = writer ?? throw new ArgumentNullException(nameof(writer));

        public void Begin(string owner)
        {
            if (drain != null && !drain.IsCompleted)
                throw new InvalidOperationException("Wait for the previous account's cloud save before switching accounts.");
            Owner = owner;
            IsReady = false;
            pending = null;
            failure = null;
        }

        public void MarkReady(string owner)
        {
            if (string.IsNullOrEmpty(owner) || owner != Owner)
                throw new InvalidOperationException("The player changed while cloud progress was loading.");
            IsReady = true;
        }

        public bool Enqueue(string owner, string json)
        {
            if (!IsReady || string.IsNullOrEmpty(owner) || owner != Owner || string.IsNullOrWhiteSpace(json))
                return false;
            pending = json;
            if (drain == null || drain.IsCompleted) drain = Drain(owner);
            return true;
        }

        public async Task Flush(string owner)
        {
            if (!IsReady || owner != Owner)
                throw new InvalidOperationException("Load cloud progress before saving this account.");
            if (pending != null && (drain == null || drain.IsCompleted)) drain = Drain(owner);
            if (drain != null) await drain;
            if (owner != Owner || pending != null || failure != null)
                throw new InvalidOperationException("Progress has not synchronized. Reconnect and retry.", failure);
        }

        private async Task Drain(string owner)
        {
            failure = null;
            while (IsReady && owner == Owner && pending != null)
            {
                var snapshot = pending;
                pending = null;
                try { await write(owner, snapshot); }
                catch (Exception exception)
                {
                    // A failed older request must never replace a more recent queued snapshot.
                    if (owner == Owner && pending == null) pending = snapshot;
                    failure = exception;
                    return;
                }
            }
        }
    }
}
