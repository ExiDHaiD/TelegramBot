using System;
using System.Threading.Tasks;
using TradingFramework.BaseConnector;
using TradingFramework.DataTypes;
using TradingFramework.ScottTradingPlot;
using TradingFramework.ObserversFactory;
using System.Threading;

namespace TradingFramework.Informers
{
    public class TfInformersScreen
    {
        public class InformerMsg
        {
            public string Msg = "";
            public System.Drawing.Bitmap Pic = null;
            public TfObserverFactory.InformerType Type;
            public long UserId;
        }

        public delegate void InformerMsgHandler(InformerMsg message);

        TfBaseConnector _connector;
        ControlGuard _cGuard;
        InformersScreenSettings _settings;
        InformerMsgHandler _handler;
        public class InformersScreenSettings
        {
            public MarketInstrument instrument;
            public LevelTool.LevelType lType = LevelTool.LevelType.None;
            public VolatilityTool.VolatilityType vType = VolatilityTool.VolatilityType.None;
            public bool drawBB = false;
            public LevelTool.LineLevelType llType;
            public long UserId;
        }
        public TfInformersScreen(
            TfBaseConnector connector, ControlGuard cGuard, InformerMsgHandler handler, InformersScreenSettings settings)
        {
            _handler = handler;
            _connector = connector;
            _cGuard = cGuard;
            _settings = settings;
            Task t = new Task(ProcessInfromer);
            t.Start();
        }

        void ProcessInfromer()
        {
            System.Windows.Forms.Control control = null;
            Guid locker = new Guid();
            InformerMsg msg = new InformerMsg();
            msg.Type = TfObserverFactory.InformerType.Screenshot;
            msg.UserId = _settings.UserId;
            try
            {
                control = _cGuard.GetControl(locker);
            }
            catch (Exception e)
            {
                msg.Msg = e.Message;
                _handler.BeginInvoke(msg, null, null);
                return;
            }

            Task<InformerMsg> screenTask = new Task<InformerMsg>(() => GetScreen(control));
            screenTask.Start();
            var complete = screenTask.Wait(60000);
            if (!complete)
                msg.Msg = "Таймаут получения скриншота";
            else
                msg = screenTask.Result;

            try
            {
                _cGuard.FreeControl(locker);
            }
            catch
            { }
            _handler.BeginInvoke(msg, null, null);
        }

        InformerMsg GetScreen(System.Windows.Forms.Control control)
        {
            InformerMsg msg = new InformerMsg();
            msg.Type = TfObserverFactory.InformerType.Screenshot;
            try
            {
                TfScottTradingPlot plot = new TfScottTradingPlot(control, _settings.instrument, _connector,
                    TfIntervals.M5,
                    250,
                    _settings.lType,
                    _settings.lType == LevelTool.LevelType.OrderBook ? 20 : 40,
                    _settings.vType,
                    10,
                    _settings.drawBB,
                    false,
                    true,
                    _settings.llType);
                int w = 2000;
                int h = _settings.lType == LevelTool.LevelType.OrderBook ? 6000 : 2000;
                msg.Pic = plot.GetScreenShot(w, h);
                plot.Dispose();
            }
            catch (Exception e)
            {
                msg.Msg = e.Message;
            }
            msg.UserId = _settings.UserId;
            return msg;
        }
    }
}
