using Telegram.Bot;

namespace TelegramTimetableBot.Methods
{
    public class SendCountdown
    {
        public static async Task SendCountdownAnimationAsync(ITelegramBotClient botClient, long chatId, string resultMessage)
        {
            // Send the initial "⏳" message
            var animationMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text : "⏳",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);

            // Wait for 2 seconds
            await Task.Delay(10000);

            // Countdown from 3 to 1
            foreach(var countdown in new[] { "3️⃣", "2️⃣", "1️⃣" })
            {
                await botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: animationMessage.MessageId,
                    text: $"<strong>{countdown} ...</strong>",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);

                await Task.Delay(2000);
            }

            // Delete the animation message
            await botClient.DeleteMessageAsync(chatId : chatId, messageId : animationMessage.MessageId);
        }
    }
}
