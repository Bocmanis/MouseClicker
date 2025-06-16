using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterClicker.Models
{
    public class OverTask
    {
        public OverTask()
        {
            FullTasks = new ObservableCollection<FullTask>();
        }
        public ObservableCollection<FullTask> FullTasks { get; set; }
        public string Name { get; set; }
        public bool OnRepeat { get; set; }
        public bool NoDelay { get; set; }

        public override string ToString()
        {
            var repeatSymbol = OnRepeat ? "↺" : "1";
            return $"{Name} ({repeatSymbol})";
        }
    }
}
