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
                List<Cart> cart = db.Cart.Where(x => x.OrderId == order.Id).ToList();
                ViewData["Cart"] = cart;
            }
            
            return View();
        }

        public IActionResult UpdateCart([FromBody] CartInput cartInput)
        {
            // Get the User ID for the current session
            string sessionId = HttpContext.Session.GetString("SessionId");
            string message = "Items have been added to cart";

            // No sessionId = user not logged in = don't allow them to add to cart for now
            if (sessionId == null)
            {
                return Json(new { status = "error" });
            }
            int userId = db.Sessions.FirstOrDefault(x => x.SessionId == sessionId).UserId;

            // Get the product Id from cartInput
            int productId = Convert.ToInt32(cartInput.ProductId);
            int quantity = Convert.ToInt32(cartInput.Quantity);

            // Check if the user currently as an order
            // Create a new order if user currently don't have any order yet
            if (db.Orders.FirstOrDefault(x => x.UserId == userId && x.IsPaid == false) == null)
            {
                db.Orders.Add(new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.Now.ToString()
                });

                db.SaveChanges();
            }

            // Get the order id
            Order order = db.Orders.FirstOrDefault(x => x.UserId == userId && x.IsPaid == false);

            // Check if the current item is already in the cart
            Cart cart = db.Cart.FirstOrDefault(x => x.ProductId == productId && x.OrderId == order.Id);

            if (cart == null)
            {
                cart = new Cart()
                {
                    OrderId = order.Id,
                    ProductId = productId,
                    Quantity = quantity
                };
                db.Cart.Add(cart);
            }
            // Don't allow user to add more that what we have in stock
            else if (cart.Quantity < db.ActivationCode.Where(x => x.ProductId == cart.ProductId && x.IsSold == false).Count())
            {
                cart.Quantity += quantity;
            }
            else
            {
                message = "Reached maximum stock";
            }

            db.SaveChanges();

            return Json(new
            {
                status = "success",
                message
            });
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
