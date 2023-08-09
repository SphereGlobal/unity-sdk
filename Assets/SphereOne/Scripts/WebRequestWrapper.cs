using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SphereOne
{
    public static class WebRequestWrapper
    {
        public const string CALLBACK_ERR = "error";

        static UnityWebRequest CreateRequest(string path, RequestType type = RequestType.GET, object bodyData = null, Dictionary<string, string> headers = null)
        {
            var request = new UnityWebRequest(path, type.ToString());

            // not tested
            // if (bodyData.GetType() == typeof(WWWForm))
            // {
            //     var form = (WWWForm)bodyData;

            //     request.uploadHandler = new UploadHandlerRaw(form.data);
            //     request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            // } else
            
            if (bodyData != null)
            {
                // Raw body (json)
                var bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(bodyData));
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

        static public async Task SendRequest(string path, RequestType type = RequestType.GET, object bodyData = null, Dictionary<string, string> headers = null, System.Action<string> callbackOnFinish = null)
        {
            int retryCount = 0;
            while (retryCount < 10)
            {
                retryCount++;

                UnityWebRequest request = CreateRequest(path, type, bodyData, headers);
                var t = request.SendWebRequest();

                while (!t.isDone)
                    await Task.Yield();


                if (HandleResponseError(request) || request.error == null)
                {
                    // Success
                    callbackOnFinish(request.downloadHandler.text);
                    return;
                }

                if (request.result != UnityWebRequest.Result.ConnectionError)
                {
                    // Only try reconnecting on connection error
                    callbackOnFinish(CALLBACK_ERR);
                    return;
                }

                await Task.Delay(2000);

                Debug.Log("Could not connect to API, retrying...");
            }

            Debug.Log("Request failed after 10 tries.");

            // Error, no success after 10 tries
            callbackOnFinish(CALLBACK_ERR);
        }

        public static async Task<Texture2D> GetRemoteTexture(string url)
        {
            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            var t = request.SendWebRequest();

            while (!t.isDone)
                await Task.Yield();

            if (HandleResponseError(request) || request.error == null)
            {
                // Success
                return DownloadHandlerTexture.GetContent(request);
            }
            else
            {
                // Fail
                return null;
            }
        }

        static bool HandleResponseError(UnityWebRequest webRequest)
        {
            string url = webRequest.url;
            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                    Debug.LogError(url + ": Connection Error: " + webRequest.error);
                    return false;
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError(url + ": Error: " + webRequest.error);
                    return false;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError(url + ": HTTP Error: " + webRequest.error + ", " + webRequest.downloadHandler.text);
                    return false;
                case UnityWebRequest.Result.Success:
                    //Debug.Log(url + ":\nReceived: " + webRequest.downloadHandler.text);
                    return true;
            }

            return false;
        }

        static void AttachHeader(UnityWebRequest request, string key, string value)
        {
            request.SetRequestHeader(key, value);
        }
    }
}

public enum RequestType
{
    GET = 0,
    POST = 1,
    PATCH = 2
}
