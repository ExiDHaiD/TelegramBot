using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TradingFramework.Informers
{
    public class ControlGuard
    {
        Control _control;
        object locker = new object();
        class Locker
        {
            public Guid _lockerId;
            public Control control;
        }
        public ControlGuard(Control control)
        {
            _control = control;
        }

        List<Locker> lockedControls = new List<Locker>();

        public Control GetControl(Guid lockerId)
        {
            Locker l = new Locker();
            l.control = new Panel();
            l.control.Size = new System.Drawing.Size(200, 200);
            l.control.Location = new System.Drawing.Point(0, 0);
            _control.Invoke(new Action(() => _control.Controls.Add(l.control)));

            lock (locker)
                lockedControls.Add(l);

            return l.control;
        }
        public void FreeControl(Guid lockerId)
        {
            lock (locker)
            {
                var l = lockedControls.Find(p => p._lockerId == lockerId);
                if (l != null)
                {
                    _control.Invoke(new Action(() => _control.Controls.Remove(l.control)));
                    lockedControls.Remove(l);
                }
            }
        }
    }
}
