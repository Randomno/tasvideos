﻿using System.Globalization;
using System.Text;
using System.Web;
using TASVideos.Common;
using TASVideos.Extensions;
using RawAttrNameVal = (string Name, string Value);

namespace TASVideos.ForumEngine;

/// <summary>
/// Provides helpers that the forum engine needs to render markup
/// </summary>
public interface IWriterHelper
{
	/// <summary>
	/// Get the title (display name) of a game.
	/// </summary>
	/// <returns>`null` if not found</returns>
	Task<string?> GetGameTitle(int id);

	/// <summary>
	/// Get the title (name) of a game group.
	/// </summary>
	/// <returns>`null` if not found</returns>
	Task<string?> GetGameGroupTitle(int id);

	/// <summary>
	/// Get the title of a movie.
	/// </summary>
	/// <returns>`null` if not found</returns>
	Task<string?> GetMovieTitle(int id);

	/// <summary>
	/// Get the title of a submission.
	/// </summary>
	/// <returns>`null` if not found</returns>
	Task<string?> GetSubmissionTitle(int id);

	/// <summary>
	/// Get the title of a topic.
	/// </summary>
	/// <returns>`null` if not found</returns>
	Task<string?> GetTopicTitle(int id);
}

public class NullWriterHelper : IWriterHelper
{
	public Task<string?> GetGameTitle(int id) => Task.FromResult<string?>(null);
	public Task<string?> GetGameGroupTitle(int id) => Task.FromResult<string?>(null);
	public Task<string?> GetMovieTitle(int id) => Task.FromResult<string?>(null);
	public Task<string?> GetSubmissionTitle(int id) => Task.FromResult<string?>(null);
	public Task<string?> GetTopicTitle(int id) => Task.FromResult<string?>(null);

	private NullWriterHelper()
	{
	}

	public static readonly NullWriterHelper Instance = new();
}

public interface INode
{
	Task WriteHtml(HtmlWriter w, IWriterHelper h);

	Task WriteMetaDescription(StringBuilder sb, IWriterHelper h);
}

public class Text : INode
{
	public string Content { get; set; } = "";
	public Task WriteHtml(HtmlWriter w, IWriterHelper h)
	{
		w.Text(Content);
		return Task.CompletedTask;
	}

	public Task WriteMetaDescription(StringBuilder sb, IWriterHelper h)
	{
		if (sb.Length >= SiteGlobalConstants.MetaDescriptionLength)
		{
			return Task.CompletedTask;
		}

		sb.Append(Content.RemoveUrls());
		return Task.CompletedTask;
	}
}

public class Element : INode
{
	private static void AddStockAttrsToHyperlink(HtmlWriter w, string targetURI)
	{
		if (UriString.IsToExternalDomain(targetURI))
		{
			w.Attribute("rel", "noopener external"); // for browsers which pre-date `Cross-Origin-Opener-Policy` response header; see https://developer.mozilla.org/en-US/docs/Web/HTML/Element/a#security_and_privacy
		}
	}

	/// <seealso cref="WriteHref"/>
	private static void WriteHyperlink(HtmlWriter w, string labelText, string targetURI, params RawAttrNameVal[] attrs)
	{
		w.OpenTag("a");
		w.Attribute("href", targetURI);
		foreach (var attr in attrs)
		{
			w.Attribute(attr.Name, attr.Value);
		}

		AddStockAttrsToHyperlink(w, targetURI); // done last so `attrs` can override (as the first has precedence when there are duplicates)
		w.Text(labelText);
		w.CloseTag("a");
	}

	/// <seealso cref="WriteHref"/>
	private static void WriteHyperlink(HtmlWriter w, Action writeContents, string targetURI, params RawAttrNameVal[] attrs)
	{
		w.OpenTag("a");
		w.Attribute("href", targetURI);
		foreach (var attr in attrs)
		{
			w.Attribute(attr.Name, attr.Value);
		}

		AddStockAttrsToHyperlink(w, targetURI); // done last so `attrs` can override (as the first has precedence when there are duplicates)
		writeContents();
		w.CloseTag("a");
	}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
	public string Name { get; set; } = "";
	public string Options { get; set; } = "";
	public List<INode> Children { get; set; } = [];
	private string GetChildText()
	{
		var sb = new StringBuilder();
		foreach (var c in Children.Cast<Text>())
		{
			sb.Append(c.Content);
		}

		return sb.ToString();
	}

	private async Task WriteChildren(HtmlWriter w, IWriterHelper h)
	{
		foreach (var c in Children)
		{
			await c.WriteHtml(w, h);
		}
	}

	private async Task WriteSimpleTag(HtmlWriter w, IWriterHelper h, string t)
	{
		w.OpenTag(t);
		await WriteChildren(w, h);
		w.CloseTag(t);
	}

	private async Task WriteSimpleHtmlTag(HtmlWriter w, IWriterHelper h, string t)
	{
		// t looks like `html:b`
		await WriteSimpleTag(w, h, t[5..]);
	}

	private async Task WriteClassyTag(HtmlWriter w, IWriterHelper h, string tag, string clazz)
	{
		w.OpenTag(tag);
		w.Attribute("class", clazz);
		await WriteChildren(w, h);
		w.CloseTag(tag);
	}

	private void TryParseSize(out int? w, out int? h)
	{
		static int? TryParse(string s)
		{
			return int.TryParse(s, out var i) ? i : null;
		}

		var ss = Options.Split('x');
		w = null;
		h = null;

		if (ss.Length > 2)
		{
			return;
		}

		if (ss.Length > 1)
		{
			h = TryParse(ss[1]);
		}

		if (ss.Length > 0)
		{
			w = TryParse(ss[0]);
		}
	}

	/// <seealso cref="WriteHyperlink"/>
	private async Task WriteHref(HtmlWriter w, IWriterHelper h, Func<string, string> transformUrl, Func<string, Task<string>>? transformUrlText)
	{
		if (Options != "")
		{
			WriteHyperlink(
				w,
				writeContents: async () => await WriteChildren(w, h),
				targetURI: transformUrl(Options),
				attrs: transformUrlText is not null ? [
					("title", await transformUrlText(Options))
				] : []);
		}
		else
		{
			// these were all parsed as ChildTagsIfParam, so we're guaranteed to have zero or one text children.
			var text = Children.Cast<Text>().SingleOrDefault()?.Content ?? "";
			if (transformUrlText != null)
			{
				text = await transformUrlText(text);
			}

			WriteHyperlink(w, labelText: text, targetURI: transformUrl(GetChildText()));
		}
	}

	public async Task WriteHtml(HtmlWriter w, IWriterHelper h)
	{
		switch (Name)
		{
			case "b":
			case "i":
			case "u":
			case "s":
			case "sub":
			case "sup":
			case "tt":
			case "table":
			case "tr":
			case "td":
			case "th":
				await WriteSimpleTag(w, h, Name);
				break;
			case "*":
				await WriteSimpleTag(w, h, "li");
				break;
			case "html:b":
			case "html:i":
			case "html:em":
			case "html:u":
			case "html:pre":
			case "html:code":
			case "html:tt":
			case "html:strike":
			case "html:s":
			case "html:del":
			case "html:sup":
			case "html:sub":
			case "html:div":
			case "html:small":
				await WriteSimpleHtmlTag(w, h, Name);
				break;
			case "left":
				await WriteClassyTag(w, h, "div", "a-l");
				break;
			case "center":
				await WriteClassyTag(w, h, "div", "a-c");
				break;
			case "right":
				await WriteClassyTag(w, h, "div", "a-r");
				break;
			case "spoiler":
				await WriteClassyTag(w, h, "span", "spoiler");
				break;
			case "warning":
				await WriteClassyTag(w, h, "div", "warning");
				break;
			case "note":
				await WriteClassyTag(w, h, "div", "forumline");
				break;
			case "highlight":
				await WriteClassyTag(w, h, "span", "highlight");
				break;
			case "quote":
				w.OpenTag("figure");
				if (Options != "")
				{
					w.OpenTag("figcaption");
					await BbParser.Parse(Options, false, true).WriteHtml(w, h);
					w.Text(" wrote:");
					w.CloseTag("figcaption");
				}

				w.OpenTag("blockquote");
				await WriteChildren(w, h);
				w.CloseTag("blockquote");
				w.CloseTag("figure");
				break;
			case "code":
				{
					// If Options is "foo" then that's a language tag.
					// If Options is "foo.bar" then "foo.bar" is a downloadable filename and "bar" is a language tag.
					var osplit = Options.Split('.', StringSplitOptions.RemoveEmptyEntries);
					if (osplit.Length == 2)
					{
						WriteHyperlink(
							w,
							labelText: $"Download {Options}",
							targetURI: $"data:text/plain,{Uri.EscapeDataString(GetChildText())}",
							[
								("class", "btn btn-info code-download"),
								("download", Options),
							]);
					}

					w.OpenTag("pre");

					// "text" is not a supported language for prism,
					// so it will just get the same text formatting as languages, but no syntax highlighting.
					var lang = PrismNames.FixLanguage(osplit.Length > 0 ? osplit[^1] : "text");

					if (lang != "text")
					{
						w.OpenTag("div");
						w.Text("Language: ");
						w.OpenTag("cite");
						w.Text(lang);
						w.CloseTag("cite");
						w.CloseTag("div");
						w.VoidTag("hr");
					}

					w.OpenTag("code");
					w.Attribute("class", $"language-{lang}");
					await WriteChildren(w, h);
					w.CloseTag("code");
					w.CloseTag("pre");
				}

				break;
			case "img":
				{
					w.VoidTag("img");
					TryParseSize(out var width, out var height);
					if (width != null)
					{
						w.Attribute("width", width.ToString()!);
					}

					if (height != null)
					{
						w.Attribute("height", height.ToString()!);
					}

					w.Attribute("src", GetChildText());
					w.Attribute("class", "mw-100");
				}

				break;
			case "url":
				await WriteHref(w, h, s => s, null);
				break;
			case "email":
				await WriteHref(w, h, s => "mailto:" + s, null);
				break;
			case "thread":
				await WriteHref(
					w,
					h,
					url => "/Forum/Topics/" + url,
					async text => (int.TryParse(text, out var id) ? $"Thread #{text}: {await h.GetTopicTitle(id)}" : null) ?? "Thread #" + text);
				break;
			case "post":
				await WriteHref(w, h, s => "/Forum/Posts/" + s, async s => "Post #" + s);
				break;
			case "game":
				await WriteHref(
					w,
					h,
					s => "/" + s + "G",
					async s => (int.TryParse(s, out var id) ? await h.GetGameTitle(id) : null) ?? "Game #" + s);
				break;
			case "gamegroup":
				await WriteHref(
					w,
					h,
					s => "/GameGroups/" + s,
					async s => (int.TryParse(s, out var id) ? await h.GetGameGroupTitle(id) : null) ?? "Game group #" + s);
				break;
			case "movie":
				await WriteHref(
					w,
					h,
					s => "/" + s + "M",
					async s => (int.TryParse(s, out var id) ? await h.GetMovieTitle(id) : null) ?? "Movie #" + s);
				break;
			case "submission":
				await WriteHref(
					w,
					h,
					s => "/" + s + "S",
					async s => (int.TryParse(s, out var id) ? await h.GetSubmissionTitle(id) : null) ?? "Submission #" + s);
				break;
			case "userfile":
				await WriteHref(w, h, s => "/userfiles/info/" + s, async s => "User movie #" + s);
				break;
			case "wiki":
				await WriteHref(w, h, s => "/" + s, async s => "Wiki: " + s);
				break;
			case "frames":
				{
					var ss = GetChildText().Split('@');
					int.TryParse(ss[0], out var n);
					var fps = 60.0;
					if (ss.Length > 1)
					{
						double.TryParse(ss[1], NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo, out fps);
					}

					if (fps <= 0)
					{
						fps = 60.0;
					}

					var timeable = new Timeable
					{
						FrameRate = fps,
						Frames = n
					};
					var time = timeable.Time().ToStringWithOptionalDaysAndHours();

					w.OpenTag("abbr");
					w.Attribute("title", $"{n} Frames @{fps} FPS");
					w.Text(time);
					w.CloseTag("abbr");
					break;
				}

			case "color":
				w.OpenTag("span");

				// TODO: More fully featured anti-style injection
				w.Attribute("style", "color: " + Options.Split(';')[0]);
				await WriteChildren(w, h);
				w.CloseTag("span");
				break;
			case "bgcolor":
				w.OpenTag("span");

				// TODO: More fully featured anti-style injection
				w.Attribute("style", "background-color: " + Options.Split(';')[0]);
				await WriteChildren(w, h);
				w.CloseTag("span");
				break;
			case "size":
				w.OpenTag("span");

				w.Attribute("class", "fontsize");

				// TODO: More fully featured anti-style injection
				var sizeStr = Options.Split(';')[0];
				if (double.TryParse(sizeStr, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo, out var sizeDouble))
				{
					// default font size of the old site was 12px, so if size was given without a unit, divide by 12 and use em
					w.Attribute("style", $"--fs: {(sizeDouble / 12).ToString(CultureInfo.InvariantCulture)}em");
				}
				else
				{
					w.Attribute("style", $"--fs: {sizeStr}");
				}

				await WriteChildren(w, h);
				w.CloseTag("span");
				break;
			case "noparse":
				await WriteChildren(w, h);
				break;
			case "hr":
				w.VoidTag("hr");
				break;
			case "google":
				if (Options == "images")
				{
					WriteHyperlink(
						w,
						labelText: $"Google Images Search: {GetChildText()}",
						targetURI: $"//www.google.com/images?q={Uri.EscapeDataString(GetChildText())}");
				}
				else
				{
					WriteHyperlink(
						w,
						labelText: $"Google Search: {GetChildText()}",
						targetURI: $"//www.google.com/search?q={Uri.EscapeDataString(GetChildText())}");
				}

				break;
			case "video":
				{
					var href = GetChildText();
					if (Uri.IsWellFormedUriString(href, UriKind.Absolute))
					{
						var uri = new Uri(href, UriKind.Absolute);
						var qq = uri.PathAndQuery.Split('?');
						var pp = new VideoParameters(uri.Host, qq[0]);
						if (qq.Length > 1)
						{
							var parsedQuery = HttpUtility.ParseQueryString(qq[1]);

							for (var i = 0; i < parsedQuery.Count; i++)
							{
								var key = parsedQuery.Keys[i];
								if (key != null)
								{
									pp.QueryParams[key] = parsedQuery.GetValues(i)![0];
								}
							}
						}

						TryParseSize(out var width, out var height);
						if (width != null && height != null)
						{
							pp.Width = width;
							pp.Height = height;
						}

						// A bit of a hack:  Since the videowriter uses the HtmlWriter's base writer, we need to get it into
						// the right state first
						w.Text("");
						WriteVideo.Write(w.BaseWriter, pp);
					}

					WriteHyperlink(w, labelText: "Link to video", targetURI: href);
					break;
				}

			case "_root":
				// We want to do <div class=postbody> but that part is handled externally now.
				await WriteChildren(w, h);
				break;
			case "list":
				await WriteSimpleTag(w, h, Options == "1" ? "ol" : "ul");
				break;
			case "html:br":
				w.VoidTag("br");
				break;
			case "html:hr":
				w.VoidTag("hr");
				break;

			default:
				throw new InvalidOperationException("Internal error on tag " + Name);
		}
	}

	private async Task WriteMetaDescriptionTransformOrContent(StringBuilder sb, IWriterHelper h, Func<string, Task<string>> transformUrlText)
	{
		if (Options == "")
		{
			var text = Children.Cast<Text>().SingleOrDefault()?.Content ?? "";
			sb.Append((await transformUrlText(text)).RemoveUrls());
		}
		else
		{
			foreach (var child in Children)
			{
				await child.WriteMetaDescription(sb, h);
			}
		}
	}

	public async Task WriteMetaDescription(StringBuilder sb, IWriterHelper h)
	{
		if (sb.Length >= SiteGlobalConstants.MetaDescriptionLength)
		{
			return;
		}

		switch (Name)
		{
			case "quote":
			case "code":
			case "img":
			case "email":
				break;
			case "thread":
				await WriteMetaDescriptionTransformOrContent(sb, h, async text => (int.TryParse(text, out var id) ? $"Thread #{text}: {await h.GetTopicTitle(id)}" : null) ?? "Thread #" + text);
				break;
			case "post":
				await WriteMetaDescriptionTransformOrContent(sb, h, async s => "Post #" + s);
				break;
			case "game":
				await WriteMetaDescriptionTransformOrContent(sb, h, async s => (int.TryParse(s, out var id) ? await h.GetGameTitle(id) : null) ?? "Game #" + s);
				break;
			case "gamegroup":
				await WriteMetaDescriptionTransformOrContent(sb, h, async s => (int.TryParse(s, out var id) ? await h.GetGameGroupTitle(id) : null) ?? "Game group #" + s);
				break;
			case "movie":
				await WriteMetaDescriptionTransformOrContent(sb, h, async s => (int.TryParse(s, out var id) ? await h.GetMovieTitle(id) : null) ?? "Movie #" + s);
				break;
			case "submission":
				await WriteMetaDescriptionTransformOrContent(sb, h, async s => (int.TryParse(s, out var id) ? await h.GetSubmissionTitle(id) : null) ?? "Submission #" + s);
				break;
			case "userfile":
				await WriteMetaDescriptionTransformOrContent(sb, h, async s => "User movie #" + s);
				break;
			case "wiki":
				await WriteMetaDescriptionTransformOrContent(sb, h, async s => "Wiki: " + s);
				break;
			case "frames":
				{
					var ss = GetChildText().Split('@');
					int.TryParse(ss[0], out var n);
					var fps = 60.0;
					if (ss.Length > 1)
					{
						double.TryParse(ss[1], NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo, out fps);
					}

					if (fps <= 0)
					{
						fps = 60.0;
					}

					var timeable = new Timeable
					{
						FrameRate = fps,
						Frames = n
					};
					var time = timeable.Time().ToStringWithOptionalDaysAndHours();
					sb.Append(time);
					break;
				}

			default:
				foreach (var child in Children)
				{
					await child.WriteMetaDescription(sb, h);
				}

				break;
		}
	}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
