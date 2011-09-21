using System;
using System.Web.Mvc;
using System.Web.Security;
using Orev.Helpers;
using Orev.Models;
using Raven.Abstractions.Exceptions;

namespace Orev.Controllers
{
	public class AccountController : RavenController
	{

		//
		// GET: /Account/LogOn

		public ActionResult LogOn()
		{
			return View();
		}

		//
		// POST: /Account/LogOn

		[HttpPost]
		public ActionResult LogOn(LogOnModel model, string returnUrl)
		{
			if (ModelState.IsValid)
			{
				var user = RavenSession.GetUser(model.Login);
				if (user != null && user.Enabled && user.ValidatePassword(model.Password))
				{
					FormsAuthentication.SetAuthCookie(model.Login, model.RememberMe);
					user.LastSeen = DateTime.Now;
					if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
						&& !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
					{
						return Redirect(returnUrl);
					}

					return RedirectToAction("Index", "Home");
				}

				ModelState.AddModelError("", "The user name or password provided is incorrect.");
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

		//
		// GET: /Account/LogOff

		public ActionResult LogOff()
		{
			FormsAuthentication.SignOut();

			return RedirectToAction("Index", "Home");
		}

		//
		// GET: /Account/Register

		public ActionResult Register()
		{
			return View();
		}

		//
		// POST: /Account/Register

		[HttpPost]
		public ActionResult Register(RegisterModel model)
		{
			if (ModelState.IsValid)
			{
				// Attempt to register the user
				RavenSession.Advanced.UseOptimisticConcurrency = true; // make sure we are not overwriting an existing user
				RavenSession.Store(new User
				                   	{
				                   		Id = "users/" + model.Email,
										FirstName = model.FirstName,
										LastName = model.LastName,
										Email = model.Email,
				                   		DateJoined = DateTime.Now,
										Role = Models.User.OperationRoles.Standard,
										Enabled = true,
				                   	}.SetPassword(model.Password));

				try
				{
					RavenSession.SaveChanges();
					RavenSession.Advanced.UseOptimisticConcurrency = false; // NOT NEEDED

					FormsAuthentication.SetAuthCookie(model.Email, false /* createPersistentCookie */);
					return RedirectToAction("Index", "Home");
				}
				catch (ConcurrencyException)
				{
					ModelState.AddModelError("", "A user name for that e-mail address already exists. Please enter a different e-mail address.");
					RavenSession.Dispose(); // MOVE TO BASE CLASS METHOD
					RavenSession = null;// MOVE TO BASE CLASS METHOD
				}

				//ModelState.AddModelError("", "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.");
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

		//
		// GET: /Account/ChangePassword

		[Authorize]
		public ActionResult ChangePassword()
		{
			return View();
		}

		//
		// POST: /Account/ChangePassword

		[Authorize]
		[HttpPost]
		public ActionResult ChangePassword(ChangePasswordModel model)
		{
			if (ModelState.IsValid)
			{
				// ChangePassword will throw an exception rather
				// than return false in certain failure scenarios.
				bool changePasswordSucceeded = false;
				try
				{
					var currentUser = RavenSession.GetUser(User.Identity.Name);
					if (currentUser.ValidatePassword(model.OldPassword))
					{
						currentUser.SetPassword(model.NewPassword);
						RavenSession.SaveChanges(); // WHY CALL THIS HERE?
						changePasswordSucceeded = true;
					}
				}
				catch (Exception) // YOU ARE LOSING EXCEPTION INFO, AT A MIN, LOG THIS
				{
					changePasswordSucceeded = false;
				}

				if (changePasswordSucceeded)
				{
					return RedirectToAction("ChangePasswordSuccess");
				}

				ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

		//
		// GET: /Account/ChangePasswordSuccess

		public ActionResult ChangePasswordSuccess()
		{
			return View();
		}
	}
}
