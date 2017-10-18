using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace XrayTEXT
{
    /// <summary>
    /// An annotation which maintains a TalkBoxLayerCtrl's relative location within an Image.
    /// </summary>
    public class TalkBoxLayer : BindableObject
    {
        #region Data

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

        #endregion // Data

        #region Private Constructor 

        private TalkBoxLayer(
            Point TalkBoxLocation,
            Image image,
            Style TalkBoxStyle,
            Style TalkBoxEditorStyle)
        {
            try {
                if (image == null)
                    throw new ArgumentNullException("image");

                _image = image;
                this.HookImageEvents(true);

                Size imageSize = _image.RenderSize;
                if (imageSize.Height == 0 || imageSize.Width == 0)
                    throw new ArgumentException("image has invalid dimensions");

                _horizPercent = TalkBoxLocation.X / imageSize.Width;
                _vertPercent = TalkBoxLocation.Y / imageSize.Height;

                _TxtBoxL_AddCont = new TalkBoxLayerCtrl(
                    this,
                    _image,
                    TalkBoxStyle,
                    TalkBoxEditorStyle,
                    TalkBoxLocation);

                this.TalkBoxLyer_Insert();
            } catch (Exception ex) {
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
        public static TalkBoxLayer Create(Image image, Point TalkBoxLocation, Style TalkBoxStyle, Style TalkBoxEditorStyle)
        {
            return new TalkBoxLayer(TalkBoxLocation, image, TalkBoxStyle, TalkBoxEditorStyle);
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
            get {
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

            get {
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
}