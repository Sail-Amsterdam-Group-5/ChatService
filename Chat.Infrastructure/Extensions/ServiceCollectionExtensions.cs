using Chat.Application.Interfaces;
using Chat.Application.Services;
using Chat.Core.Interfaces;
using Chat.Infrastructure.Configuration;
using Chat.Infrastructure.Data;
using Chat.Infrastructure.Repositories;
using Chat.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chat.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring infrastructure services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds infrastructure services to the service collection
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure WebPubSub
        services.Configure<WebPubSubSettings>(options =>
            configuration.GetSection("WebPubSub").Bind(options));

        // Configure CosmosDB
        services.Configure<CosmosDbSettings>(options =>
            configuration.GetSection("CosmosDb").Bind(options));

        // Register CosmosDB context
        services.AddSingleton<CosmosDbContext>();

        // Configure Blob Storage
        //services.Configure<BlobStorageSettings>(
        //configuration.GetSection("BlobStorage"));
        services.Configure<BlobStorageSettings>(options =>
            configuration.GetSection("BlobStorage").Bind(options));

        // Register WebPubSub service
        services.AddSingleton<IWebPubSubService, WebPubSubService>();

        // Register Blob Storage service
        services.AddSingleton<IBlobStorageService, BlobStorageService>();

        // Add repositories
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();

        // Add services
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IMessageService, MessageService>();

        // Register DeletedMessage services
        services.AddScoped<IDeletedMessageRepository, DeletedMessageRepository>();
        services.AddScoped<IDeletedMessageService, DeletedMessageService>();

        // Add DataSeeder
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}