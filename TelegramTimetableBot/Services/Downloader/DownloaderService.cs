using Microsoft.Playwright;
using System.ComponentModel;
using TelegramTimetableBot.Services.TelegramBot;

namespace TelegramTimetableBot.Services.Downloader
{
    public class DownloaderService
    {
        private IBrowser? Browser { get; set; }
        private List<string> Urls { get; set; } = new List<string>();
        private readonly ILogger<TelegramBotService> _logger;

        public event EventHandler Resume;
        public event EventHandler Pause;

        public DownloaderService(ILogger<TelegramBotService> logger)
        {
            InitializeBrowser();
            Urls.AddRange(["https://tsue.edupage.org/timetable/view.php?num=77&class=-1650"]);
            _logger = logger;

            Timer timer = new Timer(TimerCallbackAsync, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        }

        private async void InitializeBrowser()
        {
            var playwright = await Playwright.CreateAsync();

            Browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                ExecutablePath = OperatingSystem.IsWindows() ? @"C:\Program Files\Google\Chrome\Application\chrome.exe" : "/usr/bin/google-chrome",
                Headless = true
            });
        }

        private async void TimerCallbackAsync(object state)
        {
            Pause.Invoke(null, null);
            await DownloadDocsAsync();
        }

        public async Task DownloadDocsAsync()
        {
            try
            {
                var page = await Browser.NewPageAsync();

                await page.GotoAsync(Urls[0]);
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.ClickAsync("text='OK'");
                await page.WaitForTimeoutAsync(1000);
                await page.WaitForSelectorAsync("#fitheight");
                await page.EvaluateAsync("document.getElementById('fitheight').childNodes[0].remove();");
                await page.WaitForTimeoutAsync(1000);  

                string pdfFilePath = $"{Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf")}";

                await page.PdfAsync(new PagePdfOptions
                {
                    Path = pdfFilePath,
                    Landscape = true,
                    PreferCSSPageSize = true,
                    Format = "A4",
                    PrintBackground = true,
                    PageRanges = "2",
                });

                await Browser.CloseAsync();

                Resume.Invoke(pdfFilePath, null);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e.InnerException, e.StackTrace);
            }
        }

    }
}
