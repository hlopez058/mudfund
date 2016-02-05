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
       
        static void Main(string[] args)
        {

            Console.WriteLine("MudFund v1.0 - Showboat");
            
            //Start collecting stock info.
            Factory.Start();

            Console.ReadLine();

        }

        
        class Factory
        { 
            public static void Start()
            {
                //List of stocks to read
                var stocks = new List<string>()
                {
                    "TSLA",
                    "MSFT",
                    "AAPL"
                };

                var factory = new Factory();

                Console.WriteLine("Reading Stocks");

                //Launch threads to retreive stock prices
                foreach (var stock in stocks)
                {
                    var t = new Thread(factory.fetchStockAsync);
                    t.Name = string.Format("Factory thread [{0}]", stock);
                    t.Start(stock);
                }


            }

            /// <summary>
            /// Fetch stock object from an available api
            /// </summary>
            /// <param name="symbol"></param>
            public void fetchStockAsync(object symbol)
            {
                //Create Finance API's
                IStock[] stockAPIs = {new GoogleFinance(), new YahooFinance()} ;
                //Choose API to use
                var api = stockAPIs[0]; //0=Using Google Finance 1=Yahoo Finance

                Console.WriteLine("{0} : {1} ", Thread.CurrentThread.Name, api.GetPrice((string)symbol));
            }

            interface IStock
            {
                decimal GetPrice(string stockSymbol);
              
            }

            class GoogleFinance : IStock
            {
                class Stock
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

                List<Stock> Request(List<string> stockSymbols)
                {
                    try {

                        string stockSymbol = "";
                        foreach (var s in stockSymbols)
                        {
                            stockSymbol += s + ",";
                        }
                        stockSymbol = stockSymbol.TrimEnd(',');


                        //Create Request
                        WebRequest request =
                        WebRequest.Create(
                        @"http://finance.google.com/finance/info?client=ig&q="
                        + stockSymbol);

                    //Authenticate 
                    request.Credentials = CredentialCache.DefaultCredentials;

                    //Read Response
                    WebResponse response = request.GetResponse();
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);

                    // Read the content.
                    string responseFromServer = reader.ReadToEnd();

                    //Remove leading chars
                    responseFromServer = responseFromServer.Replace("//", "");
                    responseFromServer = responseFromServer.Replace("\n", "");


                    // Convert to json object
                    var resp = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Stock>>(responseFromServer);

                    // Clean up the streams and the response.
                    reader.Close();
                    response.Close();

                    return resp;
                    }
                    catch
                    {
                        throw;
                    }
                }

                public decimal GetPrice(string stockSymbol)
                {
                    //Running Average over 'n' seconds
                    var n = 3; //samples
                    decimal sum = 0;
                    //collect ten stock ticks
                    for (int i = 0; i < n; i++)
                    {
                        sum += Convert.ToDecimal(
                            this.Request(
                            new List<string>() { stockSymbol })[0].l); 
                        // Only taking a single stock symbol, 
                        // has capability to make several symbol requests per trans.

                        Thread.Sleep(1000); //wait one second 
                    }
                    var avg = sum / n;
                    return avg;
              }
            }

            class YahooFinance : IStock
            {
                /*Pricing Dividends 
                a: Ask y: Dividend Yield 
                b: Bid d: Dividend per Share 
                b2: Ask (Realtime) r1: Dividend Pay Date 
                b3: Bid (Realtime) q: Ex-Dividend Date 
                p: Previous Close  
                o: Open  
                Date 
                c1: Change d1: Last Trade Date 
                c: Change & Percent Change d2: Trade Date 
                c6: Change (Realtime) t1: Last Trade Time 
                k2: Change Percent (Realtime)  
                p2: Change in Percent  
                Averages 
                c8: After Hours Change (Realtime) m5: Change From 200 Day Moving Average 
                c3: Commission m6: Percent Change From 200 Day Moving Average 
                g: Day’s Low m7: Change From 50 Day Moving Average 
                h: Day’s High m8: Percent Change From 50 Day Moving Average 
                k1: Last Trade (Realtime) With Time m3: 50 Day Moving Average 
                l: Last Trade (With Time) m4: 200 Day Moving Average 
                l1: Last Trade (Price Only)  
                t8: 1 yr Target Price  
                Misc 
                w1: Day’s Value Change g1: Holdings Gain Percent 
                w4: Day’s Value Change (Realtime) g3: Annualized Gain 
                p1: Price Paid g4: Holdings Gain 
                m: Day’s Range g5: Holdings Gain Percent (Realtime) 
                m2: Day’s Range (Realtime) g6: Holdings Gain (Realtime) 
                52 Week Pricing Symbol Info 
                k: 52 Week High v: More Info 
                j: 52 week Low j1: Market Capitalization 
                j5: Change From 52 Week Low j3: Market Cap (Realtime) 
                k4: Change From 52 week High f6: Float Shares 
                j6: Percent Change From 52 week Low n: Name 
                k5: Percent Change From 52 week High n4: Notes 
                w: 52 week Range s: Symbol 
                s1: Shares Owned 
                x: Stock Exchange 
                j2: Shares Outstanding 
                Volume 
                v: Volume  
                a5: Ask Size  
                b6: Bid Size Misc 
                k3: Last Trade Size t7: Ticker Trend 
                a2: Average Daily Volume t6: Trade Links 
                i5: Order Book (Realtime) 
                Ratios l2: High Limit 
                e: Earnings per Share l3: Low Limit 
                e7: EPS Estimate Current Year v1: Holdings Value 
                e8: EPS Estimate Next Year v7: Holdings Value (Realtime) 
                e9: EPS Estimate Next Quarter s6 Revenue 
                b4: Book Value  
                j4: EBITDA  
                p5: Price / Sales  
                p6: Price / Book  
                r: P/E Ratio  
                r2: P/E Ratio (Realtime)  
                r5: PEG Ratio  
                r6: Price / EPS Estimate Current Year  
                r7: Price / EPS Estimate Next Year  
                s7: Short Ratio 
                */

                class Stock
                {
                    public string Symbol { get; set; }
                    public string Name { get; set; }
                    public decimal Bid { get; set; }
                    public decimal Ask { get; set; }
                    public decimal Open { get; set; }
                    public decimal PreviousClose { get; set; }
                    public decimal Last { get; set; }
                }

                static List<Stock> Parse(string csvData)
                {
                    List<Stock> prices = new List<Stock>();
 
                    string[] rows = csvData.Replace("\r", "").Split('\n');
 
                    foreach (string row in rows)
                    {
                        if (string.IsNullOrEmpty(row)) continue;
 
                        string[] cols = row.Split(',');
 
                        Stock p = new Stock();
                        p.Symbol = cols[0];
                        p.Name = cols[1];
                        p.Bid = Convert.ToDecimal(cols[2]);
                        p.Ask = Convert.ToDecimal(cols[3]);
                        p.Open = Convert.ToDecimal(cols[4]);
                        p.PreviousClose = Convert.ToDecimal(cols[5]);
                        p.Last = Convert.ToDecimal(cols[6]);
 
                        prices.Add(p);
                    }
 
                    return prices;
                }
                
                List<Stock> Request(List<string> stockSymbols)
                {
                    try 
                    { 
                        string csvData;
                        string stockSymbol = "";
                        foreach(var s in stockSymbols){
                            stockSymbol += s + "+";
                        }
                        stockSymbol = stockSymbol.TrimEnd('+');

                        using (WebClient web = new WebClient())
                        {
                            csvData = web.DownloadString("http://finance.yahoo.com/d/quotes.csv?s="+stockSymbol+"&f=snbaopl1");
                        }
 
                       return YahooFinance.Parse(csvData);
                    }
                    catch
                    {
                        throw;
                    }
                }

                public decimal GetPrice(string stockSymbol)
                {
                    //Running Average over 'n' seconds
                    var n = 3; //samples
                    decimal sum = 0;
                    //collect ten stock ticks
                    for (int i = 0; i < n; i++)
                    {
                        sum += Convert.ToDecimal(
                            this.Request(
                            new List<string>(){stockSymbol})[0].Ask);
                        // Only taking a single stock symbol, 
                        // has capability to make several symbol requests per trans.

                        Thread.Sleep(1000); //wait one second 
                    }
                    var avg = sum / n;
                    return avg;
                    
                }
            }
        }

        class Predictor
        {
            // AI, analyze and predict stock behaviour




            //create interface , 
        }

        class Trader
        {
            // AI, finance driven decisions to buy/sell 
        }

        class BankerSim
        {
            // simulate buy/sell,fees,response time, market yield
        }

        class Banker
        {
            // physical trades over api's or alert order over emails
        }

        class Watcher
        {
            // create debug/finance logs that can be parsed by ELK stack
        }
    }
}

