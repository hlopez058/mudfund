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
using Serilog.Core;
using Serilog.Events;
namespace mudfund
{
    class Program
    {
       
        static void Main(string[] args)
        {
            var levelSwitch = new LoggingLevelSwitch();
            levelSwitch.MinimumLevel = LogEventLevel.Verbose;

            var messages = new StringWriter();
            
            //Serilogger
            Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.ControlledBy(levelSwitch)
                        .Enrich.WithProperty("Name", "MudFund")
                        .Enrich.WithProperty("Version", "1.0.0")                     
                        .WriteTo.ColoredConsole()
                        //.WriteTo.Logger(lc => lc
                        //            .MinimumLevel.Is.Fatal
                        //            .WriteTo.TextWriter(
                        //            messages,
                        //            outputTemplate: "{Message}{NewLine}"))
                        .CreateLogger();

            //Load a portfolio
            var portfolio = new Portfolio();
            portfolio.Load();
            
            //Start collecting stock info.
            var stockFactory = new Factory();
            stockFactory.Start(portfolio.StockList);


            Console.ReadLine();



        }

        public class Portfolio
        {
            public List<string> StockList = new List<string>();

            /// <summary>
            /// Load stocklist from file. 
            /// </summary>
            /// <param name="filename">Full path and filename with extension</param>
            public void Load(string filename)
            {
                //read in from file
                var lines = File.ReadAllLines(filename);
                foreach (var line in lines) { StockList.Add(line); }
            }
            public void Load(List<string> stocks)
            {
                StockList = stocks;
            }
            public void Load()
            {
                //default list of stocks
                //List of stocks to read
                var stocks = new List<string>()
                {
                    "TSLA",
                    "MSFT",
                    "TSLA",
                    "MSFT",
                    "TSLA",
                };

                StockList = stocks;

                Log.Information("Portfolio:\n{0}", string.Join("\n", stocks.ToArray()));
            }

        }
        
        public class Factory
        {
            //Finance API's
            private IStock[] stockAPIs = 
            { 
                new GoogleFinance(), 
                new YahooFinance() 
            };

            private int _stocksPerTransaction = 3; // default
            public int stocksPerTransaction { 
                get
                {
                    return _stocksPerTransaction;
                } 
                set
                {
                    _stocksPerTransaction =value;
                }
            }

          

            public void Start( List<string> stocks)
            {
                var listOfStockChunks = BreakIntoChunks<string>(stocks, stocksPerTransaction);

                //Launch threads to retreive stock prices
                foreach (var chunk in listOfStockChunks)
                {
                    var t = new Thread(this.fetchStockAsync);
                    t.Name = string.Format("Factory thread [{0}]", string.Join(",",chunk.ToArray()));
                    t.Start(chunk);
                }

                Log.Information("Stock Factory Started");
            }

            /// <summary>
            /// Used to break up list into smaller chunks
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="list"></param>
            /// <param name="chunkSize"></param>
            /// <returns></returns>
            public static List<List<T>> BreakIntoChunks<T>(List<T> list, int chunkSize)
            {
                if (chunkSize <= 0)
                {
                    throw new ArgumentException("chunkSize must be greater than 0.");
                }

                List<List<T>> retVal = new List<List<T>>();
                while (list.Count > 0)
                {
                    int count = list.Count > chunkSize ? chunkSize : list.Count;
                    retVal.Add(list.GetRange(0, count));
                    list.RemoveRange(0, count);
                }
                return retVal;
            }

            /// <summary>
            /// Fetch stock object from an available api
            /// </summary>
            /// <param name="symbol"></param>
            public void fetchStockAsync(object symbol)
            {
                //Choose API to use
                stockAPIs.OrderBy(x => x.GetPriority());
                var api = stockAPIs[0];

                var stocks = (List<string>)symbol;
                var prices = api.GetPrice(stocks);

                var stocksAndPrice = stocks.Zip(prices, (s, p) => new { Stock = s, Price = p });
                Log.Debug("Ran {Name}", Thread.CurrentThread.Name);
                foreach (var sp in stocksAndPrice)
                {
                    Log.Debug("{0} : {1} ", sp.Stock, sp.Price);
                }
            }

            public interface IStock
            {
                decimal GetPrice(string stockSymbol);
                List<decimal> GetPrice(List<string> stockSymbols);
                int GetPriority();
            }

            class GoogleFinance : IStock
            {
                // priority of api usage
                private int priority = 0;
                public int GetPriority() { return priority; }

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

                public List<decimal> GetPrice(List<string> stockSymbols)
                {
                    var stockObjs = this.Request(stockSymbols);

                    var pricesString = stockObjs.Select(obj=> obj.l).ToList();
                 
                    return pricesString.ConvertAll(p => Convert.ToDecimal(p));
                }
            }

            class YahooFinance : IStock
            {
                // priority of api usage
                private int priority = 1;
                public int GetPriority() { return priority; }

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

                public List<decimal> GetPrice(List<string> stockSymbols)
                {
                    var stockObjs = this.Request(stockSymbols);

                    var pricesString = stockObjs.Select(obj => obj.Ask).ToList();

                    return pricesString.ConvertAll(p => Convert.ToDecimal(p));
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

