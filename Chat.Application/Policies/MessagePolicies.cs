using Microsoft.Azure.Cosmos;
using Polly;
using System.Net;
using Azure;

namespace Chat.Application.Policies;

public static class MessagePolicies
{
    public static IAsyncPolicy<T> GetRetryPolicy<T>()
    {
        return Policy<T>
            .Handle<CosmosException>(e => e.StatusCode == HttpStatusCode.TooManyRequests)
            .Or<RequestFailedException>()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds} seconds");
                });
    }

    public static IAsyncPolicy<T> GetTimeoutPolicy<T>()
    {
        return Policy.TimeoutAsync<T>(10);
    }
}