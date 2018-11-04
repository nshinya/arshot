using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ApiRequester : MonoBehaviour {
    /// <summary>
    /// インスタンス化直後に表示するテキスト
    /// </summary>
    public string initialStr = "";

    /// <summary>
    /// サーバ応答待ち中に表示するテキスト
    /// </summary>
    public string waitingStr = "Waiting for response...";
    
    /// <summary>
    /// RESTful API でのリクエスト実行・レスポンス取得用クラス
    /// </summary>
    private VisionApiRequest visionApiRequest;

    /// <summary>
    /// 結果表示用3DText
    /// </summary>
    private TextMesh textMesh;

    /// <summary>
    /// リクエスト実行中かどうか
    /// </summary>
    private bool queryStarted = false;

    /// <summary>
    /// サービスのレスポンス格納クラス
    /// </summary>
    [Serializable]
    public class LabelAnnotation {
        public string mid;
        public string description;
        public float score;
    }

    [Serializable]
    public class LabelAnnotations {
        public List<LabelAnnotation> labelAnnotations;
    }

    [Serializable]
    public class ServiceResponse {
        public List<LabelAnnotations> responses;
    }



    /// <summary>
    /// インスタンス化直後に自身のメンバ変数を初期化
    /// </summary>
    void Awake () {
        textMesh = GetComponent<TextMesh>();
        visionApiRequest = new VisionApiRequest();
    }
	
	// Update is called once per frame
	void Update () {
        if(visionApiRequest == null)
        {
            return;
        }

        if (queryStarted)
        {
            // Check the response and draw results
            var resJson = visionApiRequest.response;

            if (resJson != null)
            {
                var res = JsonUtility.FromJson<ServiceResponse>(resJson);
                var labelAnnotations = res.responses[0].labelAnnotations;
                //Debug.Log(res.label);

                // サーバ応答の表示
                string resText = "";
                foreach(var item in labelAnnotations) {
                    resText += item.description + " " + item.score + "%\n";
                }
                textMesh.text = resText;
            }
            else
            {
                // サーバ応答待ち中
                textMesh.text = waitingStr;
            }
        }
        else
        {
            // インスタンス化直後
            textMesh.text = initialStr;
        }
    }

    /// <summary>
    /// サーバへリクエストを開始する
    /// </summary>
    /// <param name="queryImage">認識させる画像</param>
    public void startQuery(byte[] queryImage)
    {
        if (visionApiRequest == null)
        {
            Debug.Log("uploadImage is null");
            return;
        }
        queryStarted = true;
        StartCoroutine(visionApiRequest.request(queryImage));
    }
}
