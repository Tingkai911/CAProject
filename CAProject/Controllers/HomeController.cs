using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CAProject.Models;
using CAProject.Db;
using System.Data.Common;
using Microsoft.AspNetCore.Http;

namespace CAProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DbGallery db;

        public HomeController(ILogger<HomeController> logger, DbGallery db)
        {
            _logger = logger;
            this.db = db;
        }

        public async Task<IActionResult> Index(int? pageNumber, string search)
        {
            var products = from p in db.Product
                           select p;

            // Search functionality
            if (!String.IsNullOrEmpty(search))
            {
                products = products.Where(x => x.Name.Contains(search) || x.Description.Contains(search));
            }

            int pageSize = 9;

            // Get Paginated Result
            PaginatedList<Product> paginatedList = await PaginatedList<Product>.CreateAsync(products, pageNumber ?? 1, pageSize);
            ViewData["paginatedList"] = paginatedList;

            // Get stock count for each product
            Dictionary<int, int> paginatedStockCount = new Dictionary<int, int>();
            foreach(Product p in paginatedList)
            {
                paginatedStockCount.Add(p.Id, Convert.ToInt32(db.ActivationCode.Where(
                                                    x => x.ProductId == p.Id && x.IsSold == false).Count()));
            }

            ViewData["paginatedStockCount"] = paginatedStockCount;
            string sessionId = HttpContext.Session.GetString("SessionId");
            ViewData["SessionId"] = sessionId;

            if (sessionId != null)
            {
                int userId = db.Sessions.FirstOrDefault(x => x.SessionId == sessionId).UserId;
                Order order = db.Orders.FirstOrDefault(x => x.UserId == userId && x.IsPaid == false);
                if (order != null)
                {
                    List<Cart> cart = db.Cart.Where(x => x.OrderId == order.Id).ToList();
                    ViewData["Cart"] = cart;
                }
                else
                    ViewData["Cart"] = null;
            }
            
            return View();
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
