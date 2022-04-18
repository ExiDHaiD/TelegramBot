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
    public class ObserverTradeLevel : TfBaseObserver
    {
        public struct Settings
        {
            public MarketInstrument Instrument;
            public decimal level;
        }
        Settings _settings;
        Guid _tickSubscribeId;
        TfAtomicQueue<TfTrade> tradeQ;

        public ObserverTradeLevel(TfBaseConnector connector, RuleTriggerHandler triggerHandler, Settings settings) : base(connector, triggerHandler)
        {
            _settings = settings;
        }
        public override void StartObserver()
        {
            if (_tickSubscribeId == Guid.Empty)
            {
                if (_connector.GetExchangeInstruments(false).Find(p => p.Ticker == _settings.Instrument.Ticker) == null)
                    throw new Exception("Тикер " + _settings.Instrument.Ticker + " не найден");

                _tickSubscribeId = _connector.SubscribeOnTradeStream(_settings.Instrument, TfOnNewTrade);
                tradeQ = new TfAtomicQueue<TfTrade>(TfOnNewTradeHandler);
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
                }
            }
        }

        TfTrade firstTrade = null;
        void TfOnNewTradeHandler(TfTrade trade)
        {
            if (firstTrade == null)
                firstTrade = trade;

            decimal upPrice = trade.Price >= firstTrade.Price ? trade.Price : firstTrade.Price;
            decimal downPrice = trade.Price <= firstTrade.Price ? trade.Price : firstTrade.Price;
            if ((_settings.level >= downPrice) &&
                (_settings.level <= upPrice))
            {
                ObserverMsg msg = new ObserverMsg();
                msg.ObserverId = observerId;
                msg.Type = TfObserverFactory.ObserverType.TradeLevel;
                msg.Msg = trade.Instrument.Ticker + ": достижение цены " + _settings.level;
                msg.DeleteObserver = true;
                _triggerHandler.BeginInvoke(msg, null, null);
                StopObserver();
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
