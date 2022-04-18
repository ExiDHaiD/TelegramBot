using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingFramework.BaseConnector;
using TradingFramework.TelegramBot;
using TradingFramework.ObserversFactory;

namespace TradingFramework.Observers
{
    abstract public class TfBaseObserver
    {
        public class ObserverMsg
        {
            public string Msg = "";
            public System.Drawing.Bitmap Pic = null;
            public TfObserverFactory.ObserverType Type;
            public Guid ObserverId;
            public bool DeleteObserver = false;
        }
        public delegate void RuleTriggerHandler(ObserverMsg message);
        protected TfBaseConnector _connector;
        protected RuleTriggerHandler _triggerHandler = null;

        public Guid observerId { get; }
        public TfBaseObserver(TfBaseConnector connector, RuleTriggerHandler triggerHandler)
        {            
            observerId = Guid.NewGuid();
            _triggerHandler = triggerHandler;
            _connector = connector;
        }

        abstract public void StartObserver();
        abstract public void StopObserver();
        abstract public object GetSettings();
    }
}
