CREATE TABLE [StockData].[StockReturnItem]
(
	TickerID INT NOT NULL,
	TimestampUTC DATETIME NOT NULL,
	OpenPrice FLOAT,
	ClosePrice FLOAT,
	HighPrice FLOAT,
	LowPrice FLOAT,
	Volume BIGINT,
	PRIMARY KEY (TickerID, TimestampUTC),
	FOREIGN KEY (TickerID) REFERENCES [StockData].[Ticker](TickerID)
)
