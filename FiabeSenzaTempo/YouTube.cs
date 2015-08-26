using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Threading.Tasks;
using MyToolkit.Utilities;

namespace FiabeSenzaTempo
{
	public static class YouTube
	{
		#region Video and audio stream retrieval


		private const string BotUserAgent = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";

		/// <summary>Gets the default minimum quality. </summary>
		public const YouTubeQuality DefaultMinQuality = YouTubeQuality.Quality144P;

		/// <summary>Returns the best matching YouTube stream URI which has an audio and video channel and is not 3D. </summary>
		/// <returns>The best matching <see cref="YouTubeUri"/> of the video. </returns>
		/// <exception cref="YouTubeUriNotFoundException">The <see cref="YouTubeUri"/> could not be found. </exception>
		/// <exception cref="WebException">An error occurred while downloading the resource. </exception>
		public static async Task<YouTubeUri> GetVideoUriAsync(string youTubeId, YouTubeQuality minQuality, YouTubeQuality maxQuality, CancellationToken token)
		{
			var uris = await GetUrisAsync(youTubeId, token);
			var uri = TryFindBestVideoUri(uris, minQuality, maxQuality);
			if (uri == null)
				throw new YouTubeUriNotFoundException();
			return uri;
		}

		/// <summary>Returns the best matching YouTube stream URI which has an audio and video channel and is not 3D. </summary>
		/// <returns>The best matching <see cref="YouTubeUri"/> of the video. </returns>
		/// <exception cref="YouTubeUriNotFoundException">The <see cref="YouTubeUri"/> could not be found. </exception>
		/// <exception cref="WebException">An error occurred while downloading the resource. </exception>
		public static Task<YouTubeUri> GetVideoUriAsync(string youTubeId, YouTubeQuality maxQuality)
		{
			return GetVideoUriAsync(youTubeId, DefaultMinQuality, maxQuality, CancellationToken.None);
		}

		/// <summary>Returns the best matching YouTube stream URI which has an audio and video channel and is not 3D. </summary>
		/// <returns>The best matching <see cref="YouTubeUri"/> of the video. </returns>
		/// <exception cref="YouTubeUriNotFoundException">The <see cref="YouTubeUri"/> could not be found. </exception>
		/// <exception cref="WebException">An error occurred while downloading the resource. </exception>
		public static Task<YouTubeUri> GetVideoUriAsync(string youTubeId, YouTubeQuality maxQuality, CancellationToken token)
		{
			return GetVideoUriAsync(youTubeId, DefaultMinQuality, maxQuality, token);
		}

		/// <summary>Returns the best matching YouTube stream URI which has an audio and video channel and is not 3D. </summary>
		/// <returns>The best matching <see cref="YouTubeUri"/> of the video. </returns>
		/// <exception cref="YouTubeUriNotFoundException">The <see cref="YouTubeUri"/> could not be found. </exception>
		/// <exception cref="WebException">An error occurred while downloading the resource. </exception>
		public static Task<YouTubeUri> GetVideoUriAsync(string youTubeId, YouTubeQuality minQuality, YouTubeQuality maxQuality)
		{
			return GetVideoUriAsync(youTubeId, minQuality, maxQuality, CancellationToken.None);
		}

		/// <summary>Returns all available URIs (audio-only and video) for the given YouTube ID. </summary>
		/// <returns>The <see cref="YouTubeUri"/>s of the video. </returns>
		/// <exception cref="WebException">An error occurred while downloading the resource. </exception>
		public static Task<YouTubeUri[]> GetUrisAsync(string youTubeId)
		{
			return GetUrisAsync(youTubeId, CancellationToken.None);
		}

		/// <summary>Returns all available URIs (audio-only and video) for the given YouTube ID. </summary>
		/// <returns>The <see cref="YouTubeUri"/>s of the video. </returns>
		/// <exception cref="WebException">An error occurred while downloading the resource. </exception>
		public static async Task<YouTubeUri[]> GetUrisAsync(string youTubeId, CancellationToken token)
		{
			var urls = new List<YouTubeUri>();
			string javaScriptCode = null;

			var response = await HttpGetAsync("https://www.youtube.com/watch?v=" + youTubeId + "&nomobile=1");
			var match = Regex.Match(response, "url_encoded_fmt_stream_map\": ?\"(.*?)\"");
			var data = Uri.UnescapeDataString(match.Groups[1].Value);
			match = Regex.Match(response, "adaptive_fmts\": ?\"(.*?)\"");
			var data2 = Uri.UnescapeDataString(match.Groups[1].Value);

			var arr = Regex.Split(data + "," + data2, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"); // split by comma but outside quotes
			foreach (var d in arr)
			{
				var url = "";
				var signature = "";
				var tuple = new YouTubeUri();
				foreach (var p in d.Replace("\\u0026", "\t").Split('\t'))
				{
					var index = p.IndexOf('=');
					if (index != -1 && index < p.Length)
					{
						try
						{
							var key = p.Substring(0, index);
							var value = Uri.UnescapeDataString(p.Substring(index + 1));
							if (key == "url")
								url = value;
							else if (key == "itag")
								tuple.Itag = int.Parse(value);
							else if (key == "type" && (value.Contains("video/mp4") || value.Contains("audio/mp4")))
								tuple.Type = value;
							else if (key == "sig")
								signature = value;
							else if (key == "s")
							{
								if (javaScriptCode == null)
								{
									var javaScriptUri = "http://s.ytimg.com/yts/jsbin/html5player-" +
										Regex.Match(response, "\"\\\\/\\\\/s.ytimg.com\\\\/yts\\\\/jsbin\\\\/html5player-(.+?)\\.js\"").Groups[1] + ".js";
									javaScriptCode = await HttpGetAsync(javaScriptUri);
								}

								signature = GenerateSignature(value, javaScriptCode);
							}
						}
						catch (Exception exception)
						{
							Debug.WriteLine("YouTube parse exception: " + exception.Message);
						}
					}
				}

				if (!string.IsNullOrEmpty(url))
				{
					if (url.Contains("&signature=") || url.Contains("?signature="))
						tuple.Uri = new Uri(url, UriKind.Absolute);
					else
						tuple.Uri = new Uri(url + "&signature=" + signature, UriKind.Absolute);

					if (tuple.IsValid)
						urls.Add(tuple);
				}
			}

			return urls.ToArray();
		}

		private static YouTubeUri TryFindBestVideoUri(IEnumerable<YouTubeUri> uris, YouTubeQuality minQuality, YouTubeQuality maxQuality)
		{
			return uris
				.Where(u => u.HasVideo && u.HasAudio && !u.Is3DVideo && u.VideoQuality >= minQuality && u.VideoQuality <= maxQuality)
				.OrderByDescending(u => u.VideoQuality)
				.FirstOrDefault();
		}

		private static string GenerateSignature(string signature, string javaScriptCode)
		{
			var functionNameMatch = Regex.Match(javaScriptCode, @"\.set\s*\(""signature""\s*,\s*([a-zA-Z0-9_$][\w$]*)\(");
			if (functionNameMatch.Groups.Count != 2)
			{
				functionNameMatch = Regex.Match(javaScriptCode, @"\.sig\s*\|\|\s*([a-zA-Z0-9_$][\w$]*)\(");
				if (functionNameMatch.Groups.Count != 2)
					functionNameMatch = Regex.Match(javaScriptCode, @"\.signature\s*=\s*([a-zA-Z_$][\w$]*)\([a-zA-Z_$][\w$]*\)");
			}
			var functionName = functionNameMatch.Groups[1].ToString();
			var functionMath = Regex.Match(javaScriptCode, "function " + Regex.Escape(functionName) + "\\((\\w+)\\)\\{(.+?)\\}", RegexOptions.Singleline);

			var parameterName = Regex.Escape(functionMath.Groups[1].ToString());
			var functionBody = functionMath.Groups[2].ToString();

			Dictionary<string, Func<string, int, string>> methods = null;

			foreach (var line in functionBody.Split(';').Select(l => l.Trim()))
			{
				if (Regex.IsMatch(line, parameterName + "=" + parameterName + "\\.reverse\\(\\)")) // OLD
					signature = Reverse(signature);
				else if (Regex.IsMatch(line, parameterName + "=" + parameterName + "\\.slice\\(\\d+\\)"))
					signature = Slice(signature, Convert.ToInt32(Regex.Match(line, parameterName + "=" + parameterName + "\\.slice\\((\\d+)\\)").Groups[1].ToString()));
				else if (Regex.IsMatch(line, parameterName + "=\\w+\\(" + parameterName + ",\\d+\\)"))
					signature = Swap(signature, Convert.ToInt32(Regex.Match(line, parameterName + "=\\w+\\(" + parameterName + ",(\\d+)\\)").Groups[1].ToString()));
				else if (Regex.IsMatch(line, parameterName + "\\[0\\]=" + parameterName + "\\[\\d+%" + parameterName + "\\.length\\]"))
					signature = Swap(signature, Convert.ToInt32(Regex.Match(line, parameterName + "\\[0\\]=" + parameterName + "\\[(\\d+)%" + parameterName + "\\.length\\]").Groups[1].ToString()));
				else
				{
					var match = Regex.Match(line, "(" + parameterName + "=)?(.*?)\\.(.*?)\\(" + parameterName + ",(.*?)\\)");
					if (match.Success)
					{
						var root = match.Groups[2].ToString();
						var method = match.Groups[3].ToString();
						var parameter = int.Parse(match.Groups[4].ToString());

						if (methods == null)
						{
							// Parse methods
							methods = new Dictionary<string, Func<string, int, string>>();

							var code = Regex.Match(javaScriptCode, "var " + Regex.Escape(root) + "={(.*?)};function").Groups[1].ToString();
							var methodsArray = code.Split(new[] { "}," }, StringSplitOptions.None);
							foreach (var m in methodsArray)
							{
								var arr = m.Split(':');
								var methodName = arr[0];
								var methodBody = arr[1];

								if (methodBody.Contains("reverse()"))
									methods[methodName] = (s, i) => Reverse(s);
								else if (methodBody.Contains(".splice(") || methodBody.Contains(".slice("))
									methods[methodName] = Slice;
								else
									methods[methodName] = Swap;
							}
						}

						signature = methods[method](signature, parameter);
					}
				}
			}
			return signature;
		}

		private static string Reverse(string signature)
		{
			var charArray = signature.ToCharArray();
			Array.Reverse(charArray);
			signature = new string(charArray);
			return signature;
		}

		private static string Slice(string input, int length)
		{
			return input.Substring(length);
		}

		private static string Swap(string input, int position)
		{
			var str = new StringBuilder(input);
			var swapChar = str[position];
			str[position] = str[0];
			str[0] = swapChar;
			return str.ToString();
		}


		/// <exception cref="WebException">An error occurred while downloading the resource. </exception>
		private static Task<string> HttpGetAsync(string uri)
		{
			var task = new TaskCompletionSource<string>();

			var web = new WebClient();
			web.OpenReadCompleted += (sender, args) =>
			{
				if (args.Cancelled)
					task.SetCanceled();
				else if (args.Error != null)
					task.SetException(args.Error);
				else
				{
					var bytes = args.Result.ReadToEnd();
					task.SetResult(Encoding.UTF8.GetString(bytes, 0, bytes.Length));
				}
			};

			web.Headers[HttpRequestHeader.UserAgent] = BotUserAgent;
			web.OpenReadAsync(new Uri(uri));

			return task.Task;
		}


		#endregion

		#region Retrieving title or thumbnail



		/// <summary>Returns a thumbnail for the given YouTube ID. </summary>
		/// <exception cref="ArgumentException">The value of 'size' is not defined. </exception>
		public static Uri GetThumbnailUri(string youTubeId, YouTubeThumbnailSize size = YouTubeThumbnailSize.Medium)
		{
			switch (size)
			{
			case YouTubeThumbnailSize.Small:
				return new Uri("http://img.youtube.com/vi/" + youTubeId + "/default.jpg", UriKind.Absolute);
			case YouTubeThumbnailSize.Medium:
				return new Uri("http://img.youtube.com/vi/" + youTubeId + "/hqdefault.jpg", UriKind.Absolute);
			case YouTubeThumbnailSize.Large:
				return new Uri("http://img.youtube.com/vi/" + youTubeId + "/maxresdefault.jpg", UriKind.Absolute);
			}
			throw new ArgumentException("The value of 'size' is not defined.");
		}

		#endregion

	}

	/// <summary>Exception which occurs when no <see cref="YouTubeUri"/> could be found. </summary>
	public class YouTubeUriNotFoundException : Exception
	{
		internal YouTubeUriNotFoundException()
			: base("No matching YouTube video or audio stream URI could be found. " +
				"The video may not be available in your country, " +
				"is private or uses RTMPE (protected streaming).") { }
	}

	/// <summary>An URI to a YouTube MP4 video or audio stream. </summary>
	public class YouTubeUri
	{
		/// <summary>Gets the Itag of the stream. </summary>
		public int Itag { get; internal set; }

		/// <summary>Gets the <see cref="Uri"/> of the stream. </summary>
		public Uri Uri { get; internal set; }

		/// <summary>Gets the stream type. </summary>
		public string Type { get; internal set; }

		/// <summary>Gets a value indicating whether the stream has audio. </summary>
		public bool HasAudio
		{
			get { return AudioQuality != YouTubeQuality.Unknown && AudioQuality != YouTubeQuality.NotAvailable; }
		}

		/// <summary>Gets a value indicating whether the stream has video. </summary>
		public bool HasVideo
		{
			get { return VideoQuality != YouTubeQuality.Unknown && VideoQuality != YouTubeQuality.NotAvailable; }
		}

		/// <summary>Gets a value indicating whether the stream has 3D video. </summary>
		public bool Is3DVideo
		{
			get
			{
				if (VideoQuality == YouTubeQuality.Unknown)
					return false;

				return Itag >= 82 && Itag <= 85;
			}
		}

		/// <summary>Gets stream's video quality. </summary>
		public YouTubeQuality VideoQuality
		{
			get
			{
				switch (Itag)
				{
				// video & audio
				case 5: return YouTubeQuality.Quality240P;
				case 6: return YouTubeQuality.Quality270P;
				case 17: return YouTubeQuality.Quality144P;
				case 18: return YouTubeQuality.Quality360P;
				case 22: return YouTubeQuality.Quality720P;
				case 34: return YouTubeQuality.Quality360P;
				case 35: return YouTubeQuality.Quality480P;
				case 36: return YouTubeQuality.Quality240P;
				case 37: return YouTubeQuality.Quality1080P;
				case 38: return YouTubeQuality.Quality2160P;

					// 3d video & audio
				case 82: return YouTubeQuality.Quality360P;
				case 83: return YouTubeQuality.Quality480P;
				case 84: return YouTubeQuality.Quality720P;
				case 85: return YouTubeQuality.Quality520P;

					// video only
				case 133: return YouTubeQuality.Quality240P;
				case 134: return YouTubeQuality.Quality360P;
				case 135: return YouTubeQuality.Quality480P;
				case 136: return YouTubeQuality.Quality720P;
				case 137: return YouTubeQuality.Quality1080P;
				case 138: return YouTubeQuality.Quality2160P;
				case 160: return YouTubeQuality.Quality144P;

					// audio only
				case 139: return YouTubeQuality.NotAvailable;
				case 140: return YouTubeQuality.NotAvailable;
				case 141: return YouTubeQuality.NotAvailable;
				}

				return YouTubeQuality.Unknown;
			}
		}

		/// <summary>Gets stream's audio quality. </summary>
		public YouTubeQuality AudioQuality
		{
			get
			{
				switch (Itag)
				{
				// video & audio
				case 5: return YouTubeQuality.QualityLow;
				case 6: return YouTubeQuality.QualityLow;
				case 17: return YouTubeQuality.QualityLow;
				case 18: return YouTubeQuality.QualityMedium;
				case 22: return YouTubeQuality.QualityHigh;
				case 34: return YouTubeQuality.QualityMedium;
				case 35: return YouTubeQuality.QualityMedium;
				case 36: return YouTubeQuality.QualityLow;
				case 37: return YouTubeQuality.QualityHigh;
				case 38: return YouTubeQuality.QualityHigh;

					// 3d video & audio
				case 82: return YouTubeQuality.QualityMedium;
				case 83: return YouTubeQuality.QualityMedium;
				case 84: return YouTubeQuality.QualityHigh;
				case 85: return YouTubeQuality.QualityHigh;

					// video only
				case 133: return YouTubeQuality.NotAvailable;
				case 134: return YouTubeQuality.NotAvailable;
				case 135: return YouTubeQuality.NotAvailable;
				case 136: return YouTubeQuality.NotAvailable;
				case 137: return YouTubeQuality.NotAvailable;
				case 138: return YouTubeQuality.NotAvailable;
				case 160: return YouTubeQuality.NotAvailable;

					// audio only
				case 139: return YouTubeQuality.QualityLow;
				case 140: return YouTubeQuality.QualityMedium;
				case 141: return YouTubeQuality.QualityHigh;
				}
				return YouTubeQuality.Unknown;
			}
		}

		internal bool IsValid
		{
			get { return Uri != null && Uri.ToString().StartsWith("http") && Itag > 0 && Type != null; }
		}
	}

	/// <summary>Enumeration of stream qualities. </summary>
	public enum YouTubeQuality : short
	{
		// video
		Quality144P,
		Quality240P,
		Quality270P,
		Quality360P,
		Quality480P,
		Quality520P,
		Quality720P,
		Quality1080P,
		Quality2160P,

		// audio
		QualityLow,
		QualityMedium,
		QualityHigh,

		NotAvailable,
		Unknown,
	}

	/// <summary>Enumeration of thumbnail sizes. </summary>
	public enum YouTubeThumbnailSize : short
	{
		Small,
		Medium,
		Large
	}

}

