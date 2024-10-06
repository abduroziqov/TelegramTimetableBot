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
    private async Task SendTimetablePdfAsync(ITelegramBotClient botClient, long chatId)
    {
        // Call the method to download the timetable as PDF
        string pdfFilePath = await DownloadTimetableAsPdf();

        if (System.IO.File.Exists(pdfFilePath))
        {
            using Stream stream = System.IO.File.OpenRead(pdfFilePath);

            InputOnlineFile pdfFile = new InputOnlineFile(stream, "irb-61.pdf");

            // Send the PDF file to the user
            await botClient.SendDocumentAsync(
                chatId: chatId,
                document: pdfFile,
                caption: $"📌irb-61 guruhining dars jadvali\r\n\r\nBoshqa guruh dars jadvalini olish uchun qaytadan \r\n\"📅 Dars jadvali\" tugmasini bosing! \r\n\r\nSana: {DateTime.Now.ToString("dd-MM-yyyy, HH:mm:ss")}"
            );

            System.IO.File.Delete(pdfFilePath);
        }
        else
        {
            // Notify the user if the timetable download fails
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Failed to retrieve the timetable. Please try again later."
            );
        }
    }
    private async Task<string> DownloadTimetableAsPdf()
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
            
            // Set viewport size
            await page.SetViewportSizeAsync(1920, 1080);

            // Navigate to the page with the timetable
            await page.GotoAsync("https://tsue.edupage.org/timetable/view.php");

            // Wait for network to be idle to ensure the page has fully loaded
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.ClickAsync("text='OK'");

            // Wait a moment for the popup to be removed
            await page.WaitForTimeoutAsync(1000);

            // Remove a specific div (for example, by its class or ID) using JavaScript
            // Remove the top bar menu
            await page.WaitForSelectorAsync("#fitheight");
            await page.EvaluateAsync("document.getElementById('fitheight').childNodes[0].remove();");

            // Wait a moment for the popup to be removed
            await page.WaitForTimeoutAsync(1000);

            // Define the PDF file path
            string pdfFilePath = Path.Combine(Directory.GetCurrentDirectory(), "timetable.pdf");

            // Generate the PDF and save it to the file path
            await page.PdfAsync(new PagePdfOptions
            {
                Path = pdfFilePath,
                Landscape = true,
                PreferCSSPageSize = true,
                Format = "A4", // Set the page format to A4
                PrintBackground = true, // Include background colors/images
                PageRanges = "2",
            });

            _logger.LogInformation($"PDF file saved at: {pdfFilePath}");

            // Close the browser
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
