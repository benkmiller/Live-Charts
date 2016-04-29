﻿//The MIT License(MIT)

//Copyright(c) 2016 Alberto Rodriguez

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using LiveChartsCore;

namespace LiveChartsDesktop
{
    public class LineSeries : Series
    {
        private PathFigure _pathFigure;
        private LineSegment _right = new LineSegment(new Point(), false);
        private LineSegment _bottom = new LineSegment(new Point(), false);
        private LineSegment _left = new LineSegment(new Point(), false);

        private bool _isInView;

        public LineSeries()
        {
            Model = new LineAlgorithm(this);
            InitializeDefuaults();
        }

        public LineSeries(SeriesConfiguration configuration) : base(configuration)
        {
            Model = new LineAlgorithm(this);
            InitializeDefuaults();
        }

        public static readonly DependencyProperty PointRadiusProperty = DependencyProperty.Register(
            "PointRadius", typeof (double), typeof (LineSeries), new PropertyMetadata(default(double)));

        public double PointRadius
        {
            get { return (double) GetValue(PointRadiusProperty); }
            set { SetValue(PointRadiusProperty, value); }
        }

        public static readonly DependencyProperty PointForeroundProperty = DependencyProperty.Register(
            "PointForeround", typeof (Brush), typeof (LineSeries), 
            new PropertyMetadata(default(Brush), OnPropertyChanged()));

        public Brush PointForeround
        {
            get { return (Brush) GetValue(PointForeroundProperty); }
            set { SetValue(PointForeroundProperty, value); }
        }

        public static readonly DependencyProperty LineSmoothnessProperty = DependencyProperty.Register(
            "LineSmoothness", typeof (double), typeof (LineSeries), 
            new PropertyMetadata(default(double), OnPropertyChanged((v, m) =>
            {
                var lm = m as LineAlgorithm;
                var lv = v as LineSeries;
                if (lm == null || lv == null) return;
                lm.LineSmoothness = lv.LineSmoothness;
            })));

        public double LineSmoothness
        {
            get { return (double) GetValue(LineSmoothnessProperty); }
            set { SetValue(LineSmoothnessProperty, value); }
        }

        public override void InitializeView()
        {
            if (_isInView) return;

            _isInView = true;

            var path = new Path();
            BindingOperations.SetBinding(path, Shape.StrokeProperty,
                    new Binding { Path = new PropertyPath("Stroke"), Source = this });
            BindingOperations.SetBinding(path, Shape.FillProperty,
                new Binding { Path = new PropertyPath("Fill"), Source = this });
            BindingOperations.SetBinding(path, Shape.StrokeThicknessProperty,
                new Binding { Path = new PropertyPath("StrokeThickness"), Source = this });
            BindingOperations.SetBinding(path, VisibilityProperty,
                new Binding { Path = new PropertyPath("Visibility"), Source = this });
            BindingOperations.SetBinding(path, Panel.ZIndexProperty,
                new Binding { Path = new PropertyPath(Panel.ZIndexProperty), Source = this });
            BindingOperations.SetBinding(path, Shape.StrokeDashArrayProperty,
                new Binding { Path = new PropertyPath(StrokeDashArrayProperty), Source = this });
            var geometry = new PathGeometry();
            _pathFigure = new PathFigure();
            geometry.Figures.Add(_pathFigure);
            path.Data = geometry;
            Model.Chart.View.AddToDrawMargin(path);

            var wpfChart = Model.Chart.View as Chart;
            if (wpfChart == null) return;

            if (Stroke == null)
                SetValue(StrokeProperty, new SolidColorBrush(Chart.GetDefaultColor(wpfChart.Series.IndexOf(this))));
            if (Fill == null)
                SetValue(FillProperty,
                    new SolidColorBrush(Chart.GetDefaultColor(wpfChart.Series.IndexOf(this))) {Opacity = 0.35});
        }

        public override IChartPointView RenderPoint(IChartPointView view)
        {
            var mhr = PointRadius < 5 ? 5 : PointRadius;

            var pbv = (view as PointBezierView);

            if (pbv == null)
            {
                pbv = new PointBezierView
                {
                    Segment = new BezierSegment(),
                    Container = _pathFigure,
                    IsNew = true
                };
            }
            else
            {
                pbv.IsNew = false;
            }

            if ((Model.Chart.View.HasTooltip || Model.Chart.View.HasDataClickEventAttached) && pbv.HoverShape == null)
            {
                pbv.HoverShape = new Rectangle
                {
                    Fill = Brushes.Transparent,
                    StrokeThickness = 0,
                    Width = mhr,
                    Height = mhr
                };

                Panel.SetZIndex(pbv.HoverShape, int.MaxValue);
                BindingOperations.SetBinding(pbv.HoverShape, VisibilityProperty,
                    new Binding {Path = new PropertyPath(VisibilityProperty), Source = this});

                if (Model.Chart.View.HasDataClickEventAttached)
                {
                    var wpfChart = Model.Chart.View as Chart;
                    if (wpfChart == null) return null;
                    pbv.HoverShape.MouseDown += wpfChart.DataMouseDown;
                }

                Model.Chart.View.AddToDrawMargin(pbv.HoverShape);
            }

            if (Math.Abs(PointRadius) > 0.1 && pbv.Ellipse == null)
            {
                pbv.Ellipse = new Ellipse
                {
                    Width = PointRadius*2,
                    Height = PointRadius*2,
                    Stroke = PointForeround,
                    StrokeThickness = 1
                };
                BindingOperations.SetBinding(pbv.Ellipse, Shape.FillProperty,
                    new Binding {Path = new PropertyPath(StrokeProperty), Source = this});
                BindingOperations.SetBinding(pbv.Ellipse, VisibilityProperty,
                    new Binding {Path = new PropertyPath(VisibilityProperty), Source = this});

                Panel.SetZIndex(pbv.Ellipse, int.MaxValue - 2);

                Model.Chart.View.AddToDrawMargin(pbv.Ellipse);
            }

            if (DataLabels && pbv.DataLabel == null)
            {
                pbv.DataLabel = BindATextBlock(0);
                Panel.SetZIndex(pbv.DataLabel, int.MaxValue - 1);

                Model.Chart.View.AddToDrawMargin(pbv.DataLabel);
            }

            return pbv;
        }

        public override void RemovePointView(object view)
        {
            var bezierView = view as PointBezierView;
            if (bezierView == null) return;
            Model.Chart.View.RemoveFromView(bezierView.HoverShape);
            Model.Chart.View.RemoveFromView(bezierView.Ellipse);
            Model.Chart.View.RemoveFromView(bezierView.DataLabel);
            bezierView.Container.Segments.Remove(bezierView.Segment);
        }

        public override void CloseView()
        {
            _pathFigure.StartPoint = new Point(0, Model.Chart.DrawMargin.Height);

            _pathFigure.Segments.Remove(_right);
            _pathFigure.Segments.Remove(_bottom);
            _pathFigure.Segments.Remove(_left);

            var fp = Values.Points.FirstOrDefault() ?? new ChartPoint();

            _right.Point = new Point(Model.Chart.DrawMargin.Width, Model.Chart.DrawMargin.Height);
            _bottom.Point = new Point(0, Model.Chart.DrawMargin.Height);
            _left.Point = fp.ChartLocation.AsPoint();

            _left.Point = ChartFunctions.ToDrawMargin(fp, ScalesXAt, ScalesYAt, Model.Chart).AsPoint();

            _pathFigure.Segments.Add(_right);
            _pathFigure.Segments.Add(_bottom);
            _pathFigure.Segments.Insert(0, _left);
        }

        private void InitializeDefuaults()
        {
            SetValue(LineSmoothnessProperty, .7d);
            SetValue(PointRadiusProperty, 3.5d);
            SetValue(PointForeroundProperty, Brushes.WhiteSmoke);
            SetValue(StrokeThicknessProperty, 2d);
        }
    }
}
