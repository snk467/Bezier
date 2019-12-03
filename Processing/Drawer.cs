using Bezier.ViewModel;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Linq;
using System;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using Rectangle = System.Drawing.Rectangle;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using System.Threading;

namespace Bezier.Processing
{
    public class Drawer
    {
        ParametersViewModel parameters;
        const int channels = 4;

        Canvas polylineLayerCanvas;
        Canvas imageLayerCanvas;
        List<Vertex> polylinePoints;
        WriteableBitmap polylineCanvasBackground;
        WriteableBitmap imageCanvasBackground;
        byte[] polylineCanvasBackgroundPixels;
        byte[] imageCanvasBackgroundPixels;
        double canvasWidth;
        double canvasHeight;

        Vector2[] pointsOnCurve;
        private BitmapData imageData;
        private int imageStride;
        private byte[] imageBytes;

        int currentImagePosition = 0;
        double angle;
        //double angleChange;
        float sin;
        float cos;
        bool movingBackwards = false;

        private Vector4[,] imagePixelsColorsDestination;
        private Vector4[,] imagePixelsColorsSource;
        private double shearAngle;
        private Vector4[,] imagePixelsColorsDefault;
        private int dim;
        private float shearSin;
        private float shearTan;

        public int PointDiameter { get; set; }
        public Drawer(ParametersViewModel parameters, Canvas polylineLayerCanvas, Canvas imageLayerCanvas)
        {
            this.parameters = parameters;
            this.polylineLayerCanvas = polylineLayerCanvas;
            this.imageLayerCanvas = imageLayerCanvas;
            canvasWidth = Math.Ceiling(polylineLayerCanvas.ActualWidth);
            canvasHeight = Math.Ceiling(polylineLayerCanvas.ActualHeight);
            PointDiameter = 10;
            polylineCanvasBackground = new WriteableBitmap((int)canvasWidth, (int)canvasHeight, 96, 96, PixelFormats.Pbgra32, null);
            imageCanvasBackground = new WriteableBitmap((int)canvasWidth, (int)canvasHeight, 96, 96, PixelFormats.Pbgra32, null);
            polylineCanvasBackgroundPixels = new byte[(int)canvasWidth * (int)canvasHeight * channels];
            imageCanvasBackgroundPixels = new byte[(int)canvasWidth * (int)canvasHeight * channels];
            polylineLayerCanvas.Background = new ImageBrush(polylineCanvasBackground);
            imageLayerCanvas.Background = new ImageBrush(imageCanvasBackground);
            polylinePoints = new List<Vertex>();
            pointsOnCurve = new Vector2[10000];
            angle = 0;
            //sin = (float)Math.Sin(angle);
            //cos = (float)Math.Cos(angle);
            //tan2 = (float)Math.Tan(angle / 2);

        }

        internal void RotateImage(double angleChange)
        {
            if (parameters.Image == null || polylinePoints.Count() == 0 || polylinePoints == null)
                return;

            angle += angleChange;
            if (angle > 2 * Math.PI && parameters.Rotation == Model.Rotation.Filter)
            {
                angle = 0;
                imagePixelsColorsSource = imagePixelsColorsDefault;
            }
            sin = (float)Math.Sin(angle);
            cos = (float)Math.Cos(angle);

            ClearImageBackground();
            SetRotatedImageOnImageCanvas((int)pointsOnCurve[currentImagePosition].X - imageData.Width / 2, (int)pointsOnCurve[currentImagePosition].Y - imageData.Height / 2);
            RefreshImageBackground();

            
            
        }
        internal void MoveImageToNextPosition(int jump)
        {
            if (parameters.Image == null || polylinePoints.Count() == 0 || polylinePoints == null)
                return;

            if ((currentImagePosition + jump >= pointsOnCurve.Count() && !movingBackwards) || (currentImagePosition - jump < 0 && movingBackwards))
            {
                movingBackwards = !movingBackwards;
            }
            if (movingBackwards)
            {
                currentImagePosition -= jump;
            }
            else
            {
                currentImagePosition += jump;
            }
            ClearImageBackground();
            SetImageOnImageCanvas((int)pointsOnCurve[currentImagePosition].X - imageData.Width / 2, (int)pointsOnCurve[currentImagePosition].Y - imageData.Height / 2);
            RefreshImageBackground();
        }
        private double[,] Invert2x2Matrix(double[,] matrix)
        {
            if (matrix.Length != 4)
                throw new ArgumentException();
            double a = matrix[0, 0];
            double b = matrix[1, 0];
            double c = matrix[0, 1];
            double d = matrix[1, 1];

            double det = a * d - b * c;

            if (det == 0)
                throw new ArgumentException();

            double[,] result = new double[2, 2];

            result[0, 0] = d / det;
            result[1, 0] = -b / det;
            result[0, 1] = -c / det;
            result[1, 1] = a / det;
            return result;
        }

        private Vector2 Multiply2x2MatrixBy2dVector(double[,] matrix, int x, int y)
        {
            if (matrix.Length != 4)
                throw new ArgumentException();
            double a = matrix[0, 0];
            double b = matrix[1, 0];
            double c = matrix[0, 1];
            double d = matrix[1, 1];

            return new Vector2((float)(a * x + b * y), (float)(c * x + d * y));
        }
        public void GeneratePoints()
        {
            if (parameters.NumberOfPoints < 2)
            {
                return;
            }

            double margin = 50.0;
            double actualCanvasHeight = polylineLayerCanvas.ActualHeight;
            double actualCanvasWidth = polylineLayerCanvas.ActualWidth;
            Random random = new Random();
            polylinePoints.Clear();

            ClearCanvas();

            for (double x = margin; x <= actualCanvasWidth - margin; x += (actualCanvasWidth - 2 * margin) / (parameters.NumberOfPoints - 1))
            {
                polylinePoints.Add(new Vertex(x, random.NextDouble() * (actualCanvasHeight - 2 * margin) + margin));
                if (polylinePoints.Count() > 1)
                {
                    Vertex from = polylinePoints[polylinePoints.Count() - 2];
                    Vertex to = polylinePoints[polylinePoints.Count() - 1];
                    Line line = new Line { X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y, Stroke = Brushes.Magenta };
                    from.AddLine(line);
                    to.AddLine(line);
                }
            }
            foreach (Vertex point in polylinePoints)
            {
                point.AddToCanvas(polylineLayerCanvas);
            }

            DrawBezierCurve();
            if (parameters.Image != null)
            {
                SetImageOnImageCanvas((int)pointsOnCurve[currentImagePosition].X - imageData.Width / 2, (int)pointsOnCurve[currentImagePosition].Y - imageData.Height / 2);
                RefreshImageBackground();
            }

        }
        private void ClearCanvas()
        {
            polylineLayerCanvas.Children.Clear();
        }
        public void DrawBezierCurve()
        {
            if (polylinePoints.Count() == 0 || polylinePoints == null)
                return;
            ClearPolylineBackground();


            List<Vector2> controlPoints = Enumerable.Range(0, polylinePoints.Count()).Select((i) => new Vector2((float)polylinePoints[i].X, (float)polylinePoints[i].Y)).ToList();

            Parallel.For(0, 10000, (i) =>
            {
                float u = i / 10000f;
                Vector2 result = DeCasteljau(controlPoints, u);
                pointsOnCurve[i] = result;
                SetPolylineCanvasPixel((int)result.X, (int)result.Y, Colors.Black);
            });

            //Wersja sekwencyjna
            //for (float u = 0; u <= 1; u += 1e-4f)
            //{
            //    Vector2 result = DeCasteljau(controlPoints, u);
            //    SetPixel((int)result.X, (int)result.Y, Colors.Black);
            //}
            RefreshPolylineBackground();
        }
        private Vector2 DeCasteljau(List<Vector2> controlPoints, float u)
        {
            Vector2[] Q = controlPoints.ToArray();

            for (int k = 1; k < controlPoints.Count(); k++)
            {
                for (int i = 0; i < controlPoints.Count() - k; i++)
                {
                    Q[i] = (1 - u) * Q[i] + u * Q[i + 1];
                }
            }
            return Q[0];
        }
        public void SetPolylineCanvasPixel(int x, int y, Color color)
        {
            polylineCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 0)] = color.B;
            polylineCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 1)] = color.G;
            polylineCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 2)] = color.R;
            polylineCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 3)] = color.A;
        }
        //public void SetImageCanvasPixel(int x, int y, Color color)
        //{
        //    if (x < 0 || y < 0 || x > imageCanvasBackground.Width || y > imageCanvasBackground.Height)
        //        return;
        //    imageCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 0)] = color.B;
        //    imageCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 1)] = color.G;
        //    imageCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 2)] = color.R;
        //    imageCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 3)] = color.A;
        //}
        public void SetImageCanvasPixel(int x, int y, byte R, byte G, byte B, byte A)
        {
            if (x < 0 || y < 0 || x >= canvasWidth || y >= canvasHeight)
                return;

            byte grayScale = (byte)(0.299 * R + 0.587 * G + 0.114 * B);

            imageCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 0)] = parameters.IsGrayColors? grayScale : B;
            imageCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 1)] = parameters.IsGrayColors ? grayScale : G;
            imageCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 2)] = parameters.IsGrayColors ? grayScale : R;
            imageCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 3)] = A;
        }
        public void RefreshPolylineBackground()
        {
            Int32Rect rect = new Int32Rect(0, 0, (int)canvasWidth, (int)canvasHeight);
            polylineCanvasBackground.WritePixels(rect, polylineCanvasBackgroundPixels, (int)canvasWidth * (int)channels, 0);
        }
        public void RefreshImageBackground()
        {
            Int32Rect rect = new Int32Rect(0, 0, (int)canvasWidth, (int)canvasHeight);
            imageCanvasBackground.WritePixels(rect, imageCanvasBackgroundPixels, (int)canvasWidth * (int)channels, 0);
        }
        public Vertex GetTargetedPoint(Ellipse target)
        {
            foreach (Vertex point in polylinePoints)
            {
                if (point.Ellipse == target)
                {
                    return point;
                }
            }

            return null;
        }
        public void ClearPolylineBackground()
        {
            FillPolylineBackgroundColor(Colors.White);
            RefreshPolylineBackground();
        }
        public void ClearImageBackground()
        {
            FillImageBackgroundColor(Colors.Transparent);
            RefreshImageBackground();
        }
        public void FillPolylineBackgroundColor(Color color)
        {
            for (int x = 0; x < canvasWidth; x++)
            {
                for (int y = 0; y < canvasHeight; y++)
                {
                    polylineCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 0)] = color.B;
                    polylineCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 1)] = color.G;
                    polylineCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 2)] = color.R;
                    polylineCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 3)] = color.A;
                }
            }
        }
        public void FillImageBackgroundColor(Color color)
        {

            Parallel.For(0, (int)canvasWidth, (x) =>
            {
                for (int y = 0; y < canvasHeight; y++)
                {
                    imageCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 0)] = (byte)(255 - color.B);
                    imageCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 1)] = (byte)(255 - color.G);
                    imageCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 2)] = (byte)(255 - color.R);
                    imageCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 3)] = color.A;
                }
            });
            //for (int x = 0; x < canvasWidth; x++)
            //{
            //    for (int y = 0; y < canvasHeight; y++)
            //    {
            //        imageCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 0)] = 0;
            //        imageCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 1)] = 0;
            //        imageCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 2)] = 255;
            //        imageCanvasBackgroundPixels[BackgroundPixelIndex(x, y, 3)] = 0;
            //    }
            //}
        }
        private void SetImageOnImageCanvas(int startX, int startY)
        {
            if (angle == 0)
                Parallel.For(0, imageData.Width, (x) =>
                {
                    for (int y = 0; y < imageData.Height; y++)
                    {
                        SetImageCanvasPixel(startX + x, startY + y, imageBytes[ImageDataPixelIndex(x, y, 2)], imageBytes[ImageDataPixelIndex(x, y, 1)], imageBytes[ImageDataPixelIndex(x, y, 0)], 200);
                    }
                });
            else
                SetRotatedImageOnImageCanvas(startX, startY);


            //for (int x = 0; x < imageData.Width; x++)
            //{
            //    for (int y = 0; y < imageData.Height; y++)
            //    {
            //        SetImageCanvasPixel(startX + x, startY + y, imageBytes[ImageDataPixelIndex(x, y, 2)], imageBytes[ImageDataPixelIndex(x, y, 1)], imageBytes[ImageDataPixelIndex(x, y, 0)], 200);
            //    }
            //}
        }
        private void SetRotatedImageOnImageCanvas(int startX, int startY)
        {
            if (parameters.Rotation == Model.Rotation.Naive)
                Parallel.For(0, imageData.Width, (x) =>
                {
                    for (int y = 0; y < imageData.Height; y++)
                    {
                        Vector2 newLocation;
                        newLocation = NaiveRotatePixel(startX + x, startY + y);
                        SetImageCanvasPixel((int)(newLocation.X), (int)(newLocation.Y), imageBytes[ImageDataPixelIndex(x, y, 2)], imageBytes[ImageDataPixelIndex(x, y, 1)], imageBytes[ImageDataPixelIndex(x, y, 0)], 200);
                    }
                });
            else
            {             

                if(angle > Math.PI / 4 && angle <= Math.PI * (3/4.0))
                {
                    imagePixelsColorsSource = Rotate90(imagePixelsColorsSource);
                    shearAngle = angle - Math.PI / 2;
                }
                else if (angle > Math.PI * (3 / 4.0) && angle <= Math.PI * (5 / 4.0))
                {
                    imagePixelsColorsSource = Rotate180(imagePixelsColorsSource);
                    shearAngle = angle - Math.PI;
                }
                else if (angle > Math.PI * (5 / 4.0) && angle <= Math.PI * (7 / 4.0))
                {
                    imagePixelsColorsSource = Rotate270(imagePixelsColorsSource);
                    shearAngle = angle - Math.PI * (3/2.0);
                }
                else
                {
                    shearAngle = angle > Math.PI * (3 / 2.0) ? angle - Math.PI * 2 : angle;
                }

                shearSin = (float)Math.Sin(shearAngle);
                shearTan = (float)Math.Tan(shearAngle/2);

                List<Vector2> vertexPoints = new List<Vector2>(4);

                vertexPoints.Add(TransformPoint(0, 0));
                vertexPoints.Add(TransformPoint(imagePixelsColorsSource.GetLength(0) - 1, 0));
                vertexPoints.Add(TransformPoint(0, imagePixelsColorsSource.GetLength(1) - 1));
                vertexPoints.Add(TransformPoint(imagePixelsColorsSource.GetLength(0) - 1, imagePixelsColorsSource.GetLength(1) - 1));

                float top = vertexPoints[0].X;
                float bottom = vertexPoints[0].X;
                float left = vertexPoints[0].Y;
                float right = vertexPoints[0].Y;


                for (int i = 0; i < vertexPoints.Count(); i++)
                {
                    if (vertexPoints[i].Y < top)
                        top = vertexPoints[i].Y;
                    if (vertexPoints[i].Y > bottom)
                        bottom = vertexPoints[i].Y;
                    if (vertexPoints[i].X > right)
                        right = vertexPoints[i].X;
                    if (vertexPoints[i].X < left)
                        left = vertexPoints[i].X;
                }

                ShearX();
                ShearY();
                ShearX();

                Vector4[,] tmp = new Vector4[(int)Math.Abs(left - right), (int)Math.Abs(top - bottom)];

                Parallel.For(0, tmp.GetLength(0), (x) =>
                {
                    for (int y = 0; y < tmp.GetLength(1); y++)
                    {
                        tmp[x, y].X = imagePixelsColorsSource[imagePixelsColorsSource.GetLength(0) / 2 - tmp.GetLength(0) / 2 + x, imagePixelsColorsSource.GetLength(1) / 2 - tmp.GetLength(1) / 2 + y].X;
                        tmp[x, y].Y = imagePixelsColorsSource[imagePixelsColorsSource.GetLength(0) / 2 - tmp.GetLength(0) / 2 + x, imagePixelsColorsSource.GetLength(1) / 2 - tmp.GetLength(1) / 2 + y].Y;
                        tmp[x, y].Z = imagePixelsColorsSource[imagePixelsColorsSource.GetLength(0) / 2 - tmp.GetLength(0) / 2 + x, imagePixelsColorsSource.GetLength(1) / 2 - tmp.GetLength(1) / 2 + y].Z;
                        tmp[x, y].W = imagePixelsColorsSource[imagePixelsColorsSource.GetLength(0) / 2 - tmp.GetLength(0) / 2 + x, imagePixelsColorsSource.GetLength(1) / 2 - tmp.GetLength(1) / 2 + y].W;
                    }
                });                

                imagePixelsColorsSource = tmp;


                Parallel.For(0, imagePixelsColorsSource.GetLength(0), (x) => {
                    for (int y = 0; y < imagePixelsColorsSource.GetLength(1); y++)
                    {
                        SetImageCanvasPixel((int)pointsOnCurve[currentImagePosition].X - imagePixelsColorsSource.GetLength(0) / 2 + x, (int)pointsOnCurve[currentImagePosition].Y - imagePixelsColorsSource.GetLength(1) / 2 + y, (byte)imagePixelsColorsSource[x, y].Z, (byte)imagePixelsColorsSource[x, y].Y, (byte)imagePixelsColorsSource[x, y].X, (byte)imagePixelsColorsSource[x, y].W);
                    }
                });
                imagePixelsColorsSource = imagePixelsColorsDefault;
            }

        }

        private void ShearY()
        {
            float b = shearSin;
            if (b == 0)
            {
                return;
            }
            int offset = (int)Math.Round(Math.Abs(b) * imagePixelsColorsSource.GetLength(0));

            imagePixelsColorsDestination = new Vector4[imagePixelsColorsSource.GetLength(0), imagePixelsColorsSource.GetLength(1) + offset];

            if (shearAngle > 0)
                offset = 0;

            Parallel.For(0, imagePixelsColorsSource.GetLength(0), (x) => {
                float f = b * x - (float)Math.Floor(b * x);
                for (int y = 0; y < imagePixelsColorsSource.GetLength(1); y++)
                {
                    if (y + 1 < imagePixelsColorsSource.GetLength(1))
                        imagePixelsColorsDestination[x, (int)(y + b * x) + offset] = f * imagePixelsColorsSource[x, y] + (1-f) * imagePixelsColorsSource[x, y + 1];
                    else
                        imagePixelsColorsDestination[x, (int)(y + b * x) + offset] = imagePixelsColorsSource[x, y];
                }


            });

            imagePixelsColorsSource = imagePixelsColorsDestination;
        }

        private void ShearX()
        {
            float a = -shearTan;
            if (a == 0)
            {
               return;
            }
            int offset = (int)Math.Round(Math.Abs(a) * imagePixelsColorsSource.GetLength(1));
            

            int xSize = imagePixelsColorsSource.GetLength(0) + offset;
            int ySize = imagePixelsColorsSource.GetLength(1);

            imagePixelsColorsDestination = new Vector4[xSize, ySize];

            if (shearAngle < 0)
                offset = 0;

            Parallel.For(0, imagePixelsColorsSource.GetLength(1), (y) =>
            {
                float f = a * y - (float)Math.Floor(a * y);
                for (int x = imagePixelsColorsSource.GetLength(0) - 1; x >= 0; x--)
                {
                    if (x - 1 >= 0)
                        imagePixelsColorsDestination[(int)(x + a * y) + offset, y] = (1 - f) * imagePixelsColorsSource[x, y] + f * imagePixelsColorsSource[x - 1, y];
                    else
                        imagePixelsColorsDestination[(int)(x + a * y) + offset, y] = imagePixelsColorsSource[x, y];
                }
            });

            imagePixelsColorsSource = imagePixelsColorsDestination;
        }

        private Vector2 NaiveRotatePixel(float x, float y)
        {

            // translate point back to origin:
            x -= pointsOnCurve[currentImagePosition].X;
            y -= pointsOnCurve[currentImagePosition].Y;

            // rotate point
            float xnew = x * cos - y * sin;
            float ynew = x * sin + y * cos;

            // translate point back:
            x = xnew + pointsOnCurve[currentImagePosition].X;
            y = ynew + pointsOnCurve[currentImagePosition].Y;
            return new Vector2(x, y);
        }
        
        public void LoadImageData()
        {
            if (parameters.Image == null)
            {
                return;
            }
            imageData = parameters.Image.LockBits(new Rectangle(0, 0, parameters.Image.Width, parameters.Image.Height), ImageLockMode.ReadOnly, parameters.Image.PixelFormat);
            imageStride = imageData.Stride;
            imageBytes = new byte[imageStride * parameters.Image.Height];
            IntPtr textureScan0 = imageData.Scan0;
            Marshal.Copy(textureScan0, imageBytes, 0, imageBytes.Length);
            parameters.Image.UnlockBits(imageData);

            imagePixelsColorsDefault = new Vector4[imageData.Width, imageData.Height];
            dim = (int)Math.Round(Math.Sqrt(Math.Pow(imageData.Width, 2) + Math.Pow(imageData.Height, 2)));




            for (int i = 0; i < imageData.Width; i++)
            {
                for (int j = 0; j < imageData.Height; j++)
                {
                    //BGRA
                    imagePixelsColorsDefault[i, j] = new Vector4(imageBytes[ImageDataPixelIndex(i, j, 0)], imageBytes[ImageDataPixelIndex(i, j, 1)], imageBytes[ImageDataPixelIndex(i, j, 2)], 200);
                }
            }
            imagePixelsColorsSource = imagePixelsColorsDefault;

            if (polylinePoints != null && polylinePoints.Count() > 0)
            {
                ClearImageBackground();
                SetImageOnImageCanvas((int)pointsOnCurve[currentImagePosition].X - imageData.Width / 2, (int)pointsOnCurve[currentImagePosition].Y - imageData.Height / 2);
                RefreshImageBackground();
            }

            angle = 0;
        }
        private int BackgroundPixelIndex(int x, int y, int channel)
        {
            return y * (int)canvasWidth * channels + channels * x + channel;
        }
        public void AddMouseDownEventHandlerToPoints(MouseButtonEventHandler handler)
        {
            polylinePoints.ForEach((point) => point.Ellipse.MouseDown += handler);
        }
        public string GetPointsJson()
        {
            return JsonConvert.SerializeObject(Enumerable.Range(0, polylinePoints.Count()).Select((i) => new Vector2((float)polylinePoints[i].X, (float)polylinePoints[i].Y)).ToList());
        }
        public void UpdatePointsFromJson(string json)
        {
            List<Vector2> readPoints = JsonConvert.DeserializeObject<List<Vector2>>(json);

            ClearCanvas();
            polylinePoints.Clear();

            for (int i = 0; i < readPoints.Count(); i++)
            {
                polylinePoints.Add(new Vertex(readPoints[i].X, readPoints[i].Y));
                if (polylinePoints.Count() > 1)
                {
                    Vertex from = polylinePoints[polylinePoints.Count() - 2];
                    Vertex to = polylinePoints[polylinePoints.Count() - 1];
                    Line line = new Line { X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y, Stroke = Brushes.Magenta };
                    from.AddLine(line);
                    to.AddLine(line);
                }
            }
            foreach (Vertex point in polylinePoints)
            {
                point.AddToCanvas(polylineLayerCanvas);
            }
            DrawBezierCurve();
        }
        public bool IsEmpty()
        {
            if (polylinePoints == null || polylinePoints.Count() == 0)
                return true;
            return false;
        }
        //bgra
        private int ImageDataPixelIndex(int x, int y, int z)
        {
            return y * imageStride + channels * x + z;
        }

        private void Copy2dVector4ArrayToSquereArray(Vector4[,] to, Vector4[,] from)
        {
            if (to.Length < from.Length)
                return;
            if (to.GetLength(0) != to.GetLength(1))
                return;

            int offsetX = (int)Math.Round((to.GetLength(0) - from.GetLength(0)) / 2.0);
            int offsetY = (int)Math.Round((to.GetLength(1) - from.GetLength(1)) / 2.0);

            for (int x = offsetX; x < offsetX; x++)
            {
                for (int y = offsetY; y < offsetY; y++)
                {
                    to[x, y] = from[x - offsetX, y - offsetY];
                }
            }

        }

        private Vector4[,] Rotate90(Vector4[,] source)
        {
            Vector4[,] result = new Vector4[source.GetLength(1), source.GetLength(0)];

            for (int x = 0; x < source.GetLength(0); x++)
            {
                for (int y = 0; y < source.GetLength(1); y++)
                {
                    result[source.GetLength(1) - y - 1,x] = source[x, y];
                }
            }

            return result;
        }

        private Vector4[,] Rotate180(Vector4[,] source)
        {
            Vector4[,] result = new Vector4[source.GetLength(0), source.GetLength(1)];

            for (int x = 0; x < source.GetLength(0); x++)
            {
                for (int y = 0; y < source.GetLength(1); y++)
                {
                    result[source.GetLength(0) - x - 1,source.GetLength(1) - y - 1] = source[x, y];
                }
            }

            return result;
        }

        private Vector4[,] Rotate270(Vector4[,] source)
        {
            Vector4[,] result = new Vector4[source.GetLength(1), source.GetLength(0)];

            for (int x = 0; x < source.GetLength(0); x++)
            {
                for (int y = 0; y < source.GetLength(1); y++)
                {
                    result[y, source.GetLength(1) - x - 1] = source[x, y];
                }
            }

            return result;
        }

        private Vector2 TransformPoint(int x, int y)
        {
            float a = -shearTan;
            float b = shearSin;
            return new Vector2(x + a * y + a * (b * (x + a * y) + y), b * (x + a * y) + y);
        }
    }
}