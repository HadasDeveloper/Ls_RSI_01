select max (date) from [dbo].[Ls_RSI_01_Orders]

select max (date) from [dbo].[Ls_RSI_01_HistoryEndOfDay]

select * from [dbo].[Ls_RSI_01_Values]


exec  [dbo].[ups_Ls_RSI_01_GetRSIOrders] 10,50000,1
exec  [dbo].[ups_Ls_RSI_01_GetRSIOrders] 10,50000,-1  -- reverse direction flags 


select * from [dbo].[Ls_RSI_01_Orders] where date = (select max(date) from [dbo].[Ls_RSI_01_Orders])


