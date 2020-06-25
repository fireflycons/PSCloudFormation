namespace Firefly.CloudFormation.Utils
{
    using System;
    using System.Threading.Tasks;

    using Amazon.Runtime;

    using Polly;
    using Polly.Contrib.WaitAndRetry;
    using Polly.Retry;

    /// <summary>
    /// Polly retry helpers
    /// </summary>
    internal static class PollyHelper
    {
        /// <summary>
        /// The exponential back off with jitter policy
        /// </summary>
        private static readonly AsyncRetryPolicy ExponentialBackOffWithJitter = Policy.Handle<AmazonServiceException>()
            .WaitAndRetryAsync(
                Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), 6));

        /// <summary>
        /// Executes the given delegate with polly retry.
        /// </summary>
        /// <typeparam name="T">Return type of function delegate</typeparam>
        /// <param name="func">The function delegate.</param>
        /// <returns>Awaitable result</returns>
        public static Task<T> ExecuteWithPolly<T>(Func<Task<T>> func)
        {
            return ExponentialBackOffWithJitter.ExecuteAsync(func);
        }
    }
}