using HtmlAgilityPack;
using RestSharp;
using System;
using System.IO;

namespace CourseVillain
{
    class Program
    {
        public static string Between(string STR, string FirstString, string LastString)
        {
            string FinalString;
            int Pos1 = STR.IndexOf(FirstString) + FirstString.Length;
            int Pos2 = STR.IndexOf(LastString);
            FinalString = STR.Substring(Pos1, Pos2 - Pos1);
            return FinalString;
        }

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("[i] CourseVillain - v0.1");
            Console.Write("[?] Enter the URL of the document: ");
            string url = Console.ReadLine();
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument doc = htmlWeb.Load(url);
            string filehash = Between(doc.Text, "ppt-entry-point\" data-filehash=\"", "\" data-ppt-position=\"document\"");
            string datarsid = Between(doc.Text, "/splits/", "/split-0-page-1-html-bg.jpg);background-position:0 0&quot;}}");
            string dirname = Between(doc.Text, "<title>", "</title>").Trim().Replace(" ", "_").Replace("|", "-").Replace(".", "-").Replace(":", "-").Replace("@", "-");
            DirectoryInfo di = Directory.CreateDirectory("out/" + dirname);
            string pagecount;
            if (doc.Text.Contains("Want to read the entire page?"))
                pagecount = "1";
            else
                pagecount = Between(doc.Text, "</strong> out of <strong>", "</strong> pages.");
            Console.WriteLine("[i] Found " + pagecount + " pages... downloading...");
            //download logic
            var download = new RestClient("https://www.coursehero.com/doc-asset/bg/" + filehash + "/splits/" + datarsid + "/");
            int splitct = 0;
            for(int i = 1; i <= Convert.ToInt32(pagecount); i++)
            {
                var dlreq = new RestRequest("split-" + splitct + "-page-" + i + ".jpg", Method.GET);
                byte[] dlresp = download.DownloadData(dlreq);
                if(System.Text.Encoding.Default.GetString(dlresp) == "{\"error\":{\"message\": \"Unknown error occurred\"}}")
                {
                    splitct++;
                    i--;
                    var secondarydlreq = new RestRequest("split-" + splitct + "-page-" + i + ".jpg", Method.GET);
                    byte[] secondarydlresp = download.DownloadData(secondarydlreq);
                    File.WriteAllBytes("out/" + dirname + "/" + "page" + i + "-2.jpg", secondarydlresp);
                    Console.WriteLine("[i] Downloaded 'page" + i + "-2.jpg'");
                }
                else
                {
                    File.WriteAllBytes("out/" + dirname + "/" + "page" + i + ".jpg", dlresp);
                    Console.WriteLine("[i] Downloaded 'page" + i + ".jpg'");
                }
                
            }
            Console.WriteLine("[i] Finished downloading " + pagecount + " pages! Saved to " + di.FullName);
            Console.WriteLine("--------------\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
