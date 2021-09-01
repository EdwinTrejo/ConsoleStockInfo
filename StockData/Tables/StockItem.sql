CREATE TABLE [StockData].[StockItem]
(
	TickerID INT NOT NULL,
	TimestampUpdate DATETIME NOT NULL,
	Quantity FLOAT NOT NULL,
	Account VARCHAR(MAX) NOT NULL,
	BuyCost DECIMAL NOT NULL,
	BuyDate DATE NULL,
	Price DECIMAL NULL,
	Worth DECIMAL NULL,
	OwnershipHigh DECIMAL NULL,
	OwnershipLow DECIMAL NULL,
	PriceOpen DECIMAL NULL,
	DayChange DECIMAL NULL,
	ChangeSinceBuy DECIMAL NULL,
	DaysFromPurchase INT NULL,
	PRIMARY KEY (TickerID, TimestampUpdate),
	FOREIGN KEY (TickerID) REFERENCES [StockData].[Ticker](TickerID)
)
