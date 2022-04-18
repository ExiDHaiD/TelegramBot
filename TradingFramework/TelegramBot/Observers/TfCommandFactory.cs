using System;
using System.Collections.Generic;
using TradingFramework.BaseConnector;
using TradingFramework.Observers;
using TradingFramework.Informers;
using TradingFramework.DataTypes;
using TradingFramework.TransaqConnector;
using TradingFramework.BinanceConnector;
using TradingFramework.Constnants;
using System.IO;
using System.ComponentModel;

namespace TradingFramework.ObserversFactory
{

    public class TfObserverFactory : IDisposable
    {
        public enum ConnectorType
        {
            [Description("Transaq")]
            Transaq,
            [Description("Binance")]
            Binance
        }
        public enum ObserverType
        {
            [Description("TenHourBorder")]
            TenHourBorder,
            [Description("TestObserver")]
            TestObserver,
            [Description("TradeLevel")]
            TradeLevel,
            [Description("TopLevel")]
            TopLevel,
            [Description("VolatilityBorder")]
            VolatilityBorder
        }
        public enum InformerType
        {
            [Description("Screenshot")]
            Screenshot
        }
        class Connector
        {
            public Connector(TfBaseConnector connector, ConnectorType connectorType)
            {
                ConnectorImpl = connector;
                Type = connectorType;
            }
            public TfBaseConnector ConnectorImpl { get; }
            public ConnectorType Type { get; }
        }

        List<Connector> connectors = new List<Connector>();

        class ObserverDescription
        {
            public ObserverType Type;
            public TfBaseObserver Observer;
        }
        List<ObserverDescription> currentObservers = new List<ObserverDescription>();

        ControlGuard controlGuard;

        public TfObserverFactory(System.Windows.Forms.Control control)
        {
            string transaqLogin = File.ReadAllText("C:\\login_observerFactory.txt");
#if DEBUG
            TfTransaqConnector tc = new TfTransaqConnector(TfConstnants.TransaqLoginTest, TfConstnants.TransaqPassword);
#else
            TfTransaqConnector tc = new TfTransaqConnector(transaqLogin, TfConstnants.TransaqPassword);
#endif
            connectors.Add(new Connector(tc, ConnectorType.Transaq));

            TfBinanceConnector bc = new TfBinanceConnector(TfConstnants.BinanceKey, TfConstnants.BinanceSecret);
            connectors.Add(new Connector(bc, ConnectorType.Binance));
            controlGuard = new ControlGuard(control);
        }

        public void Dispose()
        {
            while (currentObservers.Count != 0)
                    DeleteObserverById(currentObservers[0].Observer.observerId);

            foreach (Connector c in connectors)
                c.ConnectorImpl.Dispose();
        }
        public List<Guid> CreateObservers(ObserverType observer, ConnectorType connector, TfBaseObserver.RuleTriggerHandler triggerHandler, List<string> settings)
        {
            List<Guid> ret = new List<Guid>();
            switch (observer)
            {
                case ObserverType.TestObserver:
                    {
                        for (int i = 0; i < settings.Count; ++i)
                        {
                            ObserverTest.Settings s;
                            s.Instrument = new MarketInstrument(settings[i]);
                            var obId = SubscribeOnRule(connector, ObserverType.TestObserver, triggerHandler, s);
                            ret.Add(obId);
                        }
                    }
                    break;
                case ObserverType.TenHourBorder:
                    {
                        for (int i = 0; i < settings.Count; ++i)
                        {
                            ObserverTenHourBorder.Settings s;
                            s.Instrument = new MarketInstrument(settings[i]);
                            s.BorderTime = DateTime.Now.Date.AddHours(11).AddMinutes(20);
                            var obId = SubscribeOnRule(connector, ObserverType.TenHourBorder, triggerHandler, s);
                            ret.Add(obId);
                        }
                    }
                    break;
                case ObserverType.TradeLevel:
                    {
                        ObserverTradeLevel.Settings s;
                        s.Instrument = new MarketInstrument(settings[0]);
                        s.level = TfBaseConnector.ParseDecimal(settings[1]);
                        var obId = SubscribeOnRule(connector, ObserverType.TradeLevel, triggerHandler, s);
                        ret.Add(obId);
                    }
                    break;
                case ObserverType.TopLevel:
                    {
                        List<MarketInstrument> subs = new List<MarketInstrument>();
                        if (settings[0] == "all")
                        {
                            TfBaseConnector cc = null;
                            foreach (Connector c in connectors)
                            {
                                if (c.Type == connector)
                                {
                                    cc = c.ConnectorImpl;
                                    break;
                                }
                            }
                            subs = cc.GetExchangeInstruments(true);
                        }
                        else
                            subs.Add(new MarketInstrument(settings[0]));

                        foreach (var i in subs)
                        {
                            ObserverTopLevel.Settings s;
                            s.Instrument = i;
                            s.LType = settings[1] == "t" ? LevelTool.LevelType.TradesTotal : LevelTool.LevelType.OrderBook;
                            s.TopCount = settings.Count >= 3 ? int.Parse(settings[2]) : 10;
                            if (settings.Count >= 4)
                            {
                                if (settings[3] == "t") s.Period_m = 0;
                                else if (settings[3] == "e") s.Period_m = -1;
                                else
                                    s.Period_m = int.Parse(settings[3]);
                            }
                            else
                                s.Period_m = 30;
                            s.Scale = settings.Count == 5 ? int.Parse(settings[4]) : 10;

                            var obId = SubscribeOnRule(connector, ObserverType.TopLevel, triggerHandler, s);
                            ret.Add(obId);
                        }
                    }
                    break;
                case ObserverType.VolatilityBorder:
                    {
                        List<MarketInstrument> subs = new List<MarketInstrument>();
                        if (settings[0] == "all")
                        {
                            TfBaseConnector cc = null;
                            foreach (Connector c in connectors)
                            {
                                if (c.Type == connector)
                                {
                                    cc = c.ConnectorImpl;
                                    break;
                                }
                            }
                            subs = cc.GetExchangeInstruments(true);
                        }
                        else
                            subs.Add(new MarketInstrument(settings[0]));

                        foreach (var i in subs)
                        {
                            ObserverVolatility.Settings s;
                            s.Instrument = i;
                            s.Type = settings[1] == "d" ?
                                VolatilityTool.VolatilityType.Day :
                                settings[1] == "w" ? VolatilityTool.VolatilityType.Week :
                                VolatilityTool.VolatilityType.Month;
                            s.Period = settings.Count == 3 ? int.Parse(settings[2]) : 10;

                            var obId = SubscribeOnRule(connector, ObserverType.VolatilityBorder, triggerHandler, s);
                            ret.Add(obId);
                        }
                    }
                    break;
            }
            return ret;
        }
        public void CreateInformers(InformerType informer, ConnectorType connectorType, TfInformersScreen.InformerMsgHandler triggerHandler, List<string> settings, long userId)
        {
            TfBaseConnector connector = null;
            foreach (Connector c in connectors)
            {
                if (c.Type == connectorType)
                {
                    connector = c.ConnectorImpl;
                    break;
                }
            }
            if (connector == null)
                throw new Exception("Коннектор не поддерживается");

            switch (informer)
            {
                case InformerType.Screenshot:
                    {
                        TfInformersScreen.InformersScreenSettings iSettings = new TfInformersScreen.InformersScreenSettings();
                        iSettings.instrument = new MarketInstrument(settings[0]);
                        iSettings.UserId = userId;
                        for (int i = 1; i < settings.Count; ++i)
                        {
                            if (settings[i] == "o")
                                iSettings.lType = LevelTool.LevelType.OrderBook;
                            if (settings[i] == "t")
                                iSettings.lType = LevelTool.LevelType.TradesTotal;
                            if (settings[i] == "d")
                                iSettings.vType = VolatilityTool.VolatilityType.Day;
                            if (settings[i] == "w")
                                iSettings.vType = VolatilityTool.VolatilityType.Week;
                            if (settings[i] == "m")
                                iSettings.vType = VolatilityTool.VolatilityType.Month;
                            if (settings[i] == "lt")
                                iSettings.llType = LevelTool.LineLevelType.Today;
                            if (settings[i] == "le")
                                iSettings.llType = LevelTool.LineLevelType.Yesterday;
                            if (settings[i] == "lw")
                                iSettings.llType = LevelTool.LineLevelType.TwoWeek;
                            if (settings[i] == "lm")
                                iSettings.llType = LevelTool.LineLevelType.Month;
                        }

                        new TfInformersScreen(connector, controlGuard, triggerHandler, iSettings);
                    }
                    break;
            }
        }
        Guid SubscribeOnRule(ConnectorType connectorType, ObserverType observerType, TfBaseObserver.RuleTriggerHandler triggerHandler, object settings)
        {
            TfBaseConnector connector = null;
            foreach (Connector c in connectors)
            {
                if (c.Type == connectorType)
                {
                    connector = c.ConnectorImpl;
                    break;
                }
            }
            if (connector == null)
                throw new Exception("Коннектор не поддерживается");

            Guid ret = Guid.Empty;
            ObserverDescription addingObserver = new ObserverDescription();
            TfBaseObserver observer = null;
            switch (observerType)
            {
                case ObserverType.TenHourBorder:
                        observer = new ObserverTenHourBorder(connector, triggerHandler, (ObserverTenHourBorder.Settings)settings);
                        break;
                case ObserverType.TestObserver:
                        observer = new ObserverTest(connector, triggerHandler, (ObserverTest.Settings)settings);
                        break;
                case ObserverType.TradeLevel:
                        observer = new ObserverTradeLevel(connector, triggerHandler, (ObserverTradeLevel.Settings)settings);
                        break;
                case ObserverType.TopLevel:
                        observer = new ObserverTopLevel(connector, triggerHandler, (ObserverTopLevel.Settings)settings);
                        break;
                case ObserverType.VolatilityBorder:
                        observer = new ObserverVolatility(connector, triggerHandler, (ObserverVolatility.Settings)settings);
                        break;
            }
            if (observer != null)
            {
                addingObserver.Observer = observer;
                ret = observer.observerId;
                addingObserver.Type = observerType;
                lock (currentObservers)
                    currentObservers.Add(addingObserver);
            }
            return ret;
        }

        public void StartObserverById(Guid id)
        {
            var observer = currentObservers.Find(p => p.Observer.observerId == id);
            if (observer == null)
                throw new Exception("Наблюдель не найден");
            observer.Observer.StartObserver();
        }

        public void StopObserverById(Guid id)
        {
            var observer = currentObservers.Find(p => p.Observer.observerId == id);
            if (observer == null)
                throw new Exception("Наблюдель не найден");
            observer.Observer.StopObserver();
        }

        public void DeleteObserverById(Guid id)
        {
            var observer = currentObservers.Find(p => p.Observer.observerId == id);
            if (observer == null)
                throw new Exception("Наблюдель не найден");
            observer.Observer.StopObserver();
            lock (currentObservers)
                currentObservers.Remove(observer);
        }

        public ObserverType GetObserverTypeById(Guid id)
        {
            var observer = currentObservers.Find(p => p.Observer.observerId == id);
            if (observer == null)
                throw new Exception("Наблюдель не найден");
            return observer.Type;
        }

        public object GetSettingsById(Guid id)
        {
            var observer = currentObservers.Find(p => p.Observer.observerId == id);
            if (observer == null)
                throw new Exception("Наблюдель не найден");
            return observer.Observer.GetSettings();
        }

        static public List<ConnectorType> GetConnectorsList()                      
        {
            var ret = new List<ConnectorType>();
            ret.Add(ConnectorType.Transaq);
            ret.Add(ConnectorType.Binance);
            return ret;
        }

        static public List<ObserverType> GetObserversList()
        {
            var ret = new List<ObserverType>();
            //ret.Add(ObserverType.TenHourBorder);
            ret.Add(ObserverType.TestObserver);
            ret.Add(ObserverType.TradeLevel);
            ret.Add(ObserverType.TopLevel);
            ret.Add(ObserverType.VolatilityBorder);
            return ret;
        }

        static public List<InformerType> GetInformersList()
        {
            var ret = new List<InformerType>();
            ret.Add(InformerType.Screenshot);
            return ret;
        }
    }
}