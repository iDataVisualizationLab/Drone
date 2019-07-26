using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Brain_uwp.Utils
{
    // Implements multipart/form-data POST in C# http://www.ietf.org/rfc/rfc2388.txt
    // http://www.briangrinstead.com/blog/multipart-form-post-in-c
    // https://gist.github.com/bgrins/1789787
    /// <summary>
    ///  Implements multipart/form-data POST in C# http://www.ietf.org/rfc/rfc2388.txt
    /// </summary>
    public static class FormUpload
	{
		private static readonly Encoding encoding = Encoding.UTF8;
        /// <summary>
        /// Posts the MultipartFormData to the url
        /// </summary>
        /// <param name="postUrl"></param>
        /// <param name="userAgent"></param>
        /// <param name="postParameters"></param>
        /// <returns><see cref="HttpWebResponse"/></returns>
		public static HttpWebResponse MultipartFormDataPost(string postUrl, string userAgent, Dictionary<string, object> postParameters)
		{
			string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
			string contentType = "multipart/form-data; boundary=" + formDataBoundary;

			byte[] formData = GetMultipartFormData(postParameters, formDataBoundary);

			return PostForm(postUrl, userAgent, contentType, formData);
		}
        /// <summary>
        /// Post the raw data as byte to the url
        /// </summary>
        /// <param name="postUrl"></param>
        /// <param name="userAgent"></param>
        /// <param name="contentType"></param>
        /// <param name="formData"></param>
        /// <returns><see cref="HttpWebResponse"/></returns>
		private static HttpWebResponse PostForm(string postUrl, string userAgent, string contentType, byte[] formData)
		{
			HttpWebRequest request = WebRequest.Create(postUrl) as HttpWebRequest;

			if (request == null)
			{
				throw new NullReferenceException("request is not a http request");
			}

			// Set up the request properties.
			request.Method = "POST";
			request.ContentType = contentType;
			request.UserAgent = userAgent;
			request.CookieContainer = new CookieContainer();
			request.ContentLength = formData.Length;

			// You could add authentication here as well if needed:
			// request.PreAuthenticate = true;
			// request.AuthenticationLevel = System.Net.Security.AuthenticationLevel.MutualAuthRequested;
			// request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.Default.GetBytes("username" + ":" + "password")));

			// Send the form data to the request.
			using (Stream requestStream = request.GetRequestStream())
			{
				requestStream.Write(formData, 0, formData.Length);
				requestStream.Close();
			}

			return request.GetResponse() as HttpWebResponse;
		}

        /// <summary>
        /// Creates a byte array from postParameters
        /// </summary>
        /// <param name="postParameters"></param>
        /// <param name="boundary"></param>
        /// <returns>postParameters as byte[]</returns>
		private static byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary)
		{
			Stream formDataStream = new System.IO.MemoryStream();
			bool needsCLRF = false;

			foreach (var param in postParameters)
			{
				// Thanks to feedback from commenters, add a CRLF to allow multiple parameters to be added.
				// Skip it on the first parameter, add it to subsequent parameters.
				if (needsCLRF)
					formDataStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));

				needsCLRF = true;

				if (param.Value is FileParameter)
				{
					FileParameter fileToUpload = (FileParameter)param.Value;

					// Add just the first part of this param, since we will write the file data directly to the Stream
					string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\";\r\nContent-Type: {3}\r\n\r\n",
						boundary,
						param.Key,
						fileToUpload.FileName ?? param.Key,
						fileToUpload.ContentType ?? "application/octet-stream");

					formDataStream.Write(encoding.GetBytes(header), 0, encoding.GetByteCount(header));

					// Write the file data directly to the Stream, rather than serializing it to a string.
					formDataStream.Write(fileToUpload.File, 0, fileToUpload.File.Length);
				}
				else
				{
					string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
						boundary,
						param.Key,
						param.Value);
					formDataStream.Write(encoding.GetBytes(postData), 0, encoding.GetByteCount(postData));
				}
			}

			// Add the end of the request.  Start with a newline
			string footer = "\r\n--" + boundary + "--\r\n";
			formDataStream.Write(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer));

			// Dump the Stream into a byte[]
			formDataStream.Position = 0;
			byte[] formData = new byte[formDataStream.Length];
			formDataStream.Read(formData, 0, formData.Length);
			formDataStream.Close();

			return formData;
		}

        /// <summary>
        /// 
        /// </summary>
		public class FileParameter
		{
            /// <summary>
            /// 
            /// </summary>
			public byte[] File { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string FileName { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string ContentType { get; set; }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="file"></param>
			public FileParameter(byte[] file) : this(file, null) { }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="file"></param>
            /// <param name="filename"></param>
            public FileParameter(byte[] file, string filename) : this(file, filename, null) { }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="file"></param>
            /// <param name="filename"></param>
            /// <param name="contenttype"></param>
            public FileParameter(byte[] file, string filename, string contenttype)
			{
				File = file;
				FileName = filename;
				ContentType = contenttype;
			}
		}
	}
}
