using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32; // Add this at the top if not present

namespace ChatClientWPF
{
    public partial class MainWindow : Window
    {
        TcpClient chatClient;
        NetworkStream chatStream;
        TcpClient fileClient;
        NetworkStream fileStream;
        string userName;

        public MainWindow()
        {
            InitializeComponent();

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

                    Dispatcher.Invoke(() => ChatBox.Items.Add(header));
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

                    Dispatcher.Invoke(() => ChatBox.Items.Add($"{sender} is sending file: {filename} ({filesize} bytes)"));

                    string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), filename);
                    using (var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, FileOptions.SequentialScan))
                    {
                        long received = 0;
                        while (received < filesize)
                        {
                            int toRead = (int)Math.Min(buffer.Length, filesize - received);
                            int read = fileStream.Read(buffer, 0, toRead);
                            if (read <= 0) break;
                            fs.Write(buffer, 0, read);
                            received += read;
                        }
                    }
                    Dispatcher.Invoke(() => ChatBox.Items.Add($"Received file: {filename} (saved to Desktop)"));
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

            ChatBox.Items.Add("Me: " + MessageInput.Text);
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
                        fileStream.Write(headerBytes, 0, headerBytes.Length);
                        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.SequentialScan))
                        {
                            byte[] buffer = new byte[8192];
                            int bytesRead;
                            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                fileStream.Write(buffer, 0, bytesRead);
                            }
                        }
                        Dispatcher.Invoke(() => ChatBox.Items.Add($"Đã gửi file: {fileName} ({fileSize} bytes)"));
                    }
                    catch
                    {
                        Dispatcher.Invoke(() => ChatBox.Items.Add($"Gửi file thất bại: {fileName}"));
                    }
                });
            }
        }
    }
}
