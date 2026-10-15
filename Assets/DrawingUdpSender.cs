using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

// Assign this component to each drawing script in the Inspector.
public class DrawingUdpSender : MonoBehaviour
{
    public string receiverIp = "127.0.0.1";
    [Range(1, 65535)] public int receiverPort = 5005;
    public bool requireSessionHandshake = true;
    public int controlPort = 5006;
    public bool DeviceReady { get; set; }
    public string TrialId => requireSessionHandshake ? link?.State.trialId : session;
    public bool CanDraw => !requireSessionHandshake || (link != null && link.CanDraw &&
        link.LocalReady && DeviceReady && recorder != null && recorder.Error == null && sendError == null);
    RemoteTrialLink link;
    RemoteTrialRecorder recorder;
    string recordedTrial, lastPhase, sendError, completionPacket;
    float nextCompletionRetry;
    Font statusFont;
    private UdpClient client;
    private IPEndPoint destination;
    private string session;
    private int sequence;

    [Serializable]
    private class Packet
    {
        public int version = 1;
        public string session, method, eventType;
        public int sequence, character, stroke;
        public float x, y, z, pressure;
    }

    private void OnEnable()
    {
        session = Guid.NewGuid().ToString("N");
        sequence = 0;
        if (!IPAddress.TryParse(receiverIp, out var address) || receiverPort < 1 || receiverPort > 65535)
        {
            Debug.LogError("Drawing UDP: enter a valid receiver IP and port.", this);
            return;
        }
        try
        {
            destination = new IPEndPoint(address, receiverPort);
            client = new UdpClient(address.AddressFamily);
            if (requireSessionHandshake) link = new RemoteTrialLink(false, controlPort, receiverIp);
        }
        catch (SocketException e) { Debug.LogError($"Drawing UDP: {e.Message}", this); }
    }

    public void Send(string method, string eventType, Vector3 position, int character, int stroke, float pressure = 0f)
    {
        if (client == null || (requireSessionHandshake && !CanDraw)) return;
        var packet = new Packet {
            session = session, sequence = sequence++, method = method, eventType = eventType,
            character = character, stroke = stroke,
            x = position.x, y = position.y, z = position.z, pressure = pressure
        };
        string json = JsonUtility.ToJson(packet);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        try { client.Send(bytes, bytes.Length, destination); }
        catch (SocketException e)
        {
            Debug.LogError($"Drawing UDP send failed: {e.Message}. Disable and enable the component to retry.", this);
            sendError = e.Message;
        }
        recorder?.Record(eventType, character, stroke, position, pressure, packet.sequence);
        if (eventType == "experimentEnd")
        {
            completionPacket = json;
            nextCompletionRetry = Time.realtimeSinceStartup + 0.5f;
        }
    }

    private void Update()
    {
        if (link == null) return;
        link.DeviceReady = DeviceReady && recorder?.Error == null && sendError == null;
        link.LocalError = recorder?.Error ?? sendError;
        link.Tick();
        var state = link.State;
        if (!string.IsNullOrEmpty(state.trialId) && recordedTrial != state.trialId)
        {
            if (recorder != null)
            {
                if (lastPhase != "completed") recorder.Record("trialAborted", detail: "learner retried");
                recorder.Dispose();
            }
            recordedTrial = session = state.trialId;
            sequence = 0;
            completionPacket = null;
            sendError = null;
            lastPhase = null;
            recorder = new RemoteTrialRecorder(session, "teacher", state.prompt, state.participantId, false);
            Debug.Log("Trial records: " + recorder.DirectoryPath, this);
        }
        if (recorder != null && lastPhase != state.phase)
        {
            lastPhase = state.phase;
            recorder.Record(state.phase, detail: state.error);
        }
        // Repeat only the terminal packet until the learner acknowledges completion via control status.
        if (completionPacket != null && state.phase == "drawing" && link.Connected &&
            Time.realtimeSinceStartup >= nextCompletionRetry && client != null)
        {
            nextCompletionRetry = Time.realtimeSinceStartup + 0.5f;
            byte[] bytes = Encoding.UTF8.GetBytes(completionPacket);
            try { client.Send(bytes, bytes.Length, destination); }
            catch (SocketException e) { sendError = e.Message; }
        }
    }

    private void OnGUI()
    {
        if (!requireSessionHandshake) return;
        if (statusFont == null) statusFont = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo", "Arial" }, 20);
        var style = new GUIStyle(GUI.skin.box) { font = statusFont, fontSize = 20, wordWrap = true };
        string message = "体験者の接続を待っています…";
        if (link?.TimedOut == true) message = "通信が確認できません。接続を確認し、体験者側で「やり直す」を押してください。";
        else if (link?.Connected == true)
        {
            var state = link.State;
            message = state.phase == "error" ? "体験者側でやり直してください。\n" + state.error
                : state.phase == "completed" ? "体験者が全文字をなぞり終えました。"
                : state.phase == "templateCompleted" || state.phase == "tracing" ? "お手本を受信済みです。体験者が準備・なぞりを行っています。"
                : !DeviceReady ? "ペンタブと描画プレハブの準備を確認してください。"
                : CanDraw ? "お手本を書いてください。Space：次の文字　R：書き直し　Enter：完成"
                : "接続しました。両側の準備完了を待っています。";
            if (!string.IsNullOrEmpty(state.prompt)) message += "\nお題：" + state.prompt;
        }
        if (sendError != null || recorder?.Error != null || link?.Error != null)
            message = recorder?.Error ?? link?.Error ?? sendError;
        GUI.Box(new Rect(15, 15, Mathf.Min(760, Screen.width - 30), 110), message, style);
        if (link != null && link.Connected && link.State.phase == "waiting" && !link.LocalReady &&
            DeviceReady && recorder != null && recorder.Error == null &&
            GUI.Button(new Rect(15, 135, 200, 45), "準備完了"))
        {
            link.LocalReady = true;
            recorder.Record("teacherReady");
        }
    }

    private void Close()
    {
        client?.Close(); client = null;
        link?.Dispose(); link = null;
        if (recorder != null && lastPhase != "completed") recorder.Record("trialAborted", detail: "sender closed");
        recorder?.Dispose(); recorder = null;
        recordedTrial = null;
        if (statusFont != null) Destroy(statusFont);
    }
    private void OnDisable() { Close(); }
    private void OnDestroy() { Close(); }
}
