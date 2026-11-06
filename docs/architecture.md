# アーキテクチャ

SceneStateCollector → SceneTokenGenerator → LocalSceneTokenTransport → AdaptiveAudioController → SpatialAudioRendererAdapter。SceneTokenAudioManagerが同じ周期で収集・生成・制御・ログを実行する。既定20Hz、InspectorのupdateHzで1〜120Hz。遅れたtickは追いつき連打せずスキップする。Rendererの平滑化はunscaledDeltaTimeに基づくフレーム更新。

## Tokenの契約

通信DTOはUnityEngine型に依存しないSerializable struct。JSONはSceneTokenSerializerを使う。schema_versionはCSV側で1、将来のネットワークではEnvelopeに付ける。Tokenに制御結果や発話内容を含めない。

|フィールド|型・単位・範囲|未取得・境界の扱い|
|---|---|---|
|timestamp|double、アプリ起動後の単調時刻、秒、0以上|取得必須。UTCや他クライアント時刻とは直接比較しない|
|sourceId|string、1〜128文字、空白のみ不可|取得必須、manager内で一意。欠落・重複時は適用しない|
|distance|float、m、0以上の有限値|音源/聞き手なしならサンプルなし|
|relativeDirection|x/y/z各float、聞き手ローカル単位ベクトル|右+x、上+y、前+z。同位置のとき(0,0,0)。回転を使いスケールに依存しない|
|occlusion|float、0〜1、初期判定は0/1|未取得=-1。制御は保守的に1として扱う。幾何学的直接経路のコライダー判定であり遮音量の実測値ではない|
|taskPhase|enum: Unknown/Preparation/Assembly/Verification/Emergency|未取得=Unknown|
|priority|float、0〜1|既定0。通常音声0.2、指示0.7、緊急1を初期設定の例とする|
|timeToDeadline|float、秒、0以上|期限なし/未取得=-1。超過は0。再開始はTaskStateController.RestartDeadline|

NaN/Infinity・不正範囲・未定義enumはSerializer/Transportで拒否する。JSON必須フィールドの存在も検査するがネットワーク用の認証済みスキーマパーサーではない。ネットワーク入力の長さ・送信権限・順序・頻度の検証はPhase 5で追加する。

SceneStateはraw位置などUnity側観測を保持し、Tokenは要求された8項目のみ。AudioControlParametersはgainCorrection、occlusionGain、lowPassHzの別型。

## 制御方式

全方式は同一SceneTokenから計算可能。gainCorrectionは初期値1で、音源の実位置・向き・pitch・AudioSourceの距離設定は変更しない。通常の距離減衰はAudioSourceの3Dロールオフが担当する。ログのocclusionGainは距離減衰後の実測音圧ではない。

遮蔽o、軽減率rに対し残存遮蔽q=o*(1-r)、occlusionGain=1-0.8*q、LPF=22000-20000*q Hz。

- Baseline: r=0。
- Always Clear: r=0.75、作業状態によらず固定。
- Adaptive: 通常重要度(<0.6)はBaselineを維持。0.6以上の指示はAssemblyで+0.05、Verificationで+0.15、期限10秒以内で+0.15し、0〜1に制限。Emergencyは1。実効重要度0.6〜0.95をr=0〜1に線形変換し、0.95以上はr=1。

これらは検証可能な初期ルールであり人にとっての最適値ではない。Controller.ReplacePolicyで将来のポリシーを交換できる。

## Rendererの責務と復元

AudioSourceのvolumeと同一GameObjectのAudioLowPassFilterのみを所有する。指数平滑化の既定時定数は0.1秒。複数Rendererが同じAudioSourceを所有することを拒否する。
適用開始時に元volumeとLPFのcutoff/enabledを記録し、停止・manager無効化・adapter無効化時に復元する。既存有効LPFがある場合は元cutoffより高くしない。LPFがなければ追加し、停止時には無効へ戻し、adapter破棄時に自作フィルタを破棄する。
clip、再生/停止、pitch、Transform、spatialize、距離ロールオフ、spatialBlendは変更しない。Voiceの音源を使う場合も再生ライフサイクルはVoiceが所有する。
元のvolume/LPFを他スクリプトが同時更新する構成は非対応。既存SceneTokenDecoderRendererや旧距離/遮蔽制御との二重所有を取り除く。SDKの遮蔽と今回の遮蔽の所有者も一つにする。

## ログと性能

CSVはApplication.persistentDataPath/SceneTokenLogsに一意名でUTF-8出力。timestampは単調時刻、ファイル名はUTC。数値はInvariantCulture、IDはCSV引用符をエスケープする。
raw音源/聞き手位置、8項目のToken、方式、目標gain/LPF、tick時点の適用volume/LPF、collection_ms、control_ms、total_ms、frame_msを記録する。total_msは収集から適用予約まででCSV I/O・次フレームの平滑化・DSP処理時間を含まない。control_msはポリシー計算のみ。
毎秒flush、停止・無効化・pauseでcloseする。I/O失敗時は記録を停止しLastErrorを出す。

### 音響制御と観測の分離（2026-11-06）

`observationEnabled`は収集・Token生成・記録の実行を制御する。既定はtrue。
`controlEnabled`は音響制御だけを制御する。falseでも観測・記録は継続し、適用済みの音響設定は復元する。
全体を止めるには`SetObservationEnabled(false)`を使用する。再開はtrue。
`SetControlEnabled(false)`はログを一度closeするため、次の観測から新しいCSVファイルに記録する。

Rendererがない場合、無効な場合、適用に失敗した場合も、有効なTokenは`LatestToken`・`SamplesProcessed`・CSVに反映する。
制御未適用行は`mode=ObservationOnly`とし、gain・LPF・適用volume・control_msの列は空欄にする。
既存のCSV列順と8項目のToken形式は維持する。ただしmodeを列挙型として読み込む外部解析ではObservationOnlyへの対応が必要。
`LatestParameters`は制御未適用時に既定値となるため、有効な音響測定値として使用しない。

Collectorは現段階では位置取得のためAudioSourceとlistenerを必要とする。Rendererと音声再生は不要。
時刻は単一プロセス内の単調時刻であり、複数端末間の時刻同期は未実装。
今回の分離はC1〜C3比較の前段階であり、入力ログの再生、独立した正解データ、情報保持率・状態再現・データサイズ・生成単体時間の評価は未実装。
複数音源では1音源1行。比較に同じ入力が必要な場合はポリシー単体テストまたは固定Token再生を使い、ライブの別時刻を同一入力とみなさない。

## 音響SDKとXR

新規プロジェクトの既定RendererはUnity標準3D AudioSource。Meta XR Audioは未導入で、HRTF・SDK伝搬を検証済みとは扱わない。公式SDKを別途導入する場合はライセンスを確認し、spatializeとSpatializerを設定してQuest上で検証する。
LocalXRTestRigはOpenXR入力の最小テスト用。FusionのHardwareRig/NetworkRigを置換するものではない。既存環境へ持ち込む際はcollector.listenerをHardwareRigの頭部へ設定する。
