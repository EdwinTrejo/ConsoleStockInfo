using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack;
using ServiceStack.Text;
using System.Text;

namespace ConsoleStockInfo
{
    public class StockItem
    {
        public DateTime Timestamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
    }

    
    class Program
    {
        const string apiKey = ""; //Get a alpha vantage api key
        static void Main(string[] args)
        {
            string availableCommands, userInput, command, arguments = string.Empty;
            bool isExit = false;
            availableCommands = "latest stockname\nall stockname\nhigh stockname\nlow stockname\nhelp : this message\nq : exit";
            Console.WriteLine("type help for available commands");
            while (!isExit)
            {
                Console.Write(" > ");
                userInput = Console.ReadLine();
                var output = userInput.Split(' ');
                command = output[0];
                if (output.Length > 1)
                {
                    arguments = output[1];
                }
                switch (command)
                {
                    case "latest":
                        try
                        {
                            var display = getStockInfoFromName(arguments);
                            var getSome = display.Where(u => u.Timestamp < DateTime.Now);
                            Console.WriteLine($"\tTime: {getSome.FirstOrDefault().Timestamp}" +
                                            $"\n\tOpen: {getSome.FirstOrDefault().Open}" +
                                            $"\n\tHigh: {getSome.FirstOrDefault().High}" +
                                            $"\n\tLow: {getSome.FirstOrDefault().Low}" +
                                            $"\n\tClose: {getSome.FirstOrDefault().Close}" +
                                            $"\n\tVolume: {getSome.FirstOrDefault().Volume}");
                        }
                        catch (Exception e) { Console.WriteLine(e.Message); };
                        break;
                    case "all":
                        try
                        {
                            var display = getStockInfoFromName(arguments);
                            display.PrintDump();
                        }
                        catch (Exception e) { Console.WriteLine(e.Message); };
                        break;
                    case "high":
                        try
                        {
                            var display = getStockInfoFromName(arguments);
                            Console.WriteLine(display.Max(u => u.Close));
                        }
                        catch (Exception e) { Console.WriteLine(e.Message); };
                        break;
                    case "low":
                        try
                        {
                            var display = getStockInfoFromName(arguments);
                            Console.WriteLine(display.Min(u => u.Close));
                        }
                        catch (Exception e) { Console.WriteLine(e.Message); };
                        break;
                    case "volume":
                        try
                        {
                            var display = getStockInfoFromName(arguments);
                            var getSome = display.Where(u => u.Timestamp < DateTime.Now);
                            Console.WriteLine($"{getSome.FirstOrDefault().Timestamp}\n{getSome.FirstOrDefault().Volume}");
                        }
                        catch (Exception e) { Console.WriteLine(e.Message); };
                        break;
                    case "help":
                        Console.WriteLine(availableCommands);
                        break;
                    case "exit":
                    case "q":
                        isExit = true;
                        break;
                    default:
                        Console.WriteLine("sorry that command has not been inplemented yet ...");
                        break;
                }
            }
        }

        public static List<StockItem> getStockInfoFromName(string symbol)
        {
            List<StockItem> monthlyPrices = $"https://www.alphavantage.co/query?function=TIME_SERIES_MONTHLY&symbol={symbol}&apikey={apiKey}&datatype=csv".GetStringFromUrl().FromCsv<List<StockItem>>();
            return monthlyPrices;
        }
        public static StockItem getStockInfoFromLatest(string symbol)
        {
            StockItem monthlyPrices = $"https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol={symbol}&apikey={apiKey}&datatype=csv".GetStringFromUrl().FromCsv<StockItem>();
            return monthlyPrices;
        }
    }
}
