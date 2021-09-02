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
            SecondaryDark
        }

        Gdk.RGBA[] MaterialColorList = new Gdk.RGBA[Enum.GetNames(typeof(MaterialColors)).Length];

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
        }

        private async void UpdateLabelStatus(object sender, EventArgs a)
        {
            _label1.Text = $"Processing {check_unique_items_count} items\napproximate time: {check_unique_items_count} Minutes | {DateTime.Now.AddMinutes(check_unique_items_count * 3).ToShortTimeString()}";
            ShowAll();
        }

        private async void CalculateResults(object sender, EventArgs a)
        {
            try
            {
                if (!listing_file_processed) throw new Exception("Listing File has not been processed!");

                Task calc_task = Task.Run(async () =>
                {
                    await stock_processor.Process(stock_file, license_file);
                    if (!additional_columns_added) AddColumnsAfterProcessing(treeView);
                    CreateCompleteModel();
                    _scrolledwindow1.Add(treeView);
                    _scrolledwindow1.WidthRequest = 1050;
                    UpdateExtendedColumnColors();
                    _listbox1.ShowAll();
                });
                calc_task.Wait();
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
                _scrolledwindow1.Add(treeView);
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

        void UpdateExtendedColumnColors()
        {
            var treeviewmodel = treeView.Model;
            treeView.Path(out string path, out string rev_path);
            TreePath treePath = new TreePath(path);
            treeView.Model.GetIter(out TreeIter iter, treePath);
            foreach (var column in treeView.Columns)
            {
                if (column.Title == "ChangeSinceBuy")
                    foreach (var cell_val in column.Cells)
                    {
                        treeviewmodel.IterChildren(out TreeIter treeIter);
                        var cell_renderer = (cell_val as Gtk.CellRendererText);
                        RenderArtistName(column, cell_renderer, store, treeIter);
                        //if (sadsda.Text != null && sadsda.Text.Contains('-')) (cell_val as Gtk.CellRendererText).BackgroundRgba = MaterialColorList[MaterialColorEnumUtils.Value(MaterialColors.Secondary)];
                        //var col_item_value = cell_val.Data;
                    }
            }
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
                store.AppendValues(item.Ticker, item.Quantity, item.Account, BuyCost, BuyDate);
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

                /*
                    Gdk.RGBA Primary = new Gdk.RGBA();
                    Gdk.RGBA PrimaryLight = new Gdk.RGBA();
                    Gdk.RGBA PrimaryDark = new Gdk.RGBA();                
                    Gdk.RGBA Secondary = new Gdk.RGBA();
                    Gdk.RGBA SecondaryLight = new Gdk.RGBA();
                    Gdk.RGBA SecondaryDark = new Gdk.RGBA();
                
                    Primary.Alpha = 1;
                    PrimaryLight.Alpha = 1;
                    PrimaryDark.Alpha = 1;
                    Secondary.Alpha = 1;
                    SecondaryLight.Alpha = 1;
                    SecondaryDark.Alpha = 1;

                    //Primary 80c686 129 199 132
                    Primary.Red = (double)129 / (double)255;
                    Primary.Green = (double)199 / (double)255;
                    Primary.Blue = (double)132 / (double)255;
                    //PrimaryLight b1f9b3 177 249 179
                    PrimaryLight.Red = (double)177 / (double)255;
                    PrimaryLight.Green = (double)249 / (double)255;
                    PrimaryLight.Blue = (double)179 / (double)255;
                    //PrimaryDark 509556 80 149 86
                    PrimaryDark.Red = (double)80 / (double)255;
                    PrimaryDark.Green = (double)149 / (double)255;
                    PrimaryDark.Blue = (double)86 / (double)255;
                    //Secondary ef5350 239 83 80
                    Secondary.Red = (double)239 / (double)255;
                    Secondary.Green = (double)83 / (double)255;
                    Secondary.Blue = (double)80 / (double)255;
                    //SecondaryLight ff867c 255 134 124
                    SecondaryLight.Red = (double)255 / (double)255;
                    SecondaryLight.Green = (double)134 / (double)255;
                    SecondaryLight.Blue = (double)124 / (double)255;
                    //SecondaryDark b61827 182 24 39
                    SecondaryDark.Red = (double)182 / (double)255;
                    SecondaryDark.Green = (double)24 / (double)255;
                    SecondaryDark.Blue = (double)39 / (double)255;
                */

                store.AppendValues(
                    item.Ticker, item.Quantity, item.Account, BuyCost, BuyDate, Price,
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
                default: throw new ArgumentOutOfRangeException("value");
            }
        }
    }
}
