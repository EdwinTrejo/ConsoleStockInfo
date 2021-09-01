CREATE PROCEDURE [StockData].[GetLastStockReturnItemPerTickerIDAndDate]
	@TickerID INT,
	@TimestampUTC DATE
AS
	SET NOCOUNT ON;
	SELECT TOP 1
		TickerID AS 'TickerID',
		TimestampUTC AS 'TimestampUTC',
		OpenPrice AS 'OpenPrice',
		ClosePrice AS 'ClosePrice',
		HighPrice AS 'HighPrice',
		LowPrice AS 'LowPrice',
		Volume AS 'Volume'
	FROM [StockData].[StockReturnItem]
	WHERE TickerID = @TickerID
	AND CONVERT(DATE, [TimestampUTC]) = @TimestampUTC
	ORDER BY [TimestampUTC] ASC
RETURN 0
