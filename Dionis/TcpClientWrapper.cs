using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Dionis
{
    public class TcpClientWrapper
    {
        private TcpClient client;
        private NetworkStream stream;

        public event Action<string> OnMessageReceived;
        public event Action<string> OnImageReceived;
        public event Action<string> OnError;

        // Добавляем поля для логина и пароля
        private string username;
        private string password;

        public TcpClientWrapper(string proxyIp, int proxyPort, string username, string password)
        {
            client = new TcpClient();
            client.Connect(proxyIp, proxyPort);
            stream = client.GetStream();

            this.username = username;
            this.password = password;

            // После успешного подключения к прокси, отправляем команду CONNECT
            SendConnectCommand();
        }

        private void SendConnectCommand()
        {
            // Формируем команду на соединение с целевым сервером
            string connectCommand = "CONNECT 147.45.42.250:12345 HTTP/1.1\r\n" +
                                    "Host: 147.45.42.250:12345\r\n" +
                                    "Proxy-Authorization: Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")) + "\r\n" +
                                    "\r\n";

            byte[] commandBytes = Encoding.UTF8.GetBytes(connectCommand);
            stream.Write(commandBytes, 0, commandBytes.Length);

            // Проверяем ответ от прокси
            ReadProxyResponse();
        }

        private void ReadProxyResponse()
        {
            StringBuilder responseBuilder = new StringBuilder();
            byte[] buffer = new byte[1024];
            int bytesRead;

            // Читаем ответ прокси
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                responseBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                // Прекращаем чтение, если находим двойной перевод строки, означающий конец заголовков
                if (responseBuilder.ToString().EndsWith("\r\n\r\n"))
                {
                    break;
                }
            }

            // Проверяем, успешен ли ответ
            if (!responseBuilder.ToString().Contains("200 Connection established"))
            {
                OnError?.Invoke("Не удалось установить соединение с сервером через прокси.");
                throw new Exception("Не удалось установить соединение с сервером через прокси.");
            }
            else
            {
                Console.WriteLine("Соединение с сервером установлено успешно.");
            }
        }

        public void SendMessage(string message)
        {
            if (client == null || !client.Connected) return;

            byte[] messageBytes = Encoding.UTF8.GetBytes(message + "\n");
            byte[] typeByte = new byte[1] { (byte)'T' }; // Отправляем текст

            stream.Write(typeByte, 0, typeByte.Length);
            stream.Write(messageBytes, 0, messageBytes.Length);
        }

        public void StartReceiving()
        {
            var receiveThread = new Thread(ReceiveData);
            receiveThread.Start();
        }

        private void ReceiveData()
        {
            try
            {
                while (true)
                {
                    byte[] dataType = new byte[1];
                    int bytesRead = stream.Read(dataType, 0, dataType.Length);
                    if (bytesRead == 0) break; // Прерываем при разрыве соединения

                    if (dataType[0] == 'I') // Если тип данных 'I', ожидаем изображение
                    {
                        // Получаем размер изображения
                        byte[] sizeBuffer = new byte[4];
                        stream.Read(sizeBuffer, 0, sizeBuffer.Length);
                        int imageSize = BitConverter.ToInt32(sizeBuffer, 0);

                        // Читаем само изображение
                        byte[] imageData = new byte[imageSize];
                        int totalBytesRead = 0;
                        while (totalBytesRead < imageSize)
                        {
                            bytesRead = stream.Read(imageData, totalBytesRead, imageSize - totalBytesRead);
                            totalBytesRead += bytesRead;
                        }

                        string imagePath = $"received_image_{DateTime.Now.Ticks}.jpg";
                        File.WriteAllBytes(imagePath, imageData);
                        OnImageReceived?.Invoke(imagePath);
                    }
                    else if (dataType[0] == 'T') // Если тип данных 'T', ожидаем текст
                    {
                        StringBuilder messageBuilder = new StringBuilder();
                        while (true)
                        {
                            byte[] buffer = new byte[1024];
                            bytesRead = stream.Read(buffer, 0, buffer.Length);
                            if (bytesRead == 0) break; // Прерываем при разрыве соединения
                            messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                            if (messageBuilder.ToString().EndsWith("\n")) // Ожидаем конца сообщения
                            {
                                break;
                            }
                        }

                        string message = messageBuilder.ToString().TrimEnd('\n');
                        OnMessageReceived?.Invoke(message);
                    }
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Ошибка получения данных: {ex.Message}");
            }
        }

        public void Close()
        {
            if (client != null)
            {
                stream?.Close();
                client?.Close();
            }
        }

        public void Disconnect()
        {
            Close();
        }
    }
}
