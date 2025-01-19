# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution and project files first
COPY ["Chat.API/Chat.API.csproj", "Chat.API/"]
COPY ["Chat.Core/Chat.Core.csproj", "Chat.Core/"]
COPY ["Chat.Infrastructure/Chat.Infrastructure.csproj", "Chat.Infrastructure/"]
COPY ["Chat.Application/Chat.Application.csproj", "Chat.Application/"]
COPY ["Chat.Infrastructure.Tests/Chat.Infrastructure.Tests.csproj", "Chat.Infrastructure.Tests/"]
COPY ["Chat.Application.Tests/Chat.Application.Tests.csproj", "Chat.Application.Tests/"]
COPY ["Chat.API.Tests/Chat.API.Tests.csproj", "Chat.API.Tests/"]

# Restore dependencies
RUN dotnet restore "Chat.API/Chat.API.csproj"
RUN dotnet restore "Chat.Infrastructure.Tests/Chat.Infrastructure.Tests.csproj"
RUN dotnet restore "Chat.Application.Tests/Chat.Application.Tests.csproj"
RUN dotnet restore "Chat.API.Tests/Chat.API.Tests.csproj"

# Copy the rest of the source code
COPY . .

# Run tests with environment variables
ENV CosmosDb__ConnectionString="dummy-connection-string" \
    CosmosDb__DatabaseName="test-db" \
    CosmosDb__MessagesContainer="Messages" \
    CosmosDb__ChatsContainer="Chats" \
    CosmosDb__DevicesContainer="Devices" \
    CosmosDb__DeletedMessagesContainer="DeletedMessages"

# Run tests
RUN dotnet test --no-restore --verbosity normal

# Build the application
RUN dotnet build "Chat.API/Chat.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Chat.API/Chat.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
USER 1001
ENTRYPOINT ["dotnet", "Chat.API.dll"]