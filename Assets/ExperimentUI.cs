using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class ExperimentUI : MonoBehaviour
{
    public TMP_InputField participantInput;
    public TMP_Dropdown methodDropdown;
    

    public Logger logger;
    public SampleTextManager sampletextmanager;
    public GameObject settingPanel;

    [SerializeField]
    private GameObject settingCanvas;

    public void StartExperiment()
    {

        Debug.Log("Start Button");
        logger.participantID = int.Parse(participantInput.text);
        

        switch (methodDropdown.value)
        {
            case 0:
                logger.inputMethod = "PenTablet";
                break;

            case 1:
                logger.inputMethod = "Ray";
                break;

            case 2:
                logger.inputMethod = "AirDraw";
                break;
        }

        settingPanel.SetActive(false);

        logger.BeginLogging();
        sampletextmanager.Initialize();

        settingCanvas.SetActive(false);
    }
}