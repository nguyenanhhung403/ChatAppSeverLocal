using System.Configuration;
using System.Data;
using System.Windows;

namespace ChatClientWPF
{
    public class FileTransferItem : System.ComponentModel.INotifyPropertyChanged
    {
        public string Sender { get; set; }
        public string FileName { get; set; }
        public long TotalBytes { get; set; }
        public string TempPath { get; set; }
        public string LocalPath { get; set; }
        public bool IsImage { get; set; }

        int _progress;
        public int Progress { get => _progress; set { _progress = value; OnPropertyChanged(nameof(Progress)); } }

        bool _isCompleted;
        public bool IsCompleted { get => _isCompleted; set { _isCompleted = value; OnPropertyChanged(nameof(IsCompleted)); } }

        string _statusText = "";
        public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(nameof(StatusText)); } }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}

namespace ChatClientWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
    }

}
