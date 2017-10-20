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
    /// �̹��� �ּ��� �������մϴ�. �� ��Ʈ�ѿ��� IsInEditMode ���°� �ֽ��ϴ�.
    /// ����ڰ� �ּ��� ���� �� �� �ִ��� ���θ� �����մϴ�.
    /// IsInEditMode�� true�̸� �ּ��� TextBox�� ǥ�õ˴ϴ�.
    /// �ּ��� TextBlock�� ǥ�õǸ� false�Դϴ�.
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
            
            // TalkBoxLayer�� �ؽ�Ʈ ���̿� ���ε� ���� �� Content �Ӽ��� ���� �� �ٸ� �Ӽ��� ������Ʈ
            Binding binding = new Binding("Text");
			binding.Source = _talkBoxLayer;
			binding.Mode = BindingMode.TwoWay;
			this.SetBinding(ContentControl.ContentProperty, binding);

            //�� ��Ʈ�ѿ� ��Ŀ�� ���.
            //�츮�� ��Ŀ���� capture �� �� �ֵ��� �ּ��� �����ϴ� �� ���Ǵ� TextBox.
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
        /// ����ڰ� �ؽ�Ʈ�� ���� �� �� �ִ��� ����
        /// </summary>
        public bool IsInEditMode
        {
            get { return (bool)GetValue(IsInEditModeProperty); }
            set { SetValue(IsInEditModeProperty, value); }
        }

        /// <summary>
        /// InEditMode �Ӽ�
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
                // TalkBoxLayerControl ���� TextBox���� ��Ŀ���� �ָ��ΰ�,
                // �׸��� TalkBoxLayerControl ��ü�� ������ �� ��� ��.
                if (control.IsKeyboardFocusWithin)
                    control.Focus();

                // ����ڰ� �ּ��� �����Ϸ��� �õ�
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

        // Invoked ���� ��� ���� ��
        void OnTextBoxLoaded(object sender, RoutedEventArgs e)
        {
            TextBox txt = sender as TextBox;
            txt.Focus();

            // ����ڰ� ���� �ּ��� �����ϴ� ���,
            // ���ڸ� Ŭ���ϰ� �ű⿡ ĳ���� �ֽ��ϴ�..
            int charIdx = txt.GetCharacterIndexFromPoint(Mouse.GetPosition(txt), true);
            if (charIdx > -1)
                txt.SelectionStart = charIdx;
        }

        // Invoked ���� ��� ���� ��
        void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            // ���� ������ ó�� �Ϸ� �� �� �ֵ��� ��� ����.			
            base.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                (NoArgDelegate)delegate
                {
                    this.IsInEditMode = false;
                    //MainWin.SetSaveAllTextBox();
                }
            );
        }

        // ����ڰ� �ּ��� ���� �� �� 
        void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
				if (Keyboard.Modifiers == ModifierKeys.Shift)
				{
                    // Shift + Enter �� �����ǵ�.
                    TextBox txt = sender as TextBox;
					txt.AppendText(Environment.NewLine);
					++txt.SelectionStart;
				}
				else
				{
					this.IsInEditMode = false;

                    //MainWin mw = new MainWin();
                    //mw.Owner = Window.GetWindow(this);
                    //mw.btnSaveDBText.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));  // ���ͷ� ��������
                    //MessageBox.Show(_talkBoxLayer.TalkBoxLyerFileNum + " : _talkBoxLayer.TalkBoxLyerFileNum ");
                    //mw.btnSaveDBText.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));  // ���ͷ� ��������
                    // _talkBoxLayer
                    SelectDB();
                    SaveDB();

                }
            }
            else if (e.Key == Key.Escape)
            {
                //�������� ���� ��� (�ؽ�Ʈ���ִ� ���) ���� ��忡�� ���� ���ɴϴ�..
                if (!this.AttemptToDelete())
                {
                    this.IsInEditMode = false;
                }
            }
        }

        // Invoked ����ڰ� ���÷��� ��忡�� �ּ��� Ŭ���Ҷ�
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
                            //����
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