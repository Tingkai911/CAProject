using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CAProject.Controllers
{
    public class SearchController : Controller
    {
       
        public IActionResult Index()
        {

           
     
           

               //CAProject is Database name
    var products = from p in CAProject.Product
                 select p;

    if (!String.IsNullOrEmpty(searchString))
    {
        products = products.Where(s => s.Name.Contains(searchStr));
    }

    return View("Home/View");

            
        }
    }
}
