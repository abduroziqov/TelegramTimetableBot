FROM alpine AS base
WORKDIR /app

FROM alpine AS publish
RUN apk add dotnet9-sdk
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TelegramTimetableBot.csproj", "."]
RUN dotnet restore "TelegramTimetableBot.csproj"
COPY [".", "."]
RUN dotnet publish "TelegramTimetableBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
RUN apk add aspnetcore9-runtime chromium
COPY --from=publish ["/app/publish", "."]
ENTRYPOINT ["dotnet", "/app/TelegramTimetableBot.dll"]