using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace SphereOne
{
    public static class WebRequestHandler
    {
        public const string REQUEST_ERR = "error";

        static UnityWebRequest CreateRequest(string path, RequestType type = RequestType.GET, string bodyData = null, Dictionary<string, string> headers = null)
        {
            var request = new UnityWebRequest(path, type.ToString());

            if (bodyData != null)
            {
                // Raw body (json)
                // var bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(bodyData));
                // request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                // request.SetRequestHeader("Content-Type", "application/json");

                var bodyRaw = Encoding.UTF8.GetBytes(bodyData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.SetRequestHeader("Content-Type", "application/json");
            }

            request.downloadHandler = new DownloadHandlerBuffer();


            if (headers != null)
            {
                foreach (var item in headers)
                {
                    AttachHeader(request, item.Key, item.Value);
                }
            }

            return request;
        }

        static UnityWebRequest CreateFormRequest(string path, RequestType type = RequestType.POST, WWWForm body = null, Dictionary<string, string> headers = null)
        {
            var request = new UnityWebRequest(path, type.ToString());
            request.uploadHandler = new UploadHandlerRaw(body.data);
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            request.downloadHandler = new DownloadHandlerBuffer();
            if (headers != null)
            {
                foreach (var item in headers)
                {
                    AttachHeader(request, item.Key, item.Value);
                }
            }
            return request;
        }

        static public async Task<string> Get(string path, Dictionary<string, string> headers = null)
        {
            UnityWebRequest request = CreateRequest(path, RequestType.GET, null, headers);
            var t = request.SendWebRequest();

            while (!t.isDone)
                await Task.Yield();

            if (!IsResponseSuccessful(request))
                return REQUEST_ERR;

            // Success
            return request.downloadHandler.text;
        }

        static public async Task<string> Post(string path, string bodyData, Dictionary<string, string> headers = null)
        {
            UnityWebRequest request = CreateRequest(path, RequestType.POST, bodyData, headers);
            var t = request.SendWebRequest();

            while (!t.isDone)
                await Task.Yield();

            if (!IsResponseSuccessful(request))
                return REQUEST_ERR;

            // Success
            return request.downloadHandler.text;
        }

        static public async Task<string> Post(string path, WWWForm form, Dictionary<string, string> headers = null)
        {
            UnityWebRequest request = CreateFormRequest(path, RequestType.POST, form, headers);
            var t = request.SendWebRequest();

            while (!t.isDone)
                await Task.Yield();

            if (!IsResponseSuccessful(request, true))
                return REQUEST_ERR;

            // Success
            return request.downloadHandler.text;
        }

        public static async Task<Texture2D> GetRemoteTexture(string url)
        {
            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            var t = request.SendWebRequest();

            while (!t.isDone)
                await Task.Yield();

            if (!IsResponseSuccessful(request))
                return null;

            // Success
            return DownloadHandlerTexture.GetContent(request);
        }

        static bool IsResponseSuccessful(UnityWebRequest request, bool mute = false)
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
                    if (!mute) { /*Debug.Log(url + ":\nReceived: " + webRequest.downloadHandler.text);*/ } 
                        return true;
            }

            return false;
        }

        static void AttachHeader(UnityWebRequest request, string key, string value)
        {
            request.SetRequestHeader(key, value);
        }
    }

    public enum RequestType
    {
        GET = 0,
        POST = 1,
        PATCH = 2
    }
}


