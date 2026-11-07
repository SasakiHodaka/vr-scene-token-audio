# 次回はここから

**次の目標：部品を一つ把持・解放・取付し、イベントと音響TokenがCSVに残るか確認する。**

研究室外での準備：[ログ検査・独立注釈](docs/capture-audit.md)、[12月4日の概要提出までの予定](docs/submission-milestones.md)、[概要の下書き](docs/abstract-draft.md)。実操作ログはまだ未取得。ログ取得前の自動テストを有効性の実証と扱わない。

## 開くもの

| 用途 | 場所 |
|---|---|
| 実操作用Unityプロジェクト | `C:\Users\acd-pc67\Downloads\fusion-vr-training-2.0.6` |
| 開くシーン | `Assets/Training/Scene/Drone Training Capture.unity` |
| Unityの版 | `6000.0.58f2` |
| 正本の研究コード・資料 | `C:\Users\acd-pc67\scene-token-papers` の `main` |
| GitHub | [vr-scene-token-audio](https://github.com/SasakiHodaka/vr-scene-token-audio) |

## 再開する順序

1. Unityで上記プロジェクトと取得用シーンを開く。
2. Playし、サンプルの通常の接続・作業開始を行う。記録ウィンドウは日本語。
3. 作業オブジェクト生成後、記録対象と聞き手の参照を確認してから、部品一つを把持→解放→取付する。
4. 「記録したイベント数」が増えたら、Playを停止し、「ログ保存先」の`events-audio.csv`と`observations.ndjson`を開く。
5. [ログ検査](docs/capture-audit.md)を実行し、把持・解放・取付、物体ID・取付先・時刻・音響参照を照合する。

取付先や工程の通知が取れていなければ、その接続を調べる。ログ取得が確認できる前に比較方式や機能を増やさない。

## どこまで確認したか

- コードはGitHubへ保存。元シーンはハッシュ一致で変更なし。取得用シーンは別に保存。
- 研究側テスト：EditMode 33/33、PlayMode 9/9。実プロジェクト側の記録テスト：5/5。Pythonのログ検査テスト：7/7。
- Unityの表示・動作成功はユーザーから報告あり。
- **人間の実操作ログと、実セッションで生成される取付点・工程の接続は未確認。**
- 整理時点で、既定の保存先から取得用CSVは見つかっていない。別の保存先が表示された場合はそちらを確認する。
- 新規性・最終Token仕様は未確定。人工入力の予備評価では提案候補固有の優位性は出ていない。

## 困ったとき・詳細

- [実操作とCSVの詳しい手順](docs/drone-capture.md)
- [現在の状態・未検証事項・保管場所](docs/session-handoff.md)
- [資料の読み順と履歴の区別](docs/README.md)

コードと実プロジェクトの取り違えは、研究フォルダーで次を実行して確認できる。Unityの起動やファイルの変更は行わない。

```powershell
.\Tools\Check-Resume.ps1
```

この確認は実際のVR動作のテストではない。Unityで開いたままの未保存編集は、必要ならUnity側で保存する。
