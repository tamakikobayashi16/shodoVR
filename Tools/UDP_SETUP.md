# ペンタブ座標を別の Unity PC にリアルタイム送信

## 送信側

1. 描画シーンの GameObject に DrawingUdpSender を追加。
2. Receiver Ip を受信 PC の LAN IPv4 アドレスに、Receiver Port を 5005 に設定。127.0.0.1 は同じ PC 内の確認専用。
3. PenPosi の Udp Sender 欄へそのコンポーネントを割り当てる。
4. 設定は Play Mode 開始前に行う。実行中に変更した場合は送信コンポーネントを無効化・再有効化する。

## 受信側 Unity

1. Assets/DrawingUdpReceiver.cs と .meta を受信側プロジェクトへコピー。
2. 空の GameObject に DrawingUdpReceiver を追加。
3. Listen Port を 5005 に設定し、使用するレンダーパイプライン対応の線用 Material を Line Material に割り当てる。
4. 両 PC が通信できる LAN に接続し、受信側ファイアウォールで送信 PC からの UDP 5005 を許可。
5. 受信側を Play Mode にしてから送信側で描く。

送る座標は PenPosi が線に追加した Unity ワールド座標（通常 Z=9.5）。受信カメラから見えるよう Position Offset / Position Scale を調整する。受信オブジェクトの Transform は座標変換には使わない。
Space で次の文字、R で現在の文字を消去すると受信側の線も消える。線は画ごとに分かれる。Logger の既存参照・保存先も送信側で正常に設定する必要がある。

## データ形式

1パケットに1つの UTF-8 JSON:

```json
{"version":1,"session":"id","sequence":0,"method":"PenTablet","eventType":"point","character":0,"stroke":0,"x":1.25,"y":-0.5,"z":9.5,"pressure":0.7}
```

イベントは point / strokeEnd / nextCharacter / clearCharacter。文字・画番号は0始まりで、Loggerの課題番号とは独立。sessionは送信コンポーネント有効化ごとに変わる。sequenceは送信ごとに増加する。

受信スレッドでは文字列をキューに入れるだけで、JSON解析とLineRenderer更新はUnityメインスレッドで行う。受信側は単一の送信セッションを想定する。過去のsequence番号は破棄する。UDPの欠損や順序逆転に対する再送はなく、混雑時は点が抜ける可能性がある。受信開始前に描いた線は復元されない。完全な記録が必要なら信頼性のある通信方式を別途使う。

Tools/receive_drawing.py は任意の診断用受信サンプル（Python 3）。Unity受信と同じポートで同時起動しない。

検証: Python受信は実際のlocalhost UDPで不正パケット後も座標を受信できることを確認済み。Unity Editorがこのクラウドにないため、C#のコンパイル・Unity描画・2台のPC間通信は未検証。RayDraw / AirDraw / HmdTracker の既存処理は変更していない。
