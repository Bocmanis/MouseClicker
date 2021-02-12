using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BetterClicker.Logic;
using BetterClicker.Models;
using BetterClicker.Win32Actions;
using BetterClicker.Controls;

namespace BetterClicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public static AppModel AppModel { get; private set; }
        private string FilePath = "saveFile.json";
        public MainWindow()
        {
            InitializeComponent();
            AppModel = GetConfig();
            this.viewBox.Content = new TaskViewxaml();
        }
        private AppModel GetConfig()
        {
            var path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, FilePath);
            if (!File.Exists(path))
            {
                return GetBaseEmptyModel();
            }
            using (StreamReader inputFile = new StreamReader(path))
            {
                var text = inputFile.ReadToEnd();
                var result = JsonConvert.DeserializeObject<AppModel>(text);
                return result;
            }
        }
        private static AppModel GetBaseEmptyModel()
        {
            return new AppModel()
            {
                OverTasks = new ObservableCollection<OverTask>()
                    {
                        new OverTask()
                        {
                            Name = "Init",
                            FullTasks = new ObservableCollection<FullTask>()
                            {
                                new FullTask()
                                {
                                    Name = "Task",
                                    MouseActions= new ObservableCollection<MouseActionModel>()
                                    {
                                        new MouseActionModel()
                                        {
                                            TotallyNormalName = "Wait",
                                            Wait = 1000,
                                            ActionType = ActionType.None,
                                        }
                                    }
                                }
                            }
                        }
                    }
            };
        }

        private void tasksViewButton_Click(object sender, RoutedEventArgs e)
        {
            this.viewBox.Content = new TaskViewxaml();
        }

        private void settingsViewButton_Click(object sender, RoutedEventArgs e)
        {
            this.viewBox.Content = new SettingsUserControl();
        }
    }
}