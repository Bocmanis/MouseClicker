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
            if (Settings.WorldHopDelayMs == null)
            {
                Settings.WorldHopDelayMs = 8000;
            }
            if (Settings.RetryDelayMs == null)
            {
                Settings.RetryDelayMs = 2000;
            }
            this.doubleClickTextBox.Text = Settings.DoubleClickDelayMs?.ToString();
            this.inventoryPrecisionModifierTextBox.Text = Settings.InventoryPrecisionModifier?.ToString();
            this.agilityModeCheckBox.IsChecked = Settings.AgilityMode;
            this.minBlobSizeTextBox.Text = Settings.MinBlobSize?.ToString();
            this.retryDelayTextBox.Text = Settings.RetryDelayMs?.ToString();

            SetInventoryPointTexts();
            SetConditionPointTexts();
            SetCenterOfScreenPointTexts();
            SetWorldHopPointTexts();
            SetColorFilterTexts();
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
            this.worldHopDelayTextBox.Text = Settings.WorldHopDelayMs?.ToString();
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

        private async void retryDelayTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(retryDelayTextBox.Text, out int retryDelay))
            {
                Settings.RetryDelayMs = retryDelay;
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

        private async void worldHopDelayTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(worldHopDelayTextBox.Text, out int worldHopDelay))
            {
                Settings.WorldHopDelayMs = worldHopDelay;
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

        private void previewWorldHopButton_Click(object sender, RoutedEventArgs e)
        {
            var worldCount = Settings.WorldHopCount;
            var leftTop = Settings.WorldHopLeftTop;
            var rightBottom = Settings.WorldHopRightBottom;

            if (leftTop == null || rightBottom == null)
            {
                MessageBox.Show("World Hop area not set", "Not Set", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (worldCount <= 0)
            {
                MessageBox.Show("World Hop Count must be greater than 0", "Invalid Count", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var width = rightBottom.X - leftTop.X;
            var height = rightBottom.Y - leftTop.Y;

            decimal verticalIncrement = (rightBottom.Y - leftTop.Y) / (decimal)worldCount;
            var precision = (int)Math.Round(verticalIncrement / 3, 0);

            var minX = precision;
            var maxX = width - precision;
            if (minX >= maxX)
            {
                minX = 0;
                maxX = width;
            }

            var overlayWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                Left = leftTop.X,
                Top = leftTop.Y,
                Width = width,
                Height = height
            };

            var canvas = new Canvas();

            var outer = new System.Windows.Shapes.Rectangle
            {
                Width = width,
                Height = height,
                Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 0)),
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };
            Canvas.SetLeft(outer, 0);
            Canvas.SetTop(outer, 0);
            canvas.Children.Add(outer);

            for (int row = 0; row < worldCount; row++)
            {
                var yLeftTop = (int)Math.Round(precision + row * verticalIncrement);
                var yRightBottom = (int)Math.Round(-precision + verticalIncrement * (row + 1));
                if (yLeftTop >= yRightBottom)
                {
                    yLeftTop = (int)(row * verticalIncrement);
                    yRightBottom = (int)((row + 1) * verticalIncrement);
                }

                var rect = new System.Windows.Shapes.Rectangle
                {
                    Width = maxX - minX,
                    Height = yRightBottom - yLeftTop,
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)),
                    StrokeThickness = 2,
                    Fill = new SolidColorBrush(Color.FromArgb(50, 255, 0, 0))
                };
                Canvas.SetLeft(rect, minX);
                Canvas.SetTop(rect, yLeftTop);
                canvas.Children.Add(rect);

                var label = new TextBlock
                {
                    Text = (row + 1).ToString(),
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                    Padding = new Thickness(3, 0, 3, 0),
                    FontWeight = FontWeights.Bold
                };
                Canvas.SetLeft(label, minX + 2);
                Canvas.SetTop(label, yLeftTop + 2);
                canvas.Children.Add(label);
            }

            overlayWindow.Content = canvas;
            overlayWindow.MouseDown += (s, args) => overlayWindow.Close();

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                overlayWindow.Close();
            };
            timer.Start();

            overlayWindow.Show();
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

        private void SetColorFilterTexts()
        {
            redFilterRedMinTextBox.Text = Settings.RedFilterRedMin.ToString();
            redFilterRedMaxTextBox.Text = Settings.RedFilterRedMax.ToString();
            redFilterGreenMinTextBox.Text = Settings.RedFilterGreenMin.ToString();
            redFilterGreenMaxTextBox.Text = Settings.RedFilterGreenMax.ToString();
            redFilterBlueMinTextBox.Text = Settings.RedFilterBlueMin.ToString();
            redFilterBlueMaxTextBox.Text = Settings.RedFilterBlueMax.ToString();

            greenFilterRedMinTextBox.Text = Settings.GreenFilterRedMin.ToString();
            greenFilterRedMaxTextBox.Text = Settings.GreenFilterRedMax.ToString();
            greenFilterGreenMinTextBox.Text = Settings.GreenFilterGreenMin.ToString();
            greenFilterGreenMaxTextBox.Text = Settings.GreenFilterGreenMax.ToString();
            greenFilterBlueMinTextBox.Text = Settings.GreenFilterBlueMin.ToString();
            greenFilterBlueMaxTextBox.Text = Settings.GreenFilterBlueMax.ToString();

            UpdateColorPreviews();
        }

        private void UpdateColorPreviews()
        {
            redFilterPreviewMin.Background = new SolidColorBrush(Color.FromRgb(
                (byte)Settings.RedFilterRedMin, (byte)Settings.RedFilterGreenMin, (byte)Settings.RedFilterBlueMin));
            redFilterPreviewMax.Background = new SolidColorBrush(Color.FromRgb(
                (byte)Settings.RedFilterRedMax, (byte)Settings.RedFilterGreenMax, (byte)Settings.RedFilterBlueMax));

            greenFilterPreviewMin.Background = new SolidColorBrush(Color.FromRgb(
                (byte)Settings.GreenFilterRedMin, (byte)Settings.GreenFilterGreenMin, (byte)Settings.GreenFilterBlueMin));
            greenFilterPreviewMax.Background = new SolidColorBrush(Color.FromRgb(
                (byte)Settings.GreenFilterRedMax, (byte)Settings.GreenFilterGreenMax, (byte)Settings.GreenFilterBlueMax));
        }

        private async void redFilterTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(redFilterRedMinTextBox.Text, out int rMin)) Settings.RedFilterRedMin = rMin;
            if (int.TryParse(redFilterRedMaxTextBox.Text, out int rMax)) Settings.RedFilterRedMax = rMax;
            if (int.TryParse(redFilterGreenMinTextBox.Text, out int gMin)) Settings.RedFilterGreenMin = gMin;
            if (int.TryParse(redFilterGreenMaxTextBox.Text, out int gMax)) Settings.RedFilterGreenMax = gMax;
            if (int.TryParse(redFilterBlueMinTextBox.Text, out int bMin)) Settings.RedFilterBlueMin = bMin;
            if (int.TryParse(redFilterBlueMaxTextBox.Text, out int bMax)) Settings.RedFilterBlueMax = bMax;
            UpdateColorPreviews();
            await SaveFile();
        }

        private async void greenFilterTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(greenFilterRedMinTextBox.Text, out int rMin)) Settings.GreenFilterRedMin = rMin;
            if (int.TryParse(greenFilterRedMaxTextBox.Text, out int rMax)) Settings.GreenFilterRedMax = rMax;
            if (int.TryParse(greenFilterGreenMinTextBox.Text, out int gMin)) Settings.GreenFilterGreenMin = gMin;
            if (int.TryParse(greenFilterGreenMaxTextBox.Text, out int gMax)) Settings.GreenFilterGreenMax = gMax;
            if (int.TryParse(greenFilterBlueMinTextBox.Text, out int bMin)) Settings.GreenFilterBlueMin = bMin;
            if (int.TryParse(greenFilterBlueMaxTextBox.Text, out int bMax)) Settings.GreenFilterBlueMax = bMax;
            UpdateColorPreviews();
            await SaveFile();
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
