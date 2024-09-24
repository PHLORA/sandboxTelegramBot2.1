# Use the official .NET image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TgBot/TgBot.csproj", "TgBot/"]
RUN dotnet restore "TgBot/TgBot.csproj"
COPY . .
WORKDIR "/src/TgBot"
RUN dotnet build "TgBot.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "TgBot.csproj" -c Release -o /app/publish

# Final stage: Run the app
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TgBot.dll"]