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
using System.Threading;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;

namespace StockInfoGui.Structures
{
    public class StockProcessor
    {
        private string license;
        public List<StockItem> file_content;
        private List<Tuple<StockFileContentLine, List<StockReturnItem>>> query_return_content;
        private Dictionary<string, List<StockReturnItem>> query_return_content_raw;
        private readonly int global_query_timeout = 2000;
        private readonly string connection_string = ConfigurationManager.AppSettings.Get("ConnectionString");
        private MetaSqlHelper<StockReturnItem> MetaSQLManager = new MetaSqlHelper<StockReturnItem>
            (
                ConfigurationManager.AppSettings.Get("ConnectionString"),
                new List<string>()
                {
                    "TickerID",
                    "TimestampUTC",
                    "OpenPrice",
                    "ClosePrice",
                    "HighPrice",
                    "LowPrice",
                    "Volume"
                },
                "StockInfoGui.Structures.StockReturnItem"
            );

        //StockOrMutualFund
        public enum StockOrMutualFundEnum
        {
            Stock,
            MutualFund,
            Money
        }

        public void Process(StockFile stock_file, string licence_path)
        {
            if (!File.Exists(licence_path)) throw new FileNotFoundException();
            license = File.ReadAllText(licence_path).Trim();
            bool first_item = true;

            query_return_content.Clear();

            foreach (StockFileContentLine item in stock_file.line_content)
            {
                if (!query_return_content_raw.ContainsKey(item.Ticker))
                {
                    var getFromSQL = GetAllStockReturnReturnItems(item);
                    if (getFromSQL.Count <= 0)
                    {
                        //get from alphavantage
                        //1000 ms * 60 s * 3 m
                        if (!first_item) Thread.Sleep(1000 * 4 * 1);
                        List<StockReturnItem> query_res = return_results_for_single_line(item);
                        query_return_content_raw.Add(item.Ticker, query_res);
                    }
                    else if (getFromSQL != null)
                    {
                        //get from sql
                        query_return_content_raw.Add(item.Ticker, getFromSQL);
                    }
                    first_item = false;
                }
                else
                {
                    //I guess delay older than 1 hour warrants and not after extended market hours will do
                    query_return_content_raw.TryGetValue(item.Ticker, out List<StockReturnItem> look_at);
                    if (look_at != null)
                    {
                        //StockReturnItem last_file_date = look_at.Select(D => D).OrderByDescending(C => C.TimestampUTC).FirstOrDefault();
                        StockReturnItem getFromSQL = GetLastStockReturnItemPerTickerID(item.Ticker);
                        if (getFromSQL != null)
                        {
                            DateTime rn = DateTime.Now;
                            double minutes_timediff = Math.Abs((getFromSQL.TimestampUTC - rn).TotalMinutes);
                            if ((rn.Hour <= 18) && (minutes_timediff >= 500))
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
            //file_content.Clear();
            //foreach (Tuple<StockFileContentLine, List<StockReturnItem>> item in query_return_content)
            //{
            //    file_content.Add(ProcessStockReturnItemSync(item));
            //}

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
                file_content.AddRange(async_list_results);
            });
            task.Wait();

        }

        public StockProcessor()
        {
            file_content = new List<StockItem>();
            query_return_content = new List<Tuple<StockFileContentLine, List<StockReturnItem>>>();
            query_return_content_raw = new Dictionary<string, List<StockReturnItem>>();
        }

        public List<StockReturnItem> GetAllStockReturnReturnItems(StockFileContentLine symbol)
        {
            int TickerID = GetTickerID(symbol.Ticker);
            return GetAllStockReturnReturnItems(TickerID);
        }

        public List<StockReturnItem> GetAllStockReturnReturnItems(int TickerID)
        {
            List<SqlParameter> sqlParameters = new List<SqlParameter>();
            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "@TickerID",
                Direction = System.Data.ParameterDirection.Input,
                Value = TickerID,
                SqlDbType = System.Data.SqlDbType.Int
            });
            MetaSQLManager.CommitStoreProcedure(sqlParameters, "[StockData].[StockData].[GetAllStockReturnItemPerTickerID]");
            MetaSQLManager.GetResults(out List<StockReturnItem> StockReturnItemsOut);
            return StockReturnItemsOut;
        }

        public StockReturnItem GetLastStockReturnItemPerTickerID(string Ticker)
        {
            int TickerID = GetTickerID(Ticker);
            return GetLastStockReturnItemPerTickerID(TickerID);
        }

        public StockReturnItem GetLastStockReturnItemPerTickerID(int TickerID)
        {
            List<SqlParameter> sqlParameters = new List<SqlParameter>();
            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "@TickerID",
                Direction = System.Data.ParameterDirection.Input,
                Value = TickerID,
                SqlDbType = System.Data.SqlDbType.Int
            });
            MetaSQLManager.CommitStoreProcedure(sqlParameters, "[StockData].[StockData].[GetLastStockReturnItemPerTickerID]");
            MetaSQLManager.GetResults(out List<StockReturnItem> StockReturnItemsOut);
            return StockReturnItemsOut.FirstOrDefault();
        }

        public StockReturnItem GetLastStockReturnItemPerTickerIDAndDate(string Ticker, DateTime TimestampUTC)
        {
            int TickerID = GetTickerID(Ticker);
            return GetLastStockReturnItemPerTickerIDAndDate(TickerID, TimestampUTC);
        }

        public StockReturnItem GetLastStockReturnItemPerTickerIDAndDate(int TickerID, DateTime TimestampUTC)
        {
            List<SqlParameter> sqlParameters = new List<SqlParameter>();
            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "@TickerID",
                Direction = System.Data.ParameterDirection.Input,
                Value = TickerID,
                SqlDbType = System.Data.SqlDbType.Int
            });
            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "@TimestampUTC",
                Direction = System.Data.ParameterDirection.Input,
                Value = TimestampUTC.Date,
                SqlDbType = System.Data.SqlDbType.Date
            });
            MetaSQLManager.CommitStoreProcedure(sqlParameters, "[StockData].[StockData].[GetLastStockReturnItemPerTickerIDAndDate]");
            MetaSQLManager.GetResults(out List<StockReturnItem> StockReturnItemsOut);
            return StockReturnItemsOut.FirstOrDefault();
        }

        public void WriteDatapointsToDB(List<StockDataPoint> dataPoints, StockFileContentLine symbol)
        {
            int TickerID = GetTickerID(symbol.Ticker);
            foreach (var item in dataPoints)
            {
                string storedProcedure = "[StockData].[StockData].[InsertStockReturnItem]";
                var timeUtc = item.Time;
                TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                DateTime TimestampUTC = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, easternZone);
                decimal OpenPrice = item.OpeningPrice;
                decimal ClosePrice = item.ClosingPrice;
                decimal HighPrice = item.HighestPrice;
                decimal LowPrice = item.LowestPrice;
                long Volume = item.Volume;

                List<SqlParameter> sqlParameters = new List<SqlParameter>();

                //@Ticker VARCHAR(MAX),
                sqlParameters.Add(new SqlParameter
                {
                    ParameterName = "@TickerID",
                    Direction = System.Data.ParameterDirection.Input,
                    Value = TickerID,
                    SqlDbType = System.Data.SqlDbType.Int
                });

                //@TimestampUTC DATETIME,
                sqlParameters.Add(new SqlParameter
                {
                    ParameterName = "@TimestampUTC",
                    Direction = System.Data.ParameterDirection.Input,
                    Value = TimestampUTC,
                    SqlDbType = System.Data.SqlDbType.DateTime
                });

                //@OpenPrice DECIMAL,
                sqlParameters.Add(new SqlParameter
                {
                    ParameterName = "@OpenPrice",
                    Direction = System.Data.ParameterDirection.Input,
                    Value = OpenPrice,
                    SqlDbType = System.Data.SqlDbType.Float
                });

                //@HighPrice DECIMAL,
                sqlParameters.Add(new SqlParameter
                {
                    ParameterName = "@HighPrice",
                    Direction = System.Data.ParameterDirection.Input,
                    Value = HighPrice,
                    SqlDbType = System.Data.SqlDbType.Float
                });

                //@LowPrice DECIMAL,
                sqlParameters.Add(new SqlParameter
                {
                    ParameterName = "@LowPrice",
                    Direction = System.Data.ParameterDirection.Input,
                    Value = LowPrice,
                    SqlDbType = System.Data.SqlDbType.Float
                });

                //@ClosePrice DECIMAL,
                sqlParameters.Add(new SqlParameter
                {
                    ParameterName = "@ClosePrice",
                    Direction = System.Data.ParameterDirection.Input,
                    Value = ClosePrice,
                    SqlDbType = System.Data.SqlDbType.Float
                });

                //@Volume INT
                sqlParameters.Add(new SqlParameter
                {
                    ParameterName = "@Volume",
                    Direction = System.Data.ParameterDirection.Input,
                    Value = Volume,
                    SqlDbType = System.Data.SqlDbType.BigInt
                });

                CommitStoreProcedure(sqlParameters, storedProcedure);
            }
        }

        public void CommitStoreProcedure(List<SqlParameter> sqlParameters, string storedProcedure)
        {
            using (SqlConnection connection = new SqlConnection(connection_string))
            {
                connection.Open();
                using (SqlCommand sqlCommand = new SqlCommand(storedProcedure, connection) { CommandType = CommandType.StoredProcedure })
                {
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction();

                    sqlCommand.Connection = connection;
                    sqlCommand.Transaction = transaction;
                    sqlCommand.Parameters.AddRange(sqlParameters.ToArray());

                    try
                    {
                        sqlCommand.ExecuteNonQuery();
                        transaction.Commit();
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            transaction.Rollback();
                            connection.Close();
                        }
                        catch (Exception ex)
                        {
                            //return stack trace style
                            connection.Close();
                            throw new Exception($"{e.Message}{ex.Message}");
                        }
                        throw new Exception(e.Message);
                    }
                }
            }
        }

        public int GetTickerID(string ticker)
        {
            string storedProcedure = "[StockData].[StockData].[GetTickerID]";
            int TickerID = 0;
            List<SqlParameter> sqlParameters = new List<SqlParameter>();
            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "@Ticker",
                Direction = System.Data.ParameterDirection.Input,
                Value = ticker,
                SqlDbType = System.Data.SqlDbType.VarChar
            });
            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "@TickerID",
                Direction = System.Data.ParameterDirection.Output,
                Value = TickerID,
                SqlDbType = System.Data.SqlDbType.Int
            });
            CommitStoreProcedure(sqlParameters, storedProcedure);
            SqlParameter param = sqlParameters.Where(x => x.ParameterName == "@TickerID").FirstOrDefault();
            return (int)param.Value;
        }

        public void InsertTicker(string ticker, byte StockOrMutualFund = 0)
        {
            string storedProcedure = "[StockData].[StockData].[InsertTicker]";
            List<SqlParameter> sqlParameters = new List<SqlParameter>();

            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "@Ticker",
                Direction = System.Data.ParameterDirection.Input,
                Value = ticker,
                SqlDbType = System.Data.SqlDbType.VarChar
            });

            sqlParameters.Add(new SqlParameter
            {
                ParameterName = "@StockOrMutualFund",
                Direction = System.Data.ParameterDirection.Input,
                Value = StockOrMutualFund,
                SqlDbType = System.Data.SqlDbType.Bit
            });

            CommitStoreProcedure(sqlParameters, storedProcedure);
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
                StockReturnItem today_file_price_sql = GetLastStockReturnItemPerTickerID(stockFileContentLine.Ticker);

                StockReturnItem today_morning_price_sql = GetLastStockReturnItemPerTickerIDAndDate(stockFileContentLine.Ticker, DateTime.Now);

                if (today_morning_price_sql == null)
                    today_morning_price_sql = GetLastStockReturnItemPerTickerIDAndDate(stockFileContentLine.Ticker, DateTime.Now.AddDays(-1));
                if (today_morning_price_sql == null)
                    today_morning_price_sql = GetLastStockReturnItemPerTickerIDAndDate(stockFileContentLine.Ticker, DateTime.Now.AddDays(-2));
                if (today_morning_price_sql == null)
                    today_morning_price_sql = GetLastStockReturnItemPerTickerIDAndDate(stockFileContentLine.Ticker, DateTime.Now.AddDays(-3));
                if (today_morning_price_sql == null)
                    today_morning_price_sql = GetLastStockReturnItemPerTickerIDAndDate(stockFileContentLine.Ticker, DateTime.Now.AddDays(-4));
                if (today_morning_price_sql == null)
                    today_morning_price_sql = GetLastStockReturnItemPerTickerIDAndDate(stockFileContentLine.Ticker, DateTime.Now.AddDays(-5));

                //public decimal Price { get; set; }
                //new_stock_item.Price = stockReturnItems
                StockReturnItem last_file_date = stockReturnItems.Select(D => D).OrderByDescending(C => C.TimestampUTC).FirstOrDefault();
                new_stock_item.Price = today_file_price_sql.ClosePrice;

                //public decimal Worth { get; set; }
                new_stock_item.Worth = last_file_date.OpenPrice * stockFileContentLine.Quantity;

                //public decimal OwnershipHigh { get; set; }
                StockReturnItem high_file_price = stockReturnItems.Select(D => D).Where(B => B.TimestampUTC > stockFileContentLine.BuyDate).OrderByDescending(C => C.HighPrice).FirstOrDefault();
                new_stock_item.OwnershipHigh = high_file_price.HighPrice * stockFileContentLine.Quantity;

                //public decimal OwnershipLow { get; set; }
                StockReturnItem low_file_price = stockReturnItems.Select(D => D).Where(B => B.TimestampUTC > stockFileContentLine.BuyDate).OrderByDescending(C => C.LowPrice).LastOrDefault();
                new_stock_item.OwnershipLow = low_file_price.LowPrice * stockFileContentLine.Quantity;

                //public decimal PriceOpen { get; set; }
                new_stock_item.PriceOpen = today_morning_price_sql.OpenPrice * stockFileContentLine.Quantity;

                //public decimal DayChange { get; set; }
                new_stock_item.DayChange = new_stock_item.Worth - new_stock_item.PriceOpen;

                new_stock_item.ChangeSinceBuy = new_stock_item.Worth - (new_stock_item.BuyCost * stockFileContentLine.Quantity);

                //public int DaysFromPurchase { get; set; }
                new_stock_item.DaysFromPurchase = (int)(DateTime.Now - ((DateTime)stockFileContentLine.BuyDate)).TotalDays;
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
                StockReturnItem today_file_price_sql = GetLastStockReturnItemPerTickerID(stockFileContentLine.Ticker);

                //public decimal Price { get; set; }
                //new_stock_item.Price = stockReturnItems
                StockReturnItem last_file_date = stockReturnItems.Select(D => D).OrderByDescending(C => C.TimestampUTC).FirstOrDefault();
                new_stock_item.Price = today_file_price_sql.ClosePrice;

                //public decimal Worth { get; set; }
                new_stock_item.Worth = last_file_date.OpenPrice * stockFileContentLine.Quantity;

                //public decimal OwnershipHigh { get; set; }
                StockReturnItem high_file_price = stockReturnItems.Select(D => D).Where(B => B.TimestampUTC > stockFileContentLine.BuyDate).OrderByDescending(C => C.HighPrice).FirstOrDefault();
                new_stock_item.OwnershipHigh = high_file_price.HighPrice * stockFileContentLine.Quantity;

                //public decimal OwnershipLow { get; set; }
                StockReturnItem low_file_price = stockReturnItems.Select(D => D).Where(B => B.TimestampUTC > stockFileContentLine.BuyDate).OrderByDescending(C => C.LowPrice).LastOrDefault();
                new_stock_item.OwnershipLow = low_file_price.LowPrice * stockFileContentLine.Quantity;

                //public decimal PriceOpen { get; set; }
                new_stock_item.PriceOpen = today_file_price_sql.OpenPrice * stockFileContentLine.Quantity;

                //public decimal DayChange { get; set; }
                new_stock_item.DayChange = new_stock_item.Worth - new_stock_item.PriceOpen;

                new_stock_item.ChangeSinceBuy = new_stock_item.Worth - (new_stock_item.BuyCost * stockFileContentLine.Quantity);

                //public int DaysFromPurchase { get; set; }
                new_stock_item.DaysFromPurchase = (int)(DateTime.Now - ((DateTime)stockFileContentLine.BuyDate)).TotalDays;
            }
            return new_stock_item;
        }

        private List<StockReturnItem> return_results_for_single_line(StockFileContentLine symbol)
        {
            List<StockReturnItem> combined_data = new List<StockReturnItem>();
            try
            {
                combined_data.AddRange(getStockInfoFromLatest(symbol));
                combined_data.AddRange(getStockInfoFromWeekly(symbol));
            }
            catch (Exception ex)
            {
                throw new Exception($"Processing {symbol.Ticker} Failed\n{ex.InnerException?.Message}");
            }
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
                List<StockDataPoint> stockDataPoints = new List<StockDataPoint>();

                Task task = Task.Run(async () =>
                {
                    ICollection<SymbolSearchMatch> searchMatches = await stocksClient.SearchSymbolAsync(symbol.Ticker);
                    Interval curr_interval = Interval.Min30;

                    if (searchMatches.FirstOrDefault().Type == "Mutual Fund")
                    {
                        curr_interval = Interval.Daily;
                        InsertTicker(symbol.Ticker, StockOrMutualFundEnum.MutualFund.Value());
                    }
                    else
                    {
                        curr_interval = Interval.Min1;
                        InsertTicker(symbol.Ticker, StockOrMutualFundEnum.Stock.Value());
                    }

                    stockTs = await stocksClient.GetTimeSeriesAsync(symbol.Ticker, curr_interval, OutputSize.Full, isAdjusted: false);
                    if (stockTs == null) throw new AccessViolationException("Stock Access failed");
                    stockDataPoints = stockTs.DataPoints.ToList();
                    pull_intraday_information = ProcessDataForMe(stockTs.DataPoints.ToList());
                });

                if (!task.IsCompleted) Thread.Sleep(global_query_timeout);
                task.Wait();
                if (stockDataPoints.Count > 0)
                    WriteDatapointsToDB(stockDataPoints, symbol);
            }
            else
            {
                InsertTicker(symbol.Ticker, StockOrMutualFundEnum.Money.Value());
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
                List<StockDataPoint> stockDataPoints = new List<StockDataPoint>();

                Task task = Task.Run(async () =>
                {
                    ICollection<SymbolSearchMatch> searchMatches = await stocksClient.SearchSymbolAsync(symbol.Ticker);
                    stockTs = await stocksClient.GetTimeSeriesAsync(symbol.Ticker, Interval.Weekly, OutputSize.Full, isAdjusted: false);
                    if (stockTs == null) throw new AccessViolationException("Stock Access failed");
                    stockDataPoints = stockTs.DataPoints.ToList();
                    pull_weekly_information = ProcessDataForMe(stockTs.DataPoints.ToList());
                });
                if (!task.IsCompleted) Thread.Sleep(global_query_timeout);
                task.Wait();
                if (stockDataPoints.Count > 0)
                    WriteDatapointsToDB(stockDataPoints, symbol);
            }
            return pull_weekly_information;
        }

        private List<StockReturnItem> ProcessDataForMe(List<StockDataPoint> dataPoints)
        {
            List<StockReturnItem> return_items = new List<StockReturnItem>();
            foreach (var item in dataPoints)
            {
                var timeUtc = item.Time;
                TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                DateTime Timestamp = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, easternZone);

                return_items.Add(new StockReturnItem()
                {
                    TimestampUTC = Timestamp,
                    OpenPrice = (double)item.OpeningPrice,
                    HighPrice = (double)item.HighestPrice,
                    LowPrice = (double)item.LowestPrice,
                    ClosePrice = (double)item.ClosingPrice,
                    Volume = (double)item.Volume
                });
            }
            return return_items;
        }
    }

    static class StockEnumUtils
    {
        public static byte Value(this StockProcessor.StockOrMutualFundEnum value)
        {
            switch (value)
            {
                case StockProcessor.StockOrMutualFundEnum.Stock: return 0;
                case StockProcessor.StockOrMutualFundEnum.MutualFund: return 1;
                case StockProcessor.StockOrMutualFundEnum.Money: return 2;
                default: throw new ArgumentOutOfRangeException("value");
            }
        }
    }
}
