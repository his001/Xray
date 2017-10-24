using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace XrayTEXT.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region #### PropertyChangedEventHandler ####
        private string _userFileMemo;
        private string _userCutMemo;

        public string UserCutMemo
        {
            get { return _userCutMemo; }
            set
            {
                _userCutMemo = value;
                OnPropertyChanged("UserCutMemo");
            }
        }

        public string UserFileMemo
        {
            get { return _userFileMemo; }
            set
            {
                _userFileMemo = value;
                OnPropertyChanged("UserFileMemo");
            }
        }
        private void NotifyPropertyChanged(String info)
        {
            var listeners = PropertyChanged;
            if (listeners != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

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
}
