using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;

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

    public class TreeViewFile
    {
        #region ###############################################
        private readonly string _path;

        public TreeViewFile(string path)
        {
            _path = path;
        }

        public string FullPath
        {
            get { return _path; }
        }

        public string FileName
        {
            get
            {
                var dirInfo = new DirectoryInfo(FullPath);
                return dirInfo.Name;
            }
        }

        public string ImagePath
        {
            get
            {
                var fileInfo = new FileInfo(FullPath);
                var fileExt = fileInfo.Extension.TrimStart('.');
                var imagePath = string.Format(MainWin.UidProperty.Name, fileExt);

                return imagePath;
            }
        }

        public ObservableCollection<TreeViewFile> SubFolders
        {
            get
            {
                try
                {
                    var subFolders = Directory.GetDirectories(_path);
                    var treeViewSubFolders = new ObservableCollection<TreeViewFile>();
                    subFolders.ToList().ForEach(c => { if (c != null) treeViewSubFolders.Add(new TreeViewFile(c)); });

                    return treeViewSubFolders;
                }
                catch (Exception)
                {
                    return new ObservableCollection<TreeViewFile>();
                }
            }
        }

        public ObservableCollection<TreeViewFile> Files
        {
            get
            {
                try
                {
                    var files = Directory.GetFiles(_path);
                    var treeViewFiles = new ObservableCollection<TreeViewFile>();
                    files.ToList().ForEach(c => treeViewFiles.Add(new TreeViewFile(c)));
                    return treeViewFiles;
                }
                catch (Exception)
                {
                    return new ObservableCollection<TreeViewFile>();
                }
            }
        }

        #endregion ###############################################

    }
}
