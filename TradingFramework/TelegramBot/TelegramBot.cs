using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using TradingFramework.Constnants;
using TradingFramework.ObserversFactory;
using TradingFramework.Observers;
using TradingFramework.AtomicQueue;
using TradingFramework.Informers;
using System.IO;
namespace TradingFramework.TelegramBot
{
    public class TfTelegramBot : IDisposable
    {
        class Subscribe
        {
            public long UserId;
            public List<Guid> observers;
        }

        List<Subscribe> UsersSubscribes = new List<Subscribe>();
        public ITelegramBotClient Bot;
        TfObserverFactory observerFactory;
        DateTime startBotTime;
        class BotMsg
        {
            public ChatId ChatId;
            public string Msg = "";
            public System.Drawing.Bitmap Pic = null;
        }
        TfAtomicQueue<BotMsg> msgQ;
        TfAtomicQueue<TfBaseObserver.ObserverMsg> observerMsgQ;
        TfAtomicQueue<TfInformersScreen.InformerMsg> InformerMsgQ;
        TfAtomicQueue<MessageEventArgs> botMsgQ;
        public TfTelegramBot(System.Windows.Forms.Control control)
        {
            msgQ = new TfAtomicQueue<BotMsg>(msgQHandler);
            observerMsgQ = new TfAtomicQueue<TfBaseObserver.ObserverMsg>(RuleTriggerHandlerProcess);
            InformerMsgQ = new TfAtomicQueue<TfInformersScreen.InformerMsg>(InformerMsgProcess);
            botMsgQ = new TfAtomicQueue<MessageEventArgs>(Bot_OnMessageProcess);

            observerFactory = new TfObserverFactory(control);
            startBotTime = DateTime.Now;
            Bot = new TelegramBotClient(TfConstnants.TelegramBotToken);
            Bot.OnMessage += Bot_OnMessage;
            Bot.StartReceiving();
        }

        public void Dispose()
        {
            observerFactory.Dispose();

            msgQ.Dispose();
            observerMsgQ.Dispose();
            InformerMsgQ.Dispose();
            botMsgQ.Dispose();
        }
        void msgQHandler(BotMsg msg)
        {
            int id = 754581453; // мой id
            Message m;
            Task<Message> t;
            try
            {
                if (msg.Msg != "")
                {
                    t = Bot.SendTextMessageAsync(msg.ChatId, msg.Msg);
                    m = t.Result;
                }
                if (msg.Pic != null)
                {
                    MemoryStream memoryStream = new MemoryStream();
                    msg.Pic.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    Telegram.Bot.Types.InputFiles.InputOnlineFile f = new Telegram.Bot.Types.InputFiles.InputOnlineFile(memoryStream, "screenshot.png");
                    //t = Bot.SendPhotoAsync(msg.ChatId, f);
                    t = Bot.SendDocumentAsync(msg.ChatId, f);
                    m = t.Result;
                }
            }
            catch (Exception e)
            {
                msgQ.Put(msg);
                msg.ChatId = id;
                msg.Msg = "Error: " + e.InnerException.Message;
                msgQ.Put(msg);
            }
        }
        public void SendMessage(ChatId chatId, string message)
        {
            lock (msgQ)
            {
                BotMsg msg = new BotMsg();
                msg.ChatId = chatId;
                msg.Msg = message;
                msgQ.Put(msg);
            }
        }
        public void SendMessage(ChatId chatId, System.Drawing.Bitmap pic)
        {
            lock (msgQ)
            {
                BotMsg msg = new BotMsg();
                msg.ChatId = chatId;
                msg.Pic = pic;
                msgQ.Put(msg);
            }
        }

        private void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            botMsgQ.Put(e);
        }
        void Bot_OnMessageProcess(MessageEventArgs e)
        {
            e.Message.Date = e.Message.Date.AddHours(3);

            if (e.Message.Date < startBotTime)
            {
                SendMessage(e.Message.Chat.Id, "'" + e.Message.Text + "'" + " - Сообщение проигнорировано, т.к. было послано до запуска бота.");
                return;
            }

            string command;
            string[] commandArgs;
            try
            {
                while (e.Message.Text.Contains("\n"))
                    e.Message.Text = e.Message.Text.Replace("\n", " ");
                while (e.Message.Text.Contains("  "))
                    e.Message.Text = e.Message.Text.Replace("  ", " ");
                e.Message.Text = e.Message.Text.Trim();
                commandArgs = e.Message.Text.ToLower().Split(' ');
                command = commandArgs[0];
            }
            catch
            {
                SendMessage(e.Message.Chat.Id, BotMessageHelper.GetHelpText());
                return;
            }

            if (command[0] != '/')
            {
                SendMessage(e.Message.Chat.Id, BotMessageHelper.GetHelpText());
                return;
            }

            // Обработка команд без аргументов
            if (BotMessageHelper.IsSingleCommand(command) || (commandArgs.Length == 1))
            {
                // Команда "/myobservers" подразумевает работу со списком Наблюдателей, поэтому ее обработка происходит в этом классе
                // Все остальные команды без аргументов должны возвращать текст описания команды
                if (command == "/myobservers")
                {
                    ProcessMyObserversCommand(e.Message.Chat.Id);
                    return;
                }
                SendMessage(e.Message.Chat.Id, BotMessageHelper.GetCommandDescription(command));
                return;
            }

            var observerArgs = commandArgs.ToList();
            observerArgs.RemoveAt(0);
            try
            {
                if (!BotMessageHelper.CheckCommandsArgs(command, observerArgs))
                {
                    SendMessage(e.Message.Chat.Id, "Неверный формат команды");
                    return;
                }

                if (command == "/delete")
                {
                    ProcessDeleteCommand(e.Message.Chat.Id, commandArgs);
                    return;
                }

                if (BotMessageHelper.IsObserver(command))
                {
                    var observerType = BotMessageHelper.GetObserverType(command);
                    var connectorType = BotMessageHelper.GetConnectorType(observerArgs[0]);
                    observerArgs.RemoveAt(0);
                    CreateUserObserver(observerType, connectorType, observerArgs, e.Message.Chat.Id);
                    SendMessage(e.Message.Chat.Id, "Наблюдатель создан");
                }
                if (BotMessageHelper.IsInformer(command))
                {
                    var informerType = BotMessageHelper.GetInformerType(command);
                    var connectorType = BotMessageHelper.GetConnectorType(observerArgs[0]);
                    observerArgs.RemoveAt(0);
                    observerFactory.CreateInformers(informerType, connectorType, InformerMsgHandler, observerArgs, e.Message.Chat.Id);
                    SendMessage(e.Message.Chat.Id, "Команда принята");
                }
                return;
            }
            catch (Exception ex)
            {
                SendMessage(e.Message.Chat.Id, ex.Message);
            }

        }
        void ProcessMyObserversCommand(long userId)
        {
            var user = UsersSubscribes.Find(p => p.UserId == userId);
            if ((user == null) || (user.observers.Count == 0))
            {
                SendMessage(userId, "Нет активных Наблюдателей");
                return;
            }
            for (int i = 0; i < user.observers.Count; ++i)
            {
                var id = user.observers[i];
                var observerType = observerFactory.GetObserverTypeById(id);
                object settings = observerFactory.GetSettingsById(id);
                string observerSeettingsStr = observerType.ToString() + "(" + i.ToString() + ")\n";
                observerSeettingsStr += BotMessageHelper.GetSettingsStr(settings);
                SendMessage(userId, observerSeettingsStr);
            }
        }
        void ProcessDeleteCommand(long userId, string[] args)
        {

            var user = UsersSubscribes.Find(p => p.UserId == userId);
            if ((user == null) || (user.observers.Count == 0))
            {
                SendMessage(userId, "Нет активных Наблюдателей");
                return;
            }

            List<Guid> deletingId = new List<Guid>();
            List<int> deletingIdN = new List<int>();
            for (int i = 1; i < args.Length; ++i)
            {
                int first, last;
                try
                {
                    first = int.Parse(args[i]);
                    last = first;
                }
                catch
                {
                    var range = args[i].Split('-');
                    first = int.Parse(range[0]);
                    last = int.Parse(range[1]);
                }
                for (int n = first; n <= last; ++n)
                {
                    if (last >= user.observers.Count)
                    {
                        SendMessage(userId, "Неверный индекс Наблюдателя '" + args[i] + "'");
                        return;
                    }
                    if (deletingIdN.FindIndex(p => p == n) != -1)
                    {
                        SendMessage(userId, "Дублирование удаляемых индексов недопустимо. Ошибка в параметре '" + args[i] + "'");
                        return;
                    }
                    deletingId.Add(user.observers[n]);
                    deletingIdN.Add(n);
                }
            }
            for (int i = 0; i < deletingIdN.Count; ++i)
            {

                observerFactory.DeleteObserverById(deletingId[i]);
                user.observers.Remove(deletingId[i]);
                SendMessage(userId, "Наблюдатель (" + deletingIdN[i].ToString() + ") удален");
            }
        }
        void CreateUserObserver(TfObserverFactory.ObserverType observer, TfObserverFactory.ConnectorType connector, List<string> args, long userId)
        {
            Subscribe user = UsersSubscribes.Find(p => p.UserId == userId);
            if (user == null)
            {
                user = new Subscribe();
                user.UserId = userId;
                user.observers = new List<Guid>();

                lock (UsersSubscribes)
                    UsersSubscribes.Add(user);
            }
            var addingObservers = observerFactory.CreateObservers(observer, connector, RuleTriggerHandler, args);
            foreach (Guid o in addingObservers)
                observerFactory.StartObserverById(o);
            user.observers.AddRange(addingObservers);
        }

        void RuleTriggerHandler(TfBaseObserver.ObserverMsg message)
        {
            observerMsgQ.Put(message);
        }

        void RuleTriggerHandlerProcess(TfBaseObserver.ObserverMsg message)
        {
            lock (UsersSubscribes)
            {
                foreach (Subscribe s in UsersSubscribes)
                {
                    var ob = s.observers.Find(p => p == message.ObserverId);
                    if (ob != Guid.Empty)
                    {
                        if (message.Msg != "")
                            SendMessage(s.UserId, DateTime.Now.ToString("yy.MM.dd HH:mm") + " " + message.Msg);
                        if (message.Pic != null)
                            SendMessage(s.UserId, message.Pic);
                        try
                        {
                            if (message.DeleteObserver)
                            {
                                observerFactory.DeleteObserverById(ob);
                                s.observers.Remove(ob);
                            }
                        }
                        catch (Exception e)
                        {
                            SendMessage(s.UserId, DateTime.Now.ToString("yy.MM.dd HH:mm") + ". Ошибка удаления: " + e.Message);
                        }
                        break;
                    }
                }
            }
        }

        void InformerMsgHandler(TfInformersScreen.InformerMsg message)
        {
            InformerMsgQ.Put(message);
        }

        void InformerMsgProcess(TfInformersScreen.InformerMsg message)
        {
            if (message.Msg != "")
                SendMessage(message.UserId, DateTime.Now.ToString("yy.MM.dd HH:mm") + " " + message.Msg);
            if (message.Pic != null)
                SendMessage(message.UserId, message.Pic);
        }
    }
}