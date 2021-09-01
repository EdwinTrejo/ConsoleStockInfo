using System;
using System.Collections.Generic;
using System.Text;
using Gtk;
using StockInfoGui.Structures;
using System.Globalization;
using UI = Gtk.Builder.ObjectAttribute;

namespace StockInfoGui
{
    class MainWindow : Window
    {
        [UI] private MenuBar _menubar1 = null;
        [UI] private Label _label1 = null;

        [UI] private ListBox _listbox1 = null;
        [UI] private ScrolledWindow _scrolledwindow1 = null;

        [UI] private Button _button1 = null;
        [UI] private Button _button2 = null;
        [UI] private Button _button3 = null;

        private bool primary_columns_added = false;
        private bool additional_columns_added = false;

        public string license_file = @"C:\Users\horse\Desktop\license.stock";
        public string listing_file = @"C:\Users\horse\Desktop\listing.stock";

        //public ScrolledWindow sw;
        public ListStore store;
        public TreeView treeView;

        public StockFile stock_file;
        public StockProcessor stock_processor;

        enum Column
        {
            Ticker,
            Quantity,
            Account,
            BuyCost,
            BuyDate,
            Price,
            Worth,
            OwnershipHigh,
            OwnershipLow,
            PriceOpen,
            DayChange,
            ChangeSinceBuy,
            DaysFromPurchase
        }

        public MainWindow() : this(new Builder("MainWindow.glade")) { }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);

            MenuItem file = new MenuItem("File");
            MenuItem file2 = new MenuItem("About");
            Menu filemenu = new Menu();
            Menu filemenu2 = new Menu();
            file.Submenu = filemenu;
            file2.Submenu = filemenu2;

            MenuItem open_lic_file = new MenuItem("Open License File");
            open_lic_file.Activated += OpenLicenseFile;

            MenuItem open_lis_file = new MenuItem("Open Listing File");
            open_lis_file.Activated += OpenListingFile;

            MenuItem exit = new MenuItem("Exit");
            exit.Activated += OnActivated;

            filemenu.Append(open_lic_file);
            filemenu.Append(open_lis_file);
            filemenu.Append(exit);

            MenuItem about = new MenuItem("About");
            about.Activated += AboutMenu;
            filemenu2.Append(about);

            _menubar1.Append(file);
            _menubar1.Append(file2);

            stock_processor = new StockProcessor();

            store = new ListStore
                (
                    typeof(string), //Ticker
                    typeof(string), //Quantity
                    typeof(string), //Account
                    typeof(string), //BuyCost
                    typeof(string), //BuyDate
                    typeof(string), //Price
                    typeof(string), //Worth
                    typeof(string), //OwnershipHigh
                    typeof(string), //OwnershipLow
                    typeof(string), //PriceOpen
                    typeof(string), //DayChange
                    typeof(string), //ChangeSinceBuy
                    typeof(string) //DaysFromPurchase
                );

            treeView = new TreeView(store);
            treeView.Expand = true;
            treeView.HoverExpand = true;
            treeView.EnableSearch = true;
            AddColumns(treeView);

            ShowAll();

            DeleteEvent += Window_DeleteEvent;
            _button1.Clicked += ProcessFile;
            _button2.Clicked += CalculateResults;
            _button3.Clicked += ClearResults;
        }



        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }

        void OnActivated(object sender, EventArgs args)
        {
            Application.Quit();
        }

        void AboutMenu(object sender, EventArgs args)
        {
            AboutDialog about = new AboutDialog();
            about.ProgramName = "StockInfoGui";
            about.Version = "0.0.1";
            about.Copyright = "(c) Edwin Trejo";
            about.Comments = @"Check Your Stocks";
            about.Website = "https://edwingtrejo.com/";
            about.Logo = new Gdk.Pixbuf("icon.png");
            about.Run();
            about.Destroy();
        }

        void OpenLicenseFile(object sender, EventArgs args)
        {
            FileChooserDialog getfile = new FileChooserDialog("Open License File", null, FileChooserAction.Open);
            getfile.AddButton(Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
            getfile.AddButton(Gtk.Stock.Open, Gtk.ResponseType.Ok);
            getfile.DefaultResponse = Gtk.ResponseType.Ok;
            getfile.SelectMultiple = false;
            Gtk.ResponseType response = (Gtk.ResponseType)getfile.Run();
            if (response == Gtk.ResponseType.Ok)
                license_file = getfile.File.Path;
            update_label();
            getfile.Destroy();
        }

        void OpenListingFile(object sender, EventArgs args)
        {
            FileChooserDialog getfile = new FileChooserDialog("Open Listing File", null, FileChooserAction.Open);
            getfile.AddButton(Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
            getfile.AddButton(Gtk.Stock.Open, Gtk.ResponseType.Ok);
            getfile.DefaultResponse = Gtk.ResponseType.Ok;
            getfile.SelectMultiple = false;
            Gtk.ResponseType response = (Gtk.ResponseType)getfile.Run();
            if (response == Gtk.ResponseType.Ok)
                listing_file = getfile.File.Path;
            update_label();
            getfile.Destroy();
        }

        private void ClearResults(object sender, EventArgs a)
        {
            store.Clear();
        }

        private void CalculateResults(object sender, EventArgs a)
        {
            try
            {
                Dictionary<string, string> check_unique_items = new Dictionary<string, string>();
                foreach(var item in stock_file.line_content)
                {
                    if (!check_unique_items.ContainsKey(item.Ticker))
                    {
                        check_unique_items.Add(item.Ticker, item.Ticker);
                    }
                }

                _label1.Text = $"Processing {check_unique_items.Count} items\napproximate time: {check_unique_items.Count} Minutes | {DateTime.Now.AddMinutes(check_unique_items.Count * 3).ToShortTimeString()}";
                stock_processor.Process(stock_file, license_file);
                AddColumnsAfterProcessing(treeView);
                CreateCompleteModel();
                _scrolledwindow1.Add(treeView);
                _scrolledwindow1.WidthRequest = 1200;
                _listbox1.ShowAll();
            }
            catch (Exception e)
            {
                string msg = e.Message;
                string stck_msg = e.StackTrace;
                string innr_msg = (e.InnerException == null) ? string.Empty : e.InnerException.Message;
                string border = "\n------------------------\n";

                MessageDialog md = new MessageDialog(this,
                    DialogFlags.DestroyWithParent, MessageType.Error,
                    ButtonsType.Close, $"Error processing stock{border}{msg}{border}{stck_msg}{border}{innr_msg}{border}");
                md.Run();
                md.Destroy();
            }
        }

        private void ProcessFile(object sender, EventArgs a)
        {
            try
            {
                stock_file = new StockFile(listing_file);
                CreateModel();
                _scrolledwindow1.Add(treeView);
                _scrolledwindow1.HeightRequest = 500;
                _listbox1.ShowAll();
            }
            catch (Exception e)
            {
                MessageDialog md = new MessageDialog(this,
                DialogFlags.DestroyWithParent, MessageType.Error,
                ButtonsType.Close, $"Error processing file\n{e.Message}");
                md.Run();
                md.Destroy();
            }
        }

        void AddColumns(TreeView treeView)
        {
            //typeof(string), //Ticker
            CellRendererText rendererText = new CellRendererText();
            TreeViewColumn column = new TreeViewColumn("Ticker", rendererText,
                "text", Column.Ticker);
            column.SortColumnId = (int)Column.Ticker;
            column.Expand = true;
            treeView.AppendColumn(column);

            //typeof(string), //Quantity
            rendererText = new CellRendererText();
            column = new TreeViewColumn("Quantity", rendererText,
                "text", Column.Quantity);
            column.SortColumnId = (int)Column.Quantity;
            column.Expand = true;
            treeView.AppendColumn(column);

            //typeof(string), //Account
            rendererText = new CellRendererText();
            column = new TreeViewColumn("Account", rendererText,
                "text", Column.Account);
            column.SortColumnId = (int)Column.Account;
            column.Expand = true;
            treeView.AppendColumn(column);

            //typeof(string), //BuyCost
            rendererText = new CellRendererText();
            column = new TreeViewColumn("BuyCost", rendererText,
                "text", Column.BuyCost);
            column.SortColumnId = (int)Column.BuyCost;
            column.Expand = true;
            treeView.AppendColumn(column);

            //typeof(string)  //BuyDate
            rendererText = new CellRendererText();
            column = new TreeViewColumn("BuyDate", rendererText,
                "text", Column.BuyDate);
            column.SortColumnId = (int)Column.BuyDate;
            column.Expand = true;
            treeView.AppendColumn(column);
            primary_columns_added = true;
        }

        void AddColumnsAfterProcessing(TreeView treeView)
        {
            if (!additional_columns_added && primary_columns_added)
            {
                //typeof(string)  //Price
                CellRendererText rendererText = new CellRendererText();
                TreeViewColumn column = new TreeViewColumn("Price", rendererText,
                    "text", Column.Price);
                column.SortColumnId = (int)Column.Price;
                column.Expand = true;
                treeView.AppendColumn(column);

                //typeof(string)  //Worth
                rendererText = new CellRendererText();
                column = new TreeViewColumn("Worth", rendererText,
                    "text", Column.Worth);
                column.SortColumnId = (int)Column.Worth;
                column.Expand = true;
                treeView.AppendColumn(column);
                additional_columns_added = true;

                //typeof(string)  //OwnershipHigh
                rendererText = new CellRendererText();
                column = new TreeViewColumn("OwnershipHigh", rendererText,
                    "text", Column.OwnershipHigh);
                column.SortColumnId = (int)Column.OwnershipHigh;
                column.Expand = true;
                treeView.AppendColumn(column);

                //typeof(string)  //OwnershipLow
                rendererText = new CellRendererText();
                column = new TreeViewColumn("OwnershipLow", rendererText,
                    "text", Column.OwnershipLow);
                column.SortColumnId = (int)Column.OwnershipLow;
                column.Expand = true;
                treeView.AppendColumn(column);

                //typeof(string)  //PriceOpen
                rendererText = new CellRendererText();
                column = new TreeViewColumn("PriceOpen", rendererText,
                    "text", Column.PriceOpen);
                column.SortColumnId = (int)Column.PriceOpen;
                column.Expand = true;
                treeView.AppendColumn(column);

                //typeof(string)  //DayChange
                rendererText = new CellRendererText();
                column = new TreeViewColumn("DayChange", rendererText,
                    "text", Column.DayChange);
                column.SortColumnId = (int)Column.DayChange;
                column.Expand = true;
                treeView.AppendColumn(column);

                //typeof(string)  //ChangeSinceBuy
                rendererText = new CellRendererText();
                column = new TreeViewColumn("ChangeSinceBuy", rendererText,
                    "text", Column.ChangeSinceBuy);
                column.SortColumnId = (int)Column.ChangeSinceBuy;
                column.Expand = true;
                treeView.AppendColumn(column);

                //typeof(string)  //DaysFromPurchase
                rendererText = new CellRendererText();
                column = new TreeViewColumn("DaysFromPurchase", rendererText,
                    "text", Column.DaysFromPurchase);
                column.SortColumnId = (int)Column.DaysFromPurchase;
                column.Expand = true;
                treeView.AppendColumn(column);
            }
        }

        void CreateModel()
        {
            store.Clear();
            foreach (StockFileContentLine item in stock_file.line_content)
            {
                string BuyCost = item.BuyCost.ToString("C", CultureInfo.CurrentCulture);
                string BuyDate = item.BuyDate == null ? string.Empty : ((DateTime)item.BuyDate).ToShortDateString();
                string Quantity = item.Quantity.ToString();
                store.AppendValues(item.Ticker, Quantity, item.Account, BuyCost, BuyDate);
            }
        }

        void CreateCompleteModel()
        {
            store.Clear(); 
            foreach (Structures.StockItem item in stock_processor.file_content)
            {
                string BuyCost = item.BuyCost.ToString("C", CultureInfo.CurrentCulture);
                string BuyDate = item.BuyDate == null ? string.Empty : ((DateTime)item.BuyDate).ToShortDateString();
                string Quantity = item.Quantity.ToString();
                string Price = BuyCost;
                string Worth = BuyCost;
                string OwnershipHigh = BuyCost;
                string OwnershipLow = BuyCost;
                string PriceOpen = BuyCost;
                string DayChange = "0.0";
                string ChangeSinceBuy = "0.0";
                string DaysFromPurchase = "";

                if (item.BuyDate != null)
                {

                    Price = item.Price.ToString("C", CultureInfo.CurrentCulture);
                    Worth = item.Worth.ToString("C", CultureInfo.CurrentCulture);
                    OwnershipHigh = item.OwnershipHigh.ToString("C", CultureInfo.CurrentCulture);
                    OwnershipLow = item.OwnershipLow.ToString("C", CultureInfo.CurrentCulture);
                    PriceOpen = item.PriceOpen.ToString("C", CultureInfo.CurrentCulture);
                    DayChange = item.DayChange.ToString("C", CultureInfo.CurrentCulture);
                    ChangeSinceBuy = item.ChangeSinceBuy.ToString("C", CultureInfo.CurrentCulture);
                    DaysFromPurchase = item.DaysFromPurchase.ToString();
                }

                store.AppendValues(
                    item.Ticker, Quantity, item.Account, BuyCost, BuyDate, Price,
                    Worth, OwnershipHigh, OwnershipLow, PriceOpen, DayChange,
                    ChangeSinceBuy, DaysFromPurchase
                    );
            }
        }

        private string GetLineValues(StockFileContentLine line)
        {
            string BuyCost = line.BuyCost.ToString("C", CultureInfo.CurrentCulture).PadRight(15, ' ');
            string BuyDate = line.BuyDate == null ? string.Empty : line.BuyDate.ToString();
            string newline = $"{line.Ticker.PadRight(5, ' ')}\t{line.Quantity.ToString().PadRight(10, ' ')}\t{line.Account.PadRight(10, ' ')}\t{BuyCost}\t{BuyDate}";
            return newline;
        }

        private void update_label()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("License File: " + license_file);
            sb.Append("Listing File: " + listing_file);
            _label1.Text = sb.ToString();
        }
    }
}
