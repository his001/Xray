using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Threading;

namespace XrayTEXT
{
    public partial class MainWin : System.Windows.Window
    {
        #region ######################### ���� #########################
        public int pagesize = 20;
        Point prePosition; //�巹�׸� ������ ���콺 ��ǥ;
        Rectangle currentRect; //���� �׷����� �׸�
        public PhotoCollection Photos;//= new PhotoCollection(Helpers.PicFolder);
        readonly List<TalkBoxLayer> _LstTalkBoxLayer = new List<TalkBoxLayer>();  // �Ұ� ������
        //List<TalkBoxLayerControl> _LstTalkBoxLayerControl = new List<TalkBoxLayerControl>();  // �Ұ��� ��Ʈ��
        //double scaleX = 1;
        //double scaleY = 1;
        public string CurPhoto_isNormal_DB = ""; //_GetisNormal_DB; // ���� ���õ� �̹����� ���� ����/������/�Ǵ���
        public int CurShowPhoto_numb = -1; // ���� ���ο� �������� ����
        TranslateTransform translate = new TranslateTransform();

        public TalkBoxLayer Last_talkBoxLayer = null; // ������ ����/�۾� �Ǿ��� ���̾�
        public Image Last_image = null; // ������ ����/�۾� �Ǿ��� �̹���  
        public string _key = string.Empty;      //key ���� �̹���(���� ������� ���õ� ���� ��� �� �̸� )

        //public event EventHandler<EventArgs> eventMainNeedChange;
        public int curUIMemoCnt = 0;  // ȭ�� ���� �޸� ���̾� ����
        XrayTEXT.ViewModels.MainViewModel mainViewModel = new ViewModels.MainViewModel();
        public bool ShowHideMemo = true; // ������ ���̱�

        #endregion ######################### ���� #########################

        #region ######################### MainWin #########################
        public MainWin()
        {
            //this.Visibility = Visibility.Visible;
            //this.Title = "�ε����Դϴ�.";

            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {

            Helpers.PicFolder = @"D:\DEV\WPF\PRJ\XrayTEXT\XrayTEXT\Images";

            this.root.MouseLeftButtonDown += new MouseButtonEventHandler(root_MouseLeftButtonDown);
            this.root.MouseLeftButtonUp += new MouseButtonEventHandler(root_MouseLeftButtonUp);
            this.root.MouseWheel += new MouseWheelEventHandler(root_MouseWheel);
            this.root.MouseMove += new MouseEventHandler(root_MouseMove);

            Zoom.CenterX = ViewedPhoto.Width / 2;  // Zoom In, Zoom Out�� ���� Center ��ǥ ����
            Zoom.CenterY = ViewedPhoto.Height / 2;
            //ȸ���� Angle �� ������ �ǳ� �ϴ� ����

            LoadDirectories();

            DataContext = mainViewModel; // ���� ��� �ϴ��� TEXT ���� ��
            StartThread();


            Photos = (PhotoCollection)(this.Resources["Photos"] as ObjectDataProvider).Data;
            Photos.Path = Helpers.PicFolder;
            GetImageTotalCntShowFromFolder(); // ��ü ���� ������ �����ش�
        }

        private void StartThread()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += new System.EventHandler(timer_Tick);
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            TxtServerTime.Text = mainViewModel.ServerTime;
            DataSet ds = GetMemoDBCnt();
            int chkDBCnt = 0;
            int chkDBYNCnt = 0;
            if (ds != null) {
                try
                {
                    chkDBCnt = Convert.ToInt32(ds.Tables[0].Rows[0][0].ToString());
                    chkDBYNCnt = Convert.ToInt32(ds.Tables[0].Rows[0][1].ToString());
                }
                catch (Exception ex) { }
            }

            //if (eventMainNeedChange != null) { 
            //    eventMainNeedChange(this, new EventArgs());
            //}

            // ȭ���� ui ���� db �� ui ���� �ٸ��� ���� �����.
            if ((curUIMemoCnt > 0 || chkDBCnt > 0) && (curUIMemoCnt != chkDBCnt) || (chkDBCnt != chkDBYNCnt)) {
                if (chkDBCnt != chkDBYNCnt)
                {
                    SetDBupdYNMakeN();
                }  // memo �� ������ �� N ���� ���� 
                Reload_Right_Text();
            }

            //mainViewModel.UserCutMemo = curUIMemoCnt.ToString();
            //MessageBox.Show("UserCutMemo");
            TxtLayUICnt.Text = curUIMemoCnt.ToString();
            TxtLayDBCnt.Text = chkDBCnt.ToString();

            //TxtcutMemo.Text = mainViewModel.UserCutMemo;
            //TxtFileTitle.Text = mainViewModel.UserFileMemo;
        }

        private DataSet GetMemoDBCnt()
        {
            DataSet ds = new DataSet();
            string _KeyFilename = getKeyFileNameOnly();    // ���� �� �߰�
            if (_KeyFilename != "")
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(Helpers.dbCon))
                    {
                        conn.Open();
                        string sql = "Select count(*) as Cnt, sum(case updYN when 'N' then 1 else 0 end) as YNCnt From TBL_TalkBoxLayer with(nolock) where KeyFilename ='" + _KeyFilename + "' ";
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
            }
            return ds;
        }
        /// <summary>
        /// DB �� updYN ���� Y �� �ֵ��� ã�Ƽ� �� N ���� ���� 
        /// </summary>
        private void SetDBupdYNMakeN()
        {
            string _KeyFilename = getKeyFileNameOnly();    // ���� �� �߰�
            try
            {
                using (SqlConnection conn = new SqlConnection(Helpers.dbCon))
                {
                    conn.Open();
                    string sql = "update TBL_TalkBoxLayer SET updYN='N' WHERE KeyFilename = '" + _KeyFilename + "' and updYN='Y';";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        int result = cmd.ExecuteNonQuery();
                        if (result == 1)
                        {
                        }
                    }
                    conn.Close();
                }
            }
            catch (Exception ex) { }
        }

        private void Reload_Right_Text()
        {
            #region ########## text ���ε� S ##########
            string _KeyFilename = getKeyFileNameOnly();    // ���� �� �߰�
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection conn = new SqlConnection(Helpers.dbCon))
                {
                    conn.Open();
                    string sql = "Select KeyFilename, CutFilename, CutFullPath,  numb, memo, PointX, PointY, SizeW, SizeH, Fileimg ";
                    sql = sql + " From TBL_TalkBoxLayer with(nolock) where KeyFilename ='" + _KeyFilename + "' Order by  numb ";
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

            string _innerMemo = string.Empty;    // �۳���
            StringBuilder sb2 = new StringBuilder();
            if (ds != null && ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
            {
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    _innerMemo = "";
                    _innerMemo = ds.Tables[0].Rows[i]["memo"].ToString();   // �۳��� (�ǻ�Ұ�)
                    sb2.AppendLine(_innerMemo);
                }
                curUIMemoCnt = ds.Tables[0].Rows.Count;
            }
            #endregion ########## text ���ε� E ##########
            mainViewModel.UserCutMemo = sb2.ToString();
            //mainViewModel.UserFileMemo = _FileTitle;
            TxtcutMemo.Text = sb2.ToString();  // ����
        }

        private void FillTreeView(TreeViewItem parentItem, string path)
        {
            foreach (string str in Directory.EnumerateDirectories(path))
            {
                TreeViewItem item = new TreeViewItem();
                item.Header = str.Substring(str.LastIndexOf('\\') + 1);
                item.Tag = str;
                item.FontWeight = FontWeights.Normal;
                parentItem.Items.Add(item);
                FillTreeView(item, str);
            }
        }
        
        #endregion ######################### MainWin #########################

        #region ######## DB Ű ���� ########
        public string getKeyWithPath()
        {
            //string _key = PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/");
            return _key;    // �ֻ�ܿ� ����
        }
        public string getKeyFileNameOnly()
        {
            string _mainkey = getKeyWithPath();
            string FileNameOnly = _mainkey.Substring(_mainkey.LastIndexOf("/") + 1);
            return FileNameOnly;
        }
        #endregion ######## DB Ű ���� ########

        #region ############ ������ ���̱� ��� ��ư ############

        void cb_TbLabelShowHide_Unchecked(object sender, RoutedEventArgs e)
        {
            SetTbLabelHide();            //cb_TbLabelShowHide.Content = "������ �����";
            ShowHideMemo = false;
        }

        void cb_TbLabelShowHide_Checked(object sender, RoutedEventArgs e)
        {
            //cb_TbLabelShowHide.Content = "������ ���̱�";
            //SetTbLabelShow();
            if (_key == "")
            {
                //MessageBox.Show("���� �̹����� �����Ͻ� �� ���� ���� �մϴ�.");
                return;
            }
            if (!ShowHideMemo)
            {
                OnPhotoDblClick(sender, e);
            }
        }

        private void SetTbLabelHide()
        {
            if (_key == "")
            {
                //MessageBox.Show("���� �̹����� �����Ͻ� �� ���� ���� �մϴ�.");
                return;
            }
            //TxtcutMemo.Text = string.Empty;
            //TxtFileTitle.Text = string.Empty;
            SetClearTalkBoxLayer();
            TxtLayUICnt.Text = "0"; curUIMemoCnt = 0;
            TxtLayDBCnt.Text = "0";
        }


        /// <summary>
        /// �޴��� ������ ���� �˾�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void btnAdmInform_Click(object sender, RoutedEventArgs e)
        {
            MemoCRUDWin memoWin = new MemoCRUDWin(); // �˻� â
            this.Left = memoWin.Left + (memoWin.Width - this.ActualWidth) / 2;
            this.Top = memoWin.Top + (memoWin.Height - this.ActualHeight) / 2;
            memoWin.ShowDialog();
            //string str_returned = memoWin.selectedText;
        }

        #endregion ############ ������ ���̱� ��� ��ư ############

        #region ######## ���콺 ���� ################

        #region ####### ���콺 �� ###########
        /// <summary>
        /// ���콺 �� Ȯ����ұ�� �̳� ���� ��� ���� ���� - �����̴� �ܹ� �� ���� 2017-11-02
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void root_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (PhotosListBox.SelectedItem != null && (CurShowPhoto_numb>-1))
            {
                #region ####### ���콺 �� Ȯ�� ��� S ###########
                //if (e.Delta > 0)
                //{
                //    if (scaleX < 2)
                //    {
                //        scaleX += 0.1;
                //        scaleY += 0.1;
                //        Zoom.ScaleX = scaleX;
                //        Zoom.ScaleY = scaleY;
                //        //ImageScrollViewer.Height = (ViewedPhoto.Height * Zoom.ScaleY) - ImageScrollViewer.Height;
                //        //ImageScrollViewer.Width = (ViewedPhoto.Width * Zoom.ScaleX) - ImageScrollViewer.Width;
                //        if (Zoom.ScaleY > 1) { Xcanvas.Height = ViewedPhoto.Height * Zoom.ScaleY; }
                //        if (Zoom.ScaleX > 1)
                //        {
                //            Xcanvas.Width = ViewedPhoto.Width * Zoom.ScaleX;
                //            //ViewedPhoto.SetCurrentValue(LeftProperty, ( (ViewedPhoto.Width * Zoom.ScaleX) - ViewedPhoto.Width) / 2);
                //            //ViewedPhoto.SetCurrentValue(TopProperty, ( (ViewedPhoto.Height * Zoom.ScaleY) - ViewedPhoto.Height) / 2);
                //            ViewedPhoto.SetCurrentValue(LeftProperty, Convert.ToDouble(0));
                //            ViewedPhoto.SetCurrentValue(TopProperty, Convert.ToDouble(0));
                //        }
                //    }
                //}
                //else if (e.Delta < 0)
                //{
                //    if (Zoom.ScaleX > 0.2 && Zoom.ScaleY > 0.2)
                //    {
                //        scaleX = scaleX - 0.1;
                //        scaleY = scaleY - 0.1;
                //        Zoom.ScaleX = scaleX;
                //        Zoom.ScaleY = scaleY;
                //        if (Zoom.ScaleY < 1) { Xcanvas.Height = ViewedPhoto.Height; }
                //        if (Zoom.ScaleX < 1) { Xcanvas.Width = ViewedPhoto.Width;}
                //        ViewedPhoto.SetCurrentValue(LeftProperty, Convert.ToDouble(0));
                //        ViewedPhoto.SetCurrentValue(TopProperty, Convert.ToDouble(0));
                //        ImageScrollViewer.ScrollToVerticalOffset(Convert.ToDouble(0));
                //    }
                //}
                #endregion ####### ���콺 �� Ȯ�� ��� E ###########
            }
        }
        #endregion ############ ���콺 �� ############
        /// <summary>
        /// �̹��� Ȯ�� ��� - ���콺 �ٴ�� �����̴� �ٷ� ���� 2017-11-02 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnZoomImage_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsInitialized) return;
            if (PhotosListBox.SelectedItem == null) return;

            lblZoom.Content = ZoomImage.Value + "%";
            double scale = (double)(ZoomImage.Value / 100.0);
            Xcanvas.LayoutTransform = new ScaleTransform(scale, scale);
        }

        /// <summary>
        /// ���콺 ���� ��ư Ŭ�� �̺�Ʈ �ڵ鷯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (CurPhoto_isNormal_DB == "N") { MessageBox.Show("���� �Ұ߿��� �Է��� �Ұ��� �մϴ�."); return; }
            if (PhotosListBox.SelectedItem != null && (CurShowPhoto_numb>-1))
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
            if (PhotosListBox.SelectedItem != null && (CurShowPhoto_numb>-1))
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
                    #region ############# �̹��� �̵� #############
                    // ��Ŭ�� �̹��� �̵��� �޸� ������ �־� ������� �ʴ� ������ ����
                    //Mouse.OverrideCursor = Cursors.Hand;
                    //this.root.CaptureMouse();
                    //Image image = ViewedPhoto;
                    //Point mousePre = e.GetPosition(this.root);
                    //double imgX = mousePre.X - (ViewedPhoto.Width / 2);
                    //double imgY = mousePre.Y - (ViewedPhoto.Height / 2);
                    //image.SetCurrentValue(LeftProperty, imgX);
                    //image.SetCurrentValue(TopProperty, imgY);
                    #endregion ############# �̹��� �̵� #############
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
            if (PhotosListBox.SelectedItem != null && (CurShowPhoto_numb>-1))
            {
                //PhotosListBox..SelectedItem
                this.root.ReleaseMouseCapture(); //���콺 ĸó�� �����Ѵ�.
                SetRectangleProperty();
                #region ############
                try
                {
                    Image image = ViewedPhoto;
                    if (Math.Abs(image.ActualWidth) > 10 && Math.Abs(image.ActualHeight) > 10 && PhotosListBox.SelectedItem != null)
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

                        if (currentRect.Width > 10 && currentRect.Height > 10)
                        {
                            Point talkBoxLocationXY = new Point(left, top);
                            Size _size = new Size(Math.Abs(prePosition.X - currnetPosition.X), Math.Abs(prePosition.Y - currnetPosition.Y)); // �簢�� ũ�� ��ŭ �ؽ�Ʈ ���̾� ũ�� ����
                            image.RenderSize = _size; // �ؽ� Ʈ �ڽ� ũ��

                            Style _cssTalkBox = base.FindResource("cssTalkBox") as Style;
                            Style _cssTalkBoxEdit = base.FindResource("cssTalkBoxEdit") as Style;

                            Int32 fileNum = _LstTalkBoxLayer.Count() + 1;
                            string keyFilename = getKeyFileNameOnly(); //Helpers.keyFilename; 
                                                                       //string cutfileName = getSaveFile("_" + fileNum.ToString() + ".png");
                            string cutfileName = getSaveFileNoPath("_" + fileNum.ToString() + ".png"); // cutfileName �� ���� �̸������� ����
                            string fullPath = getSavePath();
                            string info_fileTxt = getSaveFile(".dat");
                            string fileTitle = TxtFileTitle.Text.ToString(); // ���ϴ�
                            string memo = "[������˻�]";    // ���� �׸��ÿ� ...
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

                            ///////////////////
                            //TalkBoxLayerControl _LstTalkBoxLayerControl = new TalkBoxLayerControl(_talkBoxLayer, _cssTalkBox, _cssTalkBoxEdit);
                            //this.CurTalkBoxControl.Add(_LstTalkBoxLayerControl);
                            ///////////////////
                            Last_talkBoxLayer = _talkBoxLayer; //������ �۾� ���̾ ���� �ϱ� ���� ...
                            Last_image = image; //������ �۾� �̹����� ���� �ϱ� ���� ...
                            Helpers.ExportToPng(fullPath + "/" + cutfileName, image, top, left);

                            #endregion ########## �簢�� �ȿ� _talkLayer ���� end ##########
                            root.Children.Remove(currentRect); // �׷��� �׸�� ���� - obj ���� �ߴ��� ������ �ȵ� ���� �� null ó��
                            currentRect.Visibility = Visibility.Hidden;
                            currentRect = null;
                            GC.Collect();

                            new Action(() => OnOpenPopupClickPRE(sender, e)).SetTimeout(100); //�߰��� ���̾ �˻� �߰�
                        }
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
            string _savePath = getKeyWithPath();
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
        public string getSaveFile(string _extension = ".dat")
        {
            string _saveFileName = getSavePath() + "\\" + System.IO.Path.GetFileNameWithoutExtension(
                getKeyWithPath() //PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/")
                ) + _extension;
            return _saveFileName;
        }

        public string getSaveFileNoPath(string _extension = ".dat")
        {
            string _saveFileName = System.IO.Path.GetFileNameWithoutExtension(getKeyWithPath()) + _extension;
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

        #region ######### �Ұ߻��� #########
        /// <summary>
        /// �Ұ߻���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		void btnDelText_Click(object sender, RoutedEventArgs e)
        {
            //SetDeleteAllTextBox();
        }

        /// <summary>
        /// �Ұ߻���
        /// </summary>
        /// <returns></returns>
        private void SetDeleteAllTextBox()
        {
            if (_key == "") {
                //MessageBox.Show("���� �̹����� �����Ͻ� �� ���� ���� �մϴ�.");
                return;
            }
            TxtcutMemo.Text = string.Empty;
            TxtFileTitle.Text = string.Empty;
            SetClearTalkBoxLayer();
            TxtLayUICnt.Text = "0"; curUIMemoCnt = 0;
            TxtLayDBCnt.Text = "0";
        }

        /// <summary>
        /// �Ұ� DB����
        /// </summary>
        /// <returns></returns>
        private void SetDeleteDB()
        {
            if (_key == "")
            {
                MessageBox.Show("���� �̹����� �����Ͻ� �� ���� ���� �մϴ�.");
                return;
            }
            TxtcutMemo.Text = string.Empty;
            TxtFileTitle.Text = string.Empty;
            SetClearTalkBoxLayer();
            try
            {
                string constr = Helpers.dbCon;
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();
                    string sql = "DELETE FROM TBL_TalkBoxLayer WHERE KeyFilename = '" + _key + "';";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        int result = cmd.ExecuteNonQuery();
                        if (result == 1)
                        {
                            TxtLayUICnt.Text = "0"; curUIMemoCnt = 0;
                            TxtLayDBCnt.Text = "0";
                        }
                    }
                    conn.Close();
                }
            }
            catch (Exception ex) { }
        }
        #endregion ######### �Ұ߻��� #########

        #region ######### �Ұ����� #########
        /// <summary>
        /// ���̾� ���� ������ DB ����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void btnSaveDBText_Click(object sender, RoutedEventArgs e)
        {
            int _saveCnt = 0;
            for (int i = 0; i < _LstTalkBoxLayer.Count; i++) {
                if (_LstTalkBoxLayer[i].Text != null)
                {
                    if (Helpers.SaveDB(_LstTalkBoxLayer[i]) == "success")
                    {
                        _saveCnt++; // ���� ����
                    }
                }
            }
        }

        #endregion ######### �Ұ����� #########

        #region ######### �Ұ߷ε� #########
        /// <summary>
        /// �Ұ��� DB���� �ε��Ѵ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void btnLoadText_Click(object sender, RoutedEventArgs e)
        {
            LoadMemeFromDB();
        }

        /// <summary>
        /// DB���� Data �� ��ȸ �� ȭ�鿡 ���ε� 
        /// </summary>
        public void LoadMemeFromDB()
        {
            TxtcutMemo.Text = "";  // ����
            TxtFileTitle.Text = "";
            SetClearTalkBoxLayer();
            string _KeyFilename = getKeyFileNameOnly();    // ���� �� �߰�

            #region ########## FileTitle ���ε� S ##########
            string _FileTitle = string.Empty;
            DataSet ds1 = new DataSet();
            try
            {
                using (SqlConnection conn = new SqlConnection(Helpers.dbCon))
                {
                    conn.Open();
                    string sql = "Select KeyFilename, isNormalYN, FileTitle ";
                    sql = sql + " From TBL_TalkBoxLayerMst with(nolock) where KeyFilename ='" + _KeyFilename + "' ";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        var adapt = new SqlDataAdapter();
                        adapt.SelectCommand = cmd;
                        adapt.Fill(ds1);
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
            if (ds1 != null && ds1.Tables[0] != null && ds1.Tables[0].Rows.Count > 0)
            {
                _FileTitle = ds1.Tables[0].Rows[0]["FileTitle"].ToString();
                TxtFileTitle.Text = _FileTitle;
            }
            #endregion ########## FileTitle ���ε� E ##########

            #region ########## text ���ε� S ##########
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection conn = new SqlConnection(Helpers.dbCon))
                {
                    conn.Open();
                    string sql = "Select A.KeyFilename, A.CutFilename, A.CutFullPath, A.numb, A.memo, A.PointX, A.PointY, A.SizeW, A.SizeH, Fileimg ";  // B.FileTitle, 
                    sql = sql + " From TBL_TalkBoxLayer A with(nolock) ";   //LEFT OUTER JOIN TBL_TalkBoxLayerMst B with(nolock) on A.KeyFilename = B.KeyFilename 
                    sql = sql + " Where A.KeyFilename ='" + _KeyFilename + "' Order by  A.numb ";
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

            //string _FileTitle = string.Empty;
            string _innerMemo = string.Empty;    // �۳���
            string _TalkBoxLyercutfileName = "";
            string _TalkBoxLyerCutFullPath = "";
            string _TalkBoxLyerFileNum = "";

            //byte[] photo_aray; //DB���� �̹����� �ҷ��´�
            StringBuilder sb2 = new StringBuilder();
            if (ds != null && ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
            {
                int insCnt = 0; // �߰��� ���̾� ��
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    #region ################ ������ Ű _TalkBoxLyercutfileName �� �ִ��� Ȯ�� ####################
                    bool chk_existLayer = false;
                    for (int j = 0; j < _LstTalkBoxLayer.Count; j++)
                    {
                        if ((getKeyFileNameOnly() == _LstTalkBoxLayer[j].TalkBoxLyerkeyFilename)
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
                        ++insCnt; // �߰��� ���̾� ��      
                        _KeyFilename = ds.Tables[0].Rows[i]["KeyFilename"].ToString();
                        _TalkBoxLyercutfileName = ds.Tables[0].Rows[i]["CutFilename"].ToString();
                        _TalkBoxLyerCutFullPath = ds.Tables[0].Rows[i]["CutFullPath"].ToString();
                        _TalkBoxLyerFileNum = ds.Tables[0].Rows[i]["numb"].ToString();
                        _innerMemo = "";
                        _innerMemo = ds.Tables[0].Rows[i]["memo"].ToString();   // �۳��� (�ǻ�Ұ�)
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
                    }
                    curUIMemoCnt = insCnt;
                    #endregion ################ ������ Ű _TalkBoxLyercutfileName ������ ��� �߰� ���� ���� ####################
                }
            }

            mainViewModel.UserCutMemo = sb2.ToString();
            mainViewModel.UserFileMemo = _FileTitle;

            TxtcutMemo.Text = sb2.ToString();  // ����
            #endregion ########## text ���ε� E ##########
            GC.Collect();
        }

        /// <summary>
        /// ���� �Ұ� ���̾� ��ȯ
        /// </summary>
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

        /// <summary>
        /// ���ڸ� �Է�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tb_OnlyNum_KeyPress(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                //if (e.Text != ".") {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    break;
                }
            }
        }

        /// <summary>
        /// �̹��� ���� ���� �Ұ��� �����
        /// </summary>
        public void SetClearTalkBoxLayer() {
            #region #### �ε� �� ���� �Ұ��� ����� ���� �Ұ��� �ҷ� �´� ####
            if (this.CurTalkBox.Count > 0)
            {
                this.CurTalkBox.ForEach(delegate (TalkBoxLayer _txt_layer) { _txt_layer.Delete(); });
                this.CurTalkBox.Clear();
            }
            #endregion #### �ε��� ���� �Ұ��� ����� ���� �Ұ��� �ҷ� �´� ####
        }
        #region ######################### ���� Ʈ������ ���� #########################

        /// <summary>
        /// �ش� ������ �� �̹��� ����
        /// </summary>
        private void GetImageTotalCntShowFromFolder()
        {
            DirectoryInfo _directory = new DirectoryInfo(Helpers.PicFolder);
            int _cnt = 0;
            try
            {
                foreach (FileInfo f in _directory.GetFiles("*.jpg").Union(_directory.GetFiles("*.png")))
                {
                    _cnt++;
                }
            }
            catch (DirectoryNotFoundException)
            {
                //System.Windows.MessageBox.Show("������ �����ϴ�.");
            }
            TxtTotalFileCnt.Text = _cnt.ToString();
        }

        /// <summary>
        /// ������ �̵� ��ư Ŭ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPageChangeClick(object sender, RoutedEventArgs e)
        {
            int _CurPage = Convert.ToInt32(TxtCurPage.Text);
            Helpers.pageIndex= _CurPage;
            //Helpers.pagesize= pagesize;
            Photos = (PhotoCollection)(this.Resources["Photos"] as ObjectDataProvider).Data;
            Photos.Path = Helpers.PicFolder;
            GetImageTotalCntShowFromFolder(); // ��ü ���� ������ �����ش�
        }

        /// <summary>
        /// ���� Ʈ������ ������ ���� Ŭ��������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPhotoDblClick(object sender, RoutedEventArgs e)
        {
            //btnSaveDBText.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); //btnSaveText.PerformClick() in wpf
            btnDelText.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            TxtFileTitle.Text = string.Empty; // �Ұ� data ����
            TxtcutMemo.Text = string.Empty;  // ����
            curUIMemoCnt = 0;   // 20171027 �߰�

            ZoomImage.Value = 100; // �ܹٷ� ����
            if (!ShowHideMemo) {
                ShowHideMemo = true;
                cb_TbLabelShowHide.IsChecked = true;
            }

            ImageSource imageSource = new BitmapImage(new Uri(PhotosListBox.SelectedItem.ToString()));
            ViewedPhoto.Source = imageSource;
            ViewedPhoto.SetCurrentValue(LeftProperty, Convert.ToDouble(0));
            ViewedPhoto.SetCurrentValue(TopProperty, Convert.ToDouble(0));
            _key = PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/");    // ���� Ŭ���� ��������
            new Action(() => btnLoadText.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent))).SetTimeout(500);

            string _GetisNormal_DB = GetisNormal_DB();
            if (_GetisNormal_DB != "")
            {
                Btn_isNormal.IsEnabled = false;
                dc_isNormal.Visibility = Visibility.Hidden;
                cb_isNormal.IsEnabled = false;
                if (_GetisNormal_DB == "Y")
                {
                    Lbl_isNormal.Content = "����Ұ�";
                    cb_isNormal.IsChecked = true;
                    CurPhoto_isNormal_DB = "N"; // ���� ���õ� �̹����� ���� ����/������/�Ǵ���
                }
                else
                {
                    Lbl_isNormal.Content = "������Ұ�";
                    cb_isNormal.IsChecked = false;
                    CurPhoto_isNormal_DB = "Y"; // ���� ���õ� �̹����� ���� ����/������/�Ǵ���
                }
            }
            else {
                Btn_isNormal.IsEnabled = true;
                Lbl_isNormal.Content = "";
                dc_isNormal.Visibility = Visibility.Visible;
                cb_isNormal.IsEnabled = true;
                CurPhoto_isNormal_DB = ""; // ���� ���õ� �̹����� ���� ����/������/�Ǵ���
            }
            CurShowPhoto_numb = GetCurPhotosListBoxNo();
        }

        /// <summary>
        /// ���� �ϴ� ���� �̹��� ���� (�� ȭ��ǥ)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnleftBtnClick(object sender, RoutedEventArgs e)
        {
            if (PhotosListBox.SelectedItem != null && (CurShowPhoto_numb>-1))
            {
                int j = GetCurPhotosListBoxNo();
                if (j - 1 == -1) { MessageBox.Show("ù ��° �Դϴ�."); }
                else{
                    PhotosListBox.SelectedItem = PhotosListBox.Items[j-1];
                    OnPhotoDblClick(sender, e);//new Action(() => OnPhotoDblClick(sender, e) ).SetTimeout(500);
                }
            }
        }

        /// <summary>
        /// ���� ������� ���� ���õ� ������� �� ��° ������ �����´�
        /// </summary>
        /// <returns></returns>
        private int GetCurPhotosListBoxNo() {
            int j = 0; // ���õ� item �� ��ȣ
            if (PhotosListBox.SelectedItem != null)    // && (CurShowPhoto_numb>-1)
            {
                for (int i = 0; i < PhotosListBox.Items.Count; i++)
                {
                    if (PhotosListBox.Items[i] == PhotosListBox.SelectedItem)
                    {
                        j = i;
                        break;
                    }
                }
            }
            return j;
        }

        /// <summary>
        /// ���� �ϴ� ���� �̹��� ���� (�� ȭ��ǥ)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnrightBtnClick(object sender, RoutedEventArgs e)
        {
            if (PhotosListBox.SelectedItem != null && (CurShowPhoto_numb>-1))
            {
                int j = GetCurPhotosListBoxNo();
                if (j + 1 == PhotosListBox.Items.Count) { MessageBox.Show("������ �Դϴ�."); }
                else
                {
                    PhotosListBox.SelectedItem = PhotosListBox.Items[j + 1];
                    OnPhotoDblClick(sender, e);//new Action(() => OnPhotoDblClick(sender, e) ).SetTimeout(500);
                }
            }
        }

        /// <summary>
        /// ���� ���� Ŭ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deletePhoto(object sender, RoutedEventArgs e)
        {
            //string _delFile = PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/");
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("���� �Ͻðڽ��ϱ�?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                MessageBox.Show(getKeyWithPath() + " �� ���� �Ǿ����ϴ�.");
            }
        }

        /// <summary>
        /// ���丮 ���� Ŭ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnImagesDirChangeClick(object sender, RoutedEventArgs e)
        {
            Photos.Clear();
            Helpers.PicFolder = ImagesDir.Text;
            Photos.Path = ImagesDir.Text;

            GetImageTotalCntShowFromFolder();     // ��ü ���� ������ �����ش�
        }
        #endregion ######################### ���� Ʈ������ ���� #########################

        /// <summary>
        /// FileTitle �� ����Ƽ�� �Է½� ������ ������Ʈ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTxtFileTitleKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter)
            {
                string _KeyFilename = getKeyFileNameOnly();    // ���� �� �߰�
                string _FileTitle = TxtFileTitle.Text;
                if (_FileTitle.Trim().Length > 0)
                {
                    try
                    {
                        string constr = Helpers.dbCon;
                        using (SqlConnection conn = new SqlConnection(constr))
                        {
                            conn.Open();
                            string sql = "UPDATE TBL_TalkBoxLayer SET FileTitle='" + Helpers.rtnSQLInj(_FileTitle) + "',updYN='N' WHERE KeyFilename = '" + _KeyFilename + "' ;";
                            using (SqlCommand cmd = new SqlCommand(sql, conn))
                            {
                                int result = cmd.ExecuteNonQuery();
                                if (result == 1)
                                {
                                }
                            }
                            conn.Close();
                        }
                    }
                    catch (Exception ex) { }
                }
                else {
                    MessageBox.Show("������ �Է��� �ּ���.");
                }
                Keyboard.ClearFocus(); // ��Ŀ�� �ƿ�
            }
        }

        /// <summary>
        /// �ǵ���� �������� �ƴ��� ����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBtn_isNormalClick(object sender, RoutedEventArgs e)
        {
            if (CurShowPhoto_numb == -1) {MessageBox.Show("�ǵ� ����� ������ ������ \r\n����Ͽ��� ����Ŭ���Ͽ� �������ּ���."); return; }
            int j = GetCurPhotosListBoxNo();  // �̸� ���� ��ȣ�� ����ִ´�.
            //if (CurShowPhoto_numb != j) { }

            if (cb_isNormal.IsChecked.Value)
            {
                if (TxtcutMemo.Text.Trim().Length > 1) {
                    if (MessageBox.Show("���� �������ֽ��ϴ�.���� ���� �Ұ����� ���� �Ͻðڽ��ϱ�?", "������Ұ� ����� ������ �����˴ϴ�.", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)== MessageBoxResult.No) {
                        return;
                    }
                    
                }
                if (SetisNormal_DB("Y") == "success")
                {
                    GetCurPhotosNumb();
                    MessageBox.Show("���� �Ұ����� ���� �Ǿ����ϴ�.");
                    GetReBindThum();
                    #region ###########
                    if (j + 1 == PhotosListBox.Items.Count) { MessageBox.Show("������ �Դϴ�."); }
                    else
                    {
                        PhotosListBox.SelectedItem = PhotosListBox.Items[j + 1];
                        OnPhotoDblClick(sender, e);
                    }
                    #endregion ############
                }
            }
            else
            {
                if (SetisNormal_DB("N") == "success")
                {
                    GetCurPhotosNumb();
                    MessageBox.Show("������ �Ұ����� ���� �Ǿ����ϴ�.");
                    GetReBindThum();

                    // ���� �̹����� �ٽ� �ε��Ѵ�
                    PhotosListBox.SelectedItem = PhotosListBox.Items[j];
                    OnPhotoDblClick(sender, e);

                }
            }
        }
        /// <summary>
        /// ����Ͽ��� ���° �������� ���� ��ȣ ���ϱ�
        /// </summary>
        /// <returns></returns>
        private int GetCurPhotosNumb()
        {
            int rtn = 0;
            for (int i = 0; i < Photos.Count; i++)
            {
                if (Photos[i] == PhotosListBox.SelectedItem)
                {
                    Photos[i].isNormalBorderColor = SetisNormalBorderColor("Y");
                    break;
                }
            }
            return rtn;
        }

        /// <summary>
        /// ���� �̻� �Ұ����� �ƴ����� ��� ���� ����
        /// </summary>
        /// <param name="isNormalYN"></param>
        /// <returns></returns>
        private string SetisNormalBorderColor(string isNormalYN)
        {
            string rtn = string.Empty;
            switch (isNormalYN)
            {
                case "Y": rtn = "#FFD8E6FF"; break; // �̻� �Ұ� ����
                case "N": rtn = "#FFFFD8D8"; break; // �̻� �Ұ� ����
                default: rtn = "white"; break; // �۾� ��
            }
            return rtn;
        }

        /// <summary>
        /// ���� ������� (�ٽ�)�ε�
        /// </summary>
        private void GetReBindThum() {
            Photos = (PhotoCollection)(this.Resources["Photos"] as ObjectDataProvider).Data;
            Photos.Path = Helpers.PicFolder;
            GetImageTotalCntShowFromFolder(); // ��ü ���� ������ �����ش�
        }

        /// <summary>
        /// ������ ������ �ִ��� ��ȸ 
        /// </summary>
        /// <returns></returns>
        private string GetisNormal_DB() {
            string _KeyFilename = getKeyFileNameOnly();    // ���� �� �߰�
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection conn = new SqlConnection(Helpers.dbCon))
                {
                    conn.Open();
                    string sql = "SELECT KeyFilename, isNormalYN, regdate FROM TBL_TalkBoxLayerMst WITH(NOLOCK) WHERE KeyFilename ='" + _KeyFilename + "' ";
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

            string _isNormalYN = string.Empty;
            if (ds != null && ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
            {
                _isNormalYN = ds.Tables[0].Rows[0]["isNormalYN"].ToString();
            }
            return _isNormalYN;
        }

        /// <summary>
        /// �ǵ���� �������� �ƴ��� ����
        /// </summary>
        /// <param name="_YN"></param>
        /// <returns></returns>
        private string SetisNormal_DB(string _YN)
        {
            string _rtn = "err";
            string _KeyFilename = getKeyFileNameOnly();    // ���� �� �߰�
            try
            {
                using (SqlConnection conn = new SqlConnection(Helpers.dbCon))
                {
                    conn.Open();
                    string sql = " ";
                    sql = sql + " IF (EXISTS(SELECT * FROM TBL_TalkBoxLayerMst WITH(NOLOCK) WHERE KeyFilename = '" + _KeyFilename + "')) ";
                    sql = sql + " BEGIN ";
                    sql = sql + "     UPDATE TBL_TalkBoxLayerMst SET isNormalYN = '" + _YN + "', FileTitle = '" + Helpers.rtnSQLInj(TxtFileTitle.Text) + "' WHERE KeyFilename = '" + _KeyFilename + "' ";
                    sql = sql + " END ";
                    sql = sql + " ELSE ";
                    sql = sql + " BEGIN ";
                    sql = sql + "     INSERT INTO TBL_TalkBoxLayerMst(KeyFilename, isNormalYN, FileTitle) ";
                    sql = sql + "     SELECT '" + _KeyFilename + "' as KeyFilename, '" + _YN + "' as isNormalYN, '" + Helpers.rtnSQLInj(TxtFileTitle.Text) + "' AS FileTitle ";
                    sql = sql + " END ";
                    #region #### �ǵ���� ������ ��� TBL_TalkBoxLayer�� data ���� ���� ####
                    if (_YN == "Y") {
                        sql = sql + " ;DELETE FROM TBL_TalkBoxLayer WHERE KeyFilename = '"+ _KeyFilename + "'";
                    }
                    #endregion #### �ǵ���� ������ ��� TBL_TalkBoxLayer�� data ���� ���� ####
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        int result = cmd.ExecuteNonQuery();
                        if (result == 1)
                        {
                            _rtn = "success";
                        }
                    }
                    conn.Close();
                }
            }
            catch (Exception ex) { }
            return _rtn;
        }

        #region ######### popup ���� #########

        public void OnOpenPopupClickPRE(object sender, RoutedEventArgs e)
        {
            /////////////////////////////////
            if (this.CurTalkBox.Count == 0) {
                MessageBox.Show("���� ���̾� ������ Ŭ�� �����Ͻʴϴ�.");
                return;
            }
            /////////////////////////////////

            int j = 0; // ���� ���õ� �Ұ� ���̾� ��ȣ
            string _lastMemo = string.Empty;
            if (this.CurTalkBox.Count > 0)
            {
                for (int i = 0; i < CurTalkBox.Count; i++)
                {
                    if (CurTalkBox[i].TalkBoxLyerEditModeNow)
                    {
                        j = i;// �ϴܿ� text ���� ��
                        _lastMemo = CurTalkBox[i].Text;
                        break;
                    }
                }
            }

            MemoSearchWin mswin = new MemoSearchWin(); // �˻� â
            this.Left = mswin.Left + (mswin.Width - this.ActualWidth) / 2;
            this.Top = mswin.Top + (mswin.Height - this.ActualHeight) / 2;
            mswin.ShowDialog();
            string str_returned = mswin.selectedText;
            if (str_returned !=null && str_returned.Trim().Length > 0) {
                if (CurTalkBox[j].Text == "[������˻�]")
                {
                    CurTalkBox[j].Text = str_returned;
                }
                else
                {
                    CurTalkBox[j].Text = _lastMemo + "/" + str_returned;
                }

                //�޸� �Էµ� data �� ù��° �Էµ� data���� Ȯ��
                string _appendTitle = CurTalkBox[j].Text;
                if (_appendTitle.IndexOf("/") > -1)
                {
                    //string[] _result = _appendTitle.Split('/');
                    //_appendTitle = _result[0];
                }
                else {
                    // �޸��� data �� ù��° data ���� TxtFileTitle �� �߰�
                    string _strFileTitle = TxtFileTitle.Text;
                    // ù��° ���������� Ȯ��
                    if (_strFileTitle.Trim().Length == 0)
                    {
                        TxtFileTitle.Text = _appendTitle;
                    }
                    else
                    {
                        TxtFileTitle.Text = _strFileTitle + "/" + _appendTitle;
                    }

                }


                // ���� ù��° ���̾ �׸����̶�� �� �ش� �̹����� ù��° �Ұ��� �ۼ� �� ���̶�� 
                if (CurTalkBox.Count == 1)
                {
                    cb_isNormal.IsChecked = false;
                }
            }
        }
        #endregion ######### popup ���� #########

        #region ######### tree #########
        private void TreeViewItem_OnItemSelected(object sender, RoutedEventArgs e)
        {
            LeftTree.Tag = e.OriginalSource;
            TreeViewItem tvi = LeftTree.Tag as TreeViewItem;
            
            ImagesDir.Text = GetFullPath(tvi);  //
        }

        public string GetFullPath(TreeViewItem node)
        {
            if (node == null)
                throw new ArgumentNullException();

            var result = Convert.ToString(node.Header);
            for (var i = GetParentItem(node); i != null; i = GetParentItem(i))
            {
                result = i.Header + "\\" + result;
            }
            result = result.Replace("\\\\", "\\");
            return result;
        }
        static TreeViewItem GetParentItem(TreeViewItem item)
        {
            for (var i = VisualTreeHelper.GetParent(item); i != null; i = VisualTreeHelper.GetParent(i))
                if (i is TreeViewItem)
                    return (TreeViewItem)i;

            return null;
        }
        //private void ListDirectory(TreeView treeView, string path)
        //{
        //    //treeView.Items.Clear();
        //    var rootDirectoryInfo = new DirectoryInfo(path);
        //    treeView.Items.Add(CreateDirectoryNode(rootDirectoryInfo));
        //}

        //private static TreeViewItem CreateDirectoryNode(DirectoryInfo directoryInfo)
        //{
        //    var directoryNode = new TreeViewItem { Header = directoryInfo.Name };
        //    foreach (var directory in directoryInfo.GetDirectories())
        //        directoryNode.Items.Add(CreateDirectoryNode(directory));

        //    foreach (var file in directoryInfo.GetFiles())
        //        directoryNode.Items.Add(new TreeViewItem { Header = file.Name });

        //    return directoryNode;
        //}

        public void LoadDirectories()
        {
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                this.LeftTree.Items.Add(this.GetItem(drive));
                //if (Helpers.PicFolder.IndexOf(drive.RootDirectory.Name.ToString()) >-1) {
                //    //MessageBox.Show(drive.RootDirectory.Name.ToString());
                //    GetItem(drive);
                //}
            }
        }
        private TreeViewItem GetItem(DriveInfo drive)
        {
            var item = new TreeViewItem
            {
                Header = drive.Name,
                DataContext = drive,
                Tag = drive
            };
            this.AddDummy(item);
            item.Expanded += new RoutedEventHandler(item_Expanded);
            return item;
        }
        private TreeViewItem GetItem(DirectoryInfo directory)
        {
            var item = new TreeViewItem
            {
                Header = directory.Name,
                DataContext = directory,
                Tag = directory
            };
            this.AddDummy(item);
            item.Expanded += new RoutedEventHandler(item_Expanded);
            return item;
        }

        private TreeViewItem GetItem(FileInfo file)
        {
            var item = new TreeViewItem
            {
                Header = file.Name,
                DataContext = file,
                Tag = file
            };
            return item;
        }
        private void AddDummy(TreeViewItem item)
        {
            item.Items.Add(new DummyTreeViewItem());
        }
        private bool HasDummy(TreeViewItem item)
        {
            return item.HasItems && (item.Items.OfType<TreeViewItem>().ToList().FindAll(tvi => tvi is DummyTreeViewItem).Count > 0);
        }
        private void RemoveDummy(TreeViewItem item)
        {
            var dummies = item.Items.OfType<TreeViewItem>().ToList().FindAll(tvi => tvi is DummyTreeViewItem);
            foreach (var dummy in dummies)
            {
                item.Items.Remove(dummy);
            }
        }

        private void ExploreDirectories(TreeViewItem item)
        {
            var directoryInfo = (DirectoryInfo)null;
            if (item.Tag is DriveInfo)
            {
                directoryInfo = ((DriveInfo)item.Tag).RootDirectory;
            }
            else if (item.Tag is DirectoryInfo)
            {
                directoryInfo = (DirectoryInfo)item.Tag;
                //ImagesDir.Text = directoryInfo.FullName.ToString();  //
            }
            //else if (item.Tag is FileInfo)
            //{
            //    directoryInfo = ((FileInfo)item.Tag).Directory;
            //}
            if (object.ReferenceEquals(directoryInfo, null)) {
                //ImagesDir.Text = directoryInfo.FullName.ToString();  //
                return;
            }
            foreach (var directory in directoryInfo.GetDirectories())
            {
                var isHidden = (directory.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                var isSystem = (directory.Attributes & FileAttributes.System) == FileAttributes.System;
                if (!isHidden && !isSystem)
                {
                    item.Items.Add(this.GetItem(directory));
                }
            }
        }

        private void ExploreFiles(TreeViewItem item)
        {
            var directoryInfo = (DirectoryInfo)null;
            if (item.Tag is DriveInfo)
            {
                directoryInfo = ((DriveInfo)item.Tag).RootDirectory;
            }
            else if (item.Tag is DirectoryInfo)
            {
                directoryInfo = (DirectoryInfo)item.Tag;
                //ImagesDir.Text = directoryInfo.FullName.ToString();  //
            }
            else if (item.Tag is FileInfo)
            {
                directoryInfo = ((FileInfo)item.Tag).Directory;
            }
            if (object.ReferenceEquals(directoryInfo, null)) return;
            // ������ ������ �ʿ䰡 ���
            //foreach (var file in directoryInfo.GetFiles())
            //{
            //    var isHidden = (file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
            //    var isSystem = (file.Attributes & FileAttributes.System) == FileAttributes.System;
            //    if (!isHidden && !isSystem)
            //    {
            //        item.Items.Add(this.GetItem(file));
            //    }
            //}
        }

        void item_Expanded(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem)sender;
            if (this.HasDummy(item))
            {
                this.Cursor = Cursors.Wait;
                this.RemoveDummy(item);
                this.ExploreDirectories(item);
                //this.ExploreFiles(item);
                this.Cursor = Cursors.Arrow;
            }
        }

        #endregion  ######### tree #########

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

        #region #### PropertyChangedEventHandler ####
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion #### PropertyChangedEventHandler ####

    }

    #region ######### Ʈ���� ��
    /// <summary>
    /// Ʈ���� ��
    /// </summary>
    public class DummyTreeViewItem : TreeViewItem
    {
        public DummyTreeViewItem()
            : base()
        {
            base.Header = "Dummy";
            base.Tag = "Dummy";
        }
    }
    #endregion  ######### Ʈ���� ��

    #region ######### Extension #########
    /// <summary>
    /// timeOut �ڹٽ�ũ��Ʈ�� SetTimeout 
    /// </summary>
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

    /// <summary>
    /// PrintNew UI ���ſ�
    /// </summary>
    public static class ExtensionMethods
    {
        private static Action EmptyDelegate = delegate () { };

        public static void PrintNew(this UIElement uiElement)
        {
            uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }
        //TxtFileTitle.PrintNew();
        //TxtcutMemo.PrintNew();
    }
    #endregion  ######### Extension #########
}
