using TelegramTimetableBot.Service.Services.TelegramBot;

namespace TelegramTimetableBot.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly TelegramBotService _botService;

        public Worker(ILogger<Worker> logger, TelegramBotService botService)
        {
            _logger = logger;
            _botService = botService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Running at: {time} Bot username: @{botname}", DateTimeOffset.Now, _botService.GetMeAsync().Result.Username);
            await _botService.DeleteWebhookAsync();
            _botService.StartReceiving(stoppingToken);
        }
    }
}
