using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ST10382638_CLDV_POE.Controllers
{
    [Authorize(Roles = "Customer, Admin")]
    public class Customer : Controller
    {
        [Authorize(Roles = "Customer")]
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> Register()
        {
            return View();
        }
    }
}
