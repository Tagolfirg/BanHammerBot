using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BanHammer
{
    class Program
    {
        public static readonly TelegramBotClient Bot = new TelegramBotClient("Enter your token here");

        static void Main(string[] args)
        {
            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnUpdate += BotOnUpdateRecieved;
            Bot.OnMessageEdited += BotOnMessageReceived;
            Bot.OnReceiveError += BotOnReceiveError;

            var me = Bot.GetMeAsync().Result;

            Bot.StartReceiving();
            Console.ReadLine();
            Bot.StopReceiving();
        }

        static List<long> kwdAddMode = new List<long>();
        static List<long> kwdRemoveMode = new List<long>();
        static List<long> gKwdAddMode = new List<long>();
        static List<long> gKwdRemoveMode = new List<long>();
        static List<long> wantedAddMode = new List<long>();
        static List<long> wantedRemoveMode = new List<long>();
        static List<long> editChatlistMode = new List<long>();

        static List<long> messages = new List<long>();
        static List<long> groups = new List<long>();
        static List<long> deck = new List<long>();
        static List<long> unban = new List<long>();

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            List<Admin> admins = DB.ReadAmdins();
            List<Keyword> keywords = DB.ReadKeywords();
            List<GKeyword> gKeywords = DB.ReadGKeywords();
            List<Wantedlist> wantedUsers = DB.ReadWantedList();
            List<MChat> chats = DB.ReadChats();


            var me = await Bot.GetMeAsync();
            var message = messageEventArgs.Message;
            var reply = message.ReplyToMessage;

            if (message == null) return;

            if (message.Text == "/chatlist")
            {
                string usage = "";
                try
                {
                    usage = System.IO.File.ReadAllText($@"Resources/chatlist");
                }
                catch (FileNotFoundException) { return; }

                if (string.IsNullOrWhiteSpace(usage))
                    return;

                await Bot.SendTextMessageAsync(message.Chat.Id, usage, ParseMode.Html, true);
            }

            if (message.Chat.Type == ChatType.Private)
            {
                if (message.From.Username == "<OwnerUsername>")
                {
                    if (message.Text == "/usage")
                    {
                        string helpmsg = $@"/newAdmin <i>@adminusername</i> - add new admin
/removeAdmin <i>@adminusername</i> - remove admin
/admins <i>Admins list</i>
/editchatlist"; 
                        await Bot.SendTextMessageAsync(message.Chat.Id, helpmsg, ParseMode.Html, true);
                    }
                    else if (message.Text.StartsWith("/newAdmin"))
                    {
                        string[] words = message.Text.Split(' ');
                        if (words.Length == 2)
                            DB.AdminAdd(words[1].TrimStart('@'));
                    }
                    else if (message.Text.StartsWith("/removeAdmin"))
                    {
                        string[] words = message.Text.Split(' ');
                        if (words.Length == 2)
                            DB.AdminsRemove(words[1].TrimStart('@'));
                    }
                    else if (message.Text == "/admins")
                    {
                        string result = "";
                        List<Admin> list = DB.ReadAmdins();
                        foreach (var admin in list)
                        {
                            result += $"{admin.Username}\n";
                        }
                        if (!string.IsNullOrWhiteSpace(result))
                            await Bot.SendTextMessageAsync(message.Chat.Id, result);
                    }
                }

                if (admins.Any(a => a.Username == message.From.Username))
                {
                    if (message.Text == "/help")
                    {
                        string usage = $@"Если ответить на сообщение командой /kill - отправитель считается спамером и будет наказан во всех известных боту чатах.
Так же банхаммер добавит в ключевые слова все ссылки которые были в сообщении.
Если ответить на сообщение командой /clean - бот удалит его.
/start - для вызова меню.
/chats - топ чатов.
reply + /mute - restrict user to readonly mode (avaliable for all administrators)";
                        await Bot.SendTextMessageAsync(message.Chat.Id, usage);
                    }
                    else if (message.Text == "/start")
                    {
                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                                new []
                                {
                                    new InlineKeyboardCallbackButton("Сообщения", "Сообщения"),
                                    new InlineKeyboardCallbackButton("Группы", "Группы"),
                                },
                                new []
                                {
                                    new InlineKeyboardCallbackButton("Доска почета", "Доска почета"),
                                    new InlineKeyboardCallbackButton("Освободить", "Освободить"),
                                }
                         });
                        await Bot.SendTextMessageAsync(message.Chat.Id, $"Настройки {me.Username}", replyMarkup: keyboard);
                    }
                    else if (message.Text == "/chats")
                    {
                        string report = "Chats, where members count > 300:\n";

                        int total = 0;

                        Dictionary<string, int> dict = new Dictionary<string, int>();

                        foreach (var chat in chats)
                        {
                            try
                            {
                                int members = await Bot.GetChatMembersCountAsync(chat.Id);
                                if (members > 300)
                                    dict.Add(chat.Username, members);
                                total += members;
                            }
                            catch (Telegram.Bot.Exceptions.ApiRequestException)
                            {
                                DB.ChatsRemove(chat.Id);
                            }
                        }

                        foreach (var pair in dict.OrderByDescending(pair => pair.Value))
                        {
                            report += $"@{pair.Key ?? "<Nousername>"} - {pair.Value}\n";
                        }

                        report += $"Total users: {total}";

                        await Bot.SendTextMessageAsync(message.Chat.Id, report); ;
                    }
                    else if (message.Text == "/editchatlist")
                    {
                        editChatlistMode.Add(message.Chat.Id);
                        await Bot.SendTextMessageAsync(message.Chat.Id, "Ну давай, отправь мне чатлист");
                    }
                }

                foreach (var id in kwdAddMode)
                {
                    if (!message.Text.StartsWith("/") && id == message.From.Id)
                    {
                        var keyboard = new InlineKeyboardMarkup(new[]
                       {
                                new []
                                {
                                    new InlineKeyboardCallbackButton("Закончить", "Закончить"),
                                }
                        });
                        if (keywords.Any(a => a.keyword == message.Text.ToLower()))
                            await Bot.SendTextMessageAsync(message.Chat.Id, "Такая фраза уже есть!\nРежим ввода ключевых слов для сообщений.\nВведите слово или фразу:", replyMarkup: keyboard);
                        else
                        {
                            DB.KeywordsAdd(message.Text.ToLower());
                            await Bot.SendTextMessageAsync(message.Chat.Id, "Добавлено.\nРежим ввода ключевых слов для сообщений.\nВведите слово или фразу:", replyMarkup: keyboard);
                        }
                    }
                }
                foreach (var id in kwdRemoveMode)
                {
                    if (!message.Text.StartsWith("/") && id == message.From.Id)
                    {
                        string usage = $"Режим удаления ключевых слов для сообщений\nВведите слово или фразу. Которое нужно удалить.";
                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                                new []
                                {
                                    new InlineKeyboardCallbackButton("Закончить", "Закончить"),
                                }
                        });
                        if (keywords.Any(a => a.keyword == message.Text.ToLower()))
                        {
                            DB.KeywordsRemove(message.Text.ToLower());
                            await Bot.SendTextMessageAsync(message.Chat.Id, $"Фраза удалена.\n{usage}", replyMarkup: keyboard);
                        }
                        else
                            await Bot.SendTextMessageAsync(message.Chat.Id, $"Нет такой фразы!\n{usage}", replyMarkup: keyboard);
                    }
                }

                foreach (var id in gKwdAddMode)
                {
                    if (!message.Text.StartsWith("/") && id == message.From.Id)
                    {
                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                                new []
                                {
                                    new InlineKeyboardCallbackButton("Закончить", "Закончить"),
                                }
                        });
                        if (gKeywords.Any(a => a.keyword == message.Text.ToLower()))
                            await Bot.SendTextMessageAsync(message.Chat.Id, "Такая фраза уже есть!\nРежим ввода ключевых слов для групп\nВведите название чата или канала:", replyMarkup: keyboard);
                        else
                        {
                            DB.GKeywordsAdd(message.Text.ToLower());
                            await Bot.SendTextMessageAsync(message.Chat.Id, "Добавлено.\nРежим ввода ключевых слов для групп\nВведите название чата или канала:", replyMarkup: keyboard);
                        }
                    }
                }
                foreach (var id in gKwdRemoveMode)
                {
                    if (!message.Text.StartsWith("/") && id == message.From.Id)
                    {
                        string usage = $"Режим удаления ключевых слов для групп\nВведите название группы. Которое нужно удалить.";
                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                                new []
                                {
                                    new InlineKeyboardCallbackButton("Закончить", "Закончить"),
                                }
                        });
                        if (gKeywords.Any(a => a.keyword == message.Text.ToLower()))
                        {
                            DB.GKeywordsRemove(message.Text.ToLower());
                            await Bot.SendTextMessageAsync(message.Chat.Id, $"Фраза удалена.\n{usage}", replyMarkup: keyboard);
                        }
                        else
                            await Bot.SendTextMessageAsync(message.Chat.Id, $"Нет такой фразы!\n{usage}", replyMarkup: keyboard);
                    }
                }

                foreach (var id in wantedAddMode)
                {
                    if (!message.Text.StartsWith("/") && id == message.From.Id)
                    {
                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                                new []
                                {
                                    new InlineKeyboardCallbackButton("Закончить", "Закончить"),
                                }
                        });
                        string usage = $"Режим ввода ориентировок\nВведите username:";
                        var usr = message.Text.TrimStart('@').ToLower();
                        if (wantedUsers.Any(a => a.Username == usr))
                            await Bot.SendTextMessageAsync(message.Chat.Id, $"Этот username уже есть на доске почета!\n{usage}", replyMarkup: keyboard);
                        else
                        {
                            DB.WantedListAdd(usr);
                            await Bot.SendTextMessageAsync(message.Chat.Id, $"Username добавлен.\n{usage}", replyMarkup: keyboard);
                        }
                    }
                }
                foreach (var id in wantedRemoveMode)
                {
                    if (!message.Text.StartsWith("/") && id == message.From.Id)
                    {
                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                                new []
                                {
                                    new InlineKeyboardCallbackButton("Закончить", "Закончить"),
                                }
                        });
                        string usage = "Введите username. Которое нужно удалить.";
                        var usr = message.Text.TrimStart('@').ToLower();
                        if (wantedUsers.Any(a => a.Username == usr))
                        {
                            DB.WantedListRemove(usr);
                            await Bot.SendTextMessageAsync(message.Chat.Id, $"Ориентировка удалена.\n{usage}", replyMarkup: keyboard);
                        }
                        else
                            await Bot.SendTextMessageAsync(message.Chat.Id, $"Я такого не разыскиваю.\n{usage}", replyMarkup: keyboard);
                    }
                }

                foreach (var id in unban)
                {
                    if (!message.Text.StartsWith("/") && id == message.From.Id)
                    {
                        List<Spamer> spamers = DB.ReadSpamers();
                        var usr = message.Text.TrimStart('@').ToLower();
                        if (spamers.Any(a => a.Username == usr))
                        {
                            DB.RemoveSpamer(usr);
                            await Bot.SendTextMessageAsync(message.Chat.Id, $"{usr} освобожден.");
                        }
                        else
                            await Bot.SendTextMessageAsync(message.Chat.Id, "А кто это?");
                    }
                }

                try
                {
                    foreach (var id in editChatlistMode)
                    {
                        if (message.Chat.Id == id && !message.Text.StartsWith("/"))
                        {
                            System.IO.File.WriteAllText($@"Resources/chatlist", message.Text);
                            await Bot.SendTextMessageAsync(message.Chat.Id, "Done!");
                            editChatlistMode.Remove(id);
                        }
                    }
                }
                catch (Exception) { }
            }
            //Work in supergroup
            if (message.Chat.Type == ChatType.Supergroup)
            {
                //Mute command
                var adms = await Bot.GetChatAdministratorsAsync(message.Chat.Id);
                bool isMeAdmin = adms.Any(a => a.User.Id.ToString() == me.Id.ToString());
                if (adms.Any(a => a.User.Id == message.From.Id) && isMeAdmin == true)
                {
                    if (reply != null && message.Text == "/mute")
                    {
                        DateTime date = new DateTime(2050, 5, 1);
                        await Bot.RestrictChatMemberAsync(message.Chat.Id, reply.From.Id, date, false, false, false, false);
                    }
                }

                //chat add
                if (!chats.Any(a => a.Id == message.Chat.Id))
                    DB.ChatsAdd(message.Chat);
                
                //kill & clean
                if (admins.Any(a => a.Username == message.From.Username) && reply != null)
                {
                    if (message.Text == "/kill")
                    {
                        if (reply.Type != MessageType.ServiceMessage)
                        {
                            string[] words = null;
                            if (reply.Text != null)
                                words = reply.Text.Split(' ');
                            else if (reply.Caption != null)
                                words = reply.Caption.Split(' ');

                            if (words != null)
                            {
                                foreach (var word in words)
                                {
                                    if (word.StartsWith("@"))
                                    {
                                        if (!keywords.Any(a => a.keyword == word.ToLower()))
                                            DB.KeywordsAdd(word.ToLower());
                                    }
                                }
                                foreach (var ent in reply.Entities)
                                {
                                    if (ent.Url != null && (ent.Url.StartsWith("http://") || ent.Url.StartsWith("https://")))
                                    {
                                        if (!keywords.Any(a => a.keyword == ent.Url.ToString().ToLower()))
                                            DB.KeywordsAdd(ent.Url.ToString().ToLower());
                                    }
                                }
                            }
                        }
                        try
                        {
                            await Bot.DeleteMessageAsync(message.Chat.Id, reply.MessageId);
                            await Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                        }
                        catch (Telegram.Bot.Exceptions.ApiRequestException) { }
                        DB.SpamersAdd(reply.From);
                    }
                    else if (message.Text == "/clean")
                    {
                        ChatMember[] adminArr = await Bot.GetChatAdministratorsAsync(message.Chat.Id);
                        if (adminArr.Any(a => a.User.Username == me.Username))
                        {
                            try
                            {
                                await Bot.DeleteMessageAsync(message.Chat.Id, reply.MessageId);
                                await Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                            }
                            catch (Telegram.Bot.Exceptions.ApiRequestException) { }
                        }
                    }
                }

                //Check messages
                if (message.Text != null && keywords.Any(a => message.Text.ToLower().Contains(a.keyword)) || message.Caption != null && keywords.Any(a => message.Caption.ToLower().Contains(a.keyword)))
                {
                    DB.SpamersAdd(message.From);
                    try
                    {
                        await Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                    }
                    catch (Telegram.Bot.Exceptions.ApiRequestException) { }
                }

                //Check forwards
                if (message.ForwardFromChat != null && gKeywords.Any(a => message.ForwardFromChat.Title.ToLower().Contains(a.keyword)))
                {
                    DB.SpamersAdd(message.From);
                    try
                    {
                        await Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                    }
                    catch (Telegram.Bot.Exceptions.ApiRequestException) { }
                }

                //Wantedlist check
                if (message.From.Username != null && wantedUsers.Any(a => a.Username == message.From.Username.ToLower()))
                {
                    DB.SpamersAdd(message.From);
                    try
                    {
                        await Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                    }
                    catch (Telegram.Bot.Exceptions.ApiRequestException) { }
                }

                List<Spamer> spamers = DB.ReadSpamers();

                //Butler
                if (message.NewChatMembers != null)
                {
                    foreach (var newUser in message.NewChatMembers)
                    {
                        if (spamers.Any(a => a.Id == newUser.Id) || newUser.Username != null && wantedUsers.Any(a => a.Username == newUser.Username.ToLower()))
                        {
                            ChatMember[] adminArr = await Bot.GetChatAdministratorsAsync(message.Chat.Id);
                            if (adminArr.Any(a => a.User.Id == me.Id))
                            {
                                await Bot.KickChatMemberAsync(message.Chat.Id, newUser.Id);
                                string file = $@"{Directory.GetCurrentDirectory()}/Resources/ButlerSticker.webp";
                                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                                {
                                    var fts = new FileToSend(file, fileStream);
                                    await Bot.SendStickerAsync(message.Chat.Id, fts, false, message.MessageId);
                                }
     
                            }
                            else
                            {
                                string msg;
                                Random rand = new Random();
                                int temp = rand.Next(adminArr.Length);
                                var otheradmin = adminArr[temp].User;

                                if (otheradmin.Username != null)
                                {
                                    if (newUser.Username != null)
                                        msg = $"@{otheradmin.Username}!Бан Упрыя!!!===>@{newUser.Username}<===";
                                    else
                                        msg = $"@{otheradmin.Username}!Бан упыря!!!===>{newUser.FirstName} {newUser.LastName}<===";
                                }
                                else
                                {
                                    if (newUser.Username != null)
                                        msg = $"{otheradmin.FirstName} {otheradmin.LastName}!Бан упыря!!! ===>@{newUser.Username}<===";
                                    else
                                        msg = $"{otheradmin.FirstName} {otheradmin.LastName}!Бан упыря!!!===>{newUser.FirstName} {newUser.LastName}<===";
                                }
                                await Bot.SendTextMessageAsync(message.Chat.Id, msg);
                            }
                        }
                    }
                }

                //spamers work
                if (spamers.Any(a => a.Id == message.From.Id))
                {
                    try
                    {
                        ChatMember[] adminArr = await Bot.GetChatAdministratorsAsync(message.Chat.Id);
                        if (adminArr.Any(a => a.User.Id == me.Id))
                        {
                            await Bot.KickChatMemberAsync(message.Chat.Id, message.From.Id);
                        }
                        else
                        {
                            string msg;
                            Random rand = new Random();
                            int temp = rand.Next(adminArr.Length);
                            var otheradmin = adminArr[temp].User;

                            if (otheradmin.Username != null)
                            {
                                if (message.From.Username != null)
                                    msg = $"@{otheradmin.Username}!Бан Упрыя!!!===>@{message.From.Username}<===";
                                else
                                    msg = $"@{otheradmin.Username}!Бан упыря!!!===>{message.From.FirstName} {message.From.LastName}<===";
                            }
                            else
                            {
                                if (message.From.Username != null)
                                    msg = $"{otheradmin.FirstName} {otheradmin.LastName}!Бан упыря!!! ===>@{message.From.Username}<===";
                                else
                                    msg = $"{otheradmin.FirstName} {otheradmin.LastName}!Бан упыря!!!===>{message.From.FirstName} {message.From.LastName}<===";
                            }

                            await Bot.SendTextMessageAsync(message.Chat.Id, msg);
                        }
                    }
                    catch(Telegram.Bot.Exceptions.ApiRequestException e) { }
                }
            }
        }
        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var me = await Bot.GetMeAsync();

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                    new []
                    {
                        new InlineKeyboardCallbackButton("Сообщения", "Сообщения"),
                        new InlineKeyboardCallbackButton("Группы", "Группы"),
                    },
                    new []
                    {
                        new InlineKeyboardCallbackButton("Доска почета", "Доска почета"),
                        new InlineKeyboardCallbackButton("Освободить", "Освободить"),
                    }
            });

            var callback = callbackQueryEventArgs.CallbackQuery;
            //Message from bot
            var message = callbackQueryEventArgs.CallbackQuery.Message;

            if (callback.Data == "Сообщения")
            {
                if (!messages.Any(a => a == message.Chat.Id))
                    messages.Add(message.Chat.Id);
                string usage = $"Настройка ключевых слов для сообщений.\nЕсли в тексте сообщения содержится фраза или слово из списка -пользователь будет наказан.";
                var k = new Keyboard();
                k.Add("Добавить", "Показать", "Удалить запись", "Очистить всё", "Назад");
                try
                {
                    await Bot.EditMessageTextAsync(message.Chat.Id, message.MessageId, usage, replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }
            else if (callback.Data == "Добавить" && messages.Any(a => a == message.Chat.Id))
            {
                var k = new Keyboard();
                k.Add("Закончить");
                if (!kwdAddMode.Contains(message.Chat.Id))
                    kwdAddMode.Add(message.Chat.Id);
                try
                {
                    await Bot.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Режим ввода ключевых слов для сообщений\nВведите слово или фразу:", replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }
            else if (callback.Data == "Закончить" && message.Text.Contains("Режим ввода ключевых слов для сообщений"))
            {
                string usage = $"Настройка ключевых слов для сообщений.\nЕсли в тексте сообщения содержится фраза или слово из списка - пользователь будет наказан.";

                kwdAddMode.Remove(message.Chat.Id);
                var k = new Keyboard();
                k.Add("Добавить", "Показать", "Удалить запись", "Очистить всё", "Назад");
                try
                {
                    await Bot.EditMessageTextAsync(message.Chat.Id, message.MessageId, usage, replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }
            else if (callback.Data == "Показать" && messages.Any(a => a == message.Chat.Id))
            {
                List<Keyword> keywords = DB.ReadKeywords();
                string readfile = "";
                foreach (var a in keywords)
                {
                    readfile += $"{a.keyword}\n";
                }
                if (string.IsNullOrWhiteSpace(readfile))
                    readfile = "Cписок пуст.";
                string usage = $"Настройка ключевых слов для сообщений.\nЕсли в тексте сообщения содержится фраза или слово из списка - пользователь будет наказан.\n`{readfile}`";
                var k = new Keyboard();
                k.Add("Добавить", "Показать", "Удалить запись", "Очистить всё", "Назад");
                try
                {
                    await Bot.EditMessageTextAsync(message.Chat.Id, message.MessageId, usage, ParseMode.Markdown, replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }
            else if (callback.Data == "Удалить запись" && messages.Any(a => a== message.Chat.Id))
            {
                List<Keyword> keywords = DB.ReadKeywords();
                string readfile = "";
                foreach (var a in keywords)
                {
                    readfile += $"{a.keyword}\n";
                }
                if (string.IsNullOrWhiteSpace(readfile))
                    readfile = "Cписок пуст";
                string usage = $"Режим удаления ключевых слов для сообщений.\nВведите слово или фразу. Которое нужно удалить.\n`{readfile}`";
                var k = new Keyboard();
                k.Add("Закончить");
                if (!kwdRemoveMode.Contains(message.Chat.Id))
                    kwdRemoveMode.Add(message.Chat.Id);
                try
                {
                    await Bot.EditMessageTextAsync(message.Chat.Id, message.MessageId, usage, ParseMode.Markdown, replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }
            else if (callback.Data == "Закончить" && message.Text.Contains("Введите слово или фразу."))
            {
                kwdRemoveMode.Remove(message.Chat.Id);
                string usage = $"Настройка ключевых слов для сообщений.\nЕсли в тексте сообщения содержится фраза или слово из списка - пользователь будет наказан.";
                var k = new Keyboard();
                k.Add("Добавить", "Показать", "Удалить запись", "Очистить всё", "Назад");
                try
                {
                    await Bot.EditMessageTextAsync(message.Chat.Id, message.MessageId, usage, replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }
            else if (callback.Data == "Очистить всё" && messages.Any(a => a == message.Chat.Id))
            {
                DB.KeywordsClear();
                string readfile = "Список пуст.";
                string usage = $"Настройка ключевых слов для сообщений.\nЕсли в тексте сообщения содержится фраза или слово из списка - пользователь будет наказан.\n`{readfile}`";
                var k = new Keyboard();
                k.Add("Добавить", "Показать", "Удалить запись", "Очистить всё", "Назад");
                try
                {
                    await Bot.EditMessageTextAsync(message.Chat.Id, message.MessageId, usage, ParseMode.Markdown, replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }

            else if (callback.Data == "Группы")
            {
                if (!groups.Any(a => a == message.Chat.Id))
                    groups.Add(message.Chat.Id);
                string usage = $"Настройка ключевых слов для групп.\nЕсли сообщение переслано из канала или чата из списка - пользователь будет наказан.";
                var k = new Keyboard();
                k.Add("Добавить", "Показать", "Удалить запись", "Очистить всё", "Назад");
                try
                {
                    await Bot.EditMessageTextAsync(callback.Message.Chat.Id, callback.Message.MessageId, usage, replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }
            else if (callback.Data == "Добавить" && groups.Any(a => a == message.Chat.Id))
            {
                var k = new Keyboard();
                k.Add("Закончить");
                if (!gKwdAddMode.Contains(message.Chat.Id))
                    gKwdAddMode.Add(message.Chat.Id);
                try
                {
                    await Bot.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Режим ввода ключевых слов для групп\nВведите название чата или канала:", replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }

            }
            else if (callback.Data == "Закончить" && message.Text.Contains("Режим ввода ключевых слов для групп"))
            {
                string usage = $"Настройка ключевых слов для групп.\nЕсли сообщение переслано из канала или чата из списка - пользователь будет наказан.";
                gKwdAddMode.Remove(message.Chat.Id);
                var k = new Keyboard();
                k.Add("Добавить", "Показать", "Удалить запись", "Очистить всё", "Назад");
                try
                {
                    await Bot.EditMessageTextAsync(callback.Message.Chat.Id, callback.Message.MessageId, usage, replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }
            else if (callback.Data == "Показать" && groups.Any(a => a == message.Chat.Id))
            {
                List<GKeyword> keywords = DB.ReadGKeywords();
                string readfile = "";
                foreach (var a in keywords)
                {
                    readfile += $"{a.keyword}\n";
                }
                if (string.IsNullOrWhiteSpace(readfile))
                    readfile = "Cписок пуст.";
                string usage = $"Настройка ключевых слов для групп.\nЕсли сообщение переслано из канала или чата из списка - пользователь будет наказан.\n`{readfile}`";
                var k = new Keyboard();
                k.Add("Добавить", "Показать", "Удалить запись", "Очистить всё", "Назад");
                try
                {
                    await Bot.EditMessageTextAsync(message.Chat.Id, message.MessageId, usage, ParseMode.Markdown, replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }
            else if (callback.Data == "Удалить запись" && groups.Any(a => a == message.Chat.Id))
            {
                List<GKeyword> gKeywords = DB.ReadGKeywords();
                string readfile = "";
                foreach (var a in gKeywords)
                {
                    readfile += $"{a.keyword}\n";
                }
                if (string.IsNullOrWhiteSpace(readfile))
                    readfile = "Cписок пуст.";
                string usage = $"Режим удаления ключевых слов для групп\nВведите название группы. Которое нужно удалить\n`{readfile}`";
                var k = new Keyboard();
                k.Add("Закончить");
                if (!gKwdRemoveMode.Contains(message.Chat.Id))
                    gKwdRemoveMode.Add(message.Chat.Id);
                try
                {
                    await Bot.EditMessageTextAsync(message.Chat.Id, message.MessageId, usage, ParseMode.Markdown, replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }

            }
            else if (callback.Data == "Закончить" && message.Text.Contains("Введите название группы."))
            {
                gKwdRemoveMode.Remove(message.Chat.Id);
                string usage = $"Настройка ключевых слов для групп.\nЕсли сообщение переслано из канала или чата из списка - пользователь будет наказан.";
                var k = new Keyboard();
                k.Add("Добавить", "Показать", "Удалить запись", "Очистить всё", "Назад");
                try
                {
                    await Bot.EditMessageTextAsync(message.Chat.Id, message.MessageId, usage, replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }
            else if (callback.Data == "Очистить всё" && groups.Any(a => a == message.Chat.Id))
            {
                DB.GKeywordsClear();
                string readfile = "Cписок пуст.";
                string usage = $"Настройка ключевых слов для групп.\nЕсли сообщение переслано из канала или чата из списка - пользователь будет наказан.\n`{readfile}`";
                var k = new Keyboard();
                k.Add("Добавить", "Показать", "Удалить запись", "Очистить всё", "Назад");
                try
                {
                    await Bot.EditMessageTextAsync(message.Chat.Id, message.MessageId, usage, ParseMode.Markdown, replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }

            else if (callback.Data == "Доска почета")
            {
                if (!deck.Any(a => a == message.Chat.Id))
                    deck.Add(message.Chat.Id);
                string usage = $"Доска почета.\nЕсли юзернейм нового участника группы будет совпадать с доской почета - участник будет наказан.Так же банхаммер сверит с ней всех участников всех чатов.";
                var k = new Keyboard();
                k.Add("Добавить", "Показать", "Удалить запись", "Очистить всё", "Назад");
                try
                {
                    await Bot.EditMessageTextAsync(callback.Message.Chat.Id, callback.Message.MessageId, usage, replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }
            else if (callback.Data == "Добавить" && deck.Any(a => a == message.Chat.Id))
            {
                var k = new Keyboard();
                k.Add("Закончить");
                if (!wantedAddMode.Contains(message.Chat.Id))
                    wantedAddMode.Add(message.Chat.Id);
                try
                {
                    await Bot.EditMessageTextAsync(callback.Message.Chat.Id, callback.Message.MessageId, "Режим ввода ориентировок\nВведите username:", replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException e) { }
            }
            else if (callback.Data == "Закончить" && message.Text.Contains("Режим ввода ориентировок"))
            {
                wantedAddMode.Remove(message.Chat.Id);
                string usage = $"Доска почета.\nЕсли юзернейм нового участника группы будет совпадать с доской почета - участник будет наказан.Так же банхаммер сверит с ней всех участников всех чатов.";
                var k = new Keyboard();
                k.Add("Добавить", "Показать", "Удалить запись", "Очистить всё", "Назад");
                try
                {
                    await Bot.EditMessageTextAsync(callback.Message.Chat.Id, callback.Message.MessageId, usage, replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }
            else if (callback.Data == "Показать" && deck.Any(a => a == message.Chat.Id))
            {
                List<Wantedlist> wantedusers = DB.ReadWantedList();
                string readfile = "";
                foreach (var a in wantedusers)
                {
                    readfile += $"{a.Username}\n";
                }
                if (string.IsNullOrWhiteSpace(readfile))
                    readfile = "Cписок пуст.";
                string usage = $"Доска почета.\nЕсли юзернейм нового участника группы будет совпадать с доской почета - участник будет наказан.Так же банхаммер сверит с ней всех участников всех чатов.\n`{readfile}`";
                var k = new Keyboard();
                k.Add("Добавить", "Показать", "Удалить запись", "Очистить всё", "Назад");
                try
                {
                    await Bot.EditMessageTextAsync(callback.Message.Chat.Id, callback.Message.MessageId, usage, ParseMode.Markdown, replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }
            else if (callback.Data == "Удалить запись" && deck.Any(a => a == message.Chat.Id))
            {
                List<Wantedlist> wanted = DB.ReadWantedList();
                string readfile = "";
                foreach (var a in wanted)
                {
                    readfile += $"{a.Username}\n";
                }
                if (string.IsNullOrWhiteSpace(readfile))
                    readfile = "Cписок пуст.";
                string usage = $"Введите username. Которое нужно удалить.\n`{readfile}`";
                var k = new Keyboard();
                k.Add("Закончить");
                if (!wantedRemoveMode.Contains(message.Chat.Id))
                    wantedRemoveMode.Add(message.Chat.Id);
                try
                {
                    await Bot.EditMessageTextAsync(message.Chat.Id, message.MessageId, usage, ParseMode.Markdown, replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }

            }
            else if (callback.Data == "Закончить" && message.Text.Contains("Введите username."))
            {
                wantedRemoveMode.Remove(message.Chat.Id);
                string usage = $"Доска почета.\nЕсли юзернейм нового участника группы будет совпадать с доской почета - участник будет наказан.Так же банхаммер сверит с ней всех участников всех чатов.";
                var k = new Keyboard();
                k.Add("Добавить", "Показать", "Удалить запись", "Очистить всё", "Назад");
                try
                {
                    await Bot.EditMessageTextAsync(callback.Message.Chat.Id, callback.Message.MessageId, usage, replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }
            else if (callback.Data == "Очистить всё" && deck.Any(a => a == message.Chat.Id))
            {
                DB.WantedListClear();
                string readfile = "Cписок пуст.";
                string usage = $"Доска почета.\nЕсли юзернейм нового участника группы будет совпадать с доской почета - участник будет наказан.Так же банхаммер сверит с ней всех участников всех чатов.\n`{readfile}`";
                var k = new Keyboard();
                k.Add("Добавить", "Показать", "Удалить запись", "Очистить всё", "Назад");
                try
                {
                    await Bot.EditMessageTextAsync(callback.Message.Chat.Id, callback.Message.MessageId, usage, ParseMode.Markdown, replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }

            else if (callback.Data == "Освободить")
            {
                if (!unban.Any(a => a == message.Chat.Id))
                    unban.Add(message.Chat.Id);
                var k = new Keyboard();
                k.Add("Закончить");
                try
                {
                    await Bot.EditMessageTextAsync(callback.Message.Chat.Id, callback.Message.MessageId, "Введите username который нужно освободить:", replyMarkup: k.key);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }
            else if (callback.Data == "Закончить" && unban.Any(a => a == message.Chat.Id))
            {
                unban.Remove(message.Chat.Id);
                try
                {
                    await Bot.EditMessageTextAsync(callback.Message.Chat.Id, callback.Message.MessageId, $"Настройки {me.Username}", replyMarkup: keyboard);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }

            else if (callback.Data == "Назад")
            {
                if (messages.Any(a => a == message.Chat.Id)) messages.Remove(message.Chat.Id);
                if (groups.Any(a => a == message.Chat.Id)) groups.Remove(message.Chat.Id);
                if (deck.Any(a => a == message.Chat.Id)) deck.Remove(message.Chat.Id);
                if (unban.Any(a => a == message.Chat.Id)) unban.Remove(message.Chat.Id);

                try
                {
                    await Bot.EditMessageTextAsync(message.Chat.Id, message.MessageId, $"Настроки {me.Username}", replyMarkup: keyboard);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException) { }
            }
        }
        private static async void BotOnUpdateRecieved(object sender, UpdateEventArgs updateEventArgs)
        {
            var post = updateEventArgs.Update.ChannelPost;
            var me = Bot.GetMeAsync();

            if (post == null) return;

            string usage = $"Recieved {post.Chat.Title}";

            await Bot.SendTextMessageAsync(post.Chat.Id, usage);
        }
        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            var error = receiveErrorEventArgs.ApiRequestException;
            Console.WriteLine(error.Message);
        }
    }
}