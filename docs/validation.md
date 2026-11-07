# 実装・検証結果

## 研究室外での取得準備・修正

ユーザー指定の作業基準日は11月9日。実行環境の時計・XMLは2026-11-07を示すため、予定日と実行タイムスタンプを混同しない。

- 研究側：EditMode 33/33、PlayMode 9/9成功。最終版の`Logs/editmode-results.xml`と`Logs/playmode-results.xml`でPassed・failed=0を確認。
- 移植先：記録処理のPlayMode 5/5成功。`fusion-vr-training-2.0.6/Logs/assembly-capture/playmode-results.xml`でPassed・failed=0を確認。
- Python：`python -m unittest discover -s Tools/tests -v`で7/7成功。CSV契約と実際のC#ヘッダーの一致、複数音源とイベント数の区別、欠測、重複、破損、非有限数、参照不一致、入力不変・注釈上書き防止を検証。
- 後から生成された取付点の追加購読、購読途中の失敗からの巻き戻し・再試行、既存把持購読の維持、初期把持由来の解放根拠を人工コールバックで確認。
- インストーラーは元シーンのハッシュ照合をコピー前に実施するよう変更。移植を実行し、Runtime/Editorの一致と元シーンのハッシュ一致を再確認。
- 元シーンSHA256：`5DAB83647B540D224FB878CCA2D811677F5B7FA7E5261A34EAFCDAB2A55C0B93`。

初回Unityは制限環境でライセンス取得に失敗し、通常環境で再実行した。実SDKを含む移植先でのコンパイル・人工通知テストは成功したが、実際のSDKイベント発生、実セッション内の動的生成、QuestのAOT・ストリッピング、実操作精度は未確認。Unityテストの通知を人間の操作ログに使用していない。既定保存先のCSVは再確認時も0件。

検査・注釈の手順は[capture-audit.md](capture-audit.md)。新しい構成比較の測定や優位性の実証は今回実施していない。

## ドローン組立の記録処理

[接続・CSV取得手順](drone-capture.md)と[検証範囲の記録](../Evaluation/assembly-capture/verification.json)を参照。
研究側でEditMode 33/33件、PlayMode 6/6件、移植先で記録処理のPlayMode 2/2件が成功。
元シーンを変更せず検証シーンを生成し、配置済み2部品・計4コールバックの購読設定を検証した。
初期シーンには取付点・活動トラッカーが検出されず、実セッション内の動的生成後の接続・人間の組立ログ取得は未確認。
CSV記録と8項目TokenのID・時刻対応をテスト用イベント源で確認したもので、実作業の妥当性や新規性の検証ではない。

## 意味関係プローブの追加検証

ユーザーの実装着手指示に基づく探索的実装。[手順・条件・予備結果](relation-probe.md)を参照。
Unity 6000.0.58f2、Windows Editor、batchmode / nographicsでEditMode **33/33件**、PlayMode **4/4件**が成功。
既存音響処理の回帰、幾何候補生成、候補部分欠落の非公開、旧版・過去訂正の区別、セッション分離、localhost UDPによる別受信コンポーネントでの復元を確認した。
予備評価は人工的なS4/S5入力を48設定で各20回反復したもの。測定した予算では削減方針の品質差は出ず、新規性・仮説の支持とは扱わない。
以下は変更前の音響基盤についての検証記録。

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
