using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace Figure
{
    class Plot
    {
        Canvas canGraph;

        public enum PLOT_TYPE {PLOT_SRC,PLOT_MEASURE};
        public enum PLOT_QUANTITY { Frequency, Amplitude };

        double wxmin;
        double wxmax;
        double wymin;
        double wymax;

        double xstep;
        double ystep;

        double xsteps = 15;
        double ysteps = 15;

        double xtic;
        double ytic;
        double dxmargin = 20;
        double dymargin = 35;
        double numOfXTick = 13;
        double numOfYTick = 10;

        // Prepare values for perform transformations.
        private Matrix WtoDMatrix, DtoWMatrix;
        private PLOT_QUANTITY quantity;
        private bool ready = false;
        public Plot(Canvas cGraph)
        {
            canGraph = cGraph;                  //take reference of Canvas

        }

        public void Init(double wxmin, double wxmax, double wymin, double wymax)
        {
            ready = true;                                       //initialization
            SizeUpdate(wxmin, wxmax, wymin, wymax, 1111 );
        }

        /// <summary>
        /// Updates transform matrixes, it has to update every time when size or min max values changes
        /// </summary>
        /// <param name="wxmin"></param>
        /// <param name="wxmax"></param>
        /// <param name="wymin"></param>
        /// <param name="wymax"></param>
        /// <param name="updateSelection">
        /// update only that parameter where there is 1 in the position, 1111 updates each parameter
        /// </param>
        public void SizeUpdate(double wxmin, double wxmax, double wymin, double wymax, int updateSelection )
        {
            if (ready == false)
            {
                return;
            }


            string updateSelectionString = updateSelection.ToString("0000");
            // Get the tic mark lengths.
            double dxmin = dymargin;
            if (wymax >= 10000)
            {
                dxmin += 5;
            }
            double dxmax = canGraph.ActualWidth - dymargin/2 - 20;
            double dymin = dxmargin/2 + 12;
            double dymax = canGraph.ActualHeight - dxmargin;

            if (updateSelectionString.Substring(0,1) == "1")
            {
                this.wxmin = wxmin;
            }
            if (updateSelectionString.Substring(1, 1) == "1")
            {
                this.wxmax = wxmax;
            }
            if (updateSelectionString.Substring(2, 1) == "1")
            {
                this.wymin = wymin;
            }
            if (updateSelectionString.Substring(3, 1) == "1")
            {
                this.wymax = wymax;
            }


            double stepInc = 1;
            while (((this.wxmax - this.wxmin) / (stepInc)) > xsteps)
            {
                stepInc+=10;
                stepInc = 10*(Math.Round(stepInc / 10,0));
            }

            xstep = stepInc;

            stepInc = 1;
            while (((this.wymax - this.wymin) / (stepInc)) > ysteps)
            {
                if (stepInc < 100)
                {
                    stepInc += 10;
                    stepInc = 10 * (Math.Round(stepInc / 10, 0));
                }
                else if (stepInc < 500)
                {
                    stepInc += 100;
                    //stepInc = 100 * (Math.Round(stepInc / 100, 0));
                }
                else
                {
                    stepInc += 500;
                    //stepInc = 1000 * (Math.Round(stepInc / 1000, 0));
                }
                
            }

            ystep = stepInc;


            // Prepare the transformation matrices.
            PrepareTransformations(this.wxmin, this.wxmax, this.wymin, this.wymax, dxmin, dxmax, dymax, dymin);
            
            Point p0 = DtoW(new Point(0, 0));
            Point p1 = DtoW(new Point(5, 5));
            xtic = p1.X - p0.X;
            ytic = p1.Y - p0.Y;

          


        }

        /// <summary>
        /// Switches Y axis, Frequency, Amplitude
        /// </summary>
        /// <param name="quantity"></param>
        public void SetYAxisQuantity( PLOT_QUANTITY quantity)
        {
            this.quantity = quantity;
        }
        //int plotSrcIndex = -1;
        //int plotMeasureIndex = -1;

        Polyline srcPolyLine;
        Polyline measPolyline;
        /// <summary>
        /// Adds plot, only 2 plots can be added, one per each type, old one will be deleted before adding new one.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="plotType"></param>
        public void PlotAdd(double[] data, PLOT_TYPE plotType )
        {
            PointCollection points = new PointCollection();
            int indexToDeleteOn;

            if (data == null )
            {
                return;
            }

            for (int i = 0; i < data.Length; i += 1)
            {
                if (data[i] < wymax)
                {
                    Point p = new Point(i, data[i]);
                    points.Add(WtoD(p));
                }
                else
                {
                    Point p = new Point(i, wymax);
                    points.Add(WtoD(p));
                }
                

            }
            
            switch(plotType)
            {
                case PLOT_TYPE.PLOT_SRC:
                    indexToDeleteOn = canGraph.Children.IndexOf(srcPolyLine);
                    if (indexToDeleteOn != -1)
                    {
                        canGraph.Children.RemoveAt(indexToDeleteOn);
                    }
                    srcPolyLine = new Polyline();
                    srcPolyLine.StrokeThickness = 1;

                    srcPolyLine.Points = points;
                    srcPolyLine.Stroke = Brushes.Red;
                    srcPolyLine.SnapsToDevicePixels = true;
                    srcPolyLine.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);

                    canGraph.Children.Add(srcPolyLine);
                    //plotSrcIndex = canGraph.Children.Count - 1;
                    break;
                case PLOT_TYPE.PLOT_MEASURE:
                    indexToDeleteOn = canGraph.Children.IndexOf(measPolyline);
                    if (indexToDeleteOn != -1)
                    {
                        canGraph.Children.RemoveAt(indexToDeleteOn);
                    }
                    measPolyline = new Polyline();
                    measPolyline.StrokeThickness = 1;

                    measPolyline.Points = points;
                    measPolyline.Stroke = Brushes.Blue;
                    measPolyline.SnapsToDevicePixels = true;
                    measPolyline.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);


                    canGraph.Children.Add(measPolyline);
                    //plotMeasureIndex = canGraph.Children.Count - 1;

                    break;
            }
        }









        List<Polyline> polylines = new List<Polyline>();

        /// <summary>
        /// Adds plot, only 2 plots can be added, one per each type, old one will be deleted before adding new one.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="plotType"></param>
        public void PlotAdd(int index, double[] data)
        {
            Polyline polyline;
            PointCollection points = new PointCollection();
            int indexToDeleteOn;

            if (data == null)
            {
                return;
            }

            for (int i = 0; i < data.Length; i += 1)
            {
                if (data[i] < wymax)
                {
                    Point p = new Point(i, data[i]);
                    points.Add(WtoD(p));
                }
                else
                {
                    Point p = new Point(i, wymax);
                    points.Add(WtoD(p));
                }
            }

            indexToDeleteOn = canGraph.Children.IndexOf(polylines[index]);
            if (indexToDeleteOn != -1)
            {
                canGraph.Children.RemoveAt(indexToDeleteOn);
            }
            polyline = new Polyline();
            polyline.StrokeThickness = 1;
            polyline.Points = points;
            polyline.Stroke = Brushes.Red;
            polyline.SnapsToDevicePixels = true;
            polyline.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);

            canGraph.Children.Add(polyline);
        }


        List<ObservableCollection<double>> activeLines = new List<ObservableCollection<double>>();
        Brush[] brushes = new Brush[] { new SolidColorBrush(Color.FromScRgb(1F, 0F, 0.4470F, 0.7410F)),
                                        new SolidColorBrush(Color.FromScRgb(1F, 0.8500F, 0.3250F, 0.0980F)),
                                        new SolidColorBrush(Color.FromScRgb(1F, 0.9290F, 0.6940F, 0.1250F)),
                                        new SolidColorBrush(Color.FromScRgb(1F, 0.4940F, 0.1840F, 0.5560F)),
                                        new SolidColorBrush(Color.FromScRgb(1F, 0.4660F, 0.6740F, 0.1880F)),
                                        new SolidColorBrush(Color.FromScRgb(1F, 0.3010F, 0.7450F, 0.9330F)),
                                        new SolidColorBrush(Color.FromScRgb(1F, 0.6350F, 0.0780F, 0.1840F)),
                                        Brushes.Red, Brushes.Green, Brushes.Yellow };

        public static Brush[] brushes2 = new Brush[] { new SolidColorBrush(Color.FromRgb(0x00, 0xB7, 0xE6)),
                                         new SolidColorBrush(Color.FromRgb(0x4D, 0xDB, 0xff)),
                                         new SolidColorBrush(Color.FromRgb(0xB3, 0xF0, 0xFF)),

                                         new SolidColorBrush(Color.FromRgb(0x72, 0xBD, 0x29)),
                                         new SolidColorBrush(Color.FromRgb(0x8C, 0xD6, 0x42)),
                                         new SolidColorBrush(Color.FromRgb(0xBF, 0xE8, 0x96)),

                                         new SolidColorBrush(Color.FromRgb(0xDA, 0xB0, 0x0B)),
                                         new SolidColorBrush(Color.FromRgb(0xf4, 0xCA, 0x25)),
                                         new SolidColorBrush(Color.FromRgb(0xF4, 0xCA, 0x55)),

                                         new SolidColorBrush(Color.FromRgb(0x74, 0x35, 0x7D)),
                                         new SolidColorBrush(Color.FromRgb(0x95, 0x45, 0xA1)),
                                         new SolidColorBrush(Color.FromRgb(0xC0, 0x82, 0xCA)),
                                        };

        /// <summary>
        /// Adds plot, only 2 plots can be added, one per each type, old one will be deleted before adding new one.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="plotType"></param>
        public void PlotAdd(IPlotData plotData)
        {
            for (int i = 0; i < plotData.lines.Count; i++)
            {
                ObservableCollection<double> data = plotData.lines[i];
                activeLines.Add(data);
                DrawCurve(data);
                data.CollectionChanged += Data_CollectionChanged;
            }
        }

        public void PlotRemove(IPlotData plotData)
        {
            for (int i = 0; i < plotData.lines.Count; i++)
            {
                ObservableCollection<double> data = plotData.lines[i];
                int indexOfData = activeLines.IndexOf(data);
                if (indexOfData != -1)
                { 
                    activeLines[indexOfData].CollectionChanged -= Data_CollectionChanged;
                    activeLines.RemoveAt(indexOfData);
                }
            }
        }

        private void Data_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var data = sender as ObservableCollection<double>;
            DrawCurve(data);
        }


        bool autoScaleY = true;
        void DrawCurve(ObservableCollection<double> data)
        {
            int indexToDeleteOn;
            int offset = 0;

            PointCollection points = new PointCollection();

            if (data != null)
            {
                if (data.Count > wxmax)
                {
                    offset = data.Count - (int)wxmax;
                }

                for (int i = offset; i < data.Count; i += 1)
                {
                    if (autoScaleY == false)
                    {
                        if (data[i] < wymax)
                        {
                            Point p = new Point(i - offset, data[i]);
                            points.Add(WtoD(p));
                        }
                        else
                        {
                            Point p = new Point(i - offset, wymax);
                            points.Add(WtoD(p));
                        }
                    }
                    else
                    {
                        if (data[i] > wymax)
                        {
                            double newWymax = 5 * (Math.Ceiling(data[i] / 5));
                            SizeUpdate(0, 0, 0, newWymax, 0001);
                            ReRender();
                        }
                        if (data[i] < wymin)
                        {
                            double newWymin = 5 * (Math.Floor(data[i] / 5));
                            SizeUpdate(0, 0, newWymin, 0, 0010);
                            ReRender();
                        }
                        Point p = new Point(i - offset, data[i]);
                        points.Add(WtoD(p));
                    }
                }

                int indexinCollection = activeLines.IndexOf(data);
                if (polylines.Count > indexinCollection)
                {
                    indexToDeleteOn = canGraph.Children.IndexOf(polylines[indexinCollection]);
                    if (indexToDeleteOn != -1)
                    {
                        canGraph.Children.RemoveAt(indexToDeleteOn);
                        //Console.WriteLine("Deletíng old polyLine");
                    }
                }
                else
                {
                    polylines.Add(new Polyline());
                    indexinCollection = polylines.Count - 1;
                }

                polylines[indexinCollection] = new Polyline();
                polylines[indexinCollection].StrokeThickness = 1;
                polylines[indexinCollection].Points = points;
                //polylines[indexinCollection].Stroke = Brushes.Red;
                polylines[indexinCollection].Stroke = brushes2[indexinCollection];
                polylines[indexinCollection].SnapsToDevicePixels = true;
                polylines[indexinCollection].SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Unspecified);

                canGraph.Children.Add(polylines[indexinCollection]);
                //Console.WriteLine("PolyLine with index:" + canGraph.Children.IndexOf(polylines[indexinCollection]).ToString());
            }
        }



        Path timeLinePath;
        /// <summary>
        /// draws verical Time progress line at specified time.
        /// </summary>
        /// <param name="time"></param>
        public void DrawProgressTimeLine(int time)
        {
            int timeLineIndex;
            Point p0 = WtoD(new Point(time, wymin));
            Point p1 = WtoD(new Point(time, wymax));

            timeLineIndex = canGraph.Children.IndexOf(timeLinePath);
            if (timeLineIndex != -1)
            {
                canGraph.Children.RemoveAt(timeLineIndex);
            }

            LineGeometry line = new LineGeometry(p0, p1);

            timeLinePath = new Path();
            timeLinePath.StrokeThickness = 1;
            timeLinePath.Stroke = Brushes.Black;
            timeLinePath.Data = line;
            timeLinePath.SnapsToDevicePixels = true;

            canGraph.Children.Add(timeLinePath);
            

        }


        /// <summary>
        /// Deletes all lines in graph and draw new ones (with updated transform matrix),
        /// 
        /// draws grid, axis, labels
        /// </summary>
        public void ReRender()
        {
            double xAxisPos = wymin;   //position in y axis where the x axis croses

            if (ready == false)
            {
                return;
            }

            if (canGraph.Children.Count >= 0)
            {
                canGraph.Children.Clear();
                //plotMeasureIndex = -1;
                //plotSrcIndex = -1;
            }

            // Make the X axis.
            GeometryGroup xaxis_geom = new GeometryGroup();
            Point p0 = WtoD(new Point(wxmin, xAxisPos));
            Point p1 = WtoD(new Point(wxmax, xAxisPos));
            xaxis_geom.Children.Add(new LineGeometry(p0, p1));

            DrawText(canGraph, "0", new Point(p0.X + 5, p0.Y+10), 12, HorizontalAlignment.Right, VerticalAlignment.Center);

            bool halfStep = true;
            for (double x = wxmin + xstep/2; x <= wxmax; x += xstep/2)
            {
                Point tic0;
                Point tic1;

                if (halfStep == true)
                {
                    tic0 = WtoD(new Point(x, xAxisPos));
                    tic1 = WtoD(new Point(x, xAxisPos + ytic / 2));
                    halfStep = false;
                }
                else
                {
                    tic0 = WtoD(new Point(x, xAxisPos));
                    tic1 = WtoD(new Point(x, xAxisPos + ytic));
                    DrawText(canGraph, x.ToString("0"), new Point(tic0.X, tic0.Y - 1), 12, HorizontalAlignment.Center, VerticalAlignment.Top);
                    halfStep = true;
                } 
                xaxis_geom.Children.Add(new LineGeometry(tic0, tic1));     
            }

            DrawText(canGraph, "t[s]", new Point(canGraph.ActualWidth - 10,canGraph.ActualHeight - 30), 12, HorizontalAlignment.Center, VerticalAlignment.Top);

            Path xaxis_path = new Path();
            xaxis_path.StrokeThickness = 1;
            xaxis_path.Stroke = Brushes.Black;
            xaxis_path.Data = xaxis_geom;
            xaxis_path.SnapsToDevicePixels = true;
            xaxis_path.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);

            canGraph.Children.Add(xaxis_path);

            // Make the Y axis.
            GeometryGroup yaxis_geom = new GeometryGroup();
            p0 = new Point(0, wymin);
            p1 = new Point(0, wymax);
            xaxis_geom.Children.Add(new LineGeometry(WtoD(p0), WtoD(p1)));

            halfStep = true;
            for (double y = wymin + ystep/2; y <= wymax; y += ystep/2)
            {
                Point tic0;
                Point tic1;

                if (halfStep == true)
                {
                    tic0 = WtoD(new Point(-xtic/2, y));
                    tic1 = WtoD(new Point(0, y));
                    halfStep = false;
                }
                else
                {
                    tic0 = WtoD(new Point(-xtic, y));
                    tic1 = WtoD(new Point(0, y));
                    halfStep = true;
                    DrawText(canGraph, y.ToString(), new Point(tic0.X + 3, tic0.Y), 12, HorizontalAlignment.Right, VerticalAlignment.Center);
                }
                xaxis_geom.Children.Add(new LineGeometry(tic0, tic1));
            }

            // Label the tic mark's Y coordinate.
            switch (quantity)
            {
                case PLOT_QUANTITY.Frequency:
                    DrawText(canGraph, "rss[dBm]", new Point(40, 20), 12, HorizontalAlignment.Center, VerticalAlignment.Bottom);
                    break;
                case PLOT_QUANTITY.Amplitude:
                    DrawText(canGraph, "A[um]", (new Point(20, 20)), 12, HorizontalAlignment.Center, VerticalAlignment.Bottom);
                    break;
            }
            
            Path yaxis_path = new Path();
            yaxis_path.StrokeThickness = 1;
            yaxis_path.Stroke = Brushes.Black;
            yaxis_path.Data = yaxis_geom;
            yaxis_path.SnapsToDevicePixels = true;
            yaxis_path.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);

            canGraph.Children.Add(yaxis_path);

            for (int i = 0; i < activeLines.Count; i++)
            {
                DrawCurve(activeLines[i]);
            }


        }



        /// <summary>
        /// calculates transform matrix
        /// </summary>
        /// <param name="wxmin"></param>
        /// <param name="wxmax"></param>
        /// <param name="wymin"></param>
        /// <param name="wymax"></param>
        /// <param name="dxmin"></param>
        /// <param name="dxmax"></param>
        /// <param name="dymin"></param>
        /// <param name="dymax"></param>
        private void PrepareTransformations(    double wxmin, double wxmax, double wymin, double wymax,
                                                double dxmin, double dxmax, double dymin, double dymax  )
        {
            // Make WtoD.
            WtoDMatrix = Matrix.Identity;
            WtoDMatrix.Translate(-wxmin, -wymin);

            double xscale = (dxmax - dxmin) / (wxmax - wxmin);
            double yscale = (dymax - dymin) / (wymax - wymin);
            WtoDMatrix.Scale(xscale, yscale);

            WtoDMatrix.Translate(dxmin, dymin);

            // Make DtoW.
            DtoWMatrix = WtoDMatrix;
            DtoWMatrix.Invert();
        }

        // Transform a point from world to device coordinates.
        private Point WtoD(Point point)
        {
            return WtoDMatrix.Transform(point);
        }

        // Transform a point from device to world coordinates.
        private Point DtoW(Point point)
        {
            return DtoWMatrix.Transform(point);
        }

        // Position a label at the indicated point.
        private void DrawText(Canvas can, string text, Point location, double font_size,
                              HorizontalAlignment halign, VerticalAlignment valign      )
        {
            // Make the label.
            Label label = new Label();
            label.Content = text;
            label.FontSize = font_size;
            can.Children.Add(label);

            // Position the label.
            label.Measure(new Size(double.MaxValue, double.MaxValue));

            double x = location.X;
            if (halign == HorizontalAlignment.Center)
                x -= label.DesiredSize.Width / 2;
            else if (halign == HorizontalAlignment.Right)
                x -= label.DesiredSize.Width;
            Canvas.SetLeft(label, x);

            double y = location.Y;
            if (valign == VerticalAlignment.Center)
                y -= label.DesiredSize.Height / 2;
            else if (valign == VerticalAlignment.Bottom)
                y -= label.DesiredSize.Height;
            Canvas.SetTop(label, y);
        }

/*
        // Draw a simple graph.
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            const double margin = 10;
            double xmin = margin;
            double xmax = canGraph.Width - margin;
            double ymin = margin;
            double ymax = canGraph.Height - margin;
            const double step = 10;

            // Make the X axis.
            GeometryGroup xaxis_geom = new GeometryGroup();
            xaxis_geom.Children.Add(new LineGeometry(
                new Point(0, ymax), new Point(canGraph.Width, ymax)));
            for (double x = xmin + step;
                x <= canGraph.Width - step; x += step)
            {
                xaxis_geom.Children.Add(new LineGeometry(
                    new Point(x, ymax - margin / 2),
                    new Point(x, ymax + margin / 2)));
            }

            Path xaxis_path = new Path();
            xaxis_path.StrokeThickness = 1;
            xaxis_path.Stroke = Brushes.Black;
            xaxis_path.Data = xaxis_geom;

            canGraph.Children.Add(xaxis_path);

            // Make the Y ayis.
            GeometryGroup yaxis_geom = new GeometryGroup();
            yaxis_geom.Children.Add(new LineGeometry(
                new Point(xmin, 0), new Point(xmin, canGraph.Height)));
            for (double y = step; y <= canGraph.Height - step; y += step)
            {
                yaxis_geom.Children.Add(new LineGeometry(
                    new Point(xmin - margin / 2, y),
                    new Point(xmin + margin / 2, y)));
            }

            Path yaxis_path = new Path();
            yaxis_path.StrokeThickness = 1;
            yaxis_path.Stroke = Brushes.Black;
            yaxis_path.Data = yaxis_geom;

            canGraph.Children.Add(yaxis_path);

            // Make some data sets.
            Brush[] brushes = { Brushes.Red, Brushes.Green, Brushes.Blue };
            Random rand = new Random();
            for (int data_set = 0; data_set < 3; data_set++)
            {
                int last_y = rand.Next((int)ymin, (int)ymax);

                PointCollection points = new PointCollection();
                for (double x = xmin; x <= xmax; x += step)
                {
                    last_y = rand.Next(last_y - 10, last_y + 10);
                    if (last_y < ymin) last_y = (int)ymin;
                    if (last_y > ymax) last_y = (int)ymax;
                    points.Add(new Point(x, last_y));
                }

                Polyline polyline = new Polyline();
                polyline.StrokeThickness = 1;
                polyline.Stroke = brushes[data_set];
                polyline.Points = points;

                canGraph.Children.Add(polyline);
            }
        }
  

 */


    }



    public interface IPlotData
    {
        List<ObservableCollection<double>> lines { get; }
    }
}
