# 調査結果とPhase 1〜4の実装計画

調査日: 2026-10-20。既存作業プロジェクトには変更せず、調査専用コピーで各ブランチの追跡ファイルを確認した。AGENTS.mdは各調査ブランチの追跡一覧に見当たらない。

|ブランチ|固定コミット|役割|
|---|---|---|
|main|a85f53d9a4e9388d6f23268e180cf4072b55c29d|Unity 2022.3.62f3、Meta XR Audio 85.0.0、周辺存在音デモ|
|fusion-vr-training-photon-backup|cf0a94dc2b2bf55a7704d643495db601cd06b62f|Fusion VR Trainingのバックアップ。Photon統合版との差分も調査|
|scene-token-prototype|9bede2424466f4af24dc2eb8c71c012d066e31f2|既存Scene Token実験プロトタイプ|
|photon-scene-token-integration|8d8a18a1cfdabaf7cf45218472ad6c9db4d4fb58|今回の主な参考元。VR TrainingとローカルScene Tokenを統合|
|master|2fa5baf9f840e5a9455ba328b8238f394ba69d42|存在を確認。今回の主な移植元には選定しない|

初回調査の「PhotonやVRがない」という結論はmain限定である。最新調査では以下の構成を確認した。

## Photon統合ブランチ

|機能|構成・依存・根拠|
|---|---|
|Unity|ProjectSettings/ProjectVersion.txt: 6000.0.58f2|
|XR|Packages/manifest.json: OpenXR 1.15.1、XR Hands 1.5.1。URP 17.0.4も使用。Assets/XRにローダー・設定あり|
|Fusion|Assets/Photon/Fusion/Assemblies/Fusion.Runtime.dll: FileVersion 2.0.6.1034、ProductVersion 2.0.6.1034+94e2793d|
|Voice|Assets/Photon/PhotonVoice/changes-voice.txt: 2.59、Realtimeライブラリ4.1.8.15|
|VRプレイヤー|Assets/Photon/FusionAddons/XRShared（ReadMe:2.0.14）のHardwareRig、HardwareHeadset、HardwareHand、NetworkRig、NetworkHeadset、NetworkHand。HardwareRig.prefab / NetworkRig.prefabと訓練用Variantあり|
|同期|NetworkRig.csはShared topology用。StateAuthorityをローカルユーザーとして扱い、FixedUpdateNetworkで頭・左右手・play areaの姿勢とHandCommandを更新。Renderでローカル追跡を反映。NetworkTransformが同期を担う|
|Voice接続|Assets/Photon/FusionAddons/VoiceHelpers（ReadMe:2.0.5）のFusionVoiceSetup.csがRunnerにFusionVoiceClientを用意し、PrimaryRecorderとマイク権限処理を設定。生成RecorderはPhoton Mic、48kHz、60ms。NetworkRigWithVoice.prefab、VoiceのFusion連携コードあり|
|シーン|Assets/Training/Scene/Drone Training.unityがBuild Settingsで有効。学習管理、磁石による組立、スコア、タイマー等に依存。初期の最小音響検証には不要|
|Meta音響|統合ブランチのmanifestにMeta XR Audioがなく、AudioManagerのSpatializerも空。mainのMeta設定が統合版で動いているとは判断できない|
|既存Token|Assets/Scripts/SceneToken。speakerId、方位・距離カテゴリ、発話/ターン/意味ラベルなどを使用。PHOTON_INTEGRATION.md上もまだローカル処理|
|既存Renderer|SceneTokenDecoderRenderer.csが音源Transform位置を再配置し、volume・pitchを変更。今回要求の方向・距離維持と競合するため同一音源に併用しない|

プレイヤーやPhoton機能はコード・Prefabの静的確認であり、2クライアント・Questでの実動作検証は未実施。

## 選別と移植方法

|移植元ファイル|依存|扱いと移植方法|
|---|---|---|
|Assets/Scripts/SceneToken/SceneToken.cs、SceneTokenManager.cs|SpeakerObject、Unity Transform、既存実験条件|構造化観測とローカル処理の考え方を参考に新実装。今回不要な意味ラベル・発話文・pitch制御は移さない|
|同/SpeakerObject.cs|AudioSource、生成クリップ、模擬発話|生成音による依存の少ない試験を参考にGeneratedTestToneを新実装。既存clip再生制御をVoice受信源には移さない|
|同/SceneTokenDecoderRenderer.cs|旧Token、SpeakerObject|位置・pitch変更を除外し、新しいSpatialAudioRendererAdapterでvolume倍率とLPFだけを制御|
|同/SceneTokenLogger.cs、SceneTokenEventLogger.cs、SceneTokenMetrics.cs|旧実験セッションと評価項目|CSV記録の役割を新実装。Culture非依存の数値、入力位置・Token・出力・時間を記録。明瞭性指標の代替にはしない|
|mainのPeripheralStateDetector.cs / EnvironmentAcousticProfile.cs|旧Target、旧CueModel|距離・ローカル方向の収集を参考。視野外を遮蔽とする近似は不採用。物理コライダーによる遮蔽を新設|
|XRShared/Scripts/Rig/*.cs、VoiceHelpers/Scripts/FusionVoiceSetup.cs|Fusion、XRShared、Voice、権限処理、Prefab群|Phase 5の接続境界を調査。Phase 1〜4ではコピー・変更せず、新モジュールに依存を持ち込まない|
|Assets/Training、IndustriesComponents、Photonサンプルのモデル・音声・フォント|学習/組立システム、外部素材とライセンス|最小音響検証に不要。サンプルAssetの公開再配布条件も未確定のため移さない|
|mainのMetaXR設定・SDK|Meta Audio 85、spatializer|標準AudioSourceとのアダプター境界を用意。SDKは公式取得・ライセンス確認後に別途導入。未導入を導入済みと扱わない|

今回は既存ソースの直接コピーをせず、必要な役割を新しいSceneTokenAudio名前空間に実装した。プロジェクト版はVR/Fusion側と同じ6000.0.58f2にした。パッケージは必要なOpenXRとテスト関連に絞り、URP・Hands・ProBuilder・ParrelSync・学習モデル等は持ち込まない。

## ライセンス・設定

統合版のAssets/License.txtはExit GamesのSample利用条件を記載し、非ソースAssetの用途を内部開発に限定している。サンプルの公開一覧に載っているだけで素材の公開再配布を許諾済みとは扱わない。新規プロジェクトにはサンプルAssetやSDK本体をコピーしていない。
[Photon公式VR Training](https://doc.photonengine.com/fusion/v2/industries-samples/fusion-vr-training)と採用するSDKの配布ライセンスを接続前に確認する。Meta SDKの扱いは初回調査の参照先も参照。

PhotonAppSettings.assetにはFusion/Voice/ChatのApp IDが設定されていることを値を表示せず確認した。値・設定Asset・履歴は新規リポジトリへコピーしていない。新規設定はローカルで再作成し、.gitignoreの対象とする。App IDのクライアント設定と、秘密鍵・サーバー認証は区別する。

## 実装計画（実装前に共有した方針）

1. Phase 1: ブランチ差、Unity版、SDK、VR/Voice、ライセンス、設定の所在を確認する。
2. Phase 2: 20Hzで位置・聞き手座標方向・直接経路遮蔽・作業情報を収集。Unity非依存DTOとJSON、ローカルTransportに分離する。
3. Phase 3: 同じTokenを処理する3ポリシーと交換可能なController。Rendererは距離ロールオフを維持し、遮蔽倍率/LPFを平滑化。停止時に元設定を復元する。
4. Phase 4: 生成音・移動壁・重要度/方式切替・CSV・EditMode/PlayModeテスト。Editorでの自動検証とQuestでの聴取確認を分けて記録する。
5. Phase 5: docs/photon-integration.mdの案を確認後、既存VR/Voiceの動作を基準測定してから接続する。今回は設計のみ。

実装・試験結果と残項目はdocs/validation.mdを参照。旧VR/Photonは元リポジトリで無変更だが、新規プロジェクトへのVR/Photon移植完了・維持確認とは区別する。
