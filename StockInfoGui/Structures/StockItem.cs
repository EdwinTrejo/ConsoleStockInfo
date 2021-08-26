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
        public decimal Price { get; set; }
        public decimal Worth { get; set; }
        public decimal OwnershipHigh { get; set; }
        public decimal OwnershipLow { get; set; }
        public decimal PriceOpen { get; set; }
        public decimal DayChange { get; set; }
        public decimal ChangeSinceBuy { get; set; }
        public int DaysFromPurchase { get; set; }
    }

    public class StockReturnItem
    {
        public DateTime Timestamp
        {
            get
            {
                return Timestamp;
            }
            set
            {
                var timeUtc = value;
                TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                Timestamp = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, easternZone);
            }
        }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
    }
}
