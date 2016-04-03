using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BezierDrawing
{
    //对象类型
    enum BezierObjectType
    {
        Null,
        StartPoint,
        LeftControl,
        RightControl,
        AnchorPoint,
        LeftAndRightControl,
        RightAndLeftControl
    };
    //操作类型
    enum OperateMode
    {
        Null,
        Add,
        Insert,
        Move,
        Delete
    }
    //贝塞尔曲线
    class BezierCurve
    {
        #region 私有变量
        private Canvas _canvas = null;//画布

        private PathFigure _figure = new PathFigure();//贝塞尔线
        private Path _figurePath = new Path();//new的时候将_figure放入此路径中
        private List<Line> _lCtrlLines = new List<Line>();//所有左控制线
        private List<Line> _rCtrlLines = new List<Line>();//所有右控制线
        private List<Path> _lCtrlPaths = new List<Path>();//所有左控制点
        private List<Path> _rCtrlPaths = new List<Path>();//所有右控制点
        private List<Path> _anchorPaths = new List<Path>();//所有锚点
        private List<BezierSegment> _segments = new List<BezierSegment>();//所有片段，其中第一个片段不会放入_figure中，而是用来设置其StartPoint，故_figure.Count比_segments.Count少1
        //事件辅助变量
        private BezierObjectType currentObjectType = BezierObjectType.Null;//当前对象类型
        private int currentIndex;//当前对象index
        private Point _clickPos;//鼠标点击位置
        private Point _lastFrameMousePos;

        private bool _selected = true;
        #endregion

        #region 公有变量
        public OperateMode OperateMode = OperateMode.Null;//当前操作模式
        #endregion

        #region 属性
        //开始点
        public Point StartPoint
        {
            get {
                if (_segments.Count > 0)
                    return _segments[0].Point3;
                else
                    return new Point();
            }
            set {
                if (_segments.Count > 0)
                    adjAnchorPoint(0, value);
            }
        }
        //曲线颜色
        public Brush Stroke
        {
            get { return _figurePath.Stroke; }
            set { _figurePath.Stroke = value; }
        }
        //曲线描边
        public double StrokeThickness
        {
            get { return _figurePath.StrokeThickness; }
            set { _figurePath.StrokeThickness = value; }
        }
        //闭合,只有当点大于等于2的时候才能谈及闭合，否则无效
        public bool Closed
        {
            get { return _figure.Segments.Count == _segments.Count && _segments.Count > 1; }
            set {
                if (_segments.Count > 1)
                {
                    if (value && !Closed)
                    {
                        _figure.Segments.Add(_segments[0]);
                    }
                    else if (!value && Closed)
                    {
                        _figure.Segments.Remove(_segments[0]);
                    }
                }
            }
        }
        //是否被选择
        public bool Selected
        {
            get { return Selected; }
            set {
                showCtrlPoint(value);
                _selected = value;
            }
        }
        #endregion

        #region 函数
        //构造函数
        public BezierCurve(Canvas cs)
        {
            _canvas = cs;

            PathGeometry pg = new PathGeometry();
            pg.Figures.Add(_figure);
            _figurePath.Stroke = Stroke;
            _figurePath.StrokeThickness = StrokeThickness;
            _figurePath.Data = pg;
            _canvas.Children.Add(_figurePath);
        }
        //添加点
        public void Add(Point p1, Point p2, Point p3)
        {
            int count = _segments.Count;
            Line lCtrlLine = createLine(p3, p2);
            Line rCtrlLine = createLine(p3, p3);
            Path lCtrlPath = createDot(p2);
            Path rCtrlPath = createDot(p3);
            Path anchorPath = createDot(p3);
            BezierSegment segment = new BezierSegment(p1, p2, p3, true);
            if(count != 0)
            {
                //调整前一个片段
                _rCtrlLines[count - 1].X2 = p1.X;
                _rCtrlLines[count - 1].Y2 = p1.Y;
                ((EllipseGeometry)_rCtrlPaths[count - 1].Data).Center = p1;
                _figure.Segments.Add(segment);//如果不是第一个，就放入figure的片段中
            }
            else
            {
                _figure.StartPoint = p3;//如果是第一个的话让figure的第一个点为p3
            }
            _lCtrlLines.Add(lCtrlLine);
            _rCtrlLines.Add(rCtrlLine);
            _lCtrlPaths.Add(lCtrlPath);
            _rCtrlPaths.Add(rCtrlPath);
            _anchorPaths.Add(anchorPath);
            _segments.Add(segment);
            _canvas.Children.Add(lCtrlLine);
            _canvas.Children.Add(rCtrlLine);
            _canvas.Children.Add(lCtrlPath);
            _canvas.Children.Add(rCtrlPath);
            _canvas.Children.Add(anchorPath);
        }
        //插入点，暂时还没有写，不要用这个函数
        public void Insert(int index, Point p1, Point p2, Point p3)
        {
            _figure.Segments.Insert(index, new BezierSegment(p1, p2, p3, true));
        }
        //清空
        public void Clear()
        {
            showCtrlPoint(false);
            _figure.Segments.Clear();
            _segments.Clear();
            _lCtrlLines.Clear();
            _rCtrlLines.Clear();
            _lCtrlPaths.Clear();
            _rCtrlPaths.Clear();
            _anchorPaths.Clear();
        }
        //删除自身
        public void Delete()
        {
            Clear();
            _canvas.Children.Remove(_figurePath);
        }
        //移动整条曲线
        public void MoveTo(Point p)
        {
            for (int i = 0; i < _segments.Count; i++)
            {
                double x = _segments[i].Point3.X + (p.X - _lastFrameMousePos.X);
                double y = _segments[i].Point3.Y + (p.Y - _lastFrameMousePos.Y);
                adjAnchorPoint(i, new Point(x, y));
            }
        }
        //鼠标按下
        public void MouseDown(MouseButtonEventArgs e)
        {
            Point pp = Mouse.GetPosition(e.Source as FrameworkElement);//WPF方法
            _clickPos = pp;
            _lastFrameMousePos = pp;
            
            //获取鼠标所在的点
            for (int i = 0; i < _segments.Count; i++ )
            {
                BezierSegment bs = _segments[i];
                if(length2(pp, getDotCenter(_anchorPaths[i])) < 25)
                {
                    currentObjectType = BezierObjectType.AnchorPoint;
                }
                else if (length2(pp, getDotCenter(_lCtrlPaths[i])) < 25)
                {
                    currentObjectType = BezierObjectType.LeftControl;
                }
                else if (length2(pp, getDotCenter(_rCtrlPaths[i])) < 25)
                {
                    currentObjectType = BezierObjectType.RightControl;
                }
                if (currentObjectType != BezierObjectType.Null)
                {
                    currentIndex = i;
                    break;
                }
            }

            if(OperateMode == OperateMode.Add && currentObjectType == BezierObjectType.Null && !Closed)
            {
                Point p1;
                if (_segments.Count > 0)
                    p1 = ((EllipseGeometry)_rCtrlPaths[_rCtrlPaths.Count - 1].Data).Center;
                else
                    p1 = _clickPos;
                Add(p1, _clickPos, _clickPos);
                currentObjectType = BezierObjectType.RightAndLeftControl;
                currentIndex = _segments.Count - 1;
            }
            else if (OperateMode == OperateMode.Delete && currentObjectType == BezierObjectType.AnchorPoint)
            {
                deleteSegment(currentIndex);
            }
        }//end of mousedown
        //鼠标拖动
        public void MouseMove(MouseEventArgs e)
        {
            Point pp = Mouse.GetPosition(e.Source as FrameworkElement);//WPF方法

            if(this.OperateMode == OperateMode.Add)
            {
                switch(currentObjectType)
                {
                    case BezierObjectType.StartPoint:
                    case BezierObjectType.AnchorPoint: adjAnchorPoint(currentIndex, pp); break;
                    case BezierObjectType.LeftControl: adjLCtrlPoint(currentIndex, pp); break;
                    case BezierObjectType.RightControl: adjRCtrlPoint(currentIndex, pp); break;
                    case BezierObjectType.RightAndLeftControl:
                        adjRCtrlPoint(currentIndex, pp);
                        Point p = ((EllipseGeometry)_anchorPaths[currentIndex].Data).Center;
                        double x = p.X + ( p.X - pp.X );
                        double y = p.Y + ( p.Y - pp.Y );
                        adjLCtrlPoint(currentIndex, new Point(x, y)); 
                        break;
                }
            }
            else if(this.OperateMode == OperateMode.Move)
            {
                MoveTo(pp);
            }

            _lastFrameMousePos = pp;
        }
        //鼠标抬起
        public void MouseUp(MouseButtonEventArgs e)
        {
            currentObjectType = BezierObjectType.Null;
            currentIndex = -1;
        }
        #endregion

        #region 辅助函数
        //选择
        private void showCtrlPoint(bool b)
        {
            if (b && !_selected)
            {
                for (int i = 0; i < _segments.Count; i++)
                {
                    _canvas.Children.Add(_lCtrlLines[i]);
                    _canvas.Children.Add(_rCtrlLines[i]);
                    _canvas.Children.Add(_lCtrlPaths[i]);
                    _canvas.Children.Add(_rCtrlPaths[i]);
                    _canvas.Children.Add(_anchorPaths[i]);
                }
            }
            else if (!b && _selected)
            {
                for (int i = 0; i < _segments.Count; i++)
                {
                    _canvas.Children.Remove(_lCtrlLines[i]);
                    _canvas.Children.Remove(_rCtrlLines[i]);
                    _canvas.Children.Remove(_lCtrlPaths[i]);
                    _canvas.Children.Remove(_rCtrlPaths[i]);
                    _canvas.Children.Remove(_anchorPaths[i]);
                }
            }
        }
        //计算两个点之间的长度的平方
        private double length2(Point p1, Point p2)
        {
            return (p1.X - p2.X)*(p1.X - p2.X) + (p1.Y - p2.Y)*(p1.Y - p2.Y);
        }
        //创建一条线
        private Line createLine(Point p1, Point p2)
        {
            Line l = new Line();
            l.Stroke = Brushes.Red;
            l.StrokeThickness = 1;
            l.X1 = p1.X;
            l.Y1 = p1.Y;
            l.X2 = p2.X;
            l.Y2 = p2.Y;
            return l;
        }
        //创建一个圆
        private Path createDot(Point center)
        {
            EllipseGeometry eg = new EllipseGeometry();
            Path p = new Path();
            p.Stroke = Brushes.Blue;
            p.StrokeThickness = 1;
            eg.Center = center;
            eg.RadiusX = 2;
            eg.RadiusY = 2;
            p.Data = eg;
            return p;
        }
        //删除一个片段
        private void deleteSegment(int index)
        {
            //画布中删除
            _canvas.Children.Remove(_lCtrlLines[index]);
            _canvas.Children.Remove(_rCtrlLines[index]);
            _canvas.Children.Remove(_lCtrlPaths[index]);
            _canvas.Children.Remove(_rCtrlPaths[index]);
            _canvas.Children.Remove(_anchorPaths[index]);
            //数据中删除
            _lCtrlLines.RemoveAt(index);
            _rCtrlLines.RemoveAt(index);
            _lCtrlPaths.RemoveAt(index);
            _rCtrlPaths.RemoveAt(index);
            _anchorPaths.RemoveAt(index);
            //_figure中删除
            if(index == 0)
            {
                _figure.StartPoint = _segments[1].Point3;
                _figure.Segments.RemoveAt(index);
                _segments.RemoveAt(index);
            }
            else
            {
                _figure.Segments.RemoveAt(index-1);
                _segments.RemoveAt(index);
                //删除之后调整前一点的右控制点
                adjRCtrlPoint(index - 1, getDotCenter(_rCtrlPaths[index - 1]));
            }
        }
        //调整左控制点
        private void adjLCtrlPoint(int index, Point pp)
        {
            //控制线
            _lCtrlLines[index].X2 = pp.X;
            _lCtrlLines[index].Y2 = pp.Y;
            //控制点
            ((EllipseGeometry)_lCtrlPaths[index].Data).Center = pp;
            //对应segment
            _segments[index].Point2 = pp;
        }
        //调整右控制点
        private void adjRCtrlPoint(int index, Point pp)
        {
            //控制线
            _rCtrlLines[index].X2 = pp.X;
            _rCtrlLines[index].Y2 = pp.Y;
            //控制点
            ((EllipseGeometry)_rCtrlPaths[index].Data).Center = pp;
            //对应segment
            if (index != _segments.Count - 1)
            {
                _segments[index+1].Point1 = pp;
            }
            else
            {
                _segments[0].Point1 = pp;
            }
        }
        //调整锚点
        private void adjAnchorPoint(int index, Point pp) 
        {
            //左右控制点
            Point old = ((EllipseGeometry)_anchorPaths[index].Data).Center;
            Point oldl = ((EllipseGeometry)_lCtrlPaths[index].Data).Center;
            Point oldr = ((EllipseGeometry)_rCtrlPaths[index].Data).Center;
            Point pl = new Point(oldl.X + ( pp.X - old.X ), oldl.Y + ( pp.Y - old.Y ));
            Point pr = new Point(oldr.X + ( pp.X - old.X ), oldr.Y + ( pp.Y - old.Y ));
            adjLCtrlPoint(index, pl);
            adjRCtrlPoint(index, pr);
            //锚点
            ((EllipseGeometry)_anchorPaths[index].Data).Center = pp;
            _lCtrlLines[index].X1 = pp.X;
            _lCtrlLines[index].Y1 = pp.Y;
            _rCtrlLines[index].X1 = pp.X;
            _rCtrlLines[index].Y1 = pp.Y;
            //对应segment
            _segments[index].Point3 = pp;
            if(index == 0)
            {
                _figure.StartPoint = pp;
            }
        }
        //获取路径下的圆的圆心，因为我将一个EllipseGeometry对象作为了path的数据
        private Point getDotCenter(Path p)
        {
            return ((EllipseGeometry)p.Data).Center;
        }
        #endregion
    }//end of class bezier
}
