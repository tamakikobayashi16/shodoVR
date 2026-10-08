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
        }
        catch (SocketException e) { Debug.LogError($"Drawing UDP: {e.Message}", this); }
    }

    public void Send(string method, string eventType, Vector3 position, int character, int stroke, float pressure = 0f)
    {
        if (client == null) return;
        var packet = new Packet {
            session = session, sequence = sequence++, method = method, eventType = eventType,
            character = character, stroke = stroke,
            x = position.x, y = position.y, z = position.z, pressure = pressure
        };
        byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(packet));
        try { client.Send(bytes, bytes.Length, destination); }
        catch (SocketException e)
        {
            Debug.LogError($"Drawing UDP send failed: {e.Message}. Disable and enable the component to retry.", this);
            Close();
        }
    }

    private void Close() { client?.Close(); client = null; }
    private void OnDisable() { Close(); }
    private void OnDestroy() { Close(); }
}
