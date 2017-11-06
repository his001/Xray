using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace XrayTEXT
{
    /// <summary>
    /// Interaction logic for MemoSearchWin.xaml
    /// </summary>
    public partial class MemoSearchWin : Window
    {
        List<CL_BCode> lstemp = new List<CL_BCode>();
        public string selectedText;
        public MemoSearchWin()
        {
            InitializeComponent();
            //textBox1.AddItem(new AutoCompleteEntry("결핵", null));
            //textBox1.AddItem(new AutoCompleteEntry("C형 간염", null));

            lstemp.Clear();
            lstemp = GetB_CODE();
            if (lstemp.Count > 0) {
                for (int i = 0; i < lstemp.Count; i++) {
                    textBox1.AddItem(new AutoCompleteEntry(lstemp[i].BName, lstemp[i].BMemo));
                }
            }
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            //App.Current.MainWindow.Focus();
            //textBox1.Focus();

            Keyboard.Focus(textBox1);
            textBox1.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));  // 포커스 주기가 이렇게 힘들줄이야 ㅠㅠ

        }

        private void btn_clear_Click(object sender, RoutedEventArgs e)
        {
            textBox1.Text = string.Empty;
        }

        private void btn_accept_Click(object sender, RoutedEventArgs e)
        {
            selectedText = textBox1.Text;
            this.Close();
        }

        public List<CL_BCode> GetB_CODE()
        {
            #region ########## 바인딩 S ##########
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection conn = new SqlConnection(Helpers.dbCon))
                {
                    conn.Open();
                    string sql = "SELECT BCode, BName, BMemo ";
                    sql = sql + " FROM TBL_B_Code WITH(NOLOCK) ";
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

            #endregion ########## 바인딩 E ##########
            if (ds != null)
            {
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {

                    Int32 _BCode = Int32.Parse(ds.Tables[0].Rows[i]["BCode"].ToString());
                    string _BName = ds.Tables[0].Rows[i]["BName"].ToString();
                    string _BMemo = ds.Tables[0].Rows[i]["BMemo"].ToString();

                    lstemp.Add(new CL_BCode() { BCode = _BCode, BName = _BName, BMemo = _BMemo });
                }
            }
            return lstemp;
        }
    }

    public class AutoCompleteEntry
    {
        private string[] keywordStrings;
        private string displayString;

        public string[] KeywordStrings
        {
            get
            {
                if (keywordStrings == null)
                {
                    keywordStrings = new string[] { displayString };
                }
                return keywordStrings;
            }
        }

        public string DisplayName
        {
            get { return displayString; }
            set { displayString = value; }
        }

        public AutoCompleteEntry(string name, params string[] keywords)
        {
            displayString = name;
            keywordStrings = keywords;
        }

        public override string ToString()
        {
            return displayString;
        }
    }
}
