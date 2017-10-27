using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace XrayTEXT
{
    delegate void NoArgDelegate();

    public class Helpers
    {

        public static string dbCon = @"Data Source=.\SQLEXPRESS;AttachDbFilename=|DataDirectory|\xraydb.mdf;Integrated Security=True;User Instance=True";
        public static string PicFolder = @"D:\DEV\WPF\PRJ\XrayTEXT\XrayTEXT\Images";
        //public static MainWin mwin = new MainWin();
        //XrayTEXT.ViewModels.MainViewModel mainViewModel = new ViewModels.MainViewModel();

        //public static string keyFilename = mwin.PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/");  // Key 파일
        //public static string keyFilename = mwin.getKeyFileNameOnly();

        //MainWin 폼에 있는 텍스트라벨을 갱신하기 위해 델리게이트 사용
        //public delegate void deleg_TxtcutMemo(string upLabelText); //델리게이트 선언
        //public event deleg_TxtcutMemo ReturnToText; //델리게이트 이벤트 선언

        //public void SetTextChange()
        //{
        //    ReturnToText("이벤트 호출 SetTextChange");//이벤트 호출
        //}


        /// <summary>
        /// 폴더에서 사진을 가져와 byte[]로 변환
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static byte[] GetPhoto(string filePath)
        {
            FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(stream);

            byte[] photo = reader.ReadBytes((int)stream.Length);

            reader.Close();
            stream.Close();

            return photo;
        }

        /// <summary>
        /// png 파일로 부분 조각 후 저장
        /// </summary>
        /// <param name="_path"></param>
        /// <param name="_img">이미지에 사이즈가 들어가있다</param>
        /// <param name="top"></param>
        /// <param name="left"></param>
        public static void ExportToPng(string _path, Image _img, double top, double left)
        {
            try
            {
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)_img.Width, (int)_img.Height, 96d, 96d, System.Windows.Media.PixelFormats.Default);
                rtb.Render(_img);
                var crop = new CroppedBitmap(rtb, new Int32Rect((int)left, (int)top, (int)_img.ActualWidth, (int)_img.ActualHeight));
                BitmapEncoder pngEncoder = new PngBitmapEncoder();
                pngEncoder.Frames.Add(BitmapFrame.Create(crop));

                //FileInfo file = new FileInfo(_path);  // 읽기전용일때의 에러 처리
                //file.IsReadOnly = false;
                using (var fs = System.IO.File.OpenWrite(_path))
                {
                    pngEncoder.Save(fs);
                }
            }
            catch (Exception ex)
            {
            }
            GC.Collect();
        }

        /// <summary>
        /// 현재 작성중인 소견 레이어 정보를 DB에저장 합니다.
        /// </summary>
        /// <returns></returns>
        public static string SaveDB(TalkBoxLayer _talkBoxLayer)
        {
            string rtn = "";
            try
            {
                string _text = string.Empty;
                string constr = Helpers.dbCon;
                byte[] photo = Helpers.GetPhoto(_talkBoxLayer.TalkBoxLyerCutFullPath + "/" + _talkBoxLayer.TalkBoxLyercutfileName);

                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();

                    string sql = "";
                    sql = sql + "IF((SELECT COUNT(*) FROM TBL_TalkBoxLayer WITH(NOLOCK) WHERE KeyFilename = '" + _talkBoxLayer.TalkBoxLyerkeyFilename + "' AND CutFilename = '" + _talkBoxLayer.TalkBoxLyercutfileName + "') > 0)";
                    sql = sql + " BEGIN";
                    sql = sql + " update TBL_TalkBoxLayer set ";
                    sql = sql + " CutFullPath = '" + _talkBoxLayer.TalkBoxLyerCutFullPath + "', ";
                    sql = sql + " FileTitle = '" + _talkBoxLayer.TalkBoxLyerFileTitle + "', ";
                    sql = sql + " numb = '" + _talkBoxLayer.TalkBoxLyerFileNum.ToString() + "', ";
                    sql = sql + " memo = '" + _talkBoxLayer.Text + "', ";
                    sql = sql + " PointX = '" + _talkBoxLayer.TalkBoxLyerPointX + "', ";
                    sql = sql + " PointY = '" + _talkBoxLayer.TalkBoxLyerPointY + "', ";
                    sql = sql + " SizeW = '" + _talkBoxLayer.TalkBoxLyerSizeW + "', ";
                    sql = sql + " SizeH = '" + _talkBoxLayer.TalkBoxLyerSizeH + "', ";
                    sql = sql + " Fileimg = @Fileimg ";
                    sql = sql + " WHERE KeyFilename = '" + _talkBoxLayer.TalkBoxLyerkeyFilename + "' and CutFilename = '" + _talkBoxLayer.TalkBoxLyercutfileName + "'";

                    sql = sql + " END";
                    sql = sql + " ELSE";
                    sql = sql + " BEGIN";
                    //print 'insert'
                    sql = sql + " insert into TBL_TalkBoxLayer(KeyFilename, CutFilename, CutFullPath, FileTitle, numb, memo, PointX, PointY, SizeW, SizeH, Fileimg) values ";
                    sql = sql + " ('";
                    sql = sql + _talkBoxLayer.TalkBoxLyerkeyFilename + "','";
                    sql = sql + _talkBoxLayer.TalkBoxLyercutfileName + "','";
                    sql = sql + _talkBoxLayer.TalkBoxLyerCutFullPath + "','";
                    sql = sql + _talkBoxLayer.TalkBoxLyerFileTitle + "',";
                    sql = sql + _talkBoxLayer.TalkBoxLyerFileNum.ToString() + ",'";
                    sql = sql + _talkBoxLayer.Text + "','";
                    sql = sql + _talkBoxLayer.TalkBoxLyerPointX + "','";
                    sql = sql + _talkBoxLayer.TalkBoxLyerPointY;
                    sql = sql + "','" + _talkBoxLayer.TalkBoxLyerSizeW;
                    sql = sql + "','" + _talkBoxLayer.TalkBoxLyerSizeH;
                    sql = sql + "',@Fileimg); update TBL_TalkBoxLayer set FileTitle ='" + _talkBoxLayer.TalkBoxLyerFileTitle + "' where KeyFilename = '" + _talkBoxLayer.TalkBoxLyerkeyFilename + "'; ";
                    sql = sql + "END";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@Fileimg", SqlDbType.Image, photo.Length).Value = photo;
                        int result = cmd.ExecuteNonQuery();
                        if (result == 1)
                        {
                            rtn = "success";
                        }
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
            //(Application.Current.MainWindow as MainWin).btnUpText.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); // 저장 후 화면의 소견을 리로드 하기위해
            //mwin.btnUpText.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); // 저장 후 화면의 소견을 리로드 하기위해

            //mainViewModel.UserCutMemo = _talkBoxLayer.Text;//.ToString();
            //mainViewModel.UserFileMemo = _talkBoxLayer.TalkBoxLyerFileTitle;
            //SetDeleteAllTextBox();
            //mwin.TxtcutMemo.Text = _talkBoxLayer.Text;
            //mwin.TxtFileTitle.Text = _talkBoxLayer.TalkBoxLyerFileTitle;

            //new Action(() => mwin.btnLoadText.RaiseEvent(new RoutedEventArgs(Button.ClickEvent))).SetTimeout(500);
            return rtn;
        }
    }

    public class TalkBoxLayerCtrl : Adorner
    {
        public TalkBoxLayerControl _control;
        private Point _location;
        private ArrayList _logicalChildren;

        #region Constructor

        public TalkBoxLayerCtrl(
            string keyFilename,
            string fileTitle,
            string cutfileName,
            string cutfullPath,
            Int32 fileNum,
            string memo,
            TalkBoxLayer txtLayer,
            Image adornedImage,
            Style _txt_layerStyle,
            Style TalkBoxEditorStyle,
            Point location
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

    public class TalkBoxLayer : BindableObject
    {
        #region 선언

        private TalkBoxLayerCtrl _TxtBoxL_AddCont;
        private AdornerLayer _TxtBoxLayer;
        private double _horizPercent, _vertPercent;
        private Image _image;
        private bool _isDeleted;
        private bool _isHidden;
        private string _memo;
        private string _pX;
        private string _pY;
        private string _pW;
        private string _pH;
        private string _KeyFilename;
        private string _fileTitle;
        private string _cutfileName;
        private string _cutfullPath;
        private Int32 _FileNum;
        #endregion // Data

        #region Private Constructor 

        private TalkBoxLayer(
            string keyFilename, string fileTitle, string cutfileName, string cutfullPath, Int32 fileNum, string memo,
            Point TalkBoxLocation,
            Image image,
            Style TalkBoxStyle,
            Style TalkBoxEditorStyle)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException("image");

                _image = image;
                this.HookImageEvents(true);

                Size imageSize = _image.RenderSize;
                if (imageSize.Height == 0 || imageSize.Width == 0)
                    throw new ArgumentException("이미지의 크기가 잘못되었습니다.");

                _horizPercent = TalkBoxLocation.X / imageSize.Width;
                _vertPercent = TalkBoxLocation.Y / imageSize.Height;

                _KeyFilename = keyFilename;
                _fileTitle = fileTitle;
                _cutfileName = cutfileName;
                _cutfullPath = cutfullPath;
                _FileNum = fileNum;
                _memo = memo;
                _pH = image.ActualHeight.ToString();
                _pW = image.ActualWidth.ToString();
                _pX = TalkBoxLocation.X.ToString();
                _pY = TalkBoxLocation.Y.ToString();

                _TxtBoxL_AddCont = new TalkBoxLayerCtrl(
                            keyFilename, _fileTitle, cutfileName, cutfullPath, fileNum, memo,
                            this,
                            _image,
                            TalkBoxStyle,
                            TalkBoxEditorStyle,
                            TalkBoxLocation);

                this.TalkBoxLyer_Insert();
            }
            catch (Exception ex)
            {
            }
        }

        #endregion

        #region Create/Delete

        /// <summary>
        /// Creates an instance of TalkBoxLayer and sets the IsDeleted flag.
        /// </summary>
        /// <param name="image">The Image which contains the item being annotated and adorned.</param>
        /// <param name="TalkBoxLocation">The location of the text relative to the Image.</param>
        /// <param name="TalkBoxStyle">The Style applied to the TextBlock in the adorner.</param>
        /// <param name="TalkBoxEditorStyle">The Style applied to the TextBox in the adorner.</param>
        /// <returns>The new instance.</returns>
        public static TalkBoxLayer Create(
            string keyFilename, string fileTitle,
            string cutfileName, string cutfullPath, Int32 fileNum, string memo,
            Image image, Point TalkBoxLocation, Style TalkBoxStyle, Style TalkBoxEditorStyle)
        {
            return new TalkBoxLayer(keyFilename, fileTitle, cutfileName, cutfullPath, fileNum, memo, TalkBoxLocation, image, TalkBoxStyle, TalkBoxEditorStyle);
        }


        /// <summary>
        /// Removes the adorner from the element tree.
        /// </summary>
        public void Delete()
        {
            this.IsDeleted = true;
        }
        public void SetHidden()
        {
            this._isHidden = true;
        }

        #endregion // Create/Delete

        #region IsDeleted

        /// <summary>
        /// Returns true if the Delete method has been called on this object.
        /// </summary>
        public bool IsDeleted
        {
            get { return _isDeleted; }
            private set
            {
                if (!value)
                    throw new InvalidOperationException("삭제에 실패하였습니다.");

                if (_isDeleted)
                    return;

                _isDeleted = true;

                this.HookImageEvents(false);

                if (_TxtBoxL_AddCont != null)
                {
                    try
                    {
                        _TxtBoxLayer.Remove(_TxtBoxL_AddCont);
                        _TxtBoxLayer = null;
                        _TxtBoxL_AddCont = null;
                    }
                    catch (Exception ex) { }
                }
                base.RaisePropertyChanged("IsDeleted");
            }
        }

        #endregion // IsDeleted

        #region #################### TalkBoxLyer ####################
        public string TalkBoxLyerPointX
        {
            get
            {
                return _pX.ToString();
                //return (_image.RenderSize.Width * _horizPercent).ToString();
            }
            set
            {
                if (_pX == value)
                    return;

                _pX = value;
            }
        }
        public string TalkBoxLyerPointY
        {
            get
            {
                return _pY.ToString();
                //return (_image.RenderSize.Height * _vertPercent).ToString();
            }
            set
            {
                if (_pY == value)
                    return;

                _pY = value;
            }
        }

        public string TalkBoxLyerSizeW
        {
            get
            {
                //return (_TxtBoxL_AddCont.RenderSize.Width).ToString();
                return _pW;
            }
            set
            {
                if (_pW == value)
                    return;

                _pW = value;
            }
        }
        public string TalkBoxLyerSizeH
        {
            get
            {
                return _pH; // (_TxtBoxL_AddCont.RenderSize.Height).ToString();
            }
            set
            {
                if (_pH == value)
                    return;

                _pH = value;
            }
        }

        /// <summary>
        /// 키파일네임 
        /// </summary>
        public string TalkBoxLyerkeyFilename
        {
            get { return _KeyFilename; }
            set { _KeyFilename = value; }
        }

        /// <summary>
        /// 잘라낸 서브 이미지 파일 저장 경로 + 파일명
        /// </summary>
        public string TalkBoxLyercutfileName
        {
            get { return _cutfileName; }
            set { _cutfileName = value; }
        }

        /// <summary>
        /// Gets/sets the annotation's Text.
        /// </summary>
        public string Text
        {
            get { return _memo; }
            set
            {
                if (_memo == value)
                    return;
                _memo = value;
                base.RaisePropertyChanged("Text");

                //if (_text == value)
                //    return;
                //_text = value;
                //base.RaisePropertyChanged("Text");
            }
        }

        /// <summary>
        /// 잘라낸 서브 이미지 파일 저장 경로
        /// </summary>
        public string TalkBoxLyerCutFullPath
        {
            get { return _cutfullPath; }
            set { _cutfullPath = value; }
        }

        public string TalkBoxLyerFileTitle
        {
            get { return _fileTitle; }
            set { _fileTitle = value; }
        }

        public bool TalkBoxLyerisHidden
        {
            get { return _isHidden; }
            private set
            {
                if (!value)
                    throw new InvalidOperationException("숨기기에 실패하였습니다.");

                if (_isHidden)
                    return;

                _isHidden = true;

                this.HookImageEvents(false);

                if (_TxtBoxL_AddCont != null)
                {
                    try
                    {
                        _TxtBoxLayer.Visibility = Visibility.Hidden;
                        //_TxtBoxLayer = null;
                        //_TxtBoxL_AddCont = null;
                    }
                    catch (Exception ex) { }
                }
                base.RaisePropertyChanged("IsHidden");
            }
        }


        /// <summary>
        /// 잘라낸 서브 이미지 파일 번호
        /// </summary>
        public Int32 TalkBoxLyerFileNum
        {
            get { return _FileNum; }
            set { _FileNum = value; }
        }

        public Image TalkBoxLyerImg
        {
            get { return _image; }
            set { _image = value; }
        }
        #endregion #################### TalkBoxLyer ####################


        #region Private Helpers

        Point CalculateEquivalentTextLocation()
        {
            double x = _image.RenderSize.Width * _horizPercent;
            double y = _image.RenderSize.Height * _vertPercent;

            return new Point(x, y);
        }

        void HookImageEvents(bool hook)
        {
            if (hook)
            {
                // Monitor changes to the size of the Image, so that
                // we know when to relocate the annotation text.
                _image.SizeChanged += this.OnImageSizeChanged;
                _image.Loaded += this.OnImageLoaded;
            }
            else
            {
                _image.SizeChanged -= this.OnImageSizeChanged;
                _image.Loaded -= this.OnImageLoaded;
            }
        }

        //void makeTalkBox() {
        //    //TalkBoxLayer(
        //    //        Point TalkBoxLocation,
        //    //        Image image,
        //    //        Style TalkBoxStyle,
        //    //        Style TalkBoxEditorStyle);
        //    string _strKV = MainWin.GetWindow.//.getTxtFileTitle();
        //}

        void TalkBoxLyer_Insert()
        {
            if (_isDeleted) { return; }
            _TxtBoxLayer = AdornerLayer.GetAdornerLayer(_image);
            if (_TxtBoxLayer == null)
            {
                throw new ArgumentException("텍스트 레이어가 없습니다.");
            }
            else
            {
                _TxtBoxLayer.Add(_TxtBoxL_AddCont);
            }
        }

        void OnImageLoaded(object sender, RoutedEventArgs e)
        {
            // If the Image element is in loaded/unloaded more than once
            // then we need to put our adorner in its adorner layer each
            // time it's loaded.  This can happen if the Image is in a 
            // TabControl, when the user switches between tabs.
            this.TalkBoxLyer_Insert();
        }

        void OnImageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Point newLocation = this.CalculateEquivalentTextLocation();
            try
            {
                _TxtBoxL_AddCont.UpdateTextLocation(newLocation);
            }
            catch (Exception ex)
            {
                //MessageBox.Show("OnImageSizeChanged : " + ex.ToString());
            }
        }

        #endregion // Private Helpers

    }

    public class TalkBox : Adorner
    {
        Thumb topLeft, topRight, bottomLeft, bottomRight;
        VisualCollection visualChildren;

        public TalkBox(UIElement adornedElement)
            : base(adornedElement)
        {
            visualChildren = new VisualCollection(this);

            BuildAdornerCorner(ref topLeft, Cursors.SizeNWSE);
            BuildAdornerCorner(ref topRight, Cursors.SizeNESW);
            BuildAdornerCorner(ref bottomLeft, Cursors.SizeNESW);
            BuildAdornerCorner(ref bottomRight, Cursors.SizeNWSE);

            // Add handlers for resizing.
            bottomLeft.DragDelta += new DragDeltaEventHandler(HandleBottomLeft);
            bottomRight.DragDelta += new DragDeltaEventHandler(HandleBottomRight);
            topLeft.DragDelta += new DragDeltaEventHandler(HandleTopLeft);
            topRight.DragDelta += new DragDeltaEventHandler(HandleTopRight);
        }

        // Handler for resizing from the bottom-right.
        void HandleBottomRight(object sender, DragDeltaEventArgs args)
        {
            FrameworkElement adornedElement = this.AdornedElement as FrameworkElement;
            Thumb hitThumb = sender as Thumb;

            if (adornedElement == null || hitThumb == null) return;
            FrameworkElement parentElement = adornedElement.Parent as FrameworkElement;

            // Ensure that the Width and Height are properly initialized after the resize.
            EnforceSize(adornedElement);

            // Change the size by the amount the user drags the mouse, as long as it's larger 
            // than the width or height of an adorner, respectively.
            adornedElement.Width = Math.Max(adornedElement.Width + args.HorizontalChange, hitThumb.DesiredSize.Width);
            adornedElement.Height = Math.Max(args.VerticalChange + adornedElement.Height, hitThumb.DesiredSize.Height);
        }

        // Handler for resizing from the bottom-left.
        void HandleBottomLeft(object sender, DragDeltaEventArgs args)
        {
            FrameworkElement adornedElement = AdornedElement as FrameworkElement;
            Thumb hitThumb = sender as Thumb;

            if (adornedElement == null || hitThumb == null) return;

            // Ensure that the Width and Height are properly initialized after the resize.
            EnforceSize(adornedElement);

            // Change the size by the amount the user drags the mouse, as long as it's larger 
            // than the width or height of an adorner, respectively.
            adornedElement.Width = Math.Max(adornedElement.Width - args.HorizontalChange, hitThumb.DesiredSize.Width);
            adornedElement.Height = Math.Max(args.VerticalChange + adornedElement.Height, hitThumb.DesiredSize.Height);
        }

        // Handler for resizing from the top-right.
        void HandleTopRight(object sender, DragDeltaEventArgs args)
        {
            FrameworkElement adornedElement = this.AdornedElement as FrameworkElement;
            Thumb hitThumb = sender as Thumb;

            if (adornedElement == null || hitThumb == null) return;
            FrameworkElement parentElement = adornedElement.Parent as FrameworkElement;

            // Ensure that the Width and Height are properly initialized after the resize.
            EnforceSize(adornedElement);

            // Change the size by the amount the user drags the mouse, as long as it's larger 
            // than the width or height of an adorner, respectively.
            adornedElement.Width = Math.Max(adornedElement.Width + args.HorizontalChange, hitThumb.DesiredSize.Width);
            adornedElement.Height = Math.Max(adornedElement.Height - args.VerticalChange, hitThumb.DesiredSize.Height);
        }

        // Handler for resizing from the top-left.
        void HandleTopLeft(object sender, DragDeltaEventArgs args)
        {
            FrameworkElement adornedElement = AdornedElement as FrameworkElement;
            Thumb hitThumb = sender as Thumb;

            if (adornedElement == null || hitThumb == null) return;

            // Ensure that the Width and Height are properly initialized after the resize.
            EnforceSize(adornedElement);

            // Change the size by the amount the user drags the mouse, as long as it's larger 
            // than the width or height of an adorner, respectively.
            adornedElement.Width = Math.Max(adornedElement.Width - args.HorizontalChange, hitThumb.DesiredSize.Width);
            adornedElement.Height = Math.Max(adornedElement.Height - args.VerticalChange, hitThumb.DesiredSize.Height);
        }

        // Arrange the Adorners.
        protected override Size ArrangeOverride(Size finalSize)
        {
            // desiredWidth and desiredHeight are the width and height of the element that's being adorned.  
            // These will be used to place the ResizingAdorner at the corners of the adorned element.  
            double desiredWidth = AdornedElement.DesiredSize.Width;
            double desiredHeight = AdornedElement.DesiredSize.Height;
            // adornerWidth & adornerHeight are used for placement as well.
            double adornerWidth = this.DesiredSize.Width;
            double adornerHeight = this.DesiredSize.Height;

            topLeft.Arrange(new Rect(-adornerWidth / 2, -adornerHeight / 2, adornerWidth, adornerHeight));
            topRight.Arrange(new Rect(desiredWidth - adornerWidth / 2, -adornerHeight / 2, adornerWidth, adornerHeight));
            bottomLeft.Arrange(new Rect(-adornerWidth / 2, desiredHeight - adornerHeight / 2, adornerWidth, adornerHeight));
            bottomRight.Arrange(new Rect(desiredWidth - adornerWidth / 2, desiredHeight - adornerHeight / 2, adornerWidth, adornerHeight));

            // Return the final size.
            return finalSize;
        }

        // Helper method to instantiate the corner Thumbs, set the Cursor property, 
        // set some appearance properties, and add the elements to the visual tree.
        void BuildAdornerCorner(ref Thumb cornerThumb, Cursor customizedCursor)
        {
            if (cornerThumb != null) return;

            cornerThumb = new Thumb();

            // Set some arbitrary visual characteristics.
            //cornerThumb.BorderBrush = System.Windows.Media.Brushes.White;
            cornerThumb.Cursor = customizedCursor;
            cornerThumb.Height = cornerThumb.Width = 10;
            cornerThumb.Opacity = 0.40;
            cornerThumb.Background = new SolidColorBrush(Colors.MediumBlue);

            visualChildren.Add(cornerThumb);
        }

        // This method ensures that the Widths and Heights are initialized.  Sizing to content produces
        // Width and Height values of Double.NaN.  Because this Adorner explicitly resizes, the Width and Height
        // need to be set first.  It also sets the maximum size of the adorned element.
        void EnforceSize(FrameworkElement adornedElement)
        {
            if (adornedElement.Width.Equals(Double.NaN))
                adornedElement.Width = adornedElement.DesiredSize.Width;
            if (adornedElement.Height.Equals(Double.NaN))
                adornedElement.Height = adornedElement.DesiredSize.Height;

            FrameworkElement parent = adornedElement.Parent as FrameworkElement;
            if (parent != null)
            {
                adornedElement.MaxHeight = parent.ActualHeight;
                adornedElement.MaxWidth = parent.ActualWidth;
            }
        }

        // Override the VisualChildrenCount and GetVisualChild properties to interface with 
        // the adorner's visual collection.
        protected override int VisualChildrenCount { get { return visualChildren.Count; } }
        protected override Visual GetVisualChild(int index) { return visualChildren[index]; }
    }

    #region #### PropertyChangedEventHandler ####
    //public event PropertyChangedEventHandler PropertyChanged;
    //void OnPropertyChanged(string prop)
    //{
    //    if (this.PropertyChanged != null)
    //        this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
    //}

    //private void NotifyPropertyChanged(String info)
    //{
    //    var listeners = PropertyChanged;
    //    if (listeners != null)
    //    {
    //        PropertyChanged(this, new PropertyChangedEventArgs(info));
    //    }
    //}

    //private string _userFileMemo;
    //private string _userCutMemo;

    //public string UserCutMemo
    //{
    //    get { return _userCutMemo; }
    //    set
    //    {
    //        if (!string.Equals(this._userCutMemo, value))
    //        {
    //            this._userCutMemo = value;
    //            NotifyPropertyChanged("UserCutMemo");
    //        }
    //    }
    //}

    //public string UserFileMemo
    //{
    //    get { return _userFileMemo; }
    //    set
    //    {
    //        if (!string.Equals(this._userFileMemo, value))
    //        {
    //            this._userFileMemo = value;
    //            NotifyPropertyChanged("UserFileMemo");
    //        }
    //    }
    //}
    #endregion #### PropertyChangedEventHandler ####


}