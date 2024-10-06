using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Microsoft.Playwright;
using Telegram.Bot.Types.InputFiles;

namespace TelegramTimetableBot.Service.Services.TelegramBot;

public class TelegramBotService
{
    private ITelegramBotClient _telegramBotClient;
    private readonly ILogger<TelegramBotService> _logger;
    private ReceiverOptions _receiverOptions;
    public readonly List<long> _userIds = new List<long>();
    private string _url = "https://tsue.edupage.org/timetable/view.php?num=77&class=-1650";

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
        if (update.Type == UpdateType.Message && update.Message.Text == "/start")
        {
            long userId = update.Message.From.Id;

            // Add the user to the list if they haven't already started to bot 
            if (!_userIds.Contains(userId))
            {
                _userIds.Add(userId);
            }

            string username = update.Message.From.FirstName;
            string welcomeMessage = $"Assalomu alaykum {username}.\n\nSizga yordam bera olishim uchun pastdagi buyruqlardan birini tanlang 👇";

            var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                [
                    [
                        new KeyboardButton("📅 Dars jadvali"), 
                        new KeyboardButton("📞 Aloqa")
                    ],
                    [
                        new KeyboardButton("📄 Ma'lumot"),     
                        new KeyboardButton("📊 Statistika")
                    ]
                ])
            {
                ResizeKeyboard = true
            };

            await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: welcomeMessage,
                replyMarkup: replyKeyboardMarkup
            );
        }
        else if (update.Type == UpdateType.Message)
        {
            var messageText = update.Message.Text;

            if (messageText == "📅 Dars jadvali")
            {
                await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: "Dars jadvali tayyorlanmoqda. Iltimos, kuting..."
                );

                _ = Task.Run(async () => await SendTimetablePdfAsync(botClient, update.Message.Chat.Id));
            }
            else if (messageText == "📞 Aloqa")
            {
                await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: "\U0001f9d1‍💻Shikoyatlar, dasturdagi xatoliklar va taklif uchun quyidagi manzillar orqali bog'lanishigiz mumkin:\r\n\r\n☎️ Telefon: +998-33-035-69-28\r\n\r\n✈️ Telegram: @abdurozikov_k"
                );
            }
            else if (messageText == "📄 Ma'lumot")
            {
                await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: "📌 Ushbu bot Toshkent Davlat Iqtisodiyot Universiteti talabalari uchun maxsus yaratilgan!\r\n\r\n\U0001f9d1‍💻 Dasturchi: @abdurozikov_k\r\n\r\n📢 Kanal: @tsueitclub"
                );
            }
            else if (messageText == "📊 Statistika")
            {
                int userCount = await GetUserCountAsync();
                await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: $"Hozirda bot bilan {userCount} foydalanuvchi aloqada bo'ldi."
                );
            }
        }
    }
    private async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception.Message);

        await Task.CompletedTask;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="botClient"></param>
    /// <param name="chatId"></param>
    /// <returns></returns>
    private async Task SendTimetablePdfAsync(ITelegramBotClient botClient, long chatId)
    {
        string pdfFilePath = await DownloadTimetableAsPdfAsync(_url);

        if (System.IO.File.Exists(pdfFilePath))
        {
            using Stream stream = System.IO.File.OpenRead(pdfFilePath);

            InputOnlineFile pdfFile = new InputOnlineFile(stream, "irb-61.pdf");

            await botClient.SendDocumentAsync(
                chatId: chatId,
                document: pdfFile,
                caption: $"📌irb-61 guruhining dars jadvali\r\n\r\nBoshqa guruh dars jadvalini olish uchun qaytadan \r\n\"📅 Dars jadvali\" tugmasini bosing! \r\n\r\nSana: {DateTime.Now.ToString("dd-MM-yyyy, HH:mm:ss")}"
            );

            System.IO.File.Delete(pdfFilePath);

            _logger.LogInformation($"[DownloadTimetableAsPdfAsync] Client:{chatId} Received");
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Failed to retrieve the timetable. Please try again later."
            );

            _logger.LogError($"[DownloadTimetableAsPdfAsync] Client:{chatId} Error");
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

            string pdfFilePath = Path.Combine(Directory.GetCurrentDirectory(), "timetable.pdf");

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
