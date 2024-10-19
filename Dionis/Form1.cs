using System;
using System.Drawing; // Не забудьте добавить эту директиву для работы с изображениями
using System.IO; // Для работы с файлами
using System.Windows.Forms;

namespace Dionis
{
    public partial class Form1 : Form
    {
        private TcpClientWrapper tcpClient;

        // Объявляем параметры подключения к прокси и серверу
        private string proxyIp;
        private int proxyPort;
        private string username = "B3us57G2"; // Логин
        private string password = "3mpRRj3q";   // Пароль

        public Form1()
        {
            InitializeComponent();
            InitializeTcpClient();
        }

        private void InitializeTcpClient()
        {
            // Инициализируем параметры подключения
            proxyIp = "185.246.220.176";
            proxyPort = 64412;

            try
            {
                // Создаем и подключаем TcpClientWrapper
                tcpClient = new TcpClientWrapper(proxyIp, proxyPort, username, password);
                MessageBox.Show("Успешное подключение к прокси!");

                // Подписываемся на события
                tcpClient.OnMessageReceived += DisplayMessage;
                tcpClient.OnImageReceived += HandleImageReceived;
                tcpClient.OnError += HandleError;

                // Запускаем получение данных
                tcpClient.StartReceiving();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Обработка инициализации после загрузки формы, если необходимо
        }

        private void DisplayMessage(string message)
        {
            // Обновляем интерфейс с полученным сообщением
            Invoke(new Action(() => textBoxMessage.Text += $"[INFO] Ответ от сервера: {message}\n"));
        }

        private void HandleImageReceived(string imagePath)
        {
            // Обработка полученного изображения
            MessageBox.Show($"Изображение получено и сохранено в {imagePath}");

            // Загружаем изображение и отображаем его в PictureBox
            try
            {
                // Загрузка изображения
                var image = Image.FromFile(imagePath);

                // Обновляем PictureBox
                Invoke(new Action(() =>
                {
                    pictureBox.Image = image; // pictureBox1 - имя вашего PictureBox
                    pictureBox.SizeMode = PictureBoxSizeMode.StretchImage; // Обеспечиваем корректный масштаб
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HandleError(string errorMessage)
        {
            // Обработка ошибок
            Invoke(new Action(() => MessageBox.Show(errorMessage, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error)));
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Убедимся, что клиент отключен корректно
            tcpClient.Disconnect();
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {

        }
    }
}
