using System;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace maimaidx_prober_gui
{
    public partial class Form1 : Form
    {
        string endl = "\r\n";

        public class Config //json类
        {
             public string username { get; set; }
             public string password { get; set; }
             public Boolean slice { get; set; }
             public int timeout { get; set; }
             public string [] mai_diffs { get; set; }
             public Boolean is_ui_first_load { get; set; }
        }
        public Config json_config;//内存中的json数据存储变量
        public Config json_read()//统一的读取json函数
        {

            string json = File.ReadAllText("config.json");
            Config temp = JsonConvert.DeserializeObject<Config>(json);
            return temp;
        }
        public void json_write(Config temp)//统一的写入json函数
        {
             string json = JsonConvert.SerializeObject(temp);
             File.WriteAllText(@"config.json", json);
             return;
        }
        public void config_read() //读取config.json文件，如不存在则创建
        {

            try
            {
                FileStream f = new FileStream("config.json", FileMode.Open, FileAccess.Read, FileShare.Read);
                f.Close();
            }
            catch (FileNotFoundException)
            {
                Config temp = new Config();
                temp.username = "请填入查分器用户名";
                temp.password = "";
                temp.timeout = 120;
                temp.is_ui_first_load = true;
                temp.mai_diffs = new string[5];
                json_write(temp);
            }
            finally
            {
                json_config = json_read();
            }
            return;
        }
        public void Base64StringToFile(string base64String, string path)//base64转文件
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(base64String)))
                {
                    using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        byte[] b = stream.ToArray();
                        fs.Write(b, 0, b.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public Form1()
        {
            InitializeComponent();
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
        }

        private void button3_Click(object sender, EventArgs e) //软件退出
        {
            StopProxyProcess();
            System.Diagnostics.Process tt = System.Diagnostics.Process.GetProcessById(System.Diagnostics.Process.GetCurrentProcess().Id);
            tt.Kill();
        }
        public void first_load()//首次加载的初始化
        {
            byte[] cert_byte;
            cert_byte = new byte[maimaidx_prober_gui.Properties.Resources.cert.Length];
            maimaidx_prober_gui.Properties.Resources.cert.CopyTo(cert_byte, 0);
            FileStream fs = new FileStream("cert.crt", FileMode.Create, FileAccess.Write);
            textbox_addline("20%");
            fs.Write(cert_byte, 0, cert_byte.Length);
            fs.Close();
            
            textbox_addline("40%");
            cert_install();
            
            byte[] key_byte;
            key_byte = new byte[maimaidx_prober_gui.Properties.Resources.key.Length];
            maimaidx_prober_gui.Properties.Resources.key.CopyTo(key_byte, 0);
            fs = new FileStream("key.pem", FileMode.Create, FileAccess.Write);
            fs.Write(key_byte, 0, key_byte.Length);
            fs.Close();

            textbox_addline("60%");
            byte[] proxy_byte;
            proxy_byte = new byte[maimaidx_prober_gui.Properties.Resources.proxy.Length];
            maimaidx_prober_gui.Properties.Resources.proxy.CopyTo(proxy_byte, 0);
            fs = new FileStream("proxy.exe", FileMode.Create, FileAccess.Write);
            textbox_addline("80%");
            fs.Write(proxy_byte, 0, proxy_byte.Length);
            fs.Close();
            textbox_addline("100%");
            MessageBox.Show("程序初次加载完成");
            textbox_addline("成功");
        }
        public void cert_install()//证书自动化安装
        {
            string certPath = @"cert.crt";   
            try
            {
                X509Certificate2 cert = new X509Certificate2(Path.GetFullPath(certPath));
                X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                store.Add(cert);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error: {e.Message}");
            }
            return;
        }
    
        private void Form1_Load(object sender, EventArgs e)
        {
            config_read();
            if (json_config.is_ui_first_load)
            {
                textbox_addline("第一次启动，请等待程序初始化");
                ThreadStart threadStart = new ThreadStart(first_load);
                Thread temp_thread = new Thread(threadStart);
                temp_thread.Start();
                json_config.is_ui_first_load = false;
                json_write(json_config);
            }
            textBox2.Text = json_config.username;
            textBox3.Text = json_config.password;
            textBox4.Text = json_config.timeout.ToString();
            checkBox6.Checked = json_config.slice;
            read_level();
        }
        public void textbox_addline(string temp)
        {
            richTextBox1.Text += temp+endl;
            return;
        }

        private Process process;
        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button5.Enabled = false;
            button4.Enabled = true;
            process = new Process();
            process.StartInfo.FileName = "proxy.exe"; // 设置控制台程序的路径
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true; // 重定向标准错误流
            process.StartInfo.CreateNoWindow = true; // 不创建窗口
            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived; // 处理标准错误流
            process.EnableRaisingEvents = true;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine(); // 开始异步读取标准错误流
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                string decodedText = DecodeGbkToUtf8(e.Data);
                UpdateRichTextBox(decodedText);
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                string decodedText = DecodeGbkToUtf8(e.Data);
                UpdateRichTextBox(decodedText);
            }
        }

        private string DecodeGbkToUtf8(string gbkText)
        {
            Encoding gbk = Encoding.GetEncoding("GBK");
            byte[] gbkBytes = gbk.GetBytes(gbkText);
            byte[] utf8Bytes = Encoding.Convert(gbk, Encoding.UTF8, gbkBytes);
            return Encoding.UTF8.GetString(utf8Bytes);
        }

        private void UpdateRichTextBox(string text)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action<string>(UpdateRichTextBox), text);
            }
            else
            {
                byte[] bytes = Encoding.Default.GetBytes(text); // 将文本编码为字节数组
                string decodedText = Encoding.UTF8.GetString(bytes); // 将字节数组解码为字符串
                richTextBox1.AppendText(decodedText + Environment.NewLine);
                richTextBox1.ScrollToCaret();
            }
        }

        public void get_level()//实现导入难度设置的保存
        {
            json_config.mai_diffs = new string[5];
            if (checkBox1.Checked) json_config.mai_diffs[0] = "bas";
            else json_config.mai_diffs[0] = "";
            if (checkBox2.Checked) json_config.mai_diffs[1] = "adv";
            else json_config.mai_diffs[1] = "";
            if (checkBox3.Checked) json_config.mai_diffs[2] = "exp";
            else json_config.mai_diffs[2] = "";
            if (checkBox4.Checked) json_config.mai_diffs[3] = "mas";
            else json_config.mai_diffs[3] = "";
            if (checkBox5.Checked) json_config.mai_diffs[4] = "rem";
            else json_config.mai_diffs[4] = "";
            return;
        }
        public void read_level()//实现导入难度设置的读取
        {
            if (json_config.mai_diffs[0] == "bas") checkBox1.Checked = true;
            else checkBox1.Checked = false;
            if (json_config.mai_diffs[1] == "adv") checkBox2.Checked = true;
            else checkBox2.Checked = false;
            if (json_config.mai_diffs[2] == "exp") checkBox3.Checked = true;
            else checkBox3.Checked = false;
            if (json_config.mai_diffs[3] == "mas") checkBox4.Checked = true;
            else checkBox4.Checked = false;
            if (json_config.mai_diffs[4] == "rem") checkBox5.Checked = true;
            else checkBox5.Checked = false;
            return;
        }
        public void save_settings()
        {
            ThreadStart threadStart = new ThreadStart(get_level);
            Thread temp_thread = new Thread(threadStart);
            temp_thread.Start();
            json_config.username = textBox2.Text;
            json_config.password = textBox3.Text;
            json_config.timeout = int.Parse(textBox4.Text);
            json_config.slice = checkBox6.Checked;
            json_write(json_config);
            MessageBox.Show("保存成功，软件将自动重启");
            return;
        }

        private void StopProxyProcess()
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
                process = null;
            }
            closeProxy();
        }
        private void button5_Click(object sender, EventArgs e)
        {
            save_settings();
            Application.ExitThread();
            Application.Exit();
            Application.Restart();
            Process.GetCurrentProcess().Kill();
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("开发中~");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            button4.Enabled = false;
            button1.Enabled = true;
            button5.Enabled = true;

            if (process != null && !process.HasExited)
            {
                process.Kill();
                process = null;
            }
            closeProxy();
        }
        
        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        private const int INTERNET_OPTION_REFRESH = 37;

        public static bool closeProxy()
        {
            const string userRoot = "HKEY_CURRENT_USER";
            const string subkey = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
            const string keyName = userRoot + @"\" + subkey;
            Registry.SetValue(keyName, "ProxyEnable","0", RegistryValueKind.DWord);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
            return (true);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.Show();
        }
    }
}
