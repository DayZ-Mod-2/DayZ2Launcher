using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using DayZ2.DayZ2Launcher.App.Properties;

namespace DayZ2.DayZ2Launcher.App.Core;

public class FileUploader
{
	private readonly HttpClient m_httpClient;
	private readonly string m_challenge;

	public FileUploader(string urlEndpoint, string challenge, HttpClient httpClient)
	{
		m_challenge = challenge;
		m_httpClient = httpClient;
		m_httpClient.BaseAddress = new Uri(urlEndpoint);
		m_httpClient.Timeout = new TimeSpan(0, 0, 30);
	}

	public class UploadFileInfo
	{
		public Stream FileStream { get; set; }
		public string FileName { get; set; }
	}

	private class Case
	{
		public string CaseId;
		public string AuthToken;
	}

	private class ServerRequest
	{
		[JsonPropertyName("datetime")]
		public DateTime DateTime { get; set; }
		[JsonPropertyName("guid")]
		public string Guid { get; set; }
		[JsonPropertyName("client-version")]
		public string ClientVersion { get; set; }
	}

	private class ServerResponse
	{
		[JsonPropertyName("auth-token")]
		public string AuthToken { get; set; }
	}

	private async Task<Case> AnnounceCase(ServerRequest serverRequest, CancellationToken cancellationToken)
	{
		using (var memoryStream = new MemoryStream())
		{
			await JsonSerializer.SerializeAsync(memoryStream, serverRequest, new JsonSerializerOptions { IncludeFields = true, WriteIndented = true }, cancellationToken);
			memoryStream.Seek(0, SeekOrigin.Begin);
				
			var request = new HttpRequestMessage()
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri("announce", UriKind.Relative),
				Content = new StreamContent(memoryStream),
			};
			request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			request.Headers.Add("Challenge", m_challenge);  // TODO: rework this

			using (HttpResponseMessage response = await m_httpClient.SendAsync(request, cancellationToken))
			{
				if (response.StatusCode != HttpStatusCode.Created)
				{
					throw new Exception("Server refused case");
				}

				ServerResponse serverResponse = await JsonSerializer.DeserializeAsync<ServerResponse>(
					await response.Content.ReadAsStreamAsync(cancellationToken),
					cancellationToken: cancellationToken
				);

				return new Case()
				{
					AuthToken = serverResponse?.AuthToken,
					CaseId = response.Headers.Location?.OriginalString,
				};
			}
		}
	}

	private async Task CommitCase(string caseId, string authToken, CancellationToken cancellationToken)
	{
		var request = new HttpRequestMessage()
		{
			Method = HttpMethod.Post,
			RequestUri = new Uri($"{caseId}/commit", UriKind.Relative),
		};
		request.Headers.Add("Authentication", authToken);
		request.Headers.Add("Challenge", m_challenge);  // TODO: rework this

		HttpResponseMessage response = await m_httpClient.SendAsync(request, cancellationToken);
		if (response.StatusCode != HttpStatusCode.OK)
		{
			throw new Exception("Server refused commit");
		}
	}

	private async Task UploadFile(Stream stream, string fileName, string caseId, string authToken, CancellationToken cancellationToken)
	{
		long pos = stream.Position;
		SHA256 sha256 = SHA256.Create();
		byte[] checksum = await sha256.ComputeHashAsync(stream, cancellationToken);
		string hash = Convert.ToBase64String(checksum);
		stream.Position = pos;

		var request = new HttpRequestMessage()
		{
			Method = HttpMethod.Put,
			RequestUri = new Uri($"{caseId}/{fileName}", UriKind.Relative),
			Content = new StreamContent(stream),
		};
		request.Headers.Add("Sha256", hash);
		request.Headers.Add("Authentication", authToken);
		request.Headers.Add("Challenge", m_challenge);  // TODO: rework this

		int tries = 0;
		while (tries < 3)
		{
			HttpResponseMessage response = await m_httpClient.SendAsync(request, cancellationToken);
			switch (response.StatusCode)
			{
				case HttpStatusCode.Conflict:
				{
					await Task.Delay(1000, cancellationToken);
					tries++;
					stream.Position = pos;
					break;
				}
				case HttpStatusCode.Created:
				{
					return;
				}
				default:
				{
					throw new Exception($"Failed to upload file {fileName} with code {response.StatusCode}");
				}
			}
		}
		throw new Exception($"Failed to upload file {fileName} after 3 retries");
	}

	public async Task Upload(IList<UploadFileInfo> files, string dayzVersion, CancellationToken cancellationToken)
	{
		var serverRequest = new ServerRequest()
		{
			DateTime = DateTime.Now,
			Guid = UserSettings.Current.GameOptions.GUID,
			ClientVersion = dayzVersion,
		};
		Case c = await AnnounceCase(serverRequest, cancellationToken);

		foreach (UploadFileInfo fileInfo in files)
		{
			await UploadFile(fileInfo.FileStream, fileInfo.FileName, c.CaseId, c.AuthToken, cancellationToken);
		}

		await CommitCase(c.CaseId, c.AuthToken, cancellationToken);
	}
}