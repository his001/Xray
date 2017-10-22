using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace XrayTEXT
{
    /// <summary>
    /// This class describes a single photo - its location, the image and 
    /// the metadata extracted from the image.
    /// </summary>
    public class Photo
    {
        public Photo(string path)
        {
            _path = path;
            _source = new Uri(path);
            try
            {
                _image = BitmapFrame.Create(_source);
            }
            catch (NotSupportedException)
            {
            }
            
            //_metadata = new ExifMetadata(_source);
            //_metadata = null;
        }

        public override string ToString()
        {
            return _source.ToString();
        }

        private string _path;

        private Uri _source;
        public string Source { get { return _path; } }

        private BitmapFrame _image;
        public BitmapFrame Image { get { return _image; } set { _image = value; } }

    }

    /// <summary>
    /// This class represents a collection of photos in a directory.
    /// </summary>
    public class PhotoCollection : ObservableCollection<Photo>
    {
        public PhotoCollection() { }

        public PhotoCollection(string path) : this(new DirectoryInfo(path)) { }

        public PhotoCollection(DirectoryInfo directory)
        {
            _directory = directory;
            GetImageRead();
        }

        public string Path
        {
            set
            {
                _directory = new DirectoryInfo(value);
                GetImageRead();
            }
            get { return _directory.FullName; }
        }

        public DirectoryInfo Directory
        {
            set
            {
                _directory = value;
                GetImageRead();
            }
            get { return _directory; }
        }

        private void GetImageRead()
        {
            this.Clear();
            try
            {
                foreach (FileInfo f in
                    //_directory.GetFiles("*.jpg")
                    _directory.GetFiles("*.jpg").Union(_directory.GetFiles("*.png"))
                    )
                {
                    Add(new Photo(f.FullName));
                }
            }
            catch (DirectoryNotFoundException)
            {
                System.Windows.MessageBox.Show("No Such Directory");
            }
        }

        DirectoryInfo _directory;
    }

}
