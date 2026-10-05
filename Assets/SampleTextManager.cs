using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;


public class SampleTextManager : MonoBehaviour
{
    [SerializeField]
    private TMP_Text sampleText;

    private readonly string[] samples =
    {
        "",

        "ÅZ\nÅ†\nÅ¢",

        "Ç±\nÇÒ\nÇ…\nÇø\nÇÕ",

        "ç°\nì˙\nÇÕ\nê∞\nÇÍ"

    };

    public Logger logger;
    private int currentIndex = 0;



   

    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            currentIndex = 0;
            UpdateSample();
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            currentIndex = 1;
            UpdateSample();
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            currentIndex = 2;
            UpdateSample();
        }
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            currentIndex = 3;
            UpdateSample();
        }
    }
   
    void UpdateSample()
    {
        if (sampleText == null)
            Debug.Log("sampleText is null");

        sampleText.text = samples[currentIndex];

        if (logger == null)
            Debug.Log("logger is null");
        logger.taskNumber = currentIndex;
        logger.characterNumber = 1;
        logger.strokeNumber = 1;
        logger.eventName = "NextTask";
    }

    public void Initialize()
    {
        currentIndex = 0;
        UpdateSample();
    }

}