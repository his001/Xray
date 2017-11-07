using System;
using System.Windows;

namespace XrayTEXT
{
    /// <summary>
    /// ProgressDialog.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ProgressDialog : Window
    {
        public ProgressDialog()
        {
            InitializeComponent();

            Application curApp = Application.Current;
            Window mainWindow = curApp.MainWindow;
            this.Left = mainWindow.Left + ((mainWindow.ActualWidth - this.ActualWidth) / 2) - 200;
            this.Top = mainWindow.Top + ((mainWindow.ActualHeight - this.ActualHeight) / 2) - 200;
        }

        public string ProgressText
        {
            set
            {
                this.lblProgress.Content = value;
            }
        }

        public int ProgressValue
        {
            set
            {
                this.progress.Value = value;
            }
        }

        public event EventHandler Cancel = delegate { };

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Cancel(sender, e);
        }
    }
}
