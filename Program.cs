using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using Digimarc.Shared.Classes;
using Digimarc.Tools;

namespace AzureMgmt
{
	class Program
	{
		private const string _certThumbprint = "3D487F020137ECC4B71FE862F530841E5BD34E58";
		private const string _devSubscriptionId = "3ae41414-117a-45f9-89b0-669e91061ae3";
		private const string _devServiceName = "dev-portal";
		private const string _testSubscriptionId = "b5dbbe6e-a144-46cf-a862-e6805db7bdd4";
		private const string _testServiceName = "test-madras";
		private const string _labsSubscriptionId = "60e9300c-8554-4c4c-81c2-84aad1e12c42";
		private const string _labsServiceName = "labs-madras";

		private static void Main(string[] args)
		{
			//DisplayEndpoints();
			GetConfig("production");

			//VipSwap();

			//string config = GetConfig("Production");
			//XElement doc = ChangeConfig(config);
			//UpdateConfig("production", doc);

			//var api = new AzureMgmtApi();
			//api.SubscriptionId = _devSubscriptionId;
			//api.Sample(_certThumbprint, reqId);
		}

		public static string GetConfig(string slot)
		{
			const string _fmtGetProperties = "https://management.core.windows.net/{0}/services/hostedservices/{1}?embed-detail=true";

			try
			{
				var requestUri = new Uri(string.Format(_fmtGetProperties, _devSubscriptionId, _devServiceName));
				var api = new AzureMgmtApi
				{
					SubscriptionId = _devSubscriptionId,
					Certificate = AzureMgmtApi.GetCertificate(_certThumbprint)
				};

				XDocument respBody;
				string reqId = api.InvokeRequest(requestUri, "GET", HttpStatusCode.OK, null, out respBody);
				Console.WriteLine("x-ms-request-id: " + reqId);
				respBody.Save(@"c:\users\brobichaud.corp\desktop\Properties.xml");

				// parse the response
				XNamespace ns = "http://schemas.microsoft.com/windowsazure";
				var configBase64 = (string)(from c in respBody.Descendants(ns + "Deployment")
													 where c.Element(ns + "DeploymentSlot").Value == slot
													 select c.Element(ns + "Configuration")).FirstOrDefault();

				string config = DataEncoder.StringFromBase64(configBase64);
				var xml = new XmlIO() { IndentChars = "  " };
				string xmlConfig = xml.FormatFragment(config);
				Console.WriteLine("\n\nConfig:\n" + xmlConfig);
				File.WriteAllText(@"c:\users\brobichaud.corp\desktop\Config.xml", xmlConfig);
				return config;
			}
			catch (Exception e)
			{
				Console.WriteLine("Error encountered: " + e.Message);
				return null;
			}
		}

		public static XElement ChangeConfig(string config)
		{
			try
			{
				XNamespace ns = "http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration";
				var doc = XElement.Parse(config);
				UpdateAppSetting(doc, ns, "OperationalState", "on");
				UpdateAppSetting(doc, ns, "Api.OperationalState", "on");
				UpdateAppSetting(doc, ns, "ApiV2.OperationalState", "on");
				UpdateAppSetting(doc, ns, "AggregatorEnabled", "true");
				UpdateAppSetting(doc, ns, "ReplicatorEnabled", "true");

				doc.Save(@"c:\users\brobichaud.corp\desktop\ConfigUpdated.xml");

				return doc;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return null;
			}
		}

		public static string UpdateConfig(string slot, XElement docConfig)
		{
			const string _fmtChangeDeployment = "https://management.core.windows.net/{0}/services/hostedservices/{1}/deploymentslots/{2}/?comp=config";

			try
			{
				var config = new StringWriter();
				docConfig.Save(config);

				// build xml body to post
				XNamespace ns = "http://schemas.microsoft.com/windowsazure";
				var doc = new XDocument(new XDeclaration("1.0", "utf-8", "true"),
												new XElement(ns + "ChangeConfiguration",
												new XElement(ns + "Configuration", DataEncoder.ToBase64(config.ToString()))));

				var requestUri = new Uri(string.Format(_fmtChangeDeployment, _devSubscriptionId, _devServiceName, slot));
				var api = new AzureMgmtApi
				{
					SubscriptionId = _devSubscriptionId,
					Certificate = AzureMgmtApi.GetCertificate(_certThumbprint)
				};

				XDocument responseBody;
				string reqId = api.InvokeRequest(requestUri, "POST", HttpStatusCode.Accepted, doc, out responseBody);
				if (!string.IsNullOrWhiteSpace(reqId)) Console.WriteLine("x-ms-request-id: " + reqId);

				OperationResult result = api.PollGetOperationStatus(reqId, 5, 300);
				api.DisplayOpResult(result, reqId);
				return reqId;
			}
			catch (Exception e)
			{
				Console.WriteLine("Error encountered: " + e.Message);
				return null;
			}
		}

		public static void VipSwap()
		{
			const string _fmsVipSwap = "https://management.core.windows.net/{0}/services/hostedservices/{1}";
			string prod, stage;
			GetDeploymentNames(out prod, out stage);
			if (string.IsNullOrWhiteSpace(stage))
			{
				Console.WriteLine("There is nothing in the staging slot");
				return;
			}

			try
			{
				// create the request
				var requestUri = new Uri(string.Format(_fmsVipSwap, _devSubscriptionId, _devServiceName));
				var api = new AzureMgmtApi
				{
					SubscriptionId = _devSubscriptionId,
					Certificate = AzureMgmtApi.GetCertificate(_certThumbprint)
				};

				// build xml body to post
				XNamespace ns = "http://schemas.microsoft.com/windowsazure";
				var doc = new XDocument(new XDeclaration("1.0", "utf-8", "true"), new XElement(ns + "Swap",
								new XElement(ns + "Production", prod),
								new XElement(ns + "SourceDeployment", stage)));
				doc.Save(@"c:\users\brobichaud.corp\desktop\VipSwap.xml");

				XDocument respBody;
				string reqId = api.InvokeRequest(requestUri, "POST", HttpStatusCode.Accepted, doc, out respBody);
				Console.WriteLine("x-ms-request-id: " + reqId);

				OperationResult result = api.PollGetOperationStatus(reqId, 5, 300);
				api.DisplayOpResult(result, reqId);
			}
			catch (Exception e)
			{
				Console.WriteLine("Error encountered: " + e.Message);
			}
		}

		public static void GetDeploymentNames(out string production, out string staging)
		{
			const string _fmtGetProperties = "https://management.core.windows.net/{0}/services/hostedservices/{1}?embed-detail=true";
			production = "";
			staging = "";

			try
			{
				var requestUri = new Uri(string.Format(_fmtGetProperties, _devSubscriptionId, _devServiceName));
				var api = new AzureMgmtApi
				{
					SubscriptionId = _devSubscriptionId,
					Certificate = AzureMgmtApi.GetCertificate(_certThumbprint)
				};

				XDocument respBody;
				string reqId = api.InvokeRequest(requestUri, "GET", HttpStatusCode.OK, null, out respBody);
				Console.WriteLine("x-ms-request-id: " + reqId);
				respBody.Save(@"c:\users\brobichaud.corp\desktop\Properties.xml");

				// parse the response
				XNamespace ns = "http://schemas.microsoft.com/windowsazure";
				production = (string)(from c in respBody.Descendants(ns + "Deployment")
											 where c.Element(ns + "DeploymentSlot").Value == "Production"
											 select c.Element(ns + "Name")).FirstOrDefault();
				staging = (string)(from c in respBody.Descendants(ns + "Deployment")
										where c.Element(ns + "DeploymentSlot").Value == "Staging"
										select c.Element(ns + "Name")).FirstOrDefault();
			}
			catch (Exception e)
			{
				Console.WriteLine("Error encountered: " + e.Message);
			}
		}

		public static void DisplayEndpoints()
		{
			const string _fmtGetProperties = "https://management.core.windows.net/{0}/services/hostedservices/{1}?embed-detail=true";

			try
			{
				var requestUri = new Uri(string.Format(_fmtGetProperties, _devSubscriptionId, _devServiceName));
				var api = new AzureMgmtApi
				{
					SubscriptionId = _devSubscriptionId,
					Certificate = AzureMgmtApi.GetCertificate(_certThumbprint)
				};

				XDocument respBody;
				string reqId = api.InvokeRequest(requestUri, "GET", HttpStatusCode.OK, null, out respBody);
				Console.WriteLine("x-ms-request-id: " + reqId);

				// parse the response
				XNamespace ns = "http://schemas.microsoft.com/windowsazure";
				var items = from c in respBody.Descendants(ns + "InputEndpoint")
								select new IPData { Vip = c.Element(ns + "Vip").Value, Port = c.Element(ns + "Port").Value };
				foreach (IPData item in items)
					Console.WriteLine("Public Endpoint: {0}:{1}", item.Vip, item.Port);
			}
			catch (Exception e)
			{
				Console.WriteLine("Error encountered: " + e.Message);
				Environment.Exit(1);
			}
		}

		private static void UpdateAppSetting(XElement doc, XNamespace ns, string settingName, string settingValue)
		{
			var sett = (from c in doc.Descendants(ns + "Setting")
							where c.Attribute("name").Value == settingName
							select c).FirstOrDefault();

			if (sett != null)
				sett.Attribute("value").Value = settingValue;
		}
	}

	public class IPData
	{
		public string Vip { get; set; }
		public string Port { get; set; }
	}
}
