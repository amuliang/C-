using System;
using System.Collections.Generic;
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
using System.Globalization;

namespace BezierDrawing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BezierCurve bsr;
        private List<BezierCurve> bcs = new List<BezierCurve>();
        private BezierCurve _selectedBC = null;

        private bool mouseDown = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Drawing()
        {
            if (_selectedBC != null) _selectedBC.Selected = false;
            //新建一条贝塞尔曲线
            bsr = new BezierCurve(canvas);
            bsr.Stroke = Brushes.Green;//设置描边颜色
            bsr.StrokeThickness = strokeSize.Value;//设置描边粗细
            bsr.OperateMode = OperateMode.Add;
            _selectedBC = bsr;

            bcs.Add(bsr);
            curveList.Items.Add("曲线" + bcs.Count.ToString());
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if(mouseDown && bsr != null)
            {
                bsr.MouseMove(e);
            }
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseDown = true;
            if(bsr != null) bsr.MouseDown(e);
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            mouseDown = false;
            if (bsr != null) bsr.MouseUp(e);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            bsr.OperateMode = OperateMode.Add;
        }

        private void delete_Click(object sender, RoutedEventArgs e)
        {
            bsr.OperateMode = OperateMode.Delete;
        }

        private void clear_Click(object sender, RoutedEventArgs e)
        {
            bsr.Clear();
        }

        private void clear_Copy_Click(object sender, RoutedEventArgs e)
        {
            bsr.Closed = !bsr.Closed;
        }

        private void newCurve_Click(object sender, RoutedEventArgs e)
        {
            Drawing();
        }

        private void moveCurve_Click(object sender, RoutedEventArgs e)
        {
            bsr.OperateMode = OperateMode.Move;
        }

        private void deleteCurve_Click(object sender, RoutedEventArgs e)
        {
            if (curveList.SelectedItem != null)
            {
                bsr.Delete();
                bcs.RemoveAt(curveList.SelectedIndex);
                curveList.Items.Remove(curveList.SelectedItem);
            }
        }

        private void curveList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (curveList.SelectedItem != null)
            {
                _selectedBC.Selected = false;
                bsr = bcs[curveList.SelectedIndex];
                bsr.Selected = true;
                _selectedBC = bsr;
                strokeSize.Value = bsr.StrokeThickness;
            }
        }

        private void strokeSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(bsr != null) bsr.StrokeThickness = strokeSize.Value;
        }

        /*Point2D PointOnCubicBezier(Point2D* cp, float t)
        {
            float ax, bx, cx; float ay, by, cy;
            float tSquared, tCubed; Point2D result;
            // 计算多项式系数 
            cx = 3.0 * (cp[1].x - cp[0].x);
            bx = 3.0 * (cp[2].x - cp[1].x) - cx;
            ax = cp[3].x - cp[0].x - cx - bx;
            cy = 3.0 * (cp[1].y - cp[0].y);
            by = 3.0 * (cp[2].y - cp[1].y) - cy;
            ay = cp[3].y - cp[0].y - cy - by;
            // 计算t位置的点值 
            tSquared = t * t;
            tCubed = tSquared * t;
            result.x = (ax * tCubed) + (bx * tSquared) + (cx * t) + cp[0].x;
            result.y = (ay * tCubed) + (by * tSquared) + (cy * t) + cp[0].y;
            return result;
        } */
    }//end of mainWindow
}
/*
namespace BrawDraw.Com.Test
{
    class CanvasCustomPaint : Canvas
    {
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            //画矩形
            dc.DrawRectangle(Brushes.Red, new Pen(Brushes.Blue, 1),
                new Rect(new Point(20, 20), new Size(100, 100)));
            //画文字
            dc.DrawText(new FormattedText("Hello, World!", CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, new Typeface("Arial"), 40, Brushes.Orange),
                new Point(50, 60));
        }
    }
}*/
