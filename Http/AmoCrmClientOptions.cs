using System;

namespace FT.AmoCRM.Http
{
    /// <summary>Configures API retry behavior.</summary>
    public sealed class AmoCrmClientOptions
    {
        /// <summary>Maximum number of retries after the initial request.</summary>
        public int MaxRetryAttempts { get; set; } = 3;
        /// <summary>Initial exponential-backoff delay.</summary>
        public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
        /// <summary>Maximum retry delay.</summary>
        public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
    }
}
