using System.IO;
using UnityEngine;

public class Logger : MonoBehaviour
{
    private StreamWriter writer;
    private float startTime;

    [Header("Experiment Info")]
    public int participantID = 1;
    public string inputMethod = "PenTblet";   // 1:ペンタブ 2:レイ 3:空中筆記
    public int taskNumber = 1;
    public int characterNumber = 1;
    public int trialNumber = 1;
    public int redoCount = 0;
    public int strokeNumber = 1;
    public string eventName = "None";


    /// <summary>
    /// 毎フレーム呼ぶ
    /// </summary>
    public void Log(Vector3 pos, float pressure, bool inRange, string penState)
    {
        if (writer == null)
        {
            Debug.LogError("Writer is NULL");
            return;
        }
        float t = Time.time - startTime;

        writer.WriteLine(
            $"{t:F4}," +
            $"{Time.frameCount}," +
            $"{participantID}," +
                          $"{inputMethod}," +
            $"{taskNumber}," +
            $"{characterNumber}," +
            $"{strokeNumber}," +
            $"{redoCount}," +
            $"{pos.x:F4}," +
            $"{pos.y:F4}," +
            $"{pos.z:F4}," +
            $"{pressure:F3}," +
            $"{(inRange ? 1 : 0)}," +
            $"{penState}," +
            $"{eventName}");

        writer.Flush();

        // イベントは1フレームだけ記録
        eventName = "None";
    }

    private void OnApplicationQuit()
    {
        EndLogging();
    }

    public void EndLogging()
    {
        writer?.Close();
        writer = null;
    }

    public void BeginLogging()
    {
        CreateLog();
    }

    private void CreateLog()
    {
        startTime = Time.time;

        string folder = @"C:\Users\admin\Desktop\DrawLog";

        Directory.CreateDirectory(folder);

        string path = Path.Combine(
            folder,
            $"P{participantID:D3}_{inputMethod}_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");

        writer = new StreamWriter(path, false);

        writer.WriteLine(
            "Time,Frame,Participant,Method,Task,Character,Stroke,Redo,X,Y,Z,Pressure,InRange,PenState,Event");
    }
}