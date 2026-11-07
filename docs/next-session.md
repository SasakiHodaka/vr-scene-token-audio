# 次回の作業カード

## 最初の確認

正本は`C:\Users\acd-pc67\scene-token-papers`。別フォルダーの古いデモから再開しない。

```powershell
Set-Location 'C:\Users\acd-pc67\scene-token-papers'
git status --short
git log -1 --oneline
powershell -NoProfile -ExecutionPolicy Bypass -File Tools\Check-Resume.ps1
```

Check-Resumeはネットワーク接続やUnity起動をせず、ファイルの一致を確認する。未コミット変更があれば内容を確認して引き継ぎ、消したり自動上書きしたりしない。GitHubとの同期は別途fetchして確認する。実機利用の可否に応じて以下へ進む。

## 研究室で実機を使える場合

今回の目標は、部品一つの把持・解放・取付を取得し、記録内容を確認すること。

1. `fusion-vr-training-2.0.6`をUnity 6000.0.58f2で開き、`Drone Training Capture.unity`を開く。
2. Playして通常の接続・作業開始を行う。作業オブジェクト生成後、聞き手の参照と記録対象数を確認する。
3. 一つの部品を把持→解放→取付。可能なら解除→再把持も行う。部品名、操作の順番、画面のイベント数、保存先を控える。
4. Playを停止し、保存先の4ファイルを保持する。`capture-info.json`、`observations.ndjson`、`events-audio.csv`、`sender-frames.ndjson`。
5. 研究フォルダーで次を実行する。出力先は未使用の名前にする。

```powershell
python Tools/audit_assembly_capture.py '画面に表示された取得フォルダー' --output 'Logs/capture-audit/first-operation'
```

成功の目安は、検査のerrorsが空であり、元観測にgrab_confirmed・release_observed・attachment_registeredが実際の操作と対応して存在すること。既存の取付はattachment_snapshotとして区別する。音響文脈の有無と取得元も確認する。取付通知のactor_id=unknownを、直前の把持者で埋めない。
検査成功だけでは取得精度の確認は完了しない。別根拠から[独立操作表](../Evaluation/templates/independent-operations.csv)を作り、記録にない操作も照合する。

| 状況 | 次に確認すること |
|---|---|
| 保存先が表示されずイベント数0 | シーン、記録の有効状態、接続・作業開始、対象部品の生成、記録エラーとConsole |
| 把持・解放だけで取付がない | 取付が実際に成立したか、生成後のAttachmentPointと購読、bridgeのLastError。取付候補だけで失敗イベントを作らない |
| 工程数0 | 実セッションにLearnerActivityTrackerが生成されているか。未生成なら工程取得成功と扱わない |
| 音響文脈がない | 聞き手、把持者の頭部・音源参照。頭部姿勢は実音声と区別する |
| 検査に不整合がある | 元ファイルを保持してaudit.jsonの該当イベントを確認。期待値を出力に合わせて変更しない |

## 研究室外で進める場合

実機なしの取得準備・記録コード修正・自動テストは完了済み。次は次の内容を進める。

1. [概要の下書き](abstract-draft.md)の背景・目的・方法と、実装から示せる範囲を整える。結果欄は実測後に更新する。
2. 提出書式、文字数、必要な図表、指導者レビュー日、発表時間を確認する。既存資料にない条件は未確認として残す。
3. 実ログ取得後の比較に必要な問い合わせと集計表を整理する。例えば「どの部品がどこへ取り付いたか」「操作者は既知か不明か」。構造化ログと関係表現に同じ観測を与える。
4. [既存の文献調査](related-papers.md)から背景と比較対象を整理する。追加の文献確認が必要なら原典を確認し、引用と推測を分ける。
5. 実操作ログが手元にある場合はログ検査と注釈を進める。現在の既定保存先は0件だったため、取得済みと仮定しない。

比較の最終条件と採点基準は、実際に取得可能な情報を確認してから固定する。現段階で新しい優位性や結果を約束しない。

## 作業を終えるときに残す記録

次の項目をsession-handoff.mdの最新節へ追加する。秘密情報・発話本文・実ログを無断でGitHubへ追加しない。

```text
実施内容：
使用したプロジェクト・シーン：
ログ保存先／検査出力先：
確認できた通知・欠測：
実行した検査と結果：
今回判断したこと／未確認事項：
次の一作業と完了条件：
Gitの保存・公開状態：
```

## 次の会話に貼る文

```text
https://github.com/SasakiHodaka/vr-scene-token-audio の続きを進めてください。
正本は C:\Users\acd-pc67\scene-token-papers です。
AGENTS.md、START_HERE.md、docs/session-handoff.md の最新節、
docs/next-session.md を読み、Git状態と照合して再開してください。
今回は研究室に【いる／いない】、実操作ログは【ある／ない】です。
前回は取得準備と自動検証まで完了し、人間の実操作取得は未確認でした。
概要提出は12月4日、発表は12月12日です。
今回の最新状況を確認して、作業カードの次の一作業から進めてください。
```
