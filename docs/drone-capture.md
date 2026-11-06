# ドローン組立への接続とCSV取得

## 現在の実体と確認済み範囲

ローカルのUnity Hub登録プロジェクトで以下を確認した。

```text
C:\Users\acd-pc67\Downloads\fusion-vr-training-2.0.6
Unity 6000.0.58f2
```

元の`Assets/Training/Scene/Drone Training.unity`を残し、検証用の`Drone Training Capture.unity`を生成した。
元シーンのSHA256は組み込み前後で一致：`5DAB83647B540D224FB878CCA2D811677F5B7FA7E5261A34EAFCDAB2A55C0B93`。
新しいシーンだけをBuild Settingsへ追加。SDKコード・元Prefab・App ID設定は置き換えていない。

- 実プロジェクトで新規モジュールのコンパイル成功。
- シーンに配置済みのNetworkGrabbable 2件へ、把持・解放イベント計4件の購読設定を検証。
- 初期シーンでAttachmentPointとLearnerActivityTrackerは検出0件。接続後に生成されるオブジェクト向けのRuntimeBinderを追加したが、実セッションでの動的接続は未検証。
- 研究プロジェクトでEditMode 33/33件、PlayMode 6/6件成功。
- 移植先ドローンプロジェクトでも、記録処理のPlayModeテスト2/2件成功。
- テストはテスト用イベント源からの通知。人間のVR操作、実際の取付成功・失敗・並行作業を取得した実験ではない。

実作業ログは未取得。以下の手動操作で取得する。未検証の取付接続を完了済みとは扱わない。

## まず行う操作

1. 上記プロジェクトをUnity 6000.0.58f2で開く。
2. `Assets/Training/Scene/Drone Training Capture.unity`を開く。
3. Playし、既存サンプルの通常の接続・作業開始手順を実行する。テスト用の接続先・参加者条件は通常の研究設定を使う。
4. 作業オブジェクト生成後、約1秒待ち、Game画面の`Runtime bindings`と`Listener`を確認する。動的な活動が生成されたのにactivitiesが0のままなら、ログ取得済みとせず接続箇所を調べる。
5. 一つの部品を把持→解放→取付。可能なら取付解除→再把持も行う。通知の種類と件数はCSVで確認する。
6. Playを停止する。画面に表示されたOutputフォルダーを開き、`events-audio.csv`と元観測を保存する。

既存の設定に基づく既定保存先：

```text
C:\Users\acd-pc67\AppData\LocalLow\Photon\FusionTraining Unity6\SceneTokenAssemblyCaptures\<capture-id>\
```

実際のパスは`AssemblyCaptureRecorder.DirectoryPath`またはGame画面のOutputを正とする。
出力はイベント発生時に作成され、書き込みごとにflushする。

| ファイル | 内容 |
|---|---|
| events-audio.csv | 作業イベントとその時点の8項目の音響Token。音源文脈ごとに1行 |
| observations.ndjson | CSV化・意味関係生成前のコールバック観測、姿勢、音響文脈 |
| sender-frames.ndjson | 同じ観測から生成した意味関係。既存比較基盤への入力候補 |
| capture-info.json | セッション、観測者、時刻系、Unity版、作成時刻 |

同じイベントの複数CSV行はevent_idでまとめる。CSV行数を操作数とみなさない。
ログに試験用の手動操作を含めた場合も、人間の意図の独立した正解データにはならない。

## 8項目の音響Tokenとの関連付け

8項目の型自体は変更しない。CSVの共通メタデータで関係付ける。

- `session_id / recorder_id / event_id / clock_id`：試行・観測者・イベント・時刻系。
- `actor_id / object_id / target_id / attempt_id`：観測できた操作関係。分からない値はunknown。
- `observed_at_s`：コールバックを受けたローカル単調時刻。
- `audio_timestamp_s`：音響文脈を採取したローカル単調時刻。イベント時刻と同一値に偽装しない。
- `actor_audio_source_id / audio_source_id / actor_audio_match`：作業者と音源参照の明示的な対応。
- `audio_timestamp_s / audio_source_id / distance_m / direction_x,y,z / occlusion / task_phase / priority / time_to_deadline_s`：既存8項目。方向は3列だが1項目。

実際のVoice音源が設定されていない場合は、NetworkGrabberの親NetworkRigのNetworkHeadset姿勢を採取する。
`audio_source_kind=tracked_head_pose`として区別し、実音声特徴や話者認識を取得したとはしない。
位置取得専用の`SceneStateCollector.sourceTransform`を追加し、音声の再生・制御なしに既存の距離・方向生成を再利用する。
遮蔽・作業段階・重要度・期限の観測元が未設定なら、既存の欠測／既定値（-1、Unknown、0、-1）を保持する。未取得を成功値に置き換えない。
音響文脈がなければ`audio_available=false`、対応する列は空欄。

時刻は同一Unityプロセス内でのみ直接比較できる。複数端末の同期は未実装。
複数参加者で取得する場合はセッション名をそろえ、recorder_idを分け、端末間時刻の対応を別に測定する。

## イベントの意味と限界

- `grab_confirmed`：NetworkGrabbable.onDidGrab。作業者は既知のResearchEntityIdentity、なければコールバック元GrabberのStateAuthorityをセッション内Player参照として記録。個人名と同一視しない。
- `release_observed`：onDidUngrab。主体の根拠は直前に確定したGrabberで、別欄に明示。
- `attachment_registered / attachment_unregistered`：AttachmentPointの通知。通知に操作者が含まれないためactor_idはunknown。直前の把持者は補足根拠にだけ残す。
- `activity_snapshot / activity_status_changed`：LearnerActivityTrackerの現在状態・変化。Failed/Succeeded等は元の文字列で残す。工程失敗と個別取付失敗を混同しない。
- `grab_snapshot / attachment_snapshot`：接続処理が後から付いた時点の初期状態。操作が今発生したイベントとして扱わない。
- 新たな把持は新しいgrab-episode。再試行と自動認定しない。
- 取付候補が見つからないだけでは失敗を生成しない。個別操作の失敗は実際の失敗分岐から`RecordExplicitFailure`を呼ぶ必要があり、現SDKの無通知経路は未接続。

RuntimeBinderは0.2秒ごとに新規生成物を探す。購読前の短い操作は取得できない可能性があり、初期スナップショットを操作履歴の完全取得の代用にしない。
SDKのRenderコールバック時刻を記録するため、操作がシミュレーション内で成立した正確なtickとは異なる。
同じ物理的取付が部品両端・複数クライアントから観測される可能性がある。各観測をログとして区別し、物理的操作数の採点は独立した照合が必要。
発話入力は`RecordUtterance`の明示入力のみで、自動音声認識・指示と操作の因果対応の推定は行わない。

## 再現・再インストール

研究リポジトリから実行する。

```powershell
.\Tools\Install-DroneCapture.ps1 -ProjectPath 'C:\Users\acd-pc67\Downloads\fusion-vr-training-2.0.6'
.\Tools\Run-DroneCapture.ps1 -Operation Verify
.\Tools\Run-DroneCapture.ps1 -Operation Test
```

初回の検証シーン作成だけ`-Operation Prepare`を使う。既存の検証シーンを自動上書きしない。
配置は`Assets/SceneTokenResearchIntegration`に限定。元SDKやモデル・認証設定を研究リポジトリへコピーしない。
ReflectionによるSDK非依存の購読をEditorで検証しており、IL2CPP/QuestのAOT・ストリッピング対応は未検証。

実作業ログ取得後は、録画・独立した注釈と照合して把持・取付・状態遷移の欠落を確認する。
sender-framesだけから正解ファイルを生成して再現精度を採点しない。
