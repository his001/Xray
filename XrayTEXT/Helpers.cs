using System;
using System.Collections;
using System.Collections.ObjectModel;
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

        /// <summary>
        /// 폴더에서 사진을 가져와 byte[]로 변환
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static byte[] GetPhoto(string filePath)
        {
            FileStream stream = new FileStream(
                filePath, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(stream);

            byte[] photo = reader.ReadBytes((int)stream.Length);

            reader.Close();
            stream.Close();

            return photo;
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
            string cutfileName,
            string fullPath,
            Int32 fileNum,
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

    public class TalkBoxLayer : BindableObject
    {
        #region 선언

        private TalkBoxLayerCtrl _TxtBoxL_AddCont;
        private AdornerLayer _TxtBoxLayer;
        private double _horizPercent, _vertPercent;
        private Image _image;
        private bool _isDeleted;
        private string _text;
        private string _pX;
        private string _pY;
        private string _pW;
        private string _pH;
        private string _KeyFilename;
        private string _cutfileName;
        private string _FullPath;
        private Int32 _FileNum;
        //private Image _Talkimg;

        #endregion // Data

        #region Private Constructor 

        private TalkBoxLayer(
            string keyFilename, string cutfileName, string fullPath, Int32 fileNum,
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
                    throw new ArgumentException("image has invalid dimensions");

                _horizPercent = TalkBoxLocation.X / imageSize.Width;
                _vertPercent = TalkBoxLocation.Y / imageSize.Height;

                _KeyFilename = keyFilename;
                _cutfileName = cutfileName;
                _FullPath = fullPath;
                _FileNum = fileNum;


                _TxtBoxL_AddCont = new TalkBoxLayerCtrl(
                            keyFilename, cutfileName, fullPath, fileNum,
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

        #endregion // Private Constructor

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
            string keyFilename,
            string cutfileName, string fullPath, Int32 fileNum,
            Image image, Point TalkBoxLocation, Style TalkBoxStyle, Style TalkBoxEditorStyle)
        {
            return new TalkBoxLayer(keyFilename, cutfileName, fullPath, fileNum, TalkBoxLocation, image, TalkBoxStyle, TalkBoxEditorStyle);
        }


        /// <summary>
        /// Removes the adorner from the element tree.
        /// </summary>
        public void Delete()
        {
            this.IsDeleted = true;
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
                    throw new InvalidOperationException("Cannot set IsDeleted to false.");

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

        #region Text

        /// <summary>
        /// Gets/sets the annotation's text.
        /// </summary>
        public string Text
        {
            get { return _text; }
            set
            {
                if (_text == value)
                    return;

                _text = value;

                base.RaisePropertyChanged("Text");
            }
        }

        #endregion // Text

        #region ##### AnnotePoint X,Y #####
        public string TalkBoxLyerPointX
        {
            get
            {
                //return _pX;
                return (_image.RenderSize.Width * _horizPercent).ToString();
            }
            set
            {
                if (_pX == value)
                    return;

                _pX = value;
            }

            //double _x = _image.RenderSize.Width * _horizPercent;
            //return _x.ToString();
        }

        public string TalkBoxLyerPointY
        {
            //double y = _image.RenderSize.Height * _vertPercent;
            //return y.ToString();

            get
            {
                //return _pY;
                return (_image.RenderSize.Height * _vertPercent).ToString();
            }
            set
            {
                if (_pY == value)
                    return;

                _pY = value;
            }
        }
        #endregion ##### AnnotePoint X,Y #####

        #region ##### AnnoteImageSize W,H #####
        public string TalkBoxLyerSizeW
        {
            get
            {
                return (_TxtBoxL_AddCont.RenderSize.Width).ToString();
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
                return (_TxtBoxL_AddCont.RenderSize.Height).ToString();
            }
            set
            {
                if (_pH == value)
                    return;

                _pH = value;
            }
        }
        #endregion ##### AnnoteImageSize W,H #####



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
        /// 잘라낸 서브 이미지 파일 저장 경로
        /// </summary>
        public string TalkBoxLyerFullPath
        {
            get { return _FullPath; }
            set { _FullPath = value; }
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
        //    string _strKV = MainWin.GetWindow.//.getTxtDocTalk();
        //}

        void TalkBoxLyer_Insert()
        {
            if (_isDeleted)
                return;

            _TxtBoxLayer = AdornerLayer.GetAdornerLayer(_image);
            if (_TxtBoxLayer == null)
            {
                //_TxtBoxLayer.Add(_TxtBoxL_AddCont);
                //_TxtBoxLayer.Add(null);


                throw new ArgumentException("image does not have have an adorner layer.");
            }
            else
            {
                // Add the adorner to the Image's adorner layer.
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
            _TxtBoxL_AddCont.UpdateTextLocation(newLocation);
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

    /// <summary>
    /// This class describes a single photo - its location, the image and 
    /// the metadata extracted from the image.
    /// </summary>
    public class Photo
    {
        public Photo(string path)
        {
            _path = path;
            _source = new Uri(path);
            try
            {
                _image = BitmapFrame.Create(_source);
            }
            catch (NotSupportedException)
            {
            }

            //_metadata = new ExifMetadata(_source);
            //_metadata = null;
        }

        public override string ToString()
        {
            return _source.ToString();
        }

        private string _path;

        private Uri _source;
        public string Source { get { return _path; } }

        private BitmapFrame _image;
        public BitmapFrame Image { get { return _image; } set { _image = value; } }

    }

    /// <summary>
    /// This class represents a collection of photos in a directory.
    /// </summary>
    public class PhotoCollection : ObservableCollection<Photo>
    {
        public PhotoCollection() { }

        public PhotoCollection(string path) : this(new DirectoryInfo(path)) { }

        public PhotoCollection(DirectoryInfo directory)
        {
            _directory = directory;
            GetImageRead();
        }

        public string Path
        {
            set
            {
                _directory = new DirectoryInfo(value);
                GetImageRead();
            }
            get { return _directory.FullName; }
        }

        public DirectoryInfo Directory
        {
            set
            {
                _directory = value;
                GetImageRead();
            }
            get { return _directory; }
        }

        private void GetImageRead()
        {
            this.Clear();
            try
            {
                foreach (FileInfo f in
                    //_directory.GetFiles("*.jpg")
                    _directory.GetFiles("*.jpg").Union(_directory.GetFiles("*.png"))
                    )
                {
                    Add(new Photo(f.FullName));
                }
            }
            catch (DirectoryNotFoundException)
            {
                System.Windows.MessageBox.Show("No Such Directory");
            }
        }

        DirectoryInfo _directory;
    }

}