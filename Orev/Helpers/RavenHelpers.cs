using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Orev.Helpers
{
	public static class RavenHelpers
	{
		public static int ToIntId(this string id)
		{
			// I assume you did that to avoid MVC routing issues, there are better ways
			// http://weblogs.asp.net/shijuvarghese/archive/2010/06/04/how-to-work-ravendb-id-with-asp-net-mvc-routes.aspx
			return int.Parse(id.Substring(id.LastIndexOf('/') + 1));
		}
	}
}