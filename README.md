# vr-scene-token-audio

VR遠隔協調作業の空間・作業・時間情報をScene Tokenに構造化し、音響制御へ利用する研究用Unityプロジェクトです。

## 開発環境

- Unity **6000.0.58f2**（Photon統合元ブランチと同じ版）
- C#、OpenXR 1.15.1、Unity Test Framework 1.5.1
- 対象デバイス: Meta Quest 3。現在の音響RendererはUnity標準3D AudioSource
- Photon Fusion / VoiceとMeta XR Audio SDKは新規プロジェクトには未導入。既存ブランチを調査し、接続境界を設計しています

## セットアップ

1. このリポジトリをcloneし、Unity Hubから6000.0.58f2で開きます。Packagesを復元します。
2. Assets/Scenes/AdaptiveAudioTest.unityを開いてPlayします。シーンがない場合は`Tools > Scene Token Audio > Create Test Scenes`で生成します（現在の編集シーンの保存確認があります）。
3. 画面左上のUIで方式、重要度、作業段階、期限、壁位置、音源距離を変更します。音源とListenerの位置・回転はInspectorからも変更できます。
4. CSVは`Application.persistentDataPath/SceneTokenLogs`に保存します。DebugUIで実際のパスを確認できます。
5. `Window > General > Test Runner`からEditMode / PlayModeを実行します。

Quest用はAndroid Build Supportを追加し、`Tools > Scene Token Audio > Configure Quest OpenXR`でAndroidローダー・Touchプロファイル・ARM64/IL2CPP設定を生成します。Androidへ切り替え後、Project SettingsのXR Plug-in Management/OpenXRのValidationを確認してビルドしてください。実機での動作は未検証です。
最小XRテストリグは頭/左右手の追跡、左スティック移動、右primaryボタンで方式切替、左primaryで通常/重要切替、右secondaryで壁切替に対応します。画面用DebugUIはVR内の操作UIではありません。

## 実装済みの範囲

- 8項目のScene Token、JSON、ローカルTransport
- 距離・聞き手ローカル方向・障害物の直接経路遮蔽・作業/期限の取得
- Baseline / Always Clear / Adaptiveの交換可能なポリシー
- 既定20Hz、volume/LPFの平滑化、停止・無効化時の設定復元
- 入力・Token・目標/適用値・処理時間のCSV記録
- 最小テストシーン生成、EditMode / PlayModeテスト

NetworkAudioTestはローカルの足場のみです。双方向Photon音声、既存協調作業の移植、Meta SDK/HRTF、Quest実機確認は未完了です。制御値の変化は明瞭性改善の証明ではありません。

## 文書

- [意味関係の最小実装・実行手順・予備評価](docs/relation-probe.md)
- [研究成立条件の検討書（現在の優先事項・仕様確定保留）](docs/research-feasibility.md)
- [ドローン組立3場面の要求条件・正解仕様とシナリオJSON](docs/scenario-spec.md)
- [研究方針・要求条件・C1〜C3・評価・マイルストーン](docs/research-plan.md)
- [関連論文（構成・要求条件・関係表現・音声Token・評価）](docs/related-papers.md)
- [調査・移植・実装計画](docs/implementation-plan.md)
- [データ仕様と責務](docs/architecture.md)
- [試験結果と制約](docs/validation.md)
- [比較実験計画](docs/experiment-plan.md)
- [Phase 5 Photon接続設計案](docs/photon-integration.md)

既存プロジェクトは変更しません。必要な役割を新設し、既存のSDK・素材・App ID・学習モデル・ビルド生成物は一括コピーしません。SDKを別途導入する場合は配布元ライセンスを確認してください。
