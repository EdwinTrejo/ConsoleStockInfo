using System;
using System.Collections.Generic;
using System.Text;
using Gtk;
using StockInfoGui.Structures;
using System.Globalization;
using UI = Gtk.Builder.ObjectAttribute;
using System.Threading;
using System.Threading.Tasks;

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

        private Dictionary<string, string> check_unique_items;
        private volatile int check_unique_items_count = 0;

        private bool primary_columns_added = false;
        private bool listing_file_processed = false;
        private volatile bool additional_columns_added = false;
        private volatile bool results_calculation_running = false;
        private volatile bool write_to_treeview = false;

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

        public enum MaterialColors
        {
            Primary,
            PrimaryLight,
            PrimaryDark,
            Secondary,
            SecondaryLight,
            SecondaryDark,
            Common
        }

        Gdk.RGBA[] MaterialColorList = new Gdk.RGBA[Enum.GetNames(typeof(MaterialColors)).Length];

        public MainWindow() : this(new Builder("MainWindow.glade")) { }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);

            MenuItem menu_item_file = new MenuItem("File");
            MenuItem menu_item_data = new MenuItem("Data");
            MenuItem menu_item_about = new MenuItem("About");
            Menu menu_file = new Menu();
            Menu menu_data = new Menu();
            Menu menu_about = new Menu();
            menu_item_file.Submenu = menu_file;
            menu_item_data.Submenu = menu_data;
            menu_item_about.Submenu = menu_about;

            MenuItem open_lic_file = new MenuItem("Open License File");
            open_lic_file.Activated += OpenLicenseFile;

            MenuItem open_lis_file = new MenuItem("Open Listing File");
            open_lis_file.Activated += OpenListingFile;

            MenuItem exit = new MenuItem("Exit");
            exit.Activated += OnActivated;

            menu_file.Append(open_lic_file);
            menu_file.Append(open_lis_file);
            menu_file.Append(exit);

            MenuItem menu_item_clear_screen = new MenuItem("Clear Screen");
            MenuItem menu_item_purge_db = new MenuItem("Purge Database");
            menu_item_clear_screen.Activated += ClearResults;
            menu_item_purge_db.Activated += PurgeDB;

            menu_data.Append(menu_item_clear_screen);
            menu_data.Append(menu_item_purge_db);

            MenuItem about = new MenuItem("About");
            about.Activated += AboutMenu;
            menu_about.Append(about);

            _menubar1.Append(menu_item_file);
            _menubar1.Append(menu_item_data);
            _menubar1.Append(menu_item_about);

            //Primary 80c686 129 199 132
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Primary)].Alpha = 1;
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Primary)].Red = (double)129 / (double)255;
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Primary)].Green = (double)199 / (double)255;
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Primary)].Blue = (double)132 / (double)255;
            //PrimaryLight b1f9b3 177 249 179
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.PrimaryLight)].Alpha = 1;
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.PrimaryLight)].Red = (double)177 / (double)255;
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.PrimaryLight)].Green = (double)249 / (double)255;
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.PrimaryLight)].Blue = (double)179 / (double)255;
            //PrimaryDark 509556 80 149 86
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.PrimaryDark)].Alpha = 1;
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.PrimaryDark)].Red = (double)80 / (double)255;
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.PrimaryDark)].Green = (double)149 / (double)255;
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.PrimaryDark)].Blue = (double)86 / (double)255;
            //Secondary ef5350 239 83 80
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Secondary)].Alpha = 1;
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Secondary)].Red = (double)239 / (double)255;
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Secondary)].Green = (double)83 / (double)255;
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Secondary)].Blue = (double)80 / (double)255;
            //SecondaryLight ff867c 255 134 124
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.SecondaryLight)].Alpha = 1;
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.SecondaryLight)].Red = (double)255 / (double)255;
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.SecondaryLight)].Green = (double)134 / (double)255;
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.SecondaryLight)].Blue = (double)124 / (double)255;
            //SecondaryDark b61827 182 24 39
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.SecondaryDark)].Alpha = 1;
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.SecondaryDark)].Red = (double)182 / (double)255;
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.SecondaryDark)].Green = (double)24 / (double)255;
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.SecondaryDark)].Blue = (double)39 / (double)255;

            stock_processor = new StockProcessor();

            store = new ListStore
                (
                    typeof(string), //Ticker
                    typeof(double), //Quantity
                    typeof(string), //Account
                    typeof(double), //BuyCost
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
            while (write_to_treeview) { }
            write_to_treeview = true;
            treeView = new TreeView(store);
            treeView.Expand = true;
            treeView.HoverExpand = true;
            write_to_treeview = false;
            treeView.EnableSearch = true;
            AddColumns(treeView);

            ShowAll();

            DeleteEvent += Window_DeleteEvent;
            _button1.Clicked += ProcessFile;
            _button2.Clicked += UpdateLabelStatus;
            _button2.Clicked += CalculateResults;
            //_button2.Clicked += delegate (object sender, EventArgs a)
            //{
            //    Task task = Task.Run(async () =>
            //    {
            //        CalculateResults();
            //    });
            //    UpdateLabelStatus();
            //    task.Wait();
            //};
            //_button2.Clicked += CalculateResults;
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
            while (write_to_treeview) { }
            write_to_treeview = true;
            foreach (var column in treeView.Columns)
            {
                foreach (var cell_val in column.Cells)
                {
                    (cell_val as Gtk.CellRendererText).BackgroundRgba = MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Common)];
                }
            }
            write_to_treeview = false;
        }

        private void PurgeDB(object sender, EventArgs a)
        {
            stock_processor.PurgeData();
        }

        private async void UpdateLabelStatus(object sender, EventArgs a)
        {
            Task calc_task = Task.Run(async () =>
            {
                if (!results_calculation_running)
                {
                    _label1.Text = $"Processing {check_unique_items_count} items\napproximate time: {check_unique_items_count} Minutes | {DateTime.Now.AddMinutes(check_unique_items_count * 3).ToShortTimeString()}";
                    ShowAll();
                }
            });
            //await calc_task;
        }

        private async void CalculateResults(object sender, EventArgs a)
        {
            try
            {
                DateTime rn = DateTime.Now;
                if (!listing_file_processed) throw new Exception("Listing File has not been processed!");
                if (results_calculation_running) throw new Exception("The calculation program is already running in the background!");
                Task calc_task = Task.Run(async () =>
                {
                    results_calculation_running = true;
                    await stock_processor.Process(stock_file, license_file);
                    await Task.Delay(1000);
                    if (!additional_columns_added) AddColumnsAfterProcessing(treeView);
                    CreateCompleteModel();
                    while (write_to_treeview) { }
                    write_to_treeview = true;
                    _scrolledwindow1.Add(treeView);
                    write_to_treeview = false;
                    _scrolledwindow1.WidthRequest = 1050;
                    UpdateExtendedColumnColors();
                    _listbox1.ShowAll();
                    string process_time = $"{(DateTime.Now.Date - rn.Date).TotalHours}:{(DateTime.Now.Date - rn.Date).TotalMinutes}:{(DateTime.Now.Date - rn.Date).TotalSeconds}:{(DateTime.Now.Date - rn.Date).TotalMilliseconds}";
                    _label1.Text = $"Processing Completed in {process_time} HR:MM:SS::MS";
                    results_calculation_running = false;
                });
                //calc_task.Wait();
                await calc_task;
            }
            catch (Exception e)
            {
                string msg = e.Message;
                string stck_msg = e.StackTrace;
                string innr_msg = (e.InnerException == null) ? string.Empty : e.InnerException.Message;
                string border = "\n------------------------\n";

                string errmsg1 = $"{border}{msg}{border}{stck_msg}{border}";
                string errmsg2 = $"{border}{msg}{border}{stck_msg}{border}{innr_msg}{border}";

                MessageDialog md = new MessageDialog(this,
                    DialogFlags.DestroyWithParent, MessageType.Error,
                    ButtonsType.Close, $"Error processing stock{errmsg1}");
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
                while (write_to_treeview) { }
                write_to_treeview = true;
                _scrolledwindow1.Add(treeView);
                write_to_treeview = false;
                _scrolledwindow1.HeightRequest = 500;
                _listbox1.ShowAll();
                check_unique_items = new Dictionary<string, string>();
                foreach (var item in stock_file.line_content)
                {
                    if (!check_unique_items.ContainsKey(item.Ticker))
                    {
                        check_unique_items.Add(item.Ticker, item.Ticker);
                    }
                }
                check_unique_items_count = check_unique_items.Count;

                listing_file_processed = true;
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
            Gdk.RGBA copy_original_rgba= rendererText.CellBackgroundRgba.Copy();
            MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Common)] = copy_original_rgba;
            rendererText.CellBackgroundRgba = MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Primary)];
            TreeViewColumn column = new TreeViewColumn("Ticker", rendererText,
                "text", Column.Ticker);
            column.SortColumnId = (int)Column.Ticker;
            column.Expand = true;
            treeView.AppendColumn(column);

            //typeof(string), //Quantity
            rendererText = new CellRendererText();
            rendererText.CellBackgroundRgba = MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Primary)];
            column = new TreeViewColumn("Quantity", rendererText,
                "text", Column.Quantity);
            column.SortColumnId = (int)Column.Quantity;
            column.Expand = true;
            treeView.AppendColumn(column);

            //typeof(string), //Account
            rendererText = new CellRendererText();
            rendererText.CellBackgroundRgba = MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Primary)];
            column = new TreeViewColumn("Account", rendererText,
                "text", Column.Account);
            column.SortColumnId = (int)Column.Account;
            column.Expand = true;
            treeView.AppendColumn(column);

            //typeof(string), //BuyCost
            rendererText = new CellRendererText();
            rendererText.CellBackgroundRgba = MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Primary)];
            column = new TreeViewColumn("BuyCost", rendererText,
                "text", Column.BuyCost);
            column.SortColumnId = (int)Column.BuyCost;
            column.Expand = true;
            treeView.AppendColumn(column);

            //typeof(string)  //BuyDate
            rendererText = new CellRendererText();
            rendererText.CellBackgroundRgba = MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Primary)];
            column = new TreeViewColumn("BuyDate", rendererText,
                "text", Column.BuyDate);
            column.SortColumnId = (int)Column.BuyDate;
            column.Expand = true;
            treeView.AppendColumn(column);
            primary_columns_added = true;
        }

        void AddColumnsAfterProcessing(TreeView treeView)
        {
            while (write_to_treeview) { }
            write_to_treeview = true;
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
            write_to_treeview = false;
        }

        void UpdateExtendedColumnColors()
        {
            var treeviewmodel = treeView.Model;
            treeView.Path(out string path, out string rev_path);
            TreePath treePath = new TreePath(path);
            treeView.Model.GetIter(out TreeIter iter, treePath);
            double amount_total_overall = 0;
            double amount_total_day = 0;

            foreach (var stock_item_read in stock_processor.file_content)
            {
                amount_total_overall += stock_item_read.ChangeSinceBuy;
                amount_total_day += stock_item_read.DayChange;
            }

            while (write_to_treeview) { }
            write_to_treeview = true;
            foreach (var column in treeView.Columns)
            {
                foreach (var cell_val in column.Cells)
                {
                    switch (column.Title)
                    {
                        case "ChangeSinceBuy":
                            {
                                if (amount_total_overall >= 0)
                                {
                                    if (amount_total_overall <= 5)
                                    {
                                        (cell_val as Gtk.CellRendererText).ForegroundRgba = MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Primary)];
                                    }
                                    else
                                    {
                                        (cell_val as Gtk.CellRendererText).ForegroundRgba = MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.PrimaryDark)];
                                    }
                                }
                                else
                                {
                                    if (amount_total_overall >= -2)
                                    {
                                        (cell_val as Gtk.CellRendererText).ForegroundRgba = MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Secondary)];
                                    }
                                    else
                                    {
                                        (cell_val as Gtk.CellRendererText).ForegroundRgba = MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.SecondaryDark)];
                                    }
                                }
                                break;
                            }
                        case "DayChange":
                            {
                                if (amount_total_day >= 0)
                                {
                                    if (amount_total_day <= 0.5)
                                    {
                                        (cell_val as Gtk.CellRendererText).ForegroundRgba = MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.PrimaryLight)];
                                    }
                                    else
                                    {
                                        (cell_val as Gtk.CellRendererText).ForegroundRgba = MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Primary)];
                                    }
                                }
                                else
                                {
                                    if (amount_total_day >= -0.5)
                                    {
                                        (cell_val as Gtk.CellRendererText).ForegroundRgba = MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.SecondaryLight)];
                                    }
                                    else
                                    {
                                        (cell_val as Gtk.CellRendererText).ForegroundRgba = MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Secondary)];
                                    }
                                }
                                break;
                            }
                    };
                }
            }
            write_to_treeview = false;
        }

        private void RenderArtistName(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.ListStore model, Gtk.TreeIter iter)
        {
            int itemcnt = stock_file.line_content.Count;
            store.IterChildren(out TreeIter mode_iter);

            for (int i = 0; i < itemcnt; i++)
            {
                //foreach (var item in model.getval)
                string str_val = (string)store.GetValue(mode_iter, 10);
                if (str_val.Contains('(') == true)
                {
                    var cellitem = (cell as Gtk.CellRendererText);
                    var cellitemcol = column.Cells;
                    cellitemcol[0].CellBackgroundRgba = MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Secondary)];
                    //(cell as Gtk.CellRendererText).ForegroundRgba = MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Secondary)];
                }
                else
                {
                    var cellitem = (cell as Gtk.CellRendererText);
                    var cellitemcol = column.Cells;
                    cellitemcol[0].CellBackgroundRgba = MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Primary)];
                    //(cell as Gtk.CellRendererText).ForegroundRgba = MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Primary)];
                }
                (cell as Gtk.CellRendererText).Text = str_val;
                store.IterNext(ref mode_iter);
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
                store.AppendValues(item.Ticker, item.Quantity, item.Account, item.BuyCost, BuyDate);
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
                double Price = item.BuyCost * item.Quantity;
                double Worth = item.BuyCost * item.Quantity;
                double OwnershipHigh = item.BuyCost * item.Quantity;
                double OwnershipLow = item.BuyCost * item.Quantity;
                double PriceOpen = item.BuyCost * item.Quantity;
                double DayChange = 0;
                double ChangeSinceBuy = 0;
                string DaysFromPurchase = "";

                if (item.BuyDate != null)
                {
                    Price = item.Price;
                    Worth = item.Worth;
                    OwnershipHigh = item.OwnershipHigh;
                    OwnershipLow = item.OwnershipLow;
                    PriceOpen = item.PriceOpen;
                    DayChange = item.DayChange;
                    ChangeSinceBuy = item.ChangeSinceBuy;
                    DaysFromPurchase = item.DaysFromPurchase.ToString();
                }

                store.AppendValues(
                    item.Ticker, item.Quantity, item.Account, item.BuyCost, BuyDate, Price,
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

    static class MaterialColorEnumUtils
    {
        public static int Value(this MainWindow.MaterialColors value)
        {
            switch (value)
            {
                case MainWindow.MaterialColors.Primary: return 0;
                case MainWindow.MaterialColors.PrimaryLight: return 1;
                case MainWindow.MaterialColors.PrimaryDark: return 2;
                case MainWindow.MaterialColors.Secondary: return 3;
                case MainWindow.MaterialColors.SecondaryLight: return 4;
                case MainWindow.MaterialColors.SecondaryDark: return 5;
                case MainWindow.MaterialColors.Common: return 6;
                default: throw new ArgumentOutOfRangeException("value");
            }
        }
    }
}
