﻿using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CndBot.Core
{
    public class GetInfoCommand : BaseCommand
    {
        private const string INFO_MSG = "Вітаємо в нашому чат-боті!";
        public override async Task ExecuteCommand(ITelegramBotClient botClient, Update update)
        {
            if (update.Message != null)
            {
                await botClient.SendTextMessageAsync(update.Message.Chat, INFO_MSG);
            }
        }
    }
}