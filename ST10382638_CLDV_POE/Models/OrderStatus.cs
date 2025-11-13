// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1

// References:
// ChatGPT, https://chat.openai.com/
// https://www.w3schools.com/cs/index.php
// https://www.w3schools.com/bootstrap/bootstrap_ver.asp
// https://stackoverflow.com/questions

namespace ST10382638_CLDV_POE.Models
{
    /// <summary>
    /// Provides a centralized definition for all valid order statuses.
    /// Modify this list only when adding, renaming, or removing statuses.
    /// </summary>
    public static class OrderStatus
    {
        // Array containing all possible order statuses used across the system.
        public static readonly string[] All =
        {
            "Placed",       // Order has been created and queued.
            "Processed",    // Order has been acknowledged and is being handled.
            "Completed",    // Order fulfillment completed successfully.
            "Cancelled"     // Order has been cancelled by user or admin.
        };
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
