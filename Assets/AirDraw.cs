using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class AirDraw : MonoBehaviour
{
    public Transform controller;
    public Transform plane;

    public GameObject linePrefab;
    public Transform cursorSphere;

    [SerializeField]
    private float drawScale = 1.0f;

    [SerializeField]
    private float downThreshold = -0.03f;

    [SerializeField]
    private float upThreshold = -0.02f;

    private Vector3 origin;

    private bool penDown = false;
    private bool originSet = false;
    private bool wasPenDown = false;

    private LineRenderer currentLine;

    private List<Vector3> points = new();
    private List<GameObject> strokes = new();

    [Header("Haptics")]
    [SerializeField]
    private XRBaseController controllerHaptics;

    [SerializeField]
    private float downAmplitude = 0.8f;

    [SerializeField]
    private float downDuration = 0.08f;

    [SerializeField]
    private float upAmplitude = 0.3f;

    [SerializeField]
    private float upDuration = 0.03f;

    [Header("Reset")]
    public InputActionProperty resetAction;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //原点設定
        if (!originSet)
        {
            origin = controller.position;
            originSet = true;
        }

        //XY座標
        Vector3 delta = controller.position - origin;

        Vector3 drawPos =
        new Vector3(
        delta.x * drawScale,
        delta.y * drawScale,
        10f);

        cursorSphere.position = drawPos;
        
        //ペンダウン判定
        float z = delta.z;
        //ヒステリシス
        if (!penDown && z < downThreshold)
        {
            penDown = true;
        }
        else
        if (penDown && z > upThreshold)
        {
            penDown = false;
        }

        

        if(penDown && !wasPenDown)
        {
             CreateStroke();
            //振動
             controllerHaptics.SendHapticImpulse(
                downAmplitude,
                downDuration);
        }

        if (penDown)
        {
            AddPoint(drawPos);
        }

        if (!penDown && wasPenDown)
        {
            EndStroke();
            //振動
            controllerHaptics.SendHapticImpulse(
                upAmplitude,
                upDuration);
        }

        //原点リセット
        if (resetAction.action.WasPressedThisFrame())
        {
            origin = controller.position;
        }

        //文字消去
        if (Keyboard.current != null &&
    Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            foreach (GameObject stroke in strokes)
            {
                Destroy(stroke);
            }

            strokes.Clear();

            currentLine = null;

            points.Clear();
        }

        void CreateStroke()
        {
            GameObject obj = Instantiate(linePrefab);

            currentLine = obj.GetComponent<LineRenderer>();

            currentLine.positionCount = 0;

            points.Clear();

            strokes.Add(obj);
        }

        void AddPoint(Vector3 point)
        {
            if (points.Count > 0 &&
                Vector3.Distance(points[^1], point) < 0.005f)
                return;

            points.Add(point);

            currentLine.positionCount = points.Count;
            currentLine.SetPositions(points.ToArray());
        }

        void EndStroke()
        {
            currentLine = null;
        }

        wasPenDown = penDown;

    }
}
