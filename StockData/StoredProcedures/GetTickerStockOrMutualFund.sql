CREATE PROCEDURE [StockData].[GetTickerStockOrMutualFund]
	@Ticker VARCHAR(MAX),
	@StockOrMutualFund TINYINT OUTPUT
AS
	SET NOCOUNT ON;
	SELECT @StockOrMutualFund = StockOrMutualFund
	FROM StockData.Ticker
	Where TickerName = @Ticker
RETURN
GO
