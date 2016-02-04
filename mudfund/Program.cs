using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft;
using Serilog;
using System.Net.Http;
using System.Net;
using System.IO;
namespace mudfund
{
    class Program
    {
        public void fetchTick(object data)
        {
                var googFinance = new GoogleStock((string)data);

                //Running Average over 'n' seconds
                var n = 10; //samples
                decimal sum = 0;
                //collect ten stock ticks
                for (int i = 0; i < n; i++)
                {
                    sum += Convert.ToDecimal(googFinance.Request().l);
                    Thread.Sleep(1000); //wait one second 
                   
                }
                var avg = sum / n;
                Console.WriteLine("{0} : {1} ",Thread.CurrentThread.Name, avg);
        }

        class GoogleStock
        {
           public class RootObject
            {
               /// <summary>
               /// Google ID of Stock Request
               /// </summary>
                public string id { get; set; }
               /// <summary>
               /// Name of Stock
               /// </summary>
                public string t { get; set; }
               /// <summary>
               /// Name of Market
               /// </summary>
                public string e { get; set; }
               /// <summary>
               /// Stock Price
               /// </summary>
                public string l { get; set; }
               /// <summary>
               /// Fixed Stock Price ??
               /// </summary>
                public string l_fix { get; set; }
               /// <summary>
               /// Current Stock Price ??
               /// </summary>
                public string l_cur { get; set; }
                public string s { get; set; }
               /// <summary>
               /// Timestamp HH:MM PM EST
               /// </summary>
                public string ltt { get; set; }
               /// <summary>
               /// Timestamp Day #, HH:MM PM EST
               /// </summary>
                public string lt { get; set; }
               /// <summary>
               /// Timestamp Expanded
               /// </summary>
                public string lt_dts { get; set; }
                public string c { get; set; }
                public string c_fix { get; set; }
                public string cp { get; set; }
                public string cp_fix { get; set; }
                public string ccol { get; set; }
                public string pcls_fix { get; set; }
                public string el { get; set; }
                public string el_fix { get; set; }
                public string el_cur { get; set; }
                public string elt { get; set; }
                public string ec { get; set; }
                public string ec_fix { get; set; }
                public string ecp { get; set; }
                public string ecp_fix { get; set; }
                public string eccol { get; set; }
                public string div { get; set; }
                public string yld { get; set; }
            }
            private string _stockName = "";

           public GoogleStock(string stockName)
           {
               this._stockName = stockName;
           }
           public RootObject Request(string stockName = "")
            {
                if (stockName == "") stockName  = _stockName;

                //Create Request
                WebRequest request =
                    WebRequest.Create(
                    @"http://finance.google.com/finance/info?client=ig&q=NASDAQ:" 
                    + stockName);

                //Authenticate 
                request.Credentials = CredentialCache.DefaultCredentials;

                //Read Response
                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);

                // Read the content.
                string responseFromServer = reader.ReadToEnd();
               
                //Remove leading chars
                responseFromServer = responseFromServer.Replace("//","");
                responseFromServer = responseFromServer.Replace("\n", "");
                responseFromServer = responseFromServer.Replace("[", "");
                responseFromServer = responseFromServer.Replace("]", "");


                // Convert to json object
                var resp = Newtonsoft.Json.JsonConvert.DeserializeObject<RootObject>(responseFromServer);

                // Clean up the streams and the response.
                reader.Close();
                response.Close();

                return resp;

            }
        }

  
        
        static void Main(string[] args)
        {
            // make instance
            Program pt = new Program();

            //make stock list
            var stocks = new List<string>()
            {
                "TSLA",
                "MSFT",
                "AAPL"
            };


            Console.WriteLine("Started");
            foreach(var stock in stocks)
            {
                var t = new Thread(pt.fetchTick);
                t.Name = string.Format("Worker thread [{0}]", stock);
                t.Start(stock);
            }

            Console.ReadLine();

        }
    }
}

