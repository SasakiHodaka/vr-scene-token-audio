# 実装・検証結果

## 基盤分離の追加検証（2026-11-06）

確認用コピーにてUnity 6000.0.58f2、Windows Editor、batchmode / nographicsで
`Tools/Run-UnityTests.ps1`を実行。XMLのresult・件数・失敗数を確認した。

|検証|結果|
|---|---|
|EditMode|24/24成功|
|PlayMode|4/4成功|
|音響制御停止後の観測|位置変更がLatestTokenとCSVに反映され、音響設定は復元されたまま|
|Rendererなしの観測|音響制御の指定にかかわらずToken生成・CSV記録に成功|
|観測の停止・再開|停止中はサンプル数が増えず、再開後に生成・記録を継続|
|CSV互換性|列数・ヘッダーを維持。制御未適用行はObservationOnly、音響関連値は空欄|

テスト出力: `Logs/editmode-results.xml`、`Logs/playmode-results.xml`（Git対象外）。
既存の音響制御3方式の試験も成功。Quest実機・Photon接続・C1〜C3比較の検証は含まない。
以下は変更前の検証記録。

実施日: 2026-10-20。Unity 6000.0.58f2、Windows Editor、batchmode / nographics。

## 結果

|検証|結果|証拠|
|---|---|---|
|パッケージ復元・Unityコンパイル|成功|Logs/bootstrap.log、Logs/quest-setup.log、最終テスト実行でもコンパイル成功|
|4シーン生成|成功|Assets/Scenes/*.unityとmetaを管理。NetworkAudioTestは無効の足場としてBuild Settingsに登録|
|Quest向けOpenXR設定生成|成功|Android OpenXRLoader、Meta Quest / Touch / Touch Plus features、ARM64 / IL2CPP、Input System設定を生成|
|EditMode|**24件成功、失敗0、skip0**|Logs/editmode-results.xml|
|PlayMode|**3件成功、失敗0、skip0**|Logs/playmode-results.xml|
|秘密情報の簡易検査|該当なし|新規Assets / Packages / ProjectSettingsにApp ID設定・APIキー形式・秘密鍵ヘッダーを検出せず。完全な秘密検出の保証ではない|
|Quest 3の表示・入力・聴取|未実施|接続実機による試験が必要|
|Android APKビルド|未実施|AndroidターゲットのValidationと端末検証が必要|
|Photon二者接続・Voice・協調作業|未実施・新規プロジェクトでは未導入|Phase 5は設計のみ|
|Meta XR Audio / HRTF|未導入・未検証|標準Unity 3D AudioSourceでローカル検証|

Unityの初回通常実行ではサンドボックス側のライセンス認識が失敗した。上記のテスト結果はユーザー環境のライセンスで実行し、XMLのPassedと件数を確認したもの。プロセス起動コマンドの終了だけを成功判定にしていない。
Logsは生成物としてGit対象外。再現手順はTools/Run-UnityTests.ps1、またはUnity Test Runnerを使用する。

## テストで確認した内容

- Baselineの無遮蔽/半遮蔽/全遮蔽のgain・LPF、Always Clearの固定軽減、Adaptiveの通常/重要指示/期限/作業段階/緊急。
- 全方式で方向と基本距離モデルを変更しないこと、未取得遮蔽の保守的フォールバック。
- 障害物の移動、聞き手が壁内部にある場合、回転/スケールに依存しない聞き手座標、同位置・ほぼ同位置の方向。
- JSON往復、ローカルTransportの一回配送、無効JSON/非有限値の拒否、文化設定に依存しないCSVとID引用符。
- PlayModeでvolume/LPFを平滑化し、pitch/spatialBlendを変えず、無効化時に元volume・既存LPFのcutoff/enabledを復元すること。
- managerからRendererへ制御値を適用してCSVへ出力し、制御停止時に設定を復元すること。
- 実際のAdaptiveAudioTestシーンをロードし、壁ありBaseline→Always Clear→緊急Adaptive→壁移動→音源移動→source登録解除→停止を実行。遮蔽・方向・制御値・CSV・解除時復元を確認した。

## 完了条件の判定

1. 空間/作業情報のToken化: ローカルテストで確認。VR頭部入力用接続と最小ローカルリグは実装、実機では未確認。
2. TokenのCSV確認: 自動試験で確認。
3. 3方式切替: ポリシー試験とシーン結合試験で確認。
4. 障害物/重要度反映: 自動試験で確認。
5. 既存VR/Photon維持: 元プロジェクトを変更していない。新規プロジェクトへの移植・維持の実証は未達。
6. テスト/制約文書化: 本書とarchitecture、migration、implementation-planに記録。

Phase 1〜4のローカルモジュール実装とEditor自動検証は実施済み。Quest聴取、既存VR/Photonとの回帰、音響SDKの統合を含む全完了条件が達成済みとは扱わない。

## 制約

遮蔽は直接経路の二値コライダー判定。回折・透過・材質・周波数別伝搬・実測遮音量は扱わない。20Hzの実時間間隔はフレーム/負荷に依存し、正確なハードリアルタイム制御ではない。
Rendererの平滑化はフレーム更新であり、DSPサンプル精度の無クリックを実測保証していない。nographicsでの試験は聴取や空間定位の検証にはならない。
音源volume/LPFを同時に別スクリプトが更新する構成は非対応。元の有効LPFより高いカットオフにはしないため、既存LPFがある環境ではAlways Clear/Adaptiveの実効効果が制約される。
LocalXRTestRigは最小の移動/追跡/ボタン入力であり、Fusion協調操作リグや物体把持の代替ではない。画面用UIはVR用UIとして設計していない。
処理時間はポリシー/収集/適用予約のCPU時間で、DSP・ログI/O・ネットワーク遅延を含まない。STI/SNR/定位/理解度・作業効率の改善は未評価。
