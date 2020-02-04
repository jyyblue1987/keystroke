using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
namespace engine
{
    public class Engine
    {
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

        public static string takeScreenshot(int screenWidth, int screenHeight)
        {
            Bitmap memoryImage;

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
    }
}
