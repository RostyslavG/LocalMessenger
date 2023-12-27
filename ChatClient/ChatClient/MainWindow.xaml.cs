using ChatData;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
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

namespace ChatClient
{
    public partial class MainWindow : Window
    {
        TcpClient socket = null;
        string path = string.Empty;
        string dirPath = string.Empty;
        BinaryFormatter formatter = null;
        NetworkStream networkStream = null;
        public MainWindow()
        {
            InitializeComponent();
            cbUsers.Items.Add("");
            formatter = new BinaryFormatter();
            dirPath = Directory.GetCurrentDirectory();
            dirPath += "\\data\\";
            Directory.CreateDirectory(dirPath);
            btLogout.IsEnabled = false;
            btLogout.Foreground = Brushes.DarkGray;
            btSelect.Foreground = Brushes.DarkGray;
            tbbtSend.Foreground = Brushes.DarkGray;
            btSelect.IsEnabled = false;
            btSend.IsEnabled = false;
            tbMessage.IsEnabled = false;
        }

        private async Task ReceiveResponses()
        {
            try
            {
                while (true)
                {
                    if (!socket.Connected) break;
                    StreamReader reader = new StreamReader(networkStream, Encoding.UTF8);
                    ChatMessage cdata = (ChatMessage)formatter.Deserialize(reader.BaseStream);
                    if (cdata != null && !string.IsNullOrEmpty(cdata.TextMessage))
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            MessageControl response = new MessageControl(cdata, dirPath);
                            if (cdata.Command.Equals("Login") && !cdata.From.Equals(tbNick.Text.Trim()))
                                cbUsers.Items.Add(cdata.From);
                            else if (cdata.Command.Equals("Logout"))
                                cbUsers.Items.Remove(cdata.From);
                            pnChat.Children.Add(response);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Receive message: " + ex.Message);
            }
        }

        private byte[] GetFileContent()
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return null;
                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    BinaryReader reader = new BinaryReader(fs);
                    byte[] buffer = reader.ReadBytes((int)reader.BaseStream.Length);
                    reader.Close();
                    path = string.Empty;
                    return buffer;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Get file content: " + ex.Message);
                return null;
            }
        }

        private bool SendMessage(ChatMessage message)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    formatter.Serialize(ms, message);
                    networkStream.Write(ms.ToArray(), 0, (int)ms.Length);
                    networkStream.Flush();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Send message: " + ex.Message);
                return false;
            }
        }

        private void btLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                socket = new TcpClient();
                socket.Connect(tbAddress.Text.Trim(), Convert.ToInt32(tbPort.Text.Trim()));
                networkStream = socket.GetStream();

                ChatMessage message = new ChatMessage()
                {
                    From = tbNick.Text.Trim(),
                    To = string.Empty,
                    Command = "Login",
                    TextMessage = string.Empty,
                    FileName = string.Empty,
                    FileData = null,
                    CreatedAT = DateTime.Now,
                };

                if (!SendMessage(message)) return;
                StreamReader sr = new StreamReader(networkStream, Encoding.UTF8);

                ChatMessage response = (ChatMessage)formatter.Deserialize(sr.BaseStream);
                MemoryStream ms = new MemoryStream(response.FileData);
                List<string> users = (List<string>)formatter.Deserialize(ms);
                ms.Close();
                foreach (string user in users)
                    cbUsers.Items.Add(user);
                cbUsers.SelectedIndex = 0;
                Task.Run(() => ReceiveResponses());

                btLogin.IsEnabled = false;
                btLogout.IsEnabled = true;
                btSelect.IsEnabled = true;
                tbMessage.IsEnabled = true;
                btLogin.Foreground = Brushes.DarkGray;
                btLogout.Foreground = Brushes.Black;
                btSelect.Foreground = Brushes.Black;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Login: " + ex.Message);
            }
        }

        private void btLogout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChatMessage logoutMessage = new ChatMessage()
                {
                    From = tbNick.Text.Trim(),
                    Command = "Logout",
                    TextMessage = string.Empty,
                    FileName = string.Empty,
                    FileData = null,
                    CreatedAT = DateTime.Now,
                };
                if (!SendMessage(logoutMessage)) return;

                cbUsers.Items.Remove(tbNick.Text.Trim());
                pnChat.Children.Clear();

                btLogin.IsEnabled = true;
                btLogout.IsEnabled = false;
                btSelect.IsEnabled = false;
                tbMessage.IsEnabled = false;
                btLogout.Foreground = Brushes.DarkGray;
                btSelect.Foreground = Brushes.DarkGray;
                tbbtSend.Foreground = Brushes.DarkGray;
                btLogin.Foreground = Brushes.Black;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Logout: " + ex.Message);
            }
        }

        private void btSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
                path = ofd.FileName;
        }

        private void btSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChatMessage message = new ChatMessage()
                {
                    From = tbNick.Text.Trim(),
                    To = string.Empty,
                    Command = "Message",
                    TextMessage = tbMessage.Text.Trim(),
                    FileName = path == string.Empty ? "" : System.IO.Path.GetFileName(path),
                    FileData = GetFileContent(),
                    CreatedAT = DateTime.Now,
                };

                string selectedUser = cbUsers.SelectedItem.ToString();
                if (!string.IsNullOrEmpty(selectedUser)) message.To = selectedUser;
                if (!SendMessage(message)) return;
                tbMessage.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Send message: " + ex.Message);
            }
        }

        private void tbMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
           btSend.IsEnabled = !string.IsNullOrEmpty(tbMessage.Text);
           tbbtSend.Foreground = Brushes.Black;
        }
    }
}
