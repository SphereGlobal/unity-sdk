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
        public string Data { get; set; }
        public string Error { get; set; }
        public bool IsSuccess { get; set; }

        public WebRequestResponse(string data, string error, bool isSuccess)
        {
            Data = data;
            Error = error;
            IsSuccess = isSuccess;
        }
    }

    public static class WebRequestHandler
    {
        private static bool IsResponseSuccessful(UnityWebRequest request, bool mute = false)
        {
            string url = request.url;
            switch (request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                    if (!mute) Debug.LogError(url + ": Connection Error: " + request.error);
                    return false;
                case UnityWebRequest.Result.DataProcessingError:
                    if (!mute) Debug.LogError(url + ": Error: " + request.error);
                    return false;
                case UnityWebRequest.Result.ProtocolError:
                    if (!mute) Debug.LogError(url + ": HTTP Error: " + request.error + ", " + request.downloadHandler.text);
                    return false;
                case UnityWebRequest.Result.Success:
                    if (!mute) Debug.Log(url + ":\nReceived: " + request.downloadHandler.text);
                    return true;
            }

            return false;
        }

        public static async Task<WebRequestResponse> Get(string url, Dictionary<string, string> headers = null)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
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
        }

        public static async Task<WebRequestResponse> Post(string url, string jsonData, Dictionary<string, string> headers = null)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

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
        }

        public static async Task<WebRequestResponse> Post(string url, WWWForm formData, Dictionary<string, string> headers = null)
        {
            using (UnityWebRequest request = UnityWebRequest.Post(url, formData))
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
        }

        public static async Task<WebRequestResponse> Put(string url, string jsonData, Dictionary<string, string> headers = null)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

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
        }

        public static async Task<WebRequestResponse> Put(string url, WWWForm formData, Dictionary<string, string> headers = null)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT))
            {
                if (formData != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(formData.data);
                    request.uploadHandler.contentType = "application/x-www-form-urlencoded";
                }
                request.downloadHandler = new DownloadHandlerBuffer();

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
        }

        public static async Task<WebRequestResponse> Delete(string url, Dictionary<string, string> headers = null)
        {
            using (UnityWebRequest request = UnityWebRequest.Delete(url))
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
