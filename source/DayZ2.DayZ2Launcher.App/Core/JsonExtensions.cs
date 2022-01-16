using System;
using System.Text.Json;

static class JsonExtensions
{
	public static Uri GetUri(this JsonElement json)
	{
		return new Uri(json.GetString());
	}
}
