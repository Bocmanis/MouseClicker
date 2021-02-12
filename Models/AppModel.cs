using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterClicker.Models
{
    public class AppModel
    {
        public ObservableCollection<OverTask> OverTasks { get; set; }
        public SettingsModel Settings { get; set; }
        public List<FullTask> PublicMouseActions { get; set; }

    }
}
