/*-------------------------------------------------------------
 * Базовый класс, описывающий коннектор к торговой платформе
-------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using TradingFramework.DataTypes;
using System.Data;
using TradingFramework.SqlBaseDescription;

namespace TradingFramework.BaseConnector
{
    // Абстрактный класс, описывающий набор функций, которые должна поддерживать торговая система
    abstract public class TfBaseConnector : IDisposable
    {
        class TradeStreamSubscriber
        {
            public Guid subscribeId;
            public MarketInstrument instrument;
            public TfOnNewTrade callback;
        }
        class CandleStreamSubscriber
        {
            public Guid subscribeId;
            public MarketInstrument instrument;
            public TfIntervals Interval;
            public TfOnNewCandle callback;
        }

        List<TradeStreamSubscriber> tradeSubscribes = new List<TradeStreamSubscriber>();
        List<CandleStreamSubscriber> candlesSubscribes = new List<CandleStreamSubscriber>();
        public LevelTool.LevelSource LevelSource = LevelTool.LevelSource.None;

        public virtual DateTime GetServerTime()
        {
            return DateTime.Now;
        }

        public virtual List<MarketInstrument> GetAllInstruments()
        {
            return new List<MarketInstrument>();
        }

        // Функция получения актуального стакана заявок по инструменту
        abstract public TfOrderbook GetOrderBook(MarketInstrument instrument);

        // Подписка на изменение стакана заявок
        abstract public void SubscribeOrderBook(MarketInstrument instrument, TfOnOrderBook callback);

        // Подписка на получение новой свечи по инструменту
        public Guid SubscribeOnCandlesStream(MarketInstrument instrument, TfIntervals interval, TfOnNewCandle callback)
        {
            var newSubscribe = new CandleStreamSubscriber();
            newSubscribe.callback = callback;
            newSubscribe.subscribeId = Guid.NewGuid();
            newSubscribe.instrument = instrument;
            newSubscribe.Interval = interval;

            candlesSubscribes.Add(newSubscribe);

            if (candlesSubscribes.FindAll(p => (p.instrument == instrument) && (p.Interval == interval)).Count == 1)
            {
                try
                {
                    SubscribeOnCandleStreamImpl(instrument, interval);
                }
                catch (Exception e)
                {
                    candlesSubscribes.Remove(newSubscribe);
                    throw e;
                }

            }
            return newSubscribe.subscribeId;
        }

        // Подписка на таблицу обезличенных сделок по инструменту
        public Guid SubscribeOnTradeStream(MarketInstrument instrument, TfOnNewTrade callback)
        {
            var newSubscribe = new TradeStreamSubscriber();
            newSubscribe.callback = callback;
            newSubscribe.subscribeId = Guid.NewGuid();
            newSubscribe.instrument = instrument;

            lock (tradeSubscribes)
                tradeSubscribes.Add(newSubscribe);

            if (tradeSubscribes.FindAll(p => p.instrument == instrument).Count == 1)
            {
                try
                {
                    SubscribeOnTradeStreamImpl(instrument);
                }
                catch (Exception e)
                {
                    lock (tradeSubscribes)
                        tradeSubscribes.Remove(newSubscribe);
                    throw e;
                }

            }
            return newSubscribe.subscribeId;
        }

        protected void BaseOnNewTrade(TfTrade trade)
        {
            for (int i = 0; i < tradeSubscribes.Count; ++i)
            {
                if (tradeSubscribes[i] == null)
                {
                    System.IO.File.AppendAllText("C:\\errors.txt", "tradeSubscribes[i] == null");
                    continue;
                }
                if (trade.Instrument == tradeSubscribes[i].instrument)
                    tradeSubscribes[i].callback.BeginInvoke(trade, null, null);
            }
        }
        protected void BaseOnNewCandle(TfCandle candle)
        {
            for (int i = 0; i < candlesSubscribes.Count; ++i)
            {
                if ((candle.Instrument == candlesSubscribes[i].instrument) &&
                    (candle.Interval == candlesSubscribes[i].Interval))
                    candlesSubscribes[i].callback.BeginInvoke(candle, null, null);
            }
        }

        // Отписка от получения таблицы обезличенных сделок по инструменту
        public void UnsubscribeOnTradeStream(Guid id)
        {
            var subscriber = tradeSubscribes.Find(p => p.subscribeId == id);
            if (subscriber == null)
                throw new Exception("По заданному guid подписчик не найден");

            var similarInstrumentSubscriber = tradeSubscribes.FindAll(p => p.instrument == subscriber.instrument);
            if(similarInstrumentSubscriber == null)
                throw new Exception("Непредвиденная ошибка отписки от потока сделок");

            if (similarInstrumentSubscriber.Count == 1)
                UnsubscribeOnTradeStreamImpl(subscriber.instrument);

            lock (tradeSubscribes)
                tradeSubscribes.Remove(subscriber);
        }
        public void UnsubscribeOnCandlesStream(Guid id)
        {
            var subscriber = candlesSubscribes.Find(p => p.subscribeId == id);
            if (subscriber == null)
                throw new Exception("По заданному guid подписчик не найден");

            var similarInstrumentSubscriber = candlesSubscribes.FindAll(p => (p.instrument == subscriber.instrument) && (p.Interval == subscriber.Interval));
            if (similarInstrumentSubscriber == null)
                throw new Exception("Непредвиденная ошибка отписки от потока сделок");

            if (similarInstrumentSubscriber.Count == 1)
                UnsubscribeOnCandleStreamImpl(subscriber.instrument, subscriber.Interval);

            candlesSubscribes.Remove(subscriber);
        }

        // Отписка от получения таблицы обезличенных сделок для всех подписок
        public void UnsubscribeOnAllTrade()
        {
            while (tradeSubscribes.Count != 0)
                UnsubscribeOnTradeStream(tradeSubscribes[0].subscribeId);
        }
        public void UnsubscribeOnAllCandles()
        {
            while (candlesSubscribes.Count != 0)
                UnsubscribeOnTradeStream(candlesSubscribes[0].subscribeId);
        }

        abstract protected void UnsubscribeOnTradeStreamImpl(MarketInstrument instrument);
        abstract protected void SubscribeOnTradeStreamImpl(MarketInstrument instrument);

        abstract protected void UnsubscribeOnCandleStreamImpl(MarketInstrument instrument, TfIntervals interval);
        abstract protected void SubscribeOnCandleStreamImpl(MarketInstrument instrument, TfIntervals interval);

        // Выставление заявки в торговую систему
        abstract public void CreateOrder(TfOrder order);

        // Функции получения исторических свечей по указанным параметрам для инструмента
        abstract public List<TfCandle> GetCandles(MarketInstrument instrument, TfIntervals interval, int count);
        abstract public List<TfCandle> GetCandles(MarketInstrument instrument, TfIntervals interval, DateTime start, int count);
        abstract public List<TfCandle> GetCandles(MarketInstrument instrument, TfIntervals interval, DateTime start, DateTime end);

        abstract public void Dispose();

        public static decimal ParseDecimal(string str)
        {
            foreach (char c in str)
                if (!(char.IsDigit(c) || c == '.' || c == ','))
                    throw new Exception("неверный формат строки для decimal");

            if((str.Split('.').Length > 2) || 
               (str.Split(',').Length > 2))
                throw new Exception("неверный формат строки для decimal");

            str = str.Replace(",", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            str = str.Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

            if (str[0] == System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0])
                str = "0" + str;

            return decimal.Parse(str);
        }

        public virtual List<MarketInstrument> GetExchangeInstruments(bool shortList)
        {
            TableTinkoffOrderBook ttob = new TableTinkoffOrderBook();
            var insts = ttob.GetInstruments((new SqlDbConnection()).Connection, shortList);

            List<MarketInstrument> ret = new List<MarketInstrument>();
            foreach (DataRow r in insts.Rows)
                ret.Add(new MarketInstrument(r[0].ToString().Trim()));
            return ret;
        }
    }
}