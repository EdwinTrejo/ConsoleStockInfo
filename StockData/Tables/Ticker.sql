CREATE TABLE [StockData].[Ticker]
(
	TickerID INT IDENTITY(1,1) PRIMARY KEY,
	TickerName VARCHAR(MAX) NOT NULL,
	StockOrMutualFund TINYINT NULL
)
GO

EXEC sp_addextendedproperty  
     @name = N'StockOrMutualFund' 
    ,@value = N'0 for Stock, 1 for Mutual Fund, 2 for Money' 
    ,@level0type = N'Schema', @level0name = 'StockData' 
    ,@level1type = N'Table',  @level1name = 'Ticker' 
    ,@level2type = N'Column', @level2name = 'StockOrMutualFund'
GO