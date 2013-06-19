using System;
using Krs.Ats.IBNet;

namespace Ls_RSI_01.Model
{
    public class OrderInfo
    {
        public int OrderId { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime Date { get; set; }
        public string Symbol { get; set; }
        public string Direction { get; set; }
        public int Amount { get; set; }
        public int RealAmount { get; set; }
    }

}
