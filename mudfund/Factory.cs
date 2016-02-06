using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mudfund
{
    public class Factory
    {

        //Finance API's
        private IStock[] stockAPIs = 
            { 
                new GoogleFinance(), 
                new YahooFinance() 
            };

        private int _stocksPerTransaction = 3; // default
        public int stocksPerTransaction
        {
            get
            {
                return _stocksPerTransaction;
            }
            set
            {
                _stocksPerTransaction = value;
            }
        }

        private int _transactionDelay = 10; //sec default
        /// <summary>
        /// Delay between transaction (sec)
        /// </summary>
        public int transactionDelay { get 
        { return _transactionDelay ; } 
            set { _transactionDelay = value ; } }

        private FileDB fileDB;
        public Factory(string fileName)
        {
            //Create a new file database
            fileDB = new FileDB(fileName);
        }

        public void Start(List<string> stocks)
        {
            // Break up stock list into chunks for multi-threads
            var listOfStockChunks = BreakIntoChunks<string>(stocks, stocksPerTransaction);

            Log.Information("Stock Factory Started");

            // Begin runtime loop
            while (true)
            {
                // Make list of threads to handle chunks
                var tasks = new List<Task<List<KeyValuePair<string, float>>>>();

                foreach (var chunk in listOfStockChunks)
                {
                    tasks.Add(new Task<List<KeyValuePair<string, float>>>
                        (() => { return fetchStockAsync(chunk); }));
                }


                Stopwatch sw = new Stopwatch();
                sw.Start();

                //Start all tasks
                foreach (var task in tasks) { task.Start(); }

                //Wait for all tasks to complete
                Task.WaitAll(tasks.ToArray());


                //Combine all task work into one key/val list
                var stockAndPrices = new List<KeyValuePair<string, float>>();

                foreach (var task in tasks)
                {
                    var stockAndPrice = task.Result;
                    stockAndPrices.AddRange(stockAndPrice);
                }

                Log.Debug("All threads complete.");
                sw.Stop();
                Log.Debug("Time elapsed: " + sw.Elapsed.TotalSeconds);

                //organize the stocks coming from threads
                stockAndPrices.OrderBy(x => x.Key);
                Log.Information("Writing to fileDB ");
                sw.Start();
                fileDB.Write(stockAndPrices);
                sw.Stop();
                Log.Debug("Time elapsed: " + sw.Elapsed.TotalSeconds);

                //Wait a normal amount of time before next pullback
                Log.Debug("Delay of {0} sec. started.",transactionDelay);
                Thread.Sleep(transactionDelay*1000);

            }//runtime loop


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
        public List<KeyValuePair<string, float>> fetchStockAsync(List<string> symbol)
        {
            //Choose API to use
            stockAPIs.OrderBy(x => x.GetPriority());
            var api = stockAPIs[0];

            var stocks = (List<string>)symbol;
            var prices = api.GetPrice(stocks);

            var stockAndPrice = stocks.Zip(prices, (s, p) =>
                new { Stock = s, Price = p });

            var stocksAndPrices = new List<KeyValuePair<string, float>>();

            foreach (var sp in stockAndPrice)
            {
                Log.Debug("{0} : {1} ", sp.Stock, sp.Price);
                stocksAndPrices.Add(
                    new KeyValuePair<string, float>(
                        sp.Stock, sp.Price));
            }

            return stocksAndPrices;

        }

        public class FileDB
        {

            private string fileName;

            /// <summary>
            /// Filename for the database file
            /// </summary>
            /// <param name="FileName">Filename including path and extension</param>
            public FileDB(string fileName)
            {
                this.fileName = fileName;
            }

            public void Write(List<KeyValuePair<string, float>> stockPrices)
            {
                //check for file
                if (File.Exists(fileName))
                {
                    //search the file for indexes. 
                    var fpStockSymIndex = new List<KeyValuePair<string, int>>();

                    var lineNo = 0;

                    foreach (var line in File.ReadAllLines(fileName))
                    {


                        //read the symbol header on the line
                        var sym = "";
                        try { sym = line.Split(',')[0]; }
                        catch { lineNo++; continue; }
                        //lookup in known list 
                        if (stockPrices.Exists(x => x.Key == sym))
                        {   //store the symbol and index locatoin
                            fpStockSymIndex.Add(new KeyValuePair<string, int>(sym, lineNo));
                        }
                        lineNo++;
                    }

                    var fileLines = File.ReadAllLines(fileName);

                    for (int i = 0; i < fileLines.Length; i++)
                    {
                        // read the sym index for the line no your on
                        var sym = fpStockSymIndex.FirstOrDefault(x => x.Value == i).Key;

                        //read the symbols price and add it to the line to print
                        fileLines[i] = fileLines[i]
                            + "," + stockPrices.FirstOrDefault(x => x.Key == sym).Value;
                    }
                    File.WriteAllLines(fileName, fileLines);
                }
                else
                {
                    //create a new file
                    var print = new List<string>();
                    foreach (var sp in stockPrices)
                    {
                        var line = string.Format("{0},{1}", sp.Key, sp.Value);
                        print.Add(line);
                    }

                    File.WriteAllLines(fileName, print.ToArray());

                }

            }

        }

        public interface IStock
        {
            float GetPrice(string stockSymbol);
            List<float> GetPrice(List<string> stockSymbols);
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
                try
                {

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

            public float GetPrice(string stockSymbol)
            {
                //Running Average over 'n' seconds
                var n = 3; //samples
                float sum = 0;
                //collect ten stock ticks
                for (int i = 0; i < n; i++)
                {
                    sum += float.Parse(
                        this.Request(
                        new List<string>() { stockSymbol })[0].l);
                    // Only taking a single stock symbol, 
                    // has capability to make several symbol requests per trans.

                    Thread.Sleep(1000); //wait one second 
                }
                var avg = sum / n;
                return avg;
            }

            public List<float> GetPrice(List<string> stockSymbols)
            {
                var stockObjs = this.Request(stockSymbols);

                var pricesString = stockObjs.Select(obj => obj.l).ToList();

                return pricesString.ConvertAll(p => float.Parse(p));
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
                public float Bid { get; set; }
                public float Ask { get; set; }
                public float Open { get; set; }
                public float PreviousClose { get; set; }
                public float Last { get; set; }
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
                    p.Bid = float.Parse(cols[2]);
                    p.Ask = float.Parse(cols[3]);
                    p.Open = float.Parse(cols[4]);
                    p.PreviousClose = float.Parse(cols[5]);
                    p.Last = float.Parse(cols[6]);

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
                    foreach (var s in stockSymbols)
                    {
                        stockSymbol += s + "+";
                    }
                    stockSymbol = stockSymbol.TrimEnd('+');

                    using (WebClient web = new WebClient())
                    {
                        csvData = web.DownloadString("http://finance.yahoo.com/d/quotes.csv?s=" + stockSymbol + "&f=snbaopl1");
                    }

                    return YahooFinance.Parse(csvData);
                }
                catch
                {
                    throw;
                }
            }

            public float GetPrice(string stockSymbol)
            {
                //Running Average over 'n' seconds
                var n = 3; //samples
                float sum = 0;
                //collect ten stock ticks
                for (int i = 0; i < n; i++)
                {
                    sum +=
                        this.Request(
                        new List<string>() { stockSymbol })[0].Ask;
                    // Only taking a single stock symbol, 
                    // has capability to make several symbol requests per trans.

                    Thread.Sleep(1000); //wait one second 
                }
                var avg = sum / n;
                return avg;

            }

            public List<float> GetPrice(List<string> stockSymbols)
            {
                var stockObjs = this.Request(stockSymbols);

                var pricesString = stockObjs.Select(obj => obj.Ask).ToList();

                return pricesString;
            }
        }



    }

}
