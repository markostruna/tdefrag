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
using System.Windows.Threading;

namespace TDefragWpf
{
    namespace Library
    {
        /// <summary>
        /// Interaction logic for DiskMap.xaml
        /// </summary>
        public partial class DiskMap : UserControl
        {
            public DiskMap()
            {
                InitializeComponent();
            }

            private void OnLoaded(object sender, RoutedEventArgs e)
            {
                kobila.InitializeComponent();
                //kobila.Reset();
            }

            public void Reset()
            {
                kobila.Reset();
            }

            public void ColorizeSquare(int pos, Color squareBrush)
            {
                kobila.ColorizeSquare(pos, squareBrush);
            }

            public Int64 NumSquares { get { return kobila.NumSquares; } }
        }
    }

    namespace SomethingElse
    {
        class MyVisualHost : FrameworkElement
        {
            DispatcherTimer dispatcherTimer;

            private int stepX = 10;
            private double maxX = 560;
            private double maxY = 330;
            private double edgeX = 560;
            private double edgeY = 330;
            private int numX = 1;
            private int numY = 1;
            private int offsetX = 0;
            private int offsetY = 0;

            bool resetView = true;

            public Int64 NumSquares { get { return numX * numY; } }

            private VisualCollection _children;

            public MyVisualHost()
            {
                _children = new VisualCollection(this);

                dispatcherTimer = new DispatcherTimer();

                dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000 / stepX);
                dispatcherTimer.Start();

                drawing = false;
                resetView = true;
            }

            private void dispatcherTimer_Tick(object sender, EventArgs e)
            {
                if (!resetView)
                    return;

                if (drawing)
                    return;

                InvalidateVisual();
                resetView = false;
            } 

            List<SolidColorBrush> squares;

            public void InitializeComponent()
            {
                maxX = ActualWidth;
                maxY = ActualHeight;

                numX = (int)(maxX / stepX);
                numY = (int)(maxY / stepX);

                offsetX = (int)((maxX - numX * stepX) / 2);
                offsetY = (int)((maxY - numY * stepX) / 2);

                DrawingVisual drawingVisual = new DrawingVisual();

                _children.Add(drawingVisual);

                rect = new Rect();
                
                if (stepX > 3)
                    rect.Size = new Size(stepX - 1, stepX - 1);
                else
                    rect.Size = new Size(stepX, stepX);

                edgeX = offsetX + stepX * numX;
                edgeY = offsetY + stepX * numY;

                Reset();
            }

            public void Reset()
            {
                squares = new List<SolidColorBrush>();

                for (Int32 pos = 0; pos <= NumSquares; pos++)
                {
                    SolidColorBrush squareBrush = new SolidColorBrush(Color.FromRgb(240, 240, 240));

                    squares.Add(squareBrush);
                }

                InvalidateVisual();
            }

            Rect rect;

            bool drawing = false;

            protected override void OnRender(DrawingContext drawingContext)
            {
                if (squares == null)
                    return;

                drawing = true;

                Int32 pos = 0;

                Point xy = new Point();

                for (Int32 y = offsetY; y < edgeY; y = y + stepX)
                {
                    for (Int32 x = offsetX; x < edgeX; x = x + stepX)
                    {
                        xy.X = x;
                        xy.Y = y;

                        rect.Location = xy;

                        drawingContext.DrawRectangle(squares[pos++], (System.Windows.Media.Pen)null, rect);
                    }
                }

                drawing = false;
            }

            public void ColorizeSquare(int pos, Color brushColor)
            {
                if (brushColor != squares[pos].Color)
                {
                    squares[pos] = new SolidColorBrush(brushColor);

                    resetView = true;
                }
            }

            // Provide a required override for the VisualChildrenCount property.
            protected override int VisualChildrenCount
            {
                get { return _children.Count; }
            }

            // Provide a required override for the GetVisualChild method.
            protected override Visual GetVisualChild(int index)
            {
                if (index < 0 || index >= _children.Count)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return _children[index];
            }
        }
    }
}
