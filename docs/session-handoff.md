# 作業再開メモ

まず[次回はここから](../START_HERE.md)の手順を実施する。現在の優先順位は「実シーンでの取得確認 → 実ログの独立した注釈 → 構成比較」。

## 保存時点

- 研究テーマはScene Tokenの構成。新規性・最終Token仕様は未確定。
- 最小の意味関係送受信・比較基盤、予備結果を保存済み。現予備結果は固有の優位性を支持していない。
- ドローン組立の取得処理、CSV出力、共通ID・ローカル単調時刻による8項目の音響Tokenとの関連付けを追加済み。
- 研究側テスト：EditMode 33/33、PlayMode 6/6。移植先の記録処理テスト：2/2。日本語表示への文言変更後はテスト再実行なし。
- 記録ウィンドウを日本語化し、移植先の同ファイルにも反映済み。
- ユーザーからUnityの表示・動作成功の報告あり。実操作のCSV内容・動的な取付／工程イベントの取得はまだ監査していない。

## 使用するプロジェクト・シーン

```text
C:\Users\acd-pc67\Downloads\fusion-vr-training-2.0.6
Assets/Training/Scene/Drone Training Capture.unity
Unity 6000.0.58f2
```

元のDrone Training.unityは変更せず、取得用シーンを別に保存。記録モジュールはAssets/SceneTokenResearchIntegration。
詳しい手順は[ドローン組立への接続・CSV取得](drone-capture.md)。

## 次回の最初の作業

1. 検証用シーンで通常の接続・作業開始を行う。
2. 部品を把持→解放→取付し、日本語ウィンドウのイベント数と記録対象数を確認する。
3. 表示されたログ保存先のevents-audio.csvとobservations.ndjsonを照合する。
4. actor_id・object_id・target_id、音響参照・時刻、取付・解除・工程通知の欠落を確認する。
5. 確認できた実ログを保存し、独立した注釈を付けてから構成比較へ進む。

取付候補がないことを操作失敗と推定しない。取付通知にない操作者を直前の把持者で確定しない。
自動テストの通知を実際の人間のVR操作データと扱わない。
このメモの保存はディスク上のコード・資料・シーンの確認であり、Unityで編集中の未保存変更を操作して保存したことは意味しない。

## 作業フォルダーとGitの整理

| 場所 | 用途・状態 |
|---|---|
| `C:\Users\acd-pc67\scene-token-papers` | 現在の研究コード・資料の正本。ブランチはmain。GitHubのmainと同期する |
| `C:\Users\acd-pc67\Downloads\fusion-vr-training-2.0.6` | 実操作用のFusionドローンプロジェクト。SDK・素材を含むため、その全体を研究GitHubへ転載していない |
| `C:\Users\acd-pc67\scene-token-review` | 初期調査・以前の音響制御分離の保管用。現在の実操作には使わない |

保管用フォルダーに残っていた7ファイルの変更をコミット`3573e60`に保存。
`archive/audio-observation-separation-20261106`ブランチで保管し、mainへ自動統合しない。
これは以前の別案で、現行のドローン記録処理とは別に扱う。ファイルを削除せず履歴を保持した。
`docs/related-papers`はmainへ早送り済みの旧作業ブランチ。以後の通常作業はmainで行う。

## ローカルバックアップ

```text
C:\Users\acd-pc67\SceneTokenBackups\20261106-122756-72dc35f3
```

取得用シーンとmeta、接続モジュール、Build Settingsのシーン一覧を保存。88ファイルのSHA256を記録し、コピー一致を確認した。
元SDK・認証設定・Unityの未保存編集はこのバックアップには含めない。
バックアップはローカル、研究コード・文書はGitHubで保存する。元プロジェクトや既存ログは削除していない。

## 再開前の整合確認

```powershell
.\Tools\Check-Resume.ps1
```

main、未コミット変更、取得用シーンの存在、インストール済みコードとの一致、元シーンのハッシュを確認する。
ネットワーク接続・Unity起動・ファイル変更は行わない。GitHubの同期確認は別途git ls-remote等で行う。
既定保存先の取得用CSVは整理時点で0件だったため、実データ取得済みとは扱わない。
必要なら`Tools/Backup-DroneCapture.ps1`で、新しい日時フォルダーへ追加バックアップを作れる。
