using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class VisionApiRequest {
    private bool uploading = false;
    private string url;
    public string response { get; private set; }
    
    public VisionApiRequest()
    {
        response = null;
        string apiKey = "API key";
        url = "https://vision.googleapis.com/v1/images:annotate?key=" + apiKey;
    }

    public IEnumerator request(byte[] image)
    {
        string encode = Convert.ToBase64String(image);
        return request(encode);
    }

    public IEnumerator request(string base64Image)
    {
        if (uploading)
        {
            yield break;
        }

        uploading = true;

        // requestBodyを作成
        var requests = new requestBody();
        requests.requests = new List<AnnotateImageRequest>();

        var request = new AnnotateImageRequest();
        request.image = new Image();
        request.image.content = base64Image;

        request.features = new List<Feature>();
        var feature = new Feature();
        feature.type = FeatureType.LABEL_DETECTION.ToString();
        feature.maxResults = 10;
        request.features.Add(feature);

        requests.requests.Add(request);

        // JSONに変換
        string jsonRequestBody = JsonUtility.ToJson(requests);

        // ヘッダを"application/json"にして投げる
        UnityWebRequest www = UnityWebRequest.Post(url, "POST");
        byte[] postData = Encoding.UTF8.GetBytes(jsonRequestBody);
        www.uploadHandler = (UploadHandler)new UploadHandlerRaw(postData);
        www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
            response = www.error;
        }
        else
        {
            Debug.Log("Form upload complete!");
            if (www.responseCode == 200)
            {
                //Debug.Log(www.downloadHandler.text);
                response = www.downloadHandler.text;
            }
            else
            {
                Debug.Log("failed");
                response = "Request failed. HTTP Response code: " + www.responseCode;
            }
        }
        uploading = false;
        yield break;
    }

    [Serializable]
    public class requestBody {
        public List<AnnotateImageRequest> requests;
    }

    [Serializable]
    public class AnnotateImageRequest {
        public Image image;
        public List<Feature> features;
        //public string imageContext;
    }

    [Serializable]
    public class Image {
        public string content;
        //public ImageSource source;
    }

    [Serializable]
    public class ImageSource {
        public string gcsImageUri;
    }

    [Serializable]
    public class Feature {
        public string type;
        public int maxResults;
    }

    public enum FeatureType {
        TYPE_UNSPECIFIED,
        FACE_DETECTION,
        LANDMARK_DETECTION,
        LOGO_DETECTION,
        LABEL_DETECTION,
        TEXT_DETECTION,
        SAFE_SEARCH_DETECTION,
        IMAGE_PROPERTIES
    }

    [Serializable]
    public class ImageContext {
        public LatLongRect latLongRect;
        public string languageHints;
    }

    [Serializable]
    public class LatLongRect {
        public LatLng minLatLng;
        public LatLng maxLatLng;
    }

    [Serializable]
    public class LatLng {
        public float latitude;
        public float longitude;
    }

    [Serializable]
    public class responseBody {
        public List<AnnotateImageResponse> responses;
    }

    [Serializable]
    public class AnnotateImageResponse {
        public List<EntityAnnotation> labelAnnotations;
    }

    [Serializable]
    public class EntityAnnotation {
        public string mid;
        public string locale;
        public string description;
        public float score;
        public float confidence;
        public float topicality;
        public BoundingPoly boundingPoly;
        public List<LocationInfo> locations;
        public List<Property> properties;
    }

    [Serializable]
    public class BoundingPoly {
        public List<Vertex> vertices;
    }

    [Serializable]
    public class Vertex {
        public float x;
        public float y;
    }

    [Serializable]
    public class LocationInfo {
        LatLng latLng;
    }

    [Serializable]
    public class Property {
        string name;
        string value;
    }
}
