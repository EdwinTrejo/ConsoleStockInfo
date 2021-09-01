CREATE PROCEDURE [StockData].[GetLastStockReturnItemPerTickerID]
	@TickerID INT
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
	ORDER BY [TimestampUTC] DESC
RETURN 0
