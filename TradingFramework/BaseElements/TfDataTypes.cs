/*-------------------------------------------------------------
 * Базовые типы библиотеки TradingFramework
-------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace TradingFramework.DataTypes
{
    // Структура, описывающая биржевой инструмент
    public class MarketInstrument
    {
        public enum Exchanges
        {
            None,
            Moex,
            Spb,
            Nasdaq,
            Nyse,

        }
        public MarketInstrument()
        {
        }

        public MarketInstrument(string ticker, string board)
        {
            _ticker = ticker;
        }
        public MarketInstrument(string ticker, Exchanges exchange)
        {
            _ticker = ticker;
            Exchange = exchange;
        }

        public MarketInstrument(string ticker)
        {
            _ticker = ticker;
        }

        public static bool operator ==(MarketInstrument f1, MarketInstrument f2)
        {
            if (ReferenceEquals(f1, null) && ReferenceEquals(f2, null))
                return true;
            else if (ReferenceEquals(f1, null) || ReferenceEquals(f2, null))
                return false;

            if (f1._ticker.ToUpper() != f2._ticker.ToUpper())
                return false;
            if (f1.Board.ToUpper() != f2.Board.ToUpper())
                return false;
            return true;
        }

        public override string ToString()
        {
            return Ticker;
        }

        public static bool operator !=(MarketInstrument f1, MarketInstrument f2)
        {
            return !(f1 == f2);
        }

        private string _ticker;
        public string Ticker {
            get {
                return _ticker.ToUpper();
            }
            set {
                _ticker = value;
            }
        }
        public string Board = "";
        public Exchanges Exchange = Exchanges.None;
        public string Name = "";
        public string ShortName = "";
        public string Isin = "";
    }

    // Доступные свечные интервалы
    public enum TfIntervals
    {
        TICK,
        M1,
        M2,
        M3,
        M4,
        M5,
        M6,
        M10,
        M15,
        M20,
        M30,
        H1,
        H2,
        H4,
        H8,
        H12,
        D1,
        W1,
        MN,
        YR
    }

    public class TfInterval
    {
        static public string TfIntervals2String(TfIntervals interval)
        {
            switch (interval)
            {
                case TfIntervals.TICK: return "TICK";
                case TfIntervals.M1: return "M1";
                case TfIntervals.M2: return "M2";
                case TfIntervals.M3: return "M3";
                case TfIntervals.M4: return "M4";
                case TfIntervals.M5: return "M5";
                case TfIntervals.M6: return "M6";
                case TfIntervals.M10: return "M10";
                case TfIntervals.M15: return "M15";
                case TfIntervals.M20: return "M20";
                case TfIntervals.M30: return "M30";
                case TfIntervals.H1: return "H1";
                case TfIntervals.H2: return "H2";
                case TfIntervals.H4: return "H4";
                case TfIntervals.H8: return "H8";
                case TfIntervals.H12: return "H12";
                case TfIntervals.D1: return "D1";
                default:
                    throw new Exception("неподдерживаемый таймфрейм");
            }
        }

        static public TfIntervals String2TfIntervals(string str)
        {
            switch (str)
            {
                case "TICK":    return TfIntervals.TICK; 
                case "M1":      return TfIntervals.M1; 
                case "M2":      return TfIntervals.M2; 
                case "M3":      return TfIntervals.M3; 
                case "M4":      return TfIntervals.M4; 
                case "M5":      return TfIntervals.M5; 
                case "M6":      return TfIntervals.M6; 
                case "M10":     return TfIntervals.M10; 
                case "M15":     return TfIntervals.M15; 
                case "M20":     return TfIntervals.M20; 
                case "M30":     return TfIntervals.M30; 
                case "H1":      return TfIntervals.H1; 
                case "H2":      return TfIntervals.H2; 
                case "H4":      return TfIntervals.H4; 
                case "H8":      return TfIntervals.H8; 
                case "H12":     return TfIntervals.H12; 
                case "D1":      return TfIntervals.D1; 
                default:
                    throw new Exception("неподдерживаемый таймфрейм");
            }

        }

        static public int Interval2Minutes(TfIntervals interval)
        {
            switch (interval)
            {
                case TfIntervals.M1: return 1;
                case TfIntervals.M2: return 2;
                case TfIntervals.M3: return 3;
                case TfIntervals.M4: return 4;
                case TfIntervals.M5: return 5;
                case TfIntervals.M6: return 6;
                case TfIntervals.M10: return 10;
                case TfIntervals.M15: return 15;
                case TfIntervals.M20: return 20;
                case TfIntervals.M30: return 30;
                case TfIntervals.H1: return 60;
                case TfIntervals.H2: return 60 * 2;
                case TfIntervals.H4: return 60 * 4;
                case TfIntervals.H8: return 60 * 8;
                case TfIntervals.H12: return 60 * 12;
                case TfIntervals.D1: return 60 * 24;
                case TfIntervals.W1: return 60 * 24 * 7;
                case TfIntervals.MN: return 60 * 24 * 30;
                default:
                    throw new Exception("Неподдерживаемый интервал");
            }
        }
    }

    // Класс стакана котировок

    [DataContractFormat]
    public class TfOrderbook
    {
        // Запись в массиве заявок
        public struct OrderbookRecord
        {
            public decimal Quantity { get; }    // количество лимитных заявок по цене
            public decimal Price { get; }   // цена 

            public OrderbookRecord(decimal quantity, decimal price)
            {
                Quantity = quantity;
                Price = price;
            }
        }

        public MarketInstrument _instrument;
        public DateTime _orderBookTime; // время получения стакана
        public List<OrderbookRecord> _bids; // массив заявок на покупку
        public List<OrderbookRecord> _asks; // массив заявок на продажу

        public TfOrderbook(MarketInstrument instrument, List<OrderbookRecord> bids, List<OrderbookRecord> asks, DateTime time)
        {
            _instrument = instrument;
            _orderBookTime = time;
            _bids = bids;
            _asks = asks;
        }
        public TfOrderbook()
        {
        }
    }

    // Структура, описывающая свечу
    public class TfCandle : IComparable
    {
        public TfCandle() { }
        public TfCandle(TfCandle c)
        {
            Time = c.Time;
            Open = c.Open;
            Close = c.Close;
            High = c.High;
            Low = c.Low;
            Volume = c.Volume;
            Interval = c.Interval;
            BidType = TfBidType.NoDir;
        }
        public TfCandle(DateTime time, decimal open, decimal close, decimal high, decimal low, decimal volume, TfIntervals interval)
        {
            Time = time;
            Open = open;
            Close = close;
            High = high;
            Low = low;
            Volume = volume;
            Interval = interval;
            BidType = TfBidType.NoDir;
        }

        public override string ToString()
        {
            string intervalStr = TfInterval.TfIntervals2String(Interval);
            return
                "Time: " + Time.ToString() + ";" +
                "Open: " + Open.ToString() + ";" +
                "Close: " + Close.ToString() + ";" +
                "High: " + High.ToString() + ";" +
                "Low: " + Low.ToString() + ";" +
                "Volume: " + Volume.ToString() + ";" +
                "TimeFrame: " + intervalStr;
        }

        public int CompareTo(object other)
        {
            if (((TfCandle)other).Time == Time) return 0;
            else if (((TfCandle)other).Time > Time) return -1;
            else  return 1;
        }

        public static string GetTimeString(TfIntervals interval, DateTime time)
        {
            string lableString = "";
            switch (interval)
            {
                case TfIntervals.M1:
                case TfIntervals.M5:
                case TfIntervals.H1:
                    lableString = time.ToString("MMM‘dd\nHH:mm");
                    break;
                case TfIntervals.D1:
                    lableString = time.ToString("yyyy\nMMM dd");
                    break;
                default:
                    lableString = time.ToString("yy.MM.dd\nHH:mm");
                    break;
            }
            return lableString;
        }

        public MarketInstrument Instrument;
        public DateTime Time;
        public decimal Open;
        public decimal Close;
        public decimal High;
        public decimal Low;
        public decimal Volume;
        public TfIntervals Interval;
        TfBidType BidType = TfBidType.NoDir; // для тиковых данных
    }

    // Структура, описывающая событие обновления стакана для инструмента
    public struct TfOrderBookEvent
    {
        public TfOrderBookEvent(MarketInstrument instrument, TfOnOrderBook callback)
        {
            Instrument = instrument;
            Callback = callback;
        }

        public MarketInstrument Instrument;
        public TfOnOrderBook Callback;
    }

    // Структура, описывающая событие сделки для инстурмента
    public struct TfTradeEvent
    {
        public TfTradeEvent(MarketInstrument instrument, TfOnNewTrade callback)
        {
            Instrument = instrument;
            Callback = callback;
        }

        public MarketInstrument Instrument;
        public TfOnNewTrade Callback;
    }

    // Структура, описывающая совершенную сделку по инструменту
    public class TfTrade
    {
        public TfTrade()
        { }
        public TfTrade(TfTrade trade)
        {
            Instrument = trade.Instrument;
            TradeId = trade.TradeId;
            Time = trade.Time;
            Volume = trade.Volume;
            Price = trade.Price;
            OperationType = trade.OperationType;
        }
        public TfTrade(MarketInstrument instrument, DateTime time, decimal volume, decimal price, TfBidType operationType, long tradeId = 0)
        {
            Instrument = instrument;
            Time = time;
            Volume = volume;
            Price = price;
            OperationType = operationType;
            TradeId = tradeId;
        }

        public MarketInstrument Instrument;
        public long TradeId;
        public DateTime Time;
        public decimal Volume;
        public decimal Price;
        public TfBidType OperationType;
    }

    // Структура, описывающая событие прихода новой свечи для инструмента
    public struct TfNewCandleEvent
    {
        public TfNewCandleEvent(MarketInstrument instrument, TfOnNewCandle callback, TfIntervals interval)
        {
            Intreval = interval;
            Instrument = instrument;
            Callback = callback;
        }

        public MarketInstrument Instrument;
        public TfIntervals Intreval;
        public TfOnNewCandle Callback;
    }

    // Перечисление доступных направлений сделки
    public enum TfBidType
    {
        Buy, Sell, NoDir
    }

    // Перечисление типов заявок
    public enum TfOrderType
    {
        Market,
        Limit,
        TakeProfit,
        StopLoss
    }

    // Структура, описывающая заявку
    public struct TfOrder
    {
        public MarketInstrument Instrument;
        public int Volume;
        public decimal Price;
        public TfBidType BidType;
        public TfOrderType OrderType;
    }

    public delegate void TfOnOrderBook(TfOrderbook orderBook);  // Шаблон функции, обрабатывающей обновление стакана
    public delegate void TfOnNewCandle(TfCandle candle);        // Шаблон функции, обрабатывающей новую свечу
    public delegate void TfOnNewTrade(TfTrade trade);           // Шаблон функции, обрабатывающей новую сделку по инструменту
}
