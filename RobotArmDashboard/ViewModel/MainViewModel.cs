using System;
using System.Windows;
using System.Windows.Media;
using GalaSoft.MvvmLight;

namespace Kobush.RobotArm.Dashboard.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly RobotArmDashboardService _service;

        internal MainViewModel(RobotArmDashboardService service)
        {
            _service = service;
        }
    }

    public class SideView : FrameworkElement
    {
        private DrawingVisual _gridVisual;

        private Rect _visiableArea;
        private double _gridFrequency = 2;

        public SideView()
        {
            _visiableArea = new Rect(-20, -5, 40, 40);

            _gridVisual = new DrawingVisual();

            UpdateGrid();
        }

        private void UpdateGrid()
        {
            using (var dc = _gridVisual.RenderOpen())
            {
                Pen pen = new Pen(Brushes.Gray, 1.0);

                var left = (int)Math.Floor(_visiableArea.Left / _gridFrequency);
                var right = (int)Math.Ceiling(_visiableArea.Right / _gridFrequency);

                for (int x = left; x <= right; x++)
                {
                    dc.DrawLine(pen, new Point(x*_gridFrequency, _visiableArea.Top),
                                new Point(x*_gridFrequency, _visiableArea.Bottom));
                }
            }

            _gridVisual.Transform = new TranslateTransform(-_visiableArea.Left, -_visiableArea.Top);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return _visiableArea.Size;
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index == 0)
                return _gridVisual;
            return null;
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return 1;
            }
        }
    }
}