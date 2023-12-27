using ChatData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChatServer
{
    public partial class MainWindow : Window
    {
        TcpListener listener = null;
        SemaphoreSlim semaphore = new SemaphoreSlim(5, 10);
        static List<ChatUser> users = new List<ChatUser>();
        BinaryFormatter formatter = null;
        public MainWindow()
        {
            InitializeComponent();
            formatter = new BinaryFormatter();
        }

        private async Task WaitForClient()
        {
            while (true)
            {
                await semaphore.WaitAsync();
                _= Task.Run(() => Listen());
            }
        }

        private async Task Listen()
        {
            try
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                NetworkStream network = client.GetStream();
                StreamReader reader = null;

                while (true)
                {
                    if (!network.DataAvailable) continue;
                    reader = new StreamReader(network, Encoding.UTF8);
                    ChatMessage message = (ChatMessage)formatter.Deserialize(reader.BaseStream);

                    switch (message.Command)
                    {
                        case "Login":
                            if (users.Any(u => u.Nick.Equals(message.From)) || string.IsNullOrEmpty(message.From))
                            {
                                await SendMessage(message, client);
                            }
                            else
                            {
                                users.Add(new ChatUser() { Nick = message.From, Client = client });
                                Dispatcher.Invoke(() => lbUsers.Items.Add(message.From));

                                using (MemoryStream ms = new MemoryStream())
                                {
                                    formatter.Serialize(ms, users.Select(u => u.Nick).ToList());
                                    ChatMessage toSend = new ChatMessage()
                                    {
                                        Command = "Login",
                                        TextMessage = message.From + " joined chat",
                                        FileName = string.Empty,
                                        FileData = ms.ToArray(),
                                        To = "All",
                                        CreatedAT = DateTime.Now,
                                        From = message.From
                                    };
                                    SendGroupMessage(toSend);
                                }
                            }
                            break;

                        case "Logout":
                            ChatMessage logoutMessage = new ChatMessage()
                            {
                                Command = "Logout",
                                TextMessage = message.From + " left chat",
                                FileName = string.Empty,
                                FileData = null,
                                To = "All",
                                CreatedAT = DateTime.Now,
                                From = message.From
                            };
                            SendGroupMessage(logoutMessage);
                            semaphore.Release();

                            var userToRemove = users.FirstOrDefault(u => u.Nick == message.From);
                            if (userToRemove != null)
                            {
                                users.Remove(userToRemove);
                                Dispatcher.Invoke(() => lbUsers.Items.Remove(message.From));
                            }
                            break;

                        case "Message":
                            Dispatcher.Invoke(() => tbResponse.Text += message.From + " > " + message.TextMessage + "\r\n");
                            if (string.IsNullOrEmpty(message.To))
                                SendGroupMessage(message);
                            else
                                SendPrivateMessage(message);
                            break;
                        case "Refresh":
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Listen: " + ex.Message);
            }
        }

        private void SendPrivateMessage(ChatMessage message)
        {
            TcpClient clientSocket = users.FirstOrDefault(u => u.Nick == message.To)?.Client;
            _= Task.Run(() => SendMessage(message, clientSocket));
        }

        private void SendGroupMessage(ChatMessage toSend)
        {
            foreach (var user in users)
            {
                _= Task.Run(() => SendMessage(toSend, user.Client));
            }
        }

        private async Task SendMessage(ChatMessage toSend, TcpClient socket)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    formatter.Serialize(ms, toSend);
                    NetworkStream network = socket.GetStream();
                    await network.WriteAsync(ms.ToArray(), 0, (int)ms.Length);
                    await network.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Send message: " + ex.Message);
            }
        }

        private async void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string address = tbAddress.Text.Trim();
                if (string.IsNullOrEmpty(address))
                {
                    var host = Dns.GetHostAddresses(Dns.GetHostName());
                    foreach (var ip in host)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                        {
                            address = ip.ToString();
                            break;
                        }
                    }
                }
                Dispatcher.Invoke(() =>
                {
                    status.Text = "On air...";
                    status.Foreground = Brushes.Green;
                });
                listener = new TcpListener(IPAddress.Parse(address), Convert.ToInt32(tbPort.Text.Trim()));
                listener.Start();
                await WaitForClient();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Listen click: " + ex.Message);
            }
        }
    }
}

