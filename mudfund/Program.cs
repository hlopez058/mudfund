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
using System.Diagnostics;
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
                        .CreateLogger();

            //Load a portfolio
            var portfolio = new Portfolio();
            portfolio.Load(@"portfolio.txt");
            
            //Start collecting stock info.
            var stockFactory = new Factory(@"stockdb.txt");
            stockFactory.stocksPerTransaction = 10;
            stockFactory.transactionDelay = 5;
            stockFactory.Start(portfolio.StockList);

            Console.ReadLine();
        }

        //class Predictor
        //{
        //    // AI, analyze and predict stock behaviour
        //    //create interface , 
        //}

        //class Trader
        //{
        //    // AI, finance driven decisions to buy/sell 
        //}

        //class BankerSim
        //{
        //    // simulate buy/sell,fees,response time, market yield
        //}

        //class Banker
        //{
        //    // physical trades over api's or alert order over emails
        //}

        //class Watcher
        //{
        //   // create debug/finance logs that can be parsed by ELK stack
        //}
    }
}

