using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DionisApp
{
    public class TcpClientWrapper
    {
        private TcpClient client;
        private NetworkStream stream;

        public event Action<string> OnMessageReceived;
        public event Action<string> OnImageReceived;
        public event Action<string> OnError;

        public TcpClientWrapper(string serverIp, int serverPort)
        {
            client = new TcpClient();
            client.Connect(serverIp, serverPort);
            stream = client.GetStream();
        }

        // Метод для отправки текста
        public void SendMessage(string message)
        {
            if (client == null || !client.Connected) return;

            byte[] messageBytes = Encoding.UTF8.GetBytes(message + "\n");
            byte[] typeByte = new byte[1] { (byte)'T' }; // Отправляем текст

            stream.Write(typeByte, 0, typeByte.Length);
            stream.Write(messageBytes, 0, messageBytes.Length);
        }

        // Метод для отправки изображения (например, скриншота)
        public void SendImage(byte[] imageBytes)
        {
            if (client == null || !client.Connected) return;

            byte[] typeByte = new byte[1] { (byte)'I' }; // Отправляем изображение
            stream.Write(typeByte, 0, typeByte.Length);

            // Отправляем размер изображения
            byte[] sizeBytes = BitConverter.GetBytes(imageBytes.Length);
            stream.Write(sizeBytes, 0, sizeBytes.Length);

            // Отправляем само изображение
            stream.Write(imageBytes, 0, imageBytes.Length);
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
                    if (bytesRead == 0) break;

                    if (dataType[0] == 'I') // Получаем изображение
                    {
                        byte[] sizeBuffer = new byte[4];
                        stream.Read(sizeBuffer, 0, sizeBuffer.Length);
                        int imageSize = BitConverter.ToInt32(sizeBuffer, 0);

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
                    else if (dataType[0] == 'T') // Получаем текст
                    {
                        StringBuilder messageBuilder = new StringBuilder();
                        while (true)
                        {
                            byte[] buffer = new byte[1024];
                            bytesRead = stream.Read(buffer, 0, buffer.Length);
                            if (bytesRead == 0) break;
                            messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                            if (messageBuilder.ToString().EndsWith("\n"))
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
            stream?.Close();
            client?.Close();
        }

        public void Disconnect()
        {
            Close();
        }
    }
}
