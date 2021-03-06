﻿using System;
using System.Net;
using System.Net.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Content;

namespace Flurl.Http
{
	/// <summary>
	/// Encapsulates request, response, and other details associated with an HTTP call. Useful for diagnostics and available in
	/// global event handlers and FlurlHttpException.Call.
	/// </summary>
	public class HttpCall
	{
		private readonly Lazy<Url> _url;

		internal HttpCall(HttpRequestMessage request, FlurlHttpSettings settings) {
			Request = request;
			if (request?.Properties != null)
				request.Properties["FlurlHttpCall"] = this;

			Settings = settings;
			_url = new Lazy<Url>(() => new Url(Request.RequestUri.AbsoluteUri));
		}

		internal static HttpCall Get(HttpRequestMessage request) {
			object obj;
			if (request?.Properties != null && request.Properties.TryGetValue("FlurlHttpCall", out obj) && obj is HttpCall)
				return (HttpCall)obj;
			return null;
		}

		/// <summary>
		/// FlurlHttpSettings used for this call.
		/// </summary>
		public FlurlHttpSettings Settings { get; }

		/// <summary>
		/// HttpRequestMessage associated with this call.
		/// </summary>
		public HttpRequestMessage Request { get; }

		/// <summary>
		/// Captured request body. Available only if Request.Content is a Flurl.Http.Content.CapturedStringContent.
		/// </summary>
		public string RequestBody {
			get {
				var csc = Request.Content as CapturedStringContent;
				if (csc == null)
					throw new FlurlHttpException(this, "RequestBody is only available when Request.Content derives from Flurl.Http.Content.CapturedStringContent.", null);
				return csc.Content;
			}
		}

		/// <summary>
		/// HttpResponseMessage associated with the call if the call completed, otherwise null.
		/// </summary>
		public HttpResponseMessage Response { get; set; }

		/// <summary>
		/// Exception that occurred while sending the HttpRequestMessage.
		/// </summary>
		public Exception Exception { get; set; }
	
		/// <summary>
		/// User code should set this to true inside global event handlers (OnError, etc) to indicate
		/// that the exception was handled and should not be propagated further.
		/// </summary>
		public bool ExceptionHandled { get; set; }

		/// <summary>
		/// DateTime the moment the request was sent.
		/// </summary>
		public DateTime StartedUtc { get; set; }

		/// <summary>
		/// DateTime the moment a response was received.
		/// </summary>
		public DateTime? EndedUtc { get; set; }

		/// <summary>
		/// Total duration of the call if it completed, otherwise null.
		/// </summary>
		public TimeSpan? Duration => EndedUtc - StartedUtc;

		/// <summary>
		/// The URL being called.
		/// </summary>
		public Url Url => _url.Value;

		/// <summary>
		/// True if a response was received, regardless of whether it is an error status.
		/// </summary>
		public bool Completed => Response != null;

		/// <summary>
		/// True if a response with a successful HTTP status was received.
		/// </summary>
		public bool Succeeded => Completed && 
			(Response.IsSuccessStatusCode || HttpStatusRangeParser.IsMatch(Settings.AllowedHttpStatusRange, Response.StatusCode));

		/// <summary>
		/// HttpStatusCode of the response if the call completed, otherwise null.
		/// </summary>
		public HttpStatusCode? HttpStatus => Completed ? (HttpStatusCode?)Response.StatusCode : null;

		/// <summary>
		/// Body of the HTTP response if unsuccessful, otherwise null. (Successful responses are not captured as strings, mainly for performance reasons.)
		/// </summary>
		public string ErrorResponseBody { get; set; }
	}
}
