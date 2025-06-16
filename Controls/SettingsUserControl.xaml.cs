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
        public bool ConditionPointReadActive { get; private set; }
        public bool WorldHopPointReadActive { get; private set; }

        public bool CenterOfScreenPointReadActive { get; private set; }


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
            this.agilityModeCheckBox.IsChecked = Settings.AgilityMode;
            this.minBlobSizeTextBox.Text = Settings.MinBlobSize?.ToString();

            SetInventoryPointTexts();
            SetConditionPointTexts();
            SetCenterOfScreenPointTexts();
            SetWorldHopPointTexts();
        }

        private void SetInventoryPointTexts()
        {
            this.rightBottomInventoryTextBox.Text = MakeCoordinateString(Settings.InventoryRightBottom);
            this.leftTopInventoryTextBox.Text = MakeCoordinateString(Settings.InventoryLeftTop);
        }
        private void SetConditionPointTexts()
        {
            this.rightBottomConditionTextBox.Text = MakeCoordinateString(Settings.ConditionRightBottom);
            this.leftTopConditionTextBox.Text = MakeCoordinateString(Settings.ConditionLeftTop);
        }
        private void SetCenterOfScreenPointTexts()
        {
            this.centerOfScreenTextBox_Copy.Text = MakeCoordinateString(Settings.ScreenCenter);
        }

        private void SetWorldHopPointTexts()
        {
            this.worldHopLeftTop_TextBox.Text = MakeCoordinateString(Settings.WorldHopLeftTop);
            this.worldHopRightBottom_TextBox.Text = MakeCoordinateString(Settings.WorldHopRightBottom);
            this.worldHopCountTextBox.Text = Settings.WorldHopCount.ToString();
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
            if (e.Key == Key.C)
            {
                if (this.InventoryPointReadActive)
                {
                    Settings.InventoryLeftTop = MouseActions.GetMousePosition();
                    SetInventoryPointTexts();
                }
                if (this.ConditionPointReadActive)
                {
                    Settings.ConditionLeftTop = MouseActions.GetMousePosition();
                    SetConditionPointTexts();
                }
                if (this.CenterOfScreenPointReadActive)
                {
                    Settings.ScreenCenter = MouseActions.GetMousePosition();
                    SetCenterOfScreenPointTexts();
                }
                if (this.WorldHopPointReadActive)
                {
                    Settings.WorldHopLeftTop = MouseActions.GetMousePosition();
                    SetWorldHopPointTexts();
                }
            }
            if (e.Key == Key.V)
            {
                if (this.InventoryPointReadActive)
                {
                    Settings.InventoryRightBottom = MouseActions.GetMousePosition();
                    SetInventoryPointTexts();
                }
                if (this.ConditionPointReadActive)
                {
                    Settings.ConditionRightBottom = MouseActions.GetMousePosition();
                    SetConditionPointTexts();
                }
                if (this.WorldHopPointReadActive)
                {
                    Settings.WorldHopRightBottom = MouseActions.GetMousePosition();
                    SetWorldHopPointTexts();
                }
            }
            await SaveFile();
        }

        public static string FilePath = "saveFile.json";

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

        private void setCenterOfScreenButton_Click(object sender, RoutedEventArgs e)
        {
            this.CenterOfScreenPointReadActive = !CenterOfScreenPointReadActive;
            if (CenterOfScreenPointReadActive)
            {
                setCenterOfScreenButton_Copy.Background = Brushes.Green;
            }
            else
            {
                setCenterOfScreenButton_Copy.Background = Brushes.Gray;
            }
        }  

        private void readConditionButton_Click(object sender, RoutedEventArgs e)
        {
            this.ConditionPointReadActive = !ConditionPointReadActive;
            if (ConditionPointReadActive)
            {
                readConditionButton.Background = Brushes.Green;
            }
            else
            {
                readConditionButton.Background = Brushes.Gray;
            }
        }

        private async void minBlobSizeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(minBlobSizeTextBox.Text, out int inventoryPrecision))
            {
                Settings.MinBlobSize = inventoryPrecision;
                await SaveFile();
            }
        }

        private async void agilityModeCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            Settings.AgilityMode = agilityModeCheckBox.IsChecked ?? false;
            await SaveFile();
        }

        private void takeScreenshotsButton_Click(object sender, RoutedEventArgs e)
        {
            new ImageProcessingLogic().GetRedBiggestBlob();
            new ImageProcessingLogic().GetGreenBiggestBlob();
        }

        private void readWorldHopButton_Click(object sender, RoutedEventArgs e)
        {
            this.WorldHopPointReadActive = !WorldHopPointReadActive;
            if (WorldHopPointReadActive)
            {
                readWorldHopButton.Background = Brushes.Green;
            }
            else
            {
                readWorldHopButton.Background = Brushes.Gray;
            }
        }

        private async void worldHopCountTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(worldHopCountTextBox.Text, out int worldCount))
            {
                Settings.WorldHopCount = worldCount;
                await SaveFile();
            }
        }
    }
}
