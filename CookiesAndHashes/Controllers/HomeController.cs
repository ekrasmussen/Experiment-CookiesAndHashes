﻿using CookiesAndHashes.Models;
using CookiesAndHashes.Services;
using CookiesAndHashes_ClassLibrary;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace CookiesAndHashes.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// When we just make a GET request, we want to just show the login form
        /// </summary>
        /// <returns>The Login View</returns>
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        //When we make a post request from the Login View, we want to try authentication with the info from
        //The username and password text field!
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            //Check if either textbox is empty
            if(string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Message = "Username or Password cannot be empty";
                return View();
            }

            //THIS IS THE AUTHENTICATION PART
            //Replace this with your means of authenticating
            //In our case, i have a API with a mock database that has username and hashed password stored, along with role
            //From here, please look at AuthenticateUser to see how Hashing and Salting works
            User foundUser = await AuthenticateUser(username, password);

            //If we found a user, meaning the user successfully typed in the right username and password, we make a cookie
            //Otherwise we return to the same page with a new ViewBag Message
            if(foundUser == null)
            {
                ViewBag.Message = "Wrong username or password";
                return View();
            }

            //This is the cookie creation, we use claims to insert specific info into the cookie, like role, id etc
            var claims = new List<Claim>
            {
                //We can add more claims to the list, lets add the username, and their role.
                //Claimtypes has default values for name and Role. But you can add whatever you want using strings (its just a Dictionary)
                new Claim(ClaimTypes.Name, foundUser.Username),
                new Claim(ClaimTypes.Role, foundUser.Role)
                //new Claim("AnythingYouWant", "AnyValue");
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            //This is optional configuration
            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,

                //Cookie stays after closing browser
                IsPersistent = true,

                //How long the cookie is valid
                ExpiresUtc = DateTime.UtcNow.AddMinutes(20)
            };

            //finally, sign in time!

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
                );

            return RedirectToAction("Index");
        }

        //And of course we want to be able to log out again
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();

            return RedirectToAction("Index");
        }

        //For testing, we make the privacy page hidden behind the fact you have to be logged in!
        [Authorize]
        public IActionResult Secret()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        //AUTHENTICATING USER USING HASH AND SALT
        public async Task<User> AuthenticateUser(string username, string password)
        {
            UserService userService = new UserService();

            //first we get the user by their Username to retrieve their User model
            User user = await userService.GetByUsername(username);

            if(user != null)
            {
                //Now we will use the salt from that user, along with the typed in password. And generate a hash
                //Meaning the input is the password that we typed in, but the salt is from the user on the database
                string loginHash = HashAndSaltGenerator.GetHash(password, user.Salt);

                //Now we check if that password matches the hashed password from the database user!
                //If it matches, it means we typed in the correct password for that user
                if (loginHash == user.HashedPassword)
                {
                    return user;
                }
            }

            //if password didnt match, or if no user is found. Return nothing
            return null;
        }
    }
}