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
using System.Diagnostics;
using System.Windows.Threading;

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
            this.centerOfScreenTextBox.Text = MakeCoordinateString(Settings.ScreenCenter);
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
        private static readonly object SaveLock = new object();
        private static bool _isSaving = false;

        private async Task SaveFile()
        {
            if (_isSaving) return;

            try
            {
                _isSaving = true;
                var text = JsonConvert.SerializeObject(MainWindow.AppModel, Formatting.Indented);
                var path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, FilePath);

                await Task.Run(() =>
                {
                    lock (SaveLock)
                    {
                        File.WriteAllText(path, text);
                    }
                });
            }
            catch (IOException)
            {
                // File is locked, ignore - will save on next opportunity
            }
            finally
            {
                _isSaving = false;
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
                setCenterOfScreenButton.Background = Brushes.Green;
            }
            else
            {
                setCenterOfScreenButton.Background = Brushes.Gray;
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

        private void openScreenshotFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var directory = System.AppDomain.CurrentDomain.BaseDirectory;
            var screenshotPath = System.IO.Path.Combine(directory, "screenshots");

            if (!Directory.Exists(screenshotPath))
            {
                Directory.CreateDirectory(screenshotPath);
            }

            Process.Start("explorer.exe", screenshotPath);
        }

        private void showInventoryButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAreaOverlay(Settings.InventoryLeftTop, Settings.InventoryRightBottom, "Inventory Area");
        }

        private void showConditionButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAreaOverlay(Settings.ConditionLeftTop, Settings.ConditionRightBottom, "Condition Area");
        }

        private void showWorldHopButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAreaOverlay(Settings.WorldHopLeftTop, Settings.WorldHopRightBottom, "World Hop Area");
        }

        private void showCenterButton_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.ScreenCenter == null)
            {
                MessageBox.Show("Center of screen not set", "Not Set", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Show a small crosshair at the center point
            var point = Settings.ScreenCenter;
            var size = 40;
            var topLeft = new Models.Point(point.X - size / 2, point.Y - size / 2);
            var bottomRight = new Models.Point(point.X + size / 2, point.Y + size / 2);
            ShowAreaOverlay(topLeft, bottomRight, "Screen Center", true);
        }

        private void ShowAreaOverlay(Models.Point topLeft, Models.Point bottomRight, string title, bool isCrosshair = false)
        {
            if (topLeft == null || bottomRight == null)
            {
                MessageBox.Show($"{title} not set", "Not Set", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var overlayWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                Left = topLeft.X,
                Top = topLeft.Y,
                Width = bottomRight.X - topLeft.X,
                Height = bottomRight.Y - topLeft.Y
            };

            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)),
                BorderThickness = new Thickness(3),
                Background = new SolidColorBrush(Color.FromArgb(50, 255, 0, 0))
            };

            if (isCrosshair)
            {
                var canvas = new Canvas();
                var horizontalLine = new Line
                {
                    X1 = 0, Y1 = overlayWindow.Height / 2,
                    X2 = overlayWindow.Width, Y2 = overlayWindow.Height / 2,
                    Stroke = Brushes.Red, StrokeThickness = 2
                };
                var verticalLine = new Line
                {
                    X1 = overlayWindow.Width / 2, Y1 = 0,
                    X2 = overlayWindow.Width / 2, Y2 = overlayWindow.Height,
                    Stroke = Brushes.Red, StrokeThickness = 2
                };
                canvas.Children.Add(horizontalLine);
                canvas.Children.Add(verticalLine);
                border.Child = canvas;
            }

            overlayWindow.Content = border;
            overlayWindow.MouseDown += (s, args) => overlayWindow.Close();

            // Auto-close after 2 seconds
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                overlayWindow.Close();
            };
            timer.Start();

            overlayWindow.Show();
        }
    }
}
