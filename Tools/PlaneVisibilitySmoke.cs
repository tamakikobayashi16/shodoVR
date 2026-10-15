using System;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Copy into Assets/Editor in a disposable checkout and invoke PlaneVisibilitySmoke.Run.
public static class PlaneVisibilitySmoke
{
    static int checks;
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var pen = UnityEngine.Object.FindObjectOfType<PenPosi>();
        Check(pen != null && pen.plane != null && pen.udpSender != null, "sample scene has plane and sender references");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        EditorApplication.playModeStateChanged += Changed;
        EditorApplication.isPlaying = true;
    }
    static void Changed(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        try
        {
            using (var host = new RemoteTrialLink(true, 0))
            {
                host.BeginTrial(Guid.NewGuid().ToString("N"), "あ");
                var root = new GameObject("PenTest");
                root.SetActive(false);
                var plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
                plane.transform.SetParent(root.transform);
                var renderer = plane.GetComponent<Renderer>();
                var sender = root.AddComponent<DrawingUdpSender>();
                sender.controlPort = host.LocalPort;
                var pen = root.AddComponent<PenPosi>();
                pen.plane = plane.transform;
                pen.udpSender = sender;
                root.SetActive(true);
                sender.DeviceReady = true;
                Check(renderer.forceRenderingOff && root.activeInHierarchy, "plane hidden on startup without disabling input object");
                Exchange(host, sender, pen);
                Check(renderer.forceRenderingOff, "connected plane stays hidden before readiness");
                var link = (RemoteTrialLink)typeof(DrawingUdpSender).GetField("link", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sender);
                link.LocalReady = true;
                host.LocalReady = true;
                Exchange(host, sender, pen);
                Check(!renderer.forceRenderingOff, "learner start signal reveals the plane");
                host.SetPhase("templateCompleted");
                Exchange(host, sender, pen);
                Check(!renderer.forceRenderingOff, "completed template remains visible");
                link.DisconnectSeconds = 0.001f;
                Thread.Sleep(20);
                Invoke(pen, "UpdatePlaneVisibility");
                Check(renderer.forceRenderingOff, "disconnect hides the plane");
                link.DisconnectSeconds = 5f;
                host.BeginTrial(Guid.NewGuid().ToString("N"), "い");
                Exchange(host, sender, pen);
                Check(renderer.forceRenderingOff, "retry returns to hidden waiting state");
                sender.requireSessionHandshake = false;
                Invoke(pen, "UpdatePlaneVisibility");
                Check(!renderer.forceRenderingOff, "standalone mode keeps surface visible");
                root.SetActive(false);
                Check(!renderer.forceRenderingOff, "disable restores original renderer visibility");
            }
            Debug.Log("PLANE_VISIBILITY_PASS: " + checks + " checks");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static void Exchange(RemoteTrialLink host, DrawingUdpSender sender, PenPosi pen)
    {
        for (int i = 0; i < 70; i++) { host.Tick(); Invoke(sender, "Update"); Thread.Sleep(10); }
        Invoke(pen, "UpdatePlaneVisibility");
    }
    static void Invoke(object obj, string name) => obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, null);
    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception("FAILED: " + label);
        checks++;
        Debug.Log("PASS: " + label);
    }
}
