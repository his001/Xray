using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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

        // Invoked by the MenuItem in the ContextMenu.
        void OnDeleteAnnotation(object sender, RoutedEventArgs e)
        {
            this.Delete();
        }

        // Invoked 편집 모드 시작 시
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

        // Invoked 편집 모드 종료 시
        void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            // 초점 변경이 처음 완료 될 수 있도록 잠시 지연.			
            base.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                (NoArgDelegate)delegate
                {
                    this.IsInEditMode = false;
                    //MainWin.SetSaveAllTextBox();
                }
            );
        }

        // 사용자가 주석을 편집 할 때 
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

                    //MainWin mw = new MainWin();
                    //mw.Owner = Window.GetWindow(this);
                    //mw.btnSaveDBText.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));  // 엔터로 빠져나감
                    //MessageBox.Show(_talkBoxLayer.TalkBoxLyerFileNum + " : _talkBoxLayer.TalkBoxLyerFileNum ");
                    //mw.btnSaveDBText.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));  // 엔터로 빠져나감
                    // _talkBoxLayer
                    SelectDB();
                    SaveDB();

                }
            }
            else if (e.Key == Key.Escape)
            {
                //삭제되지 않은 경우 (텍스트가있는 경우) 편집 모드에서 빠져 나옵니다..
                if (!this.AttemptToDelete())
                {
                    this.IsInEditMode = false;
                }
            }
        }

        // Invoked 사용자가 디스플레이 모드에서 주석을 클릭할때
        void OnTextBlockMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.IsInEditMode = true;
        }

        #endregion // Event Handling Methods

        #region Private Helpers

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

        #endregion // Private Helpers

        public DataSet SelectDB() {
            DataSet ds = new DataSet();
            string rtn = "";

            try
            {
                string _text = string.Empty;
                string constr = @"Data Source=.\SQLEXPRESS;AttachDbFilename=|DataDirectory|\xraydb.mdf;Integrated Security=True;User Instance=True";
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();
                    string sql = "Select * From TBL_TalkBoxLayer";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        var adapt = new SqlDataAdapter();
                        adapt.SelectCommand = cmd;
                        adapt.Fill(ds);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return ds;
        }

        public string SaveDB()
        {
            string rtn = "";
            try
            {
                string _text = string.Empty;
                string constr = @"Data Source=.\SQLEXPRESS;AttachDbFilename=|DataDirectory|\xraydb.mdf;Integrated Security=True;User Instance=True";
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();
                    string sql = "insert into TBL_TalkBoxLayer(filename, file_title, numb, text, PointX, PointY, SizeW, SizeH, Fileimg) values ";
                    sql = sql + "('" + _talkBoxLayer.TalkBoxLyerFileName + "','" + _talkBoxLayer.Text.ToString() 
                        + "',(select count(*) from TBL_TalkBoxLayer with(nolock) where filename='" + _talkBoxLayer.TalkBoxLyerFileName + "' ),'" 
                        + _talkBoxLayer.Text + "','" + _talkBoxLayer.TalkBoxLyerPointX + "','" + _talkBoxLayer.TalkBoxLyerPointY 
                        + "','" + _talkBoxLayer.TalkBoxLyerSizeW + "','" + _talkBoxLayer.TalkBoxLyerSizeH + "',@Fileimg)";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add(new SqlParameter("Fileimg", _talkBoxLayer.TalkBoxLyerImg));
                        int result = cmd.ExecuteNonQuery();
                        if (result == 1)
                        {
                            //MessageBox.Show("Image Added");
                            //정상
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return rtn;
        }
    }
}