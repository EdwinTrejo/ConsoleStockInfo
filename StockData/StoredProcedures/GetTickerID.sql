CREATE PROCEDURE [StockData].[GetTickerID]
	@Ticker VARCHAR(MAX),
	@TickerID INT OUTPUT
AS
	SET NOCOUNT ON;
	SELECT @TickerID = TickerID
	FROM StockData.Ticker
	Where TickerName = @Ticker
RETURN
GO
