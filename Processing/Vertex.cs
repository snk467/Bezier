using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Bezier.Processing
{
    public class Vertex
    {
        Ellipse ellipse;
        List<Line> lines;
        public Vertex(int diameter = 10)
        {
            lines = new List<Line>();
            ellipse = new Ellipse
            {
                Height = diameter,
                Width = diameter,
                Fill = Brushes.Red,
                //Stroke = Brushes.Black,
            };
        }
        public Vertex(double x, double y, int diameter = 10) : this(diameter)
        {
            X = x;
            Y = y;
        }
        public double X
        {
            get => (Canvas.GetLeft(ellipse) + ellipse.Width / 2);
            set
            {
                double oldX = X;
                Canvas.SetLeft(ellipse, value - ellipse.Width / 2);
                UpdateLinesX(oldX);
            }
        }
        public double Y
        {
            get => (Canvas.GetTop(ellipse) + ellipse.Height / 2);
            set
            {
                double oldY = Y;
                Canvas.SetTop(ellipse, value - ellipse.Height / 2);
                UpdateLinesY(oldY);
            }
        }
        public Ellipse Ellipse { get => ellipse; }
        private void UpdateLinesY(double oldY)
        {
            foreach (Line line in lines)
            {
                if (line.Y1 == oldY && line.X1 == X)
                {
                    line.Y1 = Y;
                }
                else
                {
                    line.Y2 = Y;
                }
            }
        }
        private void UpdateLinesX(double oldX)
        {
            foreach (Line line in lines)
            {
                if (line.Y1 == Y && line.X1 == oldX)
                {
                    line.X1 = X;
                }
                else
                {
                    line.X2 = X;
                }
            }
        }
        public void AddLine(Line line)
        {
            if (lines.Contains(line))
                return;
            lines.Add(line);
        }
        public void AddToCanvas(Canvas canvas)
        {
            if (canvas == null)
                throw new ArgumentNullException(nameof(canvas));
            if (canvas.Children.Contains(ellipse))
                return;
            foreach (Line line in lines)
            {
                if (canvas.Children.Contains(line))
                    continue;
                canvas.Children.Add(line);
            }
            canvas.Children.Add(ellipse);
        }
        public int CompareTo(Vertex other)
        {
            return (int)Math.Round(Y - other.Y);
        }
        public override string ToString()
        {
            return "(" + X + "," + Y + ")";
        }

    }
}
