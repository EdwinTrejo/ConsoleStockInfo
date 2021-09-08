CREATE PROCEDURE [StockData].[DeleteAllData]
AS
	DELETE FROM [StockData].[StockItem]
	DELETE FROM [StockData].[StockReturnItem]
RETURN 0
