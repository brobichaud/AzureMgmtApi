using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace Digimarc.Tools
{
	public class AzureMgmtApi
	{
		private const string _apiVersion = "2012-03-01";
		private static readonly XNamespace _nsAzure = "http://schemas.microsoft.com/windowsazure";

		/// <summary>
		/// Gets or sets the certificate to be used
		/// </summary>
		public X509Certificate2 Certificate { get; set; }

		/// <summary>
		/// Gets or sets the subscription id
		/// </summary>
		public string SubscriptionId { get; set; }

		public void Sample(string subscriptionId, string thumbprint, string requestId)
		{
			try
			{
				SubscriptionId = subscriptionId;
				Certificate = GetCertificate(thumbprint);
				OperationResult result = PollGetOperationStatus(requestId, 5, 300);
				DisplayOpResult(result, requestId);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception: " + ex);
			}
		}

		public void DisplayOpResult(OperationResult result, string reqId)
		{
			switch (result.Status)
			{
				case OperationStatus.TimedOut:
					Console.WriteLine("Poll of Get Operation Status timed out: Operation {0} is still in progress after {1}.",
											reqId, result.RunningTime.ToString(@"hh\:mm\:ss"));
					break;

				case OperationStatus.Failed:
					Console.WriteLine("Failed: Operation {0} failed after {1} with status {2} ({3}) - {4}: {5}",
											reqId, result.RunningTime.ToString(@"hh\:mm\:ss"), (int)result.StatusCode, result.StatusCode,
											result.Code, result.Message);
					break;

				case OperationStatus.Succeeded:
					Console.WriteLine("Succeeded: Operation {0} completed after {1} with status {2} ({3})",
											reqId, result.RunningTime.ToString(@"hh\:mm\:ss"), (int)result.StatusCode, result.StatusCode);
					break;
			}
		}

		/// <summary>
		/// Polls Get Operation Status for the operation specified by requestId every pollSeconds until
		/// timeoutSeconds have passed or the operation has returned a Failed or Succeeded status. 
		/// </summary>
		/// <param name="requestId">The requestId of the operation to get status for</param>
		/// <param name="pollSeconds">The interval between calls to Get Operation Status</param>
		/// <param name="timeoutSeconds">The maximum number of seconds to poll</param>
		/// <returns>An OperationResult structure with status or error information</returns>
		public OperationResult PollGetOperationStatus(string requestId, int pollSeconds, int timeoutSeconds)
		{
			var result = new OperationResult();
			var beginPollTime = DateTime.UtcNow;
			var pollInterval = new TimeSpan(0, 0, pollSeconds);
			DateTime endPollTime = beginPollTime + new TimeSpan(0, 0, timeoutSeconds);

			bool done = false;
			while (!done)
			{
				XElement operation = GetOperationStatus(requestId);
				result.RunningTime = DateTime.UtcNow - beginPollTime;
				try
				{
					// Turn the Status string into an OperationStatus value
					result.Status = (OperationStatus)Enum.Parse(typeof(OperationStatus), operation.Element(_nsAzure + "Status").Value);
				}
				catch (Exception)
				{
					throw new ApplicationException(string.Format(
						 "Get Operation Status {0} returned unexpected status:\n{1}", requestId, operation));
				}

				switch (result.Status)
				{
					case OperationStatus.InProgress:
						Console.WriteLine("In progress for: {0}", result.RunningTime.ToString(@"hh\:mm\:ss"));
						Thread.Sleep((int)pollInterval.TotalMilliseconds);
						break;

					case OperationStatus.Failed:
						result.StatusCode = (HttpStatusCode)Convert.ToInt32(operation.Element(_nsAzure + "HttpStatusCode").Value);
						XElement error = operation.Element(_nsAzure + "Error");
						result.Code = error.Element(_nsAzure + "Code").Value;
						result.Message = error.Element(_nsAzure + "Message").Value;
						done = true;
						break;

					case OperationStatus.Succeeded:
						result.StatusCode = (HttpStatusCode)Convert.ToInt32(operation.Element(_nsAzure + "HttpStatusCode").Value);
						done = true;
						break;
				}

				if (!done && DateTime.UtcNow > endPollTime)
				{
					result.Status = OperationStatus.TimedOut;
					done = true;
				}
			}

			return result;
		}

		/// <summary>
		/// Calls the Get Operation Status operation in the Service 
		/// Management REST API for the specified subscription and requestId 
		/// and returns the Operation XML element from the response.
		/// </summary>
		/// <param name="requestId">The requestId of the operation to track</param>
		/// <returns>The Operation XML element from the response</returns>
		public XElement GetOperationStatus(string requestId)
		{
			const string uriFormat = "https://management.core.windows.net/{0}/operations/{1}";
			var uri = new Uri(string.Format(uriFormat, SubscriptionId, requestId));
			XDocument responseBody;
			InvokeRequest(uri, "GET", HttpStatusCode.OK, null, out responseBody);
			return responseBody.Element(_nsAzure + "Operation");
		}

		/// <summary>
		/// A helper function to invoke a Service Management REST API operation.
		/// Throws an ApplicationException on unexpected status code results.
		/// </summary>
		/// <param name="uri">The URI of the operation to invoke using a web request.</param>
		/// <param name="method">The method of the web request, GET, PUT, POST, or DELETE.</param>
		/// <param name="expectedCode">The expected status code.</param>
		/// <param name="requestBody">The XML body to send with the web request. Use null to send no request body.</param>
		/// <param name="responseBody">The XML body returned by the request, if any.</param>
		/// <returns>The requestId returned by the operation.</returns>
		public string InvokeRequest(Uri uri, string method, HttpStatusCode expectedCode, XDocument requestBody, out XDocument responseBody)
		{
			responseBody = null;
			string requestId = String.Empty;

			var request = (HttpWebRequest)WebRequest.Create(uri);
			request.Method = method;
			request.Headers.Add("x-ms-Version", _apiVersion);
			request.ClientCertificates.Add(Certificate);
			request.ContentType = "application/xml";
			request.Accept = "application/xml";

			if (requestBody != null)
			{
				using (Stream requestStream = request.GetRequestStream())
				{
					using (var streamWriter = new StreamWriter(requestStream, System.Text.Encoding.UTF8))
					{
						requestBody.Save(streamWriter, SaveOptions.DisableFormatting);
					}
				}
			}

			HttpWebResponse response;
			var statusCode = HttpStatusCode.Unused;
			try
			{
				response = (HttpWebResponse)request.GetResponse();
			}
			catch (WebException ex)
			{
				// GetResponse throws a WebException for 4XX and 5XX status codes
				response = (HttpWebResponse)ex.Response;
			}

			try
			{
				statusCode = response.StatusCode;
				if (response.ContentLength > 0)
				{
					using (var reader = XmlReader.Create(response.GetResponseStream()))
					{
						responseBody = XDocument.Load(reader);
					}
				}

				if (response.Headers != null)
				{
					requestId = response.Headers["x-ms-request-id"];
				}
			}
			finally
			{
				response.Close();
			}

			if (!statusCode.Equals(expectedCode))
			{
				if (responseBody == null) responseBody = new XDocument();

				throw new ApplicationException(string.Format(
					 "Call to {0} returned an error:\nStatus Code: {1} ({2}):\n{3}", uri, (int)statusCode, statusCode,
					 responseBody.ToString(SaveOptions.OmitDuplicateNamespaces)));
			}

			return requestId;
		}

		/// <summary>
		/// Gets the certificate matching the thumbprint from the local store.
		/// Throws an ArgumentException if a matching certificate is not found.
		/// </summary>
		/// <param name="thumbprint">The thumbprint of the certificate to find</param>
		public static X509Certificate2 GetCertificate(string thumbprint)
		{
			var locations = new List<StoreLocation> 
			{ 
					StoreLocation.CurrentUser, 
					StoreLocation.LocalMachine 
			};

			foreach (var location in locations)
			{
				var store = new X509Store("My", location);
				try
				{
					store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
					X509Certificate2Collection certificates = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
					if (certificates.Count == 1) return certificates[0];
				}
				finally
				{
					store.Close();
				}
			}

			throw new ArgumentException(string.Format("A Certificate with Thumbprint '{0}' could not be located.", thumbprint));
		}
	}

	/// <summary>
	/// The operation status values from PollGetOperationStatus
	/// </summary>
	public enum OperationStatus
	{
		InProgress,
		Failed,
		Succeeded,
		TimedOut
	}

	/// <summary>
	/// The results from PollGetOperationStatus are passed in this struct
	/// </summary>
	public struct OperationResult
	{
		/// <summary>The status: InProgress, Failed, Succeeded, or TimedOut</summary>
		public OperationStatus Status { get; set; }

		/// <summary>The http status code of the requestId operation, if any</summary>
		public HttpStatusCode StatusCode { get; set; }

		/// <summary>The approximate running time for PollGetOperationStatus</summary>
		public TimeSpan RunningTime { get; set; }

		/// <summary>The error code for the failed operation</summary>
		public string Code { get; set; }

		/// <summary>The message for the failed operation</summary>
		public string Message { get; set; }
	}

	/// <summary>
	/// Helpful extension methods for converting strings to and from Base-64.
	/// </summary>
	public static class StringExtensions
	{
		/// <summary>
		/// Converts a UTF-8 string to a Base-64 version of the string.
		/// </summary>
		/// <param name="s">The string to convert to Base-64.</param>
		/// <returns>The Base-64 converted string.</returns>
		public static string ToBase64(this string s)
		{
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(s);
			return Convert.ToBase64String(bytes);
		}

		/// <summary>
		/// Converts a Base-64 encoded string to UTF-8.
		/// </summary>
		/// <param name="s">The string to convert from Base-64.</param>
		/// <returns>The converted UTF-8 string.</returns>
		public static string FromBase64(this string s)
		{
			byte[] bytes = Convert.FromBase64String(s);
			return System.Text.Encoding.UTF8.GetString(bytes);
		}
	}
}