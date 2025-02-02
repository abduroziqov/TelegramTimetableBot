using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramTimetableBot.Methods;

namespace TelegramTimetableBot.Services;

public class TelegramBotService(
    IConfiguration configuration,
    ILogger<TelegramBotService> logger,
    BrowserService browserService)
{
    private          ITelegramBotClient          _telegramBotClient = new TelegramBotClient(configuration["BotToken"]!);

    private readonly ReceiverOptions             _receiverOptions = new() { AllowedUpdates = [] };
    private readonly List<long>                  _userIds = new();
    private string                               _url = "https://tsue.edupage.org/timetable/view.php?num=81&class=-1650";
    private readonly Dictionary<long, DateTime>  _lastTimetableRequestTime = new();

    private Task[] Tasks { get; set; } = [];

    public async Task<User> GetMe() => await _telegramBotClient.GetMe();
    public async Task DeleteWebhook() => await _telegramBotClient.DeleteWebhook();
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

                ReplyKeyboardMarkup replyKeyboardMarkup = TelegramKeyboards.GetMainMenuKeyboard();

                

                Tasks.Append(botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: welcomeMessage,
                    replyMarkup: replyKeyboardMarkup));
            }
            else if (update.Type == UpdateType.Message)
            {
                var messageText = update.Message.Text;
                long userId = update.Message.From.Id;

                if (messageText == "🔙 Orqaga")
                {
                    // Call the GetMainMenuKeyboard method from the class
                    var replyKeyboardMarkup = TelegramKeyboards.GetMainMenuKeyboard();

                    await botClient.SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: "Please select an option:",
                        replyMarkup: replyKeyboardMarkup
                    );
                }

                else if (messageText == "📅 Dars jadvali")
                {
                    /*if (_lastTimetableRequestTime.TryGetValue(userId, out DateTime lastRequestTime))
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
                    }*/

                    // Update the last time request time
                    _lastTimetableRequestTime[userId] = DateTime.UtcNow;

                    // Create a new keyboard with the addintional buttons
                    var replyKeyboardMarkupForIRB = new ReplyKeyboardMarkup(new[]
                    {
                        new[] { new KeyboardButton("IRB-61")},
                        new[] { new KeyboardButton("IRB-62")},
                        new[] { new KeyboardButton("🔙 Orqaga") }
                    })
                    {
                        ResizeKeyboard = true
                    };

                    Tasks.Append(botClient.SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: "Iltimos, quyidagi tugmalardan birini tanlang:",
                        replyMarkup: replyKeyboardMarkupForIRB
                        ));

                    //Tasks.Append(SendTimetablePdfAsync(botClient, update));
                }
                /* else if(messageText == "IRB-61")
                 {
                     // Send the timetable PDF for IRB-61
                     await SendCountdown.SendCountdownAnimationAsync(botClient, update.Message.Chat.Id, "IRB-61 uchun dars jadvali tayyorlanmoqda.");
                     *//*_url = "https://tsue.edupage.org/timetable/view.php?num=81&class=-1650";  // Link for IRB-61
                     Tasks.Append(SendTimetablePdfAsync(botClient, update));*//*
                     await SendTimetablePdfAsync(botClient, update, "IRB-61");  // Pass "IRB-61"
                 }
                 else if (messageText == "IRB-62")
                 {
                     // Send message for IRB-62 
                     await SendCountdown.SendCountdownAnimationAsync(botClient, update.Message.Chat.Id, "IRB-62 uchun dars jadvali tayyorlanmoqda.");
                     await SendTimetablePdfAsync(botClient, update, "IRB-62");  // Pass "IRB-62"
                 }*/
                else if (messageText == "IRB-61")
                {
                    _url = "https://tsue.edupage.org/timetable/view.php?num=81&class=-1650";  // URL for IRB-61
                    await SendCountdown.SendCountdownAnimationAsync(botClient, update.Message.Chat.Id, "IRB-61 uchun dars jadvali tayyorlanmoqda.");
                    await SendTimetablePdfAsync(botClient, update, "IRB-61");
                }
                else if (messageText == "IRB-62")
                {
                    _url = "https://tsue.edupage.org/timetable/view.php?num=81&class=-1651";  // URL for IRB-62
                    await SendCountdown.SendCountdownAnimationAsync(botClient, update.Message.Chat.Id, "IRB-62 uchun dars jadvali tayyorlanmoqda.");
                    await SendTimetablePdfAsync(botClient, update, "IRB-62");
                }

                else if (messageText == "📄 Ma'lumot")
                {
                    Tasks.Append(botClient.SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: "📌 Ushbu bot Toshkent Davlat Iqtisodiyot Universiteti talabalari uchun maxsus yaratilgan!\r\n\r\n\U0001f9d1‍💻 Dasturchi: @abdurozikov_k\r\n\r\n📢 Kanal: @bek_sharpist"));
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: "Men bu buyruqni tushunmadim. Iltimos, variantni tanlang."
                    );
                }
            }
        }
        catch (Exception ex)
        {
            Tasks.Append(botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: "Too many requests. Please try later."));

            logger.LogError($"[HandleUpdateAsync] (@{update.Message.From.Username ?? update.Message.From.FirstName}) {ex.Message}");
        }
    }

    private async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception.Message);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Send timetable to client
    /// </summary>
    /// <param name="botClient"></param>
    /// <param name="update"></param>
    /// <returns></returns>
  
    private async Task SendTimetablePdfAsync(ITelegramBotClient botClient, Update update, string groupName)
    {
        // Step 1: Download the PDF as a byte array
        byte[] pdfBytes = await browserService.DownloadTimetableAsPdfAsync(_url);

        if (pdfBytes.Length == 0)
        {
            logger.LogError("Failed to generate the PDF.");

            // Notify the user if PDF generation fails
            await botClient.SendMessage(
                chatId: update.Message.Chat.Id,
                text: $"❌ PDF-ni yuklashda xatolik yuz berdi. Iltimos, qayta urinib ko'ring."
            );
            return;
        }

        logger.LogInformation("PDF successfully generated in memory.");

        try
        {
            // Step 2: Send the PDF to the user
            using (var stream = new MemoryStream(pdfBytes))
            {
                var pdf = new InputFileStream(stream, "Dars_jadvali.pdf");

                string caption = groupName == "IRB-61"
                ? $"📌 IRB-61 guruhining dars jadvali\r\n\r\nBoshqa guruh dars jadvalini olish uchun qaytadan\r\n\"📅 Dars jadvali\" tugmasini bosing!\r\n\r\nSana: {DateTime.Now:dd-MM-yyyy, HH:mm:ss}"
                : $"📌 IRB-62 guruhining dars jadvali\r\n\r\nBoshqa guruh dars jadvalini olish uchun qaytadan\r\n\"📅 Dars jadvali\" tugmasini bosing!\r\n\r\nSana: {DateTime.Now:dd-MM-yyyy, HH:mm:ss}";


                await botClient.SendDocument(
                    chatId: update.Message.Chat.Id,
                    document: pdf,
                    caption: caption
                );
            }

            logger.LogInformation($"[DownloadTimetableAsPdfAsync] Client: {update.Message.From.Username ?? update.Message.From.FirstName} received the timetable.");
        }
        catch (Exception ex)
        {
            logger.LogError($"Exception: {ex.Message}");

            // Notify the user of the exception
            await botClient.SendMessage(
                chatId: update.Message.Chat.Id,
                text: $"❌ Xatolik: {ex.Message}"
            );
        }
    }

    public static class TelegramKeyboards
    {
        public static ReplyKeyboardMarkup GetMainMenuKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
            new[] { new KeyboardButton("📅 Dars jadvali") },
            new[] { new KeyboardButton("📄 Ma'lumot") }
        })
            {
                ResizeKeyboard = true
            };
        }
    }
}
