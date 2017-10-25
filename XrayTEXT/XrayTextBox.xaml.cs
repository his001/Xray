using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace XrayTEXT
{
    /// <summary>
    /// 이미지 주석을 렌더링합니다. 이 컨트롤에는 IsInEditMode 상태가 있습니다.
    /// 사용자가 주석을 편집 할 수 있는지 여부를 결정합니다.
    /// IsInEditMode가 true이면 주석이 TextBox에 표시됩니다.
    /// 주석이 TextBlock에 표시되면 false입니다.
    /// </summary>
    public partial class TalkBoxLayerControl : ContentControl
	{

        private readonly TalkBoxLayer _talkBoxLayer;
		#region Constructor
		public TalkBoxLayerControl( TalkBoxLayer talkBoxLayer, Style _txt_layerStyle,  Style TalkBoxEditorStyle)
        {
            InitializeComponent();

			if (talkBoxLayer == null)
				throw new ArgumentNullException("talkBoxLayer");

			_talkBoxLayer = talkBoxLayer;
            
            // TalkBoxLayer의 텍스트 사이에 바인딩 설정 및 Content 속성을 변경 및 다른 속성을 업데이트
            Binding binding = new Binding("Text");
			binding.Source = _talkBoxLayer;
			binding.Mode = BindingMode.TwoWay;
			this.SetBinding(ContentControl.ContentProperty, binding);

            //이 컨트롤에 포커스 허용.
            //우리가 포커스를 capture 할 수 있도록 주석을 편집하는 데 사용되는 TextBox.
            base.Focusable = true;
            base.FocusVisualStyle = null;
            if (_txt_layerStyle != null)
            {
                base.Resources.Add("STYLE_Annotation", _txt_layerStyle);
            }

            if (TalkBoxEditorStyle != null)
            {
                base.Resources.Add("STYLE_AnnotationEditor", TalkBoxEditorStyle);
            }
            this.IsInEditMode = true;
          
        }

        #endregion // Constructor

        #region Public Properties

        /// <summary>
        /// 사용자가 텍스트를 편집 할 수 있는지 여부
        /// </summary>
        public bool IsInEditMode
        {
            get { return (bool)GetValue(IsInEditModeProperty); }
            set { SetValue(IsInEditModeProperty, value); }
        }

        /// <summary>
        /// InEditMode 속성
        /// </summary>
        public static readonly DependencyProperty IsInEditModeProperty =
            DependencyProperty.Register(
            "IsInEditMode",
            typeof(bool),
            typeof(TalkBoxLayerControl),
            new UIPropertyMetadata(false, OnIsInEditModeChanged));

        static void OnIsInEditModeChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            TalkBoxLayerControl control = depObj as TalkBoxLayerControl;
            if (control != null && !control.IsInEditMode)
            {
                // TalkBoxLayerControl 내의 TextBox에서 포커스를 멀리두고,
                // 그리고 TalkBoxLayerControl 자체에 초점을 맞 춥니 다.
                if (control.IsKeyboardFocusWithin)
                    control.Focus();

                // 사용자가 주석을 삭제하려고 시도
                control.AttemptToDelete();
            }
        }

        #endregion // Public Properties

        #region Event Handling Methods

        // 소견 텍스트 삭제시
        void OnDeleteAnnotation(object sender, RoutedEventArgs e)
        {
            string constr = Helpers.dbCon;
            using (SqlConnection conn = new SqlConnection(constr))
            {
                conn.Open();
                string sql = "DELETE FROM TBL_TalkBoxLayer WHERE KeyFilename = '" + _talkBoxLayer.TalkBoxLyerkeyFilename + "' and  CutFilename = '" + _talkBoxLayer.TalkBoxLyercutfileName + "' ;";
                // 삭제 후 numb(index) 재 배열 
                sql = sql + " UPDATE TBL_TalkBoxLayer SET numb = B.RowNum ";
                sql = sql + " FROM TBL_TalkBoxLayer A JOIN ";
                sql = sql + " ( ";
                sql = sql + "  SELECT t.idx , ROW_NUMBER() OVER (ORDER BY t.regdate) AS RowNum ";
                sql = sql + "  FROM TBL_TalkBoxLayer t with(nolock) ";
                sql = sql + "  WHERE t.KeyFilename = '" + _talkBoxLayer.TalkBoxLyerkeyFilename + "' ";
                sql = sql + " ) AS B on A.idx = B.idx ";
                sql = sql + " WHERE A.KeyFilename = '" + _talkBoxLayer.TalkBoxLyerkeyFilename + "'";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    int result = cmd.ExecuteNonQuery();
                    if (result == 1)
                    {
                        //MessageBox.Show("Image Added");
                        //정상
                    }
                }
                conn.Close();
            }
            this.Delete();
            //LoadTxtBoxDB();
            MainWin mwin = new MainWin();
            //mwin.btnLoadText.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // 기존 저장된 정보가 있다면 로딩
            new Action(() => mwin.btnLoadText.RaiseEvent(new RoutedEventArgs(Button.ClickEvent))).SetTimeout(500);
        }

        // 소견 텍스트 편집 모드 시작 시
        void OnTextBoxLoaded(object sender, RoutedEventArgs e)
        {
            TextBox txt = sender as TextBox;
            txt.Focus();

            // 사용자가 기존 주석을 편집하는 경우,
            // 문자를 클릭하고 거기에 캐럿을 넣습니다..
            int charIdx = txt.GetCharacterIndexFromPoint(Mouse.GetPosition(txt), true);
            if (charIdx > -1)
                txt.SelectionStart = charIdx;
        }

        // 소견 텍스트 편집 모드 종료 시
        void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            // 초점 변경이 처음 완료 될 수 있도록 잠시 지연.			
            base.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                (NoArgDelegate)delegate
                {
                    this.IsInEditMode = false;
                    //MainWin.SetSaveAllTextBox();
                    if (_talkBoxLayer.TalkBoxLyerCutFullPath != null) {
                        if (Helpers.SaveDB(_talkBoxLayer) == "success")
                        {
                            // 저장 성공
                        }
                    }
                }
            );
        }

        // 소견 텍스트 편집 할 때 
        void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
				if (Keyboard.Modifiers == ModifierKeys.Shift)
				{
                    // Shift + Enter 시 라인피드.
                    TextBox txt = sender as TextBox;
					txt.AppendText(Environment.NewLine);
					++txt.SelectionStart;
				}
				else
				{
					this.IsInEditMode = false;
                    //SaveDB();
                }
            }
            else if (e.Key == Key.Escape)
            {
                //삭제되지 않은 경우 (텍스트가있는 경우) 편집 모드에서 빠져 나옵니다..
                if (!this.AttemptToDelete())
                {
                    this.IsInEditMode = false;
                    //SaveDB();
                }
            }
        }

        // 사용자가 디스플레이 모드에서 주석을 클릭할때
        void OnTextBlockMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.IsInEditMode = true;
        }

        #endregion // Event Handling Methods

        bool AttemptToDelete()
        {
			bool deleted = String.IsNullOrEmpty(_talkBoxLayer.Text);
            if (deleted)
            {
                this.Delete();
            }
            return deleted;
        }

        void Delete()
        {
			_talkBoxLayer.Delete();
        }

        ///// <summary>
        ///// 현재 작성중인 소견 레이어 정보를 DB에서 불러옵니다.
        ///// </summary>
        ///// <returns></returns>
        //public DataSet SelectDB() {
        //    DataSet ds = new DataSet();

        //    try
        //    {
        //        string _text = string.Empty;
        //        string constr = Helpers.dbCon;
        //        using (SqlConnection conn = new SqlConnection(constr))
        //        {
        //            conn.Open();
        //            string sql = "Select idx, KeyFilename, CutFilename, CutFullPath, FileTitle, numb, memo, PointX, PointY, SizeW, SizeH, Fileimg, regdate ";
        //            sql = sql + " From TBL_TalkBoxLayer with(nolock) where KeyFilename ='"+ _talkBoxLayer.TalkBoxLyerkeyFilename + "' ";
        //            using (SqlCommand cmd = new SqlCommand(sql, conn))
        //            {
        //                var adapt = new SqlDataAdapter();
        //                adapt.SelectCommand = cmd;
        //                adapt.Fill(ds);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //    return ds;
        //}

        #region ########### adonation ##############
        //AdornerLayer aLayer;
        //Canvas myCanvas = new Canvas();

        //bool _isDown;
        //bool _isDragging;
        //bool selected = false;
        //UIElement selectedElement = null;

        //Point _startPoint;
        //private double _originalLeft;
        //private double _originalTop;
        //private void Window_Loaded(object sender, RoutedEventArgs e)
        //{
        //    this.MouseLeftButtonDown += new MouseButtonEventHandler(Window1_MouseLeftButtonDown);
        //    this.MouseLeftButtonUp += new MouseButtonEventHandler(DragFinishedMouseHandler);
        //    this.MouseMove += new MouseEventHandler(Window1_MouseMove);
        //    this.MouseLeave += new MouseEventHandler(Window1_MouseLeave);

        //    myCanvas.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(myCanvas_PreviewMouseLeftButtonDown);
        //    myCanvas.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(DragFinishedMouseHandler);
        //}
        //// Handler for drag stopping on leaving the window
        //void Window1_MouseLeave(object sender, MouseEventArgs e)
        //{
        //    StopDragging();
        //    e.Handled = true;
        //}

        //// Handler for drag stopping on user choise
        //void DragFinishedMouseHandler(object sender, MouseButtonEventArgs e)
        //{
        //    StopDragging();
        //    e.Handled = true;
        //}

        //// Method for stopping dragging
        //private void StopDragging()
        //{
        //    if (_isDown)
        //    {
        //        _isDown = false;
        //        _isDragging = false;
        //    }
        //}

        //// Hanler for providing drag operation with selected element
        //void Window1_MouseMove(object sender, MouseEventArgs e)
        //{
        //    if (_isDown)
        //    {
        //        if ((_isDragging == false) &&
        //            ((Math.Abs(e.GetPosition(myCanvas).X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance) ||
        //            (Math.Abs(e.GetPosition(myCanvas).Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)))
        //            _isDragging = true;

        //        if (_isDragging)
        //        {
        //            Point position = Mouse.GetPosition(myCanvas);
        //            Canvas.SetTop(selectedElement, position.Y - (_startPoint.Y - _originalTop));
        //            Canvas.SetLeft(selectedElement, position.X - (_startPoint.X - _originalLeft));
        //        }
        //    }
        //}

        //// Handler for clearing element selection, adorner removal
        //void Window1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (selected)
        //    {
        //        selected = false;
        //        if (selectedElement != null)
        //        {
        //            aLayer.Remove(aLayer.GetAdorners(selectedElement)[0]);
        //            selectedElement = null;
        //        }
        //    }
        //}

        //// Handler for element selection on the canvas providing resizing adorner
        //void myCanvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    // Remove selection on clicking anywhere the window
        //    if (selected)
        //    {
        //        selected = false;
        //        if (selectedElement != null)
        //        {
        //            // Remove the adorner from the selected element
        //            aLayer.Remove(aLayer.GetAdorners(selectedElement)[0]);
        //            selectedElement = null;
        //        }
        //    }

        //    // If any element except canvas is clicked, 
        //    // assign the selected element and add the adorner
        //    if (e.Source != myCanvas)
        //    {
        //        _isDown = true;
        //        _startPoint = e.GetPosition(myCanvas);

        //        selectedElement = e.Source as UIElement;

        //        _originalLeft = Canvas.GetLeft(selectedElement);
        //        _originalTop = Canvas.GetTop(selectedElement);

        //        aLayer = AdornerLayer.GetAdornerLayer(selectedElement);
        //        aLayer.Add(new ResizingAdorner(selectedElement));
        //        selected = true;
        //        e.Handled = true;
        //    }
        //}


        //#region ########### ResizingAdorner ##############
        //public class ResizingAdorner : Adorner
        //{
        //    // Resizing adorner uses Thumbs for visual elements.  
        //    // The Thumbs have built-in mouse input handling.
        //    Thumb topLeft, topRight, bottomLeft, bottomRight;

        //    // To store and manage the adorner's visual children.
        //    VisualCollection visualChildren;

        //    // Initialize the ResizingAdorner.
        //    public ResizingAdorner(UIElement adornedElement)
        //        : base(adornedElement)
        //    {
        //        visualChildren = new VisualCollection(this);

        //        // Call a helper method to initialize the Thumbs
        //        // with a customized cursors.
        //        BuildAdornerCorner(ref topLeft, Cursors.SizeNWSE);
        //        BuildAdornerCorner(ref topRight, Cursors.SizeNESW);
        //        BuildAdornerCorner(ref bottomLeft, Cursors.SizeNESW);
        //        BuildAdornerCorner(ref bottomRight, Cursors.SizeNWSE);

        //        // Add handlers for resizing.
        //        bottomLeft.DragDelta += new DragDeltaEventHandler(HandleBottomLeft);
        //        bottomRight.DragDelta += new DragDeltaEventHandler(HandleBottomRight);
        //        topLeft.DragDelta += new DragDeltaEventHandler(HandleTopLeft);
        //        topRight.DragDelta += new DragDeltaEventHandler(HandleTopRight);
        //    }

        //    // Handler for resizing from the bottom-right.
        //    void HandleBottomRight(object sender, DragDeltaEventArgs args)
        //    {
        //        FrameworkElement adornedElement = this.AdornedElement as FrameworkElement;
        //        Thumb hitThumb = sender as Thumb;

        //        if (adornedElement == null || hitThumb == null) return;
        //        FrameworkElement parentElement = adornedElement.Parent as FrameworkElement;

        //        // Ensure that the Width and Height are properly initialized after the resize.
        //        EnforceSize(adornedElement);

        //        // Change the size by the amount the user drags the mouse, as long as it's larger 
        //        // than the width or height of an adorner, respectively.
        //        adornedElement.Width = Math.Max(adornedElement.Width + args.HorizontalChange, hitThumb.DesiredSize.Width);
        //        adornedElement.Height = Math.Max(args.VerticalChange + adornedElement.Height, hitThumb.DesiredSize.Height);
        //    }

        //    // Handler for resizing from the top-right.
        //    void HandleTopRight(object sender, DragDeltaEventArgs args)
        //    {
        //        FrameworkElement adornedElement = this.AdornedElement as FrameworkElement;
        //        Thumb hitThumb = sender as Thumb;

        //        if (adornedElement == null || hitThumb == null) return;
        //        FrameworkElement parentElement = adornedElement.Parent as FrameworkElement;

        //        // Ensure that the Width and Height are properly initialized after the resize.
        //        EnforceSize(adornedElement);

        //        // Change the size by the amount the user drags the mouse, as long as it's larger 
        //        // than the width or height of an adorner, respectively.
        //        adornedElement.Width = Math.Max(adornedElement.Width + args.HorizontalChange, hitThumb.DesiredSize.Width);
        //        //adornedElement.Height = Math.Max(adornedElement.Height - args.VerticalChange, hitThumb.DesiredSize.Height);

        //        double height_old = adornedElement.Height;
        //        double height_new = Math.Max(adornedElement.Height - args.VerticalChange, hitThumb.DesiredSize.Height);
        //        double top_old = Canvas.GetTop(adornedElement);
        //        adornedElement.Height = height_new;
        //        Canvas.SetTop(adornedElement, top_old - (height_new - height_old));
        //    }

        //    // Handler for resizing from the top-left.
        //    void HandleTopLeft(object sender, DragDeltaEventArgs args)
        //    {
        //        FrameworkElement adornedElement = AdornedElement as FrameworkElement;
        //        Thumb hitThumb = sender as Thumb;

        //        if (adornedElement == null || hitThumb == null) return;

        //        // Ensure that the Width and Height are properly initialized after the resize.
        //        EnforceSize(adornedElement);

        //        // Change the size by the amount the user drags the mouse, as long as it's larger 
        //        // than the width or height of an adorner, respectively.
        //        //adornedElement.Width = Math.Max(adornedElement.Width - args.HorizontalChange, hitThumb.DesiredSize.Width);
        //        //adornedElement.Height = Math.Max(adornedElement.Height - args.VerticalChange, hitThumb.DesiredSize.Height);

        //        double width_old = adornedElement.Width;
        //        double width_new = Math.Max(adornedElement.Width - args.HorizontalChange, hitThumb.DesiredSize.Width);
        //        double left_old = Canvas.GetLeft(adornedElement);
        //        adornedElement.Width = width_new;
        //        Canvas.SetLeft(adornedElement, left_old - (width_new - width_old));

        //        double height_old = adornedElement.Height;
        //        double height_new = Math.Max(adornedElement.Height - args.VerticalChange, hitThumb.DesiredSize.Height);
        //        double top_old = Canvas.GetTop(adornedElement);
        //        adornedElement.Height = height_new;
        //        Canvas.SetTop(adornedElement, top_old - (height_new - height_old));
        //    }

        //    // Handler for resizing from the bottom-left.
        //    void HandleBottomLeft(object sender, DragDeltaEventArgs args)
        //    {
        //        FrameworkElement adornedElement = AdornedElement as FrameworkElement;
        //        Thumb hitThumb = sender as Thumb;

        //        if (adornedElement == null || hitThumb == null) return;

        //        // Ensure that the Width and Height are properly initialized after the resize.
        //        EnforceSize(adornedElement);

        //        // Change the size by the amount the user drags the mouse, as long as it's larger 
        //        // than the width or height of an adorner, respectively.
        //        //adornedElement.Width = Math.Max(adornedElement.Width - args.HorizontalChange, hitThumb.DesiredSize.Width);
        //        adornedElement.Height = Math.Max(args.VerticalChange + adornedElement.Height, hitThumb.DesiredSize.Height);

        //        double width_old = adornedElement.Width;
        //        double width_new = Math.Max(adornedElement.Width - args.HorizontalChange, hitThumb.DesiredSize.Width);
        //        double left_old = Canvas.GetLeft(adornedElement);
        //        adornedElement.Width = width_new;
        //        Canvas.SetLeft(adornedElement, left_old - (width_new - width_old));
        //    }

        //    // Arrange the Adorners.
        //    protected override Size ArrangeOverride(Size finalSize)
        //    {
        //        // desiredWidth and desiredHeight are the width and height of the element that's being adorned.  
        //        // These will be used to place the ResizingAdorner at the corners of the adorned element.  
        //        double desiredWidth = AdornedElement.DesiredSize.Width;
        //        double desiredHeight = AdornedElement.DesiredSize.Height;
        //        // adornerWidth & adornerHeight are used for placement as well.
        //        double adornerWidth = this.DesiredSize.Width;
        //        double adornerHeight = this.DesiredSize.Height;

        //        topLeft.Arrange(new Rect(-adornerWidth / 2, -adornerHeight / 2, adornerWidth, adornerHeight));
        //        topRight.Arrange(new Rect(desiredWidth - adornerWidth / 2, -adornerHeight / 2, adornerWidth, adornerHeight));
        //        bottomLeft.Arrange(new Rect(-adornerWidth / 2, desiredHeight - adornerHeight / 2, adornerWidth, adornerHeight));
        //        bottomRight.Arrange(new Rect(desiredWidth - adornerWidth / 2, desiredHeight - adornerHeight / 2, adornerWidth, adornerHeight));

        //        // Return the final size.
        //        return finalSize;
        //    }

        //    // Helper method to instantiate the corner Thumbs, set the Cursor property, 
        //    // set some appearance properties, and add the elements to the visual tree.
        //    void BuildAdornerCorner(ref Thumb cornerThumb, Cursor customizedCursor)
        //    {
        //        if (cornerThumb != null) return;

        //        cornerThumb = new Thumb();

        //        // Set some arbitrary visual characteristics.
        //        cornerThumb.Cursor = customizedCursor;
        //        cornerThumb.Height = cornerThumb.Width = 10;
        //        cornerThumb.Opacity = 0.40;
        //        cornerThumb.Background = new SolidColorBrush(Colors.MediumBlue);

        //        visualChildren.Add(cornerThumb);
        //    }

        //    // This method ensures that the Widths and Heights are initialized.  Sizing to content produces
        //    // Width and Height values of Double.NaN.  Because this Adorner explicitly resizes, the Width and Height
        //    // need to be set first.  It also sets the maximum size of the adorned element.
        //    void EnforceSize(FrameworkElement adornedElement)
        //    {
        //        if (adornedElement.Width.Equals(Double.NaN))
        //            adornedElement.Width = adornedElement.DesiredSize.Width;
        //        if (adornedElement.Height.Equals(Double.NaN))
        //            adornedElement.Height = adornedElement.DesiredSize.Height;

        //        FrameworkElement parent = adornedElement.Parent as FrameworkElement;
        //        if (parent != null)
        //        {
        //            adornedElement.MaxHeight = parent.ActualHeight;
        //            adornedElement.MaxWidth = parent.ActualWidth;
        //        }
        //    }
        //    // Override the VisualChildrenCount and GetVisualChild properties to interface with 
        //    // the adorner's visual collection.
        //    protected override int VisualChildrenCount { get { return visualChildren.Count; } }
        //    protected override Visual GetVisualChild(int index) { return visualChildren[index]; }
        //}
        //#endregion ########### ResizingAdorner ##############


        #endregion ########### adonation ##############
    }
}