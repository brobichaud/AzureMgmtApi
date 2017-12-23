using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Digimarc.Shared.Classes
{
	/// <summary>
	/// Xml serialization related utility class
	/// </summary>
	public class XmlIO
	{
		/// <summary>
		/// Default namespace to use for serialization
		/// </summary>
		public string DefaultNamespace { get; set; }

		/// <summary>
		/// Encoding format to use for serialization
		/// </summary>
		public Encoding EncodingFormat { get; set; }

		/// <summary>
		/// Whether to output the xml declaration for serialization
		/// </summary>
		public bool OmitXmlDeclaration { get; set; }

		/// <summary>
		/// Whether to format data for serialization
		/// </summary>
		public bool FormatData { get; set; }

		/// <summary>
		/// Indentation characters to use for serialization
		/// </summary>
		public string IndentChars { get; set; }

		public XmlIO()
		{
			DefaultNamespace = string.Empty;
			EncodingFormat = Encoding.UTF8;
			OmitXmlDeclaration = true;
			FormatData = false;
			IndentChars = "\t";
		}

		/// <summary>
		/// Saves the passed object to the passed file, using the supplied default namespace
		/// </summary>
		/// <param name="data">Object to save</param>
		/// <param name="file">File to save to</param>
		/// <param name="defaultNamespace">Default xml namespace to use</param>
		public void Serialize(object data, string file, string defaultNamespace)
		{
			DefaultNamespace = defaultNamespace;
			Serialize(data, file, true);
		}

		/// <summary>
		/// Saves the passed object to the passed file, optionally removing the namespace
		/// </summary>
		/// <param name="data">Object to save</param>
		/// <param name="file">File to save to</param>
		/// <param name="useNamespace">False removes all namespace declarations</param>
		public void Serialize(object data, string file, bool useNamespace = true)
		{
			XmlSerializer ser = new XmlSerializer(data.GetType(), DefaultNamespace);
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Encoding = EncodingFormat;
			settings.OmitXmlDeclaration = OmitXmlDeclaration;
			settings.Indent = FormatData;
			settings.IndentChars = IndentChars;

			using (XmlWriter writer = XmlWriter.Create(file, settings))
			{
				if (!useNamespace)
				{
					// remove all namespace declarations
					var ns = new XmlSerializerNamespaces();
					ns.Add("", "");
					ser.Serialize(writer, data, ns);
				}
				else
					ser.Serialize(writer, data);
			}
		}

		/// <summary>
		/// Saves the passed object to a stream, using the supplied default namespace
		/// </summary>
		/// <param name="data">Object to save</param>
		/// <param name="stream">Stream to save to</param>
		/// <param name="defaultNamespace">Default xml namespace to use</param>
		public void Serialize(object data, Stream stream, string defaultNamespace)
		{
			DefaultNamespace = defaultNamespace;
			Serialize(data, stream, true);
		}

		/// <summary>
		/// Saves the passed object to a stream, optionally removing the namespace
		/// </summary>
		/// <param name="data">Object to save</param>
		/// <param name="stream">Stream to save to</param>
		/// <param name="useNamespace">False removes all namespace declarations</param>
		public void Serialize(object data, Stream stream, bool useNamespace = true)
		{
			XmlSerializer ser = new XmlSerializer(data.GetType(), DefaultNamespace);
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Encoding = EncodingFormat;
			settings.OmitXmlDeclaration = OmitXmlDeclaration;
			settings.Indent = FormatData;
			settings.IndentChars = IndentChars;

			using (XmlWriter writer = XmlWriter.Create(stream, settings))
			{
				if (!useNamespace)
				{
					// remove all namespace declarations
					var ns = new XmlSerializerNamespaces();
					ns.Add("", "");
					ser.Serialize(writer, data, ns);
				}
				else
					ser.Serialize(writer, data);
			}
		}

		/// <summary>
		/// Saves the passed object to a StringBuilder, using the supplied default namespace
		/// </summary>
		/// <param name="data">Object to save</param>
		/// <param name="buffer">Stringbuilder to save into</param>
		/// <param name="defaultNamespace">Default xml namespace to use</param>
		public void Serialize(object data, StringBuilder buffer, string defaultNamespace)
		{
			DefaultNamespace = defaultNamespace;
			Serialize(data, buffer, true);
		}

		/// <summary>
		/// Saves the passed object to a StringBuilder, optionally removing the namespace
		/// </summary>
		/// <param name="data">Object to save</param>
		/// <param name="buffer">Stringbuilder to save into</param>
		/// <param name="useNamespace">False removes all namespace declarations</param>
		public void Serialize(object data, StringBuilder buffer, bool useNamespace = true)
		{
			XmlSerializer ser = new XmlSerializer(data.GetType(), DefaultNamespace);
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Encoding = EncodingFormat;
			settings.OmitXmlDeclaration = OmitXmlDeclaration;
			settings.Indent = FormatData;
			settings.IndentChars = IndentChars;

			using (XmlWriter writer = XmlWriter.Create(buffer, settings))
			{
				if (!useNamespace)
				{
					// remove all namespace declarations
					var ns = new XmlSerializerNamespaces();
					ns.Add("", "");
					ser.Serialize(writer, data, ns);
				}
				else
					ser.Serialize(writer, data);
			}
		}

		/// <summary>
		/// Loads the passed file into an object of the passed type, using the default namespace
		/// </summary>
		/// <param name="dataType">Type of object to load</param>
		/// <param name="file">File to load from</param>
		public object Deserialize(Type dataType, string file)
		{
			XmlSerializer ser = new XmlSerializer(dataType, DefaultNamespace);

			using (XmlReader reader = XmlReader.Create(file))
			{
				return ser.Deserialize(reader);
			}
		}

		/// <summary>
		/// Loads the passed stream into an object of the passed type, using the default namespace
		/// </summary>
		/// <param name="dataType">Type of object to load</param>
		/// <param name="stream">Stream to load from</param>
		public object Deserialize(Type dataType, Stream stream)
		{
			XmlSerializer ser = new XmlSerializer(dataType, DefaultNamespace);

			stream.Position = 0;
			using (XmlReader reader = XmlReader.Create(stream))
			{
				return ser.Deserialize(reader);
			}
		}

		/// <summary>
		/// Loads the passed buffer into an object of the passed type, using the default namespace
		/// </summary>
		/// <param name="dataType">Type of object to load</param>
		/// <param name="buffer">Buffer to load from</param>
		public object DeserializeBuffer(Type dataType, string buffer)
		{
			XmlSerializer ser = new XmlSerializer(dataType, DefaultNamespace);

			using (var stream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(buffer)))
			{
				return ser.Deserialize(stream);
			}
		}

		/// <summary>
		/// Formats an xml fragment so that it is indented
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
		public string FormatFragment(string fragment)
		{
			using (StringWriter sw = new StringWriter())
			{
				XmlWriterSettings settings = new XmlWriterSettings();
				settings.Indent = true;  // force formatting
				settings.IndentChars = IndentChars;
				settings.OmitXmlDeclaration = OmitXmlDeclaration;

				using (XmlWriter writer = XmlWriter.Create(sw, settings))
				{
					XmlDocument doc = new XmlDocument();
					doc.LoadXml(fragment);
					doc.WriteTo(writer);
				}

				return sw.ToString();
			}
		}

		/// <summary>
		/// Writes an xml fragment to the specified file
		/// </summary>
		/// <param name="fragment">Fragment to write</param>
		/// <param name="fileName">Full path of file to write to</param>
		public void WriteFragment(string fragment, string fileName)
		{
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = FormatData;
			settings.IndentChars = IndentChars;

			using (XmlWriter writer = XmlWriter.Create(fileName, settings))
			{
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(fragment);
				doc.WriteTo(writer);
			}
		}

		/// <summary>
		/// Removes any namespace declarations from an xml fragment
		/// </summary>
		/// <param name="fragment">Xml fragment</param>
		public string RemoveNamespaces(string fragment)
		{
			XDocument doc = XDocument.Parse(fragment);
			XElement root = doc.Root;

			foreach (XElement e in root.DescendantsAndSelf())
			{
				if (e.Name.Namespace != XNamespace.None)
					e.Name = XNamespace.None.GetName(e.Name.LocalName);

				if (e.Attributes().Where(a => a.IsNamespaceDeclaration || a.Name.Namespace != XNamespace.None).Any())
				{
					e.ReplaceAttributes(e.Attributes().Select(a =>
							a.IsNamespaceDeclaration ? null : a.Name.Namespace != XNamespace.None ?
							new XAttribute(XNamespace.None.GetName(a.Name.LocalName), a.Value) : a));
				}
			}

			return doc.ToString();
		}
	}
}
