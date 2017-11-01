using System.Windows;

namespace XrayTEXT
{
    /// <summary>
    /// Interaction logic for MemoSearchWin.xaml
    /// </summary>
    public partial class MemoSearchWin : Window
    {
        public MemoSearchWin()
        {
            InitializeComponent();
            //textBox1.AddItem(new AutoCompleteEntry("Toyota Camry", "Toyota Camry", "camry", "car", "sedan"));
            //textBox1.AddItem(new AutoCompleteEntry("Toyota Corolla", "Toyota Corolla", "corolla", "car", "compact"));
            //textBox1.AddItem(new AutoCompleteEntry("Toyota Tundra", "Toyota Tundra", "tundra", "truck"));
            //textBox1.AddItem(new AutoCompleteEntry("Chevy Impala", null));  // null matching string will default with just the name
            //textBox1.AddItem(new AutoCompleteEntry("Chevy Tahoe", "Chevy Tahoe", "tahoe", "truck", "SUV"));
            //textBox1.AddItem(new AutoCompleteEntry("Chevrolet Malibu", "Chevrolet Malibu", "malibu", "car", "sedan"));
            textBox1.AddItem(new AutoCompleteEntry("결핵", null));
            textBox1.AddItem(new AutoCompleteEntry("간염", null));
            textBox1.AddItem(new AutoCompleteEntry("A형 결핵", null));
            textBox1.AddItem(new AutoCompleteEntry("A형 간염", null));
            textBox1.AddItem(new AutoCompleteEntry("B형 결핵", null));
            textBox1.AddItem(new AutoCompleteEntry("B형 간염", null));
            textBox1.AddItem(new AutoCompleteEntry("C형 결핵", null));
            textBox1.AddItem(new AutoCompleteEntry("C형 간염", null));

        }

        private void btn_clear_Click(object sender, RoutedEventArgs e)
        {
            textBox1.Text = string.Empty;
        }

        private void btn_accept_Click(object sender, RoutedEventArgs e)
        {
            textBox1.Text = string.Empty;
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
