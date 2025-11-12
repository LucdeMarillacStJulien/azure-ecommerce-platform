namespace ST10382638_CLDV_POE.Models
{
    public static class OrderStatus
    {
        // Edit here only when you add/remove/rename a status
        public static readonly string[] All =
        {
            "Placed",
            "Processed",     // <- canonical spelling
            "Completed",
            "Cancelled"
        };
    }
}
