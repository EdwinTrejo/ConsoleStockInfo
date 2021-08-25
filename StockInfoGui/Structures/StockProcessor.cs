using ServiceStack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.Text;
using System.Text;
using System.Threading.Tasks;

namespace StockInfoGui.Structures
{
    public class StockProcessor
    {
        private string license;
        public string filepath;
        public List<StockItem> file_content;
        public List<Tuple<StockFileContentLine, List<StockReturnItem>>> query_return_content;
        public Dictionary<string, List<StockReturnItem>> query_return_content_raw;

        public StockProcessor(StockFile stock_file, string licence_path)
        {
            if (!File.Exists(licence_path)) throw new FileNotFoundException();
            file_content = new List<StockItem>();
            query_return_content = new List<Tuple<StockFileContentLine, List<StockReturnItem>>>();
            query_return_content_raw = new Dictionary<string, List<StockReturnItem>>();

            license = File.ReadAllText(licence_path).Trim();

            foreach (StockFileContentLine item in stock_file.line_content)
            {
                if (query_return_content_raw.ContainsKey(item.Ticker))
                {
                    List<StockReturnItem> query_res = return_results_for_single_line(item);
                    query_return_content_raw.Add(item.Ticker, query_res);
                }
            }

            foreach (StockFileContentLine item in stock_file.line_content)
            {
                query_return_content_raw.TryGetValue(item.Ticker, out List<StockReturnItem> query_res);
                query_return_content.Add(new Tuple<StockFileContentLine, List<StockReturnItem>>(item, query_res));
            }

            //
            //
            // Process the data and divide between items. Queries are expensive I think
            //
            //

            var tasks = new List<Task<StockItem>>();
            foreach (Tuple<StockFileContentLine, List<StockReturnItem>> item in query_return_content)
            {
                //perform the calculations
                tasks.Add(ProcessStockReturnItem(item));
            }

            Task task = Task.Run(async () =>
            {
                StockItem[] async_results = await Task.WhenAll(tasks);
                var async_list_results = async_results.ToList();
                file_content.AddRange(async_list_results);
            });
            task.Wait();
        }

        private async Task<StockItem> ProcessStockReturnItem(Tuple<StockFileContentLine, List<StockReturnItem>> return_item)
        {
            StockItem new_stock_item = new StockItem();
            StockFileContentLine stockFileContentLine = return_item.Item1;
            List<StockReturnItem> stockReturnItems = return_item.Item2;

            //public string Ticker { get; set; }
            new_stock_item.Ticker = stockFileContentLine.Ticker;

            //public double Quantity { get; set; }
            new_stock_item.Quantity = stockFileContentLine.Quantity;

            //public string Account { get; set; }
            new_stock_item.Account = stockFileContentLine.Account;

            //public decimal BuyCost { get; set; }
            new_stock_item.BuyCost = stockFileContentLine.BuyCost;

            //public DateTime? BuyDate;
            new_stock_item.BuyDate = stockFileContentLine.BuyDate;

            //with the data we have to get the values from the class
            //public decimal Price { get; set; }
            //new_stock_item.Price = stockReturnItems lynq query from other program

            //public decimal Worth { get; set; }
            //public decimal OwnershipHigh { get; set; }
            //public decimal OwnershipLow { get; set; }
            //public decimal PriceOpen { get; set; }
            //public decimal DayChange { get; set; }
            //public double ChangeSinceBuy { get; set; }
            //public int DaysFromPurchase { get; set; }

            return new_stock_item;
        }

        private List<StockReturnItem> return_results_for_single_line(StockFileContentLine symbol)
        {
            List<StockReturnItem> combined_data = new List<StockReturnItem>();
            combined_data.AddRange(getStockInfoFromLatest(symbol));
            combined_data.AddRange(getStockInfoFromWeekly(symbol));
            return combined_data;
        }

        private List<StockReturnItem> getStockInfoFromLatest(StockFileContentLine symbol)
        {
            List<StockReturnItem> pull_intraday_information = $"https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol={symbol.Ticker}&outputsize=compact&apikey={license}&datatype=csv&interval=30m".GetStringFromUrl().FromCsv<List<StockReturnItem>>();
            return pull_intraday_information;
        }

        private List<StockReturnItem> getStockInfoFromWeekly(StockFileContentLine symbol)
        {
            List<StockReturnItem> pull_weekly_information = $"https://www.alphavantage.co/query?function=TIME_SERIES_WEEKLY&symbol={symbol.Ticker}&apikey={license}&datatype=csv".GetStringFromUrl().FromCsv<List<StockReturnItem>>();
            return pull_weekly_information;
        }
    }

    public class StockStoreData
    {
        public StockFileContentLine stockFileContentLine;
        public List<StockReturnItem> stockReturnItems;
    }
}
