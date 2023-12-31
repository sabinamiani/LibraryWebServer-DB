﻿using LibraryWebServer.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Xml.Linq;

namespace LibraryWebServer.Controllers
{
    public class HomeController : Controller
    {

        // WARNING:
        // This very simple web server is designed to be as tiny and simple as possible
        // This is NOT the way to save user data.
        // This will only allow one user of the web server at a time (aside from major security concerns).
        private static string user = "";
        private static int card = -1;

        private readonly ILogger<HomeController> _logger;


        /// <summary>
        /// Given a Patron name and CardNum, verify that they exist and match in the database.
        /// If the login is successful, sets the global variables "user" and "card"
        /// </summary>
        /// <param name="name">The Patron's name</param>
        /// <param name="cardnum">The Patron's card number</param>
        /// <returns>A JSON object with a single field: "success" with a boolean value:
        /// true if the login is accepted, false otherwise.
        /// </returns>
        [HttpPost]
        public IActionResult CheckLogin(string name, int cardnum)
        {
            // TODO: Fill in. Determine if login is successful or not.
            bool loginSuccessful = false;
            using (Team52LibraryContext db = new Team52LibraryContext())
            {
                var query = from patron in db.Patrons
                            where patron.Name == name
                            && patron.CardNum == cardnum
                            select patron;

                if (query.Count() == 1)
                {
                    loginSuccessful = true;
                }
            }

            if (!loginSuccessful)
            {
                return Json(new { success = false });
            }
            else
            {
                user = name;
                card = cardnum;
                return Json(new { success = true });
            }
        }


        /// <summary>
        /// Logs a user out. This is implemented for you.
        /// </summary>
        /// <returns>Success</returns>
        [HttpPost]
        public ActionResult LogOut()
        {
            user = "";
            card = -1;
            return Json(new { success = true });
        }

        /// <summary>
        /// Returns a JSON array representing all known books.
        /// Each book should contain the following fields:
        /// {"isbn" (string), "title" (string), "author" (string), "serial" (uint?), "name" (string)}
        /// Every object in the list should have isbn, title, and author.
        /// Books that are not in the Library's inventory (such as Dune) should have a null serial.
        /// The "name" field is the name of the Patron who currently has the book checked out (if any)
        /// Books that are not checked out should have an empty string "" for name.
        /// </summary>
        /// <returns>The JSON representation of the books</returns>
        [HttpPost]
        public ActionResult AllTitles()
        {
            JsonResult queryJson;
            // TODO: Implement
            using (Team52LibraryContext db = new Team52LibraryContext())
            {
                var query = from book in db.Titles
                            join inv in db.Inventory on book.Isbn equals inv.Isbn into allTitles
                            from at in allTitles.DefaultIfEmpty()
                            join co in db.CheckedOut on at.Serial equals co.Serial into allCheckedOut
                            from aco in allCheckedOut.DefaultIfEmpty()
                            join ptr in db.Patrons on aco.CardNum equals ptr.CardNum into allPatrons
                            from allPtr in allPatrons.DefaultIfEmpty()
                            select new
                            {
                                isbn = book.Isbn,
                                title = book.Title,
                                author = book.Author,
                                serial = at == null ? null : (uint?) at.Serial,
                                name = allPtr == null ? "" : allPtr.Name
                            };
                queryJson = Json(query.ToArray());
            }

            return queryJson;

        }

        /// <summary>
        /// Returns a JSON array representing all books checked out by the logged in user 
        /// The logged in user is tracked by the global variable "card".
        /// Every object in the array should contain the following fields:
        /// {"title" (string), "author" (string), "serial" (uint) (note this is not a nullable uint) }
        /// Every object in the list should have a valid (non-null) value for each field.
        /// </summary>
        /// <returns>The JSON representation of the books</returns>
        [HttpPost]
        public ActionResult ListMyBooks()
        {
            JsonResult queryJson;
            // TODO: Implement
            using (Team52LibraryContext db = new Team52LibraryContext())
            {
                var query = from book in db.Titles
                            join inv in db.Inventory on book.Isbn equals inv.Isbn into allTitles
                            from at in allTitles.DefaultIfEmpty()
                            join co in db.CheckedOut on at.Serial equals co.Serial into allCheckedOut
                            from aco in allCheckedOut.DefaultIfEmpty()
                            where book.Inventory.Any() && aco.CardNum == card
                            select new
                            {
                                title = book.Title,
                                author = book.Author,
                                serial = at.Serial,
                            };
                queryJson = Json(query.ToArray());
            }

            return queryJson;
        }


        /// <summary>
        /// Updates the database to represent that
        /// the given book is checked out by the logged in user (global variable "card").
        /// In other words, insert a row into the CheckedOut table.
        /// You can assume that the book is not currently checked out by anyone.
        /// </summary>
        /// <param name="serial">The serial number of the book to check out</param>
        /// <returns>success</returns>
        [HttpPost]
        public ActionResult CheckOutBook(int serial)
        {
            // You may have to cast serial to a (uint)
            using (Team52LibraryContext db = new Team52LibraryContext())
            {
                CheckedOut book = new CheckedOut();
                book.Serial = (uint) serial;
                book.CardNum = (uint) card;
                db.CheckedOut.Add(book);
                try
                {
                    db.SaveChanges();
                } catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                }
            }

            return Json(new { success = true });
        }

        /// <summary>
        /// Returns a book currently checked out by the logged in user (global variable "card").
        /// In other words, removes a row from the CheckedOut table.
        /// You can assume the book is checked out by the user.
        /// </summary>
        /// <param name="serial">The serial number of the book to return</param>
        /// <returns>Success</returns>
        [HttpPost]
        public ActionResult ReturnBook(int serial)
        {
            // You may have to cast serial to a (uint)
            using (Team52LibraryContext db = new Team52LibraryContext())
            {
                CheckedOut book = new CheckedOut();
                book.Serial = (uint)serial;
                book.CardNum = (uint)card;
                db.CheckedOut.Remove(book);
                try
                {
                    db.SaveChanges();
                } catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                }
               
            }

            return Json(new { success = true });
        }


        /*******************************************/
        /****** Do not modify below this line ******/
        /*******************************************/


        public IActionResult Index()
        {
            if (user == "" && card == -1)
                return View("Login");

            return View();
        }


        /// <summary>
        /// Return the Login page.
        /// </summary>
        /// <returns></returns>
        public IActionResult Login()
        {
            user = "";
            card = -1;

            ViewData["Message"] = "Please login.";

            return View();
        }

        /// <summary>
        /// Return the MyBooks page.
        /// </summary>
        /// <returns></returns>
        public IActionResult MyBooks()
        {
            if (user == "" && card == -1)
                return View("Login");

            return View();
        }

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
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