using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

using System.Net;
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
    private int strokeIndex = 0;

    [Header("Hit Point")]
    public Transform hitPointVisual;

    [Header("UDP")]
    public string ipAddress = "127.0.0.1";
    public int port = 5005;

    private UdpClient udpClient;

    // 現在描画中の線
    private LineRenderer currentLine;

    // 線の点列
    private List<Vector3> points =
        new List<Vector3>();

    // 全LineRenderer管理
    private List<GameObject> allLines =
        new List<GameObject>();

    // 文字単位管理
    private List<List<Vector3>> characters =
        new List<List<Vector3>>();

    private List<Vector3> currentCharacter =
        new List<Vector3>();

    private int characterIndex = 0;

    void Start()
    {
        udpClient = new UdpClient();

        characters.Add(currentCharacter);
    }

    void Update()
    {
        // Trigger入力
        bool isDrawing =
            triggerAction.action.ReadValue<float>() > 0.1f;

        // Ray生成
        Ray ray = new Ray(
            transform.position,
            transform.forward
        );

        // Debug表示
        Debug.DrawRay(
            transform.position,
            transform.forward * rayDistance,
            Color.red
        );

        // Raycast
        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            rayDistance,
            drawLayer))
        {
            // 衝突位置に球表示
            if (hitPointVisual != null)
            {
                hitPointVisual.position =
                    hit.point + hit.normal * 0.02f;
            }

            // 描画中
            if (isDrawing)
            {
                if (currentLine == null)
                {
                    CreateNewLine();
                }

                AddPoint(
                    hit.point + hit.normal * 0.02f
                );
            }
            else
            {
                currentLine = null;

                strokeIndex++;
            }
        }

        // Nキーで次文字
        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            NextCharacter();
        }
    }

    void CreateNewLine()
    {
        GameObject lineObj =
            new GameObject("DrawLine");

        currentLine =
            lineObj.AddComponent<LineRenderer>();

        currentLine.material = lineMaterial;

        currentLine.startWidth = lineWidth;
        currentLine.endWidth = lineWidth;

        currentLine.positionCount = 0;

        currentLine.useWorldSpace = true;

        currentLine.numCornerVertices = 10;
        currentLine.numCapVertices = 10;

        currentLine.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        currentLine.receiveShadows = false;

        points.Clear();

        allLines.Add(lineObj);
    }

    void AddPoint(Vector3 point)
    {
        // 点が近すぎたら追加しない
        if (points.Count > 0)
        {
            float distance =
                Vector3.Distance(
                    points[points.Count - 1],
                    point
                );

            if (distance < 0.005f)
                return;
        }

        points.Add(point);

        currentCharacter.Add(point);

        SendPoint(point);

        currentLine.positionCount =
            points.Count;

        currentLine.SetPositions(
            points.ToArray()
        );
    }

    void SendPoint(Vector3 point)
    {
        string message =
            point.x + "," +
            point.y + "," +
            strokeIndex + "," +
            characterIndex;

        byte[] data =
            Encoding.UTF8.GetBytes(message);

        udpClient.Send(
            data,
            data.Length,
            ipAddress,
            port
        );
    }

    void ClearLines()
    {
        foreach (GameObject line in allLines)
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

        currentCharacter =
            new List<Vector3>();

        characters.Add(currentCharacter);

        ClearLines();
    }
}