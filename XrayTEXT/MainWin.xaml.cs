using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace XrayTEXT
{
    public partial class MainWin : System.Windows.Window
    {
        #region ######################### ���� #########################
        Point prePosition; //�巹�׸� ������ ���콺 ��ǥ;
        Rectangle currentRect; //���� �׷����� �׸�

        protected string _PicFolder = @"D:\DEV\WPF\PRJ\XrayTEXT\XrayTEXT\Images";
        public PhotoCollection Photos;

        readonly List<TalkBoxLayer> _LstTalkBoxLayer = new List<TalkBoxLayer>();  // �Ұ� ������
        double scaleX = 1;
        double scaleY = 1;
        TranslateTransform translate = new TranslateTransform();
        #endregion ######################### ���� #########################

        #region ######################### MainWin #########################
        public MainWin()
        {
            InitializeComponent();

            Photos = (PhotoCollection)(this.Resources["Photos"] as ObjectDataProvider).Data;
            Photos.Path = _PicFolder;
            
            this.root.MouseLeftButtonDown += new MouseButtonEventHandler(root_MouseLeftButtonDown);
            this.root.MouseLeftButtonUp += new MouseButtonEventHandler(root_MouseLeftButtonUp);
            this.root.MouseWheel += new MouseWheelEventHandler(root_MouseWheel);
            this.root.MouseMove += new MouseEventHandler(root_MouseMove);

            // ��ǥ�� ��Ÿ���� TextBlock�� �ֻ����� ...
            //Grid.SetZIndex(this.tbPosition, 99999);

            // Zoom In, Zoom Out�� ���� Center ��ǥ ����
            Zoom.CenterX = ViewedPhoto.Width / 2;
            Zoom.CenterY = ViewedPhoto.Height / 2;
            //ȸ���� Angle �� ������ �ǳ� �ϴ� ����

            //DataContext = new ModelTextbox() { TxWidth = 100, TxHeight=100 };
        }
        #endregion ######################### MainWin #########################

        #region ######## ���콺 ���� ################

        #region ####### ���콺 �� ###########
        void root_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                scaleX += 0.1;
                scaleY += 0.1;
                Zoom.ScaleX = scaleX;
                Zoom.ScaleY = scaleY;
            }
            else if (e.Delta < 0)
            {
                if (Zoom.ScaleX >= 0.1 && Zoom.ScaleY >= 0.1)
                {
                    scaleX = scaleX - 0.1;
                    scaleY = scaleY - 0.1;
                    Zoom.ScaleX = scaleX;
                    Zoom.ScaleY = scaleY;
                }
            }
        }
        #endregion ############ ���콺 �� ############

        /// <summary>
        /// ���콺 ���� ��ư Ŭ�� �̺�Ʈ �ڵ鷯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //���콺�� ��ǥ�� �����Ѵ�.
            prePosition = e.GetPosition(this.root);
            //���콺�� Grid������ ������ ��ġ�� �� �� �ֵ��� ���콺 �̺�Ʈ�� ĸó�Ѵ�.
            this.root.CaptureMouse();
            if (currentRect == null)
            {
                //�簢���� �����Ѵ�.
                CreteRectangle();
            }
        }

        /// <summary>
        /// ���콺 �̵� �̺�Ʈ �ڵ鷯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void root_MouseMove(object sender, MouseEventArgs e)
        {
            //���� �̵��� ���콺�� ��ǥ�� ���´�
            Point currnetPosition = e.GetPosition(this.root);
            //��ǥ�� ǥ���Ѵ�.
            //this.tbPosition.Text = string.Format("���콺 ��ǥ : [{0},{1}]", currnetPosition.X, currnetPosition.Y);
            //���콺 ���� ��ư�� ����������
            if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
            {
                if (currentRect != null)
                {
                    //�簢���� ��Ÿ�� �������� �����Ѵ�.
                    double left = prePosition.X;
                    double top = prePosition.Y;
                    //���콺�� ��ġ�� ���� ������ �������� �����Ѵ�.
                    if (prePosition.X > currnetPosition.X)
                    {
                        left = currnetPosition.X;
                    }
                    if (prePosition.Y > currnetPosition.Y)
                    {
                        top = currnetPosition.Y;
                    }
                    currentRect.Margin = new Thickness(left, top, 0, 0); //�簢���� ��ġ ������(Margin)�� �����Ѵ�
                    currentRect.Width = Math.Abs(prePosition.X - currnetPosition.X); //�簢���� ũ�⸦ �����Ѵ�. ������ ���� �� �����Ƿ� ���밪�� �����ش�.
                    currentRect.Height = Math.Abs(prePosition.Y - currnetPosition.Y);
                }
            }
            if (e.RightButton == MouseButtonState.Pressed)
            {
                Mouse.OverrideCursor = Cursors.Hand;
                this.root.CaptureMouse();
                Image image = ViewedPhoto;
                Point mousePre = e.GetPosition(this.root);
                double imgX = mousePre.X - (ViewedPhoto.Width / 2);
                double imgY = mousePre.Y - (ViewedPhoto.Height / 2);

                image.SetCurrentValue(LeftProperty, imgX);
                image.SetCurrentValue(TopProperty, imgY);
            }
            else
            {
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// ���콺 Ŭ�� �� ����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void root_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.root.ReleaseMouseCapture(); //���콺 ĸ�縦 �����Ѵ�.
            SetRectangleProperty();

            Image image = ViewedPhoto;
            if (Math.Abs(image.ActualWidth) > 10 && PhotosListBox.SelectedItem != null)
            {
                #region ########## �簢�� �ȿ� _talkLayer ���� ##########
                Point currnetPosition = e.GetPosition(this.root);
                double left = prePosition.X;
                double top = prePosition.Y;

                if (currentRect != null)
                {
                    if (prePosition.X > currnetPosition.X)
                    {
                        left = currnetPosition.X;
                    }
                    if (prePosition.Y > currnetPosition.Y)
                    {
                        top = currnetPosition.Y;
                    }
                }

                Point talkBoxLocationXY = new Point(left, top);
                Size _size = new Size(Math.Abs(prePosition.X - currnetPosition.X), Math.Abs(prePosition.Y - currnetPosition.Y)); // �簢�� ũ�� ��ŭ �ؽ�Ʈ ���̾� ũ�� ����
                image.RenderSize = _size; // �ؽ� Ʈ �ڽ� ũ��

                Style _cssTalkBox = base.FindResource("cssTalkBox") as Style;
                Style _cssTalkBoxEdit = base.FindResource("cssTalkBoxEdit") as Style;

                #region ########### ���� ###########
                //_cssTalkBox.Setters.Add(new Setter(TextBox.WidthProperty, _size.Width));
                //_cssTalkBox.Setters.Add(new Setter(TextBox.HeightProperty, _size.Height));
                //_cssTalkBoxEdit.Setters.Add(new Setter(TextBox.WidthProperty, _size.Width));
                //_cssTalkBoxEdit.Setters.Add(new Setter(TextBox.HeightProperty, _size.Height));

                //_cssTalkBox.Setters.OfType<Setter>().FirstOrDefault(X => X.Property == TextBox.WidthProperty).Value = _size.Width;
                //_cssTalkBox.Setters.OfType<Setter>().FirstOrDefault(X => X.Property == TextBox.HeightProperty).Value = _size.Height;

                //_cssTalkBoxEdit.Setters.OfType<Setter>().FirstOrDefault(X => X.Property == TextBox.WidthProperty).Value = _size.Width;
                //_cssTalkBoxEdit.Setters.OfType<Setter>().FirstOrDefault(X => X.Property == TextBox.HeightProperty).Value = _size.Height;

                //ModelTextbox mt = new ModelTextbox();
                //mt.TxWidth = _size.Width;
                //mt.TxHeight = _size.Height;
                //this.DataContext = mt;
                //this.DataContext = new ModelTextbox() { TxWidth = _size.Width, TxHeight = _size.Height };
                #endregion ########### ���� ###########

                TalkBoxLayer _talkBoxLayer = TalkBoxLayer.Create(
                    image,
                    talkBoxLocationXY,
                    _cssTalkBox,
                    _cssTalkBoxEdit);
                this.CurTalkBox.Add(_talkBoxLayer);

                //string _savePath = @"D:\DEV\WPF\saveImg\" + PhotosListBox.SelectedItem.ToString() + "\\" + _LstTalkBoxLayer.Count.ToString() + ".png";
                string _savePath = PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace(".jpg", "").Replace("/", "\\");
                System.IO.Directory.CreateDirectory(_savePath); // ������ ������ ����
                string _savePathFull = _savePath + "\\" + _LstTalkBoxLayer.Count.ToString() + ".png";
                
                ExportToPng(_savePathFull, image, top, left);

                #endregion ########## �簢�� �ȿ� _talkLayer ���� end ##########
                root.Children.Remove(currentRect); // �׷��� �׸�� ���� - obj ���� �ߴ��� ������ �ȵ� ���� �� null ó��
                currentRect.Visibility = Visibility.Hidden;
                currentRect = null;
                GC.Collect();
            }
            
        }

        public void ExportToPng(string _path, Image _img, double top, double left)
        {
            try
            {
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)_img.Width, (int)_img.Height, 96d, 96d, System.Windows.Media.PixelFormats.Default);
                rtb.Render(_img);
                var crop = new CroppedBitmap(rtb, new Int32Rect((int)left, (int)top, (int)_img.ActualWidth, (int)_img.ActualHeight));
                BitmapEncoder pngEncoder = new PngBitmapEncoder();
                pngEncoder.Frames.Add(BitmapFrame.Create(crop));

                //FileInfo file = new FileInfo(_path);  // �б������϶��� ���� ó��
                //file.IsReadOnly = false;
                using (var fs = System.IO.File.OpenWrite(_path))
                {
                    pngEncoder.Save(fs);
                }
            }
            catch (Exception ex) {
            }
        }

        #region ######## �׸� ################
        private void SetRectangleProperty()
        {
            try
            {
                //�簢���� ������ 100% �� ����
                currentRect.Opacity = 1;
                //�簢���� ������ ����
                currentRect.Fill = new SolidColorBrush(Colors.LightYellow);
                currentRect.Opacity = 0.35;
                //�簢���� �׵θ��� ������ ����
                currentRect.StrokeDashArray = new DoubleCollection();
            }
            catch (Exception e) { }
        }

        private void CreteRectangle()
        {

            currentRect = new Rectangle();
            currentRect.Stroke = new SolidColorBrush(Colors.White);
            currentRect.StrokeThickness = 1;
            currentRect.Opacity = 0.7;
            //�簢���� �׸��� ������ �׵θ��� Dash ��Ÿ�Ϸ� �����Ѵ�.
            DoubleCollection dashSize = new DoubleCollection();
            dashSize.Add(1);
            dashSize.Add(1);
            currentRect.StrokeDashArray = dashSize;
            currentRect.StrokeDashOffset = 0;
            //�簢���� ���� ������ �����Ѵ�.
            currentRect.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            currentRect.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            //�׸��忡 �߰��Ѵ�.
            this.root.Children.Add(currentRect);
        }

        #endregion ######## �׸� ################

        #endregion ######## ���콺 ���� ################

        #region ######################### ���� ���� #########################
        #region ######### �Ұ߻��� #########
        /// <summary>
        /// �Ұ߻���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		void OnDeleteButtonClick(object sender, RoutedEventArgs e)
        {
            SetDeleteAllTextBox();
        }

        /// <summary>
        /// �Ұ߻���
        /// </summary>
        /// <returns></returns>
        private string SetDeleteAllTextBox() {
            string _rtn = "";
            if (this.CurTalkBox.Count > 0)
            {
                this.CurTalkBox.ForEach(delegate (TalkBoxLayer _txt_layer) { _txt_layer.Delete(); });
                this.CurTalkBox.Clear();
            }
            TxtDocTalkShow.Text = string.Empty;
            return _rtn;
        }
        #endregion ######### �Ұ߻��� #########

        #region ######### �Ұ����� #########
        /// <summary>
        /// �Ұ� ����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.CurTalkBox.Count > 0)
            {
                SetSaveAllTextBox();
            }
        }

        private string SetSaveAllTextBox()
        {
            string _rtn = "";

            string str_text = string.Empty;
            StringBuilder sb = new StringBuilder(); // �����(����, ��ġ,ũ�� ����)
            StringBuilder sb2 = new StringBuilder(); // �ð���(��ġ,ũ�� ���� X)
            for (int i = 0; i < _LstTalkBoxLayer.Count; i++)
            {
                if (_LstTalkBoxLayer[i].Text != null)
                {
                    sb.Append(
                        PhotosListBox.SelectedItem.ToString()
                        + "��" + _LstTalkBoxLayer[i].Text.ToString()
                        + "��" + _LstTalkBoxLayer[i].TalkBoxLyerPointX
                        + "��" + _LstTalkBoxLayer[i].TalkBoxLyerPointY
                        + "��" + _LstTalkBoxLayer[i].TalkBoxLyerSizeW
                        + "��" + _LstTalkBoxLayer[i].TalkBoxLyerSizeH
                        + "��\r\n");
                    sb2.AppendLine(_LstTalkBoxLayer[i].Text.ToString());
                }
            }
            TxtDocTalk.Text = sb.ToString();
            TxtDocTalkShow.Text = sb2.ToString();
            return _rtn;
        }

        public string getTxtDocTalk() {
            string _Talk = string.Empty;
            _Talk = TxtDocTalk.Text.ToString();
            return _Talk;
        }
        #endregion ######### �Ұ����� #########

        #region ######### �Ұ߷ε� #########
        /// <summary>
        /// �Ұ��� �ε��Ѵ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnLoadButtonClick(object sender, RoutedEventArgs e)
        {
            #region #### �ε��� ���� �Ұ��� ����� ���� �Ұ��� �ҷ� �´� ####
            if (this.CurTalkBox.Count > 0)
            {
                this.CurTalkBox.ForEach(delegate (TalkBoxLayer _txt_layer) { _txt_layer.Delete(); });
                this.CurTalkBox.Clear();
            }
            #endregion #### �ε��� ���� �Ұ��� ����� ���� �Ұ��� �ҷ� �´� ####

            LoadTxtBox();
        }

        /// <summary>
        /// �Ұ� data Load
        /// </summary>
        void LoadTxtBox() {
            string _str = TxtDocTalk.Text;
            if (_str.Trim().Length == 0) {
                MessageBox.Show("�Ұ��� �����ϴ�.");
                return;
            }
            string[] _strArr = _str.Split('��');
            string _filePathWithName = string.Empty;    // ���� �� �߰�
            string _innerText = string.Empty;    // �۳���
            StringBuilder sb2 = new StringBuilder();
            #region ########## text ���ε� S ##########
            for (int i = 0; i < _strArr.Length - 1; i++) {
                string[] _strArr2 = _strArr[i].Split('��');

                _filePathWithName = _strArr2[0];
                _innerText = "";
                _innerText = _strArr2[1];   // �۳��� (�ǻ�Ұ�)
                sb2.AppendLine(_strArr2[1]);
                Point talkBoxLocationXY = new Point(Convert.ToDouble(_strArr2[2]), Convert.ToDouble(_strArr2[3]));
                Image image = new Image();
                image = ViewedPhoto;
                Size _size = new Size(Convert.ToDouble(_strArr2[4]), Convert.ToDouble(_strArr2[5]));
                image.RenderSize = _size;
                Style _cssTalkBox = base.FindResource("cssTalkBox") as Style;
                Style _cssTalkBoxEdit = base.FindResource("cssTalkBoxEdit") as Style;
                TalkBoxLayer talkBoxLayer = TalkBoxLayer.Create(
                    image,
                    talkBoxLocationXY,
                    _cssTalkBox,
                    _cssTalkBoxEdit
                    );
                this.CurTalkBox.Add(talkBoxLayer);
                talkBoxLayer.Text = _innerText.ToString();

            }
            #endregion ########## text ���ε� E ##########

            TxtDocTalkShow.Text = sb2.ToString();
        }

        List<TalkBoxLayer> CurTalkBox
        {
            get
            {
                //if (this.tabControl.SelectedIndex == 0)
                return _LstTalkBoxLayer;
                //return _habitatAnnotations;
            }
        }
        #endregion ######### �Ұ߷ε� #########

        #endregion ######################### ���� ���� #########################

        #region ######################### ���� Ʈ������ ���� #########################
        /// <summary>
        /// ���� Ʈ������ ������ ���� Ŭ��������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPhotoClick(object sender, RoutedEventArgs e)
        {
            btnSaveText.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); //btnSaveText.PerformClick() in wpf
            btnDelText.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            TxtDocTalk.Text = string.Empty; // �Ұ� data ����
            TxtDocTalkShow.Text = string.Empty;
            //OnDeleteButtonClick
            ImageSource imageSource = new BitmapImage(new Uri(PhotosListBox.SelectedItem.ToString()));
            ViewedPhoto.Source = imageSource;
            this.SizeToContent = SizeToContent.WidthAndHeight;
            //Grid.SetZIndex(this.ViewedPhoto, 99999);
        }

        private void deletePhoto(object sender, RoutedEventArgs e)
        {
            string _delFile = PhotosListBox.SelectedItem.ToString();
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("���� �Ͻðڽ��ϱ�?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes) {
                MessageBox.Show(_delFile + " �� ���� �Ǿ����ϴ�.");
            }

            //ImageSource imageSource = new BitmapImage(new Uri(PhotosListBox.SelectedItem.ToString()));
            //ViewedPhoto.Source = imageSource;
        }

        private void OnImagesDirChangeClick(object sender, RoutedEventArgs e)
        {
            Photos.Path = ImagesDir.Text;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Photos = (PhotoCollection)(this.Resources["Photos"] as ObjectDataProvider).Data;
            //LoadAsync(this, e);
            Photos.Path = _PicFolder; //Environment.CurrentDirectory + "\\images";
        }

        //private void LoadAsync(object sender, DoWorkEventArgs e)
        //{
        //    BitmapDecoder decoder;
        //    using (Stream imageStreamSource = new FileStream(_PicFolder, FileMode.Open, FileAccess.Read, FileShare.Read))
        //    {
        //        decoder = BitmapDecoder.Create(imageStreamSource, BitmapCreateOptions.PreservePixelFormat,
        //            BitmapCacheOption.OnLoad);
        //    }
        //    foreach (var frame in decoder.Frames)
        //    {
        //        frame.Freeze();
        //        (sender as BackgroundWorker).ReportProgress(0, frame);
        //    }
        //}

        //private void UpdateAsync(object send, ProgressChangedEventArgs e)
        //{
        //    //SyncImages.Add((BitmapSource)e.UserState);
        //    //OnPropertyChanged("SyncImages");
        //}
        #endregion ######################### ���� Ʈ������ ���� #########################

    }

    public class ModelTextbox
    {
        public double _TxWidth, _TxHeight;
        public double TxWidth
        {
            get
            {
                return _TxWidth;
            }
            set
            {
                _TxWidth = value;
                PropertyChanged(this, new PropertyChangedEventArgs("ModelTextbox"));
            }
        }
        public double TxHeight
        {
            get
            {
                return _TxHeight;
            }
            set
            {
                _TxHeight = value;
                PropertyChanged(this, new PropertyChangedEventArgs("ModelTextbox"));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }

 }
