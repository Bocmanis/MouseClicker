using BetterClicker.Logic;
using BetterClicker.Models;
using BetterClicker.Win32Actions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BetterClicker.Controls
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsUserControl : UserControl
    {
        public bool InventoryPointReadActive { get; private set; }
        public SettingsModel Settings { get; private set; }

        public SettingsUserControl()
        {
            InitializeComponent();
            
            if (MainWindow.AppModel.Settings == null)
            {
                MainWindow.AppModel.Settings = new SettingsModel();
            }
            this.Settings = MainWindow.AppModel.Settings;
            if (Settings.DoubleClickDelayMs == null)
            {
                Settings.DoubleClickDelayMs = 350;
            }
            this.doubleClickTextBox.Text = Settings.DoubleClickDelayMs?.ToString();
            this.inventoryPrecisionModifierTextBox.Text = Settings.InventoryPrecisionModifier?.ToString();
            SetInventoryPointTexts();
        }

        private void SetInventoryPointTexts()
        {
            this.rightBottomInventoryTextBox.Text = MakeCoordinateString(Settings.InventoryRightBottom);
            this.leftTopInventoryTextBox.Text = MakeCoordinateString(Settings.InventoryLeftTop);
        }

        private string MakeCoordinateString(Models.Point point)
        {
            if (point == null)
            {
                return "not set";
            }
            return $"{point.X};{point.Y}";
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {

        }

        private void readInventoryButton_Click(object sender, RoutedEventArgs e)
        {
            this.InventoryPointReadActive = !InventoryPointReadActive;
            if (InventoryPointReadActive)
            {
                readInventoryButton.Background = Brushes.Green;
            }
            else
            {
                readInventoryButton.Background = Brushes.Gray;
            }
        }

        private async void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (this.InventoryPointReadActive)
            {
                if (e.Key == Key.C)
                {
                    Settings.InventoryLeftTop = MouseActions.GetMousePosition();
                    SetInventoryPointTexts();
                }
                if (e.Key == Key.V)
                {
                    Settings.InventoryRightBottom = MouseActions.GetMousePosition();
                    SetInventoryPointTexts();
                }
                await SaveFile();
            }
        }

        private string FilePath = "saveFile.json";

        private async Task SaveFile()
        {
            var text = JsonConvert.SerializeObject(MainWindow.AppModel, Formatting.Indented);
            var path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, FilePath);
            using (StreamWriter outputFile = new StreamWriter(path))
            {
                await outputFile.WriteAsync(text);
            }
        }

        private async void doubleClickTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(doubleClickTextBox.Text, out int doubleClickDelay))
            {
                Settings.DoubleClickDelayMs = doubleClickDelay;
                await SaveFile();
            }
        }

        private async void inventoryPrecisionModifierTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(inventoryPrecisionModifierTextBox.Text, out int inventoryPrecision))
            {
                Settings.InventoryPrecisionModifier = inventoryPrecision;
                await SaveFile();
            }
        }

        private void doScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            var point = new ImageProcessingLogic().Test();
            MouseActions.DoLeftClick(point);
        }
    }
}
