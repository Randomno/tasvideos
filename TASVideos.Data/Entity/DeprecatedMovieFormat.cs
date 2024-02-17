﻿namespace TASVideos.Data.Entity;

public class DeprecatedMovieFormat : BaseEntity
{
	public int Id { get; set; }

	[Required]
	[StringLength(8)]
	public string FileExtension { get; set; } = "";

	public bool Deprecated { get; set; } = true;
}
