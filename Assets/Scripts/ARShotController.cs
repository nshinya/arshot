namespace GoogleARCore.Examples.ARShot {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using GoogleARCore;
    using UnityEngine;

#if UNITY_EDITOR
    // Set up touch input propagation while using Instant Preview in the editor.
    using Input = InstantPreviewInput;
#endif
    
    public class ARShotController : MonoBehaviour {
        /// <summary>
        /// The first-person camera being used to render the passthrough camera image (i.e. AR background).
        /// </summary>
        public Camera FirstPersonCamera;

        /// <summary>
        /// A prefab for tracking and visualizing detected planes.
        /// </summary>
        public GameObject DetectedPlanePrefab;

        /// <summary>
        /// A model to place when a raycast from a user touch hits a plane.
        /// </summary>
        public GameObject resultPrefab;
        
        /// <summary>
        /// A gameobject parenting UI for displaying the "searching for planes" snackbar.
        /// </summary>
        public GameObject SearchingForPlaneUI;
        
        public ARCoreSession ARSessionManager;

        public ARCoreSessionConfig sessionConfig;
        

        /// <summary>
        /// A list to hold all planes ARCore is tracking in the current frame. This object is used across
        /// the application to avoid per-frame allocations.
        /// </summary>
        private List<DetectedPlane> m_AllPlanes = new List<DetectedPlane>();

        /// <summary>
        /// True if the app is in the process of quitting due to an ARCore connection error, otherwise false.
        /// </summary>
        private bool m_IsQuitting = false;

        private TextureReaderWrapper TextureReaderWrapper = null;

        public void Start()
        {
            // カメラの解像度を上げるには、設定ファイルを最後のインデックスのものにしてやる必要があり、そのためにコールバックを設定する
            // https://developers.google.com/ar/reference/unity/class/GoogleARCore/ARCoreSession
            ARSessionManager.RegisterChooseCameraConfigurationCallback(_ChooseCameraConfiguration);
            //// Pause and resume the ARCore session to apply the camera configuration.
            ARSessionManager.enabled = false;
            ARSessionManager.enabled = true;

            // ARCoreSessionConfigをセット（オートフォーカスなど）
            ARSessionManager.SessionConfig = sessionConfig;
            
            TextureReaderWrapper = GetComponent<TextureReaderWrapper>();
        }

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
            _UpdateApplicationLifecycle();

            // Hide snackbar when currently tracking at least one plane.
            Session.GetTrackables<DetectedPlane>(m_AllPlanes);
            bool showSearchingUI = true;
            DetectedPlane anchorPlane = null;
            for (int i = 0; i < m_AllPlanes.Count; i++)
            {
                if (m_AllPlanes[i].TrackingState == TrackingState.Tracking)
                {
                    anchorPlane = m_AllPlanes[i];
                    showSearchingUI = false;
                    break;
                }
            }

            SearchingForPlaneUI.SetActive(showSearchingUI);

            if (showSearchingUI)
            {
                return;
            }

            // If the player has not touched the screen, we are done with this update.
            Touch touch;
            if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
            {
                return;
            }


            // Instantiate the object to display the server response.
            Vector3 position = FirstPersonCamera.transform.position + FirstPersonCamera.transform.forward * 0.5f;
            var anchor = Session.CreateAnchor(new Pose(position, FirstPersonCamera.transform.rotation), anchorPlane);

            // Make the text object a child of the anchor.
            GameObject prefab = resultPrefab;
            var responseObj = Instantiate(prefab, position, FirstPersonCamera.transform.rotation);
            responseObj.transform.parent = anchor.transform;

            byte[] jpg = TextureReaderWrapper.FrameTexture.EncodeToJPG();

            // Start query (request to the server)
            responseObj.GetComponent<ApiRequester>().startQuery(jpg);
        }

        /// <summary>
        /// Check and update the application lifecycle.
        /// </summary>
        private void _UpdateApplicationLifecycle()
        {
            // Exit the app when the 'back' button is pressed.
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }

            // Only allow the screen to sleep when not tracking.
            if (Session.Status != SessionStatus.Tracking)
            {
                const int lostTrackingSleepTimeout = 15;
                Screen.sleepTimeout = lostTrackingSleepTimeout;
            }
            else
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }

            if (m_IsQuitting)
            {
                return;
            }

            // Quit if ARCore was unable to connect and give Unity some time for the toast to appear.
            if (Session.Status == SessionStatus.ErrorPermissionNotGranted)
            {
                _ShowAndroidToastMessage("Camera permission is needed to run this application.");
                m_IsQuitting = true;
                Invoke("_DoQuit", 0.5f);
            }
            else if (Session.Status.IsError())
            {
                _ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
                m_IsQuitting = true;
                Invoke("_DoQuit", 0.5f);
            }
        }

        /// <summary>
        /// Actually quit the application.
        /// </summary>
        private void _DoQuit()
        {
            Application.Quit();
        }

        /// <summary>
        /// Show an Android toast message.
        /// </summary>
        /// <param name="message">Message string to show in the toast.</param>
        private void _ShowAndroidToastMessage(string message)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (unityActivity != null)
            {
                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
                unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
                        message, 0);
                    toastObject.Call("show");
                }));
            }
        }

        
        private int _ChooseCameraConfiguration(List<CameraConfig> supportedConfigurations)
        {
            // always return CPU resolution setting
            //return 0;

            // always return GPU resolution setting
            return supportedConfigurations.Count - 1;
        }
    }
}
