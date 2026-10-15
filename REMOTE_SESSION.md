# 遠隔お手本：接続確認・やり直し・試行記録

体験者側SHOnoMITI_HMDの`codex/remote-template-preview`と組み合わせる。PenTabletを対象とする。

1. 既存のPenPosiのUdp SenderにDrawingUdpSenderが割り当てられていることを確認する。
2. DrawingUdpSenderのReceiver IPを体験者PCのIPv4へ変更する。Receiver Portは5005、Control Portは5006。Require Session Handshakeを有効にする（既定値）。追加のアタッチは不要。
3. 体験者側でRemotePreviewを本番モードで起動する。接続後、両側で「準備完了」を押す。ペンタブとLineRenderer付き描画プレハブが必要。
4. 画面の案内後に書く。Spaceで次の文字、Rで現在の文字の書き直し、Enterで全文字の完成。
5. エラー時は体験者側で「やり直す」を押す。こちらも新しい試行IDへ切り替わり、筆跡・文字番号・完成状態をリセットする。再び準備完了を押す。

## 描画用Planeの表示

PenPosiのPlaneに割り当てられたオブジェクトは、起動直後・接続待ち・準備待ちには表示しない。両側の準備完了後、体験者側から描画開始状態が届くと表示する。完成後は表示を維持し、やり直し・通信切断・エラー時は再び隠す。PlaneのRendererだけを隠すため、入力や通信コンポーネントは止まらない。

SampleSceneのPentabにはDrawingUdpSenderを追加・割り当て済み。別PCで使用する前にReceiver IPを変更する。独自シーンではPenPosiのPlaneとUdp Senderを割り当てる。送信コンポーネントが未設定の場合はPlaneを隠して描画を待機する。Require Session Handshakeを無効にした単体動作では開始待ちを行わない。

接続確認は0.5秒ごと。5秒応答がなければ描画できない。最初の接続待ちは20秒。体験者側では描画データの停止（既定60秒）も検出する。完成合図だけは受信確認まで再送する。途中のデータ欠落は体験者側で検出し、試行をやり直す。後から来た古い試行の筆跡は使わない。

## 記録

`Application.persistentDataPath/RemoteTrials/<共通試行ID>/`に`teacher_metadata.json`と`teacher_events.jsonl`を保存する。Windowsでは通常`%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/RemoteTrials/`。Consoleにも保存先を出す。

お題・匿名参加者IDは体験者側から受信する。各イベントに試行ID、UTC時刻、ローカル経過秒、座標、筆圧、文字・画・通し番号を記録する。同じIDの体験者側の記録と対応付けられる。ファイル転送や時計同期は行わない。

連携モードのPenPosiは従来Loggerへの毎フレーム記録を使わず、この試行記録を使用する。Require Session Handshakeを無効にすると従来の単方向送信・Logger方式になるが、新しい体験者側の本番モードとは接続できない。他の入力方式の挙動を変えないために必要な場合のみ無効にする。

## 確認

対応Unityは2022.3.62f2。実際のDrawingUdpSenderを使ったループバック連携テストは体験者側リポジトリのTests/RemoteHandwriting/SessionFlowSmoke.csにある。実際の2台のPC・ペンタブを含む操作は別途確認が必要。
