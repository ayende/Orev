using System;
using System.Linq;
using System.Web.Mvc;
using Orev.Helpers;
using Orev.Infrastructure;
using Orev.Models;

namespace Orev.Controllers
{
    public class JudgeController : RavenController
    {
		[HttpGet]
		[Authorize]
		public ActionResult Index(int? topicId, int? corpusId, string lang)
		{
			// Go to topic selection if none selected
			if (topicId == null || topicId <= 0)
			{
				if (!string.IsNullOrEmpty(lang) || corpusId > 0)
					return RedirectToAction("SelectTopic", new {corpusId = corpusId, lang = lang});
				return RedirectToAction("SelectLanguage");
			}

			// Get the actual topic to get its language identifier
			var topic = RavenSession.Load<Topic>(topicId.Value);
			if (topic == null || string.IsNullOrWhiteSpace(topic.Language))
			{
				ViewBag.Message = "No such topic exists";
				return RedirectToAction("SelectTopic", new { corpusId = corpusId, lang = lang });
			}
			lang = topic.Language;
			ViewBag.Topic = topic;

			// Load the requested corpus if specified, failsafe below
			if (corpusId > 0)
			{
				var corpus = RavenSession.Load<Corpus>(corpusId.Value);
				if (corpus != null && lang.Equals(corpus.Language, StringComparison.InvariantCultureIgnoreCase))
				{
					ViewBag.Corpus = corpus;
				}
			}

			// If no corpus was selected, auto-select one matching the topic's language
			if (ViewBag.Corpus == null)
			{
				var corpus = RavenSession.Query<Corpus>()
					.Where(x => x.Language == lang)
					.FirstOrDefault();

				if (corpus == null)
				{
					ViewData.ModelState.AddModelError("NoCorpus", "No corpus was found for the selected language");
				}
				else
				{
					ViewBag.Corpus = corpus;
				}
			}

			return View(topic);
		}

		//[HttpPost]
		[Authorize]
		public JsonResult SaveJudgment(string topicId, string corpusId, string docId, string jdgmnt)
		{
			Judgment.Verdict verdict;
			if (!string.IsNullOrWhiteSpace(jdgmnt) && Enum.TryParse(jdgmnt, out verdict))
			{
				// TODO: input validation

				if (verdict != Judgment.Verdict.Skip)
				{
					var j = new Judgment
					        	{
					        		CorpusId = corpusId,
					        		TopicId = topicId,
					        		DocumentId = docId,
					        		UserJudgement = verdict,
					        		UserId = "users/" + User.Identity.Name
					        	};

					RavenSession.Store(j);
					RavenSession.SaveChanges();// WHY CALL SaveChanges() here?
				}
			}

			var query = RavenSession.Advanced.LuceneQuery<CorpusDocument, CorpusDocuments_ByNextUnrated>()
				.Where("Topics:*")
				.AndAlso()
				.Not
				.WhereEquals("Topics", topicId);

			// TODO: Make this optional
			query = query.AndAlso().WhereEquals("CorpusId", corpusId);

			// Topics:* AND -Topics:topics/1 AND CorpusId:corpus/1

			// TODO: Introduce randomization

// WHY do you care for WaitForNonStaleResults here? You just get a document, you don't care which
			var nextDoc = query.WaitForNonStaleResultsAsOfNow(TimeSpan.FromSeconds(10)).FirstOrDefault();
			if (nextDoc == null)
				return Json(new { corpusDone = true, });

			return Json(new
			            	{
			            		docId = nextDoc.Id,
								docContents = nextDoc.Content,
								docTitle = nextDoc.Title,
			            	});
		}

    	[HttpGet]
		[Authorize]
		public ActionResult SelectLanguage()
		{
			var langs = RavenSession.Advanced.DatabaseCommands.GetTerms("TopicsBasicIndex", "Language", null, 50);
			if (langs.Count() == 0)
				ViewBag.Message = "No languages were found. Please add a new topic";

			return View(langs);
		}

		[HttpGet]
		[Authorize]
		public ActionResult SelectTopic(int? corpusId, string lang)
		{
			Corpus corpus = null;
			if (corpusId > 0)
			{
				corpus = RavenSession.Load<Corpus>(corpusId.Value);
				if (corpus != null) lang = corpus.Language;
			}

			var topicsByLang = RavenSession.Query<Topic>()
				.Where(x => x.Language == lang)
				.ToArray();

			ViewBag.CorpusId = corpus == null ? string.Empty : corpus.Id;

			return View(topicsByLang);
		}
    }
}
