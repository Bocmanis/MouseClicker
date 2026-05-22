using System.Collections.Generic;

namespace BetterClicker.Models
{
    public class SettingsModel
    {
        public int? DoubleClickDelayMs { get; set; }
        public Point InventoryLeftTop { get; set; }
        public Point InventoryRightBottom { get; set; }
        public int? InventoryPrecisionModifier { get; set; }
        public Point ConditionLeftTop { get; set; }
        public Point ConditionRightBottom { get; set; }
        public Point StatusCheckLeftTop { get; set; }
        public Point StatusCheckRightBottom { get; set; }
        public Point ScreenCenter { get; set; }
        public int? MinBlobSize { get; set; }
        public int? RetryDelayMs { get; set; }
        public Point WorldHopLeftTop { get; set; }
        public Point WorldHopRightBottom { get; set; }
        public int WorldHopCount { get; set; }
        public int? WorldHopDelayMs { get; set; }

        // Red color filter RGB ranges
        public int RedFilterRedMin { get; set; } = 140;
        public int RedFilterRedMax { get; set; } = 255;
        public int RedFilterGreenMin { get; set; } = 0;
        public int RedFilterGreenMax { get; set; } = 60;
        public int RedFilterBlueMin { get; set; } = 140;
        public int RedFilterBlueMax { get; set; } = 255;

        // Green color filter RGB ranges
        public int GreenFilterRedMin { get; set; } = 0;
        public int GreenFilterRedMax { get; set; } = 80;
        public int GreenFilterGreenMin { get; set; } = 140;
        public int GreenFilterGreenMax { get; set; } = 255;
        public int GreenFilterBlueMin { get; set; } = 0;
        public int GreenFilterBlueMax { get; set; } = 80;
    }
}