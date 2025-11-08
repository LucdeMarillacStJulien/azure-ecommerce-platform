using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST10382638_CLDV_POE.Data;

namespace ST10382638_CLDV_POE.Controllers
{

    [Authorize(Roles = "Customer, Admin")]
    public class CustomerController : Controller
    {
        private readonly AppDbContext _context;

        public CustomerController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Customer")]
        public IActionResult Main()
        {

            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(int? id)
        {
            var customer = await _context.Customer.ToListAsync();
            return View(customer);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }
    }
}
