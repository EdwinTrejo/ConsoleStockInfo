using ServiceStack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.Text;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using AlphaVantage.Net.Common;
using AlphaVantage.Net.Core.Client;
using System.Net;
using AlphaVantage.Net.Common.Intervals;
using AlphaVantage.Net.Common.Size;
using AlphaVantage.Net.Stocks;
using AlphaVantage.Net.Stocks.Client;


namespace StockInfoGui.Structures
{
    public class StockProcessor
    {
        private string license;
        public List<StockItem> file_content;
        private List<Tuple<StockFileContentLine, List<StockReturnItem>>> query_return_content;
        private Dictionary<string, List<StockReturnItem>> query_return_content_raw;

        public void Process(StockFile stock_file, string licence_path)
        {
            if (!File.Exists(licence_path)) throw new FileNotFoundException();
            license = File.ReadAllText(licence_path).Trim();

            foreach (StockFileContentLine item in stock_file.line_content)
            {
                if (!query_return_content_raw.ContainsKey(item.Ticker))
                {
                    List<StockReturnItem> query_res = return_results_for_single_line(item);
                    query_return_content_raw.Add(item.Ticker, query_res);
                }
                else
                {
                    //I guess delay older than 1 hour warrants and not after extended market hours will do
                    query_return_content_raw.TryGetValue(item.Ticker, out List<StockReturnItem> look_at);

                    if (look_at != null)
                    {
                        StockReturnItem last_file_date = look_at.Select(D => D).OrderByDescending(C => C.Timestamp).FirstOrDefault();
                        if (last_file_date != null)
                        {
                            DateTime rn = DateTime.Now;
                            double minutes_timediff = (last_file_date.Timestamp - rn).TotalMinutes;
                            if ((rn.Hour < 18) && (minutes_timediff >= 12))
                            {
                                List<StockReturnItem> query_res = getStockInfoFromLatest(item);
                                query_return_content_raw[item.Ticker].AddRange(query_res);
                            }
                        }
                    }
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

            foreach (Tuple<StockFileContentLine, List<StockReturnItem>> item in query_return_content)
            {
                file_content.Add(ProcessStockReturnItemSync(item));
            }

            /*
            var tasks = new List<Task<StockItem>>();
            foreach (Tuple<StockFileContentLine, List<StockReturnItem>> item in query_return_content)
            {
                //perform the calculations
                tasks.Add(ProcessStockReturnItemAsync(item));
            }
            Task task = Task.Run(async () =>
            {
                StockItem[] async_results = await Task.WhenAll(tasks);
                var async_list_results = async_results.ToList();
                file_content.Add(async_list_results);
            });
            task.Wait();
            */
        }

        public StockProcessor()
        {
            file_content = new List<StockItem>();
            query_return_content = new List<Tuple<StockFileContentLine, List<StockReturnItem>>>();
            query_return_content_raw = new Dictionary<string, List<StockReturnItem>>();
        }

        private async Task<StockItem> ProcessStockReturnItemAsync(Tuple<StockFileContentLine, List<StockReturnItem>> return_item)
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
            //lynq query from other program
            if (stockFileContentLine.BuyDate != null)
            {
                //public decimal Price { get; set; }
                //new_stock_item.Price = stockReturnItems
                StockReturnItem last_file_date = stockReturnItems.Select(D => D).OrderByDescending(C => C.Timestamp).FirstOrDefault();
                new_stock_item.Price = last_file_date.Open;

                //public decimal Worth { get; set; }
                new_stock_item.Worth = last_file_date.Open * (decimal)stockFileContentLine.Quantity;

                //public decimal OwnershipHigh { get; set; }
                StockReturnItem high_file_price = stockReturnItems.Select(D => D).OrderByDescending(C => C.High).FirstOrDefault();
                new_stock_item.OwnershipHigh = high_file_price.High * (decimal)stockFileContentLine.Quantity;

                //public decimal OwnershipLow { get; set; }
                StockReturnItem low_file_price = stockReturnItems.Select(D => D).OrderByDescending(C => C.Low).LastOrDefault();
                new_stock_item.OwnershipLow = low_file_price.Low * (decimal)stockFileContentLine.Quantity;

                //public decimal PriceOpen { get; set; }
                StockReturnItem today_file_price = stockReturnItems.Select(D => D).OrderByDescending(C => C.Timestamp).Where(B => B.Timestamp.Date == DateTime.Now.Date).FirstOrDefault();
                new_stock_item.PriceOpen = today_file_price.Open * (decimal)stockFileContentLine.Quantity;

                //public decimal DayChange { get; set; }
                new_stock_item.DayChange = new_stock_item.PriceOpen - new_stock_item.Price;

                //public double ChangeSinceBuy { get; set; }
                StockReturnItem buy_day_file_price = stockReturnItems.Select(D => D).OrderByDescending(C => C.Timestamp).Where(B => B.Timestamp.Date == ((DateTime)stockFileContentLine.BuyDate).Date).FirstOrDefault();
                new_stock_item.ChangeSinceBuy = buy_day_file_price.Close * (decimal)stockFileContentLine.Quantity;

                //public int DaysFromPurchase { get; set; }
                new_stock_item.DaysFromPurchase = (int)(DateTime.Now.Date - ((DateTime)stockFileContentLine.BuyDate).Date).TotalDays;
            }
            return new_stock_item;
        }

        private StockItem ProcessStockReturnItemSync(Tuple<StockFileContentLine, List<StockReturnItem>> return_item)
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
            //lynq query from other program
            if (stockFileContentLine.BuyDate != null)
            {
                //public decimal Price { get; set; }
                //new_stock_item.Price = stockReturnItems
                StockReturnItem last_file_date = stockReturnItems.Select(D => D).OrderByDescending(C => C.Timestamp).FirstOrDefault();
                new_stock_item.Price = last_file_date.Open;

                //public decimal Worth { get; set; }
                new_stock_item.Worth = last_file_date.Open * (decimal)stockFileContentLine.Quantity;

                //public decimal OwnershipHigh { get; set; }
                StockReturnItem high_file_price = stockReturnItems.Select(D => D).OrderByDescending(C => C.High).FirstOrDefault();
                new_stock_item.OwnershipHigh = high_file_price.High * (decimal)stockFileContentLine.Quantity;

                //public decimal OwnershipLow { get; set; }
                StockReturnItem low_file_price = stockReturnItems.Select(D => D).OrderByDescending(C => C.Low).LastOrDefault();
                new_stock_item.OwnershipLow = low_file_price.Low * (decimal)stockFileContentLine.Quantity;

                //public decimal PriceOpen { get; set; }
                StockReturnItem today_file_price = stockReturnItems.Select(D => D).OrderByDescending(C => C.Timestamp).Where(B => B.Timestamp.Date == DateTime.Now.Date).FirstOrDefault();
                new_stock_item.PriceOpen = today_file_price.Open * (decimal)stockFileContentLine.Quantity;

                //public decimal DayChange { get; set; }
                new_stock_item.DayChange = new_stock_item.PriceOpen - new_stock_item.Price;

                //public double ChangeSinceBuy { get; set; }
                StockReturnItem buy_day_file_price = stockReturnItems.Select(D => D).OrderByDescending(C => C.Timestamp).Where(B => B.Timestamp.Date == ((DateTime)stockFileContentLine.BuyDate).Date).FirstOrDefault();
                new_stock_item.ChangeSinceBuy = buy_day_file_price.Close * (decimal)stockFileContentLine.Quantity;

                //public int DaysFromPurchase { get; set; }
                new_stock_item.DaysFromPurchase = (int)(DateTime.Now.Date - ((DateTime)stockFileContentLine.BuyDate).Date).TotalDays;
            }
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
            List<StockReturnItem> pull_intraday_information = new List<StockReturnItem>();
            if (symbol.BuyDate != null)
            {
                using var client = new AlphaVantageClient(license);
                using var stocksClient = client.Stocks();
                StockTimeSeries stockTs;
                Task task = Task.Run(async () =>
                {
                    stockTs = await stocksClient.GetTimeSeriesAsync(symbol.Ticker, Interval.Min60, OutputSize.Full, isAdjusted: false);
                    if (stockTs != null) pull_intraday_information = ProcessDataForMe(stockTs.DataPoints.ToList());
                });
                task.Wait();
            }
            return pull_intraday_information;
        }

        private List<StockReturnItem> getStockInfoFromWeekly(StockFileContentLine symbol)
        {
            List<StockReturnItem> pull_weekly_information = new List<StockReturnItem>();
            if (symbol.BuyDate != null)
            {
                using var client = new AlphaVantageClient(license);
                using var stocksClient = client.Stocks();
                StockTimeSeries stockTs;
                Task task = Task.Run(async () =>
                {
                    stockTs = await stocksClient.GetTimeSeriesAsync(symbol.Ticker, Interval.Weekly, OutputSize.Full, isAdjusted: false);
                    if (stockTs != null) pull_weekly_information = ProcessDataForMe(stockTs.DataPoints.ToList());
                });
                task.Wait();
            }
            return pull_weekly_information;
        }

        private List<StockReturnItem> ProcessDataForMe(List<StockDataPoint> dataPoints)
        {
            List<StockReturnItem> return_items = new List<StockReturnItem>();
            foreach(var item in dataPoints)
            {
                return_items.Add(new StockReturnItem()
                {
                    Timestamp = item.Time,
                    Open = item.OpeningPrice,
                    High = item.HighestPrice,
                    Low = item.LowestPrice,
                    Close = item.ClosingPrice,
                    Volume = item.Volume
                });
            }
            return return_items;
        }
    }
}
