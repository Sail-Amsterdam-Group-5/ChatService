using Microsoft.Azure.Cosmos;
using Polly;
using System.Net;
using Azure;
using Chat.Application.DTOs;
using Polly.RateLimit;
using Microsoft.Extensions.Configuration;

namespace Chat.Application.Policies;

public static class MessagePolicies
{
    private static readonly Dictionary<string, AsyncRateLimitPolicy<MessageDto>> _userRateLimiters =
        new Dictionary<string, AsyncRateLimitPolicy<MessageDto>>();

    private static IConfiguration _configuration;

    public static void Initialize(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public static IAsyncPolicy<MessageDto> GetRateLimitPolicy(string userId)
    {
        if (!_userRateLimiters.ContainsKey(userId))
        {
            var messagesPerTimeSpan = _configuration?.GetValue<int>("RateLimit:MessagesPerTimeSpan") ?? 5;
            var timeSpanSeconds = _configuration?.GetValue<int>("RateLimit:TimeSpanSeconds") ?? 10;
            var maxBurst = _configuration?.GetValue<int>("RateLimit:MaxBurst") ?? 2;

            var policy = Policy.RateLimitAsync<MessageDto>(
                numberOfExecutions: messagesPerTimeSpan,
                perTimeSpan: TimeSpan.FromSeconds(timeSpanSeconds),
                maxBurst: maxBurst);

            _userRateLimiters[userId] = policy;
        }

        return _userRateLimiters[userId];
    }

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
        return Policy.TimeoutAsync<T>(30);
    }
}