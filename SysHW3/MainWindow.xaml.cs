using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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

namespace SysHW3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CancellationTokenSource cancellation;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void FileOpenBtn_Click(object sender, RoutedEventArgs e)
        {
            FileDialog dialog = new OpenFileDialog();
            dialog.Filter = "txt files |*.txt";

            var result = dialog.ShowDialog();
            if (result == true)
                FilePathtxtbox.Text = dialog.FileName;

        }

        private void EncryptDecryptBtn_Click(object sender, RoutedEventArgs e)
        {
            Progressbar.Value = 0;

            cancellation = new CancellationTokenSource();

            if (Encryptrbtn.IsChecked == true)
                Encrypt(cancellation.Token);

            else if (Decryptrbtn.IsChecked == true)
                Decrypt(cancellation.Token);

        }

        private void Encrypt(CancellationToken token)
        {
            var text = File.ReadAllText(FilePathtxtbox.Text);
            var key = Encoding.UTF8.GetBytes(Passwordtxtbox.Password);

            var bytesToWrite = EncryptStringToBytes(text, key, key);
            var fs = new FileStream(FilePathtxtbox.Text, FileMode.Truncate);

            ThreadPool.QueueUserWorkItem(o =>
            {
                for (int i = 0; i < bytesToWrite.Length; i++)
                {
                    if (i % 32 == 0)
                    {
                        if (token.IsCancellationRequested)
                        {
                            fs.Dispose();
                            Dispatcher.Invoke(() => File.WriteAllText(FilePathtxtbox.Text, text));
                            Dispatcher.Invoke(() => Progressbar.Value = 0);
                            return;
                        }

                        Thread.Sleep(500);
                        if (i != 0)
                            Dispatcher.Invoke(() => Progressbar.Value = 100 * i / bytesToWrite.Length);
                    }
                    fs.WriteByte(bytesToWrite[i]);
                }

                fs.Seek(0, SeekOrigin.Begin);

                Dispatcher.Invoke(() => Progressbar.Value = 100);
            });
        }

        private void Decrypt(CancellationToken token)
        {
            var bytes = File.ReadAllBytes(FilePathtxtbox.Text);

            var key = Encoding.UTF8.GetBytes(Passwordtxtbox.Password);

            var text = DecryptStringFromBytes(bytes, key, key);
            var bytesToWrite = Encoding.UTF8.GetBytes(text);

            StartBtn.IsEnabled = false;
            CancelBtn.IsEnabled = true;

            ThreadPool.QueueUserWorkItem(o =>
            {
                using var fs = new FileStream(FilePathtxtbox.Text, FileMode.Truncate);

                for (int i = 0; i < bytesToWrite.Length; i++)
                {
                    if (i % 32 == 0)
                    {
                        if (token.IsCancellationRequested)
                        {
                            fs.Dispose();
                            Dispatcher.Invoke(() => File.WriteAllBytes(FilePathtxtbox.Text, bytes));
                            Dispatcher.Invoke(() => Progressbar.Value = 0);
                            return;
                        }

                        Thread.Sleep(500);
                        if (i != 0)
                            Dispatcher.Invoke(() => Progressbar.Value = 100 * i / bytesToWrite.Length);
                    }
                    fs.WriteByte(bytesToWrite[i]);
                }

                fs.Seek(0, SeekOrigin.Begin);

                Dispatcher.Invoke(() => Progressbar.Value = 100);
            });
        }

        private static byte[] EncryptStringToBytes(string original, byte[] key, byte[] IV)
        {
            byte[] encrypted;
            using (var encryption = Aes.Create())
            {
                encryption.Key = key;
                encryption.IV = IV;


                ICryptoTransform encryptor = encryption.CreateEncryptor(encryption.Key, encryption.IV);

                using var msEncrypt = new MemoryStream();
                using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

                using (var swEncrypt = new StreamWriter(csEncrypt))
                    swEncrypt.Write(original);

                encrypted = msEncrypt.ToArray();
            }

            return encrypted;
        }

        private static string DecryptStringFromBytes(byte[] encrypted, byte[] key, byte[] IV)
        {
            string plaintext = string.Empty;

            using (var encryption = Aes.Create())
            {
                encryption.Key = key;
                encryption.IV = IV;

                ICryptoTransform decryptor = encryption.CreateDecryptor(encryption.Key, encryption.IV);

                using MemoryStream msDecrypt = new MemoryStream(encrypted);
                using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    plaintext = srDecrypt.ReadToEnd();
                }
            }

            return plaintext;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            cancellation.Cancel();
        }
    }
}
