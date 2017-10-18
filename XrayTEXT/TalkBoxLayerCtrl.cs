using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace XrayTEXT
{
    /// <summary>
    /// Renders an TalkBoxLayerControl in an Image's adorner layer.
    /// </summary>
    public class TalkBoxLayerCtrl : Adorner
    {
        public TalkBoxLayerControl _control;
        private Point _location;
        private ArrayList _logicalChildren;

        #region Constructor

        public TalkBoxLayerCtrl(
            TalkBoxLayer txtLayer,
            Image adornedImage,
            Style _txt_layerStyle,
            Style TalkBoxEditorStyle,
            Point location
            //, double width, double height
            )
            : base(adornedImage)
        {
            _location = location;

            _control = new TalkBoxLayerControl(txtLayer, _txt_layerStyle, TalkBoxEditorStyle);
            _control.Width = adornedImage.ActualWidth;
            _control.Height = adornedImage.ActualHeight;
            base.AddLogicalChild(_control);
            base.AddVisualChild(_control);
        }

        #endregion // Constructor

        #region UpdateTextLocation

        public void UpdateTextLocation(Point newLocation)
        {
            _location = newLocation;
            _control.InvalidateArrange();
        }

        #endregion // UpdateTextLocation

        #region Measure/Arrange

        /// <summary>
        /// Allows the control to determine how big it wants to be.
        /// </summary>
        /// <param name="constraint">A limiting size for the control.</param>
        protected override Size MeasureOverride(Size constraint)
        {
            _control.Measure(constraint);
            return _control.DesiredSize;
        }

        /// <summary>
        /// Positions and sizes the control.
        /// </summary>
        /// <param name="finalSize">The actual size of the control.</param>		
        protected override Size ArrangeOverride(Size finalSize)
        {
            Rect rect = new Rect(_location, finalSize);
            _control.Arrange(rect);
            return finalSize;
        }

        #endregion // Measure/Arrange

        #region Visual Children

        /// <summary>
        /// Required for the element to be rendered.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        /// <summary>
        /// Required for the element to be rendered.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
                throw new ArgumentOutOfRangeException("index");

            return _control;
        }

        #endregion // Visual Children

        #region Logical Children

        /// <summary>
        /// Required for the displayed element to inherit property values
        /// from the logical tree, such as FontSize.
        /// </summary>
        protected override IEnumerator LogicalChildren
        {
            get
            {
                if (_logicalChildren == null)
                {
                    _logicalChildren = new ArrayList();
                    _logicalChildren.Add(_control);
                }

                return _logicalChildren.GetEnumerator();
            }
        }

        #endregion // Logical Children
    }
}