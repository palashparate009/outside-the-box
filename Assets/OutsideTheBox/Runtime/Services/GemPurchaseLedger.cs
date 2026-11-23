using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace OutsideTheBox
{
    /// <summary>Stable pack quantities and duplicate-safe wallet credits, without store or Unity dependencies.</summary>
    public static class GemPurchaseLedger
    {
        private const string Prefix = "otb_gems_";

        public static bool TryGetPackAmount(string productId, out int amount)
        {
            amount = 0;
            return productId != null && productId.StartsWith(Prefix, StringComparison.Ordinal) &&
                   int.TryParse(productId.Substring(Prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out amount) &&
                   amount > 0 && amount <= 1000000 && productId == Prefix + amount.ToString(CultureInfo.InvariantCulture);
        }

        public static string Key(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A purchase/account identifier is required.");
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", string.Empty).ToLowerInvariant();
        }

        public static bool Grant(ICollection<string> completed, string transactionKey, int amount,
            ref int balance, ref int lifetimeEarned)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            if (string.IsNullOrWhiteSpace(transactionKey)) throw new ArgumentException("A transaction key is required.");
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (completed.Contains(transactionKey)) return false;
            var nextBalance = checked(balance + amount);
            var nextEarned = (int)Math.Min(int.MaxValue, (long)Math.Max(0, lifetimeEarned) + amount);
            completed.Add(transactionKey);
            balance = nextBalance;
            lifetimeEarned = nextEarned;
            return true;
        }
    }
}
