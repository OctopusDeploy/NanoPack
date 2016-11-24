using System;
using System.IO;
using System.Net;
using System.Text;

namespace NanoPack
{
    public static class OctoPusher
    {
        public static void Upload(string octopusUrl, string apiKey, string packageFilePath, Action<string> log, bool replaceExisting = false)
        {
            var packageUrl = octopusUrl + "/api/packages/raw?replace=" + replaceExisting;
            log($"Uploading {packageFilePath} to {packageUrl}");

            var webRequest = (HttpWebRequest)WebRequest.Create(packageUrl);
            webRequest.Accept = "application/json";
            webRequest.ContentType = "application/json";
            webRequest.Method = "POST";
            webRequest.Headers["X-Octopus-ApiKey"] = apiKey;

            using (var packageFileStream = new FileStream(packageFilePath, FileMode.Open))
            {
                var requestStream = webRequest.GetRequestStreamAsync().Result;

                var boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");
                var boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
                webRequest.ContentType = "multipart/form-data; boundary=" + boundary;
                requestStream.Write(boundarybytes, 0, boundarybytes.Length);

                var headerTemplate = "Content-Disposition: form-data; filename=\"{0}\"\r\nContent-Type: application/octet-stream\r\n\r\n";
                var header = string.Format(headerTemplate, Path.GetFileName(packageFilePath));
                var headerbytes = Encoding.UTF8.GetBytes(header);
                requestStream.Write(headerbytes, 0, headerbytes.Length);
                packageFileStream.CopyTo(requestStream);
                requestStream.Write(boundarybytes, 0, boundarybytes.Length);
                requestStream.Flush();
                requestStream.Dispose();
            }

            using (var webResponse = (HttpWebResponse)webRequest.GetResponseAsync().Result)
            {
                var statusCode = (int)webResponse.StatusCode;
                log($"{statusCode} {webResponse.StatusDescription}");
                var success = (statusCode >= 200) && (statusCode <= 299);
                if (!success)
                {
                    throw new Exception($"Uploading package to Octopus server failed with error {statusCode} {webResponse.StatusDescription}");
                }
            }
        }
    }
}