using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CndBot.Core.Actions;
using CndBot.Core.Database;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace CndBot.Core
{
    public class Client
    {
        public const string SITE_URL = "http://google.com";
        
        public const long MAIN_CHAT_ID = tokens.tokens.MAIN_CHAT_ID;
        public const long SIGNING_CHAT_ID = tokens.tokens.SIGNING_CHAT_ID;

        private const string API_TOKEN = "2111288854:AAHqmPxK9CCz87hFlxh_odfbqJDYkltzIXU";

        private const long SIGN_CHAT_ID = -1;
        
        public static DataBaseProvider DataBaseProvider;

        private static ITelegramBotClient _client;

        private List<BotCommand> _botCommands = new List<BotCommand>();

        public void Init()
        {
            DataBaseProvider = new DataBaseProvider(new DbContextOptions<DataBaseProvider>());

            DataBaseProvider.SaveChanges();

            _client = new TelegramBotClient(API_TOKEN);

            _botCommands.AddRange(new []
            {
                new BotCommand
                {
                    Command = "start",
                    Description = "Розпочати роботу з ботом"
                },
                new BotCommand
                {
                    Command = "get_info",
                    Description = "Отримати необхідну інформацію"
                }
            });
            
            _client.SetMyCommandsAsync(_botCommands);

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }
            };

            _client.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );

            Console.ReadKey();
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            if (update.Message is {From: { }} message)
            {
                var userId = message.From.Id;

                //Command handler
                if (message.Text != null && message.Text.StartsWith("/"))
                {
                    var command = CommandFactory.GetCommand(message.Text);
                    
                    try
                    {
                        await Task.Run(() => command.ExecuteCommand(botClient, update), cancellationToken);
                    }
                    catch (ApiRequestException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    
                    return;
                }

                //Message logic handler
                //todo: Refactor
                switch (message.Text)
                {
                    //Register a new form request
                    case StartCommand.REGISTER_MSG:
                        var pair = RegisterFormAction.StagesById.FirstOrDefault(x => 
                            update.Message.From != null && x.Key.UserId == userId);

                        var form = pair.Key;
                        if (form == null)
                        {
                            form = new RegisterFormAction(botClient, message.From.Id);
                            RegisterFormAction.StagesById.Add(form, new FormDataModel());

                            RegisterFormAction.StagesById.TryGetValue(form, out var dataModel);
                                    
                            await form.InitForm(dataModel, update);
                        }
                        break;
                        
                    //Contact us request
                    case StartCommand.CONTACT_US_MSG:
                        await botClient.SendTextMessageAsync(message.Chat, 
                            "🕔 Графік роботи: 09:00 - 22:00 (пн-пт) \n \n" +
                            "☎ Контактні номери: +3806366636 | +3806366636 \n \n" +
                            "📨 Email-адреса: somemail@gmail.com", 
                            replyMarkup: new
                                InlineKeyboardMarkup(InlineKeyboardButton.WithUrl("Перейти на сайт 🌎", SITE_URL)),
                            cancellationToken: cancellationToken);
                        break;
                        
                    //Check events request
                    case StartCommand.CHECK_EVENTS_MSG:
                        await botClient.SendTextMessageAsync(message.Chat,
                            "🕔 Виступ дитячого гурту 'Сопілочка' [22/12/21]\n\n" +
                            "🕔 Набір дітей віком до 12 років в хор[15/12/21]\n\n" +
                            "🕔 Набір в танцювальний гурток [06/02/22]");
                    break;
                        
                    //Show on map request
                    case StartCommand.SHOW_ON_MAP_MSG:
                        await botClient.SendTextMessageAsync(message.Chat,
                            "Ми знаходимося за адресою: проспект Свободи, 28", 
                            cancellationToken: cancellationToken);
                            
                        await botClient.SendLocationAsync(message.Chat, 49.843641d, 24.026442d, 
                            cancellationToken: cancellationToken);
                        break;
                }

                if (RegisterFormAction.StagesById.Any(x => message.From 
                    != null && x.Key.UserId == userId) && message.Text != StartCommand.REGISTER_MSG)
                {
                    var key = RegisterFormAction.StagesById.FirstOrDefault(x => 
                        update.Message.From != null && x.Key.UserId == userId).Key;

                    await key.ProcessNextStage(RegisterFormAction.StagesById[key], update);
                }
            }
        }

        private static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
            CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(MAIN_CHAT_ID, exception.Message,
                cancellationToken: cancellationToken);
        }
    }
}