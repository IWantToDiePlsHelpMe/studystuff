using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebAnalyser
{
    internal class Programm
    {
        private static WebSite WebSite { get; set; } =
        new WebSite(new WebClient { BaseAddress = "https://www.susu.ru/ru/structure" }, 2);

        public static void Main()
        {
            Menu();
            /*
            Console.WriteLine(WebSite.GetAddress());
            WebSite.Nesting = 10;
            WebSite.Scanning += webPage =>
            {
                Console.Clear();
                Console.WriteLine(webPage);
            };
            WebSite.ScanPage();
            */
        }

    private static void Menu()
    {
            Console.WriteLine(WebSite.GetAddress());
            Console.WriteLine("1. Scan \n2. Nesting lvl  \n3. Save \n4. New address \n -----");
            switch ( Input("", "^[1-6]$") )
        {
                case "1":
                    WebSite.Scanning += webPage =>
                    {
                        Console.Clear();
                        Console.WriteLine(webPage);
                    };
                    WebSite.ScanPage();
                    break;
                case "2":
                    WebSite.Nesting = Convert.ToInt32(Input("New nesting level: ","^[0-9]{1,9}$"));
                    break;
                case "3":
                    try
                    {
                        StreamWriter Csv = new StreamWriter(Input("File name: ", "") + ".csv");
                        Csv.Write(WebSite.Csv);
                        Csv.Flush();
                        Csv.Close();
                    }
                    catch
                    {
                        break;
                    }
                    break;
                case "4":
                    WebSite.SetAddress(Input("New base address: ","^https?://.*$"));
                    break;
                case "5": default: return;
        }
        Menu();
    }


    private static string Input(string value, string pattern)
    {
        string input;
        do
        {
            Console.WriteLine(value);
            input = Console.ReadLine();
            Console.Clear();
        }  while (!Regex.IsMatch(input, pattern));
        return input;
    }
}

internal class WebSite
    {
        private WebPage BaseWebPage { get; set; }
        private WebClient WebClient { get; set; }
        public int Nesting { get; set; }
        public string Csv { get; set; } = null;

        public WebSite(WebClient client, int nesting)
        {
            BaseWebPage = new WebPage(client, client.BaseAddress);
            WebClient = client;
            Nesting = nesting;
        }

        public void SetAddress(string address)
        {
            BaseWebPage = new WebPage(WebClient, address);
            WebClient.BaseAddress = address;
            Csv = null;
        }

        public string GetAddress()
        {
            return WebClient.BaseAddress;
        }

        public void ScanPage()
        {
            Csv = null;
            ScanPage(BaseWebPage);
        }
        private void ScanPage(WebPage Page)
        {
            for (int i = 0; i < Page.Nesting - BaseWebPage.Nesting; i++)
            {
                Csv += "|-- ";
            }
            Csv += $"{Page.Title}; {Page.Host + Page.Path}; {Page.Nesting - BaseWebPage.Nesting};";

            //// в делегат вывода собсна
            OnScanning(Csv);

            if (Page.Nesting - BaseWebPage.Nesting >= Nesting) return;

            foreach (string Href in Page.Hrefs)
            {
                try
                {
                    Csv += '\n';
                    ScanPage(new WebPage(WebClient, Page.Host + Page.Path + Href));
                }
                catch { }
            }

        }

        public event Action<string> Scanning;
        private void OnScanning(string webPage)
        {
            Scanning?.Invoke(webPage);
        }

    }

    internal class WebPage
    {
        /// Page information
        public string Host { get; } = null;
        public string Path { get; } = null;
        public int Nesting { get; } = -3;
        public string Title { get; } = null;

        /// References
        public List<string> Hrefs { get; } = new List<string>();


        public WebPage(WebClient Client, string address)
        {
            Console.WriteLine(" PAGE CHECK");

            string HtmlPage = Client.DownloadString(address);

            if (address[^1] != '/') address += '/';
            ////////
            for (int i = 0; i < address.Length && (address[i] != '/' || ++Nesting < 0); i++)
                Host += address[i];

            for (int i = Host.Length; i < address.Length && (address[i] != '/' || ++Nesting > 0); i++)
                Path += address[i];

            for (int i = Regex.Match(HtmlPage, "<title>").Index + 7; i < Regex.Match(HtmlPage, "</title>").Index; i++)
                Title += HtmlPage[i];
            ////////
            foreach (Match it in Regex.Matches(HtmlPage, "(?<=(<a href=\"" + Path + "))([-.0-9A-Za-z]+)(?=[\"/])"))
                if (!Hrefs.Contains(it.ToString())) Hrefs.Add(it.ToString());
        }

    }
}