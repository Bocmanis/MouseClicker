using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterClicker.Models
{
    public class FullTask
    {
        public FullTask()
        {
            MouseActions = new ObservableCollection<MouseActionModel>();
        }

        public Guid? MouseActionsId { get; set; }
        public ObservableCollection<MouseActionModel> MouseActions { get; set; }
        public string Name { get; set; }
        public int RepeatTaskTimes { get; set; }
        public int TimeBetweenTasks { get; set; }
        public bool IsPublic { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }
}
