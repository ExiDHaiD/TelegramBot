using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingFramework.ObserversFactory;
using TradingFramework.Observers;
using TradingFramework.BaseConnector;

namespace TradingFramework.TelegramBot
{
    class BotMessageHelper
    {
        static List<TfObserverFactory.ConnectorType> connectorsList = TfObserverFactory.GetConnectorsList();
        static List<TfObserverFactory.ObserverType> observersList = TfObserverFactory.GetObserversList();
        static List<TfObserverFactory.InformerType> informersList = TfObserverFactory.GetInformersList();
        static public string GetCommandDescription(string command)
        {
            switch (command)
            {
                case "/connectors":
                    {
                        string msg = connectorsList.Count == 0 ? "Нет доступных коннекторов" : "";
                        for (int i = 0; i < connectorsList.Count; ++i)
                            msg += connectorsList[i].ToString() + "(" + i + ")\n";
                        return msg;
                    }
                case "/observers":
                    {
                        string msg = observersList.Count == 0 ? "Нет доступных типов Наблюдаелей" : "";
                        for (int i = 0; i < observersList.Count; ++i)
                            msg += "/" + observersList[i].ToString() + "(" + i + ")\n";
                        return msg;
                    }
                case "/informers":
                    {
                        string msg = informersList.Count == 0 ? "Нет доступных типов Информаторов" : "";
                        for (int i = 0; i < informersList.Count; ++i)
                            msg += "/" + informersList[i].ToString() + "(" + (observersList.Count + i) + ")\n";
                        return msg;
                    }
                case "/delete":
                    {
                        string description = "Удаление указанного Наблюдателя.\n" +
                            "/delete <N1> <N2> <N3> <N5>-<N10>\n" +
                            "N - номера Наблюдателей, полученных через /myobservers\n";
                        return description;
                    }
                default: return GetCommandsDescription(command);
            }
        }
        static public string GetCommandsDescription(string command)
        {
            TfObserverFactory.ObserverType observerType;
            TfObserverFactory.InformerType informerType;
            try
            {
                observerType = GetObserverType(command);
                return GetObserverDescription(observerType);
            }
            catch
            {
                try
                {
                    informerType = GetInformerType(command);
                    return GetInformerDescription(informerType);
                }
                catch { }

                return GetHelpText();
            }
        }
        static string GetObserverDescription(TfObserverFactory.ObserverType observerType)
        {

            if (observerType == TfObserverFactory.ObserverType.TenHourBorder)
            {
                return "Наблюдатель пробития 10 часовой свечи\n" +
                    "/" + observerType +
                    " <connector> ticker1 ticker2 .... tickerN\n" +
                    "<connector> - коннектор из списка /connectors\n"
                    + "tickerN - тикеры бумаг для наблюдения";
            }
            if (observerType == TfObserverFactory.ObserverType.TestObserver)
            {
                return "Тестовый Наблюдатель\n" +
                    "/" + TfObserverFactory.ObserverType.TestObserver +
                    " <connector> ticker1 ticker2 .... tickerN\n" +
                    "<connector> - коннектор из списка /connectors\n"
                    + "tickerN - тикеры бумаг для наблюдения";
            }
            if (observerType == TfObserverFactory.ObserverType.TradeLevel)
            {
                return "Отслеживание отработки цены\n" +
                    "/" + observerType +
                    " <connector> ticker price\n" +
                    "<connector> - коннектор из списка /connectors\n" +
                    "ticker - тикер бумаги\n" +
                    "price - цена отслеживания\n";
            }
            if (observerType == TfObserverFactory.ObserverType.TopLevel)
            {
                return "Отслеживание отработки уровней\n" +
                    "/" + observerType +
                    " <connector> ticker <levelType> topN Period Scale \n" +
                    "<connector> - коннектор из списка /connectors\n" +
                    "ticker - тикер бумаги, либо \"all\" для подписки на все инструменты коннектора,\n" +
                    "<levelType> - t - уровни по сделкам, o - уровни по заявкам,\n" +
                    "topN - до какого топа отслеживать, 10 - значение по умолчанию,\n" +
                    "Period - за сколько последних минут брать уровни, 30 - значение по умолчанию,\n" +
                    "Вместо периода можно указать 't' - подписка топ уровней за сегодня, либо 'e' - топ за вчера.\n" +
                    "Scale - диапазон цены +- в % от последней сделки, 0 - нет ограничения по цене, 10 - значение по-умолчанию.\n" +
                    "Поля со значениями по умолчанию можно не указывать, но нельзя пропускать, если указываются следующие поля";
            }
            if (observerType == TfObserverFactory.ObserverType.VolatilityBorder)
            {
                return "Отслеживание отработки границ волатильности\n" +
                    "/" + observerType +
                    " <connector> ticker <volatilityType> Period\n" +
                    "<connector> - коннектор из списка /connectors\n" +
                    "ticker - тикер бумаги, либо \"all\" для подписки на все инструменты коннектора,\n" +
                    "<volatilityType> - Тип волатильности: d - дневная, w - недельная, m - месячная,\n" +
                    "Period - период, 10 - значение по-умолчанию\n" +
                    "Поля со значениями по умолчанию можно не указывать";
            }
            return GetHelpText();

        }

        static string GetInformerDescription(TfObserverFactory.InformerType informerType)
        {
            if (informerType == TfObserverFactory.InformerType.Screenshot)
            {
                return "Скриншот графика M5\n" +
                    "/" + informerType +
                    " <connector> ticker\n" +
                    "<connector> - коннектор из списка /connectors\n"
                    + "ticker - тикер бумаги\n" + 
                    "Доступны параметры в любом количестве и последовательности:\n" +
                    "o - спектр по заявкам, период 20\n" +
                    "t - спектр по сделкам, период 40\n" +
                    "d - дневная волатильность\n" +
                    "w - недельная волатильность\n" +
                    "m - месячная волатильность\n" +
                    "lt - топ 5 уровней за последний торговый день\n" +
                    "le - топ 5 уровней за предпоследний торговый день\n" +
                    "lw - топ 5 уровней за 2 недели\n" +
                    "lm - топ 5 уровней за месяц\n" +
                    "Нельзя одновременно использовать несколько параметров волатильности, несколько параметров спектра или несколько параметров уровней." +
                    "Для коннектора binance диапазон спектра +- 10%";

            }
            return GetHelpText();
        }
        static public string GetSettingsStr(object settings)
        {
            string ret = "";
            if (settings is ObserverTenHourBorder.Settings)
            {
                ObserverTenHourBorder.Settings s = (ObserverTenHourBorder.Settings)settings;
                ret +=
                    s.Instrument.Ticker + "\n" +
                    s.BorderTime.ToString();
            }

            if (settings is ObserverTest.Settings)
            {

                ObserverTest.Settings s = (ObserverTest.Settings)settings;
                ret +=
                    s.Instrument.Ticker + "\n";
            }

            if (settings is ObserverTradeLevel.Settings)
            {

                ObserverTradeLevel.Settings s = (ObserverTradeLevel.Settings)settings;
                ret +=
                    s.Instrument.Ticker + "\n" +
                    s.level + "\n";
            }

            if (settings is ObserverTopLevel.Settings)
            {
                ObserverTopLevel.Settings s = (ObserverTopLevel.Settings)settings;
                string periodStr = (s.Period_m > 0) ? s.Period_m.ToString() : (s.Period_m == 0) ? " топы за сегодня" : " топы за вчера";
                ret +=
                    "Тикер: " + s.Instrument.Ticker + "\n" +
                    "Тип уровней: " + s.LType + "\n" +
                    "Топ : " + s.TopCount + "\n" +
                    "Период: " + periodStr + "\n" +
                    "Диапазон: " + s.Scale + "\n";
            }

            if (settings is ObserverVolatility.Settings)
            {

                ObserverVolatility.Settings s = (ObserverVolatility.Settings)settings;
                ret +=
                    "Тикер: " + s.Instrument.Ticker + "\n" +
                    "Тип волатильности: " + s.Type + "\n" +
                    "Период: " + s.Period + "\n";
            }
            return ret;
        }

        static public TfObserverFactory.ObserverType GetObserverType(string observer)
        {
            string name = observer.Replace("/", "");
            try
            {
                uint index = uint.Parse(name);
                if (index < observersList.Count)
                    return observersList[(int)index];
            }
            catch
            {
                int i = observersList.FindIndex(p => p.ToString().ToLower() == name.ToLower());
                if (i != -1)
                    return observersList[i];
            }

            throw new Exception("Наблюдатель с таким именем не найден");
        }

        static public TfObserverFactory.InformerType GetInformerType(string informer)
        {
            string name = informer.Replace("/", "");
            try
            {
                int index = (int)uint.Parse(name) - observersList.Count;
                if ((index < informersList.Count) && (index >= 0))
                    return informersList[index];
            }
            catch
            {
                int i = informersList.FindIndex(p => p.ToString().ToLower() == name.ToLower());
                if (i != -1)
                    return informersList[i];
            }

            throw new Exception("Информатор с таким именем не найден");
        }

        static public TfObserverFactory.ConnectorType GetConnectorType(string connector)
        {
            string name = connector.Replace("/", "");
            try
            {
                uint index = uint.Parse(name);
                if (index < connectorsList.Count)
                    return connectorsList[(int)index];
            }
            catch
            {
                int i = connectorsList.FindIndex(p => p.ToString().ToLower() == name.ToLower());
                if (i != -1)
                    return connectorsList[i];
            }

            throw new Exception("Коннектор с таким именем не найден");
        }

        static public bool IsSingleCommand(string command)
        {
            if ((command == "/connectors") ||
                (command == "/observers") ||
                (command == "/myobservers"))
                return true;
            return false;
        }

        static public bool CheckCommandsArgs(string command, List<string> args)
        {
            if (command == "/delete")
            {
                foreach (string s in args)
                {
                    try
                    {
                        uint.Parse(s);
                    }
                    catch
                    {
                        var range = s.Split('-');
                        if(range.Length != 2)
                            return false;
                        try
                        {
                            uint.Parse(range[0]);
                            uint.Parse(range[1]);

                        }
                        catch
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            else
            {
                if (IsObserver(command))
                    return CheckObserverArgs(command, args);
                if (IsInformer(command))
                    return CheckInformerArgs(command, args);
            }
            return false;
        }

        static public bool IsObserver(string command)
        {
            try
            {
                GetObserverType(command);
                return true;
            }
            catch
            {
                return false;
            }
        }

        static public bool IsInformer(string command)
        {
            try
            {
                GetInformerType(command);
                return true;
            }
            catch
            {
                return false;
            }

        }

        static bool CheckObserverArgs(string command, List<string> args)
        {
            var observer = GetObserverType(command);
            var connector = GetConnectorType(args[0]);

            if (args.Count < 2)
                return false;

            List<string> settingsStrList = new List<string>(args);
            settingsStrList.RemoveAt(0);

            switch (observer)
            {
                case TfObserverFactory.ObserverType.TestObserver:
                case TfObserverFactory.ObserverType.TenHourBorder:
                    return true;
                case TfObserverFactory.ObserverType.TradeLevel:
                    {
                        if (settingsStrList.Count != 2)
                            return false;
                        try
                        {
                            TfBaseConnector.ParseDecimal(settingsStrList[1]);
                        }
                        catch
                        {
                            return false;
                        }
                        return true;
                    }
                case TfObserverFactory.ObserverType.TopLevel:
                    {
                        if (settingsStrList.Count < 2)
                            return false;
                        if ((settingsStrList[1].ToLower() != "t") && (settingsStrList[1].ToLower() != "o"))
                            return false;
                        try
                        {
                            if (settingsStrList.Count == 3)
                                int.Parse(settingsStrList[2]);
                            if (settingsStrList.Count == 4)
                            {
                                try
                                {
                                    int period = int.Parse(settingsStrList[3]);
                                    if (period <= 0)
                                        return false;
                                }
                                catch
                                {
                                    if ((settingsStrList[3] != "t") &&
                                      (settingsStrList[3] != "e"))
                                        return false;
                                }
                            }
                            if (settingsStrList.Count == 5)
                                int.Parse(settingsStrList[4]);
                        }
                        catch
                        {
                            return false;
                        }

                        return true;

                    }
                case TfObserverFactory.ObserverType.VolatilityBorder:
                    {
                        if (settingsStrList.Count < 2)
                            return false;

                        if ((settingsStrList[1].ToLower() != "d") &&
                            (settingsStrList[1].ToLower() != "w") &&
                            (settingsStrList[1].ToLower() != "m"))
                            return false;
                        if (settingsStrList.Count == 3)
                        {
                            try
                            {
                                if (settingsStrList.Count == 3)
                                {
                                    var p = int.Parse(settingsStrList[2]);
                                    return p >= 1;
                                }
                            }
                            catch
                            {
                                return false;
                            }
                            return true;
                        }
                        else
                            return true;
                    }
            }
            return false;
        }
        static bool CheckInformerArgs(string command, List<string> args)
        {
            var informer = GetInformerType(command);
            var connector = GetConnectorType(args[0]);

            if (args.Count < 2)
                return false;

            List<string> settingsStrList = new List<string>(args);
            settingsStrList.RemoveAt(0);

            switch (informer)
            {
                case TfObserverFactory.InformerType.Screenshot:
                    {
                        settingsStrList.RemoveAt(0);
                        int levelC = 0;
                        int volC = 0;
                        int lineC = 0;
                        foreach (string arg in settingsStrList)
                        {
                            if ((arg == "o") ||
                                (arg == "t"))
                                levelC++;
                            else if (
                                (arg == "d") ||
                                (arg == "w") ||
                                (arg == "m"))
                                volC++;
                            else if (
                                (arg == "lt") ||
                                (arg == "le") ||
                                (arg == "lw") ||
                                (arg == "lm"))
                                lineC++;
                            else
                                return false;
                        }
                        if ((levelC > 1) ||
                            (volC > 1) ||
                            (lineC > 1))
                            return false;
                    }
                    return true;
            }
            return false;
        }

        static public string GetHelpText()
        {
            return
                "Доступные команды:\n" +
                "/connectors - Список доступных коннекторов\n" +
                "/informers - Список доступных Информаторов\n" +
                "/observers - Список доступных Наблюдателей\n" +
                "/myobservers - Текущий список Наблюдателей\n" +
                "/delete - Отписаться от указанных Наблюдателей";
        }
    }
}