using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterClicker.Models
{
    public class MouseActionModel
    {
        public int Wait { get; set; }

        [DisplayName("Δ")]
        public int WaitDelta { get; set; }
        public string TotallyNormalName { get; set; }

        [DisplayName("X")]
        public int PointX { get; set; }
        [DisplayName("Y")]
        public int PointY { get; set; }
        [DisplayName("X²")]
        public int RcPtX { get; set; }
        [DisplayName("Y²")]
        public int RcPtY { get; set; }
        public ActionType ActionType { get; set; }

        [DisplayName("InvSpot")]
        public int InventorySpot { get; set; }

        [DisplayName("+")]
        public int Increase { get; set; }
        public bool Remember { get; set; }
        public int RepTimes { get; set; }
        public KeyPresses ClickKey { get; set; }

        [DisplayName("Con")]
        public ConditionType CheckCondition { get; set; }

        public MouseActionModel ShallowCopy()
        {
            return (MouseActionModel)this.MemberwiseClone();
        }
    }
}
