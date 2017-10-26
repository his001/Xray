using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using XrayTEXT.ViewModels;

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

            //���� ��� �ּ���
            //Helpers helper = new Helpers(); //class1�� �����Ҵ�
            //helper.ReturnToText += new Helpers.deleg_TxtcutMemo(TxtcutMemo_set);//�̺�Ʈ �ڵ鷯 ����
            //helper.SetTextChange();//�̺�Ʈ ȣ�� ���� ���� �Լ� ȣ��

            DataContext = new MainViewModel(); // ���� ��� �ϴ��� TEXT ���� ��
        }
        //private void TxtcutMemo_set(string upLabelText)
        //{
        //    TxtcutMemo.Text = upLabelText;//���� �ؽ�Ʈ �� ����
        //}
        
        /// <summary>
        /// �����Ұ߷ε� ��ư Ŭ����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UpTextSetBind(object sender, RoutedEventArgs e)//(string changeText="")
        {
            //string info_fileTxt = getSaveFile(".dat");
            //SetSaveAllTextBox(info_fileTxt);

            getTextFromDB();

            //XrayTEXT.ViewModels.MainViewModel mainViewModel = new MainViewModel();
            //mainViewModel.UserCutMemo = "UserCutMemo test";
            //mainViewModel.UserFileMemo = "UserFileMemo test";
            //MessageBox.Show(
            //    mainViewModel.UserCutMemo + " : mainViewModel.UserCutMemo\r\n" 
            //    + mainViewModel.UserFileMemo + " : mainViewModel.UserFileMemo\r\n"
            //    + TxtcutMemo.Text + " : TxtcutMemo.Text\r\n"
            //    + TxtFileTitle.Text + " : TxtFileTitle.Text\r\n"
            //    );
        }

        /// <summary>
        /// DB ���� �Ұ߰� ���ϳ����� �޾ƿ� ���ε� �Ѵ�.
        /// </summary>
        private void getTextFromDB() {
            #region ########## text ���ε� S ##########

            DataSet ds = new DataSet();
            try
            {
                string _text = string.Empty;
                string constr = Helpers.dbCon;
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();
                    string sql = "Select KeyFilename, CutFilename, CutFullPath, FileTitle, numb, memo, PointX, PointY, SizeW, SizeH, Fileimg ";
                    sql = sql + " From TBL_TalkBoxLayer with(nolock) where KeyFilename ='" + getKeyFileNameOnly() + "' Order by  numb ";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        var adapt = new SqlDataAdapter();
                        adapt.SelectCommand = cmd;
                        adapt.Fill(ds);
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
            string _FileTitle = string.Empty;
            string _innerMemo = string.Empty;    // �۳���

            StringBuilder sb2 = new StringBuilder();
            if (ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
            {
                _FileTitle = ds.Tables[0].Rows[0]["FileTitle"].ToString();
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    _innerMemo = "";
                    _innerMemo = ds.Tables[0].Rows[i]["memo"].ToString();   // �۳��� (�ǻ�Ұ�)
                    sb2.AppendLine(_innerMemo);
                }
            }
            TxtcutMemo.Text = sb2.ToString();  // ����
            TxtFileTitle.Text = _FileTitle;
            #endregion ########## text ���ε� E ##########
        }


        #endregion ######################### MainWin #########################

        

        #region ######## DB Ű ���� ########
        public string getKeyWithPath()
        {
            string _key = PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/");
            return _key;
        }
        public string getKeyFileNameOnly()
        {
            string _key = getKeyWithPath();
            string FileNameOnly = _key.Substring(_key.LastIndexOf("/") + 1);
            return FileNameOnly;
        }
        #endregion ######## DB Ű ���� ########

        private void reinit() {
            //_LstTalkBoxLayer.Clear();
            //_LstTalkBoxLayer = new List<TalkBoxLayer>();
            scaleX = 1;
            scaleY = 1;
        }

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
                    CreateRectangle();
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
                        string keyFilename = getKeyFileNameOnly(); //Helpers.keyFilename; //PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/");
                        //string cutfileName = getSaveFile("_" + fileNum.ToString() + ".png");
                        string cutfileName = getSaveFileNoPath("_" + fileNum.ToString() + ".png"); // cutfileName �� ���� �̸������� ����
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
                        Helpers.ExportToPng(fullPath +"/"+ cutfileName, image, top, left);

                        #endregion ########## �簢�� �ȿ� _talkLayer ���� end ##########
                        root.Children.Remove(currentRect); // �׷��� �׸�� ���� - obj ���� �ߴ��� ������ �ȵ� ���� �� null ó��
                        currentRect.Visibility = Visibility.Hidden;
                        currentRect = null;
                        GC.Collect();
                        SetSaveAllTextBox(info_fileTxt);
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
            string _savePath = getKeyWithPath();//PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/");
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
                getKeyWithPath() //PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/")
                ) + _extension;
            return _saveFileName;
        }

        public string getSaveFileNoPath(string _extension = ".dat")
        {
            string _saveFileName = System.IO.Path.GetFileNameWithoutExtension( getKeyWithPath() ) + _extension;
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

        private void CreateRectangle()
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
            TxtcutMemo.Text = string.Empty;
            TxtFileTitle.Text = string.Empty;
            SetClearTalkBoxLayer();
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


        public void SetSaveAllTextBox(string _path = "")
        {
            string str_text = string.Empty;
            StringBuilder sb = new StringBuilder(); // �����(����, ��ġ,ũ�� ����)
            StringBuilder sb2 = new StringBuilder(); // �ð���(��ġ,ũ�� ���� X)
            string FileTitle = "";
            for (int i = 0; i < _LstTalkBoxLayer.Count; i++)
            {
                if (_LstTalkBoxLayer[i].Text != null)
                {
                    if (FileTitle == "" ) { FileTitle = _LstTalkBoxLayer[i].TalkBoxLyerFileTitle; }
                    sb.Append(
                        _LstTalkBoxLayer[i].TalkBoxLyerkeyFilename.ToString() //PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/")
                        + "��" + _LstTalkBoxLayer[i].TalkBoxLyercutfileName.ToString()
                        + "��" + _LstTalkBoxLayer[i].TalkBoxLyerCutFullPath.ToString()
                        + "��" + _LstTalkBoxLayer[i].TalkBoxLyerFileNum.ToString()
                        + "��" + _LstTalkBoxLayer[i].Text.ToString()
                        + "��" + _LstTalkBoxLayer[i].TalkBoxLyerPointX
                        + "��" + _LstTalkBoxLayer[i].TalkBoxLyerPointY
                        + "��" + _LstTalkBoxLayer[i].TalkBoxLyerSizeW
                        + "��" + _LstTalkBoxLayer[i].TalkBoxLyerSizeH
                        + "��\r\n");
                    sb2.AppendLine(_LstTalkBoxLayer[i].Text.ToString());
                }
            }
            TxtFileTitle.Text = FileTitle;
            TxtcutMemo.Text = sb2.ToString();
            if (_path != "") { File.WriteAllText(_path, sb.ToString()); }

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
        /// �Ұ��� DB���� �ε��Ѵ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnLoadButtonClick(object sender, RoutedEventArgs e)
        {
            //LoadTxtBox();
            //string keyFilename = PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/");
            LoadMemeFromDB();
            //LoadTxtBox();
        }


        /// <summary>
        /// �Ұ� data Load
        /// </summary>
        void LoadTxtBox(string _path = "")
        {
            //string _str = TxtFileTitle.Text;
            //if (_path != "")
            //{
            //    string _loadFileName = getSaveFile(".dat");
            //    if (File.Exists(_loadFileName))
            //    {
            //        _str = File.ReadAllText(_path);
            //    }
            //}
            //#region ########## text ���ε� S ##########
            //if (_str.Trim().Length == 0)
            //{
            //    //MessageBox.Show("�Ұ��� �����ϴ�.");
            //    return;
            //}
            //string[] _strArr = _str.Split('��');
            //string _filePathWithName = string.Empty;    // ���� �� �߰�
            //string _innerMemo = string.Empty;    // �۳���
            //string _TalkBoxLyerKeyFilename = "";
            //string _TalkBoxLyerFileTitle = "";
            //string _TalkBoxLyercutfileName = "";
            //string _TalkBoxLyerCutFullPath = "";
            //string _TalkBoxLyerFileNum = "";
            //StringBuilder sb2 = new StringBuilder();
            
            //for (int i = 0; i < _strArr.Length - 1; i++)
            //{
            //    string[] _strArr2 = _strArr[i].Split('��');

            //    _filePathWithName    = _strArr2[0];
            //    _TalkBoxLyerFileTitle = _strArr2[1];
            //    _TalkBoxLyercutfileName = _strArr2[2];
            //    _TalkBoxLyerCutFullPath = _strArr2[3];
            //    _TalkBoxLyerFileNum  = _strArr2[4];
            //    _innerMemo = "";
            //    _innerMemo = _strArr2[5];   // �۳��� (�ǻ�Ұ�)
            //    sb2.AppendLine(_strArr2[6]);
            //    Point talkBoxLocationXY = new Point(Convert.ToDouble(_strArr2[7]), Convert.ToDouble(_strArr2[8]));
            //    Image image = new Image();
            //    image = ViewedPhoto;
            //    Size _size = new Size(Convert.ToDouble(_strArr2[9]), Convert.ToDouble(_strArr2[10]));
            //    image.RenderSize = _size;
            //    Style _cssTalkBox = base.FindResource("cssTalkBox") as Style;
            //    Style _cssTalkBoxEdit = base.FindResource("cssTalkBoxEdit") as Style;
            //    TalkBoxLayer talkBoxLayer = TalkBoxLayer.Create(
            //        _TalkBoxLyerKeyFilename,
            //        _TalkBoxLyerFileTitle,
            //        _TalkBoxLyercutfileName,
            //        _TalkBoxLyerCutFullPath,
            //        Convert.ToInt32(_TalkBoxLyerFileNum),
            //        _innerMemo,
            //        image,
            //        talkBoxLocationXY,
            //        _cssTalkBox,
            //        _cssTalkBoxEdit
            //        );
            //    this.CurTalkBox.Add(talkBoxLayer);
            //    talkBoxLayer.Text = _innerMemo.ToString();

            //}
            //TxtcutMemo.Text = sb2.ToString();
            //#endregion ########## text ���ε� E ##########
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

        public void SetClearTalkBoxLayer() {
            #region #### �ε� �� ���� �Ұ��� ����� ���� �Ұ��� �ҷ� �´� ####
            if (this.CurTalkBox.Count > 0)
            {
                this.CurTalkBox.ForEach(delegate (TalkBoxLayer _txt_layer) {
                    _txt_layer.Delete();
                    //_txt_layer.SetHidden();
                });
                this.CurTalkBox.Clear();
            }
            #endregion #### �ε��� ���� �Ұ��� ����� ���� �Ұ��� �ҷ� �´� ####

        }

        /// <summary>
        /// DB���� Data �� ��ȸ �� ȭ�鿡 ���ε� 
        /// </summary>
        public void LoadMemeFromDB()
        {
            TxtcutMemo.Text = "";  // ����
            TxtFileTitle.Text = "";
            //SetClearTalkBoxLayer();
            //string _key = Helpers.keyFilename;//_talkBoxLayer.TalkBoxLyerkeyFilename;
            #region ########## text ���ε� S ##########
            DataSet ds = new DataSet();
            try
            {
                string _text = string.Empty;
                using (SqlConnection conn = new SqlConnection(Helpers.dbCon))
                {
                    conn.Open();
                    string sql = "Select KeyFilename, CutFilename, CutFullPath, FileTitle, numb, memo, PointX, PointY, SizeW, SizeH, Fileimg ";
                    sql = sql + " From TBL_TalkBoxLayer with(nolock) where KeyFilename ='" + getKeyFileNameOnly() + "' Order by  numb ";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        var adapt = new SqlDataAdapter();
                        adapt.SelectCommand = cmd;
                        adapt.Fill(ds);
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }

            string _KeyFilename = string.Empty;    // ���� �� �߰�
            string _FileTitle = string.Empty;
            string _innerMemo = string.Empty;    // �۳���
            string _TalkBoxLyercutfileName = "";
            string _TalkBoxLyerCutFullPath = "";
            string _TalkBoxLyerFileNum = "";

            TalkBoxLayer talkBoxLayer_Last_Add =  null;

            //byte[] photo_aray; //DB���� �̹����� �ҷ��´�
            StringBuilder sb2 = new StringBuilder();
            if (ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
            {
                _FileTitle = ds.Tables[0].Rows[0]["FileTitle"].ToString();
                
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    #region ################ ������ Ű _TalkBoxLyercutfileName �� �ִ��� Ȯ�� ####################
                    bool chk_existLayer = false;
                    for (int j = 0; j < _LstTalkBoxLayer.Count; j++)
                    {
                        if ( (getKeyFileNameOnly() == _LstTalkBoxLayer[j].TalkBoxLyerkeyFilename) 
                            && (ds.Tables[0].Rows[i]["CutFullPath"].ToString() == _LstTalkBoxLayer[j].TalkBoxLyercutfileName))
                        {
                            chk_existLayer = true;
                            break;
                        }
                    }
                    //var dataLayer = from CutFilename in _LstTalkBoxLayer
                    //  where (_KeyFilename == getKeyFileNameOnly()) && (CutFilename == ds.Tables[0].Rows[i]["CutFullPath"].ToString())
                    //  orderby CutFilename
                    //select CutFilename;
                    #endregion ################ ������ Ű _TalkBoxLyercutfileName �� �ִ��� Ȯ�� ####################

                    #region ################ ������ Ű _TalkBoxLyercutfileName ������ ��� �߰� ���� ���� ####################
                    if (!chk_existLayer)  
                    {
                        _KeyFilename            = ds.Tables[0].Rows[i]["KeyFilename"].ToString();
                        _TalkBoxLyercutfileName = ds.Tables[0].Rows[i]["CutFilename"].ToString();
                        _TalkBoxLyerCutFullPath = ds.Tables[0].Rows[i]["CutFullPath"].ToString();
                        _TalkBoxLyerFileNum     = ds.Tables[0].Rows[i]["numb"].ToString();
                        _innerMemo = "";
                        _innerMemo              = ds.Tables[0].Rows[i]["memo"].ToString();   // �۳��� (�ǻ�Ұ�)
                        sb2.AppendLine(_innerMemo);

                        Point talkBoxLocationXY = new Point(Convert.ToDouble(ds.Tables[0].Rows[i]["PointX"].ToString()), Convert.ToDouble(ds.Tables[0].Rows[i]["PointY"].ToString()));
                        Image image = new Image();
                        image = ViewedPhoto;

                        //DB���� �̹����� �ҷ��´�
                        //if (ds.Tables[0].Rows[i]["Fileimg"] != System.DBNull.Value)
                        //{
                        //    photo_aray = (byte[])ds.Tables[0].Rows[i]["Fileimg"];
                        //    BitmapImage bi3 = new BitmapImage();
                        //    bi3.BeginInit();
                        //    bi3.UriSource = new Uri(_TalkBoxLyercutfileName, UriKind.Relative);
                        //    bi3.EndInit();
                        //    image.Source = bi3;
                        //}

                        Size _size = new Size(Convert.ToDouble(ds.Tables[0].Rows[i]["SizeW"].ToString()), Convert.ToDouble(ds.Tables[0].Rows[i]["SizeH"].ToString()));
                        image.RenderSize = _size;
                        Style _cssTalkBox = this.FindResource("cssTalkBox") as Style;
                        Style _cssTalkBoxEdit = this.FindResource("cssTalkBoxEdit") as Style;

                        TalkBoxLayer talkBoxLayer = TalkBoxLayer.Create(
                        _KeyFilename,
                        _FileTitle,
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
                        talkBoxLayer_Last_Add = talkBoxLayer;
                    }
                    #endregion ################ ������ Ű _TalkBoxLyercutfileName ������ ��� �߰� ���� ���� ####################
                }
                #region ##### �������� �߰��� ���̾ ���� ��忡�� ���� #####
                if (talkBoxLayer_Last_Add != null)  
                {
                    TalkBoxLayerControl _control;
                    Style _cssTalkBox = this.FindResource("cssTalkBox") as Style;
                    Style _cssTalkBoxEdit = this.FindResource("cssTalkBoxEdit") as Style;
                    _control = new TalkBoxLayerControl(talkBoxLayer_Last_Add, _cssTalkBox, _cssTalkBoxEdit);
                    _control.IsEnabled = false;
                }
                #endregion ##### �������� �߰��� ���̾ ���� ��忡�� ���� #####
            }
            TxtcutMemo.Text = sb2.ToString();  // ����
            TxtFileTitle.Text = _FileTitle;
            #endregion ########## text ���ε� E ##########
            GC.Collect();
        }

        #region ######################### ���� Ʈ������ ���� #########################
        /// <summary>
        /// ���� Ʈ������ ������ ���� Ŭ��������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPhotoDblClick(object sender, RoutedEventArgs e)
        {
            btnSaveText.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); //btnSaveText.PerformClick() in wpf
            btnDelText.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            TxtFileTitle.Text = string.Empty; // �Ұ� data ����
            TxtcutMemo.Text = string.Empty;  // ����

            ImageSource imageSource = new BitmapImage(new Uri(PhotosListBox.SelectedItem.ToString()));
            ViewedPhoto.Source = imageSource;
            //this.SizeToContent = SizeToContent.WidthAndHeight;
            //Grid.SetZIndex(this.ViewedPhoto, 99999);

            //reinit();   // ȭ���� ��ȯ �Ǹ� �ʱ� ���� ���� ����!!!!
            //btnLoadText.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // ���� ����� ������ �ִٸ� �ε�

            //RoutedEventArgs routedEventArgs = new RoutedEventArgs(ButtonBase.ClickEvent, btnLoadText);
            //btnLoadText.RaiseEvent(routedEventArgs);
            //new Action(() => btnLoadText.RaiseEvent(routedEventArgs)).SetTimeout(500);

            new Action(() => btnLoadText.RaiseEvent(new RoutedEventArgs(Button.ClickEvent))).SetTimeout(500);
        }

        private void deletePhoto(object sender, RoutedEventArgs e)
        {
            //string _delFile = PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/");
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("���� �Ͻðڽ��ϱ�?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                MessageBox.Show(getKeyWithPath() + " �� ���� �Ǿ����ϴ�.");
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

        #region ##################### ��Ÿ ���� ����Ҽ� �־ �ϴ� �ּ� ó�� #####################

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

        #endregion ##################### ��Ÿ ���� ����Ҽ� �־ �ϴ� �ּ� ó�� #####################
        
    }


    #region ######### menu
    #endregion  ######### menu

    public static class SettimeoutDelegateExtension
    {
        /// <summary> 
        /// Inspired by HTML DOM, executes a delegate via a DispatcherTimer once. 
        /// </summary> 
        /// <example>new Action(() => someObject.DoSomethingCool()).SetTimeout(100); 
        /// </example> 
        /// <remarks>Frequently things need to be executed in a timeout, but constructing a Timer is a pain especially for 
        /// one-off calls. Combined with Lambda expressions this makes the whole process relativey painless. 
        /// </remarks> 
        /// <param name="action">Any delegate to execute</param> 
        /// <param name="timeout">How long to wait to execute</param> 
        /// <param name="args">Any arguments to pass to the delegate</param> 
        public static void SetTimeout(this Delegate action, TimeSpan timeout, params object[] args)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = timeout;
            timer.Tick += new EventHandler(delegate (object sender, EventArgs e)
            {
                timer.Stop();
                timer = null;
                action.DynamicInvoke(args);
            });
            timer.Start();
        }

        /// <summary> 
        /// Inspired by HTML DOM, executes a delegate via a DispatcherTimer once. 
        /// </summary> 
        /// <example>new Action(() => someObject.DoSomethingCool()).SetTimeout(100); 
        /// </example> 
        /// <remarks>Frequently things need to be executed in a timeout, but constructing a Timer is a pain especially for 
        /// one-off calls. Combined with Lambda expressions this makes the whole process relativey painless. 
        /// </remarks> 
        /// <param name="action">Any delegate to execute</param> 
        /// <param name="timeout">How long to wait to execute in milliseconds</param> 
        /// <param name="args">Any arguments to pass to the delegate</param> 
        public static void SetTimeout(this Delegate action, int timeout, params object[] args)
        {
            SetTimeout(action, TimeSpan.FromMilliseconds(timeout), args);
        }
    }
}
