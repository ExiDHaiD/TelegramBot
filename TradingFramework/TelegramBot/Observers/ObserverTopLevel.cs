using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingFramework.ObserversFactory;
using TradingFramework.BaseConnector;
using TradingFramework.DataTypes;
using TradingFramework.CandlesBuilder;
using System.Threading;
using TradingFramework.AtomicQueue;

namespace TradingFramework.Observers
{
    public class ObserverTopLevel : TfBaseObserver
    {
        public struct Settings
        {
            public MarketInstrument Instrument;
            public int TopCount;
            public LevelTool.LevelType LType;
            public int Period_m;
            public int Scale;
        }

        class ObserverLevel
        {
            public decimal Level;
            public bool PauseTriggered;
        }
        Settings _settings;
        Guid _tickSubscribeId;
        TfAtomicQueue<TfTrade> tradeQ;
        Thread CheckTopLevelsTrade;
        TfTrade lastTrade = null;

        public ObserverTopLevel(TfBaseConnector connector, RuleTriggerHandler triggerHandler, Settings settings) : base(connector, triggerHandler)
        {
            _settings = settings;
        }
        public override void StartObserver()
        {
            if (_connector.GetExchangeInstruments(false).Find(p => p.Ticker == _settings.Instrument.Ticker) == null)
                throw new Exception("Тикер " + _settings.Instrument.Ticker + " не найден");

            if (_tickSubscribeId == Guid.Empty)
            {
                tradeQ = new TfAtomicQueue<TfTrade>(TfOnNewTradeHandler);
                _tickSubscribeId = _connector.SubscribeOnTradeStream(_settings.Instrument, TfOnNewTrade);

                CheckTopLevelsTrade = new Thread(UpdateTopLevelLoop);
                CheckTopLevelsTrade.Start();
            }
        }

        public override void StopObserver()
        {
            lock (_connector)
            {
                if (_tickSubscribeId != Guid.Empty)
                {
                    _connector.UnsubscribeOnTradeStream(_tickSubscribeId);
                    _tickSubscribeId = Guid.Empty;
                    tradeQ.Dispose();

                    if ((CheckTopLevelsTrade != null) && CheckTopLevelsTrade.IsAlive)
                        CheckTopLevelsTrade.Abort();
                }
            }
        }

        void TfOnNewTradeHandler(TfTrade trade)
        {
            if (lastTrade == null)
                lastTrade = trade;

            decimal upPrice = Math.Max(trade.Price, lastTrade.Price);
            decimal downPrice = Math.Min(trade.Price, lastTrade.Price);
            lock (observerLevels)
            {
                for (int i = 0; i < observerLevels.Count; ++i)
                {
                    if (!observerLevels[i].PauseTriggered &&
                        (observerLevels[i].Level >= downPrice) &&
                        (observerLevels[i].Level <= upPrice))
                    {
                        ObserverMsg msg = new ObserverMsg();
                        msg.ObserverId = observerId;
                        msg.Type = TfObserverFactory.ObserverType.TopLevel;
                        msg.Msg = trade.Instrument.Ticker + ": достижение цены топ " + (i + 1) + "(" + observerLevels.Count + ") - " + observerLevels[i].Level;
                        _triggerHandler.BeginInvoke(msg, null, null);
                        observerLevels[i].PauseTriggered = true;
                    }
                }
            }
            lastTrade = trade;
        }

        List<ObserverLevel> observerLevels = new List<ObserverLevel>();
        void UpdateTopLevelLoop()
        {
            DateTime oldTime = _connector.GetServerTime().AddMinutes(-10);
            while (true)
            {
                Thread.Sleep(1000);
                var time = _connector.GetServerTime();
                if (((time.Minute / 5) != (oldTime.Minute / 5)) && (lastTrade != null))
                {
                    oldTime = time;
                    var maxPrice = lastTrade.Price * (decimal)(1 + _settings.Scale / 100.0f);
                    var minPrice = lastTrade.Price / (decimal)(1 + _settings.Scale / 100.0f);
                    if (_settings.Scale == 0)
                        minPrice = maxPrice = 0;
                    List<LevelTool.Level> max;
                    try
                    {
                        DateTime from = time.AddMinutes(-_settings.Period_m);
                        DateTime to = time;
                        if (_settings.Period_m <= 0)
                        {
                            var days = _connector.GetCandles(_settings.Instrument, TfIntervals.D1, 2);
                            if (days.Count != 2)
                                continue;

                            if (_settings.Period_m == 0)
                            {
                                from = days[1].Time.Date;
                                to = days[1].Time.Date.AddDays(1);
                            }
                            else
                            {
                                from = days[0].Time.Date;
                                to = days[0].Time.Date.AddDays(1);
                            }
                        }
                        max = LevelTool.GetExtremumLevel(
                            _settings.Instrument,
                            _connector.LevelSource,
                            from,
                            to,
                            _settings.LType,
                            0.5M,
                            minPrice, maxPrice);
                    }
                    catch { continue; }

                    lock (observerLevels)
                    {
                        observerLevels.Clear();
                        for (int i = 0; (i < _settings.TopCount) && (i < max.Count); ++i)
                        {
                            ObserverLevel lvl = new ObserverLevel();
                            lvl.Level = max[i].level;
                            lvl.PauseTriggered = false;
                            observerLevels.Add(lvl);
                        }
                    }
                }
            }
        }

        void TfOnNewTrade(TfTrade trade)
        {
            tradeQ.Put(trade);
        }

        public override object GetSettings()
        {
            return _settings;
        }
    }
}
