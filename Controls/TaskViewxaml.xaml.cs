using BetterClicker.Logic;
using BetterClicker.Models;
using BetterClicker.Win32Actions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace BetterClicker.Controls
{
    /// <summary>
    /// Interaction logic for TaskViewxaml.xaml
    /// </summary>
    public partial class TaskViewxaml : UserControl
    {
        public ObservableCollection<FullTask> FullTasks { get; set; }
        public FullTask CurrentTask { get; set; }
        public MouseActionModel CurrentMouseAction { get; set; }
        public TaskRunner CurrentTaskRunning { get; private set; }
        public AppModel AppModel { get; set; }

        public ObservableCollection<OverTask> OverTaskSource { get; set; }
        public ListCollectionView OverTasks { get; set; }
        public OverTask CurrentOverTask { get; private set; }
        public Stopwatch StopWatch { get; private set; }
        public TaskRunner TaskRunner { get; private set; }
        public DispatcherTimer WaitTimeTimer { get; private set; }
        public TimeSpan WaitTimeTotal { get; private set; }
        public string GreenBoxTimeMessage { get; private set; }
        public Exception CurrentException { get; private set; }

        public TaskViewxaml()
        {
            InitializeComponent();
            this.AppModel = MainWindow.AppModel;
            if (AppModel.PublicMouseActions == null)
            {
                AppModel.PublicMouseActions = new List<FullTask>();
            }
            CreateGrids();
            ResetPublicMouseActionsContent();
            this.StopWatch = new Stopwatch();
            WaitTimeTimer = new DispatcherTimer();
            WaitTimeTimer.Tick += new EventHandler(OnTimerTick);
            WaitTimeTimer.Interval = TimeSpan.FromMilliseconds(237);
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (CurrentTaskRunning?.DoStop ?? true)
            {
                waitTimeLogBox.Text = string.Empty;
                StopWatch.Reset();
                return;
            }
            var elapsed = StopWatch.Elapsed.ToString(@"mm\:ss\:fff");
            var total = WaitTimeTotal.ToString(@"mm\:ss\:fff");

            waitTimeLogBox.Text = $"{elapsed}/{total}";
        }

        private void ResetPublicMouseActionsContent()
        {
            fullTaskComboBox.ItemsSource = null;
            fullTaskComboBox.ItemsSource = AppModel.PublicMouseActions;
            fullTaskComboBox.SelectedItem = AppModel?.PublicMouseActions?.FirstOrDefault();
            if (fullTaskComboBox.SelectedItem == null)
            {
                addTaskFromComboBoxButton.IsEnabled = false;
            }
            else
            {
                addTaskFromComboBoxButton.IsEnabled = true;
            }
        }

        private void CreateGrids()
        {
            OverTaskSource = AppModel.OverTasks;
            OverTasks = new ListCollectionView(OverTaskSource);

           
            FullTasks = CurrentOverTask?.FullTasks;

            overTaskDataGrid.SelectionChanged += OverTaskDataGridSelectionChanged;
            overTaskDataGrid.ItemsSource = OverTasks;

            if (!string.IsNullOrEmpty(AppModel.LastOverTaskName))
            {
                var selectTask = OverTaskSource.FirstOrDefault(x => x.Name == AppModel.LastOverTaskName);
                if (selectTask != null)
                {
                    CurrentOverTask = selectTask;
                }
                else
                {
                    CurrentOverTask = OverTaskSource.FirstOrDefault();
                }
            }

            overTaskDataGrid.SelectedItem = CurrentOverTask;

            fullTasksDataGrid.CanUserAddRows = false;
            fullTasksDataGrid.ItemsSource = FullTasks;
            fullTasksDataGrid.SelectionChanged += FullTasksDataGridSelectionChanged;
            fullTasksDataGrid.SelectedIndex = 0;
            fullTasksDataGrid.SelectionMode = DataGridSelectionMode.Single;
            fullTasksDataGrid.ColumnWidth = DataGridLength.Auto;
            fullTasksDataGrid.RowHeaderWidth = 20;
            fullTasksDataGrid.CanUserResizeRows = false;

            mouseActionsDataGrid.ColumnWidth = DataGridLength.SizeToHeader;
            mouseActionsDataGrid.SelectionMode = DataGridSelectionMode.Single;
            mouseActionsDataGrid.CanUserAddRows = false;
            mouseActionsDataGrid.SelectionChanged += MouseActionsDataGridSelectionChanged;
            mouseActionsDataGrid.RowHeaderWidth = 20;
            mouseActionsDataGrid.CanUserResizeRows = false;
        }

        private async void ResetPublicMouseActions()
        {
            if (CurrentTask == null)
            {
                return;
            }

            if (AppModel.PublicMouseActions.Any(x => x.MouseActionsId == CurrentTask.MouseActionsId))
            {
                if (!CurrentTask.IsPublic)
                {
                    AppModel.PublicMouseActions = AppModel.PublicMouseActions.Where(x => x.MouseActionsId != CurrentTask.MouseActionsId).ToList();
                    await SaveFile();
                    ResetPublicMouseActionsContent();
                }
            }
            else
            {
                if (CurrentTask.IsPublic)
                {
                    AppModel.PublicMouseActions.Add(CurrentTask);
                    await SaveFile();
                    ResetPublicMouseActionsContent();
                }
            }
        }

        private void OverTaskDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentOverTask = (OverTask)overTaskDataGrid.SelectedItem;
            FullTasks = CurrentOverTask?.FullTasks;
            fullTasksDataGrid.ItemsSource = null;
            fullTasksDataGrid.ItemsSource = CurrentOverTask?.FullTasks;
            fullTasksDataGrid.SelectedItem = CurrentOverTask?.FullTasks?.FirstOrDefault();

            CurrentTask = (FullTask)fullTasksDataGrid.SelectedItem;
            mouseActionsDataGrid.ItemsSource = null;
            mouseActionsDataGrid.ItemsSource = CurrentTask?.MouseActions;
            CurrentMouseAction = CurrentTask?.MouseActions?.FirstOrDefault();
            Resetcolumns();

            repeatCountTextBox.Text = CurrentOverTask?.RepeatCount.ToString() ?? "0";
            overTaskNameTextBox.Text = CurrentOverTask?.Name;
        }

        private void MouseActionsDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentMouseAction = (MouseActionModel)mouseActionsDataGrid.SelectedItem;
            Resetcolumns();
        }

        private void FullTasksDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResetPublicMouseActions();

            CurrentTask = (FullTask)fullTasksDataGrid.SelectedItem;
            if (CurrentTask != null && CurrentTask.IsPublic)
            {
                CurrentTask.MouseActions = AppModel.PublicMouseActions.FirstOrDefault(x => x.MouseActionsId == CurrentTask.MouseActionsId)?.MouseActions;
            }
            mouseActionsDataGrid.ItemsSource = null;
            mouseActionsDataGrid.ItemsSource = CurrentTask?.MouseActions;
            if (CurrentTask != null && CurrentTask.MouseActions != null)
            {
                CurrentMouseAction = CurrentTask.MouseActions.FirstOrDefault();
            }
            Resetcolumns();
        }

        private void addNewButtonName_Click(object sender, RoutedEventArgs e)
        {
            var newTask = new FullTask()
            {
                MouseActionsId = Guid.NewGuid(),
                MouseActions = new ObservableCollection<MouseActionModel>(),
            };
            FullTasks.Add(newTask);

            fullTasksDataGrid.SelectedItem = newTask;
            addMouseActionButton_Click(sender, e);
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            Resetcolumns();
        }

        private void Resetcolumns()
        {
            if (fullTasksDataGrid.Columns.Count > 7)
            {
                fullTasksDataGrid.Columns[0].Visibility = Visibility.Hidden; // MouseActionsId
                fullTasksDataGrid.Columns[1].Visibility = Visibility.Hidden; // MouseActions
                fullTasksDataGrid.Columns[2].Width = new DataGridLength(1, DataGridLengthUnitType.Star); // Name - takes remaining space
                fullTasksDataGrid.Columns[3].Width = 40;  // Rep
                fullTasksDataGrid.Columns[4].Width = 50;  // Delay
                fullTasksDataGrid.Columns[5].Width = 50;  // Skip
                fullTasksDataGrid.Columns[6].Visibility = Visibility.Hidden; // IgnoreInvSpacesList
                fullTasksDataGrid.Columns[7].Width = 35;  // Pub
            }

            if (mouseActionsDataGrid.Columns.Count > 13)
            {
                mouseActionsDataGrid.Columns[2].Width = 150; // TotallyNormalName
                mouseActionsDataGrid.Columns[7].Width = 90;  // ActionType
                mouseActionsDataGrid.Columns[13].Width = 70; // Con
            }
        }

        private void addMouseActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentTask.MouseActions == null)
            {
                CurrentTask.MouseActions = new ObservableCollection<MouseActionModel>();
            }

            CurrentTask.MouseActions.Add(new MouseActionModel()
            {
                ActionType = ActionType.LeftClick,
                Wait = 900,
                WaitDelta = 300,
            });
        }

        private void mainGrid_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                StopTask();
            }

            if (CurrentMouseAction == null)
            {
                return;
            }
            if (e.Key == Key.C)
            {
                var position = MouseActions.GetMousePosition();
                CurrentMouseAction.PointX = position.X;
                CurrentMouseAction.PointY = position.Y;
                mouseActionsDataGrid.CancelEdit();
                mouseActionsDataGrid.CancelEdit();

                mouseActionsDataGrid.Items.Refresh();
            }
            if (e.Key == Key.V)
            {
                var position = MouseActions.GetMousePosition();
                CurrentMouseAction.RcPtX = position.X;
                CurrentMouseAction.RcPtY = position.Y;
                mouseActionsDataGrid.CancelEdit();
                mouseActionsDataGrid.CancelEdit();

                mouseActionsDataGrid.Items.Refresh();
            }
        }

        private async void startTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentTask == null)
            {
                return;
            }

            AppModel.LastOverTaskName = CurrentOverTask.Name;
            await SaveFile();
            MouseActions.DoubleClickDelay = AppModel.Settings.DoubleClickDelayMs ?? 350;
            TaskRunner = new TaskRunner();
            TaskRunner.OnInfoChanged += onLoggerAddition;
            TaskRunner.OnNewWaitTime += onNewWaitTimeLogger;
            CurrentTaskRunning = TaskRunner;
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                /* run your code here */
                try
                {
                    TaskRunner.RunTasks(CurrentOverTask);
                }
                catch (Exception ex)
                {
                    CurrentException = ex;
                    return;
                }
                
            }).Start();
        }

        private void onLoggerAddition(object sender, EventArgs e)
        {
            InfoChangedEventArgs args = null;
            if (e.GetType() == typeof(InfoChangedEventArgs))
            {
                args = (InfoChangedEventArgs)e;
                GreenBoxTimeMessage = args.GreenBoxTimeMessage;
            }
            this.Dispatcher.Invoke(() =>
            {
                var repeatCount = TaskRunner.CurrentOverTask.RepeatCount;
                var repeat = repeatCount > 0 ? $"Repeat {TaskRunner.OverTaskCounter}/{repeatCount}" : "Single";
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"OT:  {TaskRunner.CurrentOverTask?.Name}");
                stringBuilder.AppendLine($"     {repeat}");
                stringBuilder.AppendLine($"-------------------------------------");
                stringBuilder.AppendLine($"FT:  {TaskRunner.CurrentFullTask?.Name}");
                stringBuilder.AppendLine($"     {TaskRunner.FullTaskRepeatText}");
                stringBuilder.AppendLine($"-------------------------------------");
                stringBuilder.AppendLine($"MA:  {TaskRunner.CurrentMouseAction?.TotallyNormalName}");
                stringBuilder.AppendLine($"     {TaskRunner.MouseActionCounterText}");
                stringBuilder.AppendLine($"Info: {sender}");
                stringBuilder.AppendLine($"{GreenBoxTimeMessage}");
                if (CurrentException != null)
                {
                    stringBuilder.AppendLine($"Error: {CurrentException.Message}");
                    stringBuilder.AppendLine($"Stack: {CurrentException.StackTrace}");
                }

                if (TaskRunner.DoStop)
                {
                    stringBuilder.AppendLine("\n Status: Stopped");
                }
                logTextBox.Text = stringBuilder.ToString();
            });
        }

        private void onNewWaitTimeLogger(object sender, EventArgs e)
        {
            WaitTimeTotal = TimeSpan.FromMilliseconds((int)sender);

            this.Dispatcher.Invoke(() =>
            {
                StopWatch.Restart();
                WaitTimeTimer.Start();
            });
        }

        private string FilePath = SettingsUserControl.FilePath;
        private static readonly object SaveLock = new object();
        private static bool _isSaving = false;

        private async void saveConfigButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveFile();
        }

        private async Task SaveFile()
        {
            if (_isSaving) return;

            try
            {
                _isSaving = true;
                var text = JsonConvert.SerializeObject(AppModel, Formatting.Indented);
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
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            StopTask();
        }

        private void StopTask()
        {
            if (CurrentTaskRunning == null)
            {
                return;
            }
            CurrentTaskRunning.DoStop = true;

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var overTask = new OverTask()
            {
                FullTasks = new ObservableCollection<FullTask>(),
                Name = "New..",
            };
            OverTaskSource.Add(overTask);
            overTaskDataGrid.SelectedItem = overTask;
        }

        private void overTaskNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CurrentOverTask.Name = overTaskNameTextBox.Text;
        }

        private void repeatCountTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(repeatCountTextBox.Text, out int count) && count >= 0)
            {
                CurrentOverTask.RepeatCount = count;
            }
            else
            {
                repeatCountTextBox.Text = CurrentOverTask?.RepeatCount.ToString() ?? "0";
            }
        }

        private void repeatCountTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void addTaskFromComboBoxButton_Click(object sender, RoutedEventArgs e)
        {
            var fullTask = (FullTask)fullTaskComboBox.SelectedItem;
            FullTasks = CurrentOverTask?.FullTasks;
            fullTasksDataGrid.ItemsSource = null;
            fullTasksDataGrid.ItemsSource = CurrentOverTask?.FullTasks;
            fullTasksDataGrid.SelectedItem = CurrentOverTask?.FullTasks?.FirstOrDefault();

            CurrentTask = (FullTask)fullTasksDataGrid.SelectedItem;
        }

        private void mouseActionsDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyDescriptor is PropertyDescriptor descriptor)
            {
                e.Column.Header = descriptor.DisplayName ?? descriptor.Name;
                e.Column.MinWidth = 35;
            }
        }

        private void mouseActionsDataGrid_AutoGeneratedColumns(object sender, EventArgs e)
        {
            // Remove existing Preview column if it exists
            var existingPreview = mouseActionsDataGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == "Preview");
            if (existingPreview != null)
            {
                mouseActionsDataGrid.Columns.Remove(existingPreview);
            }

            // Add Preview button column at the end
            var previewColumn = new DataGridTemplateColumn
            {
                Header = "Preview",
                Width = 55
            };

            var buttonFactory = new FrameworkElementFactory(typeof(Button));
            buttonFactory.SetValue(Button.ContentProperty, "Show");
            buttonFactory.SetValue(Button.PaddingProperty, new Thickness(2));
            buttonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(PreviewButton_Click));

            previewColumn.CellTemplate = new DataTemplate { VisualTree = buttonFactory };
            mouseActionsDataGrid.Columns.Add(previewColumn);
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var mouseAction = (button?.DataContext) as MouseActionModel;
            if (mouseAction == null) return;

            ShowActionPreview(mouseAction);
        }

        private void ShowActionPreview(MouseActionModel action)
        {
            int x = action.PointX;
            int y = action.PointY;
            int x2 = action.RcPtX;
            int y2 = action.RcPtY;

            // Determine the area to capture
            int left, top, width, height;
            bool isColorAction = action.ActionType == ActionType.ClickRedBox ||
                                 action.ActionType == ActionType.ClickGreenBox ||
                                 action.ActionType == ActionType.ClickBiggestColBox ||
                                 action.ActionType == ActionType.ClickNearestToCenterColBox ||
                                 action.ActionType == ActionType.QuickGreenBox ||
                                 action.ActionType == ActionType.FindGreens;

            if (x2 > 0 && y2 > 0)
            {
                // Rectangle defined
                left = Math.Min(x, x2);
                top = Math.Min(y, y2);
                width = Math.Abs(x2 - x);
                height = Math.Abs(y2 - y);
            }
            else if (isColorAction)
            {
                // Color actions scan the full screen
                left = 0;
                top = 0;
                width = (int)SystemParameters.PrimaryScreenWidth;
                height = (int)SystemParameters.PrimaryScreenHeight;
            }
            else if (x > 0 && y > 0)
            {
                // Single point - show 100x100 area around it
                left = x - 50;
                top = y - 50;
                width = 100;
                height = 100;
            }
            else
            {
                // No coordinates - capture center of screen
                left = (int)(SystemParameters.PrimaryScreenWidth / 2) - 150;
                top = (int)(SystemParameters.PrimaryScreenHeight / 2) - 150;
                width = 300;
                height = 300;
            }

            // Ensure valid dimensions
            left = Math.Max(0, left);
            top = Math.Max(0, top);
            width = Math.Max(50, width);
            height = Math.Max(50, height);

            try
            {
                // Capture screenshot
                var bitmap = new System.Drawing.Bitmap(width, height);
                using (var g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(left, top, 0, 0, new System.Drawing.Size(width, height));
                }

                // For color actions, highlight the blobs
                int blobCount = 0;
                if (isColorAction)
                {
                    // Save original for debugging
                    var screenshotDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshots");
                    if (!Directory.Exists(screenshotDir))
                        Directory.CreateDirectory(screenshotDir);
                    bitmap.Save(System.IO.Path.Combine(screenshotDir, "preview_original.png"));

                    blobCount = HighlightColorBlobs(bitmap, action.ActionType);

                    // Save processed for debugging
                    bitmap.Save(System.IO.Path.Combine(screenshotDir, "preview_highlighted.png"));
                }

                // Draw crosshair at click point if single point
                if (x > 0 && y > 0 && (x2 == 0 || y2 == 0))
                {
                    using (var g = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        var pen = new System.Drawing.Pen(System.Drawing.Color.Yellow, 2);
                        int cx = x - left;
                        int cy = y - top;
                        g.DrawLine(pen, cx - 10, cy, cx + 10, cy);
                        g.DrawLine(pen, cx, cy - 10, cx, cy + 10);
                    }
                }

                // Show in a popup window
                ShowPreviewWindow(bitmap, action, left, top, isColorAction, blobCount);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not capture preview: {ex.Message}", "Preview Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private int HighlightColorBlobs(System.Drawing.Bitmap bitmap, ActionType actionType)
        {
            int blobCount = 0;
            try
            {
                // Convert to 24bpp RGB format for AForge
                System.Drawing.Bitmap processBitmap = new System.Drawing.Bitmap(bitmap.Width, bitmap.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                using (var g = System.Drawing.Graphics.FromImage(processBitmap))
                {
                    g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                }

                // Apply color filter based on action type
                if (actionType == ActionType.ClickRedBox)
                {
                    var filter = new AForge.Imaging.Filters.ColorFiltering();
                    filter.Red = new AForge.IntRange(140, 255);
                    filter.Green = new AForge.IntRange(0, 60);
                    filter.Blue = new AForge.IntRange(140, 255);
                    filter.ApplyInPlace(processBitmap);
                }
                else
                {
                    var filter = new AForge.Imaging.Filters.YCbCrFiltering();
                    filter.Cb = new AForge.Range(-0.7f, -0.2f);
                    filter.Cr = new AForge.Range(-0.7f, -0.2f);
                    filter.ApplyInPlace(processBitmap);
                }

                // Save filtered image for debugging
                var screenshotDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshots");
                processBitmap.Save(System.IO.Path.Combine(screenshotDir, "preview_filtered.png"));

                // Find blobs - use settings MinBlobSize or default to 10 for preview
                var blobCounter = new AForge.Imaging.BlobCounter();
                blobCounter.FilterBlobs = true;
                int minSize = MainWindow.AppModel?.Settings?.MinBlobSize ?? 10;
                // Use smaller size for preview to be more sensitive
                blobCounter.MinHeight = Math.Max(5, minSize / 2);
                blobCounter.MinWidth = Math.Max(5, minSize / 2);
                blobCounter.ProcessImage(processBitmap);
                var blobs = blobCounter.GetObjectsInformation();
                blobCount = blobs.Length;

                // Draw rectangles on original bitmap
                using (var g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    // Black and white pens for visibility on any background
                    var blackPen = new System.Drawing.Pen(System.Drawing.Color.Black, 5);
                    var whitePen = new System.Drawing.Pen(System.Drawing.Color.White, 3);

                    foreach (var blob in blobs)
                    {
                        // Draw black outline first (thicker)
                        g.DrawRectangle(blackPen, blob.Rectangle);
                        // Draw white outline on top (thinner)
                        g.DrawRectangle(whitePen, blob.Rectangle);

                        // Draw blob info with background
                        var font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold);
                        var text = $"{blob.Area}px";
                        var textSize = g.MeasureString(text, font);

                        // Background for text
                        g.FillRectangle(System.Drawing.Brushes.Black,
                            blob.Rectangle.X, blob.Rectangle.Y - 20, textSize.Width + 4, textSize.Height);
                        g.DrawString(text, font, System.Drawing.Brushes.White, blob.Rectangle.X + 2, blob.Rectangle.Y - 18);
                    }

                    // If no blobs found, draw warning text
                    if (blobCount == 0)
                    {
                        var font = new System.Drawing.Font("Arial", 100, System.Drawing.FontStyle.Bold);
                        var text = "No blobs detected!";
                        var textSize = g.MeasureString(text, font);
                        var x = (bitmap.Width - textSize.Width) / 2;
                        var y = (bitmap.Height - textSize.Height) / 2;

                        // Draw background for text
                        g.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(180, 0, 0, 0)),
                            x - 10, y - 5, textSize.Width + 20, textSize.Height + 10);
                        g.DrawString(text, font, System.Drawing.Brushes.Red, x, y);
                    }
                    else
                    {
                        // Draw summary at top
                        var font = new System.Drawing.Font("Arial", 16, System.Drawing.FontStyle.Bold);
                        var largestBlob = blobs.OrderByDescending(b => b.Area).First();
                        var text = $"Found {blobCount} blob(s). Largest: {largestBlob.Area}px at ({largestBlob.Rectangle.X}, {largestBlob.Rectangle.Y})";
                        g.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(200, 0, 0, 0)), 0, 0, bitmap.Width, 30);
                        g.DrawString(text, font, System.Drawing.Brushes.Yellow, 10, 5);
                    }
                }

                processBitmap.Dispose();
            }
            catch
            {
                // Ignore blob detection errors
            }
            return blobCount;
        }

        private void ShowPreviewWindow(System.Drawing.Bitmap bitmap, MouseActionModel action, int screenX, int screenY, bool isColorAction = false, int blobCount = 0)
        {
            string title = $"Preview: {action.ActionType} at ({screenX}, {screenY})";
            if (isColorAction)
            {
                title += blobCount > 0 ? $" - {blobCount} blob(s) found" : " - NO BLOBS FOUND!";
            }

            var previewWindow = new Window
            {
                Title = title,
                Width = Math.Min(bitmap.Width + 40, 800),
                Height = Math.Min(bitmap.Height + 80, 600),
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            // Convert bitmap to WPF ImageSource
            var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            var image = new Image
            {
                Source = bitmapSource,
                Stretch = Stretch.Uniform
            };

            // Red border if no blobs found for color actions
            var borderColor = (isColorAction && blobCount == 0) ? Brushes.Red : Brushes.Green;

            var border = new Border
            {
                BorderBrush = borderColor,
                BorderThickness = new Thickness(3),
                Margin = new Thickness(10),
                Child = image
            };

            previewWindow.Content = border;
            previewWindow.Show();
        }

        private void fullTasksDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var tooltips = new Dictionary<string, (string header, string tooltip)>
            {
                { "Name", ("Name", "Task name") },
                { "RepeatTaskTimes", ("Rep", "Times to repeat task") },
                { "TimeBetweenTasks", ("Delay", "Delay between task repeats (ms)") },
                { "IgnoreInvSpaces", ("Skip", "Inventory spaces to skip") },
                { "IsPublic", ("Pub", "Public task (shared)") }
            };

            if (tooltips.TryGetValue(e.PropertyName, out var info))
            {
                var headerBlock = new TextBlock
                {
                    Text = info.header,
                    ToolTip = info.tooltip
                };
                e.Column.Header = headerBlock;
            }
        }

        private void deleteOvertaskButton_Click(object sender, RoutedEventArgs e)
        {
            OverTaskSource.Remove(CurrentOverTask);
            overTaskDataGrid.ItemsSource = null;
            overTaskDataGrid.ItemsSource = OverTasks;
            overTaskDataGrid.SelectedItem = OverTasks.CurrentItem;
        }

        private void overTaskNameSearchBox_TextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void overTaskNameSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = e.OriginalSource as TextBox;
            var text = textBox.Text;

            OverTasks.Filter = x =>
            {
                if (string.IsNullOrEmpty(text))
                    return true;

                OverTask task = x as OverTask;

                return task.Name.Contains(text, StringComparison.OrdinalIgnoreCase);
            };
            overTaskDataGrid.IsDropDownOpen = true;
        }

        private async void copyTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentTask == null)
            {
                return;
            }
            var task = CurrentTask.ShallowCopy();
            CurrentOverTask.FullTasks.Add(task);
            await SaveFile();
        }

        private void doWithoutDelayCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            CurrentOverTask.NoDelay = doWithoutDelayCheckbox.IsChecked ?? false;
        }
    }
}
