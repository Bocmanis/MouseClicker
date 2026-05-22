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
using Microsoft.Win32;

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
            CreateGrids();
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
            CurrentTask = (FullTask)fullTasksDataGrid.SelectedItem;
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

        private void insertTaskAbove_Click(object sender, RoutedEventArgs e)
        {
            var newTask = new FullTask()
            {
                MouseActionsId = Guid.NewGuid(),
                MouseActions = new ObservableCollection<MouseActionModel>(),
            };

            int insertIndex = fullTasksDataGrid.SelectedIndex;
            if (insertIndex < 0)
            {
                insertIndex = 0;
            }

            FullTasks.Insert(insertIndex, newTask);
            fullTasksDataGrid.SelectedItem = newTask;
            addMouseActionButton_Click(sender, e);
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            Resetcolumns();
        }

        private void Resetcolumns()
        {
            if (fullTasksDataGrid.Columns.Count > 6)
            {
                fullTasksDataGrid.Columns[0].Visibility = Visibility.Hidden; // MouseActionsId
                fullTasksDataGrid.Columns[1].Visibility = Visibility.Hidden; // MouseActions
                fullTasksDataGrid.Columns[2].Width = new DataGridLength(1, DataGridLengthUnitType.Star); // Name - takes remaining space
                fullTasksDataGrid.Columns[3].Width = 40;  // Rep
                fullTasksDataGrid.Columns[4].Width = 50;  // Delay
                fullTasksDataGrid.Columns[5].Width = 50;  // Skip
                fullTasksDataGrid.Columns[6].Visibility = Visibility.Hidden; // IgnoreInvSpacesList
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

        private void insertMouseActionAbove_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentTask == null)
            {
                return;
            }

            if (CurrentTask.MouseActions == null)
            {
                CurrentTask.MouseActions = new ObservableCollection<MouseActionModel>();
            }

            var newAction = new MouseActionModel()
            {
                ActionType = ActionType.LeftClick,
                Wait = 900,
                WaitDelta = 300,
            };

            int insertIndex = mouseActionsDataGrid.SelectedIndex;
            if (insertIndex < 0)
            {
                insertIndex = 0;
            }

            CurrentTask.MouseActions.Insert(insertIndex, newAction);
            mouseActionsDataGrid.SelectedItem = newAction;
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
            if (CurrentMouseAction.ActionType == ActionType.ClickNearestToCenterColBox)
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

        private static bool IsColorAction(ActionType actionType)
        {
            return actionType == ActionType.ClickRedBox ||
                   actionType == ActionType.ClickGreenBox ||
                   actionType == ActionType.ClickBiggestColBox ||
                   actionType == ActionType.ClickNearestToCenterColBox ||
                   actionType == ActionType.QuickGreenBox ||
                   actionType == ActionType.FindGreens;
        }

        private void ShowActionPreview(MouseActionModel action)
        {
            if (IsColorAction(action.ActionType))
            {
                ShowColorActionPreview(action);
                return;
            }

            int x = action.PointX;
            int y = action.PointY;
            int x2 = action.RcPtX;
            int y2 = action.RcPtY;

            if (x > 0 && y > 0 && x2 > 0 && y2 > 0)
            {
                int left = Math.Min(x, x2);
                int top = Math.Min(y, y2);
                int right = Math.Max(x, x2);
                int bottom = Math.Max(y, y2);
                ShowAreaOverlay(left, top, right, bottom, $"{action.ActionType}");
            }
            else if (x > 0 && y > 0)
            {
                int size = 40;
                ShowAreaOverlay(x - size / 2, y - size / 2, x + size / 2, y + size / 2, $"{action.ActionType}", isCrosshair: true);
            }
            else
            {
                MessageBox.Show("No coordinates set for this action.", "Not Set", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ShowColorActionPreview(MouseActionModel action)
        {
            var imageProcessor = new Logic.ImageProcessingLogic();
            var blobRect = imageProcessor.GetBiggestBlobRectangle(action.ActionType);

            if (blobRect == null)
            {
                MessageBox.Show("No blob detected on screen.", "Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var rect = blobRect.Value;
            ShowAreaOverlay(rect.Left, rect.Top, rect.Right, rect.Bottom, $"{action.ActionType}", useWhiteBorder: true);
        }

        private void ShowAreaOverlay(int left, int top, int right, int bottom, string title, bool isCrosshair = false, bool useWhiteBorder = false)
        {
            var overlayWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                Left = left,
                Top = top,
                Width = right - left,
                Height = bottom - top
            };

            var borderColor = useWhiteBorder
                ? new SolidColorBrush(Color.FromArgb(255, 255, 255, 255))
                : new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            var fillColor = useWhiteBorder
                ? new SolidColorBrush(Color.FromArgb(50, 255, 255, 255))
                : new SolidColorBrush(Color.FromArgb(50, 255, 0, 0));

            var border = new Border
            {
                BorderBrush = borderColor,
                BorderThickness = new Thickness(3),
                Background = fillColor
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

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                overlayWindow.Close();
            };
            timer.Start();

            overlayWindow.Show();
        }



        private void fullTasksDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var tooltips = new Dictionary<string, (string header, string tooltip)>
            {
                { "Name", ("Name", "Task name") },
                { "RepeatTaskTimes", ("Rep", "Times to repeat task") },
                { "TimeBetweenTasks", ("Delay", "Delay between task repeats (ms)") },
                { "IgnoreInvSpaces", ("Skip", "Inventory spaces to skip") }
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

        private void overTaskNameSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = overTaskNameSearchBox.Text;

            if (text.Length < 3)
            {
                searchResultsPopup.IsOpen = false;
                return;
            }

            var filtered = OverTaskSource
                .Where(x => x.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
                .ToList();

            searchResultsListBox.ItemsSource = filtered;
            searchResultsListBox.DisplayMemberPath = "Name";
            searchResultsPopup.IsOpen = filtered.Count > 0;
        }

        private void searchResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = searchResultsListBox.SelectedItem as OverTask;
            if (selected == null) return;

            overTaskDataGrid.SelectedItem = selected;
            searchResultsPopup.IsOpen = false;
            overTaskNameSearchBox.Clear();
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

        private void exportOverTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentOverTask == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                FileName = CurrentOverTask.Name,
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() == true)
            {
                var json = JsonConvert.SerializeObject(CurrentOverTask, Formatting.Indented);
                File.WriteAllText(dialog.FileName, json);
            }
        }

        private void importOverTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var overTask = JsonConvert.DeserializeObject<OverTask>(json);
                    if (overTask == null)
                    {
                        MessageBox.Show("Could not parse the file.", "Import Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    OverTaskSource.Add(overTask);
                    overTaskDataGrid.SelectedItem = overTask;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
