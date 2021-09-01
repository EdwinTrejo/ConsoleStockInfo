using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockInfoGui.Structures
{
    public class StockItem : StockFileContentLine
    {
        public double Price { get; set; }
        public double Worth { get; set; }
        public double OwnershipHigh { get; set; }
        public double OwnershipLow { get; set; }
        public double PriceOpen { get; set; }
        public double DayChange { get; set; }
        public double ChangeSinceBuy { get; set; }
        public int DaysFromPurchase { get; set; }
    }

    public class StockReturnItem
    {
        public int TickerID { get; set; }
        public DateTime TimestampUTC { get; set; }
        public double OpenPrice { get; set; }
        public double ClosePrice { get; set; }
        public double HighPrice { get; set; }
        public double LowPrice { get; set; }
        public double Volume { get; set; }
    }
}
