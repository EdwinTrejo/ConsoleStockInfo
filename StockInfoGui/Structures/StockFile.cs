using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockInfoGui.Structures
{
    public class StockFile
    {
        public string filepath;
        public List<StockFileContentLine> line_content;

        public StockFile(string file_path)
        {
            filepath = file_path;
            line_content = new List<StockFileContentLine>();
            ProcessFile();
        }

        private void ProcessFile()
        {
            if (!File.Exists(filepath)) throw new FileNotFoundException();
            string line;
            StreamReader file = new StreamReader(filepath);

            while ((line = file.ReadLine()) != null)
            {
                try
                {
                    line_content.Add(ProcessLine(line));
                }
                catch { }
            }
            file.Close();
        }

        private StockFileContentLine ProcessLine(string current_line)
        {
            string[] parts = current_line.Split(',');
            string Ticker = parts[0];
            double.TryParse(parts[1], out double Quantity);
            string Account = parts[2];
            double.TryParse(parts[3], out double BuyCost);

            StockFileContentLine new_content = new StockFileContentLine()
            {
                Ticker = Ticker,
                Quantity = Quantity,
                Account = Account,
                BuyCost = BuyCost,
                BuyDate = null
            };

            if (parts.Length == 5)
            {
                DateTime.TryParse(parts[4], out DateTime BuyDate);
                new_content.BuyDate = BuyDate;
            }

            return new_content;
        }
    }

    public class StockFileContentLine
    {
        public string Ticker { get; set; }
        public double Quantity { get; set; }
        public string Account { get; set; }
        public double BuyCost { get; set; }
        public DateTime? BuyDate;
    }
}
