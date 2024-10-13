using TelegramTimetableBot;
using TelegramTimetableBot.Services.TelegramBot;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<TelegramBotOptions>(builder.Configuration.GetSection(TelegramBotOptions.Secrets));
builder.Services.AddSingleton<TelegramBotService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
