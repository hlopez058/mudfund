using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mudfund
{
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

            Log.Debug("Portfolio:\n{0}", string.Join("\n", stocks.ToArray()));
        }

    }
        
}
