
namespace Ls_RSI_01.Model
{
    public class StoredProcedures
    {
        //get the last RSI orders
        public const string SqlGetTodaysOeders = "select * from Ls_RSI_01_Orders where date = (select max(date) from Ls_RSI_01_Orders) and userId = '{0}'";

        //calculate new RSI orders
        public const string CalculateTodaysOeders = "exec dbo.ups_Ls_RSI_01_GetRSIOrders {0},{1},{2},{3}";

        //user settings (user's id , user's tws login password, user's tws port, capital , num of orders)
        public const string SqlGetUserSettings = "select * from Ls_RSI_01_UsersSetting where userId = '{0}'";
    }
}
