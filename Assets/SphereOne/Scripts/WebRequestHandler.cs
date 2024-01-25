using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace SphereOne
{
    public class WebRequestResponse
    {
        public string Data { get; }
        public string Error { get; }
        public bool IsSuccess { get; }

        public WebRequestResponse(string data, string error, bool isSuccess)
        {
            Data = data;
            Error = error;
            IsSuccess = isSuccess;
        }
    }

    public static class WebRequestHandler
    {
        private static bool IsResponseSuccessful(UnityWebRequest request)
        {
            return request.result == UnityWebRequest.Result.Success;
        }

        private static async Task<WebRequestResponse> SendRequest(UnityWebRequest request, Dictionary<string, string> headers)
        {
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }

            var res = request.SendWebRequest();
            while (!res.isDone)
            {
                await Task.Yield();
            }

            bool isSuccess = IsResponseSuccessful(request);
            string responseData = request.downloadHandler != null ? request.downloadHandler.text : null;
            string error = isSuccess ? null : request.error;

            return new WebRequestResponse(responseData, error, isSuccess);
        }

        public static async Task<WebRequestResponse> Get(string url, Dictionary<string, string> headers = null)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                return await SendRequest(request, headers);
            }
        }

        public static async Task<WebRequestResponse> Post(string url, string jsonData, Dictionary<string, string> headers = null)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                return await SendRequest(request, headers);
            }
        }

        public static async Task<WebRequestResponse> Post(string url, WWWForm formData, Dictionary<string, string> headers = null)
        {
            using (UnityWebRequest request = UnityWebRequest.Post(url, formData))
            {
                return await SendRequest(request, headers);
            }
        }

        public static async Task<WebRequestResponse> Put(string url, string jsonData, Dictionary<string, string> headers = null)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                return await SendRequest(request, headers);
            }
        }

        public static async Task<WebRequestResponse> Put(string url, WWWForm formData, Dictionary<string, string> headers = null)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
            {
                if (formData != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(formData.data);
                    request.uploadHandler.contentType = "application/x-www-form-urlencoded";
                }
                request.downloadHandler = new DownloadHandlerBuffer();
                return await SendRequest(request, headers);
            }
        }

        public static async Task<WebRequestResponse> Delete(string url, Dictionary<string, string> headers = null)
        {
            using (UnityWebRequest request = UnityWebRequest.Delete(url))
            {
                return await SendRequest(request, headers);
            }
        }

        public static async Task<Texture2D> GetRemoteTexture(string url, int timeoutSeconds = 10)
        {
            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            request.timeout = timeoutSeconds;
            var res = request.SendWebRequest();

            while (!res.isDone)
            {
                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error while downloading texture: {request.error}");
                    return null;
                }

                await Task.Yield();
            }

            if (!IsResponseSuccessful(request))
            {
                Debug.LogError($"Failed to download texture from {url}: {request.error}");
                return null;
            }

            // Success
            return DownloadHandlerTexture.GetContent(request);
        }
    }
}
