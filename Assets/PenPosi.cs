using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.InputSystem.Controls;

public class PenPosi : MonoBehaviour
{
    public DrawingUdpSender udpSender;
    private int udpCharacter;
    private int udpStroke;
    public LineRenderer line;
    public Transform plane;
    

    [Header("Scale")]
    [SerializeField]
    private float drawScale = 1.0f;

    [Header("Pressure")]
    [SerializeField]
    private float drawPressureThreshold = 0.05f;

    [SerializeField]
    private Logger logger;

    public GameObject linePrefab;

    private LineRenderer currentLine;
    private List<GameObject> strokes = new List<GameObject>();

    private Vector3 lastPos;
    private Vector3 p;
    private bool firstPoint = true;
    private float wasPressure = 0f;



    private float pressure;
    private bool inRange;

    private bool drawingNow;
    private bool drawingBefore;
    private bool pressed;

    [Header("ペン先表示")]
    [SerializeField]
    private Transform penTipSphere;

    void Start()
    {
        line.positionCount = 0;

        line.startWidth = 30f;
        line.endWidth = 30f;

        line.useWorldSpace = true;
    }

    void Update()
    {

        if (Pen.current == null)
            return;

        pressure = Pen.current.pressure.ReadValue();
        inRange = Pen.current.inRange.isPressed;

        drawingNow = pressure >= drawPressureThreshold;
        drawingBefore = wasPressure >= drawPressureThreshold;
        pressed = Pen.current.tip.isPressed;

        //書き直し
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            foreach (GameObject obj in strokes)
            {
                Destroy(obj);
            }

            strokes.Clear();

            currentLine = null;
            firstPoint = true;

            udpStroke = 0;
            drawingBefore = false;
            udpSender?.Send("PenTablet", "clearCharacter", Vector3.zero, udpCharacter, udpStroke);
            logger.redoCount++;
            logger.strokeNumber = 1;
            logger.eventName = "Redo";
        }
        //次の文字
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {

            foreach (GameObject obj in strokes)
            {
                Destroy(obj);
            }

            strokes.Clear();

            currentLine = null;
            firstPoint = true;

            udpCharacter++;
            udpStroke = 0;
            drawingBefore = false;
            udpSender?.Send("PenTablet", "nextCharacter", Vector3.zero, udpCharacter, udpStroke);
            logger.characterNumber++;
            logger.strokeNumber = 1;
            logger.eventName = "NextCharacter";

            
        }
        string penState;

            if (!inRange)
                penState = "OutOfRange";
            else if (drawingNow)
                penState = "Down";
            else
                penState = "Hover";

            logger.Log(

                p,
                pressure,
                inRange,
                penState
            );

        Vector2 pos = Pen.current.position.ReadValue();

        // 実測値
        float minX = 0f;
        float maxX = 1920f;

        float minY = 0f;
        float maxY = 1080f;

        // 0～1に正規化
        float u = (pos.x - minX) / (maxX - minX);
        float v = (pos.y - minY) / (maxY - minY);

        float centerX = (minX + maxX) / 2f;
        float centerY = (minY + maxY) / 2f;

        float normX = (pos.x - centerX) / ((maxX - minX) / 2f);
        float normY = (pos.y - centerY) / ((maxY - minY) / 2f);

        p = new Vector3(
            normX * drawScale * 1.920f,
            normY * drawScale * 1.080f,
            9.5f
         );

        

        // カーソル表示
        if (penTipSphere != null)
        {
            penTipSphere.gameObject.SetActive(inRange);

            if (inRange)
            {
                penTipSphere.position = p;
            }
        }

        
        // 描画開始
        

        if (drawingNow && !drawingBefore)
        {
            GameObject obj = Instantiate(linePrefab);

            currentLine = obj.GetComponent<LineRenderer>();

            currentLine.positionCount = 0;
            currentLine.startWidth = 0.3f;
            currentLine.endWidth = 0.3f;

            strokes.Add(obj);

            firstPoint = true;
        }

        // 描画中
        if (drawingNow && currentLine != null)
        {
            if (firstPoint ||
                Vector3.Distance(p, lastPos) > 0.01f)
            {
                currentLine.positionCount++;

                currentLine.SetPosition(
                    currentLine.positionCount - 1,
                    p);

                udpSender?.Send("PenTablet", "point", p, udpCharacter, udpStroke, pressure);
                lastPos = p;
                firstPoint = false;
            }
        }

        // ペンを離した
        if (!drawingNow && currentLine != null)
        {
            currentLine = null;
            firstPoint = true;

            udpSender?.Send("PenTablet", "strokeEnd", p, udpCharacter, udpStroke, pressure);
            udpStroke++;
            logger.strokeNumber++;
        }

        wasPressure = pressure;
        
    }
}