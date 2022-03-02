using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Shapes;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Newtonsoft.Json;
/* TODO:
* add robot preview
* add heading,
* actions, 
* stop, 
* and speed
*/


//https://docs.microsoft.com/en-us/dotnet/api/system.windows.shapes.ellipse?view=windowsdesktop-6.0


namespace robotics_app
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Scale: 2cm per pixel
    /// </summary>
    public partial class MainWindow : Window
    {
        private string filePath;
        private string folderPath;
        private int selectedPointIndex = -1;
        private List<pathPoint> pathPoints = new List<pathPoint>();
        private List<ControlPoint> controlPoints = new List<ControlPoint>();
        private Planner planner;
        private double unitCoefficient = 1.0;
        public bool loaded = false;
        private bool moving = false;
        private bool newSelected = false;
        private string cwd = System.AppDomain.CurrentDomain.BaseDirectory;
        private double robotWidth = 0;
        private double robotLength = 0;
        public MainWindow()
        {
            InitializeComponent();
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            Trace.WriteLine(cwd);
            if (File.Exists($"{cwd}\\CreatorSettings.json"))
            {
                string settingsFile = File.ReadAllText($"{cwd}\\CreatorSettings.json");
                JsonDocument document = JsonDocument.Parse(settingsFile);
                JsonElement jsonElement = document.RootElement;
                robotWidth = jsonElement.GetProperty("robotWidth").GetDouble();
                robotLength = jsonElement.GetProperty("robotLength").GetDouble();
                RobotWidth.Text = robotWidth.ToString();
                RobotLength.Text = robotLength.ToString();
            }
            else
            {
                string exportJSON = JsonConvert.SerializeObject(new Settings(robotWidth, robotLength));
                File.WriteAllText($"{cwd}\\CreatorSettings.json", exportJSON);
            }

        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal || this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Minimized;
            }
            else if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }
        }

        /*private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
            }
            else if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
        }*/

        private void WindowTop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
            /*if (e.ClickCount == 1)
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    *//*double CurrentWindowWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
                    double CurrentWindowHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
                    // eventually need to do math to find ratio of current cursor position in terms of its relation to the width
                    // height isnt too important as i can just leave it at the top anyway
                    // then need to "teleport" window to where mouse would be ratio wise after getting normalized
                    this.WindowState = WindowState.Normal;*//*
                }
                else
                {
                    this.DragMove();
                }

            }*/
            /*else if (e.ClickCount == 2)
            {
                if (this.WindowState == WindowState.Normal)
                {
                    this.WindowState = WindowState.Maximized;
                }
                else if (this.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Normal;
                }
            }*/
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
            // TODO: Add save on exit and other optional features later
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            EraseGrid();
            if (this.WindowState == WindowState.Maximized)
            {
                this.BorderThickness = new Thickness(7);
            }
            else if (this.WindowState == WindowState.Normal)
            {
                this.BorderThickness = new Thickness(0);
            }
            //DrawGrid();
        }

        private void Import(string filePath) // setter method
        {
            double x;
            double y;
            double d;
            double theta;
            double speed;
            double heading;
            bool stop;
            double temp;

            string jsonString = File.ReadAllText(filePath);
            JsonDocument document = JsonDocument.Parse(jsonString);
            JsonElement jsonElement = document.RootElement;
            List<ControlPoint> controlPoints_ = new List<ControlPoint>();
            foreach (JsonElement point in jsonElement.EnumerateArray())
            {
                List<string> actions = new List<string>();
                x = point.GetProperty("x").GetDouble();
                y = point.GetProperty("y").GetDouble();
                d = point.GetProperty("d").GetDouble();
                theta = point.GetProperty("theta").GetDouble();
                speed = point.GetProperty("speed").GetDouble();
                heading = point.GetProperty("heading").GetDouble();
                stop = point.GetProperty("stop").GetBoolean();
                foreach (JsonElement action in point.GetProperty("actions").EnumerateArray())
                {
                    actions.Add(action.ToString());
                }
                // swap x and y
                theta += Math.PI / 2;
                heading += Math.PI / 2;
                if (theta > Math.PI)
                {
                    theta -= 2*Math.PI;
                }
                if (heading > Math.PI)
                {
                    heading -= 2*Math.PI;
                }
                temp = x;
                x = y;
                y = -temp;
                ControlPoint controlPoint = new ControlPoint(x, y, theta, d, heading, speed, stop, actions);
                controlPoints_.Add(controlPoint);
            }
            planner = new Planner(controlPoints_);
            pathPoints = planner.updatePath(controlPoints_);
            controlPoints = controlPoints_;


        }

        private void Export()
        {
            List <exportPoint> exportPoints = new List<exportPoint>();
            string path = $"{folderPath}\\{PlanNameTextBox.Text}.json";
            foreach (ControlPoint controlPoint in controlPoints)
            {
                exportPoints.Add(new exportPoint(controlPoint));
            }
            string exportJSON = JsonConvert.SerializeObject(exportPoints);
            File.WriteAllText(path, exportJSON);
            MessageBox.Show($"Path: {PlanNameTextBox.Text}, has been saved in {folderPath}", "Done!");
        }

        private void Refresh()
        {
            (double centerX, double centerY) = GetCenter();
            (double width, double height) = GetDimensions();
            foreach (ControlPoint controlPoint in controlPoints)
            {
                controlPoint.UpdateSubPoints();
            }
            pathPoints = planner.updatePath(controlPoints);
            loaded = true;
            PathCanvas.Children.RemoveRange(1, PathCanvas.Children.Count);
            //Trace.WriteLine("\nControlPoints:");
            for (int i = 0; i < controlPoints.Count; i++)
            {
                ControlPoint selectedPoint = controlPoints[i];
                double x = selectedPoint.x / 2;
                double y = selectedPoint.y / 2;
                double newX = x + centerX;
                double newY = -y + centerY;
                if (i == controlPoints.Count - 1)
                {
                    selectedPoint.stop = true;
                }
                SolidColorBrush pointColor;
                if (selectedPoint.selected)
                {
                    pointColor = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    TransformGroup transformGroup = new TransformGroup();
                    RotateTransform rotateTransform = new RotateTransform()
                    {
                        Angle = selectedPoint.heading * 180 / Math.PI
                    };
                    TranslateTransform translateTransform = new TranslateTransform()
                    {
                        X = x,
                        Y = -y
                    };
                    transformGroup.Children.Add(rotateTransform);
                    transformGroup.Children.Add(translateTransform);

                    Rectangle robotFrame = new Rectangle()
                    {
                        Height = robotLength / 2,
                        Width = robotWidth / 2,
                        Stroke = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                        RenderTransformOrigin = new Point(0.5, 0.5),
                        RenderTransform = transformGroup,
                        StrokeThickness = 2
                    };
                    PathCanvas.Children.Add(robotFrame);

                    TransformGroup lineTransform = new TransformGroup();
                    RotateTransform lineRot = new RotateTransform()
                    {
                        Angle = selectedPoint.heading * 180 / Math.PI,
                        CenterX = x,
                        CenterY = -y
                    };

                    TranslateTransform lineTranslate = new TranslateTransform()
                    {
                        X = x,
                        Y = -y - (0.125 * robotLength)
                    };
                    lineTransform.Children.Add(lineTranslate);
                    lineTransform.Children.Add(lineRot);
                    Rectangle headingIndicator = new Rectangle()
                    {
                        /*X1 = x,
                        X2 = x,
                        Y1 = -y,
                        Y2 = -y - (0.25 * robotLength),*/
                        Height = robotLength / 4,
                        Width = robotWidth / 2,
                        Stroke = new SolidColorBrush(Color.FromRgb(255, 0, 255)),
                        StrokeThickness = 3,
                        RenderTransformOrigin = new Point(0.5, 0.5),
                        RenderTransform = lineTransform

                    };
                    PathCanvas.Children.Add(headingIndicator);
                }
                else
                {
                    pointColor = new SolidColorBrush(Color.FromRgb(47, 208, 65));
                }
                Ellipse ellipse = new Ellipse()
                {
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = new TranslateTransform(x, -y),
                    Height = 25,
                    Width = 25,
                    Stroke = pointColor,
                    StrokeThickness = 3
                     //Fill = new SolidColorBrush(Color.FromRgb(Convert.ToByte(70 + (15 * i)), 25, Convert.ToByte(110 - (15 * i))))

                 };
                this.PathCanvas.Children.Add(ellipse);

                //Trace.WriteLine(selectedPoint.ToString());
            }
            //Trace.WriteLine("\nPathPoints:");

            int j = 1;
            for (int i = 0; i < pathPoints.Count; i++)
            {
                if (j == pathPoints.Count)
                {
                    break;
                }
                double x = pathPoints[i].x / 2;
                double y = pathPoints[i].y / 2;
                Line line = new Line()
                {
                    //RenderTransformOrigin = new Point(0.5, 0.5),
                    //RenderTransform = new TranslateTransform(x, y),
                    X1 = x + centerX,
                    Y1 = -y + centerY,
                    X2 = pathPoints[j].x / 2 + centerX,
                    Y2 = -pathPoints[j].y / 2 + centerY,
                    StrokeThickness = 2,
                    Stroke = new SolidColorBrush(Color.FromRgb(255, 0, 255))
                };
                this.PathCanvas.Children.Add(line);
                j++;
            }
            RefreshUI();
            //Trace.WriteLine(pathPoints[0].ToString());
            //Trace.WriteLine(pathPoints[pathPoints.Count - 1].ToString());
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            if (!newSelected & !loaded & savePathTextBox.Text != "" & PlanNameTextBox.Text != "")
            {
                newSelected = false;
                controlPoints.Add(new ControlPoint(0.0, 0.0, 0.0, 50.0, 0.0, 1.0, false, new List<string>()));
                planner = new Planner(controlPoints);
                Refresh();
            }
            else if (!newSelected)
            {
                MessageBoxResult messageResult = MessageBox.Show("To create a new path please select the save path and the file name.", "Creating New Path...");
                if (messageResult == MessageBoxResult.OK)
                {
                    controlPoints.Clear();
                    pathPoints.Clear();
                    PathCanvas.Children.RemoveRange(1, PathCanvas.Children.Count);
                    RefreshUI();
                    savePathTextBox.Text = "";
                    PlanNameTextBox.Text = "";
                    newSelected = true;
                }

            }
            else
            {
                if (savePathTextBox.Text != "" & PlanNameTextBox.Text != "")
                {
                    newSelected = false;
                    controlPoints.Add(new ControlPoint(0.0, 0.0, 0.0, 50.0, 0.0, 1.0, false, new List<string>()));
                    planner = new Planner(controlPoints);
                    Refresh();
                }
                else
                {
                    MessageBox.Show("To create a new path please select the save path and the file name.", "Creating New Path...");
                }

            }

        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (loaded)
            {
                Export();
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            //Planner planner = new Planner();
            
            if (openFileDialog.ShowDialog() == true)
            {
                // dumbass check;
                filePath = openFileDialog.FileName;
                if (filePath.Substring(filePath.Length - 5, 5) != ".json")
                {
                    MessageBoxResult messageResult = MessageBox.Show("Please select a .json type file.", "User Error!");
                    if (messageResult == MessageBoxResult.OK)
                    {
                        Open_Click(sender, e);
                    }
                }
                else
                {
                    savePathTextBox.Text = filePath;
                    int connectionTerminated = 0; // I'm sorry Elizabeth.
                    int idx = 0;
                    foreach(char c in filePath)
                    {
                        if (c == '\\')
                        {
                            connectionTerminated = idx + 1;
                        }
                        idx++;
                    }
                    PlanNameTextBox.Text = filePath.Substring(connectionTerminated, filePath.Length - connectionTerminated - 5);
                    folderPath = filePath.Substring(0, filePath.Length - PlanNameTextBox.Text.Length - 6);

                    Import(filePath);
                    Refresh();
                }
            }
        }

        private void PathCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var relativePosition = e.GetPosition(PathCanvas);
            if (e.ClickCount == 1)
            {
               //Trace.WriteLine("PathCanvas Click!");
               //var mousePosition = PointToScreen(relativePosition);
               (bool clickedPoint, int pointIndex) = CheckControlPointBounds(relativePosition.X, relativePosition.Y);
                if (moving & selectedPointIndex != -1)
                {
                    (double midX, double midY) = GetCenter();
                    double mouseX = (relativePosition.X - midX) * 2;
                    double mouseY = (-relativePosition.Y + midY) * 2;
                    controlPoints[selectedPointIndex].x = mouseX;
                    controlPoints[selectedPointIndex].y = mouseY;
                    controlPoints[selectedPointIndex].UpdateSubPoints();
                    Refresh();
                }
                else if (clickedPoint) // clicked a control point
                {
                    foreach (ControlPoint point in controlPoints)
                    {
                        point.selected = false;
                    }
                    moving = false;
                    controlPoints[pointIndex].selected = true;
                    selectedPointIndex = pointIndex;
                    Refresh();
                }
                else if (loaded) // check if trying to move control point to a new position if not then deselect the currently selected control-point
                {
                    foreach (ControlPoint point in controlPoints)
                    {
                        point.selected = false;
                    }
                    selectedPointIndex = -1;
                    Refresh();
                }
            }
            else if (e.ClickCount == 2)
            {

            }
        }

        private void MaximizeButton_MouseEnter(object sender, MouseEventArgs e)
        {
            //Brush newBackground = new SolidColorBrush(Color.FromRgb(56, 56, 56));
            //this.MaximizeBRect.Fill = newBackground;
            Brush newBackground = new SolidColorBrush(Color.FromRgb(32, 32, 32));
            this.MaximizeBRect.Fill = newBackground;
        }

        private void MaximizeButton_MouseLeave(object sender, MouseEventArgs e)
        {
            Brush newBackground = new SolidColorBrush(Color.FromRgb(32, 32, 32));
            this.MaximizeBRect.Fill = newBackground;
        }

        private void btnSelectPath_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog openFolderDialog = new FolderBrowserDialog();
            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                savePathTextBox.Text = openFolderDialog.SelectedPath;
                folderPath = openFolderDialog.SelectedPath;
            }
        }

        private (bool, int) CheckControlPointBounds(double mouseX, double mouseY)
        {
            (double midX, double midY) = GetCenter();
            mouseX = (mouseX - midX) * 2;
            mouseY = (-mouseY + midY) * 2;
            int idx = 0;
            //Trace.WriteLine($"MouseX: {mouseX}, MouseY: {mouseY}");
            foreach(ControlPoint point in controlPoints)
            {
                //Trace.WriteLine($"Control {idx} at ({point.x}, {point.y})");
                if (point.x > mouseX - 25 & point.x < mouseX + 25 & point.y > mouseY - 25 & point.y < mouseY + 25)
                {
                    //race.WriteLine($"Clicked a point at index of {idx}");
                    return (true, idx);
                }
                idx++;
            }
            return (false, 0);
        }

        private (double, double) GetCenter()
        {
            return (this.PathCanvas.ActualWidth / 2, this.PathCanvas.ActualHeight / 2);
        }

        private (double, double) GetDimensions()
        {
            return (this.PathCanvas.ActualWidth, this.PathCanvas.ActualHeight);
        }
        private void EraseGrid()
        {
            this.PathCanvas.Children.Clear();
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            if (loaded)
            {
                controlPoints.Insert(selectedPointIndex + 1, new ControlPoint(0.0, 0.0, 0.0, 50.0, 0.0, 1.0, false, new List<string>()));
                Refresh();
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (loaded)
            {

                if (selectedPointIndex != -1)
                {
                    if (MessageBox.Show($"Are you sure you would like to delete your selected control point at an index of {selectedPointIndex}?",
                            "Confirmation",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        controlPoints.RemoveAt(selectedPointIndex);
                        Refresh();
                    }
                }
                else
                {
                    MessageBox.Show("Please select a control point to remove.", "User Error!");
                }
            }
        }

        private void AppendButton_Click(object sender, RoutedEventArgs e)
        {
            if (loaded)
            {
                controlPoints.Add(new ControlPoint(0.0, 0.0, 0.0, 50.0, 0.0, 1.0, true, new List<string>()));
                controlPoints[controlPoints.Count - 2].stop = false;
                Refresh();
            }
        }

        private void UnitSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (UnitSelector.SelectedIndex == 0)
            {
                unitCoefficient = 1.0;
            }
            else if (UnitSelector.SelectedIndex == 1)
            {
                unitCoefficient = 0.01;
            }
            else if (UnitSelector.SelectedIndex == 2)
            {
                unitCoefficient = 0.3937008;
            }
            else
            {
                unitCoefficient = 0.03280839993;
            }
            if (loaded)
            {
                RefreshUI();
            }
        }

        public void RefreshUI()
        {
            if (selectedPointIndex != -1)
            {
                ThetaSlider.Value = Math.Round(controlPoints[selectedPointIndex].theta, 2);
                DValSlider.Value = Math.Round(controlPoints[selectedPointIndex].d, 2);
                xCoordinateBox.Text = Math.Round((controlPoints[selectedPointIndex].x * unitCoefficient), 2).ToString();
                yCoordinateBox.Text = Math.Round((controlPoints[selectedPointIndex].y * unitCoefficient), 2).ToString();
                PointIndexField.Text = selectedPointIndex.ToString();
                HeadingSlider.Value = controlPoints[selectedPointIndex].heading;
                if (moving)
                {
                    MoveButton.Background = System.Windows.Media.Brushes.Green;
                    MoveButtonText.Text = "Moving";
                }
                if (controlPoints[selectedPointIndex].stop)
                {
                    StopButton.Background = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    StopButton.Background = System.Windows.Media.Brushes.Gray;
                }
            }
            else
            {
                ThetaSlider.Value = 0;
                DValSlider.Value = 50;
                SpeedSlider.Value = 1;
                xCoordinateBox.Text = "";
                yCoordinateBox.Text = "";
                PointIndexField.Text = "";
                MoveButton.Background = System.Windows.Media.Brushes.Red;
                MoveButtonText.Text = "Move";
                StopButton.Background = System.Windows.Media.Brushes.Gray;
            }
        }

        private void xCoordinateBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (loaded)
            {
                if (selectedPointIndex != -1)
                {
                    if (Double.TryParse(xCoordinateBox.Text, out double n))
                    {
                        controlPoints[selectedPointIndex].x = n / unitCoefficient;
                        controlPoints[selectedPointIndex].UpdateSubPoints();
                        Refresh();
                    }
                }
            }
        }

        private void yCoordinateBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (loaded)
            {
                if (selectedPointIndex != -1)
                {
                    if (Double.TryParse(yCoordinateBox.Text, out double n))
                    {
                        controlPoints[selectedPointIndex].y = n / unitCoefficient;
                        controlPoints[selectedPointIndex].UpdateSubPoints();
                        Refresh();
                    }
                }
            }
        }

        private void ThetaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (loaded)
            {
                if (selectedPointIndex != -1)
                {
                    controlPoints[selectedPointIndex].theta = ThetaSlider.Value;
                    controlPoints[selectedPointIndex].UpdateSubPoints();
                    Refresh();
                }
            }
        }

        private void DValSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (loaded)
            {
                if (selectedPointIndex != -1)
                {
                    controlPoints[selectedPointIndex].d = DValSlider.Value;
                    controlPoints[selectedPointIndex].UpdateSubPoints();
                    Refresh();
                }
            }
        }

        private void PointIndexField_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (loaded)
            {
                if (int.TryParse(PointIndexField.Text, out int n))
                {
                    if (n >= 0 & n < controlPoints.Count)
                    {
                        selectedPointIndex = n;
                        foreach (ControlPoint point in controlPoints)
                        {
                            point.selected = false;
                        }

                        controlPoints[selectedPointIndex].selected = true;
                        Refresh();
                    }
                }
            }
        }

        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!moving & selectedPointIndex != -1)
            {
                MoveButton.Background = System.Windows.Media.Brushes.Green;
                MoveButtonText.Text = "Moving";
                moving = true;
            }
            else
            {
                MoveButton.Background = System.Windows.Media.Brushes.Red;
                MoveButtonText.Text = "Move";
                moving = false;
            }
        }
        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.M & selectedPointIndex != -1)
            {
                if (!moving)
                {
                    MoveButton.Background = System.Windows.Media.Brushes.Green;
                    MoveButtonText.Text = "Moving";
                    moving = true;
                }
                else
                {
                    MoveButton.Background = System.Windows.Media.Brushes.Red;
                    MoveButtonText.Text = "Move";
                    moving = false;
                }
            }
            if (e.Key == Key.Right & selectedPointIndex != -1)
            {
                if (!moving & selectedPointIndex > 0)
                {
                    selectedPointIndex--;
                    foreach (ControlPoint point in controlPoints)
                    {
                        point.selected = false;
                    }
                    controlPoints[selectedPointIndex].selected = true;
                    Refresh();
                }
                else if (moving)
                {
                    controlPoints[selectedPointIndex].x = ((controlPoints[selectedPointIndex].x * unitCoefficient) + 1) / unitCoefficient;
                    controlPoints[selectedPointIndex].UpdateSubPoints();
                    Refresh();
                }
            }
            if (e.Key == Key.Left & selectedPointIndex != -1)
            {
                if (!moving & selectedPointIndex < controlPoints.Count - 1)
                {
                    selectedPointIndex++;
                    foreach (ControlPoint point in controlPoints)
                    {
                        point.selected = false;
                    }
                    controlPoints[selectedPointIndex].selected = true;
                    Refresh();
                }
                else if (moving)
                {
                    controlPoints[selectedPointIndex].x = ((controlPoints[selectedPointIndex].x * unitCoefficient) - 1) / unitCoefficient;
                    controlPoints[selectedPointIndex].UpdateSubPoints();
                    Refresh();
                }
            }
            if (e.Key == Key.Up & selectedPointIndex != -1)
            {
                if (moving)
                {
                    controlPoints[selectedPointIndex].y = ((controlPoints[selectedPointIndex].y * unitCoefficient) + 1) / unitCoefficient;
                    controlPoints[selectedPointIndex].UpdateSubPoints();
                    Refresh();
                }
            }
            if (e.Key == Key.Down & selectedPointIndex != -1)
            {
                if (moving)
                {
                    controlPoints[selectedPointIndex].y = ((controlPoints[selectedPointIndex].y * unitCoefficient) -1) / unitCoefficient;
                    controlPoints[selectedPointIndex].UpdateSubPoints();
                    Refresh();
                }
            }
        }

        private void HeadingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (loaded)
            {
                if (selectedPointIndex != -1)
                {
                    controlPoints[selectedPointIndex].heading = HeadingSlider.Value;
                    controlPoints[selectedPointIndex].UpdateSubPoints();
                    Refresh();
                }
            }
        }

        private void RobotWidth_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (Double.TryParse(RobotWidth.Text, out double n))
            {
                Trace.WriteLine(robotWidth.ToString());
                robotWidth = n;
                string exportJSON = JsonConvert.SerializeObject(new Settings(robotWidth, robotLength));
                File.WriteAllText($"{cwd}\\CreatorSettings.json", exportJSON);
            }
        }

        private void RobotLength_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (Double.TryParse(RobotLength.Text, out double n))
            {
                robotLength = n ;
                string exportJSON = JsonConvert.SerializeObject(new Settings(robotWidth, robotLength));
                File.WriteAllText($"{cwd}\\CreatorSettings.json", exportJSON);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (loaded)
            {
                if (selectedPointIndex != -1)
                {
                    controlPoints[selectedPointIndex].stop = !controlPoints[selectedPointIndex].stop;
                    if (controlPoints[selectedPointIndex].stop)
                    {
                        StopButton.Background = System.Windows.Media.Brushes.Red;
                    }
                    else
                    {
                        StopButton.Background = System.Windows.Media.Brushes.Gray;
                    }
                    Refresh();
                }
            }
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void ActionsTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveActionButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }

}