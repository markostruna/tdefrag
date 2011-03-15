using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TDefragLib;
using System.Threading;
using System.Windows.Threading;
using TDefragWpf.Lib.Common;

namespace TDefragWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            defragLib = new MainLib(this);

            FillDiskArray();

            InitCanvas();
        }

        private Thread defragThread;

        private void startDefrag(object sender, RoutedEventArgs e)
        {
            defragLog.Text = String.Empty;

            defragThread = new Thread(Defrag);
            defragThread.Name = "Defrag Engine";
            defragThread.Priority = ThreadPriority.Normal;

            defragThread.Start(diskArray.Text);
        }

        private void Defrag(object driveObject)
        {
            String drive = (driveObject is String) ? (String)driveObject : "C";

            try
            {
                defragLib.StartDefrag(drive);
            }
            catch (Exception e1)
            {
                AddLine(e1.Message);
            }
        }

        public void AddLine(String line)
        {
            line = line.Replace("\r", String.Empty);
            line = line.Replace("\n", String.Empty);

            this.Dispatcher.BeginInvoke(new Action(delegate()
            {
                if (String.IsNullOrEmpty(defragLog.Text))
                {
                    defragLog.Text = line;
                }
                else
                {
                    defragLog.Text = String.Concat(defragLog.Text, Environment.NewLine, line);
                }
            }), DispatcherPriority.Send);
        }

        private void FillDiskArray()
        {
            String[] DriveList = Environment.GetLogicalDrives();

            foreach (String drive in DriveList)
                diskArray.Items.Add(drive);

            diskArray.SelectedIndex = 0;
        }

        private int stepX = 10;
        private int maxX = 560;
        private int maxY = 330;
        private int numX = 1;
        private int numY = 1;

        Dictionary<Int32, Int32> squares;

        private void InitCanvas()
        {
            //canvas1.Background = Brushes.Aqua;

            double wid = maxX;
            double hei = maxY;

            numX = maxX / stepX;
            numY = maxY / stepX;

            squares = new Dictionary<int, int>(numX * numY);

            for (int x = 0; x < numX; x++)
            {
                for (int y = 0; y < numY; y++)
                {
                    LinearGradientBrush myLinearGradientBrush = new LinearGradientBrush();

                    myLinearGradientBrush.StartPoint = new Point(0, 0);
                    myLinearGradientBrush.EndPoint = new Point(0, 1);

                    myLinearGradientBrush.GradientStops.Add(new GradientStop(Colors.LightGray, 0.0));
                    myLinearGradientBrush.GradientStops.Add(new GradientStop(Colors.White, 0.95));
                    myLinearGradientBrush.GradientStops.Add(new GradientStop(Colors.LightGray, 1.00));

                    Rectangle rec = new Rectangle();

                    rec.RadiusX = 0;
                    rec.RadiusY = 0;
                    rec.Width = stepX;
                    rec.Height = stepX;
                    rec.Fill = myLinearGradientBrush;
                    rec.StrokeThickness = 0;
                    rec.Stroke = Brushes.Transparent;

                    Int32 idx = canvas1.Children.Add(rec);

                    squares.Add(y * numX + x, idx);

                    Canvas.SetTop(rec, y * stepX);
                    Canvas.SetLeft(rec, x * stepX);
                }
            }
        }

        public void SetClusterState(UInt64 clusterNumber, UInt64 numClusters, eClusterState state)
        {
            SetClusterState(clusterNumber, clusterNumber, numClusters, state);
        }

        public void SetClusterState(UInt64 clusterNumber, UInt64 nextCluster, UInt64 numClusters, eClusterState state)
        {
            this.Dispatcher.BeginInvoke(new Action(delegate()
            {
                int max = numX * numY;

                int pos1 = (Int32)((double)max * (double)((double)clusterNumber / (double)numClusters));
                int pos2 = (Int32)((double)max * (double)((double)nextCluster / (double)numClusters));

                for (int pos = pos1; pos <= pos2; pos++)
                {
                    colorizeSquare(state, pos);
                }

            }), DispatcherPriority.Send);
        }

        private void colorizeSquare(eClusterState state, int pos)
        {
            if ((pos < 0) || (pos > squares.Count))
                return;

            if ((squares[pos] < 0) || (squares[pos] > canvas1.Children.Count))
                return;

            Shape square = (Shape)canvas1.Children[squares[pos]];

            Brush brush = square.Fill;

            if (brush is LinearGradientBrush)
            {
                LinearGradientBrush myLinearGradientBrush = (LinearGradientBrush)brush;
                Color color = Colors.White;

                switch (state)
                {
                    case eClusterState.Allocated:
                        color = Colors.LightBlue;
                        break;
                    case eClusterState.Busy:
                        color = Colors.Blue;
                        break;
                    case eClusterState.Error:
                        color = Colors.Red;
                        break;
                    case eClusterState.Fragmented:
                        color = Colors.Orange;
                        break;
                    case eClusterState.Free:
                        color = Colors.LightGray;
                        break;
                    case eClusterState.Mft:
                        color = Colors.Pink;
                        break;
                    case eClusterState.SpaceHog:
                        color = Colors.DarkCyan;
                        break;
                    case eClusterState.Unfragmented:
                        color = Colors.Green;
                        break;
                    case eClusterState.Unmovable:
                        color = Colors.Yellow;
                        break;
                }

                Double radius = 2;
                Double gradientEndPointX = 1;

                byte darkness = 30;
                byte brightness = 20;

                if (state == eClusterState.Free)
                {
                    darkness = 40;
                    brightness = 0;
                    radius = 0;
                    gradientEndPointX = 0;
                }

                Color darkColor = Color.Subtract(color, Color.FromArgb(255, darkness, darkness, darkness));
                Color brightColor = Color.Add(color, Color.FromArgb(255, brightness, brightness, brightness));
                Color endColor = brightColor;

                if (state == eClusterState.Free)
                {
                    Color tempColor = darkColor;

                    darkColor = brightColor;
                    brightColor = tempColor;
                    endColor = darkColor;
                }

                Point endPoint = myLinearGradientBrush.EndPoint;
                endPoint.X = gradientEndPointX;

                myLinearGradientBrush.EndPoint = endPoint;

                myLinearGradientBrush.GradientStops[0].Color = darkColor;
                myLinearGradientBrush.GradientStops[1].Color = brightColor;
                myLinearGradientBrush.GradientStops[2].Color = endColor;

                if (square is Rectangle)
                {
                    Rectangle rec = (Rectangle)square;

                    rec.RadiusX = radius;
                    rec.RadiusY = radius;
                }

                square.Fill = myLinearGradientBrush;
            }

            canvas1.Children[squares[pos]] = square;
        }

        private MainLib defragLib;

    }
}
