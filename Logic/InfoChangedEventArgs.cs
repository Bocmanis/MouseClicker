using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterClicker.Logic
{
    public class InfoChangedEventArgs : EventArgs
    {
        public InfoChangedEventArgs() { }
        public string GreenBoxTimeMessage { get; set; }
    }
}
