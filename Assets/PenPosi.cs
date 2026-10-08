using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PenPosi : MonoBehaviour
{
    [Header("Scene References")]
    public LineRenderer line;
    public Transform plane; // Retained for compatibility with existing scene references.
    public GameObject linePrefab;
    public DrawingUdpSender udpSender;
    [SerializeField] private Logger logger;
    [SerializeField] private Transform penTipSphere;

    [Header("Drawing")]
    [SerializeField] private float drawScale = 1f;
    [SerializeField] private float drawPressureThreshold = 0.05f;

    private const float ScreenWidth = 1920f;
    private const float ScreenHeight = 1080f;
    private const float DrawingDepth = 9.5f;
    private const float MinimumPointDistance = 0.01f;
    private const float StrokeWidth = 0.3f;

    private readonly List<GameObject> strokes = new List<GameObject>();
    private LineRenderer currentLine;
    private Vector3 lastPosition;
    private bool wasDrawing;
    private bool experimentEnded;
    private int udpCharacter;
    private int udpStroke;

    private void Start()
    {
        if (line == null) return;
        line.positionCount = 0;
        line.startWidth = line.endWidth = 30f;
        line.useWorldSpace = true;
    }

    private void Update()
    {
        if (experimentEnded) return;
        var keyboard = Keyboard.current;
        if (keyboard != null &&
            (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
        {
            FinishExperiment();
            return;
        }

        var pen = Pen.current;
        if (pen == null) return;

        float pressure = pen.pressure.ReadValue();
        bool inRange = pen.inRange.isPressed;
        bool isDrawing = pressure >= drawPressureThreshold;
        Vector3 position = ToDrawingPosition(pen.position.ReadValue());

        HandleKeyboard();
        UpdateCursor(position, inRange);

        string penState = !inRange ? "OutOfRange" : isDrawing ? "Down" : "Hover";
        logger?.Log(position, pressure, inRange, penState);

        if (isDrawing && !wasDrawing) BeginStroke();
        if (isDrawing && currentLine != null) AddPoint(position, pressure);
        if (!isDrawing && currentLine != null) EndStroke(position, pressure);

        wasDrawing = isDrawing;
    }

    public void FinishExperiment()
    {
        if (experimentEnded) return;
        if (currentLine != null) EndStroke(lastPosition, 0f);
        Send("experimentEnd", Vector3.zero);
        experimentEnded = true;
        wasDrawing = false;
        if (penTipSphere != null) penTipSphere.gameObject.SetActive(false);
        if (logger != null)
        {
            logger.eventName = "ExperimentEnd";
            logger.Log(lastPosition, 0f, false, "End");
            logger.EndLogging();
        }
        Debug.Log("Drawing experiment finished.", this);
    }

    private Vector3 ToDrawingPosition(Vector2 position)
    {
        float x = (position.x - ScreenWidth / 2f) / (ScreenWidth / 2f);
        float y = (position.y - ScreenHeight / 2f) / (ScreenHeight / 2f);
        return new Vector3(x * drawScale * 1.920f, y * drawScale * 1.080f, DrawingDepth);
    }

    private void HandleKeyboard()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.rKey.wasPressedThisFrame) ResetCharacter(false);
        if (keyboard.spaceKey.wasPressedThisFrame) ResetCharacter(true);
    }

    private void ResetCharacter(bool advance)
    {
        foreach (var stroke in strokes) Destroy(stroke);
        strokes.Clear();
        currentLine = null;
        wasDrawing = false;
        udpStroke = 0;
        if (advance) udpCharacter++;

        Send(advance ? "nextCharacter" : "clearCharacter", Vector3.zero);
        if (logger == null) return;
        if (advance) logger.characterNumber++;
        else logger.redoCount++;
        logger.strokeNumber = 1;
        logger.eventName = advance ? "NextCharacter" : "Redo";
    }

    private void UpdateCursor(Vector3 position, bool inRange)
    {
        if (penTipSphere == null) return;
        penTipSphere.gameObject.SetActive(inRange);
        if (inRange) penTipSphere.position = position;
    }

    private void BeginStroke()
    {
        var stroke = Instantiate(linePrefab);
        currentLine = stroke.GetComponent<LineRenderer>();
        currentLine.positionCount = 0;
        currentLine.startWidth = currentLine.endWidth = StrokeWidth;
        strokes.Add(stroke);
    }

    private void AddPoint(Vector3 position, float pressure)
    {
        if (currentLine.positionCount > 0 &&
            Vector3.Distance(position, lastPosition) <= MinimumPointDistance) return;

        int index = currentLine.positionCount;
        currentLine.positionCount = index + 1;
        currentLine.SetPosition(index, position);
        Send("point", position, pressure);
        lastPosition = position;
    }

    private void EndStroke(Vector3 position, float pressure)
    {
        currentLine = null;
        Send("strokeEnd", position, pressure);
        udpStroke++;
        if (logger != null) logger.strokeNumber++;
    }

    private void Send(string eventType, Vector3 position, float pressure = 0f)
    {
        udpSender?.Send("PenTablet", eventType, position, udpCharacter, udpStroke, pressure);
    }
}
