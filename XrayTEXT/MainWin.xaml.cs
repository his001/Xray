using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
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

        public PhotoCollection Photos;

        readonly List<TalkBoxLayer> _LstTalkBoxLayer = new List<TalkBoxLayer>();  // �Ұ� ������

        double scaleX = 1;
        double scaleY = 1;
        TranslateTransform translate = new TranslateTransform();
        //DataSet ds;

        public TalkBoxLayer Last_talkBoxLayer = null; // ������ ����/�۾� �Ǿ��� ���̾�
        public Image Last_image = null; // ������ ����/�۾� �Ǿ��� �̹���
        #endregion ######################### ���� #########################

        #region ######################### MainWin #########################
        public MainWin()
        {
            InitializeComponent();

            Photos = (PhotoCollection)(this.Resources["Photos"] as ObjectDataProvider).Data;
            Photos.Path = Helpers.PicFolder;

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
            if (PhotosListBox.SelectedItem != null)
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
        }
        #endregion ############ ���콺 �� ############

        /// <summary>
        /// ���콺 ���� ��ư Ŭ�� �̺�Ʈ �ڵ鷯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (PhotosListBox.SelectedItem != null)
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
        }

        /// <summary>
        /// ���콺 �̵� �̺�Ʈ �ڵ鷯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void root_MouseMove(object sender, MouseEventArgs e)
        {
            if (PhotosListBox.SelectedItem != null)
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
        }

        /// <summary>
        /// ���콺 Ŭ�� �� ����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void root_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (PhotosListBox.SelectedItem != null)
            {
                this.root.ReleaseMouseCapture(); //���콺 ĸ�縦 �����Ѵ�.
                SetRectangleProperty();
                #region ############
                try
                {
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

                        Int32 fileNum = _LstTalkBoxLayer.Count() + 1;
                        string keyFilename = Helpers.keyFilename; //PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/");
                        string cutfileName = getSaveFile("_" + fileNum.ToString() + ".png");
                        string fullPath = getSavePath();
                        string info_fileTxt = getSaveFile(".dat");
                        string fileTitle = TxtFileTitle.Text.ToString(); // ���ϴ�
                        string memo = "";
                        TalkBoxLayer _talkBoxLayer = TalkBoxLayer.Create(
                            keyFilename,
                            fileTitle,
                            cutfileName,
                            fullPath,
                            fileNum,
                            memo,
                            image,
                            talkBoxLocationXY,
                            _cssTalkBox,
                            _cssTalkBoxEdit);
                        this.CurTalkBox.Add(_talkBoxLayer);

                        Last_talkBoxLayer = _talkBoxLayer; //������ �۾� ���̾ ���� �ϱ� ���� ...
                        Last_image = image; //������ �۾� �̹����� ���� �ϱ� ���� ...
                        Helpers.ExportToPng(cutfileName, image, top, left);

                        #endregion ########## �簢�� �ȿ� _talkLayer ���� end ##########
                        root.Children.Remove(currentRect); // �׷��� �׸�� ���� - obj ���� �ߴ��� ������ �ȵ� ���� �� null ó��
                        currentRect.Visibility = Visibility.Hidden;
                        currentRect = null;
                        GC.Collect();
                        //SetSaveAllTextBox(info_fileTxt);
                    }
                }
                catch (Exception ex) {

                }
                #endregion ############
            }
        }
        

        /// <summary>
        /// ����� ���� ���
        /// </summary>
        /// <returns></returns>
        public string getSavePath() {
            //string _savePath = System.IO.Path.GetFileNameWithoutExtension(PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/"));
            string _savePath = Helpers.keyFilename;//PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/");
            if (_savePath.Length > 10) {
                _savePath = _savePath.Substring(0, _savePath.Length - 4);
            }
            System.IO.Directory.CreateDirectory(_savePath); // ������ ������ ����
            File.SetAttributes(_savePath, FileAttributes.Hidden); // ���� �Ӽ��� �������� ó��
            return _savePath;
        }

        /// <summary>
        /// ����� ���ϸ�
        /// </summary>
        /// <returns></returns>
        public string getSaveFile(string _extension=".dat")
        {
            string _saveFileName = getSavePath() + "\\" + System.IO.Path.GetFileNameWithoutExtension(
                Helpers.keyFilename//PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/")
                ) + _extension;
            return _saveFileName;
        }

        #region ######## �׸� ################
        private void SetRectangleProperty()
        {
            try
            {
                //�簢���� ������ 100% �� ����
                currentRect.Opacity = 0.7;
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
        private string SetDeleteAllTextBox()
        {
            string _rtn = "";
            if (this.CurTalkBox.Count > 0)
            {
                this.CurTalkBox.ForEach(delegate (TalkBoxLayer _txt_layer) { _txt_layer.Delete(); });
                this.CurTalkBox.Clear();
            }
            TxtcutMemo.Text = string.Empty;
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
            else {
                //MessageBox.Show();
                //this.CurTalkBox.ForEach(delegate (TalkBoxLayer _txt_layer) { MessageBox.Show(_txt_layer.Text); });
            }
        }

        /// <summary>
        /// ���̾� ���� ������ DB ����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnSaveDBButtonClick(object sender, RoutedEventArgs e)
        {
            int _saveCnt = 0;
            for (int i = 0; i < _LstTalkBoxLayer.Count; i++) {
                if (_LstTalkBoxLayer[i].Text != null)
                {
                    if (Helpers.SaveDB(_LstTalkBoxLayer[i]) == "success")
                    {
                        _saveCnt++;
                        // ���� ����
                    }
                }
            }
        }

        #region #### PropertyChangedEventHandler ####
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
        #endregion #### PropertyChangedEventHandler ####

        public void SetSaveAllTextBox(string _path = "")
        {
            //string str_text = string.Empty;
            //StringBuilder sb = new StringBuilder(); // �����(����, ��ġ,ũ�� ����)
            //StringBuilder sb2 = new StringBuilder(); // �ð���(��ġ,ũ�� ���� X)

            //for (int i = 0; i < _LstTalkBoxLayer.Count; i++)
            //{
            //    if (_LstTalkBoxLayer[i].TalkBoxLyerMemo != null)
            //    {
            //        sb.Append(
            //            PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/")
            //            + "��" + _LstTalkBoxLayer[i].TalkBoxLyercutfileName.ToString()
            //            + "��" + _LstTalkBoxLayer[i].TalkBoxLyerCutFullPath.ToString()
            //            + "��" + _LstTalkBoxLayer[i].TalkBoxLyerFileNum.ToString()
            //            + "��" + _LstTalkBoxLayer[i].TalkBoxLyerMemo.ToString()
            //            + "��" + _LstTalkBoxLayer[i].TalkBoxLyerPointX
            //            + "��" + _LstTalkBoxLayer[i].TalkBoxLyerPointY
            //            + "��" + _LstTalkBoxLayer[i].TalkBoxLyerSizeW
            //            + "��" + _LstTalkBoxLayer[i].TalkBoxLyerSizeH
            //            + "��\r\n");
            //        sb2.AppendLine(_LstTalkBoxLayer[i].TalkBoxLyerMemo.ToString());
            //    }
            //}
            ////TxtFileTitle.Text = sb.ToString();
            ////TxtcutMemo.Text = sb2.ToString();
            //if (_path != "") { File.WriteAllText(_path, sb.ToString()); }

        }

        public string getTxtFileTitle()
        {
            string _Talk = string.Empty;
            _Talk = TxtFileTitle.Text.ToString();
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

            //LoadTxtBox();
            //string keyFilename = PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/");
            Helpers.LoadTxtBoxDB();
        }

        /// <summary>
        /// 
        /// </summary>
        

        //public BitmapImage ImageFromBytearray(byte[] imageData)
        //{

        //    if (imageData == null)
        //        return null;
        //    MemoryStream strm = new MemoryStream();
        //    strm.Write(imageData, 0, imageData.Length);
        //    strm.Position = 0;
        //    Image img = Image.FromStream(strm);

        //    BitmapImage bitmapImage = new BitmapImage();
        //    bitmapImage.BeginInit();
        //    MemoryStream memoryStream = new MemoryStream();
        //    img.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
        //    memoryStream.Seek(0, SeekOrigin.Begin);
        //    bitmapImage.StreamSource = memoryStream;
        //    bitmapImage.EndInit();

        //    return bitmapImage;
        //}

        /// <summary>
        /// �Ұ� data Load
        /// </summary>
        void LoadTxtBox(string _path = "")
        {
            string _str = TxtFileTitle.Text;
            if (_path != "")
            {
                string _loadFileName = getSaveFile(".dat");
                if (File.Exists(_loadFileName))
                {
                    _str = File.ReadAllText(_path);
                }
            }
            #region ########## text ���ε� S ##########
            if (_str.Trim().Length == 0)
            {
                //MessageBox.Show("�Ұ��� �����ϴ�.");
                return;
            }
            string[] _strArr = _str.Split('��');
            string _filePathWithName = string.Empty;    // ���� �� �߰�
            string _innerMemo = string.Empty;    // �۳���
            string _TalkBoxLyerKeyFilename = "";
            string _TalkBoxLyerFileTitle = "";
            string _TalkBoxLyercutfileName = "";
            string _TalkBoxLyerCutFullPath = "";
            string _TalkBoxLyerFileNum = "";
            StringBuilder sb2 = new StringBuilder();
            
            for (int i = 0; i < _strArr.Length - 1; i++)
            {
                string[] _strArr2 = _strArr[i].Split('��');

                _filePathWithName    = _strArr2[0];
                _TalkBoxLyerFileTitle = _strArr2[1];
                _TalkBoxLyercutfileName = _strArr2[2];
                _TalkBoxLyerCutFullPath = _strArr2[3];
                _TalkBoxLyerFileNum  = _strArr2[4];
                _innerMemo = "";
                _innerMemo = _strArr2[5];   // �۳��� (�ǻ�Ұ�)
                sb2.AppendLine(_strArr2[6]);
                Point talkBoxLocationXY = new Point(Convert.ToDouble(_strArr2[7]), Convert.ToDouble(_strArr2[8]));
                Image image = new Image();
                image = ViewedPhoto;
                Size _size = new Size(Convert.ToDouble(_strArr2[9]), Convert.ToDouble(_strArr2[10]));
                image.RenderSize = _size;
                Style _cssTalkBox = base.FindResource("cssTalkBox") as Style;
                Style _cssTalkBoxEdit = base.FindResource("cssTalkBoxEdit") as Style;
                TalkBoxLayer talkBoxLayer = TalkBoxLayer.Create(
                    _TalkBoxLyerKeyFilename,
                    _TalkBoxLyerFileTitle,
                    _TalkBoxLyercutfileName,
                    _TalkBoxLyerCutFullPath,
                    Convert.ToInt32(_TalkBoxLyerFileNum),
                    _innerMemo,
                    image,
                    talkBoxLocationXY,
                    _cssTalkBox,
                    _cssTalkBoxEdit
                    );
                this.CurTalkBox.Add(talkBoxLayer);
                talkBoxLayer.Text = _innerMemo.ToString();

            }
            TxtcutMemo.Text = sb2.ToString();
            #endregion ########## text ���ε� E ##########
        }


        public List<TalkBoxLayer> CurTalkBox
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
            TxtFileTitle.Text = string.Empty; // �Ұ� data ����
            TxtcutMemo.Text = string.Empty;  // ����

            ImageSource imageSource = new BitmapImage(new Uri(PhotosListBox.SelectedItem.ToString()));
            ViewedPhoto.Source = imageSource;
            this.SizeToContent = SizeToContent.WidthAndHeight;
            //Grid.SetZIndex(this.ViewedPhoto, 99999);

            btnLoadText.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // ���� ����� ������ �ִٸ� �ε�
        }

        private void deletePhoto(object sender, RoutedEventArgs e)
        {
            //string _delFile = PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/");
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("���� �Ͻðڽ��ϱ�?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                MessageBox.Show(Helpers.keyFilename + " �� ���� �Ǿ����ϴ�.");
            }
        }

        private void OnImagesDirChangeClick(object sender, RoutedEventArgs e)
        {
            Photos.Path = ImagesDir.Text;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Photos = (PhotoCollection)(this.Resources["Photos"] as ObjectDataProvider).Data;
            Photos.Path = Helpers.PicFolder;
        }

        #endregion ######################### ���� Ʈ������ ���� #########################

    }
}
