CREATE PROCEDURE [StockData].[GetAllStockReturnItemPerTickerID]
	@TickerID INT
AS
	SELECT
		TickerID AS 'TickerID',
		TimestampUTC AS 'TimestampUTC',
		OpenPrice AS 'OpenPrice',
		ClosePrice AS 'ClosePrice',
		HighPrice AS 'HighPrice',
		LowPrice AS 'LowPrice',
		Volume AS 'Volume'
	FROM [StockData].[StockReturnItem]
	WHERE TickerID = @TickerID
RETURN 0
