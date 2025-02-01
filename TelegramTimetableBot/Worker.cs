using TelegramTimetableBot.Services;

namespace TelegramTimetableBot
{
    public class Worker(ILogger<Worker> logger, TelegramBotService botService) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Running at: {time} Bot username: @{botname}", DateTimeOffset.Now, botService.GetMe().Result.Username);
            await botService.DeleteWebhook();
            botService.StartReceiving(stoppingToken);
        }
    }
}
