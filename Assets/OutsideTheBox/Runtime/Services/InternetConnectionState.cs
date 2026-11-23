namespace OutsideTheBox
{
    /// <summary>Rejects captive portals and tolerates a single failed background probe.</summary>
    public sealed class InternetConnectionState
    {
        public bool HasResult { get; private set; }
        public bool IsOnline { get; private set; }
        private int failedProbes;

        public void RecordProbe(bool success, bool hasNetworkPath, int failureThreshold)
        {
            if (success && hasNetworkPath)
            {
                HasResult = IsOnline = true;
                failedProbes = 0;
                return;
            }
            failedProbes++;
            if (!hasNetworkPath || !HasResult || failedProbes >= System.Math.Max(1, failureThreshold))
            {
                HasResult = true;
                IsOnline = false;
            }
        }

        public static bool IsSuccessfulProbe(long responseCode, bool transportSucceeded) =>
            transportSucceeded && responseCode == 204;
    }
}
