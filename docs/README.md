# 資料の読み順

現在の入口は[次回はここから](../START_HERE.md)。まず実操作ログの取得・照合を進める。

| 資料 | 位置付け |
|---|---|
| [session-handoff.md](session-handoff.md) | 現在の作業状態・未検証事項・作業フォルダー・バックアップ |
| [next-session.md](next-session.md) | 次回の作業カード、実機の有無による分岐、成功条件、再開文 |
| [drone-capture.md](drone-capture.md) | 実ドローンプロジェクトでCSVを取る手順とイベントの意味 |
| [capture-audit.md](capture-audit.md) | 取得後のファイル整合性検査・独立注釈。実操作の証明と区別する |
| [submission-milestones.md](submission-milestones.md) | 12月4日の概要提出・12月12日の発表までの到達目標 |
| [abstract-draft.md](abstract-draft.md) | 実機評価前の概要下書き。測定後に結果・考察を更新する |
| [relation-probe.md](relation-probe.md) | 最小送受信実装と人工入力による予備評価。優位性の証明ではない |
| [validation.md](validation.md) | 検証記録。追加検証を上に掲載。古い件数・未実装記述は当時の状態 |
| [related-papers.md](related-papers.md) | 文献一覧。列挙だけで新規性を認めた資料ではない |
| [research-plan.md](research-plan.md) | 従来の研究計画。C1〜C3・期限・仕様案は採用確定ではない |
| [research-feasibility.md](research-feasibility.md) | 実装着手前の仮説・公平な比較条件の検討記録 |
| [scenario-spec.md](scenario-spec.md) | 未承認の人工正常系サンプル。C2だけに工程推定を禁止する条件は撤回済み |
| [experiment-plan.md](experiment-plan.md) | 過去の音響制御比較計画。現在の意味関係の比較とは区別する |
| [architecture.md](architecture.md) | 既存8項目の音響Tokenの契約。全意味関係を含む最終Token仕様ではない |
| [implementation-plan.md](implementation-plan.md)・[migration.md](migration.md) | 移植元を調べた際の履歴。現在の実装済み範囲は再開メモを優先 |
| [photon-integration.md](photon-integration.md) | Photon音声・共有状態との接続案。完了済みとしない |

## データの区別

- `Evaluation/relation-probe/recorded`：人工入力の測定結果。960行は同一入力の反復であり960作業例ではない。
- `Evaluation/relation-probe/input.json / expected.json`：人工入力と独立に記述した期待値。実作業ではない。
- `Evaluation/assembly-capture/verification.json`：実プロジェクトへの配置・購読・テストの確認範囲。実操作の取得結果ではない。
- `Evaluation/scenarios / ground-truth`：以前の人工組立サンプル。最終評価仕様ではない。
- 実操作のCSV・NDJSON：UnityのpersistentDataPath配下。取得後に観測条件・独立した注釈と一緒に管理する。
- `Logs`：再生成可能なテスト出力。Git対象外。これらを正解データへ自動変換しない。

既存資料を削除・移動せず、研究上の判断を後から追える形で保持している。
