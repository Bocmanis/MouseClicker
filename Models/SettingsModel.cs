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
        public Point ScreenCenter { get; set; }
        public int? MinBlobSize { get; set; }
        public bool AgilityMode { get; set; }
    }
}