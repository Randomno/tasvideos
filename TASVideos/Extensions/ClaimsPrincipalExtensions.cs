using System.Linq;
using System.Security.Claims;

namespace TASVideos.Extensions
{
	public static class CleamsPrincipalExtensions
	{
		public static int UserId(this ClaimsPrincipal user)
		{
			if (user == null || !user.Identity.IsAuthenticated)
			{
				return 0;
			}

			return int.Parse(user.Claims
				.First(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value)
		}
	}
}
