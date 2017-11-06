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
            GetBindData();
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
                    sql = sql + " From TBL_B_Code WITH(NOLOCK) ";
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

        private void GetBindData()
        {
            lstemp.Clear();
            lstemp = GetB_CODE();
            Dtgrid1.ItemsSource = null;
            Dtgrid1.ItemsSource = lstemp;

            for (int i = 0; i < Dtgrid1.Columns.Count; i++)
            {
                if (i == 0)
                {
                    Dtgrid1.Columns[i].Width = new DataGridLength(80, DataGridLengthUnitType.Pixel);
                }
                else if (i == 1)
                {
                    Dtgrid1.Columns[i].Width = new DataGridLength(260, DataGridLengthUnitType.Pixel);
                }
                else if (i == 2)
                {
                    Dtgrid1.Columns[i].Width = new DataGridLength(148, DataGridLengthUnitType.Pixel);
                }
                //else
                //{
                //    Dtgrid1.Columns[i].Width = new DataGridLength(Dtgrid1.ActualWidth / Dtgrid1.Columns.Count, DataGridLengthUnitType.Pixel);
                //}
                Dtgrid1.Columns[i].IsReadOnly = true;
            }
            TxtBCode.Text = GetMaxNumb();
        }

        private void Dtgrid1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                object item = Dtgrid1.SelectedItem;
                DataTable dt1 = new DataTable();
                string _ID = (Dtgrid1.SelectedCells[0].Column.GetCellContent(item) as TextBlock).Text;
                if (_ID == null || _ID == "")
                {
                    TxtBCode.Text = GetMaxNumb();
                    TxtBName.Text = "";
                    TxtBMemo.Text = "";
                    Cmd_Save.Content = "추가";
                    return;
                }

                using (SqlConnection sqlcon = new SqlConnection(Helpers.dbCon))
                {
                    sqlcon.Open();
                    SqlDataAdapter sqladpsc = new SqlDataAdapter("SELECT BCode, BName, BMemo FROM TBL_B_Code WITH(NOLOCK) WHERE BCode=" + _ID, sqlcon);
                    sqladpsc.Fill(dt1);
                    if (dt1.Rows.Count > 0)
                    {
                        TxtBCode.Text = dt1.DefaultView[0]["BCode"].ToString();
                        TxtBName.Text = dt1.DefaultView[0]["BName"].ToString();
                        TxtBMemo.Text = dt1.DefaultView[0]["BMemo"].ToString();
                        Cmd_Save.Content = "수정";
                    }
                    sqlcon.Close();
                }
            }
            catch (Exception ex) { }
        }

        private void Cmd_Load_Click(object sender, RoutedEventArgs e)
        {
            GetBindData();
        }

        private void Cmd_Save_Click(object sender, RoutedEventArgs e)
        {
            using (SqlConnection sqlcon = new SqlConnection(Helpers.dbCon))
            {
                sqlcon.Open();
                if (Cmd_Save.Content.ToString() == "수정")
                {
                    SqlCommand sqlcmd = new SqlCommand("UPDATE TBL_B_Code SET BName='" + Helpers.rtnSQLInj(TxtBName.Text) + "',BMemo='" + Helpers.rtnSQLInj(TxtBMemo.Text) + "' WHERE BCode=" + TxtBCode.Text, sqlcon);
                    sqlcmd.ExecuteNonQuery();
                    sqlcon.Close();
                    MessageBox.Show("수정 되었습니다.");
                }
                else if (Cmd_Save.Content.ToString() == "추가")
                {
                    SqlCommand sqlcmd = new SqlCommand("INSERT INTO TBL_B_Code (BName, BMemo) VALUES ('" + Helpers.rtnSQLInj(TxtBName.Text) + "','" + Helpers.rtnSQLInj(TxtBMemo.Text) + "')", sqlcon);
                    sqlcmd.ExecuteNonQuery();
                    sqlcon.Close();
                    MessageBox.Show("추가 되었습니다.");
                }
            }
            GetBindData();
        }

        private void Cmd_Del_Click(object sender, RoutedEventArgs e)
        {
            using (SqlConnection sqlcon = new SqlConnection(Helpers.dbCon))
            {
                sqlcon.Open();
                string _ID = TxtBCode.Text;
                if (_ID != "" && _ID != null)
                {
                    SqlCommand sqlcmd = new SqlCommand("DELETE FROM TBL_B_Code WHERE BCode = " + _ID, sqlcon);
                    sqlcmd.ExecuteNonQuery();
                    sqlcon.Close();
                    MessageBox.Show("삭제 되었습니다.");
                }
                sqlcon.Close();
            }
            GetBindData();
        }

        private string GetMaxNumb() {

            DataTable dt1 = new DataTable();
            string _rtn = string.Empty;
            using (SqlConnection sqlcon = new SqlConnection(Helpers.dbCon))
            {
                sqlcon.Open();
                SqlDataAdapter sqladpsc = new SqlDataAdapter(" SELECT ISNULL(MAX(BCode), 0) + 1 as NextBCode FROM TBL_B_Code WITH(NOLOCK) ", sqlcon);
                sqladpsc.Fill(dt1);
                if (dt1.Rows.Count > 0)
                {
                    _rtn = dt1.Rows[0]["NextBCode"].ToString();
                }
                sqlcon.Close();
            }
            return _rtn;
        }

    }
}
