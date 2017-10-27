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
        #region ######################### 선언 #########################
        Point prePosition; //드레그를 시작한 마우스 좌표;
        Rectangle currentRect; //현재 그려지는 네모
        public PhotoCollection Photos = new PhotoCollection(Helpers.PicFolder);
        readonly List<TalkBoxLayer> _LstTalkBoxLayer = new List<TalkBoxLayer>();  // 소견 데이터
        //List<TalkBoxLayerControl> _LstTalkBoxLayerControl = new List<TalkBoxLayerControl>();  // 소견의 컨트롤
        double scaleX = 1;
        double scaleY = 1;
        TranslateTransform translate = new TranslateTransform();

        public TalkBoxLayer Last_talkBoxLayer = null; // 마지막 선택/작업 되었던 레이어
        public Image Last_image = null; // 마지막 선택/작업 되었던 이미지  
        public string _key = string.Empty;      //key 파일 이미지(좌측 썸네일중 선택된 파일 경로 및 이름 )

        //public event EventHandler<EventArgs> eventMainNeedChange;
        public int curUIMemoCnt = 0;  // 화면 상의 메모 레이어 갯수
        XrayTEXT.ViewModels.MainViewModel mainViewModel = new ViewModels.MainViewModel();
        #endregion ######################### 선언 #########################

        #region ######################### MainWin #########################
        public MainWin()
        {
            InitializeComponent();
            //Photos = (PhotoCollection)(this.Resources["Photos"] as ObjectDataProvider).Data;
            //Photos.Path = Helpers.PicFolder;

            this.root.MouseLeftButtonDown += new MouseButtonEventHandler(root_MouseLeftButtonDown);
            this.root.MouseLeftButtonUp += new MouseButtonEventHandler(root_MouseLeftButtonUp);
            this.root.MouseWheel += new MouseWheelEventHandler(root_MouseWheel);
            this.root.MouseMove += new MouseEventHandler(root_MouseMove);

            // 좌표를 나타내는 TextBlock을 최상위로 ...
            //Grid.SetZIndex(this.tbPosition, 99999);

            // Zoom In, Zoom Out을 위한 Center 좌표 설정
            Zoom.CenterX = ViewedPhoto.Width / 2;
            Zoom.CenterY = ViewedPhoto.Height / 2;
            //회전은 Angle 만 돌리면 되나 일단 보류

            //DataContext = new ModelTextbox() { TxWidth = 100, TxHeight=100 };

            //우측 상단 주석들
            //Helpers helper = new Helpers(); //class1을 동적할당
            //helper.ReturnToText += new Helpers.deleg_TxtcutMemo(TxtcutMemo_set);//이벤트 핸들러 연결
            //helper.SetTextChange();//이벤트 호출 값을 지닌 함수 호출


            //TreeViewFile treeView = new TreeViewFile(Helpers.PicFolder);
            //LeftTree.Items.Add(treeView);
            LoadDirectories();
            new Action(() => setLeftTreeBind()).SetTimeout(500);

            DataContext = mainViewModel; // 좌측 상단 하단의 TEXT 변경 용
            StartThread();
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
            int chkDBCnt = GetMemoDBCnt();
            
            //if (eventMainNeedChange != null) { 
            //    eventMainNeedChange(this, new EventArgs());
            //}

            // 화면의 ui 수와 db 의 ui 수가 다르면 같게 맞춘다.
            if ((curUIMemoCnt>0 || chkDBCnt>0) && (curUIMemoCnt != chkDBCnt) ) {
                Reload_Right_Text();
            }

            //mainViewModel.UserCutMemo = curUIMemoCnt.ToString();
            //MessageBox.Show("UserCutMemo");
            TxtLayUICnt.Text = curUIMemoCnt.ToString();
            TxtLayDBCnt.Text = chkDBCnt.ToString();

            //TxtcutMemo.Text = mainViewModel.UserCutMemo;
            //TxtFileTitle.Text = mainViewModel.UserFileMemo;
        }
        private int GetMemoDBCnt()
        {
            int rtn = 0;
            string _KeyFilename = getKeyFileNameOnly();    // 파일 명 추가
            DataSet ds = new DataSet();
            try
            {
                string _text = string.Empty;
                using (SqlConnection conn = new SqlConnection(Helpers.dbCon))
                {
                    conn.Open();
                    string sql = "Select count(*) From TBL_TalkBoxLayer with(nolock) where KeyFilename ='" + _KeyFilename + "' ";
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
            rtn = Convert.ToInt32(ds.Tables[0].Rows[0][0].ToString());
            return rtn;
        }
        private void Reload_Right_Text()
        {
            #region ########## text 바인딩 S ##########
            string _KeyFilename = getKeyFileNameOnly();    // 파일 명 추가
            DataSet ds = new DataSet();
            try
            {
                string _text = string.Empty;
                using (SqlConnection conn = new SqlConnection(Helpers.dbCon))
                {
                    conn.Open();
                    string sql = "Select KeyFilename, CutFilename, CutFullPath, FileTitle, numb, memo, PointX, PointY, SizeW, SizeH, Fileimg ";
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

            string _FileTitle = string.Empty;
            string _innerMemo = string.Empty;    // 글내용
            StringBuilder sb2 = new StringBuilder();
            if (ds != null && ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
            {
                _FileTitle = ds.Tables[0].Rows[0]["FileTitle"].ToString();
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    _innerMemo = "";
                    _innerMemo = ds.Tables[0].Rows[i]["memo"].ToString();   // 글내용 (의사소견)
                    sb2.AppendLine(_innerMemo);
                }
                curUIMemoCnt = ds.Tables[0].Rows.Count;
            }
            #endregion ########## text 바인딩 E ##########
            mainViewModel.UserCutMemo = sb2.ToString();
            mainViewModel.UserFileMemo = _FileTitle;
            TxtcutMemo.Text = sb2.ToString();  // 우상단
            TxtFileTitle.Text = _FileTitle;
        }

        private void setLeftTreeBind() {
            //DirectoryInfo _directory = new DirectoryInfo(Helpers.PicFolder);
            //item.Items.Add(this.GetItem(directory));
            //ExploreDirectories(TreeViewItem item)

            //Helpers.PicFolder
            foreach (string s in Directory.EnumerateDirectories(Helpers.PicFolder))
            {
                TreeViewItem item = new TreeViewItem();
                item.Header = s.Substring(s.LastIndexOf('\\') + 1);
                item.Tag = s;
                item.FontWeight = FontWeights.Normal;

                FillTreeView(item, s);
                LeftTree.Items.Add(item);
            }
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


        /// <summary>
        /// DB 에서 소견과 파일내용을 받아와 바인딩 한다.
        /// </summary>
        private void getTextFromDB() {
            #region ########## text 바인딩 S ##########

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
            string _innerMemo = string.Empty;    // 글내용

            StringBuilder sb2 = new StringBuilder();
            if (ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
            {
                _FileTitle = ds.Tables[0].Rows[0]["FileTitle"].ToString();
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    _innerMemo = "";
                    _innerMemo = ds.Tables[0].Rows[i]["memo"].ToString();   // 글내용 (의사소견)
                    sb2.AppendLine(_innerMemo);
                }
            }
            
            #endregion ########## text 바인딩 E ##########
            mainViewModel.UserCutMemo = sb2.ToString();
            mainViewModel.UserFileMemo = _FileTitle;

            TxtcutMemo.Text = sb2.ToString();  // 우상단
            TxtFileTitle.Text = _FileTitle;
        }


        #endregion ######################### MainWin #########################

        #region ######## DB 키 생성 ########
        public string getKeyWithPath()
        {
            //string _key = PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/");
            return _key;    // 최상단에 정의
        }
        public string getKeyFileNameOnly()
        {
            string _mainkey = getKeyWithPath();
            string FileNameOnly = _mainkey.Substring(_mainkey.LastIndexOf("/") + 1);
            return FileNameOnly;
        }
        #endregion ######## DB 키 생성 ########

        #region ######## 마우스 관련 ################

        #region ####### 마우스 휠 ###########
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
        #endregion ############ 마우스 휠 ############

        /// <summary>
        /// 마우스 왼쪽 버튼 클릭 이벤트 핸들러
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (PhotosListBox.SelectedItem != null)
            {
                //마우스의 좌표를 저장한다.
                prePosition = e.GetPosition(this.root);
                //마우스가 Grid밖으로 나가도 위치를 알 수 있도록 마우스 이벤트를 캡처한다.
                this.root.CaptureMouse();
                if (currentRect == null)
                {
                    //사각형을 생성한다.
                    CreateRectangle();
                }
            }
        }

        /// <summary>
        /// 마우스 이동 이벤트 핸들러
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void root_MouseMove(object sender, MouseEventArgs e)
        {
            if (PhotosListBox.SelectedItem != null)
            {
                //현재 이동한 마우스의 좌표를 얻어온다
                Point currnetPosition = e.GetPosition(this.root);
                //좌표를 표시한다.
                //this.tbPosition.Text = string.Format("마우스 좌표 : [{0},{1}]", currnetPosition.X, currnetPosition.Y);
                //마우스 왼쪽 버튼이 눌려있으면
                if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
                {
                    if (currentRect != null)
                    {
                        //사각형이 나타날 기준점을 설정한다.
                        double left = prePosition.X;
                        double top = prePosition.Y;
                        //마우스의 위치에 따라 적절히 기준점을 변경한다.
                        if (prePosition.X > currnetPosition.X)
                        {
                            left = currnetPosition.X;
                        }
                        if (prePosition.Y > currnetPosition.Y)
                        {
                            top = currnetPosition.Y;
                        }
                        currentRect.Margin = new Thickness(left, top, 0, 0); //사각형의 위치 기준점(Margin)을 설정한다
                        currentRect.Width = Math.Abs(prePosition.X - currnetPosition.X); //사각형의 크기를 설정한다. 음수가 나올 수 없으므로 절대값을 취해준다.
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
        /// 마우스 클릭 후 뗄때
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void root_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (PhotosListBox.SelectedItem != null)
            {
                this.root.ReleaseMouseCapture(); //마우스 캡처를 제거한다.
                SetRectangleProperty();
                #region ############
                try
                {
                    Image image = ViewedPhoto;
                    if (Math.Abs(image.ActualWidth) > 10 && PhotosListBox.SelectedItem != null)
                    {
                        #region ########## 사각형 안에 _talkLayer 삽입 ##########
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
                        Size _size = new Size(Math.Abs(prePosition.X - currnetPosition.X), Math.Abs(prePosition.Y - currnetPosition.Y)); // 사각형 크기 만큼 텍스트 레이어 크기 지정
                        image.RenderSize = _size; // 텍스 트 박스 크기

                        Style _cssTalkBox = base.FindResource("cssTalkBox") as Style;
                        Style _cssTalkBoxEdit = base.FindResource("cssTalkBoxEdit") as Style;

                        Int32 fileNum = _LstTalkBoxLayer.Count() + 1;
                        string keyFilename = getKeyFileNameOnly(); //Helpers.keyFilename; 
                        //string cutfileName = getSaveFile("_" + fileNum.ToString() + ".png");
                        string cutfileName = getSaveFileNoPath("_" + fileNum.ToString() + ".png"); // cutfileName 도 순수 이름만으로 변경
                        string fullPath = getSavePath();
                        string info_fileTxt = getSaveFile(".dat");
                        string fileTitle = TxtFileTitle.Text.ToString(); // 우하단
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

                        ///////////////////
                        //TalkBoxLayerControl _LstTalkBoxLayerControl = new TalkBoxLayerControl(_talkBoxLayer, _cssTalkBox, _cssTalkBoxEdit);
                        //this.CurTalkBoxControl.Add(_LstTalkBoxLayerControl);
                        ///////////////////

                        Last_talkBoxLayer = _talkBoxLayer; //마지막 작업 레이어를 저장 하기 위해 ...
                        Last_image = image; //마지막 작업 이미지를 저장 하기 위해 ...
                        Helpers.ExportToPng(fullPath +"/"+ cutfileName, image, top, left);

                        #endregion ########## 사각형 안에 _talkLayer 삽입 end ##########
                        root.Children.Remove(currentRect); // 그려진 네모는 삭제 - obj 삭제 했더니 재사용이 안되 히든 및 null 처리
                        currentRect.Visibility = Visibility.Hidden;
                        currentRect = null;
                        GC.Collect();
                        //SetSaveAllTextBox(info_fileTxt);
                        //curUIMemoCnt = curUIMemoCnt + 1;  // 저장시 현재 레이어 갯수를 올려준다.
                    }
                }
                catch (Exception ex) {

                }
                #endregion ############
            }
        }
        
        /// <summary>
        /// 저장될 파일 경로
        /// </summary>
        /// <returns></returns>
        public string getSavePath() {
            string _savePath = getKeyWithPath();
            if (_savePath.Length > 10) {
                _savePath = _savePath.Substring(0, _savePath.Length - 4);
            }
            System.IO.Directory.CreateDirectory(_savePath); // 폴더가 없으면 생성
            File.SetAttributes(_savePath, FileAttributes.Hidden); // 폴더 속성을 히든으로 처리
            return _savePath;
        }

        /// <summary>
        /// 저장될 파일명
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

        #region ######## 네모 ################
        private void SetRectangleProperty()
        {
            try
            {
                //사각형의 투명도를 100% 로 설정
                currentRect.Opacity = 0.7;
                //사각형의 색상을 지정
                currentRect.Fill = new SolidColorBrush(Colors.LightYellow);
                currentRect.Opacity = 0.35;
                //사각형의 테두리를 선으로 지정
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
            //사각형을 그리는 동안은 테두리를 Dash 스타일로 설정한다.
            DoubleCollection dashSize = new DoubleCollection();
            dashSize.Add(1);
            dashSize.Add(1);
            currentRect.StrokeDashArray = dashSize;
            currentRect.StrokeDashOffset = 0;
            //사각형의 정렬 기준을 설정한다.
            currentRect.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            currentRect.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            //그리드에 추가한다.
            this.root.Children.Add(currentRect);
        }

        #endregion ######## 네모 ################

        #endregion ######## 마우스 관련 ################


        #region ######### 소견삭제 #########
        /// <summary>
        /// 소견삭제
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		void btnDelText_Click(object sender, RoutedEventArgs e)
        {
            SetDeleteAllTextBox();
        }

        /// <summary>
        /// 소견삭제
        /// </summary>
        /// <returns></returns>
        private void SetDeleteAllTextBox()
        {
            if (_key == "") {
                //MessageBox.Show("좌측 이미지를 선택하신 후 삭제 가능 합니다.");
                return;
            }
            TxtcutMemo.Text = string.Empty;
            TxtFileTitle.Text = string.Empty;
            SetClearTalkBoxLayer();
            TxtLayUICnt.Text = "0"; curUIMemoCnt = 0;
            TxtLayDBCnt.Text = "0";
                       
        }

        /// <summary>
        /// 소견 DB삭제
        /// </summary>
        /// <returns></returns>
        private void SetDeleteDB()
        {
            if (_key == "")
            {
                MessageBox.Show("좌측 이미지를 선택하신 후 삭제 가능 합니다.");
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
        #endregion ######### 소견삭제 #########

        #region ######### 소견저장 #########
        /// <summary>
        /// 레이어 빠저 나갈때 DB 저장
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
                        _saveCnt++; // 저장 성공
                    }
                }
            }
        }
        
        ///// <summary>
        ///// 파일로 저장 하는 부분 삭제
        ///// </summary>
        ///// <returns></returns>
        //public string getTxtFileTitle()
        //{
        //    string _Talk = string.Empty;
        //    _Talk = TxtFileTitle.Text.ToString();
        //    return _Talk;
        //}
        #endregion ######### 소견저장 #########

        #region ######### 소견로드 #########
        /// <summary>
        /// 소견을 DB에서 로드한다
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void btnLoadText_Click(object sender, RoutedEventArgs e)
        {
            LoadMemeFromDB();
        }

        /// <summary>
        /// DB에서 Data 를 조회 후 화면에 바인딩 
        /// </summary>
        public void LoadMemeFromDB()
        {
            TxtcutMemo.Text = "";  // 우상단
            TxtFileTitle.Text = "";
            SetClearTalkBoxLayer();

            string _KeyFilename = getKeyFileNameOnly();    // 파일 명 추가

            #region ########## text 바인딩 S ##########
            DataSet ds = new DataSet();
            try
            {
                string _text = string.Empty;
                using (SqlConnection conn = new SqlConnection(Helpers.dbCon))
                {
                    conn.Open();
                    string sql = "Select KeyFilename, CutFilename, CutFullPath, FileTitle, numb, memo, PointX, PointY, SizeW, SizeH, Fileimg ";
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

            string _FileTitle = string.Empty;
            string _innerMemo = string.Empty;    // 글내용
            string _TalkBoxLyercutfileName = "";
            string _TalkBoxLyerCutFullPath = "";
            string _TalkBoxLyerFileNum = "";

            TalkBoxLayer talkBoxLayer_Last_Add = null;

            //byte[] photo_aray; //DB에서 이미지를 불러온다
            StringBuilder sb2 = new StringBuilder();
            if (ds != null && ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
            {
                _FileTitle = ds.Tables[0].Rows[0]["FileTitle"].ToString();

                int insCnt = 0; // 추가된 레이어 수
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    #region ################ 동일한 키 _TalkBoxLyercutfileName 가 있는지 확인 ####################
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
                    #endregion ################ 동일한 키 _TalkBoxLyercutfileName 가 있는지 확인 ####################

                    #region ################ 동일한 키 _TalkBoxLyercutfileName 이있을 경우 추가 하지 않음 ####################
                    if (!chk_existLayer)
                    {
                        ++insCnt; // 추가된 레이어 수      
                        _KeyFilename = ds.Tables[0].Rows[i]["KeyFilename"].ToString();
                        _TalkBoxLyercutfileName = ds.Tables[0].Rows[i]["CutFilename"].ToString();
                        _TalkBoxLyerCutFullPath = ds.Tables[0].Rows[i]["CutFullPath"].ToString();
                        _TalkBoxLyerFileNum = ds.Tables[0].Rows[i]["numb"].ToString();
                        _innerMemo = "";
                        _innerMemo = ds.Tables[0].Rows[i]["memo"].ToString();   // 글내용 (의사소견)
                        sb2.AppendLine(_innerMemo);

                        Point talkBoxLocationXY = new Point(Convert.ToDouble(ds.Tables[0].Rows[i]["PointX"].ToString()), Convert.ToDouble(ds.Tables[0].Rows[i]["PointY"].ToString()));
                        Image image = new Image();
                        image = ViewedPhoto;

                        //DB에서 이미지를 불러온다
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

                        //TalkBoxLayerControl _LstTalkBoxLayerControl = new TalkBoxLayerControl(talkBoxLayer, _cssTalkBox, _cssTalkBoxEdit);
                        //this.CurTalkBoxControl.Add(_LstTalkBoxLayerControl);

                    }
                    curUIMemoCnt = insCnt;
                    #endregion ################ 동일한 키 _TalkBoxLyercutfileName 이있을 경우 추가 하지 않음 ####################
                }
                #region ##### 마지막에 추가된 레이어를 편집 모드에서 해제 #####
                if (talkBoxLayer_Last_Add != null)
                {
                    TalkBoxLayerControl _control;
                    Style _cssTalkBox = this.FindResource("cssTalkBox") as Style;
                    Style _cssTalkBoxEdit = this.FindResource("cssTalkBoxEdit") as Style;
                    _control = new TalkBoxLayerControl(talkBoxLayer_Last_Add, _cssTalkBox, _cssTalkBoxEdit);
                    _control.IsEnabled = false;
                }
                #endregion ##### 마지막에 추가된 레이어를 편집 모드에서 해제 #####
            }

            mainViewModel.UserCutMemo = sb2.ToString();
            mainViewModel.UserFileMemo = _FileTitle;

            TxtcutMemo.Text = sb2.ToString();  // 우상단
            TxtFileTitle.Text = _FileTitle;
            #endregion ########## text 바인딩 E ##########
            GC.Collect();
        }

        /// <summary>
        /// 현재 소견 레이어 반환
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

        //public List<TalkBoxLayerControl> CurTalkBoxControl
        //{
        //    get
        //    {
        //        //if (this.tabControl.SelectedIndex == 0)
        //        return _LstTalkBoxLayerControl;
        //        //return _habitatAnnotations;
        //    }
        //}
        

        #endregion ######### 소견로드 #########



        public void SetClearTalkBoxLayer() {
            #region #### 로딩 전 기존 소견을 지우고 현재 소견을 불러 온다 ####
            if (this.CurTalkBox.Count > 0)
            {
                this.CurTalkBox.ForEach(delegate (TalkBoxLayer _txt_layer) {
                    _txt_layer.Delete();
                    //_txt_layer.SetHidden();
                });
                this.CurTalkBox.Clear();
            }

            //this.CurTalkBoxControl.Clear();


            #endregion #### 로딩전 기존 소견을 지우고 현재 소견을 불러 온다 ####

        }

        ////
        //public void GetTalkBoxEnable(object sender, RoutedEventArgs e) {
        //    string str = CurTalkBoxControl.Count().ToString();
        //    for (int i = 0; i < CurTalkBoxControl.Count(); i++)
        //    {
        //        str = str + "," + CurTalkBoxControl[i].IsEnabled.ToString();
        //    }
        //    MessageBox.Show(str);
        //    TxtFileTitle.Text = str;
        //}
        #region ######################### 좌측 트리에서 사진 #########################
        /// <summary>
        /// 좌측 트리에서 사진을 더블 클릭했을때
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPhotoDblClick(object sender, RoutedEventArgs e)
        {
            //btnSaveDBText.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); //btnSaveText.PerformClick() in wpf
            btnDelText.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            TxtFileTitle.Text = string.Empty; // 소견 data 삭제
            TxtcutMemo.Text = string.Empty;  // 우상단
            curUIMemoCnt = 0;   // 20171027 추가
            ImageSource imageSource = new BitmapImage(new Uri(PhotosListBox.SelectedItem.ToString()));
            ViewedPhoto.Source = imageSource;

            _key = PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/");    // 더블 클릭시 변경으로
            new Action(() => btnLoadText.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent))).SetTimeout(500);
        }

        private void deletePhoto(object sender, RoutedEventArgs e)
        {
            //string _delFile = PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/");
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("삭제 하시겠습니까?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                MessageBox.Show(getKeyWithPath() + " 가 삭제 되었습니다.");
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

        #endregion ######################### 좌측 트리에서 사진 #########################

        #region ######### tree #########
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
            }
            else if (item.Tag is FileInfo)
            {
                directoryInfo = ((FileInfo)item.Tag).Directory;
            }
            if (object.ReferenceEquals(directoryInfo, null)) return;
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
                ImagesDir.Text = directoryInfo.FullName.ToString();  //
            }
            else if (item.Tag is FileInfo)
            {
                directoryInfo = ((FileInfo)item.Tag).Directory;
            }
            if (object.ReferenceEquals(directoryInfo, null)) return;
            foreach (var file in directoryInfo.GetFiles())
            {
                var isHidden = (file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                var isSystem = (file.Attributes & FileAttributes.System) == FileAttributes.System;
                if (!isHidden && !isSystem)
                {
                    item.Items.Add(this.GetItem(file));
                }
            }
        }

        void item_Expanded(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem)sender;
            if (this.HasDummy(item))
            {
                this.Cursor = Cursors.Wait;
                this.RemoveDummy(item);
                this.ExploreDirectories(item);
                this.ExploreFiles(item);
                this.Cursor = Cursors.Arrow;
            }
        }

        #endregion  ######### tree #########

        #region ##################### 기타 차후 사용할수 있어서 일단 주석 처리 #####################

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

        #endregion ##################### 기타 차후 사용할수 있어서 일단 주석 처리 #####################

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


    #region ######### menu
    #endregion  ######### menu


    /// <summary>
    /// 트리뷰 용
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

    #region ########### 썸네일용 

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

                //BitmapImage bmi = new BitmapImage();
                //bmi.BeginInit();
                //bmi.CacheOption =
                //          BitmapCacheOption.OnLoad;
                //bmi.UriSource = _source;
                //bmi.EndInit();
                //image1.Source = bmi;

                // 기존
                _image = BitmapFrame.Create(_source);
                //_image.Metadata as BitmapMetadata;
                //_image = BitmapFrame.Create(_source, BitmapCreateOptions.None, BitmapCacheOption.None);

                //// 신규
                //BitmapImage image = new BitmapImage();
                //using (FileStream stream = File.OpenRead(path))
                //{
                //    image.BeginInit();
                //    image.StreamSource = stream;
                //    image.CacheOption = BitmapCacheOption.OnLoad;
                //    image.EndInit(); // load the image from the stream
                //    _image = BitmapFrame.Create(_source, stream);
                //} 
                //_image = image;

                //case 2
                //var filename = "fb.png";
                //using (var image = Image.FromFile(filename))
                //{
                //    using (var thumbnail = image.GetThumbnailImage(20/*width*/, 40/*height*/, null, IntPtr.Zero))
                //    {
                //        thumbnail.Save("thumb.png");
                //    }
                //}
            }
            catch (NotSupportedException)
            {
                //MessageBox.Show("NotSupportedException");
            }
        }

        private Image getPngImage(string path) {
            Image myImage = new Image();
            myImage.Width = 200;

            // Create source
            BitmapImage myBitmapImage = new BitmapImage();

            // BitmapImage.UriSource must be in a BeginInit/EndInit block
            myBitmapImage.BeginInit();
            myBitmapImage.UriSource = new Uri(path);
            myBitmapImage.DecodePixelWidth = 200;
            myBitmapImage.EndInit();
            //set image source
            myImage.Source = myBitmapImage;
            return myImage;
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
        DirectoryInfo _directory;

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
                foreach (FileInfo f in _directory.GetFiles("*.jpg").Union(_directory.GetFiles("*.png")))
                {
                    Add(new Photo(f.FullName));
                }
            }
            catch (DirectoryNotFoundException)
            {
                System.Windows.MessageBox.Show("폴더가 없습니다.");
            }
        }

        

    }
    #endregion ########### 썸네일용

    #region ######### Extension #########
    /// <summary>
    /// timeOut 자바스크립트의 SetTimeout 
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
    /// PrintNew UI 갱신용
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
