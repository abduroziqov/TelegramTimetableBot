using Microsoft.Playwright;
using TelegramTimetableBot;
using TelegramTimetableBot.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<IBrowser>();
builder.Services.AddSingleton<TelegramBotService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
