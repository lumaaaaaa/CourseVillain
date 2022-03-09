using HtmlAgilityPack;
using RestSharp;
using System;
using System.IO;
using System.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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
            htmlWeb.UserAgent = "Mozilla/5.0 (Windows NT 10.0; rv:91.0) Gecko/20100101 Firefox/91.0";
            htmlWeb.UseCookies = true;
            htmlWeb.PreRequest += request =>
            {
                var headers = request.Headers;
                headers.Add("Accept-Language", "en-US,en;q=0.5");
                headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
                return true;
            };
            HtmlDocument doc = htmlWeb.Load(url);
            if (doc.Text.Contains("Incapsula incident ID"))
            {
                Console.WriteLine("[X] Complete the captcha on site in your browser then try again.");
                Thread.Sleep(5000);
                Environment.Exit(42);
            }
            string filehash = Between(doc.Text, "ppt-entry-point\" data-filehash=\"", "\" data-ppt-position=\"document\"");
            string datarsid = Between(doc.Text, "preloadImages\":[\"/doc-asset/bg/" + filehash +"/splits/", "/split-0-page-1-html-bg.jpg\"],");
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
                    
                    using Image<Rgba32> image = Image.Load<Rgba32>("out/" + dirname + "/" + "page" + i + ".jpg");
                    using Image<Rgba32> image2 = Image.Load<Rgba32>("out/" + dirname + "/" + "page" + i + "-2.jpg");
                    Rgba32[] pixelArray = new Rgba32[image.Width * image.Height];
                    image.CopyPixelDataTo(pixelArray);
                    Rgba32[] pixelArray2 = new Rgba32[image2.Width * image2.Height];
                    Rgba32[] outArray = new Rgba32[image2.Width * image2.Height];
                    image2.CopyPixelDataTo(pixelArray2);
                    for (int j = 0; j < pixelArray.Length; j++)
                    {
                        if (pixelArray[j].R == pixelArray2[j].R)
                        {
                            outArray[j] = pixelArray[j];
                        }
                        else
                        {
                            if (pixelArray[j].R > pixelArray2[j].R)
                            {
                                outArray[j] = pixelArray2[j];
                            }
                            else
                            {
                                outArray[j] = pixelArray[j];
                            }
                        }
                    }
                    using (var img = Image.LoadPixelData(outArray, image2.Width, image2.Height))
                    {
                        File.Delete("out/" + dirname + "/" + "page" + i + "-2.jpg");
                        File.Delete("out/" + dirname + "/" + "page" + i + ".jpg");
                        img.Save("out/" + dirname + "/" + "page" + i + ".jpg");
                        Console.WriteLine("[i] Merged 'page" + i + ".jpg'");
                    }
                }
                else
                {
                    File.WriteAllBytes("out/" + dirname + "/" + "page" + i + ".jpg", dlresp);
                    Console.WriteLine("[i] Downloaded 'page" + i + ".jpg'");
                }
                
            }
            
            Console.WriteLine("[i] Finished downloading " + pagecount + " pages! Saved to '" + di.FullName + "'");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
