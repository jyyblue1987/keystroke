using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace WebUtils
{
    public class WebHttpUtils
    {
        public static string getResponse(String url, string cookie, string data)
        {
            HttpWebRequest wReq;
            HttpWebResponse wRes;

            try
            {
                Uri uri = new Uri(url); 
                wReq = (HttpWebRequest)WebRequest.Create(uri); 
                wReq.Method = "GET";
                wReq.ServicePoint.Expect100Continue = false;
                wReq.CookieContainer = new CookieContainer();
                wReq.CookieContainer.SetCookies(uri, cookie); 

                string resResult;
                using (wRes = (HttpWebResponse)wReq.GetResponse())
                {
                    Stream respPostStream = wRes.GetResponseStream();
                    StreamReader readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("UTF-8"), true);

                    resResult = readerPost.ReadToEnd();
                }

                return resResult;
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    var resp = (HttpWebResponse)ex.Response;
                    if (resp.StatusCode == HttpStatusCode.NotFound)
                    {
                       
                    }
                    else
                    {
                       
                    }
                }
                else
                {
                    
                }

                return "";
            }
        }
    }
}
