using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Threading.Tasks;

namespace ChatClientWPF
{
    public partial class MainWindow : Window
    {
        TcpClient client;
        NetworkStream stream;
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
                client = new TcpClient("192.168.1.14", 5000);
                stream = client.GetStream();

                Task.Run(() => ReceiveMessages());
            }
            catch
            {
                MessageBox.Show("Không kết nối được đến server!");
                Application.Current.Shutdown();
            }
        }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            int bytesRead;
            try
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Dispatcher.Invoke(() => ChatBox.Items.Add(msg));
                }
            }
            catch { }
        }

        private void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MessageInput.Text)) return;

            string msg = $"{userName}: {MessageInput.Text}";
            byte[] data = Encoding.UTF8.GetBytes(msg);
            stream.Write(data, 0, data.Length);

            ChatBox.Items.Add("Me: " + MessageInput.Text);
            MessageInput.Clear();
        }
        private void EmojiBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageInput.Text += "😊";
            MessageInput.Focus();
            MessageInput.CaretIndex = MessageInput.Text.Length;
        }
    }
}
