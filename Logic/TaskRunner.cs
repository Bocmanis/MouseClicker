using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            this.WorldHopIncreaser = new Dictionary<string, int>();
            this.ImageProcessor = new ImageProcessingLogic();
        }

        public EventHandler OnInfoChanged;
        public EventHandler OnNewWaitTime;

        public Random Random { get; }
        public bool DoStop { get; internal set; }
        public InputSimulator InputSimulator { get; private set; }
        public Dictionary<MouseActionModel, int> MouseActionIncreaser { get; private set; }
        public Dictionary<string, int> WorldHopIncreaser { get; private set; }
        public ImageProcessingLogic ImageProcessor { get; private set; }
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
                ImageProcessor.SetCondition();
                SetActionConditions(overTask);
                try
                {
                    DoOverTask(overTask);
                }
                catch (Exception ex)
                {
                    DoStop = true;
                    OnInfoChanged($"Task execution failed. Message: {ex.Message}", new EventArgs());
                }
            }
            finally
            {
                this.MouseActionIncreaser = new Dictionary<MouseActionModel, int>();
                this.WorldHopIncreaser = new Dictionary<string, int>();
            }
        }

        private void SetActionConditions(OverTask overTask)
        {
            foreach (var action in overTask.FullTasks.SelectMany(x => x.MouseActions))
            {
                if (action.CheckCondition == ConditionType.Local || action.CheckCondition == ConditionType.LocalIfSame)
                {
                    var topLeft = new Models.Point(action.PointX, action.PointY);
                    var bottomRight = new Models.Point(action.RcPtX, action.RcPtY);

                    ImageProcessor.AddCondition(topLeft, bottomRight);
                }
            }
        }

        private void DoOverTask(OverTask overTask)
        {
            CurrentOverTask = overTask;
            // Reset counts at the start of each overtask iteration
            // Remember.Task persists counts within fulltask repeats, Remember.Always persists across overtask repeats
            var alwaysEntries = MouseActionIncreaser.Where(x => x.Key.Remember == RememberType.Always).ToList();
            MouseActionIncreaser.Clear();
            foreach (var entry in alwaysEntries)
            {
                MouseActionIncreaser[entry.Key] = entry.Value;
            }
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
                        this.FullTaskRepeatText = $"{i + 1}/{fullTask.RepeatTaskTimes}";
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

            if (overTask.RepeatCount > 0 && this.OverTaskCounter < overTask.RepeatCount)
            {
                this.OverTaskCounter++;
                DoOverTask(overTask);
            }
        }

        private async void ExecuteTasks(FullTask fullTask)
        {
            if (string.IsNullOrEmpty(fullTask.IgnoreInvSpaces))
            {
                fullTask.IgnoreInvSpacesList = new List<int>();
            }
            else
            {
                try
                {
                    fullTask.IgnoreInvSpacesList = fullTask.IgnoreInvSpaces.Split(',').Select(x => int.Parse(x.Trim())).ToList();
                }
                catch (Exception ex)
                {
                    DoStop = true;
                    OnInfoChanged($"Failed to parse Skip value '{fullTask.IgnoreInvSpaces}' in fulltask '{fullTask.Name}'. Expected comma-separated numbers. Error: {ex.Message}", new EventArgs());
                    return;
                }
            }

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

                var doTheTask = IsConditionMet(task);
                if (!doTheTask)
                {
                    continue;
                }

                CurrentMouseAction = task;

                var isUntilColor = task.CheckCondition == ConditionType.UntilHasGreen || task.CheckCondition == ConditionType.UntilHasRed;

                do
                {
                    if (task.RepTimes == 0)
                    {
                        this.MouseActionCounterText = "1/1";
                        OnInfoChanged($"Doing single task: {task.TotallyNormalName}", new EventArgs());
                        KeyBoardDown(task.ClickKey);
                        DoClick(task);
                        Thread.Sleep(200);
                        KeyboardUp(task.ClickKey);
                    }
                    else
                    {
                        KeyBoardDown(task.ClickKey);
                        try
                        {
                            for (int i = 0; i < task.RepTimes; i++)
                            {
                                if (DoStop)
                                {
                                    return;
                                }
                                this.MouseActionCounterText = $"{i + 1}/{task.RepTimes}";
                                OnInfoChanged($"Doing rep task: {task.TotallyNormalName}", new EventArgs());
                                DoClick(task);
                                Thread.Sleep(200);
                            }
                        }
                        finally
                        {
                            KeyboardUp(task.ClickKey);
                        }
                    }
                } while (isUntilColor && !DoStop && !IsUntilColorMet(task));

                if (task.Remember == RememberType.No)
                {
                    MouseActionIncreaser.Remove(task);
                }
            }
        }

        private bool IsConditionMet(MouseActionModel task, int tryCount = 1)
        {
            var result = false;
            switch (task.CheckCondition)
            {
                case ConditionType.Global:
                    var isSamePictures = ImageProcessor.IsConditionMet(MainWindow.AppModel.Settings.ConditionLeftTop, MainWindow.AppModel.Settings.ConditionRightBottom);
                    if (!isSamePictures)
                    {
                        result = true;
                    }
                    break;
                case ConditionType.GlobalIfSame:
                    var isSamePictures2 = ImageProcessor.IsConditionMet(MainWindow.AppModel.Settings.ConditionLeftTop, MainWindow.AppModel.Settings.ConditionRightBottom);
                    result = isSamePictures2;
                    break;
                case ConditionType.Local:
                    var topLeft = new Point(task.PointX, task.PointY);
                    var bottomRight = new Point(task.RcPtX, task.RcPtY);
                    var isSamePictures3 = ImageProcessor.IsConditionMet(topLeft, bottomRight);
                    if (!isSamePictures3)
                    {
                        result = true;
                    }
                    break;
                case ConditionType.LocalIfSame:
                    var topLeft2 = new Point(task.PointX, task.PointY);
                    var bottomRight2 = new Point(task.RcPtX, task.RcPtY);
                    var isSamePictures4 = ImageProcessor.IsConditionMet(topLeft2, bottomRight2);
                    result = isSamePictures4;
                    break;
                case ConditionType.UntilHasGreen:
                case ConditionType.UntilHasRed:
                case ConditionType.None:
                default:
                    result = true;
                    break;
            }


            if (!result && task.ActionType == ActionType.WaitForCondition)
            {
                OnInfoChanged($"Waiting for condition. Action = {task.TotallyNormalName} Attempt = {tryCount}", new EventArgs());
                if (tryCount > 20)
                {
                    return true;
                }
                Thread.Sleep(task.Wait);
                tryCount++;
                return IsConditionMet(task, tryCount);
            }
            return result;
        }

        private bool IsUntilColorMet(MouseActionModel task)
        {
            bool isGreen = task.CheckCondition == ConditionType.UntilHasGreen;
            var scanTopLeft = new Point(Math.Min(task.PointX, task.RcPtX), Math.Min(task.PointY, task.RcPtY));
            var scanBottomRight = new Point(Math.Max(task.PointX, task.RcPtX), Math.Max(task.PointY, task.RcPtY));
            var hasColor = ImageProcessor.HasColorInRegion(scanTopLeft, scanBottomRight, isGreen);
            OnInfoChanged($"UntilHas{(isGreen ? "Green" : "Red")}: color {(hasColor ? "found" : "not found")}, {(hasColor ? "proceeding" : "repeating")}", new EventArgs());
            return hasColor;
        }

        private async void DoClick(MouseActionModel task)
        {
            if (task.ActionType == ActionType.FindGreens)
            {
                var stopWatchTotalTime = new Stopwatch();
                if (task.WaitDelta != 0)
                {
                    stopWatchTotalTime.Start();
                }
                while (stopWatchTotalTime.Elapsed.TotalSeconds < task.WaitDelta)
                {

                    CancellationTokenSource source = new CancellationTokenSource();
                    CancellationToken token = source.Token;
                    if (DoStop)
                    {
                        source.Cancel();
                        return;
                    }

                    var colourPoint = ImageProcessor.SearchForGreen(task.Wait, token);
                    MouseActions.DoLeftClick(colourPoint);
                }
                return;
            }
            int waitTime = GetWaitTime(task);

            var sw = new Stopwatch();
            sw.Start();
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
                case ActionType.ClickBiggestColBox:
                case ActionType.ClickNearestToCenterColBox:
                case ActionType.ClickRedBox:
                case ActionType.ClickGreenBox:
                    var swa = Stopwatch.StartNew();
                    Models.Point overrideCenter = null;
                    if (task.ActionType == ActionType.ClickNearestToCenterColBox && task.PointX != 0)
                    {
                        overrideCenter = new Point(task.PointX, task.PointY);
                    }
                    var colourPoint = ImageProcessor.GetColouredBoxPoint(task.ActionType, overrideCenter);
                    if (colourPoint.X == 0)
                    {
                        colourPoint = GetPoint(task);
                    }

                    MouseActions.DoLeftClick(colourPoint);
                    swa.Stop();
                    OnInfoChanged("ClickGreenBoxTime: " + swa.ElapsedMilliseconds, new InfoChangedEventArgs() { GreenBoxTimeMessage = "ClickGreenBoxTime: " + sw.ElapsedMilliseconds });
                    break;
                case ActionType.WorldHop:
                    var worldHopPoint = GetWorldHopPoint(task);
                    MouseActions.DoDoubleLeftClick(worldHopPoint);
                    Thread.Sleep(2000);
                    KeyBoardDown(KeyPresses.Space);
                    Thread.Sleep(200);
                    KeyboardUp(KeyPresses.Space);
                    Thread.Sleep(MainWindow.AppModel.Settings.WorldHopDelayMs ?? 8000);
                    break;
                case ActionType.QuickGreenBox:
                    var swat = Stopwatch.StartNew();
                    var colourPointFast = ImageProcessor.GetColouredBoxPoint(task.ActionType);
                    if (colourPointFast.X == 0)
                    {
                        colourPointFast = GetPoint(task);
                    }

                    MouseActions.DoLeftClick(colourPointFast);
                    swat.Stop();
                    OnInfoChanged("ClickGreenBoxTime: " + swat.ElapsedMilliseconds, new InfoChangedEventArgs() { GreenBoxTimeMessage = "ClickGreenBoxTime: " + sw.ElapsedMilliseconds });
                    break;
                case ActionType.ClosestToCoordinates:
                    var closestPoint = ImageProcessor.FindClosestToCoordinates(task.PointX, task.PointY, task.RcPtX, task.RcPtY);
                    if (closestPoint.X != 0)
                    {
                        MouseActions.DoLeftClick(closestPoint);
                    }
                    break;
                case ActionType.WaitForCondition:
                case ActionType.None:
                default:
                    break;
            }
            sw.Stop();
            Thread.Sleep(waitTime);
        }

        private int GetWaitTime(MouseActionModel task)
        {
            var waitDelta = task.WaitDelta;
            if (CurrentOverTask.NoDelay)
            {
                waitDelta = 0;
            }
            var waitTime = task.Wait + Random.Next(waitDelta);
            OnNewWaitTime(waitTime, new EventArgs());
            return waitTime;
        }

        private Point GetWorldHopPoint(MouseActionModel task)
        {
            var worldCount = MainWindow.AppModel.Settings.WorldHopCount;
            var worldHopLeftTop = MainWindow.AppModel.Settings.WorldHopLeftTop;
            var worldHopRightBottom = MainWindow.AppModel.Settings.WorldHopRightBottom;

            if (worldCount <= 0 || worldHopLeftTop == null || worldHopRightBottom == null)
            {
                OnInfoChanged("WorldHop settings not configured properly. Please set WorldHopCount, WorldHopLeftTop, and WorldHopRightBottom in settings.", new EventArgs());
                return new Point(0, 0);
            }

            var taskName = string.IsNullOrEmpty(task.TotallyNormalName) ? CurrentOverTask.Name : task.TotallyNormalName;

            var spot = 0;
            if (WorldHopIncreaser.ContainsKey(taskName))
            {
                spot = WorldHopIncreaser[taskName];
            }
            else
            {
                WorldHopIncreaser.Add(taskName, spot);
            }
            var row = spot % worldCount;

            var leftTopX = worldHopLeftTop.X;
            var leftTopY = worldHopLeftTop.Y;
            var rightBotX = worldHopRightBottom.X;
            var rightBotY = worldHopRightBottom.Y;

            decimal verticalIncrement = (rightBotY - leftTopY) / (decimal)worldCount;
            var precision = (int)Math.Round(verticalIncrement / 3, 0);

            var minX = precision + leftTopX;
            var maxX = -precision + rightBotX;
            if (minX >= maxX)
            {
                minX = leftTopX;
                maxX = rightBotX;
            }
            var pointX = minX < maxX ? Random.Next(minX, maxX) : minX;

            var yLeftTop = (int)Math.Round(precision + row * verticalIncrement + leftTopY);
            var yRightBottom = (int)Math.Round(-precision + leftTopY + verticalIncrement * (row + 1));
            if (yLeftTop >= yRightBottom)
            {
                yLeftTop = leftTopY + (int)(row * verticalIncrement);
                yRightBottom = leftTopY + (int)((row + 1) * verticalIncrement);
            }
            var pointY = yLeftTop < yRightBottom ? Random.Next(yLeftTop, yRightBottom) : yLeftTop;

            spot++;
            WorldHopIncreaser[taskName] = spot;
            return new Point(pointX, pointY);
        }

        private void KeyboardUp(KeyPresses keyPress)
        {
            if (keyPress != KeyPresses.None)
            {
                switch (keyPress)
                {
                    case KeyPresses.Esc:
                        InputSimulator.Keyboard.KeyUp(VirtualKeyCode.ESCAPE);

                        break;
                    case KeyPresses.Shift:
                        InputSimulator.Keyboard.KeyUp(VirtualKeyCode.SHIFT);


                        break;
                    case KeyPresses.Ctrl:
                        InputSimulator.Keyboard.KeyUp(VirtualKeyCode.CONTROL);

                        break;
                    default:
                        break;
                }
            }
        }

        private void KeyBoardDown(KeyPresses keyPress)
        {
            if (keyPress != KeyPresses.None)
            {
                switch (keyPress)
                {
                    case KeyPresses.Esc:
                        ClickKeyboard(VirtualKeyCode.ESCAPE);

                        break;
                    case KeyPresses.Shift:
                        ClickKeyboard(VirtualKeyCode.SHIFT);

                        break;
                    case KeyPresses.Space:
                        ClickKeyboard(VirtualKeyCode.SPACE);

                        break;
                    case KeyPresses.One:
                        ClickKeyboard(VirtualKeyCode.NUMPAD1);

                        break;
                    case KeyPresses.Two:
                        ClickKeyboard(VirtualKeyCode.NUMPAD2);

                        break;
                    case KeyPresses.Three:
                        ClickKeyboard(VirtualKeyCode.NUMPAD3);

                        break;
                    case KeyPresses.Four:
                        ClickKeyboard(VirtualKeyCode.NUMPAD4);

                        break;
                    case KeyPresses.Ctrl:
                        ClickKeyboard(VirtualKeyCode.CONTROL);

                        break;
                    case KeyPresses.Q:
                        ClickKeyboard(VirtualKeyCode.VK_Q);

                        break;
                    case KeyPresses.W:
                        ClickKeyboard(VirtualKeyCode.VK_W);

                        break;
                    case KeyPresses.E:
                        ClickKeyboard(VirtualKeyCode.VK_E);

                        break;
                    case KeyPresses.R:
                        ClickKeyboard(VirtualKeyCode.VK_R);

                        break;
                    default:
                        break;
                }
            }
        }

        private void ClickKeyboard(VirtualKeyCode key)
        {
            InputSimulator.Keyboard.KeyDown(key);
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

                var horizontalIncrement = (rightBotX - leftTopX) / 4;
                var verticalIncrement = (rightBotY - leftTopY) / 7;
                var precisionModifier = MainWindow.AppModel.Settings.InventoryPrecisionModifier ?? 5;
                task.PointX = precisionModifier + MainWindow.AppModel.Settings.InventoryLeftTop.X + horizontalIncrement * column;
                task.RcPtX = -precisionModifier + MainWindow.AppModel.Settings.InventoryLeftTop.X + horizontalIncrement * (column + 1);
                task.PointY = precisionModifier + MainWindow.AppModel.Settings.InventoryLeftTop.Y + verticalIncrement * row;
                task.RcPtY = -precisionModifier + MainWindow.AppModel.Settings.InventoryLeftTop.Y + verticalIncrement * (row + 1);
                if (task.Increase != 0)
                {
                    int newValue = GetNextValue(task, spot);

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
                var minY = Math.Min(task.PointY, task.RcPtY);
                var maxY = Math.Max(task.PointY, task.RcPtY);

                // Inset by 10% on each side to avoid clicking edges
                var marginX = (int)Math.Ceiling((maxX - minX) * 0.10);
                var marginY = (int)Math.Ceiling((maxY - minY) * 0.10);
                var safeMinX = minX + marginX;
                var safeMaxX = maxX - marginX;
                var safeMinY = minY + marginY;
                var safeMaxY = maxY - marginY;

                // Fallback to center if rectangle is too small for margins
                if (safeMinX >= safeMaxX || safeMinY >= safeMaxY)
                {
                    return new Point((minX + maxX) / 2, (minY + maxY) / 2);
                }

                var pointX = Random.Next(safeMinX, safeMaxX);
                var pointY = Random.Next(safeMinY, safeMaxY);

                return new Point(pointX, pointY);
            }
            var apointX = task.PointX;
            var apointY = task.PointY;

            return new Point(apointX, apointY);
        }

        private int GetNextValue(MouseActionModel task, int spot)
        {
            var newValue = spot + 1 + task.Increase;
            if (newValue > 28)
            {
                newValue = newValue - 28;
            }
            if (CurrentFullTask.IgnoreInvSpacesList.Contains(newValue))
            {
                spot = newValue - 1;
                newValue = GetNextValue(task, spot);
            }
            return newValue;
        }
    }
}
