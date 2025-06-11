using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using Newtonsoft.Json.Linq;
using Unity.Collections;


namespace RecognX
{
    public class DiscoveryManager : IDiscoveryManager
    {
        private static ARCameraManager arCameraManager;

        private ARMeshManager arMeshManager;

        private Camera arCamera;

        private BackendService backendService;

        private Vector3 cameraPosAtCapture;
        private Quaternion cameraRotAtCapture;

        public DiscoveryManager(ARCameraManager cameraManager, ARMeshManager meshManager, Camera unityCamera,
            BackendService backend)
        {
            arCameraManager = cameraManager;
            arMeshManager = meshManager;
            arCamera = unityCamera;
            backendService = backend;

            // Check if ARMesh layer exists and inform developer if missing
            if (LayerMask.NameToLayer("ARMesh") == -1)
            {
                Debug.LogWarning(
                    "⚠️ The 'ARMesh' layer is not defined. Please add an 'ARMesh' layer to your project to ensure accurate raycasting.");
            }

            AssignARMeshLayer();
        }

        public async Task<List<LocalizedObject>> LocateObjectsAsync(List<int> activeYoloIds)
        {
            float startTime = Time.realtimeSinceStartup;
            Texture2D tex = CaptureCameraTextureAsync();
            cameraPosAtCapture = arCamera.transform.position;
            cameraRotAtCapture = arCamera.transform.rotation;

            float backendStart = Time.realtimeSinceStartup;
            List<YoloDetection> detections = await backendService.DetectObjectsAsync(tex, activeYoloIds);
            float backendDuration = Time.realtimeSinceStartup - backendStart;
            Debug.Log($"[Latency] DetectObjectsAsync took {backendDuration * 1000f} ms");

            foreach (var detection in detections)
                Debug.Log(
                    $"Detected {detection.class_name} (YOLO ID: {detection.yoloId}) @ confidence {detection.confidence}");

            float raycastStart = Time.realtimeSinceStartup;
            var results = HandleDetections(detections, tex.width, tex.height);
            float raycastDuration = Time.realtimeSinceStartup - raycastStart;
            Debug.Log($"[Latency] Raycasting took {raycastDuration * 1000f} ms");

            float totalDuration = Time.realtimeSinceStartup - startTime;
            Debug.Log($"[Latency] LocateObjectsAsync total {totalDuration * 1000f} ms");
            return results;
        }

        private List<LocalizedObject> HandleDetections(List<YoloDetection> detections, int imageWidth, int imageHeight)
        {
            // TODO: Come up with a solution for this ARMesh, because I think it would be good.
            var results = new List<LocalizedObject>();
            foreach (var detection in detections)
            {
                float[] box = detection.bounding_box;
                if (box.Length < 4) continue;

                string label = detection.class_name;
                float x1 = box[0], y1 = box[1], x2 = box[2], y2 = box[3];

                Vector2 boxCenter = new Vector2((x1 + x2) / 2f, (y1 + y2) / 2f);
                Vector2 normalizedViewport =
                    new Vector2(1f - (boxCenter.x / imageWidth), 1f - (boxCenter.y / imageHeight));

                Vector3 currentRay = arCamera.ViewportPointToRay(normalizedViewport).direction;
                Vector3 adjustedDirection =
                    cameraRotAtCapture * (Quaternion.Inverse(arCamera.transform.rotation) * currentRay);
                Debug.Log(
                    $"⚡ Attempting raycast for {label} with center at {boxCenter}, viewport {normalizedViewport}");
                Ray ray = new Ray(cameraPosAtCapture, adjustedDirection);
                //TODO: Decide on Max distance
                //TODO: Fix this ARMesh Layermask because this will most likely break it.
                if (Physics.Raycast(ray, out RaycastHit hit, 10f, LayerMask.GetMask("ARMesh")))
                {
                    results.Add(new LocalizedObject(label, detection.yoloId, hit.point));
                }
            }

            foreach (var result in results)
                Debug.Log($"Localized: {result.label} at {result.position}");
            return results;
        }

        private void AssignARMeshLayer()
        {
            Debug.Log("AssignARMeshLayer() called");
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
                {
                    mesh.gameObject.layer = arMeshLayer;
                    Debug.Log($"Mesh assigned layer: {mesh.gameObject.layer}");
                }
            };
        }

        public static Texture2D CaptureCameraTextureAsync()
        {
            if (arCameraManager == null || !arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
                return null;

            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(0, 0, image.width, image.height),
                outputDimensions = new Vector2Int(image.width, image.height),
                outputFormat = TextureFormat.RGB24,
                transformation = XRCpuImage.Transformation.None
            };

            int size = image.GetConvertedDataSize(conversionParams);
            using var rawTextureData = new NativeArray<byte>(size, Allocator.Temp);
            image.Convert(conversionParams, rawTextureData);
            image.Dispose();

            var tex = new Texture2D(conversionParams.outputDimensions.x, conversionParams.outputDimensions.y,
                TextureFormat.RGB24, false);
            tex.LoadRawTextureData(rawTextureData);
            tex.Apply();

            return tex;
        }
    }
}