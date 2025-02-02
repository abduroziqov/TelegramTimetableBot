FROM alpine AS base
USER $APP_UID
WORKDIR /app

FROM alpine AS build
RUN apk add dotnet9-sdk
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
RUN apk add aspnetcore9-runtime
RUN apk add chromium
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TelegramTimetableBot.dll"]