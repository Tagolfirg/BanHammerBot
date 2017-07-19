using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using Telegram.Bot.Types.ReplyMarkups;

namespace BanHammer
{
    class Keyboard
    {
        public InlineKeyboardMarkup key { get; set; }

        public void Add(string button1, string button2, string button3, string button4, string button5)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    new InlineKeyboardButton(button1, button1),
                    new InlineKeyboardButton(button2, button2),
                },
                new []
                {
                    new InlineKeyboardButton(button3, button3),
                    new InlineKeyboardButton(button4, button4),
                },
                  new []
                {
                    new InlineKeyboardButton(button5, button5),
                }
            });
            key = keyboard;
        }
        public void Add(string button1, string button2, string button3, string button4)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    new InlineKeyboardButton(button1, button1),
                    new InlineKeyboardButton(button2, button2),
                },
                new []
                {
                    new InlineKeyboardButton(button3, button3),
                    new InlineKeyboardButton(button4, button4),
                }
            });
            key = keyboard;
        }
        public void Add(string button1, string button2, string button3)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    new InlineKeyboardButton(button1, button1),
                    new InlineKeyboardButton(button2, button2),
                },
                new []
                {
                    new InlineKeyboardButton(button3, button3),
                }
            });
            key = keyboard;
        }
        public void Add(string button1, string button2)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
          {
                new []
                {
                    new InlineKeyboardButton(button1, button1),
                    new InlineKeyboardButton(button2, button2),
                },
            });
            key = keyboard;
        }
        public void Add(string button1)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
          {
                new []
                {
                    new InlineKeyboardButton(button1, button1),
                },
            });
            key = keyboard;
        }
    }
}
