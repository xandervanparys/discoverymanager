# RecognX

**RecognX** is a Unity package for object recognition, 3D localization, and instruction tracking in augmented reality environments. Built for compatibility with ARKit, Unity AR Foundation, and Apple Vision Pro, it allows you to detect objects via YOLO and project them accurately into world space using live mesh raycasting.

---

## ✨ Features

* 🔍 YOLO-based object detection (remote or local)
* 🌍 Accurate 3D object localization using AR mesh + pose reprojection
* 🧠 Instruction tracking integration (coming soon)
* 📦 Clean and modular Unity package
* ✅ Designed for Apple Vision Pro and Unity 6000.0.40f1+

---

## 📦 Installation

### Via Unity Package Manager (UPM)

1. Open your Unity project.
2. Go to `Window > Package Manager`.
3. Click the `+` button and choose `Add package from Git URL...`
4. Paste this URL:

```
https://github.com/xandervanparys/recogx.git
```

---

## 💠 Usage

1. Add the `DiscoveryManager` component to any GameObject.
2. In the Inspector, assign:

   * Your **AR Camera**
   * A **label prefab** to visualize results
   * Your **YOLO backend URL**
3. Call:

```csharp
DiscoveryManager.Instance.CaptureAndRecognize();
```

4. Optionally subscribe to the result event:

```csharp
DiscoveryManager.Instance.OnObjectsLocalized += (List<DiscoveredObject> objects) =>
{
    foreach (var obj in objects)
        Debug.Log($"Detected: {obj.className} at {obj.position}");
};
```

---

## 🔍 How It Works

* Captures an image using the ARKit camera
* Saves the pose at the time of capture
* Sends the image to a YOLO backend for detection
* Projects bounding boxes into 3D rays using Unity's viewport space
* Corrects for camera movement and casts into the live AR mesh
* Returns hit positions with corresponding labels

---

## 🛍️ Roadmap

* [x] Remote object recognition and spatial localization
* [x] Pose reprojection correction
* [ ] Instruction tracking integration
* [ ] On-device YOLO inference (CoreML / Barracuda)
* [ ] Scene understanding + clustering support
* [ ] MirageXR integration

---


## 🤝 Contributing

Pull requests, issues, and feature suggestions are welcome.

1. Fork the repository
2. Create a feature branch
3. Submit a pull request

---

## 𞷳 License

This project is licensed under the [MIT License](LICENSE).

---

© 2025 Xander Vanparys — [xandervanparys.com](https://github.com/xandervanparys)

