using TelegramTimetableBot.Services.Downloader;
using TelegramTimetableBot.Services.TelegramBot;

namespace TelegramTimetableBot
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly TelegramBotService _botService;
        private readonly DownloaderService _downloader;

        public Worker(ILogger<Worker> logger, TelegramBotService botService, DownloaderService downloader)
        {
            _logger = logger;
            _botService = botService;
            _downloader = downloader;
            
            _downloader.Pause += DownloaderOnPause;
            _downloader.Resume += DownloaderOnResume;
        }

        private void DownloaderOnResume(object? sender, EventArgs e)
        {
            _botService.StartReceiving();
        }

        private void DownloaderOnPause(object? sender, EventArgs e)
        {
            _botService.StopReceiving();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Running at: {time} Bot username: @{botname}", DateTimeOffset.Now, _botService.GetMeAsync().Result.Username);
            await _botService.DeleteWebhookAsync();
            _botService.StartReceiving();
        }
    }
}
