using System.Windows; // Ensure this namespace is included

namespace ChatClientWPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Add the missing event handler for the SendFileBtn_Click
        private void SendFileBtn_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder logic for handling the "Send File" button click
            MessageBox.Show("Send File button clicked!");
        }
    }
}