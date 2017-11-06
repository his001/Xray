using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace XrayTEXT
{
    /// <summary>
    /// MemoCRUDWin.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MemoCRUDWin : Window
    {
        //DataTable dt;
        List<CL_BCode> lstemp = new List<CL_BCode>();

        public MemoCRUDWin()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(MemoCRUDWin_Loaded);
        }

        void MemoCRUDWin_Loaded(object sender, RoutedEventArgs e)
        {
            FillGrid();
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
                    string sql = "Select BCode, BName, BMemo ";
                    sql = sql + " From TBL_B_Code with(nolock) ";
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
            if (ds != null) {
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++) {

                    Int32 _BCode = Int32.Parse(ds.Tables[0].Rows[i]["BCode"].ToString());
                    string _BName = ds.Tables[0].Rows[i]["BName"].ToString();
                    string _BMemo = ds.Tables[0].Rows[i]["BMemo"].ToString();

                    lstemp.Add(new CL_BCode() { BCode = _BCode, BName = _BName, BMemo = _BMemo });
                }
            }
            return lstemp;
        }

        private void FillGrid()
        {
            lstemp = GetB_CODE();
            Dtgrid1.ItemsSource = lstemp;
        }

        private void Dtgrid1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object item = Dtgrid1.SelectedItem;
            DataTable dt1 = new DataTable();
            string _ID = (Dtgrid1.SelectedCells[0].Column.GetCellContent(item) as TextBlock).Text;

            using (SqlConnection sqlcon = new SqlConnection(Helpers.dbCon))
            {
                sqlcon.Open();
                SqlDataAdapter sqladpsc = new SqlDataAdapter("Select * from  TBL_B_Code where BCode=" + _ID, sqlcon);
                sqladpsc.Fill(dt1);
                if (dt1.Rows.Count > 0)
                {
                    TxtBName.Text = dt1.DefaultView[0]["BName"].ToString();
                    TxtBMemo.Text = dt1.DefaultView[0]["BMemo"].ToString();
                    Cmd_Save.Content = "Update";
                }
                sqlcon.Close();
            }
            
        }

        private void Cmd_Save_Click(object sender, RoutedEventArgs e)
        {
            using (SqlConnection sqlcon = new SqlConnection(Helpers.dbCon))
            {
                if (Cmd_Save.Content == "Update")
                {
                    sqlcon.Open();
                    SqlCommand sqlcmd = new SqlCommand("Update TBL_B_Code set BName='" + TxtBName.Text + "',BMemo='" + TxtBName.Text + "' where BCode=" + TxtBName.Text, sqlcon);
                    sqlcmd.ExecuteNonQuery();
                }
                else if (Cmd_Save.Content == "Save")
                {
                    sqlcon.Open();
                    SqlCommand sqlcmd = new SqlCommand("Insert into TBL_B_Code values('" + TxtBName.Text + "','" + TxtBName.Text + "')", sqlcon);
                    sqlcmd.ExecuteNonQuery();
                }
                else if (Cmd_Save.Content == "Delete")
                {
                    sqlcon.Open();
                    SqlCommand sqlcmd = new SqlCommand("Delete  From TBL_B_Code where BCode=" + TxtBName.Text, sqlcon);
                    sqlcmd.ExecuteNonQuery();
                }
            }
            FillGrid();
        }

    }

    public class CL_BCode {
        private Int32 _BCode;
        private string _BMemo;
        private string _BName;

        public Int32 BCode
        {
            get { return _BCode; }
            set { _BCode = value; }
        }
        public string BMemo
        {
            get { return _BMemo; }
            set { _BMemo = value; }
        }
        public string BName
        {
            get { return _BName; }
            set { _BName = value; }
        }
    }
}
