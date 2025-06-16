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

            overtaskOnRepeatCheckBox.IsChecked = CurrentOverTask?.OnRepeat;
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

            if (fullTasksDataGrid.Columns.Count != 0)
            {
                fullTasksDataGrid.Columns[0].Visibility = Visibility.Hidden;
                fullTasksDataGrid.Columns[1].Visibility = Visibility.Hidden;
                fullTasksDataGrid.Columns[2].Width = 150;
                fullTasksDataGrid.Columns[6].Visibility = Visibility.Hidden;
            }

            if (mouseActionsDataGrid.Columns.Count != 0)
            {
                mouseActionsDataGrid.Columns[2].Width = 200;
                mouseActionsDataGrid.Columns[7].Width = 156;
                mouseActionsDataGrid.Columns[13].Width = 80;

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
                var repeat = TaskRunner.CurrentOverTask.OnRepeat ? $"RepeatOn, {TaskRunner.OverTaskCounter} times so far" : "Single";
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
        private async void saveConfigButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveFile();
        }

        private async Task SaveFile()
        {
            var text = JsonConvert.SerializeObject(AppModel, Formatting.Indented);
            var path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, FilePath);
            using (StreamWriter outputFile = new StreamWriter(path))
            {
                await outputFile.WriteAsync(text);
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

        private void overtaskOnRepeatCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CurrentOverTask.OnRepeat = overtaskOnRepeatCheckBox.IsChecked ?? false;
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
