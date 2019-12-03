using Bezier.Processing;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Bezier.ViewModel
{
    public class ControlsViewModel
    {
        readonly ParametersViewModel parameters;
        readonly TextBox numberOfPointsTextBox;
        readonly Button generateButton;
        readonly Button loadPolylineButton;
        readonly Button savePolylineButton;
        readonly Button imageLoadButton;
        readonly Button startButton;
        readonly Button stopButton;
        Image thumbnialImage;
        Drawer drawer;
        readonly Canvas polylineLayerCanvas;
        private Canvas imageLayerCanvas;
        Vertex movingPoint;
        DispatcherTimer animationTimer;

        public ControlsViewModel(ParametersViewModel parameters, Canvas polylineLayerCanvas, Canvas imageLayerCanvas, TextBox numberOfPointsTextBox, Button generateButton, Button loadPolylineButton, Button savePolylineButton, Button imageLoadButton, Button startButton, Button stopButton, Image thumbnialImage)
        {
            this.parameters = parameters;
            this.numberOfPointsTextBox = numberOfPointsTextBox;
            this.generateButton = generateButton;
            this.loadPolylineButton = loadPolylineButton;
            this.savePolylineButton = savePolylineButton;
            this.imageLoadButton = imageLoadButton;
            this.startButton = startButton;
            this.stopButton = stopButton;
            this.thumbnialImage = thumbnialImage;
            this.polylineLayerCanvas = polylineLayerCanvas;
            this.imageLayerCanvas = imageLayerCanvas;
            
            SetEvents();
        }

        private void animationTimerCallback(object sender, EventArgs e)
        {
            if (parameters.Animation == Model.Animation.Move)
                drawer.MoveImageToNextPosition(10);
            else
            {
                drawer.RotateImage(Math.PI/180);
            }
        }

        private void SetEvents()
        {
            generateButton.Click += GenerateButton_Click;
            loadPolylineButton.Click += LoadPolylineButton_Click;
            savePolylineButton.Click += SavePolylineButton_Click;
            imageLoadButton.Click += ImageLoadButton_Click;
            startButton.Click += StartButton_Click;
            stopButton.Click += StopButton_Click;
            polylineLayerCanvas.Loaded += Canvas_Loaded;
            polylineLayerCanvas.MouseMove += Canvas_MouseMove;
            polylineLayerCanvas.MouseLeave += Canvas_MouseLeave;
            polylineLayerCanvas.MouseUp += Canvas_MouseUp;
            parameters.PropertyChanged += Parameters_PropertyChanged;
        }

        private void Parameters_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(parameters.IsPolylineVisible))
            {
                if(parameters.IsPolylineVisible)
                {
                    EnablePolylineCanvasChlidren();
                }
                else
                {
                    DisablePolylineCanvasChlidren();    
                }
            }
            if(e.PropertyName == nameof(parameters.IsGrayColors))
            {
                drawer.MoveImageToNextPosition(0);
            }
        }

        private void DisablePolylineCanvasChlidren()
        {
            for (int i = 0; i < polylineLayerCanvas.Children.Count; i++)
            {
                polylineLayerCanvas.Children[i].Visibility = Visibility.Hidden;
                polylineLayerCanvas.Children[i].IsHitTestVisible = false;
            }
        }

        private void EnablePolylineCanvasChlidren()
        {
            for (int i = 0; i < polylineLayerCanvas.Children.Count; i++)
            {
                polylineLayerCanvas.Children[i].Visibility = Visibility.Visible;
                polylineLayerCanvas.Children[i].IsHitTestVisible = true;
            }
        }

        private void Canvas_Loaded(object sender, RoutedEventArgs e)
        {

            drawer = new Drawer(parameters, polylineLayerCanvas, imageLayerCanvas);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (animationTimer.IsEnabled)
                animationTimer.Stop();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (drawer == null || drawer.IsEmpty() || parameters.Image == null)
                return;
            if (animationTimer == null)
                animationTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(20), DispatcherPriority.Normal, animationTimerCallback, Dispatcher.CurrentDispatcher);
            else
                animationTimer.Start();
        }

        private void ImageLoadButton_Click(object sender, RoutedEventArgs e)
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string myPicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            openFileDialog.InitialDirectory = myPicturesPath;
            openFileDialog.Filter = "jpg (*.jpg)|*.jpg|png (*.png)|*.png|bmp (*.bmp)|*.bmp";

            if (openFileDialog.ShowDialog() == true)
            {
                filePath = openFileDialog.FileName;
                parameters.Image = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(filePath);
                parameters.Image = new System.Drawing.Bitmap(parameters.Image, parameters.Image.Width / 10, parameters.Image.Height / 10);
                Debug.WriteLine(parameters.Image.PixelFormat);
                thumbnialImage.Source = new BitmapImage(new Uri(filePath));
                drawer.LoadImageData();
                if (animationTimer.IsEnabled)
                    animationTimer.Stop();
            }
        }

        private void SavePolylineButton_Click(object sender, RoutedEventArgs e)
        {
            if (drawer.IsEmpty())
            {
                MessageBox.Show("There is no polyline!");
                return;
            }

            Directory.CreateDirectory("Polylines");
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "json (*.json)|*.json";
            string CombinedPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Polylines");
            saveFileDialog.InitialDirectory = System.IO.Path.GetFullPath(CombinedPath);
            saveFileDialog.FileName = "polyline";
            if (saveFileDialog.ShowDialog() == true)
                File.WriteAllText(saveFileDialog.FileName, drawer.GetPointsJson());
        }

        private void LoadPolylineButton_Click(object sender, RoutedEventArgs e)
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string CombinedPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Polylines");
            openFileDialog.InitialDirectory = System.IO.Path.GetFullPath(CombinedPath);
            openFileDialog.Filter = "json (*.json)|*.json";

            if (openFileDialog.ShowDialog() == true)
            {
                filePath = openFileDialog.FileName;
                drawer.UpdatePointsFromJson(File.ReadAllText(filePath));
                drawer.AddMouseDownEventHandlerToPoints(Ellipse_MouseDown);
                drawer.MoveImageToNextPosition(0);
                if (!parameters.IsPolylineVisible)
                    DisablePolylineCanvasChlidren();
            }

        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                parameters.NumberOfPoints = uint.Parse(numberOfPointsTextBox.Text);
            }
            catch
            {
                MessageBox.Show("Invalid number of points!");
            }

            drawer.GeneratePoints();
            drawer.AddMouseDownEventHandlerToPoints(Ellipse_MouseDown);
            drawer.MoveImageToNextPosition(0);
            if (!parameters.IsPolylineVisible)
                DisablePolylineCanvasChlidren();
        }
        //Points movement
        #region 
        private void Canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            movingPoint = null;
        }
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (movingPoint == null)
            {
                return;
            }
            if (e.GetPosition(polylineLayerCanvas).X < drawer.PointDiameter / 2 || e.GetPosition(polylineLayerCanvas).Y < drawer.PointDiameter / 2 || e.GetPosition(polylineLayerCanvas).X > polylineLayerCanvas.ActualWidth - drawer.PointDiameter / 2 || e.GetPosition(polylineLayerCanvas).Y > polylineLayerCanvas.ActualHeight - drawer.PointDiameter / 2)
            {
                return;
            }

            movingPoint.X = e.GetPosition(polylineLayerCanvas).X;
            movingPoint.Y = e.GetPosition(polylineLayerCanvas).Y;
            drawer.DrawBezierCurve();
            if (parameters.Animation == Model.Animation.Rotation)
                drawer.RotateImage(0);
            else
                drawer.MoveImageToNextPosition(0);
        }
        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            movingPoint = null;
        }
        private void Ellipse_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Ellipse target = sender as Ellipse;

            movingPoint = drawer.GetTargetedPoint(target);
        }
        #endregion

    }
}
