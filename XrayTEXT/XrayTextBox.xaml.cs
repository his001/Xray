using System;
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

        //MainWin ParentForm;
        //public TalkBoxLayerControl(MainWin form)
        //{
        //    this.ParentForm = form;
        //    InitializeComponent();
        //}

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
                    //btnSaveText
                    //Parent.SetCurrentValue.;
                    //ParentForm.SetSaveAllTextBox();
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
				}
            }
            else if (e.Key == Key.Escape)
            {
                //�������� ���� ��� (�ؽ�Ʈ���ִ� ���) ���� ��忡�� ���� ���ɴϴ�..
                if (!this.AttemptToDelete())
                    this.IsInEditMode = false;
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
    }
}