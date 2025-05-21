using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using Newtonsoft.Json.Linq;


namespace RecognX
{
    public class DiscoveryManager : MonoBehaviour
    {
        public static DiscoveryManager Instance { get; private set; }

        [Header("Dependencies")]
        [SerializeField] private ARCameraManager arCameraManager;

        [SerializeField] private ARMeshManager arMeshManager;

        [SerializeField] private Camera arCamera;


        [Header("Debug Visualization")]
        [SerializeField] private bool showRays = false;
        [SerializeField] private bool showLabels = false;
        [SerializeField] private float rayLength = 8f;

        private readonly string backendUrl = "https://api.web-present.be/yolo/detect_bottle_and_cellphone/";
        
        private GameObject labelPrefab;
        private GameObject labelContainer;

        public Action<List<LocalizedObject>> OnObjectsLocalized;

        private Vector3 cameraPosAtCapture;
        private Quaternion cameraRotAtCapture;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;

            if (showLabels)
            {
                labelContainer = new GameObject("RecognX_Labels");
                labelPrefab = Resources.Load<GameObject>("Label3D");
                if (labelPrefab == null)
                {
                    Debug.LogWarning("Could not load LabelPrefab from Resources");
                }
            }
            
            AssignARMeshLayer();
        }

        public void Capture()
        {
            if (arCameraManager != null && arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                cameraPosAtCapture = arCamera.transform.position;
                cameraRotAtCapture = arCamera.transform.rotation;

                var conversionParams = new XRCpuImage.ConversionParams
                {
                    inputRect = new RectInt(0, 0, image.width, image.height),
                    outputDimensions = new Vector2Int(image.width, image.height),
                    outputFormat = TextureFormat.RGB24,
                    transformation = XRCpuImage.Transformation.None
                };

                image.ConvertAsync(conversionParams, (status, _, data) =>
                {
                    if (status == XRCpuImage.AsyncConversionStatus.Ready)
                    {
                        byte[] imageBytes = data.ToArray();
                        data.Dispose();
                        StartCoroutine(SendToBackend(imageBytes, conversionParams.outputDimensions.x, conversionParams.outputDimensions.y));
                    }
                    else
                    {
                        Debug.LogError("Image conversion failed: " + status);
                    }
                });
            }
        }

        private IEnumerator SendToBackend(byte[] imageBytes, int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.LoadRawTextureData(imageBytes);
            tex.Apply();
            byte[] pngData = tex.EncodeToPNG();

            WWWForm form = new WWWForm();
            form.AddBinaryData("file", pngData, "image.png", "image/png");

            using UnityWebRequest request = UnityWebRequest.Post(backendUrl, form);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Backend error: " + request.error);
                yield break;
            }

            HandleDetections(request.downloadHandler.text, width, height);
        }

        private void HandleDetections(string json, int imageWidth, int imageHeight)
        {
            var results = new List<LocalizedObject>();
            var data = JObject.Parse(json);
            var boxes = data["bounding_boxes"] as JArray;
            var classes = data["classes"] as JArray;

            if (boxes == null || classes == null)
            {
                Debug.LogError("Invalid detection response.");
                return;
            }

            for (int i = 0; i < boxes.Count; i++)
            {
                var box = boxes[i] as JArray;
                if (box == null || box.Count < 4 || i >= classes.Count) continue;

                string label = classes[i].ToString();
                float x1 = (float)box[0];
                float y1 = (float)box[1];
                float x2 = (float)box[2];
                float y2 = (float)box[3];
                Vector2 boxCenter = new Vector2((x1 + x2) / 2f, (y1 + y2) / 2f);
                Vector2 normalizedViewport = new Vector2(1f - (boxCenter.x / imageWidth), 1f - (boxCenter.y / imageHeight));

                Vector3 currentRay = arCamera.ViewportPointToRay(normalizedViewport).direction;
                Vector3 adjustedDirection = cameraRotAtCapture * (Quaternion.Inverse(arCamera.transform.rotation) * currentRay);
                Ray ray = new Ray(cameraPosAtCapture, adjustedDirection);

                if (showRays) DrawRay(ray.origin, ray.direction);

                if (Physics.Raycast(ray, out RaycastHit hit, 10f, LayerMask.GetMask("ARMesh")))
                {
                    if (showLabels)
                    {
                        GameObject labelObj = Instantiate(labelPrefab, hit.point, Quaternion.identity);
                        var text = labelObj.GetComponentInChildren<TextMeshPro>();
                        if (text != null) text.text = label;
                        labelObj.transform.SetParent(labelContainer.transform, false);
                    }
                    results.Add(new LocalizedObject(label, hit.point));
                }
            }

            OnObjectsLocalized?.Invoke(results);
        }

        private void DrawRay(Vector3 origin, Vector3 direction)
        {
            GameObject go = new GameObject("DebugRay");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, origin);
            lr.SetPosition(1, origin + direction * rayLength);
            lr.startWidth = lr.endWidth = 0.002f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = Color.magenta;
            Destroy(go, 5f);
        }

        private void AssignARMeshLayer()
        {
            int arMeshLayer = LayerMask.NameToLayer("ARMesh");

            if (arMeshLayer == -1)
            {
                Debug.LogWarning("⚠️ ARMesh layer not defined. Using Default layer for mesh raycasts.");
                arMeshLayer = 0;
            }
            
            if (arMeshManager == null)
            {
                Debug.LogWarning("ARMeshManager not assigned to DiscoveryManager.");
                return;
            }

            arMeshManager.meshesChanged += args =>
            {
                foreach (var mesh in args.added)
                    mesh.gameObject.layer = arMeshLayer;
            };
        }
        
        public void ClearLabels()
        {
            if (labelContainer != null)
                Destroy(labelContainer);

            labelContainer = new GameObject("RecognX_Labels");
        }
    }
}
