using Microsoft.Playwright;
using static System.Threading.Tasks.Task;

namespace TelegramTimetableBot.Services;

public class BrowserService
{
    private readonly ILogger<BrowserService> _logger;
    private IBrowser? Instance { get; set; }
    private const string WindowsPath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
    private const string LinuxPath = "/usr/bin/chromium-browser";

    public BrowserService(ILogger<BrowserService> logger)
    {
        _logger = logger;
        
        Run(LaunchBrowserAsync);
    }

    private async Task LaunchBrowserAsync()
    {
        var playwright = await Playwright.CreateAsync();

        Instance = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            ExecutablePath = OperatingSystem.IsWindows() ? WindowsPath : LinuxPath,
            Headless = true
        });
    }
    
    public async Task<byte[]> DownloadTimetableAsPdfAsync(string url)
    {
        try
        {
            if (Instance is not null)
            {
                var page = await Instance.NewPageAsync();

                await page.GotoAsync(url);
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.ClickAsync("text='OK'");
                await page.WaitForTimeoutAsync(1000);
                await page.WaitForSelectorAsync("#fitheight");
                await page.EvaluateAsync("document.getElementById('fitheight').childNodes[0].remove();");
                await page.WaitForTimeoutAsync(1000);

                // Generate the PDF as a byte array
                var pdfBytes = await page.PdfAsync(new PagePdfOptions
                {
                    Landscape = true,
                    PreferCSSPageSize = true,
                    Format = "A4",
                    PrintBackground = true,
                    PageRanges = "2",
                });

                return pdfBytes;
            }

            return [];
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, e.InnerException, e.StackTrace);

            return [];
        }
    }
}