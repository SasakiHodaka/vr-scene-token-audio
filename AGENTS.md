# このプロジェクトを再開するとき

- 最初に`START_HERE.md`、`docs/session-handoff.md`の最新節、`docs/next-session.md`を読み、Gitの状態と照合する。記録日や過去のテスト件数を現在の状態とみなさない。
- 研究の中心はScene Tokenの構成。正本はこの研究リポジトリ。実操作用Unityプロジェクトは`C:\Users\acd-pc67\Downloads\fusion-vr-training-2.0.6`、シーンは`Assets/Training/Scene/Drone Training Capture.unity`。
- 直近の優先順位は実操作ログ取得→独立した注釈・取得品質確認→公平な構成比較。実機が使えない場合は`docs/next-session.md`の研究室外の作業を進める。
- 旧8場面は開発デモ。人工通知テスト、実セッション接続、実操作、Quest動作、研究上の有効性を分けて報告する。正解や未取得値を実装出力から補完しない。
- 共通ID・時刻を持つ構造化ログを対照に残し、比較方式に同じ観測・事前知識を与える。固有の優位性・最終仕様は未確定。
- Unityは6000.0.58f2。C#変更時は`Tools/Run-UnityTests.ps1`で結果XMLまで確認する。移植する変更は`Tools/Run-DroneCapture.ps1 -Operation Test`も実行。Python検査変更時は`python -m unittest discover -s Tools/tests -v`。文書だけの変更でUnityテストを繰り返さない。
- 元SDK・元シーン・Prefab・認証設定を変更・公開しない。移植は既存の専用モジュールとインストーラーを使い、必要なら先に`Tools/Backup-DroneCapture.ps1`で保存する。Unityアセット追加時はmetaを含める。
- 作業後は最新の判断、検証結果、残作業を`docs/session-handoff.md`へ反映する。公開対象に取得ログ・認証情報・Library・Logsを含めない。
