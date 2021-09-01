CREATE PROCEDURE [StockData].[InsertTicker]
	@Ticker VARCHAR(MAX),
	@StockOrMutualFund TINYINT = 0
AS
BEGIN
	IF NOT EXISTS (SELECT * FROM [StockData].[Ticker] WHERE TickerName = @Ticker)
	BEGIN
		INSERT INTO [StockData].[Ticker] 
		(
			TickerName,
			StockOrMutualFund
		)
		VALUES 
		(
			@Ticker,
			@StockOrMutualFund
		)
	END
END