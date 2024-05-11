namespace SELENE_STUDIO.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalPendingOrders { get; set; }
        public int TotalProductsSold { get; set; }
        public Dictionary<int, decimal> TotalPriceSoldOfEachProduct { get; set; }
        public Dictionary<string, decimal> OverallSalesPerMonth { get; set; }
    }
}
