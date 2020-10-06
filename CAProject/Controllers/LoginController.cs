﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAProject.Db;
using CAProject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CAProject.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Auth(string email, string password, [FromServices] DbGallery db)
        {
            Debug.WriteLine($"email: {email} password: {password}");
            User user = db.Users.FirstOrDefault(x =>
                x.Email == email && x.Password == password);

            if (user == null)
            {
                ViewData["errMsg"] = "Email or incorrect password.";
                return View("Index");
            }
            else 
            {
                // session goes here 
                return RedirectToAction("index", "home");
            }

        }
    }
}