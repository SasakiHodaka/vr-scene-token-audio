# 初回調査・移植計画（mainのみの履歴記録）

**更新:** 全ブランチを調査した最新の結果は[移植実装計画](implementation-plan.md)を参照。以下はmainだけを調査した当初の記録であり、Photon統合ブランチにはVR・Fusion・Voice・ドローンシーンが存在する。新規プロジェクトの採用Unity版は6000.0.58f2へ更新した。

調査日: 2026-10-20。
対象: https://github.com/SasakiHodaka/xr-peripheral-presence-audio
固定コミット: `a85f53d9a4e9388d6f23268e180cf4072b55c29d`（取得時main）。
独立した調査用コピーで追跡ファイルを静的調査した。既存の作業プロジェクトは変更していない。
未追跡ファイル、他ブランチ、過去履歴は対象外。Unity起動・ビルド・Quest実機検証は未実施。

## 調査結果

公開版は周辺人物の状態を音響キューへ変換する単体デモであり、指示にあるPhoton協調作業システムは確認できない。別の移植元がなければVR・通信・双方向音声は新規実装となる。

|項目|確認結果|根拠（移植元相対パス）|
|---|---|---|
|Unity|2022.3.62f3、revision 96770f904ca7|ProjectSettings/ProjectVersion.txt|
|XRパッケージ|Meta XR Audio 85.0.0。OpenXR、XR Plugin Management、XR Interaction Toolkit、Input System、Meta XR Coreは依存一覧にない|Packages/manifest.json、packages-lock.json|
|組み込みXR|modules.xr、modules.vrあり。この指定だけではQuestローダー・入力構成にならない|Packages/manifest.json|
|Photon Fusion / Voice|SDK、設定、通信スクリプト、プレイヤーPrefabを追跡ファイル内で確認できない|全追跡ファイル一覧、Packages、Assets/Scripts|
|Meta音響|Audio SDK依存あり。Spatializer設定はMeta XR Audio。Audio/Acoustic/MaterialMapping設定あり|ProjectSettings/AudioManager.asset、Assets/Resources/MetaXR*.asset|
|VRプレイヤー|Main CameraとAudioListener。XR Origin・HMD/左右手追跡構成は確認できない|Assets/Scenes/SampleScene.unity|
|同期|NetworkRunner、NetworkObject等の実装なし。デモ人物の移動はローカル|Assets/Scripts/zyama/*Mover.cs|
|音声|AudioClip再生。isSpeakingやVキーは模擬発話状態。マイク送受信ではない|Assets/Scripts/GroupWorkPresenceAudio.cs、zyama/ExampleVoiceState.cs|
|シーン|追跡されたunityシーンはSampleSceneのみ。Build Settingsのシーン一覧は空|Assets/Scenes/SampleScene.unity、ProjectSettings/EditorBuildSettings.asset|

SampleSceneにはMain Camera、Directional Light、Plane、Table、RemoteAvater群、PeripheralSystem、PeripheralTargets、Target_Approach / Crossing / Speaking / Back、足音・呼吸・衣擦れのAudioSourceがある。
RemoteAvaterという名称だけでネットワークアバターとは判定できない。ドローン組み立てシーンは当該コミットにない。

## 音響の注意点

- PeripheralCueAudioEmitterは子AudioSourceを生成し、spatialBlend、距離ロールオフ、AudioLowPassFilter、reverbZoneMixを設定する。ただし生成時にspatialize=trueを設定していない。
- SampleSceneにはSpatialize=1だが無効化されたRemoteAvater音源と、Spatialize=0の足音等がある。SDK選択済みでも全音源のHRTF適用を保証できない。
- EnvironmentAcousticProfileのocclusionは視野外0.55・背後0.45を合算する近似。障害物のRaycastや伝搬経路判定ではない。Scene Tokenの実際の遮蔽としてそのまま転用しない。
- GroupWorkPresenceAudioは距離からvolumeを変更する。新実装ではUnity/SDKの距離減衰との二重適用を防ぐ。

## 選択的移植表

以下のScripts内のパスはAssets/Scripts/からの相対パス。既存コードのコピーはまだ行っていない。

|機能|移植元|依存|移植方法|
|---|---|---|---|
|距離・相対位置|zyama/PeripheralStateDetector.cs、PeripheralTarget.cs|Transform、対象リスト|計算方法を参考にSceneStateCollectorを新設。HMD座標系の方向、動的参加退出、障害物・作業状態を追加|
|状態から制御|zyama/PeripheralCueModel.cs|旧DetectionResult、AcousticProfile、学習モデル型|旧cue選択は移植せずAdaptiveAudioControllerと差替可能なルールポリシーを新設|
|音響パラメータ|zyama/EnvironmentAcousticProfile.cs|旧State、Mathf|分離設計の参考。視野外を遮蔽とする式は不採用。測定と制御を分ける|
|3D音源・フィルタ|zyama/PeripheralCueAudioEmitter.cs、PresenceAudioEmitter.cs|AudioSource、LowPassFilter、旧モデル・実験制御|SpatialAudioRendererAdapterに必要な役割のみ再実装。Photon Speakerへ接続し、clip差替やStopで受信を妨げない|
|CSV記録|zyama/PeripheralStateLogger.cs、PresenceDataLogger.cs|旧Detector / Model / Emitter / Trial / Experiment|InvariantCulture、persistentDataPathを参考にAudioEvaluationLoggerを新設。Token・制御出力・条件・試行・schema版を記録|
|試行・比較|zyama/PeripheralTrialController.cs、PeripheralCueExperimentController.cs、PeripheralCueTrialSequencer.cs|旧条件制御・キーボード入力・logger|試行管理の考え方のみ再利用。Quest入力と3方式の順序釣り合いを追加|
|表示|zyama/PeripheralDebugUI.cs|旧研究コンポーネント群|Token表示UIを新設|
|テスト生成|zyama/Editor/PeripheralResearchSetup.cs|UnityEditor、旧スクリプト群、clip/modelパス|4テストシーン用Editor処理を新設。旧シーン全体はコピーしない|
|Meta音響|Packages/manifest.json、ProjectSettings/AudioManager.asset、Assets/Resources/MetaXR*.asset|Meta XR Audio 85.0.0|公式配布から導入し設定を再構築。Unity/Android互換と実機動作を検証|
|VR表示・入力|該当実装なし|今後選定するXRローダー・入力・移動|別の移植元を確認、または新規構築|
|ルーム・位置回転同期・協調作業|該当実装なし|Fusion、アバター・操作権限|別の移植元を確認、または新規構築。維持すべき協調操作の具体化が必要|
|双方向音声|該当実装なし|Voice、Fusion連携、マイク権限|別の移植元を確認、または新規構築。player/sourceIdとSpeakerを対応付ける|

## 初期移植対象外

- Assets/Models/cue_model_unity.json、PeripheralCueLearnedModel.cs、PeripheralSimulationDatasetGenerator.cs、Toolsの既存学習・データ生成系。今回とは目的とラベルが異なる。
- Assets/presence_training_data.csv、既存ログ、過去の研究報告一式。
- Assets/Audioのmp3。ファイル名から再配布許諾を確定できない。初期テストは生成音を使用する。
- 旧Presence系、GroupWorkPresenceAudio、ExampleVoiceState、デモ移動系、Assets/Re.csの一括コピー。必要な役割のみ新設する。
- SampleScene全体・旧階層・机・アバター。ドローンシーンも初期対象外（当該コミットには存在しない）。

## ライセンス・秘密情報

追跡ファイル一覧にリポジトリ全体のLICENSEは見当たらない。公開されていることだけで再利用許諾は確定しないため、独自コードを複製する際は権利と許諾を確認する。現段階では調査と新設計の提案のみ。

Metaは[公式導入手順](https://developers.meta.com/vr/documentation/unity/meta-xr-audio-sdk-unity-req-setup/)と[公式サンプルのライセンス案内](https://github.com/oculus-samples/Unity-MetaXRAudioSDK/blob/main/README.md)を確認。採用版付属ライセンスを確認し、SDK本体を無条件に公開リポジトリへコピーしない。
Photonは[公式配布ページ](https://www.photonengine.com/sdks)から取得し、採用するFusion/Voiceのlicense.txtと第三者表示条件を確認する。OSS通知をSDK全体の許諾と解釈しない。SDK未取得のため採用版の再配布条件確認は未完了。

Scripts / Resources / Packages / ProjectSettingsの設定キー名検索ではPhoton App ID設定を確認できなかった。完全な秘密情報監査・履歴監査を行ったという意味ではない。設定や履歴を一括コピーしない。
新規プロジェクトはApp ID未設定で追跡し、ローカル設定・認証情報・鍵をgitignoreで除外する。ビルドに含まれるApp IDは秘密の保管手段にはならず、サーバー側認証・制限とは分けて扱う。

## 新設計と検証ゲート

Unityは2022.3.62f3を基準、Meta XR Audio 85.0.0を第一候補とする。新規XR / Fusion / Voiceの組合せは依存解決・実機確認後に固定する。未確認の組合せを互換と断言しない。

1. VR: 新規XRリグ・入力・移動・最小操作。EditorコンパイルとQuest 3の両眼表示・頭部/左右手追跡・移動操作を確認。
2. Fusion: 同室接続、参加退出、頭部/左右手の位置回転、操作権限。2クライアント・Questを含む接続試験で確認。既存協調機能の維持は移植元確認が必要。
3. Voice: 双方向発話、発話者/Speaker一致、再接続、Questマイク権限、二重再生・ハウリング防止を確認。
4. 音響: 左右定位・頭部回転・距離・壁ありなし・二重減衰・SDK競合を確認。

各Stepで不具合を修正し、検証完了前に次のStepへ進まない。Editor確認とQuest実機確認を別々に記録する。

構造案: SceneStateCollector → SceneTokenGenerator → SceneTokenSerializer / AdaptiveAudioController → SpatialAudioRendererAdapter。各段階をAudioEvaluationLoggerで記録する。
SceneToken（timestamp / sourceId / distance / relativeDirection / occlusion / taskPhase / priority / timeToDeadline）とAudioControlParameters（gain / lowPassHz等）は別型にする。
方向は聞き手ローカル単位ベクトル、距離はm、期限は秒を基本案とし、timestampの基準・同期方法、遮蔽の正規化、期限なし表現を実装前に定義する。
Baselineは距離と実測遮蔽、Always Clearは遮蔽損失の固定割合軽減、Adaptiveは重要度・期限に応じた軽減。全方式で距離モデルを共有し、将来のMLはポリシーを差し替える。

|テストシーン|検証|状態|
|---|---|---|
|BasicAudioTest|音源・聞き手・障害物|未作成・未検証|
|SceneTokenTest|生成・シリアライズ・表示|未作成・未検証|
|AdaptiveAudioTest|同一条件で3方式比較|未作成・未検証|
|NetworkAudioTest|2クライアントの同期・双方向音声・空間化|未作成・未検証|

## 現在の成果物

ユーザーが https://github.com/SasakiHodaka/vr-scene-token-audio を作成し、ローカルGitをorigin/mainに接続した。既存のREADMEとUnity用gitignoreを保持している。
研究成果物はこの初回調査報告のみ。新Unityプロジェクト、各モジュール、動作検証は未実施。
次の実装段階でREADMEを拡充し、architecture、experiment-planを追加する。Library / Temp / Logs / Obj / ビルド成果物 / UserSettings / ローカル認証設定を追跡対象外とする。
