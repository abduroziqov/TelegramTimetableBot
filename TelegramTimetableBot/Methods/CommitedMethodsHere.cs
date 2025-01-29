namespace TelegramTimetableBot.Methods;

public class CommitedMethodsHere
{
    /*  private async Task SendTimetablePdfAsync(ITelegramBotClient botClient, Update update)
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
                    *//*InputOnlineFile pdfFile = new InputOnlineFile(stream, $"{pdfFilePath.Split(['/', '\\']).Last()}");*//*
                    InputOnlineFile pdfFile = new InputOnlineFile(
                         stream,
                         $"{pdfFilePath.Split(new[] { '/', '\\' }).Last()}");


                    // Step 2: send this file to the client
                    await botClient.SendDocumentAsync(
                        chatId: update.Message.Chat.Id,
                        //document: pdfFile,
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
    }*/


    /*private async Task<string> DownloadTimetableAsPdfAsync(string url)
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

            //await Browser.CloseAsync();

            return pdfFilePath;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, e.InnerException, e.StackTrace);

            return string.Empty;
        }
    }*/

}
