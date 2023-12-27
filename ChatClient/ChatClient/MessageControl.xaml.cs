using ChatData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
    public partial class MessageControl : UserControl
    {
        string dirPath = string.Empty;
        ChatMessage message = null;
        public MessageControl(ChatMessage message, string dirPath)
        {
            InitializeComponent();
            this.message = message;
            this.dirPath = dirPath;
            ustbMessage.Text = message.TextMessage;
            ustbNick.Content = message.From;
            ustbCreatedAT.Content = message.CreatedAT.ToString();
            usbtLoad.Visibility = string.IsNullOrEmpty(message.FileName) ? Visibility.Collapsed : Visibility.Visible;

            if (usbtLoad.Visibility == Visibility.Visible)
            {
                using (MemoryStream ms = new MemoryStream(message.FileData))
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = ms;
                    bitmapImage.EndInit();
                    pictureBox1.Source = bitmapImage;
                }
            }
        }

        private void usbtLoad_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (message.FileData == null) return;
            using (FileStream fs = new FileStream(dirPath + message.FileName, FileMode.Create))
            {
                fs.Write(message.FileData, 0, message.FileData.Length);
            }
        }
    }
}
