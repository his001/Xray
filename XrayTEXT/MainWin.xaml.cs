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
        #region ######################### 선언 #########################
        Point prePosition; //드레그를 시작한 마우스 좌표;
        Rectangle currentRect; //현재 그려지는 네모
        public PhotoCollection Photos;
        readonly List<TalkBoxLayer> _LstTalkBoxLayer = new List<TalkBoxLayer>();  // 소견 데이터
        double scaleX = 1;
        double scaleY = 1;
        TranslateTransform translate = new TranslateTransform();

        public TalkBoxLayer Last_talkBoxLayer = null; // 마지막 선택/작업 되었던 레이어
        public Image Last_image = null; // 마지막 선택/작업 되었던 이미지


  
        #endregion ######################### 선언 #########################

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

            DataContext = new MainViewModel(); // 좌측 상단 하단의 TEXT 변경 용
        }
        //private void TxtcutMemo_set(string upLabelText)
        //{
        //    TxtcutMemo.Text = upLabelText;//라벨의 텍스트 값 변경
        //}
        
        /// <summary>
        /// 우측소견로드 버튼 클릭시
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
            TxtcutMemo.Text = sb2.ToString();  // 우상단
            TxtFileTitle.Text = _FileTitle;
            #endregion ########## text 바인딩 E ##########
        }


        #endregion ######################### MainWin #########################

        

        #region ######## DB 키 생성 ########
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
        #endregion ######## DB 키 생성 ########

        private void reinit() {
            //_LstTalkBoxLayer.Clear();
            //_LstTalkBoxLayer = new List<TalkBoxLayer>();
            scaleX = 1;
            scaleY = 1;
        }

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
                this.root.ReleaseMouseCapture(); //마우스 캡춰를 제거한다.
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
                        string keyFilename = getKeyFileNameOnly(); //Helpers.keyFilename; //PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/");
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

                        Last_talkBoxLayer = _talkBoxLayer; //마지막 작업 레이어를 저장 하기 위해 ...
                        Last_image = image; //마지막 작업 이미지를 저장 하기 위해 ...
                        Helpers.ExportToPng(fullPath +"/"+ cutfileName, image, top, left);

                        #endregion ########## 사각형 안에 _talkLayer 삽입 end ##########
                        root.Children.Remove(currentRect); // 그려진 네모는 삭제 - obj 삭제 했더니 재사용이 안되 히든 및 null 처리
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
        /// 저장될 파일 경로
        /// </summary>
        /// <returns></returns>
        public string getSavePath() {
            //string _savePath = System.IO.Path.GetFileNameWithoutExtension(PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/"));
            string _savePath = getKeyWithPath();//PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/");
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

        #region ######################### 메인 사진 #########################
        #region ######### 소견삭제 #########
        /// <summary>
        /// 소견삭제
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		void OnDeleteButtonClick(object sender, RoutedEventArgs e)
        {
            SetDeleteAllTextBox();
        }

        /// <summary>
        /// 소견삭제
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
        #endregion ######### 소견삭제 #########

        #region ######### 소견저장 #########
        /// <summary>
        /// 소견 저장
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
        /// 레이어 빠저 나갈때 DB 저장
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
                        // 저장 성공
                    }
                }
            }
        }


        public void SetSaveAllTextBox(string _path = "")
        {
            string str_text = string.Empty;
            StringBuilder sb = new StringBuilder(); // 저장용(파일, 위치,크기 정보)
            StringBuilder sb2 = new StringBuilder(); // 시각용(위치,크기 정보 X)
            string FileTitle = "";
            for (int i = 0; i < _LstTalkBoxLayer.Count; i++)
            {
                if (_LstTalkBoxLayer[i].Text != null)
                {
                    if (FileTitle == "" ) { FileTitle = _LstTalkBoxLayer[i].TalkBoxLyerFileTitle; }
                    sb.Append(
                        _LstTalkBoxLayer[i].TalkBoxLyerkeyFilename.ToString() //PhotosListBox.SelectedItem.ToString().Replace("file:///", "").Replace("\\", "/")
                        + "▤" + _LstTalkBoxLayer[i].TalkBoxLyercutfileName.ToString()
                        + "▤" + _LstTalkBoxLayer[i].TalkBoxLyerCutFullPath.ToString()
                        + "▤" + _LstTalkBoxLayer[i].TalkBoxLyerFileNum.ToString()
                        + "▤" + _LstTalkBoxLayer[i].Text.ToString()
                        + "▤" + _LstTalkBoxLayer[i].TalkBoxLyerPointX
                        + "▤" + _LstTalkBoxLayer[i].TalkBoxLyerPointY
                        + "▤" + _LstTalkBoxLayer[i].TalkBoxLyerSizeW
                        + "▤" + _LstTalkBoxLayer[i].TalkBoxLyerSizeH
                        + "▥\r\n");
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
        #endregion ######### 소견저장 #########

        #region ######### 소견로드 #########
        /// <summary>
        /// 소견을 DB에서 로드한다
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
        /// 소견 data Load
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
            //#region ########## text 바인딩 S ##########
            //if (_str.Trim().Length == 0)
            //{
            //    //MessageBox.Show("소견이 없습니다.");
            //    return;
            //}
            //string[] _strArr = _str.Split('▥');
            //string _filePathWithName = string.Empty;    // 파일 명 추가
            //string _innerMemo = string.Empty;    // 글내용
            //string _TalkBoxLyerKeyFilename = "";
            //string _TalkBoxLyerFileTitle = "";
            //string _TalkBoxLyercutfileName = "";
            //string _TalkBoxLyerCutFullPath = "";
            //string _TalkBoxLyerFileNum = "";
            //StringBuilder sb2 = new StringBuilder();
            
            //for (int i = 0; i < _strArr.Length - 1; i++)
            //{
            //    string[] _strArr2 = _strArr[i].Split('▤');

            //    _filePathWithName    = _strArr2[0];
            //    _TalkBoxLyerFileTitle = _strArr2[1];
            //    _TalkBoxLyercutfileName = _strArr2[2];
            //    _TalkBoxLyerCutFullPath = _strArr2[3];
            //    _TalkBoxLyerFileNum  = _strArr2[4];
            //    _innerMemo = "";
            //    _innerMemo = _strArr2[5];   // 글내용 (의사소견)
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
            //#endregion ########## text 바인딩 E ##########
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
        #endregion ######### 소견로드 #########

        #endregion ######################### 메인 사진 #########################

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
            #endregion #### 로딩전 기존 소견을 지우고 현재 소견을 불러 온다 ####

        }

        /// <summary>
        /// DB에서 Data 를 조회 후 화면에 바인딩 
        /// </summary>
        public void LoadMemeFromDB()
        {
            TxtcutMemo.Text = "";  // 우상단
            TxtFileTitle.Text = "";
            //SetClearTalkBoxLayer();
            //string _key = Helpers.keyFilename;//_talkBoxLayer.TalkBoxLyerkeyFilename;
            #region ########## text 바인딩 S ##########
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

            string _KeyFilename = string.Empty;    // 파일 명 추가
            string _FileTitle = string.Empty;
            string _innerMemo = string.Empty;    // 글내용
            string _TalkBoxLyercutfileName = "";
            string _TalkBoxLyerCutFullPath = "";
            string _TalkBoxLyerFileNum = "";

            TalkBoxLayer talkBoxLayer_Last_Add =  null;

            //byte[] photo_aray; //DB에서 이미지를 불러온다
            StringBuilder sb2 = new StringBuilder();
            if (ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
            {
                _FileTitle = ds.Tables[0].Rows[0]["FileTitle"].ToString();
                
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    #region ################ 동일한 키 _TalkBoxLyercutfileName 가 있는지 확인 ####################
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
                    #endregion ################ 동일한 키 _TalkBoxLyercutfileName 가 있는지 확인 ####################

                    #region ################ 동일한 키 _TalkBoxLyercutfileName 이있을 경우 추가 하지 않음 ####################
                    if (!chk_existLayer)  
                    {
                        _KeyFilename            = ds.Tables[0].Rows[i]["KeyFilename"].ToString();
                        _TalkBoxLyercutfileName = ds.Tables[0].Rows[i]["CutFilename"].ToString();
                        _TalkBoxLyerCutFullPath = ds.Tables[0].Rows[i]["CutFullPath"].ToString();
                        _TalkBoxLyerFileNum     = ds.Tables[0].Rows[i]["numb"].ToString();
                        _innerMemo = "";
                        _innerMemo              = ds.Tables[0].Rows[i]["memo"].ToString();   // 글내용 (의사소견)
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
                    }
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
            TxtcutMemo.Text = sb2.ToString();  // 우상단
            TxtFileTitle.Text = _FileTitle;
            #endregion ########## text 바인딩 E ##########
            GC.Collect();
        }

        #region ######################### 좌측 트리에서 사진 #########################
        /// <summary>
        /// 좌측 트리에서 사진을 더블 클릭했을때
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPhotoDblClick(object sender, RoutedEventArgs e)
        {
            btnSaveText.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); //btnSaveText.PerformClick() in wpf
            btnDelText.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            TxtFileTitle.Text = string.Empty; // 소견 data 삭제
            TxtcutMemo.Text = string.Empty;  // 우상단

            ImageSource imageSource = new BitmapImage(new Uri(PhotosListBox.SelectedItem.ToString()));
            ViewedPhoto.Source = imageSource;
            //this.SizeToContent = SizeToContent.WidthAndHeight;
            //Grid.SetZIndex(this.ViewedPhoto, 99999);

            //reinit();   // 화면이 전환 되면 초기 값들 새로 시작!!!!
            //btnLoadText.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // 기존 저장된 정보가 있다면 로딩

            //RoutedEventArgs routedEventArgs = new RoutedEventArgs(ButtonBase.ClickEvent, btnLoadText);
            //btnLoadText.RaiseEvent(routedEventArgs);
            //new Action(() => btnLoadText.RaiseEvent(routedEventArgs)).SetTimeout(500);

            new Action(() => btnLoadText.RaiseEvent(new RoutedEventArgs(Button.ClickEvent))).SetTimeout(500);
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
