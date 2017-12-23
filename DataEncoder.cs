using System;
using System.Text;

namespace Digimarc.Shared.Classes
{
	/// <summary>
	/// Wrapper class created to simplify encoding and decoding of strings
	/// </summary>
	public static class DataEncoder
	{
		/// <summary>
		/// Encodes a string as a base64 string
		/// </summary>
		/// <param name="data">Data to encode</param>
		public static string ToBase64(string data)
		{
			return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
		}

		/// <summary>
		/// Encodes a byte array as a base64 string
		/// </summary>
		/// <param name="bytes">Bytes to encode</param>
		public static string ToBase64(byte[] bytes)
		{
			return Convert.ToBase64String(bytes);
		}

		/// <summary>
		/// Decodes base64 encoded data into a string
		/// </summary>
		/// <param name="data">Data to decode</param>
		public static string StringFromBase64(string data)
		{
			byte[] decoded = Convert.FromBase64String(data);
			return Encoding.UTF8.GetString(decoded);
		}

		/// <summary>
		/// Decodes base64 encoded data into a byte array
		/// </summary>
		/// <param name="data">Data to decode</param>
		public static byte[] BytesFromBase64(string data)
		{
			return Convert.FromBase64String(data);
		}

		/// <summary>
		/// Encodes a string as a hex string
		/// </summary>
		/// <param name="data">Data to encode</param>
		public static string ToHex(string data)
		{
			var hex = new StringBuilder();
			foreach (char c in data)
			{
				int tmp = c;
				hex.AppendFormat("{0:X2}", Convert.ToUInt32(tmp.ToString()));
			}

			return hex.ToString();
		}

		/// <summary>
		/// Encodes a byte array as a hex string
		/// </summary>
		/// <param name="bytes">Bytes to encode</param>
		public static string ToHex(byte[] bytes)
		{
			var hex = new StringBuilder(bytes.Length * 2);
			foreach (byte b in bytes)
				hex.AppendFormat("{0:X2}", b);

			return hex.ToString();
		}

		/// <summary>
		/// Decodes hex encoded data into a string
		/// </summary>
		/// <param name="data">Data to decode</param>
		public static string StringFromHex(string data)
		{
			int NumberChars = data.Length;
			byte[] bytes = new byte[NumberChars / 2];

			for (int i = 0; i < NumberChars; i += 2)
				bytes[i / 2] = Convert.ToByte(data.Substring(i, 2), 16);

			return Encoding.UTF8.GetString(bytes);
		}

		/// <summary>
		/// Decodes hex encoded data into a byte array
		/// </summary>
		/// <param name="data">Data to decode</param>
		public static byte[] BytesFromHex(string data)
		{
			int NumberChars = data.Length;
			byte[] bytes = new byte[NumberChars / 2];

			for (int i = 0; i < NumberChars; i += 2)
				bytes[i / 2] = Convert.ToByte(data.Substring(i, 2), 16);

			return bytes;
		}
	}
}
