using DionisApp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace DionisApp
{
    public partial class Form1 : Form
    {
        private TcpClientWrapper tcpClient;

        public Form1()
        {
            InitializeComponent();
            InitializeTcpClient();
        }

        private void InitializeTcpClient()
        {
            try
            {
                tcpClient = new TcpClientWrapper("147.45.42.250", 12345);
                MessageBox.Show("Успешное подключение к серверу!");

                tcpClient.OnError += HandleError;
                tcpClient.StartReceiving();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
            }
        }

        private void HandleError(string errorMessage)
        {
            Invoke(new Action(() => MessageBox.Show(errorMessage, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error)));
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            tcpClient.Disconnect();
        }

        private void buttonSendScreenshot_Click(object sender, EventArgs e)
        {
            byte[] screenshotBytes = CaptureScreen();
            tcpClient.SendImage(screenshotBytes);
        }

        // Метод для захвата скриншота
        private byte[] CaptureScreen()
        {
            using (Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Jpeg);
                    return ms.ToArray();
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string message = richTextBox1.Text;
            tcpClient.SendMessage(message);
            richTextBox1.Text = "";
        }
    }
}
