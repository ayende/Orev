using System;
using System.Web.Mvc;
using Raven.Client;

namespace Orev.Controllers
{
	public abstract class RavenController : Controller
	{
		public const int DefaultPage = 1;
		public const int PageSize = 25;

		public IDocumentSession RavenSession { get; protected set; }

		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			RavenSession = MvcApplication.DocumentStore.OpenSession();
		}

		protected override void OnActionExecuted(ActionExecutedContext filterContext)
		{
			if (filterContext.IsChildAction || RavenSession == null)
				return;

			if (RavenSession == null) // ALREADY CHECKED
				return;

			using (RavenSession)
			{
				if (filterContext.Exception == null)
					RavenSession.SaveChanges();
			}
		}
		
		protected int CurrentPage
		{
			get
			{
				var s = Request.QueryString["page"];
				int result;
				return int.TryParse(s, out result) ? Math.Max(DefaultPage, result) : DefaultPage;
			}
		}

		protected HttpStatusCodeResult HttpNotModified()
		{
			return new HttpStatusCodeResult(304);
		}

		protected HttpStatusCodeResult HttpForbidden()
		{
			return new HttpStatusCodeResult(403);
		}
		
		/*protected ActionResult Xml(XDocument xml, string etag)
		{
			return new XmlResult(xml, etag);
		}

		protected new JsonNetResult Json(object data)
		{
			var settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
			return new JsonNetResult(data, settings);
		}*/
	}
}