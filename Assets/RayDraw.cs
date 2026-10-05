using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Net.Sockets;
using System.Text;

public class RayDraw : MonoBehaviour
{
    [Header("Ray Settings")]
    public float rayDistance = 20f;
    public LayerMask drawLayer;

    [Header("Input")]
    public InputActionProperty triggerAction;

    [Header("Line")]
    public Material lineMaterial;
    public float lineWidth = 0.02f;

    [Header("Hit Point")]
    public Transform hitPointVisual;

    [Header("UDP")]
    public string ipAddress = "127.0.0.1";
    public int port = 5005;

    private UdpClient udpClient;

    private LineRenderer currentLine;
    private List<Vector3> points = new List<Vector3>();
    private List<GameObject> allLines = new List<GameObject>();

    // ===== ç\ë¢â¸ëP =====
    private List<List<List<Vector3>>> characters = new();
    private List<List<Vector3>> currentCharacter = new();
    private List<Vector3> currentStroke = new();

    private int characterIndex = 0;
    private int strokeIndex = 0;

    private bool wasDrawing = false;

    void Start()
    {
        udpClient = new UdpClient();
        characters.Add(currentCharacter);
    }

    void Update()
    {
        bool isDrawing = triggerAction.action.ReadValue<float>() > 0.1f;

        HandleStrokeState(isDrawing);

        Ray ray = new Ray(transform.position, transform.forward);

        Debug.DrawRay(transform.position, transform.forward * rayDistance, Color.red);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, drawLayer))
        {
            if (hitPointVisual != null)
            {
                hitPointVisual.position = hit.point + hit.normal * 0.02f;
            }

            if (isDrawing)
            {
                if (currentLine == null)
                {
                    CreateNewStroke();
                }

                AddPoint(hit.point + hit.normal * 0.02f);
            }
        }

        if (Keyboard.current != null &&
            Keyboard.current.nKey.wasPressedThisFrame)
        {
            NextCharacter();
        }
    }

    void HandleStrokeState(bool isDrawing)
    {
        // âüÇµÇΩèuä‘
        if (isDrawing && !wasDrawing)
        {
            CreateNewStroke();
        }

        // ó£ÇµÇΩèuä‘
        if (!isDrawing && wasDrawing)
        {
            EndStroke();
        }

        wasDrawing = isDrawing;
    }

    void CreateNewStroke()
    {
        GameObject lineObj = new GameObject("Stroke");
        currentLine = lineObj.AddComponent<LineRenderer>();

        currentLine.material = lineMaterial;
        currentLine.startWidth = lineWidth;
        currentLine.endWidth = lineWidth;
        currentLine.useWorldSpace = true;

        currentLine.numCornerVertices = 8;
        currentLine.numCapVertices = 8;

        points = new List<Vector3>();

        currentStroke = new List<Vector3>();
        currentCharacter.Add(currentStroke);

        allLines.Add(lineObj);
    }

    void AddPoint(Vector3 point)
    {
        if (points.Count > 0)
        {
            if (Vector3.Distance(points[^1], point) < 0.005f)
                return;
        }

        points.Add(point);
        currentStroke.Add(point);

        SendPoint(point);

        currentLine.positionCount = points.Count;
        currentLine.SetPositions(points.ToArray());
    }

    void EndStroke()
    {
        currentLine = null;
        strokeIndex++;
    }

    void SendPoint(Vector3 point)
    {
        string message =
            point.x + "," +
            point.y + "," +
            point.z + "," +
            strokeIndex + "," +
            characterIndex;

        byte[] data = Encoding.UTF8.GetBytes(message);

        udpClient.Send(data, data.Length, ipAddress, port);
    }

    void ClearLines()
    {
        foreach (var line in allLines)
        {
            Destroy(line);
        }

        allLines.Clear();
        currentLine = null;
    }

    void NextCharacter()
    {
        characterIndex++;
        strokeIndex = 0;

        currentCharacter = new List<List<Vector3>>();
        characters.Add(currentCharacter);

        ClearLines();
    }
}