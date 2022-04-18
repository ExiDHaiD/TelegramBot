using System;
using System.Collections.Generic;
using TradingFramework.ObserversFactory;
using TradingFramework.BaseConnector;
using TradingFramework.DataTypes;
using TradingFramework.VolatilityTool;
using TradingFramework.AtomicQueue;
using TradingFramework.CandlesBuilder;
using System.Threading;

namespace TradingFramework.Observers
{
    public class ObserverVolatility : TfBaseObserver
    {
        TfAtomicQueue<TfTrade> tradeQ;
        Guid tradeSubs = Guid.Empty;
        TfTrade lastTrade = null;
        DateTime currentDay;
        int currentWeek;
        int currentMonth;
        Volatility startVolatility = null;
        decimal currentLow = 0;
        decimal currentHigh = 0;
        decimal open = 0;

        bool restart = false;

        DateTime lastUpTime = DateTime.Now.AddMinutes(-30);
        DateTime lastDownTime = DateTime.Now.AddMinutes(-30);

        public struct Settings
        {
            public MarketInstrument Instrument;
            public VolatilityType Type;
            public int Period;
        }
        Settings _settings;

        public ObserverVolatility(TfBaseConnector connector, RuleTriggerHandler triggerHandler, Settings settings) : base(connector, triggerHandler)
        {
            _settings = settings;
        }
        void TradeQHandler(TfTrade trade)
        {
            tradeQ.Put(trade);
        }

        void TradeQProcess(TfTrade trade)
        {
            if (lastTrade == null) lastTrade = new TfTrade(trade);

            if ((startVolatility == null) || 
                ((_settings.Type == VolatilityType.Day) &&
                (_connector.GetServerTime().Date != startVolatility.dt)) ||
                ((_settings.Type == VolatilityType.Week) &&
                (Volatility.GetWeekNumber(_connector.GetServerTime().Date) != startVolatility.weekNumber)) ||
                ((_settings.Type == VolatilityType.Month) &&
                (Volatility.GetMonthNumber(_connector.GetServerTime().Date) != startVolatility.monthNumber)))
            {
                try
                {
                    Thread.Sleep(10000);
                    UpdateStartVolatility();
                }
                catch (Exception e)
                {
                    ObserverMsg msg = new ObserverMsg();
                    msg.Type = TfObserverFactory.ObserverType.VolatilityBorder;
                    msg.ObserverId = observerId;
                    msg.Msg = "deleted ObserverVolatility " + _settings.Instrument.Ticker + ": " + e.Message;
                    msg.DeleteObserver = true;
                    _triggerHandler.BeginInvoke(msg, null, null);
                    return;
                }
            }

            if (trade.Price > currentHigh) currentHigh = trade.Price;
            if (trade.Price < currentLow) currentLow = trade.Price;

            var downVLine = open - startVolatility.Value + (currentHigh - open);
            var upVLine = open + startVolatility.Value - (open - currentLow);

            var max = Math.Max(lastTrade.Price, trade.Price);
            var min = Math.Min(lastTrade.Price, trade.Price);
            lastTrade = trade;

            if ((downVLine >= min) && (downVLine <= max))
            {
                if ((DateTime.Now - lastDownTime).TotalMinutes >= 30)
                {
                    lastDownTime = DateTime.Now;
                    ObserverMsg msg = new ObserverMsg();
                    msg.Type = TfObserverFactory.ObserverType.VolatilityBorder;
                    msg.ObserverId = observerId;
                    msg.Msg = _settings.Instrument.Ticker + ": нижняя граница волатильности: " + _settings.Type;
                    /*if (lastTrade.Price > trade.Price)
                        msg.Msg += "⬊";
                    else
                        msg.Msg += "⮍";*/
                    _triggerHandler.BeginInvoke(msg, null, null);
                }
            }
            if ((upVLine >= min) && (upVLine <= max))
            {
                if ((DateTime.Now - lastUpTime).TotalMinutes >= 30)
                {
                    lastUpTime = DateTime.Now;
                    ObserverMsg msg = new ObserverMsg();
                    msg.Type = TfObserverFactory.ObserverType.VolatilityBorder;
                    msg.ObserverId = observerId;
                    msg.Msg = _settings.Instrument.Ticker + ": верхняя граница волатильности: " + _settings.Type;
                    /*if (lastTrade.Price > trade.Price)
                        msg.Msg += "⬊";
                    else
                        msg.Msg += "⮍";*/
                    _triggerHandler.BeginInvoke(msg, null, null);
                }
            }
        }
        public override void StartObserver()
        {
            try
            {
                if (tradeSubs == Guid.Empty)
                {
                    if (_connector.GetExchangeInstruments(false).Find(p => p.Ticker == _settings.Instrument.Ticker) == null)
                        throw new Exception("Тикер " + _settings.Instrument.Ticker + " не найден");
                }

                UpdateStartVolatility();

                tradeQ = new TfAtomicQueue<TfTrade>(TradeQProcess);
                tradeSubs = _connector.SubscribeOnTradeStream(_settings.Instrument, TradeQHandler);
            }
            catch(Exception e)
            {
                if (restart)
                {
                    ObserverMsg msg = new ObserverMsg();
                    msg.Type = TfObserverFactory.ObserverType.VolatilityBorder;
                    msg.ObserverId = observerId;
                    msg.Msg = "deleted ObserverVolatility " + _settings.Instrument.Ticker + ": " + e.Message;
                    msg.DeleteObserver = true;
                    _triggerHandler.BeginInvoke(msg, null, null);
                    return;
                }
                else
                    throw e;
            }
            restart = true;
        }

        void UpdateStartVolatility()
        {

            currentDay = _connector.GetServerTime().Date;
            currentWeek = Volatility.GetWeekNumber(currentDay);
            currentMonth = Volatility.GetMonthNumber(currentDay);
            var vs = TfVolatilityTool.GetCandlesVolatility(_connector, _settings.Instrument, _settings.Type, _settings.Period);

            startVolatility = null;
            TfIntervals interval = TfIntervals.D1;
            if (_settings.Type == VolatilityType.Day)
            {
                startVolatility = vs.Find(p => p.dt.Date == currentDay);
                interval = TfIntervals.D1;
            }
            if (_settings.Type == VolatilityType.Week)
            {
                startVolatility = vs.Find(p => p.weekNumber == currentWeek);
                interval = TfIntervals.W1;
            }
            if (_settings.Type == VolatilityType.Month)
            {
                startVolatility = vs.Find(p => p.monthNumber == currentMonth);
                interval = TfIntervals.MN;
            }
            if (startVolatility == null)
                return;

            var lastCandle = _connector.GetCandles(_settings.Instrument, interval, 1);
            if ((startVolatility != null) && (lastCandle.Count != 0))
            {
                if (((_settings.Type == VolatilityType.Day) &&
                    (lastCandle[0].Time.Date == startVolatility.dt)) ||
                    ((_settings.Type == VolatilityType.Week) &&
                    (Volatility.GetWeekNumber(lastCandle[0].Time) == startVolatility.weekNumber)) ||
                    ((_settings.Type == VolatilityType.Month) &&
                    (Volatility.GetMonthNumber(lastCandle[0].Time.Date) == startVolatility.monthNumber)))
                {
                    open = lastCandle[0].Open;
                    currentLow = lastCandle[0].Low;
                    currentHigh = lastCandle[0].High;
                }
            }
        }

        public override void StopObserver()
        {
            if (tradeQ != null)
                tradeQ.Dispose();
        }

        public override object GetSettings()
        {
            return _settings;
        }
    }
}
