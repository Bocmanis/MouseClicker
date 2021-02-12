using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using BetterClicker.Models;
using BetterClicker.Win32Actions;
using WindowsInput;
using WindowsInput.Native;

namespace BetterClicker.Logic
{
    public class TaskRunner
    {
        public TaskRunner()
        {
            this.InputSimulator = new InputSimulator();
            this.Random = new Random(DateTime.Now.Millisecond);
            this.MouseActionIncreaser = new Dictionary<MouseActionModel, int>();
        }

        public EventHandler OnInfoChanged;

        public Random Random { get; }
        public bool DoStop { get; internal set; }
        public InputSimulator InputSimulator { get; private set; }
        public Dictionary<MouseActionModel,int> MouseActionIncreaser { get; private set; }
        public OverTask CurrentOverTask { get; private set; }
        public FullTask CurrentFullTask { get; private set; }
        public string FullTaskRepeatText { get; private set; }
        public int OverTaskCounter { get; private set; }
        public MouseActionModel CurrentMouseAction { get; private set; }
        public string MouseActionCounterText { get; private set; }

        public async void RunTasks(OverTask overTask)
        {
            try
            {
                Thread.Sleep(3000);
                this.OverTaskCounter = 1;
                DoOverTask(overTask);

            }
            finally
            {
                this.MouseActionIncreaser = new Dictionary<MouseActionModel, int>();
            }
        }

        private void DoOverTask(OverTask overTask)
        {
            CurrentOverTask = overTask;
            foreach (var fullTask in overTask.FullTasks)
            {
                CurrentFullTask = fullTask;
                OnInfoChanged($"Starting fulltask: {fullTask.Name}", new EventArgs());
                if (fullTask.RepeatTaskTimes != 0)
                {
                    for (int i = 0; i < fullTask.RepeatTaskTimes; i++)
                    {
                        if (DoStop)
                        {
                            return;
                        }
                        this.FullTaskRepeatText = $"{i+1}/{fullTask.RepeatTaskTimes}";
                        ExecuteTasks(fullTask);
                        Thread.Sleep(fullTask.TimeBetweenTasks);
                    }
                }
                else
                {
                    this.FullTaskRepeatText = $"1/1";
                    ExecuteTasks(fullTask);
                }
                OnInfoChanged($"Task finished: {fullTask.Name}", new EventArgs());
            }
            OnInfoChanged($"Overtask finished: {overTask.Name}", new EventArgs());

            if (DoStop)
            {
                return;
            }

            if (overTask.OnRepeat)
            {
                this.OverTaskCounter++;
                DoOverTask(overTask);
            }
        }

        private async void ExecuteTasks(FullTask fullTask)
        {
            OnInfoChanged($"Starting {fullTask.Name}", new EventArgs());
            if (DoStop)
            {
                return;
            }
            foreach (var task in fullTask.MouseActions)
            {
                if (DoStop)
                {
                    return;
                }
                CurrentMouseAction = task;
                KeyBoardDown(task);
                if (task.RepTimes == 0)
                {
                    this.MouseActionCounterText = "1/1";
                    OnInfoChanged($"Did {task.TotallyNormalName}", new EventArgs());
                    DoClick(task);
                }
                else
                {
                    for (int i = 0; i < task.RepTimes; i++)
                    {
                        if (DoStop)
                        {
                            return;
                        }
                        this.MouseActionCounterText = $"{i+1}/{task.RepTimes}";
                        OnInfoChanged($"Did {task.TotallyNormalName}", new EventArgs());
                        DoClick(task);
                    }
                }

                KeyboardUp(task);
                if (!task.Remember)
                {
                    MouseActionIncreaser.Remove(task);
                }
                OnInfoChanged($"Did {task.TotallyNormalName}", new EventArgs());
               
            }
        }

        private void DoClick(MouseActionModel task)
        {
            switch (task.ActionType)
            {
                case ActionType.RightClick:
                    MouseActions.DoRightClick(GetPoint(task));
                    break;
                case ActionType.LeftClick:
                    MouseActions.DoLeftClick(GetPoint(task));
                    break;
                case ActionType.DoubleRightClick:
                    MouseActions.DoDoubleRightClick(GetPoint(task));
                    break;
                case ActionType.DoubleLeftClick:
                    var point = GetPoint(task);
                    MouseActions.DoDoubleLeftClick(point);
                    break;
                case ActionType.DrinkAllPot:
                    MouseActions.DoQuadLeftClick(GetPoint(task));

                    break;
                default:
                    break;
            }
            Thread.Sleep(task.Wait + Random.Next(task.WaitDelta));
        }

        private void KeyboardUp(MouseActionModel task)
        {
            if (task.ClickKey != KeyPresses.None)
            {
                switch (task.ClickKey)
                {
                    case KeyPresses.Esc:
                        InputSimulator.Keyboard.KeyUp(VirtualKeyCode.ESCAPE);

                        break;
                    case KeyPresses.Shift:
                        InputSimulator.Keyboard.KeyUp(VirtualKeyCode.SHIFT);

                        break;
                    default:
                        break;
                }
            }
        }

        private void KeyBoardDown(MouseActionModel task)
        {
            if (task.ClickKey != KeyPresses.None)
            {
                switch (task.ClickKey)
                {
                    case KeyPresses.Esc:
                        InputSimulator.Keyboard.KeyDown(VirtualKeyCode.ESCAPE);

                        break;
                    case KeyPresses.Shift:
                        InputSimulator.Keyboard.KeyDown(VirtualKeyCode.SHIFT);

                        break;
                    default:
                        break;
                }
            }
        }

        private Point GetPoint(MouseActionModel task)
        {
            if (task.InventorySpot != 0)
            {
                var spot = task.InventorySpot;
                if (MouseActionIncreaser.ContainsKey(task))
                {
                    spot = MouseActionIncreaser[task];
                }
                spot = spot - 1; //remove 1 to start at 0
                var row = spot / 4;
                var column = spot % 4;

                var leftTopX = MainWindow.AppModel.Settings.InventoryLeftTop.X;
                var leftTopY = MainWindow.AppModel.Settings.InventoryLeftTop.Y;
                var rightBotX = MainWindow.AppModel.Settings.InventoryRightBottom.X;
                var rightBotY = MainWindow.AppModel.Settings.InventoryRightBottom.Y;

                var horizontalIncrement = (rightBotX - leftTopX)/4;
                var verticalIncrement = (rightBotY - leftTopY)/7;
                var precisionModifier = MainWindow.AppModel.Settings.InventoryPrecisionModifier ?? 5;
                task.PointX = precisionModifier + MainWindow.AppModel.Settings.InventoryLeftTop.X + horizontalIncrement * column;
                task.RcPtX = -precisionModifier + MainWindow.AppModel.Settings.InventoryLeftTop.X + horizontalIncrement * (column + 1);
                task.PointY = precisionModifier + MainWindow.AppModel.Settings.InventoryLeftTop.Y + verticalIncrement * row;
                task.RcPtY = -precisionModifier + MainWindow.AppModel.Settings.InventoryLeftTop.Y + verticalIncrement * (row + 1);
                if (task.Increase != 0)
                {
                    var newValue = spot + 1 + task.Increase;
                    if (newValue > 28)
                    {
                        newValue = newValue - 28;
                    }
                    this.MouseActionIncreaser[task] = newValue;
                }
            }
            if (task.PointX == 0)
            {
                return new Point(0, 0);
            }
            if (task.RcPtX != 0)
            {
                var minX = Math.Min(task.PointX, task.RcPtX);
                var maxX = Math.Max(task.PointX, task.RcPtX);

                var pointX = Random.Next(minX, maxX);

                var minY = Math.Min(task.PointY, task.RcPtY);
                var maxY = Math.Max(task.PointY, task.RcPtY);

                var pointY = Random.Next(minY, maxY);

                return new Point(pointX, pointY);
            }
            var apointX = task.PointX;
            var apointY = task.PointY;

            return new Point(apointX, apointY);
        }
    }
}
