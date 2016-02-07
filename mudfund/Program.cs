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
            var stockFactory = new Factory(@"stockdb.txt",portfolio.StockList);
            stockFactory.stocksPerTransaction = 10;
            stockFactory.transactionDelay = 86400; //daily pullback 24hrs,86400sec
            stockFactory.LoadHistorical(); //may take time to load
            stockFactory.Start();


            //-----------------------------------------------------
            //-----------------------------------------------------
            //-----------------------------------------------------
            // IMPLEMENT AI ON STOCK DATA IN FILE.

            //Run the prediction on stockdata
            //Port the output of the predictor 
            //to elasticsearch in order to view stock info

            //-----------------------------------------------------
            //-----------------------------------------------------
            //-----------------------------------------------------



            Console.ReadLine();
        }

    }
}

