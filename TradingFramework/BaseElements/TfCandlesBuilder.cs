using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingFramework.DataTypes;
using TradingFramework.BaseConnector;

namespace TradingFramework.CandlesBuilder
{
    public struct FsFieldsStruct
    {
        public char splitter;
        public char timeSplitter;
        public int tradeNoIndex;
        public int dateIndex;
        public int timeIndex;
        public int priceIndex;
        public int volumeIndex;
        public int tickerIndex;
        public int boardIndex;
        public int bidTypeIndex;
        public string buyBidString;
        public string sellBidString;
    }

    public class TfCandlesBuilder
    {
        // Построение свечей в новый таймфрейм
        static public List<TfCandle> BuildCandles(List<TfCandle> candles, TfIntervals newInterval)
        {
            if (candles.Count == 0)
                return new List<TfCandle>();

            if (!CheckConvertAvailable(candles[0].Interval, newInterval))
                throw new Exception("Неподдерживаемый интервал для конвертации");

            var ret = new List<TfCandle>();
            TfCandle addingC = new TfCandle(candles[0]);
            addingC.Time = GetIntervalTime(newInterval, candles[0].Time);
            addingC.Interval = newInterval;
            for(int i = 0; i < candles.Count; ++i)
            {
                var c = candles[i];
                if (GetIntervalTime(newInterval, c.Time) !=
                   GetIntervalTime(newInterval, addingC.Time) ||
                   (i == candles.Count - 1))
                {
                    ret.Add(addingC);
                    addingC = new TfCandle(c);
                    addingC.Time = GetIntervalTime(newInterval, c.Time);
                    addingC.Interval = newInterval;
                }
                else
                {
                    addingC.Volume += c.Volume;
                    if (addingC.High < c.High) addingC.High = c.High;
                    if (addingC.Low > c.Low) addingC.Low = c.Low;
                    addingC.Close = c.Close;
                }
            }

            return ret;
        }

        static bool CheckConvertAvailable(TfIntervals oldI, TfIntervals newI)
        {
            bool supportedTm =
                (oldI == TfIntervals.TICK) ||
                (oldI == TfIntervals.M1) ||
                (oldI == TfIntervals.M5) ||
                (oldI == TfIntervals.M15) ||
                (oldI == TfIntervals.M30) ||
                (oldI == TfIntervals.H1) ||
                (oldI == TfIntervals.D1) ||
                (oldI == TfIntervals.W1) ||
                (oldI == TfIntervals.MN) ||
                (oldI == TfIntervals.YR);
            if (supportedTm && (oldI < newI))
                return true;

            return false;
        }

        // Построение свечей из построчного журнала сделок 
        public static List<TfCandle> BuildCandles(TfIntervals interval, List<string> trades)
        {
            FsFieldsStruct fieldsId = GetTradeFieldIndexes(trades[0]);
            trades.RemoveAt(0);
            var ret = new List<TfCandle>();
            DateTime buildingCandleTime = DateTime.Now.Date;
            List<TfTrade> buildingCandleTrades = new List<TfTrade>();

            foreach (string t in trades)
            {
                TfTrade trade = BuildTrade(fieldsId, t);
                DateTime candleTime = GetIntervalTime(interval, trade.Time);
                if (candleTime != buildingCandleTime)
                {
                    if (buildingCandleTrades.Count != 0)
                        ret.Add(BuildCandle(interval, buildingCandleTrades));

                    buildingCandleTime = candleTime;
                    buildingCandleTrades.Clear();
                }

                buildingCandleTrades.Add(trade);
            }

            ret.Add(BuildCandle(interval, buildingCandleTrades));

            return ret;
        }

        // Возвращает время свечи для указанного интервала для переданного времени
        public static DateTime GetIntervalTime(TfIntervals interval, DateTime time)
        {
            if (interval == TfIntervals.TICK)
                return time;

            DateTime ret = time.Date;

            if (interval == TfIntervals.D1)
                return ret;

            if (interval == TfIntervals.W1)
            {
                while (ret.DayOfWeek != DayOfWeek.Monday)
                    ret = ret.AddDays(-1);
                return ret;
            }

            if (interval == TfIntervals.MN)
                return new DateTime(time.Year, time.Month, 1);

            if (interval == TfIntervals.YR)
                return new DateTime(time.Year, 1, 1);
               


            int intervalMin = TfInterval.Interval2Minutes(interval);
            while (true)
            {
                if ((time >= ret) && (time < ret.AddMinutes(intervalMin)))
                    return ret;
                ret = ret.AddMinutes(intervalMin);
            }
        }

        // инициализация индексов структуры FsFieldsStruct по переданному заголовку
        public static FsFieldsStruct GetTradeFieldIndexes(string header)
        {
            FsFieldsStruct ret;
            ret.splitter = ';';
            ret.timeSplitter = ':';

            if (header == "TRADEDATE;TRADETIME;SECID;BOARDID;PRICE;VOLCUR;INVCURVOL;BUYSELL;TRADENO")
            {
                //currency
                ret.dateIndex = 0;
                ret.timeIndex = 1;
                ret.tickerIndex = 2;
                ret.boardIndex = 3;
                ret.priceIndex = 4;
                ret.volumeIndex = 5;
                ret.bidTypeIndex = 7;
                ret.tradeNoIndex = 8;
                ret.buyBidString = "B";
                ret.sellBidString = "S";
            }
            else if (header == "SESSIONDATE;TRADENO;TRADEDATE;TRADETIME;SECID;BOARDID;PRICE;QUANTITY;OFFMARKETDEAL;IQSDEAL;RFSDEAL")
            {
                //futures
                ret.tradeNoIndex = 1;
                ret.dateIndex = 2;
                ret.timeIndex = 3;
                ret.tickerIndex = 4;
                ret.boardIndex = 5;
                ret.priceIndex = 6;
                ret.volumeIndex = 7;
                ret.bidTypeIndex = 0;
                ret.buyBidString = "";
                ret.sellBidString = "";
            }
            else if (header == "TRADENO;TRADEDATE;TRADETIME;SECID;BOARDID;PRICE;QUANTITY;VALUE;TYPE;BUYSELL;TRADINGSESSION")
            {
                //stock
                ret.tradeNoIndex = 0;
                ret.dateIndex = 1;
                ret.timeIndex = 2;
                ret.tickerIndex = 3;
                ret.boardIndex = 4;
                ret.priceIndex = 5;
                ret.volumeIndex = 6;
                ret.bidTypeIndex = 9;
                ret.buyBidString = "B";
                ret.sellBidString = "S";
            }
            else
                throw new Exception("Неизвестный формат истории сделок");

            return ret;
        }

        // Построение сделки по строке сделки
        static TfTrade BuildTrade(FsFieldsStruct fieldsId, string trade)
        {
            var fields = trade.Split(fieldsId.splitter);
            var tradeDate = DateTime.Parse(fields[fieldsId.dateIndex]);
            var tradeTime = DateTime.Parse(fields[fieldsId.timeIndex]);
            DateTime tradeDateTime = new DateTime(
                tradeDate.Year, tradeDate.Month, tradeDate.Day,
                tradeTime.Hour, tradeTime.Minute, tradeTime.Second);

            TfTrade ret = new TfTrade(
                new MarketInstrument(fields[fieldsId.tickerIndex]),
                tradeDateTime,
                int.Parse(fields[fieldsId.volumeIndex]),
                TfBaseConnector.ParseDecimal(fields[fieldsId.priceIndex]),
                (fields[fieldsId.bidTypeIndex] == 
                fieldsId.buyBidString) ? TfBidType.Buy : 
                (fields[fieldsId.bidTypeIndex] == fieldsId.sellBidString) ? 
                TfBidType.Sell : TfBidType.NoDir);

            return ret;
        }

        // Построение тиковой свечи по сделке
        public static TfCandle Trade2TickCandle(TfTrade trade)
        {
            return new TfCandle(
                trade.Time, 
                trade.Price, 
                trade.Price, 
                trade.Price, 
                trade.Price, 
                trade.Volume, 
                TfIntervals.TICK);
        }

        // Построение свечи указанного интервала по заданному списку сделок
        static TfCandle BuildCandle(TfIntervals interval, List<TfTrade> trades)
        {
            var StartTime = GetIntervalTime(interval, trades[0].Time);
            foreach (TfTrade trade in trades)
            {
                if (GetIntervalTime(interval, trade.Time) != StartTime)
                    throw new Exception("Список сделок выходит за диапазон указанного интервала");
            }

            var high = trades.Max(TfTrade => TfTrade.Price);
            var low = trades.Min(TfTrade => TfTrade.Price);
            var open = trades[0].Price;
            var close = trades[trades.Count - 1].Price;

            decimal volume = 0;
            foreach (var i in trades) volume += i.Volume;

            return new TfCandle(StartTime, open, close, high, low, volume, interval);
        }

        // Построение свечи указанного интервала по заданному списку тиковых свечей
        public static TfCandle BuildCandle(TfIntervals interval, List<TfCandle> ticks)
        {
            var StartTime = GetIntervalTime(interval, ticks[0].Time);
            foreach (TfCandle tick in ticks)
            {
                if (GetIntervalTime(interval, tick.Time) != StartTime)
                    throw new Exception("Список сделок выходит за диапазон указанного интервала");

                if (tick.Interval != TfIntervals.TICK)
                    throw new Exception("Массив должен состоять из тиковых свечей");
            }

            var high = ticks.Max(TfCandle => TfCandle.Close);
            var low = ticks.Min(TfCandle => TfCandle.Close);
            var open = ticks[0].Close;
            var close = ticks[ticks.Count - 1].Close;

            decimal volume = 0;
            foreach (var i in ticks) volume += i.Volume;

            return new TfCandle(StartTime, open, close, high, low, volume, interval);

        }

        // Построение тиковой свечи по строке сделки
        public static TfCandle BuildCandle(FsFieldsStruct fieldsId, string trade)
        {
            return Trade2TickCandle(BuildTrade(fieldsId, trade));
        }
    }
}
