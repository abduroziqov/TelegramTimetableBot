FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TelegramTimetableBot/TelegramTimetableBot.csproj", "TelegramTimetableBot/"]
RUN dotnet restore "TelegramTimetableBot/TelegramTimetableBot.csproj"
COPY . .
WORKDIR "/src/TelegramTimetableBot"
RUN dotnet build "TelegramTimetableBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TelegramTimetableBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TelegramTimetableBot.dll"]
