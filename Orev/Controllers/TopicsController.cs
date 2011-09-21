using System.Linq;
using System.Web.Mvc;
using Orev.Helpers;
using Orev.Models;

namespace Orev.Controllers
{
    public class TopicsController : RavenController
    {
        //
        // GET: /Topics/

        public ActionResult Index()
        {
        	//WHY Wait?
			var topics = RavenSession.Query<Topic>().Customize(x => x.WaitForNonStaleResultsAsOfLastWrite()).ToArray();
            return View(topics);
        }

		public ActionResult View(int id)
		{
			var topic = RavenSession.Load<Topic>(id);
			if (topic == null)
				return HttpNotFound();

			return View(topic);
		}

		[HttpGet]
		[Authorize]
		public ActionResult Add(string lang)
		{
			return View("Edit", new Topic { Language = (string.IsNullOrWhiteSpace(lang) ? "en-US" : lang) });
		}

		[HttpGet]
		[Authorize]
		public ActionResult Edit(int id)
		{
			var user = RavenSession.GetUser(User.Identity.Name); // Duplicate Code
			if (user == null || user.Role != Models.User.OperationRoles.Admin)
				return HttpForbidden();

			var topic = RavenSession.Load<Topic>(id);
			if (topic == null)
				return HttpNotFound("The requested corpus does not exist.");

			return View(topic);
		}

		[HttpPost]
		[Authorize]
		public ActionResult Update(Topic input)
		{
			if (!ModelState.IsValid)
				return View("Edit", input);

			var topic = string.IsNullOrWhiteSpace(input.Id) ? null : RavenSession.Load<Topic>(input.Id);
			if (topic != null)
			{
				var user = RavenSession.GetUser(User.Identity.Name); // Duplicate code
				if (user == null || user.Role != Models.User.OperationRoles.Admin)
					return HttpForbidden();

				topic.Description = input.Description;
				topic.Language = input.Language;
				topic.Narrator = input.Narrator;
				topic.Title = input.Title;
			}
			else
			{
				input.UserId = "users/" + User.Identity.Name;
				RavenSession.Store(input);
			}

			return RedirectToAction("Index");
		}

    }
}
