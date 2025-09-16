using System.Net.Sockets;
using System.Text;
using System.Windows; // Ensure this namespace is included
using System.Threading.Tasks;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32; // Add this at the top if not present
using System.Collections.ObjectModel;

namespace ChatClientWPF
{
    public partial class MainWindow : Window
    {
        TcpClient chatClient;
        NetworkStream chatStream;
        TcpClient fileClient;
        NetworkStream fileStream;
        string userName;
        public ObservableCollection<object> ChatItems { get; } = new ObservableCollection<object>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Nhập tên người dùng
            userName = Microsoft.VisualBasic.Interaction.InputBox(
                "Nhập tên của bạn:", "Login", "User");

            if (string.IsNullOrWhiteSpace(userName))
                userName = "Anonymous";

            ConnectToServer();
        }

        private void ConnectToServer()
        {
            try
            {
                // đổi IP này thành IP của máy chạy server
                chatClient = new TcpClient("192.168.1.20", 5000);
                chatStream = chatClient.GetStream();
                fileClient = new TcpClient("192.168.1.20", 5001);
                fileStream = fileClient.GetStream();

                Task.Run(() => ReceiveMessages());
                Task.Run(() => ReceiveFiles());
            }
            catch
            {
                MessageBox.Show("Không kết nối được đến server!");
                Application.Current.Shutdown();
            }
        }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[8192];
            try
            {
                while (true)
                {
                    string header = ReadLine(chatStream, buffer);
                    if (header == null) break;

                    Dispatcher.Invoke(() => ChatItems.Add(header));
                }
            }
            catch { }
        }

        private void ReceiveFiles()
        {
            byte[] buffer = new byte[8192];
            try
            {
                while (true)
                {
                    string header = ReadLine(fileStream, buffer);
                    if (header == null) break;
                    if (!header.StartsWith("FILE:")) continue;

                    var parts = header.Split(':');
                    string sender = parts[1];
                    string filename = parts[2];
                    long filesize = long.Parse(parts[3]);

                    var item = new FileTransferItem
                    {
                        Sender = sender,
                        FileName = filename,
                        TotalBytes = filesize,
                        TempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + Path.GetExtension(filename)),
                        IsImage = IsImageExtension(filename)
                    };
                    Dispatcher.Invoke(() => ChatItems.Add(item));

                    using (var fs = new FileStream(item.TempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, FileOptions.SequentialScan))
                    {
                        long received = 0;
                        while (received < filesize)
                        {
                            int toRead = (int)Math.Min(buffer.Length, filesize - received);
                            int read = fileStream.Read(buffer, 0, toRead);
                            if (read <= 0) break;
                            fs.Write(buffer, 0, read);
                            received += read;
                            var r = received; var total = filesize;
                            Dispatcher.Invoke(() => item.Progress = (int)(r * 100 / total));
                        }
                    }
                    Dispatcher.Invoke(() => { item.IsCompleted = true; item.StatusText = "Received"; });
                }
            }
            catch { }
        }

        private static string? ReadLine(NetworkStream stream, byte[] buffer)
        {
            using (var ms = new MemoryStream())
            {
                while (true)
                {
                    int b = stream.ReadByte();
                    if (b == -1) return null;
                    if (b == (byte)'\n') break;
                    ms.WriteByte((byte)b);
                    // Prevent unbounded memory in case of malformed input
                    if (ms.Length > 1024 * 1024) return null;
                }
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        private void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MessageInput.Text)) return;

            string msg = $"{userName}: {MessageInput.Text}\n";
            byte[] data = Encoding.UTF8.GetBytes(msg);
            chatStream.Write(data, 0, data.Length);

            ChatItems.Add("Me: " + MessageInput.Text);
            MessageInput.Clear();
        }

        private void EmojiBtn_Click(object sender, RoutedEventArgs e)
        {
            // Tạo menu emoji
            ContextMenu menu = new ContextMenu();

            string[] emojis = {
                "😊", "👍", "❤️", "😂", "😢", "😡", "🎉", "🙏", "😎", "🥰", "🤔", "🙌", "🔥", "👏", "😆", "😴"
            };

            foreach (var em in emojis)
            {
                MenuItem item = new MenuItem { Header = em };
                item.Click += (s, args) =>
                {
                    MessageInput.Text += em;
                    MessageInput.Focus();
                    MessageInput.CaretIndex = MessageInput.Text.Length;
                };
                menu.Items.Add(item);
            }

            // Hiển thị menu ngay chỗ nút
            menu.PlacementTarget = EmojiBtn;
            menu.IsOpen = true;
        }

        private void SendFileBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Chọn file để gửi";
            dialog.Filter = "All Files|*.*";
            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
                FileInfo fi = new FileInfo(filePath);
                string fileName = fi.Name;
                long fileSize = fi.Length;

                // Gửi header: FILE:username:filename:filesize\n
                string header = $"FILE:{userName}:{fileName}:{fileSize}\n";
                byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                Task.Run(() =>
                {
                    try
                    {
                        var item = new FileTransferItem
                        {
                            Sender = userName,
                            FileName = fileName,
                            TotalBytes = fileSize,
                            LocalPath = filePath,
                            IsImage = IsImageExtension(fileName),
                            StatusText = "Uploading"
                        };
                        Dispatcher.Invoke(() => ChatItems.Add(item));

                        fileStream.Write(headerBytes, 0, headerBytes.Length);
                        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.SequentialScan))
                        {
                            byte[] buffer = new byte[8192];
                            int bytesRead;
                            long sent = 0;
                            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                fileStream.Write(buffer, 0, bytesRead);
                                sent += bytesRead;
                                var s = sent; var total = fileSize;
                                Dispatcher.Invoke(() => item.Progress = (int)(s * 100 / total));
                            }
                        }
                        Dispatcher.Invoke(() => { item.IsCompleted = true; item.StatusText = "Uploaded"; });
                    }
                    catch
                    {
                        Dispatcher.Invoke(() => ChatItems.Add($"Gửi file thất bại: {fileName}"));
                    }
                });
            }
        }

        private bool IsImageExtension(string filename)
        {
            var ext = Path.GetExtension(filename).ToLowerInvariant();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".bmp" || ext == ".webp";
        }

        private void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is FileTransferItem item && item.IsCompleted)
            {
                var sfd = new SaveFileDialog();
                sfd.FileName = item.FileName;
                if (sfd.ShowDialog() == true)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(item.TempPath) && File.Exists(item.TempPath))
                        {
                            File.Copy(item.TempPath, sfd.FileName, true);
                        }
                        else if (!string.IsNullOrEmpty(item.LocalPath) && File.Exists(item.LocalPath))
                        {
                            File.Copy(item.LocalPath, sfd.FileName, true);
                        }
                        else
                        {
                            MessageBox.Show("File không tồn tại tạm thời để lưu.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi lưu file: " + ex.Message);
                    }
                }
            }
        }
    }
}
