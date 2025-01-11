using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Microsoft.Playwright;
using Telegram.Bot.Types.InputFiles;

namespace TelegramTimetableBot.Services.TelegramBot;

public class TelegramBotService
{
    private          ITelegramBotClient          _telegramBotClient;
    private readonly ILogger<TelegramBotService> _logger;
    private          ReceiverOptions             _receiverOptions;
    public readonly  List<long>                  _userIds = new List<long>();
    //private string                               _url = "https://tsue.edupage.org/timetable/view.php?num=77&class=-1650";
    private string                               _url = "https://tsue.edupage.org/timetable/view.php?num=80&class=-1650";
    private Dictionary<long, DateTime>           _lastTimetableRequestTime = new Dictionary<long, DateTime>();

    private Task[] Tasks { get; set; } = Array.Empty<Task>();
    private IBrowser Browser { get; set; }

    public TelegramBotService(IConfiguration configuration, ILogger<TelegramBotService> logger)
    {
        _telegramBotClient = new TelegramBotClient(configuration["Secrets:BotToken"]!);
        _receiverOptions   = new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() };
        _logger            = logger;

        InitializeBrowser();
    }

    /// <summary>
    /// Creates and launch single Browser instance
    /// </summary>
    private async void InitializeBrowser()
    {
        var playwright = await Playwright.CreateAsync();

        Browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            ExecutablePath = OperatingSystem.IsWindows() ? @"C:\Program Files\Google\Chrome\Application\chrome.exe" : @"/usr/bin/google-chrome",
            Headless = true
        });
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

                var replyKeyboardMarkup = new ReplyKeyboardMarkup([
                    [new KeyboardButton("📅 Dars jadvali")],
                    [new KeyboardButton("📞 Aloqa")],
                    [new KeyboardButton("📄 Ma'lumot")],
                    [new KeyboardButton("📊 Statistika")]])
                {
                    ResizeKeyboard = true
                };

                Tasks.Append(botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: welcomeMessage,
                    replyMarkup: replyKeyboardMarkup));
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

                            Tasks.Append(botClient.SendTextMessageAsync(
                                chatId: update.Message.Chat.Id,
                                text: $"Siz dars jadvalini yaqinda oldingiz. Iltimos, {minutesRemaining} daqiqa {secondsRemaining} soniyadan keyin qayta urinib ko'ring."));
                            return;
                        }
                    }

                    _lastTimetableRequestTime[userId] = DateTime.UtcNow;

                    Tasks.Append(SendTimetablePdfAsync(botClient, update));
                }
                else if (messageText == "📞 Aloqa")
                {
                    Tasks.Append(botClient.SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: "\U0001f9d1‍💻Shikoyatlar, dasturdagi xatoliklar va taklif uchun quyidagi manzillar orqali bog'lanishingiz mumkin:\r\n\r\n☎️ Telefon: +998-33-035-69-28\r\n\r\n✈️ Telegram: @abdurozikov_k"));

                }
                else if (messageText == "📄 Ma'lumot")
                {
                    Tasks.Append(botClient.SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: "📌 Ushbu bot Toshkent Davlat Iqtisodiyot Universiteti talabalari uchun maxsus yaratilgan!\r\n\r\n\U0001f9d1‍💻 Dasturchi: @abdurozikov_k\r\n\r\n📢 Kanal: @bek_sharpist"));
                }
                else if (messageText == "📊 Statistika")
                {
                    Tasks.Append(botClient.SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: "Ushbu bo'lim ishlab chiqilmoqda"));
                }
            }
        }
        catch (Exception ex)
        {
            Tasks.Append(botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: "Too many requests. Please try later."));

            _logger.LogError($"[HandleUpdateAsync] (@{update.Message.From.Username ?? update.Message.From.FirstName}) {ex.Message}");
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

        _logger.LogInformation($"PDF should be downloaded to: {pdfFilePath}");

        try
        {
            if (System.IO.File.Exists(pdfFilePath))
            {
                _logger.LogInformation("File found: " + pdfFilePath);

                // Step 1: check the access to read file and save it to buffer with [guid].pdf
                using (Stream stream = System.IO.File.Open(pdfFilePath, FileMode.Open))
                {
                    InputOnlineFile pdfFile = new InputOnlineFile(stream, $"{pdfFilePath.Split(['/', '\\']).Last()}");

                    // Step 2: send this file to the client
                    await botClient.SendDocumentAsync(
                        chatId: update.Message.Chat.Id,
                        document: pdfFile,
                        caption: $"📌irb-61 guruhining dars jadvali\r\n\r\nBoshqa guruh dars jadvalini olish uchun qaytadan \r\n\"📅 Dars jadvali\" tugmasini bosing! \r\n\r\nSana: {DateTime.Now.ToString("dd-MM-yyyy, HH:mm:ss")}");
                }

                // Step 3: drop this file
                System.IO.File.Delete(pdfFilePath);

                _logger.LogInformation($"[DownloadTimetableAsPdfAsync] Client: {update.Message.From.Username ?? update.Message.From.FirstName} Received");
            }
            else
            {
                _logger.LogError("File not found after download attempt: " + pdfFilePath);

                // Step 1: Send the clickable line once the user is notified
                var preparingMessage = await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: $"📅 Dars jadvalini ko'rish uchun bosing: [Dars jadvali](https://tsue.edupage.org/timetable/view.php?num=77&class=-1650)",
                    parseMode: ParseMode.Markdown);

                // Step 2: Pin the clickable link message
                await botClient.PinChatMessageAsync(
                    chatId: update.Message.Chat.Id,
                    messageId: preparingMessage.MessageId,
                    disableNotification: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception: {ex.Message}");

            await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: $"Exception : {ex.Message}");
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
            var page = await Browser.NewPageAsync();

            await page.GotoAsync(_url);
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

            return pdfFilePath;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, e.InnerException, e.StackTrace);

            return string.Empty;
        }
    }
}
