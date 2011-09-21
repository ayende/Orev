using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Security;
using Orev.CorpusReaders.Standard;
using Orev.Helpers;
using Orev.Infrastructure;
using Orev.Models;

namespace Orev.Controllers
{
    public class CorporaController : RavenController
    {
        //
        // GET: /Corpora/

        public ActionResult Index()
        {
        	// DO YOU REALLY CARE ABOUT CONSISTENCY HERE? Why force a wait?
        	var corpora = RavenSession.Query<Corpus>().Customize(x => x.WaitForNonStaleResultsAsOfLastWrite()).ToArray();
            return View("Index", corpora);
        }

		[HttpGet]
		[Authorize]
		public ActionResult Add(string lang)
		{
			return View("Edit", new Corpus { Language = (string.IsNullOrWhiteSpace(lang) ? "en-US" : lang) });
		}

		[HttpGet]
		[Authorize]
		public ActionResult Edit(int id)
		{
			var user = RavenSession.GetUser(User.Identity.Name);
			if (user == null || user.Role != Models.User.OperationRoles.Admin)
				return HttpForbidden();

			var corpus = RavenSession.Load<Corpus>(id);
			if (corpus == null)
				return HttpNotFound("The requested corpus does not exist.");

			return View(corpus);
		}

		[HttpPost]
		[Authorize]
		public ActionResult Update(Corpus input)
		{
			if (!ModelState.IsValid)
				return View("Edit", input);

			var corpus = string.IsNullOrWhiteSpace(input.Id) ? null : RavenSession.Load<Corpus>(input.Id);
			if (corpus != null)
			{
				var user = RavenSession.GetUser(User.Identity.Name);// DUPLICATE CODE, Refactor to a method
				if (user == null || user.Role != Models.User.OperationRoles.Admin)
					return HttpForbidden();

				corpus.Description = input.Description;
				corpus.Language = input.Language;
				corpus.Name = input.Name;
			}
			else
			{
				RavenSession.Store(input);
			}

			return RedirectToAction("Index");
		}

		[HttpGet]
		public ActionResult Details(int id)
		{
			var corpus = RavenSession.Load<Corpus>(id);
			if (corpus == null)
				return HttpNotFound();

			return View(corpus);
		}

		[HttpGet]
		[Authorize]
		public ActionResult FeedDocuments(int corpusId)
		{
			var user = RavenSession.GetUser(User.Identity.Name);// DUPLICATE CODE
			if (user == null || user.Role != Models.User.OperationRoles.Admin)
				return HttpForbidden();

			var corpus = RavenSession.Load<Corpus>(corpusId);
			if (corpus == null)
				return HttpNotFound();

			ViewBag.CorpusName = corpus.Name;

			return View(new CorpusFeedingInput {CorpusId = corpus.Id.ToIntId()});
		}

		[HttpPost]
		[Authorize]
		public ActionResult FeedDocuments(CorpusFeedingInput input)
		{
			var user = RavenSession.GetUser(User.Identity.Name); // DUPLICATE CODE
			if (user == null || user.Role != Models.User.OperationRoles.Admin)
				return HttpForbidden();

			if (!ModelState.IsValid)
			{
				ViewData.ModelState.AddModelError("NoCorpus", "Invalid corpus Id");
				return Index();
			}

			var corpus = RavenSession.Load<Corpus>(input.CorpusId);
			if (corpus == null)
				return HttpNotFound("The requested corpus does not exist.");
			
			var task = new Task(() =>
			                    	{
			                    		var wc = new WebClient {Proxy = null};

			                    		var tempFile = Path.GetTempFileName();
										wc.DownloadFile(input.DocumentsZipUrl, tempFile);

										var reader = new RavenCorpusReader(new ZippedCorpusReader(tempFile){FileEncoding = Encoding.GetEncoding("windows-1255")})
										{
											CorpusId = "corpus/" + input.CorpusId,
											InitialDeployment = true, // TODO
										};

			                    		reader.Read();
			                    	});
			task.Start(); // NO ERROR HNADLING, Will crash the application on an error

			ViewData.ModelState.AddModelError("FeedOperation", "Corpus is being fed with documents in the background");
			return Index();
		}
    }
}
