CREATE PROCEDURE [StockData].[InsertStockReturnItem]
	@TickerID INT,
	@TimestampUTC DATETIME,
	@OpenPrice FLOAT,
	@HighPrice FLOAT,
	@LowPrice FLOAT,
	@ClosePrice FLOAT,
	@Volume BIGINT
AS
	IF NOT EXISTS (SELECT * FROM [StockData].[StockReturnItem] WHERE TickerID = @TickerID AND TimestampUTC = @TimestampUTC)
	BEGIN
		INSERT INTO [StockData].[StockReturnItem]
		(
			TickerID,
			TimestampUTC,
			OpenPrice,
			ClosePrice,
			HighPrice,
			LowPrice,
			Volume
		)
		VALUES 
		(
			@TickerID,
			@TimestampUTC,
			@OpenPrice,
			@ClosePrice,
			@HighPrice,
			@LowPrice,
			@Volume
		)
	END
RETURN
GO
