using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class DrawingUdpReceiver : MonoBehaviour
{
    [Range(1, 65535)] public int listenPort = 5005;
    public Material lineMaterial;
    public float lineWidth = 0.03f;
    public Vector3 positionOffset;
    public float positionScale = 1f;
    public int maxPacketsPerFrame = 1000;

    [Serializable]
    private class Packet
    {
        public int version;
        public string session, method, eventType;
        public int sequence, character, stroke;
        public float x, y, z, pressure;
    }

    private readonly ConcurrentQueue<string> incoming = new ConcurrentQueue<string>();
    private readonly Dictionary<string, LineRenderer> lines = new Dictionary<string, LineRenderer>();
    private UdpClient socket;
    private Thread worker;
    private volatile bool running;
    private string session;
    private int lastSequence = -1;
    private string receiveError;

    private void OnEnable()
    {
        try
        {
            socket = new UdpClient(new IPEndPoint(IPAddress.Any, listenPort));
            running = true;
            worker = new Thread(ReceiveLoop) { IsBackground = true };
            worker.Start();
        }
        catch (Exception e) { Debug.LogError($"Drawing UDP receiver: {e.Message}", this); }
    }

    private void ReceiveLoop()
    {
        var endpoint = new IPEndPoint(IPAddress.Any, 0);
        while (running)
        {
            try
            {
                var bytes = socket.Receive(ref endpoint);
                // Bound pending work when the application cannot keep up.
                if (incoming.Count < 10000) incoming.Enqueue(Encoding.UTF8.GetString(bytes));
            }
            catch (SocketException e) { if (running) receiveError = e.Message; break; }
            catch (ObjectDisposedException) { break; }
        }
    }

    private void Update()
    {
        if (receiveError != null)
        {
            Debug.LogError($"Drawing UDP receiver: {receiveError}", this);
            receiveError = null;
        }
        for (int i = 0; i < Mathf.Max(1, maxPacketsPerFrame) && incoming.TryDequeue(out var json); i++)
        {
            Packet p;
            try { p = JsonUtility.FromJson<Packet>(json); }
            catch (ArgumentException) { continue; }
            if (p == null || p.version != 1 || p.method != "PenTablet" || string.IsNullOrEmpty(p.session)) continue;
            if (session != p.session)
            {
                ClearAll();
                session = p.session;
                lastSequence = -1;
            }
            if (p.sequence <= lastSequence) continue;
            lastSequence = p.sequence;
            if (p.eventType == "clearCharacter")
            {
                string prefix = p.character + ":";
                var keys = new List<string>(lines.Keys);
                foreach (var key in keys)
                    if (key.StartsWith(prefix, StringComparison.Ordinal))
                    { Destroy(lines[key].gameObject); lines.Remove(key); }
            }
            else if (p.eventType == "nextCharacter") ClearAll();
            else if (p.eventType == "point")
            {
                if (!Finite(p.x) || !Finite(p.y) || !Finite(p.z)) continue;
                string key = p.character + ":" + p.stroke;
                if (!lines.TryGetValue(key, out var line))
                {
                    var obj = new GameObject("ReceivedStroke_" + key);
                    obj.transform.SetParent(transform, false);
                    line = obj.AddComponent<LineRenderer>();
                    line.useWorldSpace = true;
                    line.sharedMaterial = lineMaterial;
                    line.startWidth = line.endWidth = lineWidth;
                    line.positionCount = 0;
                    line.numCapVertices = 8;
                    lines.Add(key, line);
                }
                int index = line.positionCount;
                line.positionCount = index + 1;
                line.SetPosition(index, new Vector3(p.x, p.y, p.z) * positionScale + positionOffset);
            }
            // strokeEnd needs no action: each stroke already has its own line.
        }
    }

    private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    private void ClearAll()
    {
        foreach (var line in lines.Values) if (line != null) Destroy(line.gameObject);
        lines.Clear();
    }
    private void OnDisable()
    {
        running = false;
        socket?.Close();
        worker?.Join(1000);
        socket = null;
        worker = null;
        while (incoming.TryDequeue(out _)) { }
        ClearAll();
        session = null;
        lastSequence = -1;
    }
}
