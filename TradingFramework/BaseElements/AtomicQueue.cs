using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace TradingFramework.AtomicQueue
{
    public class TfAtomicQueue<T> : IDisposable
    {
        public delegate void ProcessHandler(T item);

        Thread _processThd;
        ProcessHandler _handler;
        AutoResetEvent _newitemEvent = new AutoResetEvent(false);
        Queue<T> _queue = new Queue<T>();
        object _locker = new object();
        bool _isStop = false;

        public TfAtomicQueue(ProcessHandler handler)
        {
            if (handler == null)
                throw new Exception("Нулевой указатель на обработчик недопустим");

            _handler = handler;
            _processThd = new Thread(ProcessLoop);
            _processThd.Start();
        }

        public void Put(T item)
        {
            lock (_locker)
            {
                _queue.Enqueue(item);
                _newitemEvent.Set();
            }
        }

        public void Clear()
        {
            lock (_locker)
            {
                _queue.Clear();
            }
        }

        public void Stop()
        {
            _isStop = true;
        }

        void ProcessLoop()
        {
            while (true)
            {
                _newitemEvent.WaitOne();
                while ((_queue.Count > 0) && !_isStop)
                {
                    T item;
                    lock (_locker)
                    {
                        item = _queue.Dequeue();
                    }
                    _handler(item);
                }
            }            
        }

        public int GetQueueCount()
        {
            int ret = 0;
            lock (_locker)
            {
                ret = _queue.Count;
            }
            return ret;
        }

        public void Dispose()
        {
            _processThd.Abort();
            Clear();
        }
    }
}
