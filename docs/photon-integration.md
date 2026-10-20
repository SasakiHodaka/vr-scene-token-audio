# Phase 5の設計案（未実装）

Phase 1〜4のローカル動作・実機検証後、既存Photon環境への影響を確認して着手する。今回、Fusion/Voice SDK、App ID設定、NetworkBehaviour、既存VRシーンは新規リポジトリへコピーしていない。

## 同期の境界

|共有する情報|送信元・方式の案|
|---|---|
|sourceIdとNetworkObject/Speakerの対応|スポーン時にStateAuthorityが確定。再接続・退出時に解除|
|音源位置・回転|既存NetworkRig/NetworkTransformを継続使用し、Tokenで重複送信しない|
|taskPhase、priority、期限の終了tick|作業対象のStateAuthorityがNetworked propertyとして送る。変更時/ネットワークtickに更新|
|音声ストリーム|既存FusionVoiceClient / Recorder / VoiceNetworkObject / Speakerを維持。Tokenに音声データを入れない|
|距離・relativeDirection・occlusion|各聞き手のローカル頭部と同期済み音源から生成。全員に一つのTokenを配信しない|
|CSV・参加者情報・制御方式|実験制御上共有が必要なものを別途定義。生音声・個人識別情報はこのログには不要|

現在のISceneTokenTransportは聞き手ローカル観測の配送用。そのPublishをそのままPhoton Broadcastに差し替えない。共有状態用DTO/adapterを追加し、受信した共有状態から各聞き手のcollectorがTokenを生成する。将来Tokenそのものを送る用途ではlistener IDとschema versionをEnvelopeに持たせる。
timestampは現在アプリ起動後の秒。端末間で直接比較しない。期限共有はFusion tick基準で同期し、聞き手側で残秒へ変換する。順序番号・送信上限・sourceIdの権限・期限の上限・未知enum・退出時の破棄を検証する。

## 既存環境に追加する手順案

1. Photon統合ブランチ6000.0.58f2 / Fusion 2.0.6 / Voice 2.59の現状で、2クライアントの接続・頭手同期・組立操作・双方向音声を測定して基準を保存する。
2. 新規SceneTokenAudioモジュールを独立フォルダー/asmdefとして追加。新モジュールはFusion型に依存しない。既存Prefabを直接変更する代わりにVariantまたは外部binderを検討する。
3. Remote Speakerが生成された後、その再生AudioSourceへrendererを一つだけ接続する。sourceId、Remote Player、Voice streamの対応を確認する。ローカル自声には適用しない。
4. listenerはローカルHardwareRigの頭部。HeadsetとSpeakerの実位置を維持し、旧SceneTokenDecoderRendererの再配置・pitch/volume制御を同じ音源で無効にする。
5. SDK側の遮蔽を使うか今回の幾何学遮蔽を使うか決定し、減衰・LPFの二重処理を避ける。距離ロールオフの設定は共通化する。
6. 既存シーンの操作を変えずに、途中参加/退出/再接続/権限移譲/マイク拒否/2人以上の発話を検証する。

App IDは新規アプリでローカル設定し、除外対象のPhotonAppSettings.assetを生成する。秘密鍵やサーバートークンをクライアントへ入れない。サンプルAssetとSDKの公開再配布は採用ライセンスを確認してから判断する。

着手前の確認事項: SDK版を維持するか、対象ネットワークtopologyをSharedで固定するか、保存する作業段階とそのStateAuthority、採用する音響SDK/遮蔽所有者、比較実験条件の同期方法。実機の基準試験が未実施のため既存Photon機能の維持を実証済みとはしない。
