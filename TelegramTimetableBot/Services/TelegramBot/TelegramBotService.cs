using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Microsoft.Playwright;
using Telegram.Bot.Types.InputFiles;
using System.Diagnostics;

namespace TelegramTimetableBot.Service.Services.TelegramBot;

public class TelegramBotService
{
    private ITelegramBotClient _telegramBotClient;
    private readonly ILogger<TelegramBotService> _logger;
    private ReceiverOptions _receiverOptions;
    public readonly List<long> _userIds = new List<long>();
    private string _url = "https://tsue.edupage.org/timetable/view.php?num=77&class=-1650";
    private Task[] Tasks { get; set; } = new Task[10];

    private Dictionary<long, DateTime> _lastTimetableRequestTime = new Dictionary<long, DateTime>();

    public TelegramBotService(IConfiguration configuration, ILogger<TelegramBotService> logger)
    {
        _telegramBotClient = new TelegramBotClient(configuration["Secrets:BotToken"]!);
        _logger = logger;
        _receiverOptions = new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() };
    }

    public async Task<User> GetMeAsync() => await _telegramBotClient.GetMeAsync();
    public async Task DeleteWebhookAsync() => await _telegramBotClient.DeleteWebhookAsync();
    public void StartReceiving(CancellationToken cancellationToken) => _telegramBotClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, _receiverOptions, cancellationToken);
    public async Task<int> GetUserCountAsync() => await Task.FromResult(_userIds.Count);

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.Type == UpdateType.Message && update.Message.Text == "/start")
            {
                long userId = update.Message.From.Id;

                if (!_userIds.Contains(userId))
                {
                    _userIds.Add(userId);
                }

                string username = update.Message.From.FirstName;
                string welcomeMessage = $"Assalomu alaykum {username}.\n\nSizga yordam bera olishim uchun pastdagi buyruqlardan birini tanlang 👇";

                var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                    new[]
                    {
                    new[]
                    {
                        new KeyboardButton("📅 Dars jadvali"),
                        new KeyboardButton("📞 Aloqa")
                    },
                    new[]
                    {
                        new KeyboardButton("📄 Ma'lumot"),
                        new KeyboardButton("📊 Statistika")
                    }
                    })
                {
                    ResizeKeyboard = true
                };

                var sentMessage = await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: welcomeMessage,
                    replyMarkup: replyKeyboardMarkup
                );

                // Store and delete the message after the action
                //await DeleteMessageAfterActionAsync(botClient, update.Message.Chat.Id, sentMessage.MessageId);
            }
            else if (update.Type == UpdateType.Message)
            {
                var messageText = update.Message.Text;
                long userId = update.Message.From.Id;

                if (messageText == "📅 Dars jadvali")
                {
                    if (_lastTimetableRequestTime.TryGetValue(userId, out DateTime lastRequestTime))
                    {
                        TimeSpan timeSinceLastRequest = DateTime.UtcNow - lastRequestTime;
                        if (timeSinceLastRequest.TotalHours < 1)
                        {
                            int minutesRemaining = 59 - timeSinceLastRequest.Minutes;
                            int secondsRemaining = 59 - timeSinceLastRequest.Seconds;

                            var waitMessage = await botClient.SendTextMessageAsync(
                                chatId: update.Message.Chat.Id,
                                text: $"Siz dars jadvalini yaqinda oldingiz. Iltimos, {minutesRemaining} daqiqa {secondsRemaining} soniyadan keyin qayta urinib ko'ring."
                            );

                            //await DeleteMessageAfterActionAsync(botClient, update.Message.Chat.Id, waitMessage.MessageId);
                            return;
                        }
                    }

                    _lastTimetableRequestTime[userId] = DateTime.UtcNow;

                    var preparingMessage = await botClient.SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: "Dars jadvali tayyorlanmoqda(biroz vaqt oladi). Iltimos, kuting..."
                    );

                    // await DeleteMessageAfterActionAsync(botClient, update.Message.Chat.Id, preparingMessage.MessageId);

                    await SendTimetablePdfAsync(botClient, update);
                }
                else if (messageText == "📞 Aloqa")
                {
                    var contactMessage = await botClient.SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: "\U0001f9d1‍💻Shikoyatlar, dasturdagi xatoliklar va taklif uchun quyidagi manzillar orqali bog'lanishingiz mumkin:\r\n\r\n☎️ Telefon: +998-33-035-69-28\r\n\r\n✈️ Telegram: @abdurozikov_k"

                    );

                    //await DeleteMessageAfterActionAsync(botClient, update.Message.Chat.Id, contactMessage.MessageId);
                }
                else if (messageText == "📄 Ma'lumot")
                {
                    var infoMessage = await botClient.SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: "📌 Ushbu bot Raqamli Iqtisodiyot Fakulteti uchun maxsus yaratilgan!\r\n\r\n\U0001f9d1‍💻 Dasturchi: @abdurozikov_k\r\n\r\n📢 Kanal: @bek_sharpist"
                    );

                    // Store and delete the message after action
                    //await DeleteMessageAfterActionAsync(botClient, update.Message.Chat.Id, infoMessage.MessageId);
                }
                else if (messageText == "📊 Statistika")
                {
                    var statsMessage = await botClient.SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: "Ushbu bo'lim ishlab chiqilmoqda"
                    );

                    //await DeleteMessageAfterActionAsync(botClient, update.Message.Chat.Id, statsMessage.MessageId);
                }
            }
        }
        catch (Exception ex)
        {
            await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: "Too many requests. Please try later."
            );

            _logger.LogError($"[HandleUpdateAsync] (@{update.Message.From.Username ?? update.Message.From.FirstName}) {ex.Message}");
        }
    }

    private async Task DeleteMessageAfterActionAsync(ITelegramBotClient botClient, long chatId, int messageId)
    {
        // Wait for a few seconds before deleting the message
        await Task.Delay(10000); // 10 seconds delay

        try
        {
            await botClient.DeleteMessageAsync(chatId, messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting message {messageId}: {ex.Message}");
        }
    }

    private async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception.Message);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Send timetable to client
    /// </summary>
    /// <param name="botClient"></param>
    /// <param name="update"></param>
    /// <returns></returns>
    private async Task SendTimetablePdfAsync(ITelegramBotClient botClient, Update update)
    {
        string pdfFilePath = await DownloadTimetableAsPdfAsync(_url);

        // Close Chrome instances after the timetable PDF is downloaded
        CloseChromeInstances();

        _logger.LogInformation($"PDF should be downloaded to: {pdfFilePath}");

        try
        {
            if (System.IO.File.Exists(pdfFilePath))
            {
                _logger.LogInformation("File found: " + pdfFilePath);

                using (Stream stream = System.IO.File.Open(pdfFilePath, FileMode.Open))
                {
                    InputOnlineFile pdfFile = new InputOnlineFile(stream, $"{Guid.NewGuid()}.pdf");

                    await botClient.SendDocumentAsync(
                        chatId: update.Message.Chat.Id,
                        document: pdfFile,
                        caption: $"📌irb-61 guruhining dars jadvali\r\n\r\nBoshqa guruh dars jadvalini olish uchun qaytadan \r\n\"📅 Dars jadvali\" tugmasini bosing! \r\n\r\nSana: {DateTime.Now.ToString("dd-MM-yyyy, HH:mm:ss")}"
                    );
                }

                System.IO.File.Delete(pdfFilePath);
                _logger.LogInformation($"[DownloadTimetableAsPdfAsync] Client: {update.Message.From.Username ?? update.Message.From.FirstName} Received");
            }
            else
            {
                _logger.LogError("File not found after download attempt: " + pdfFilePath);

                await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: "Failed to retrieve the timetable. Please try again later."
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception: {ex.Message}");

            await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: $"Exception : {ex.Message}"
            );
        }
        finally
        {
            CloseChromeInstances();
        }
    }



    private void CloseChromeInstances()
    {
        try
        {
            // Get all processes with the name "chrome"
            Process[] chromeProcesses = Process.GetProcessesByName("chrome");

            // Iterate through each process and kill it
            foreach (Process process in chromeProcesses)
            {
                process.Kill();
            }

            _logger.LogInformation("All Chrome instances have been closed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to close Chrome processes: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns timetable from specific url
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private async Task<string> DownloadTimetableAsPdfAsync(string url)
    {
        try
        {
            var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                ExecutablePath = OperatingSystem.IsWindows() ? @"C:\Program Files\Google\Chrome\Application\chrome.exe" : @"/usr/bin/google-chrome",
                Headless = true
            });
            var page = await browser.NewPageAsync();

            await page.GotoAsync(_url);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.ClickAsync("text='OK'");
            await page.WaitForTimeoutAsync(1000);
            await page.WaitForSelectorAsync("#fitheight");
            await page.EvaluateAsync("document.getElementById('fitheight').childNodes[0].remove();");
            await page.WaitForTimeoutAsync(1000);

            string pdfFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Files", $"{Guid.NewGuid().ToString()}.pdf");

            await page.PdfAsync(new PagePdfOptions
            {
                Path = pdfFilePath,
                Landscape = true,
                PreferCSSPageSize = true,
                Format = "A4",
                PrintBackground = true,
                PageRanges = "2",
            });

            await browser.CloseAsync();

            return pdfFilePath;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, e.InnerException, e.StackTrace);

            return string.Empty;
        }
    }
}
