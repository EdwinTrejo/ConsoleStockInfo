CREATE PROCEDURE [StockData].[InsertStockFileContentLine]
	@Ticker VARCHAR(MAX),
	@Quantity FLOAT,
	@Account VARCHAR(MAX),
	@BuyCost DECIMAL,
	@BuyDate DATE = NULL
AS

	IF NOT EXISTS (SELECT * FROM [StockData].[Ticker] WHERE TickerName = @Ticker)
	BEGIN
		INSERT INTO [StockData].[Ticker] (TickerName)
		VALUES (@Ticker)
	END
	
RETURN 0
