CREATE TABLE [StockData].[StockFileContentLine]
(
	TickerID INT NOT NULL,
	Quantity FLOAT NOT NULL,
	Account VARCHAR(MAX) NOT NULL,
	BuyCost DECIMAL NOT NULL,
	BuyDate DATE NULL,
	FOREIGN KEY (TickerID) REFERENCES [StockData].[Ticker](TickerID)
)
