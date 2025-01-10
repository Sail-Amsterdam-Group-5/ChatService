# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution and project files first
COPY ["Chat.API/Chat.API.csproj", "Chat.API/"]
COPY ["Chat.Core/Chat.Core.csproj", "Chat.Core/"]
COPY ["Chat.Infrastructure/Chat.Infrastructure.csproj", "Chat.Infrastructure/"]
COPY ["Chat.Application/Chat.Application.csproj", "Chat.Application/"]

# Restore dependencies
RUN dotnet restore "Chat.API/Chat.API.csproj"

# Copy the rest of the source code
COPY . .

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