using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramTimetableBot.Services.Downloader;

namespace TelegramTimetableBot.Services.TelegramBot;

public class TelegramBotService
{
    private ITelegramBotClient _telegramBotClient;
    private readonly ILogger<TelegramBotService> _logger;
    private ReceiverOptions _receiverOptions;
    public readonly List<long> _userIds = new List<long>();

    private string _url = "https://tsue.edupage.org/timetable/view.php?num=77&class=-1650";

    private Dictionary<long, DateTime> _lastTimetableRequestTime = new Dictionary<long, DateTime>();
    private Task[] Tasks { get; set; } = Array.Empty<Task>();
    private DownloaderService _downloaderService;


    public TelegramBotService(IConfiguration configuration, ILogger<TelegramBotService> logger, DownloaderService downloaderService)
    {
        _telegramBotClient = new TelegramBotClient(configuration["Secrets:BotToken"]!);
        _receiverOptions = new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() };
        _logger = logger;
        _downloaderService = downloaderService;

        _downloaderService.Pause += Pause;
        _downloaderService.Resume += Resume;
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

    private void Pause(object sender, EventArgs e)
    {

        // message from user => here
    }

    private void Resume(object sender, EventArgs e)
    {
        // pause restart
        // answer for messages 
    }
}
