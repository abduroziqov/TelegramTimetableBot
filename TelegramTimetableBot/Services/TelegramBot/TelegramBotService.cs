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
    private Task[] Tasks { get; set; } = Array.Empty<Task>();
    private Dictionary<long, DateTime> _lastTimetableRequestTime = new Dictionary<long, DateTime>();
    private IBrowserContext Browser { get; set; }

    public TelegramBotService(IConfiguration configuration, ILogger<TelegramBotService> logger)
    {
        _telegramBotClient = new TelegramBotClient(configuration["Secrets:BotToken"]!);
        _logger = logger;
        _receiverOptions = new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() };

        Browser = InitializeBrowser().Result;
    }

    /// <summary>
    /// Initializes single instance of browser
    /// </summary>
    /// <returns></returns>
    private async Task<IBrowserContext> InitializeBrowser()
    {
        var playwright = await Playwright.CreateAsync();

        return await playwright.Chromium.LaunchPersistentContextAsync(OperatingSystem.IsWindows() ? @"C:\Program Files\Google\Chrome\Application\chrome.exe" : @"/usr/bin/google-chrome");
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

                 await (botClient.SendTextMessageAsync(
                     chatId: update.Message.Chat.Id,
                     text: welcomeMessage,
                     replyMarkup: replyKeyboardMarkup
                 ));
             }
             else if (update.Type == UpdateType.Message)
             {
                 var messageText = update.Message.Text;

                 if (messageText == "📅 Dars jadvali")
                 {
                     Tasks.Append(botClient.SendTextMessageAsync(
                         chatId: update.Message.Chat.Id,
                         text: "Dars jadvali tayyorlanmoqda(biroz vaqt oladi). Iltimos, kuting..."
                     ));

                     Tasks.Append(SendTimetablePdfAsync(botClient, update));
                 }
                 else if (messageText == "📞 Aloqa")
                 {
                     Tasks.Append(botClient.SendTextMessageAsync(
                         chatId: update.Message.Chat.Id,
                         text: "\U0001f9d1‍💻Shikoyatlar, dasturdagi xatoliklar va taklif uchun quyidagi manzillar orqali bog'lanishigiz mumkin:\r\n\r\n☎️ Telefon: +998-33-035-69-28\r\n\r\n✈️ Telegram: @abdurozikov_k"
                     ));
                 }
                 else if (messageText == "📄 Ma'lumot")
                 {
                     Tasks.Append(botClient.SendTextMessageAsync(
                         chatId: update.Message.Chat.Id,
                         text: "📌 Ushbu bot Toshkent Davlat Iqtisodiyot Universiteti talabalari uchun maxsus yaratilgan!\r\n\r\n\U0001f9d1‍💻 Dasturchi: @abdurozikov_k\r\n\r\n📢 Kanal: @" 
                     ));
                 }
                 else if (messageText == "📊 Statistika")
                 {
                     int userCount = await GetUserCountAsync();

                     await botClient.SendTextMessageAsync(
                         chatId: update.Message.Chat.Id,
                         text: "Ushbu bo'lim ishlab chiqilmoqda"
                     //text: $"Hozirda bot bilan {userCount} foydalanuvchi aloqada bo'ldi."
                     );
                 }
             }
         }
         catch(Exception ex)
         {
             await botClient.SendTextMessageAsync(
                 chatId: update.Message.Chat.Id,
                 text: "Too many requests. Please try later.");

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

        if (System.IO.File.Exists(pdfFilePath))
        {
            using (Stream stream = System.IO.File.Open(pdfFilePath, FileMode.Open))
            {
                InputOnlineFile pdfFile = new InputOnlineFile(stream, $"{Guid.NewGuid().ToString()}.pdf");

                await botClient.SendDocumentAsync(
                    chatId: update.Message.Chat.Id,
                    document: pdfFile,
                    caption: $"📌irb-61 guruhining dars jadvali\r\n\r\nBoshqa guruh dars jadvalini olish uchun qaytadan \r\n\"📅 Dars jadvali\" tugmasini bosing! \r\n\r\nSana: {DateTime.Now.ToString("dd-MM-yyyy, HH:mm:ss")}"
                );

                System.IO.File.Delete(pdfFilePath);
            }

            _logger.LogInformation($"[DownloadTimetableAsPdfAsync] Client:{update.Message.From.Username ?? update.Message.From.FirstName} Received");
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: "Failed to retrieve the timetable. Please try again later."
            );

            _logger.LogError($"[DownloadTimetableAsPdfAsync] Client:" + $" {update.Message.From.Username ?? update.Message.From.FirstName} Error");
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

            await page.CloseAsync();

            return pdfFilePath;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, e.InnerException, e.StackTrace);

            return string.Empty;
        }
    }
}
