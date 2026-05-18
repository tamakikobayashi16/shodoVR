using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Net;
using UnityEngine.InputSystem;
using System.IO; // ファイル書き込みのために使用
using System;   // DateTime のために使用

public class HmdTracker : MonoBehaviour
{
    // --- UDP送信設定 ---
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;

    [SerializeField] private string serverIP = "127.0.0.1";
    [SerializeField] private int serverPort = 50000;

    [Tooltip("UDPデータを送信する間隔（秒）。0.1 = 1秒間に10回")]
    [SerializeField] private float sendInterval = 0.1f;

    private float nextSendTime = 0f;

    // --- 正面リセット用 ---
    private float calibrationYaw = 0f;

    // --- HMD/カメラ ---
    [SerializeField] private Transform hmdTransform;

    // --- ログファイル書き込み用 ---
    private StreamWriter logFile;

    void Start()
    {
        // --- UDPクライアントの初期化 ---
        try
        {
            udpClient = new UdpClient();
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
            Debug.Log($"UDP送信先: {serverIP}:{serverPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"UDPクライアントの初期化に失敗: {e.Message}");
            this.enabled = false;
            return;
        }

        // --- HMD Transform の初期化 ---
        if (hmdTransform == null)
        {
            Debug.Log("hmdTransformが設定されていなかったため、'this.transform' を使用します。");
            hmdTransform = this.transform;
        }

        ResetForwardDirection();
        nextSendTime = Time.time + sendInterval;

        // ★ 3. ログファイルの初期化 (保存場所を変更)
        try
        {
            // ★★★ 1. 保存先のフォルダパスを定義 ★★★
            // @ を先頭につけると、\ (バックスラッシュ) をそのままの文字として扱えます
            string savePath = @"C:\Users\admin\Desktop\HMDLog";

            // ★★★ 2. フォルダが存在するか確認し、なければ作成 ★★★
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
                Debug.Log($"保存フォルダを作成しました: {savePath}");
            }

            // ファイル名に現在時刻を含める
            string timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"HMD_Log_{timeStamp}.csv";

            // ★★★ 3. 保存パスとファイル名を結合 ★★★
            string filePath = Path.Combine(savePath, fileName);

            // ファイルを開く
            logFile = new StreamWriter(filePath, true, Encoding.UTF8);

            // 1行目にヘッダー（列の名前）を書き込む
            logFile.WriteLine("Timestamp,Pitch,RelativeYaw,Roll");

            Debug.Log($"ログファイルの保存を開始しました: {filePath}");
        }
        catch (Exception e)
        {
            // (重要) 指定パスへのアクセス権限がない場合、ここにエラーが出ます
            Debug.LogError($"ログファイルの初期化に失敗: {e.Message}");
            Debug.LogError("指定されたパス（デスクトップなど）への書き込み権限があるか確認してください。");
            logFile = null; // ログ機能を無効化
        }
    }

    public void ResetForwardDirection()
    {
        if (hmdTransform == null) return;
        calibrationYaw = hmdTransform.eulerAngles.y;
        Debug.Log($"正面をリセットしました。基準Yaw: {calibrationYaw:F2}");
    }

    void Update()
    {
        // --- リセット入力チェック ---
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ResetForwardDirection();
        }

        // --- 送信タイミングのチェック ---
        if (Time.time < nextSendTime)
        {
            return; // 送信をスキップ
        }

        nextSendTime += sendInterval;

        if (hmdTransform == null) return;

        // --- 現在のオイラー角を取得 ---
        Vector3 currentEuler = hmdTransform.eulerAngles;

        // --- 相対的な回転量を計算 ---
        float pitch = currentEuler.x;
        float roll = currentEuler.z;
        float relativeYaw = Mathf.DeltaAngle(calibrationYaw, currentEuler.y);

        // --- UDP送信用データ ---
        string dataToSend = $"{pitch:F4},{relativeYaw:F4},{roll:F4}";

        // ★ 4. ログファイルへの書き込み
        if (logFile != null)
        {
            string logTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logLine = $"{logTimestamp},{dataToSend}";

            logFile.WriteLine(logLine);
        }

        // --- UDPデータを送信 ---
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(dataToSend);
            udpClient.Send(data, data.Length, remoteEndPoint);
        }
        catch (Exception e)
        {
            if (this.enabled)
            {
                Debug.LogError($"UDP送信エラー: {e.Message}");
                this.enabled = false;
            }
        }
    }

    // ★ 5. アプリケーション終了時にファイルを閉じる
    void OnApplicationQuit()
    {
        if (logFile != null)
        {
            Debug.Log("ログファイルを閉じています...");
            logFile.Close();
            logFile = null;
        }
    }

    void OnDestroy()
    {
        if (udpClient != null)
        {
            udpClient.Close();
        }

        if (logFile != null)
        {
            logFile.Close();
            logFile = null;
        }
    }
}