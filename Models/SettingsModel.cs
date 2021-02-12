namespace BetterClicker.Models
{
    public class SettingsModel
    {
        public int? DoubleClickDelayMs { get; set; }
        public Point InventoryLeftTop { get; set; }
        public Point InventoryRightBottom { get; set; }
        public int? InventoryPrecisionModifier { get; set; }
    }
}