using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TgBot;


var cts = new CancellationTokenSource();
var botClient = new TelegramBotClient("6454828135:AAHEClrhq9RF44QFQVyKSqz1EiYahklMVlo");

await botClient.DeleteWebhookAsync();

var userState = new Dictionary<long, string>();
var dbContext = new ApplicationDbContext();
var repo = new ElectrocityRepos(dbContext);


botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();
Console.WriteLine($"@{me.Username} is running... Press Enter to terminate");
Console.ReadLine();
cts.Cancel(); 

// Handle updates from Telegram
async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not null)
    {
        await HandleMessageAsync(botClient, update.Message, cancellationToken);
    }
    else if (update.CallbackQuery is not null)
    {
        await HandleCallbackQueryAsync(botClient, update.CallbackQuery, cancellationToken);
    }
}

// Handle incoming messages
async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
{
    if (message.Text == "/start")
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Добавить", "add"),
            InlineKeyboardButton.WithCallbackData("Просмотр", "view")
        });

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Привет! Выбери действие:",
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken
        );
    }
    else if (userState.TryGetValue(message.Chat.Id, out var state) && state == "waiting_for_number")
    {
        if (Regex.IsMatch(message.Text, @"^\d{5}$") )
        {
            await repo.Add(message.Text);
            userState.Remove(message.Chat.Id, out _);
            
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Данные успешно записаны!",
                cancellationToken: cancellationToken
            );
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Введите данные корректно!",
                cancellationToken: cancellationToken
            );
        }
    }
}

// Handle callback queries from inline buttons
async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery,
    CancellationToken cancellationToken)
{
    await botClient.AnswerCallbackQueryAsync(
        callbackQuery.Id,
        text: $"Выполняется...",
        cancellationToken: cancellationToken
    );

    if (callbackQuery.Data == "add")
    {
        userState[callbackQuery.Message.Chat.Id] = "waiting_for_number";

        await botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            text: $"Введите показания за {DateTime.Today:d}: ",
            cancellationToken: cancellationToken
        );
        return;
    }

    if (callbackQuery.Data == "view")
    {
        var inlineKeyboardMarkupMonths = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Январь", "01"),
                InlineKeyboardButton.WithCallbackData("Февраль", "02"),
                InlineKeyboardButton.WithCallbackData("Март", "03")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Апрель", "04"),
                InlineKeyboardButton.WithCallbackData("Май", "05"),
                InlineKeyboardButton.WithCallbackData("Июнь", "06")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Июль", "07"),
                InlineKeyboardButton.WithCallbackData("Август", "08"),
                InlineKeyboardButton.WithCallbackData("Сентябрь", "09")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Октябрь", "10"),
                InlineKeyboardButton.WithCallbackData("Ноябрь", "11"),
                InlineKeyboardButton.WithCallbackData("Декабрь", "12")
            }
        });

        await botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            text: "Выберите месяц: ",
            replyMarkup: inlineKeyboardMarkupMonths,
            cancellationToken: cancellationToken
        );
        return;
    }
    
    var months = new Dictionary<int, string>()
    {
        {01, "Январь"},
        {02, "Февраль"},
        {03, "Март"},
        {04, "Апрель"},
        {05, "Май"},
        {06, "Июнь"},
        {07, "Июль"},
        {08, "Август"},
        {09, "Сентябрь"},
        {10, "Октябль"},
        {11, "Ноябрь"},
        {12, "Декабрь"}
    };
    
    if (months.ContainsKey(Convert.ToInt32(callbackQuery.Data)))
    {
        List<Electrocity> electrocities = await repo.Get(callbackQuery.Data);

            if (electrocities.Count > 0)
            { 
                string messageText = $"Таблица за {months
                    .Where(p=>p.Key == Convert.ToInt32(callbackQuery.Data))
                    .Select(p=>p.Value)
                    .FirstOrDefault()}";

            foreach (var item in electrocities)
                messageText +=
                    $"\n{item.Date.ToString("dd.MM.yyyy"):d}   |   " +
                    $"{(item.Indicate / 10).ToString("0.0")}   |   " +
                    $"{item.Difference/10}";

            await botClient.SendTextMessageAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                text: messageText,
                cancellationToken: cancellationToken
            );
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                text: "Данные не найдены.",
                cancellationToken: cancellationToken
            );
        }
    }
}

// Handle errors
async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var errorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(errorMessage);
}