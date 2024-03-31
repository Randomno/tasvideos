﻿using Microsoft.AspNetCore.Mvc.ViewComponents;
using TASVideos.WikiEngine;

namespace TASVideos.WikiModules;

[WikiModule(ModuleNames.ActiveTab)]
public class ActiveTab : ViewComponent
{
	public IViewComponentResult Invoke(string? tab)
	{
		TempData["ActiveTab"] = tab;
		return new ContentViewComponentResult("");
	}
}
