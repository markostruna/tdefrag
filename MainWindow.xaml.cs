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
using TDefragWpf.Library.Common;
using System.Runtime.InteropServices;
using System.Windows.Interop;

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

            if (this.WindowStyle == System.Windows.WindowStyle.SingleBorderWindow)
            {
                Thickness t = new Thickness(0);
                MainBorder.BorderThickness = t;

                MainGrid.RowDefinitions[0].Height = new GridLength(0);
            }

            defragLib = new MainLibrary(this);

            FillDiskArray();
        }

        private Thread defragThread;

        private void startDefrag(object sender, RoutedEventArgs e)
        {
            defragLog.Text = String.Empty;

            defragThread = new Thread(Defrag);
            defragThread.Name = "Defrag Engine";
            defragThread.Priority = ThreadPriority.Normal;

            defragThread.Start(diskArray.Text);
            progressBar1.Visibility = System.Windows.Visibility.Visible;
            progressBar1.Value = 0.0;
        }

        private void Defrag(object driveObject)
        {
            String drive = (driveObject is String) ? (String)driveObject : "C";
            Int64 numSquares = numX * numY;

            try
            {
                defragLib.StartDefrag(drive, numSquares);
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

        public void UpdateProgress(Double progress)
        {
            this.Dispatcher.BeginInvoke(new Action(delegate()
            {
                progressBar1.Value = progress;
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
        private double maxX = 560;
        private double maxY = 330;
        private int numX = 1;
        private int numY = 1;

        Dictionary<Int32, Int32> squares;

        private void InitCanvas()
        {
            maxX = canvas1.ActualWidth;
            maxY = canvas1.ActualHeight;

            double wid = maxX;
            double hei = maxY;

            numX = (int)(maxX / stepX);
            numY = (int)(maxY / stepX);

            int offsetX = (int)((maxX - numX * stepX) / 2);
            int offsetY = (int)((maxY - numY * stepX) / 2);

            squares = new Dictionary<int, int>(numX * numY);

            for (int x = 0; x < numX; x++)
            {
                for (int y = 0; y < numY; y++)
                {
                    SolidColorBrush myBrush = new SolidColorBrush(Color.FromRgb(245, 245, 245));

                    LinearGradientBrush myLinearGradientBrush = new LinearGradientBrush();

                    myLinearGradientBrush.StartPoint = new Point(0, 0);
                    myLinearGradientBrush.EndPoint = new Point(0, 1);

                    myLinearGradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(240, 240, 240), 0.0));
                    myLinearGradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(240, 240, 240), 0.95));
                    myLinearGradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(240, 240, 240), 1.00));
                    //myLinearGradientBrush.GradientStops.Add(new GradientStop(Colors.LightGray, 0.0));
                    //myLinearGradientBrush.GradientStops.Add(new GradientStop(Colors.White, 0.95));
                    //myLinearGradientBrush.GradientStops.Add(new GradientStop(Colors.LightGray, 1.00));

                    Rectangle rec = new Rectangle();

                    rec.RadiusX = 0;
                    rec.RadiusY = 0;
                    rec.Width = stepX - 1;
                    rec.Height = stepX - 1;
                    rec.Fill = myLinearGradientBrush;
                    rec.StrokeThickness = 0;
                    rec.Stroke = Brushes.Transparent;

                    Int32 idx = canvas1.Children.Add(rec);

                    squares.Add(y * numX + x, idx);

                    Canvas.SetLeft(rec, offsetX + x * stepX);
                    Canvas.SetTop(rec, offsetY + y * stepX);
                }
            }
        }

        bool initialized = false;

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (!initialized)
            {
                InitCanvas();

                initialized = true;
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
                double clustersPerSquare = (double)((double)numClusters / (double)max);

                int pos1 = (Int32)((double)clusterNumber / clustersPerSquare);
                int pos2 = (Int32)((double)nextCluster / clustersPerSquare);

                UInt64 clusterNum1 = clusterNumber;

                for (int pos = pos1; pos <= pos2; pos++)
                {
                    UInt64 clusterNum2 = (UInt64)(clusterNum1 + clustersPerSquare - 1);

                    if (clusterNum2 > numClusters)
                    {
                        clusterNum2 = numClusters - 1;
                    }

                    //eClusterState maxClusterState = defragLib.GetMaxState(clusterNum1, clusterNum2);
                    eClusterState maxClusterState = state;
                    colorizeSquare(maxClusterState, pos);

                    clusterNum1 = clusterNum2 + 1;
                }

            }), DispatcherPriority.Send);
                }

        public void SetClusterState(UInt64 pos, eClusterState state)
        {
            this.Dispatcher.BeginInvoke(new Action(delegate()
            {
                colorizeSquare(state, (Int32)pos);
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
                        color = Color.FromRgb(240, 240, 240);
                        break;
                    case eClusterState.Mft:
                        color = Colors.Pink;
                        break;
                    case eClusterState.SpaceHog:
                        color = Colors.DarkCyan;
                        break;
                    case eClusterState.Unfragmented:
                        color = Colors.LightGreen;
                        break;
                    case eClusterState.Unmovable:
                        color = Colors.Yellow;
                        break;
                }

                Double radius = 1;
                Double gradientEndPointX = 1;

                byte brightness = 50;
                byte darkness = 60;

                if (state == eClusterState.Free)
                {
                    darkness = 40;
                    brightness = 0;
                    radius = 0;
                    gradientEndPointX = 0;
                }

                Color brightColor = Color.Add(color, Color.FromArgb(255, brightness, brightness, brightness));
                Color darkColor = Color.Subtract(color, Color.FromArgb(255, darkness, darkness, darkness));
                Color endColor = darkColor;

                if (state == eClusterState.Free)
                {
                    Color tempColor = darkColor;

                    darkColor = brightColor;
                    brightColor = tempColor;
                    endColor = brightColor;
                }

                Point endPoint = myLinearGradientBrush.EndPoint;
                endPoint.X = gradientEndPointX;

                myLinearGradientBrush.EndPoint = endPoint;

                myLinearGradientBrush.GradientStops[1].Color = color;
                myLinearGradientBrush.GradientStops[0].Color = color;
                myLinearGradientBrush.GradientStops[2].Color = color;

                //myLinearGradientBrush.GradientStops[1].Color = brightColor;
                //myLinearGradientBrush.GradientStops[0].Color = darkColor;
                //myLinearGradientBrush.GradientStops[2].Color = endColor;

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

        private MainLibrary defragLib;

        [DllImport("DwmApi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins pMarInset);

        [StructLayout(LayoutKind.Sequential)]
        public struct Margins
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern void DwmEnableBlurBehindWindow(IntPtr hwnd, ref DWM_BLURBEHIND blurBehind);

        public enum DwmBlurBehindDwFlags
        {
            Dwm_bb_enable = 0x00000001,
            Dwm_bb_blurRegion = 0x00000002,
            Dwm_bb_transitionOnMaximized = 0x00000004
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DWM_BLURBEHIND
        {
            public DwmBlurBehindDwFlags dwFlags;
            public bool fEnable;
            public IntPtr hRgnBlur;
            public bool fTransitionOnMaximized;
        }

        public void EnableBlurBehind(IntPtr hwnd)
        {
            // Create and populate the Blur Behind structure
            DWM_BLURBEHIND bb = new DWM_BLURBEHIND();

            // Disable Blur Behind and Blur Region;
            bb.dwFlags = DwmBlurBehindDwFlags.Dwm_bb_enable;
            bb.fEnable = true;
            bb.hRgnBlur = IntPtr.Zero;

            // Disable Blur Behind
            DwmEnableBlurBehindWindow(hwnd, ref bb);
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Obtain the window handle for WPF application
                IntPtr mainWindowPtr = new WindowInteropHelper(this).Handle;
                HwndSource mainWindowSrc = HwndSource.FromHwnd(mainWindowPtr);
                mainWindowSrc.CompositionTarget.BackgroundColor = Color.FromArgb(255, 255, 255, 255);

                // Get System Dpi
                System.Drawing.Graphics desktop = System.Drawing.Graphics.FromHwnd(mainWindowPtr);
                float DesktopDpiX = desktop.DpiX;
                float DesktopDpiY = desktop.DpiY;

                // Set Margins
                Margins margins = new Margins();

                // Extend glass frame into client area
                // Note that the default desktop Dpi is 96dpi. The  margins are
                // adjusted for the system Dpi.
                margins.cxLeftWidth = Convert.ToInt32(-1 * (DesktopDpiX / 96));
                margins.cxRightWidth = Convert.ToInt32(-1 * (DesktopDpiX / 96));
                margins.cyTopHeight = Convert.ToInt32(-1 * (DesktopDpiX / 96));
                margins.cyBottomHeight = Convert.ToInt32(-1 * (DesktopDpiX / 96));

                //int hr = DwmExtendFrameIntoClientArea(mainWindowSrc.Handle, ref margins);

                //if (hr < 0)
                //{
                //    Brush br = Application.Current.MainWindow.Background;

                //    if (br is LinearGradientBrush)
                //    {
                //        LinearGradientBrush b1 = br as LinearGradientBrush;

                //        b1.GradientStops[0].Color = Color.FromArgb(255, 255, 255, 255);
                //        b1.GradientStops[1].Color = Color.FromArgb(255, 0, 0, 128);
                //        b1.GradientStops[2].Color = Color.FromArgb(255, 0, 0, 128);
                //        b1.GradientStops[3].Color = Color.FromArgb(255, 255, 255, 255);
                //    }
                //    //DwmExtendFrameIntoClientArea Failed
                //}

                //EnableBlurBehind(mainWindowSrc.Handle);
            }
            // If not Vista, paint background white.
            catch (DllNotFoundException)
            {
                Application.Current.MainWindow.Background = Brushes.White;
            }
        }

        private void CloseButtonMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Brush br = CloseBorder.Background;

            if (br is LinearGradientBrush)
            {
                LinearGradientBrush brush = br as LinearGradientBrush;

                brush.GradientStops[0].Color = Color.FromArgb(255, 85, 0, 0);
                brush.GradientStops[1].Color = Color.FromArgb(255, 255, 0, 0);
                brush.GradientStops[2].Color = Color.FromArgb(255, 85, 0, 0);
            }

            CloseButton.Foreground = Brushes.White;
        }

        private void CloseButtonMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void CloseButtonMouseEnter(object sender, MouseEventArgs e)
        {
            LinearGradientBrush brush = new LinearGradientBrush();

            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(0, 1);

            CloseBorder.BorderBrush = Brushes.Black;
            CloseBorder.Background = brush;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                brush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 85, 0, 0), 0.0));
                brush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 255, 0, 0), 0.5));
                brush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 85, 0, 0), 1.0));

                CloseButton.Foreground = Brushes.White;
            }
            else
            {
                brush.GradientStops.Add(new GradientStop(Colors.White, 0.0));
                brush.GradientStops.Add(new GradientStop(Colors.Gray, 0.5));
                brush.GradientStops.Add(new GradientStop(Colors.White, 1.0));

                CloseButton.Foreground = Brushes.DarkRed;
            }
            //Brush br = CloseBorder.Background;

            //if (br is LinearGradientBrush)
            //{
            //    LinearGradientBrush brush = br as LinearGradientBrush;

            //    if (e.LeftButton == MouseButtonState.Pressed)
            //    {
            //        brush.GradientStops[0].Color = Color.FromArgb(255, 85, 0, 0);
            //        brush.GradientStops[1].Color = Color.FromArgb(255, 255, 0, 0);
            //        brush.GradientStops[2].Color = Color.FromArgb(255, 85, 0, 0);
            //    }
            //    else
            //    {
            //        brush.GradientStops[0].Color = Color.FromArgb(255, 255, 0, 0);
            //        brush.GradientStops[1].Color = Color.FromArgb(255, 85, 0, 0);
            //        brush.GradientStops[2].Color = Color.FromArgb(255, 255, 0, 0);
            //    }
            //}
        }

        private void CloseButtonMouseLeave(object sender, MouseEventArgs e)
        {
            CloseBorder.BorderBrush = Brushes.Transparent;
            CloseBorder.Background = Brushes.Transparent;
            CloseButton.Foreground = Brushes.Gray;

            //Brush br = CloseBorder.Background;

            //if (br is LinearGradientBrush)
            //{
            //    LinearGradientBrush brush = br as LinearGradientBrush;

            //    brush.GradientStops[0].Color = Color.FromArgb(255, 85, 0, 0);
            //    brush.GradientStops[1].Color = Color.FromArgb(255, 170, 102, 102);
            //    brush.GradientStops[2].Color = Color.FromArgb(255, 85, 0, 0);
            //}
        }

        private void MaximizeButtonMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Maximized;
        }

        private void ChangeViewButtonMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Normal;
        }

        private void MinimizeButtonMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void DragableGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
