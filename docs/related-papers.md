# 関連論文

研究テーマ：VR遠隔協調作業（意味的空間音声・通信手法）のためのScene Tokenの構成。

調査日：2026-11-06。以下は一次資料の書誌情報・要旨・公開説明を確認した読解候補と、その適用案。全論文の全文精読・実験再現を完了したものではない。掲載内容だけで本研究の新規性が確定したとは扱わない。

本研究のScene Tokenは、音声・空間・動作・作業状況を共通IDと時刻で関連付ける構造化情報表現を想定する。ニューラルモデルの連続埋め込み、音声コーデックのコード、VQ-VAEの学習型離散コードとは区別する。

## 優先して読む論文

### 1. Point-3D LLM: Studying the Impact of Token Structure for 3D Scene Understanding With Large Language Models

- 著者・年：Hugues Thomas, Chen Chen, Jian Zhang, 2025。
- 一次資料：[Apple Machine Learning Research](https://machinelearning.apple.com/research/pts3d-llm)。
- 内容：モデルのバックボーン・パラメータをそろえ、動画型と点群型のToken構造を比較する。3D特徴の統合、点の選択・順序も検討する。
- 参考にする点：入力条件と他の設定を固定して表現方法の影響を調べる実験設計。
- 違い：C1〜C3は構造だけでなく含める情報も変えるため、主に情報追加の効果を検証する比較となる。同一情報量での構造比較とは区別する。

### 2. SceneReasoner: Decoupled Spatial Tokenization for Indoor Large-Scene Understanding with LLMs

- 著者・年：Bohang Shi, Shengjun Tang, Xiaoming Li, Weixi Wang, Linfu Xie, Baoding Zhou, 2026。
- 掲載：ISPRS Archives, XLIX-B2-2026, 1319–1325。
- 一次資料：[論文・書誌情報](https://isprs-archives.copernicus.org/articles/XLIX-B2-2026/1319/2026/)。
- 内容：質問に関連する点特徴を圧縮前に重み付けし、2D・3D特徴の融合と層ごとの特徴注入を行う。
- 参考にする点：必要な処理に応じて情報を選択し、背景の冗長情報を減らす発想。
- 違い：質問への関連性と、協調作業上の必須情報は同一ではない。後者は作業シナリオから別途定義する。

### 3. 3DGraphLLM: Combining Semantic Graphs and Large Language Models for 3D Scene Understanding

- 著者・掲載：Tatiana Zemskova, Dmitry Yudin, ICCV 2025。arXiv初稿は2024年。
- 一次資料：[原論文](https://arxiv.org/abs/2412.18450)、[ICCV論文PDF](https://openaccess.thecvf.com/content/ICCV2025/papers/Zemskova_3DGraphLLM_Combining_Semantic_Graphs_and_Large_Language_Models_for_3D_ICCV_2025_paper.pdf)。
- 内容：物体の特徴だけでなく物体間の意味的関係を含む学習可能なシーングラフ表現をLLMへ入力する。
- 参考にする点：作業者・対象物・操作の関係を明示する設計と、関係情報を追加する効果の検証。
- 違い：作業工程の依存関係や発話と操作の時間関係を、そのまま解決する研究ではない。これらへの拡張は本研究側の設計案。

## 要求条件・動的な関係表現の基礎

### 4. A Descriptive Framework of Workspace Awareness for Real-Time Groupware

- 著者・掲載：Carl Gutwin, Saul Greenberg, Computer Supported Cooperative Work 11, 411–446, 2002。
- 一次資料：[出版社・DOI](https://doi.org/10.1023/A:1021271517844)。
- 内容：共有作業空間で他者の活動を把握するための知識、知覚の仕組み、協調作業での利用を整理する。
- 参考にする点：「誰が・どこで・何をしているか」など、Tokenに保持すべき情報の要求条件を導く根拠。
- 違い：Token化やVR固有の実装方法を提案するものではない。ドローン組立への要求条件の具体化が必要。

### 5. 3D Dynamic Scene Graphs: Actionable Spatial Perception with Places, Objects, and Humans

- 著者・掲載：Antoni Rosinol, Arjun Gupta, Marcus Abate, Jingnan Shi, Luca Carlone, RSS 2020。
- 一次資料：[原論文](https://arxiv.org/abs/2002.06289)、[著者公開PDF](https://www.mit.edu/~arosinol/papers/Rosinol20rss-dsg.pdf)。
- 内容：場所・物体・人と、それらの階層的・時空間的関係を動的なシーングラフで統合する。
- 参考にする点：作業者・部品・場所をIDで管理し、変化する関係を表す設計。既存の明示的構造化表現として比較する。
- 違い：音声・発話・組立工程を含む本研究の要求をすべて扱うものではない。

## 発話と空間の対応・音声Token

### 6. SceneVerse: Scaling 3D Vision-Language Learning for Grounded Scene Understanding

- 著者・掲載：Baoxiong Jia, Yixin Chenほか, ECCV 2024。
- 一次資料：[原論文](https://arxiv.org/abs/2401.09340)、[公式コード・引用情報](https://github.com/scene-verse/SceneVerse)。
- 内容：3Dシーンと言語の大規模な対応データと、グラウンディングに向けた学習を扱う。
- 参考にする点：発話中の対象物と空間内の物体を対応付ける設計、物体属性・関係を使う記述。
- 違い：実際の発話イベント、話者、操作時刻の管理をそのまま提供する研究ではない。

### 7. Moshi: a speech-text foundation model for real-time dialogue

- 著者・年：Alexandre Défossez, Laurent Mazaréほか, 2024。
- 一次資料：[原論文](https://arxiv.org/abs/2410.00037)、[Kyutai公開PDF](https://kyutai.org/Moshi.pdf)。
- 内容：ニューラル音声コーデックのTokenとテキストを用い、並列音声ストリームによる双方向対話を扱う。
- 参考にする点：将来のSpeech Tokenizer接続、音声とテキストの時間整合、テキスト化で失われる非言語情報。
- 違い：発話テキスト・意味ラベルを格納する現在の構造化Scene Tokenと、音声コードは異なる。直近の実装にモデル学習や波形Token化を必須としない。

## 圧縮・逐次更新・学習型表現

| 論文 | 年・一次資料 | 参考にする点と適用上の違い |
|---|---|---|
| SceneTok: A Compressed, Diffusable Token Space for 3D Scenes — Mohammad Asim, Christopher Wewer, Jan Eric Lenssen | 2026：[原論文](https://arxiv.org/abs/2602.18882)、[公式コード](https://github.com/mohammadasim98/scenetok) | 多視点画像を圧縮表現に変換し、新規視点画像を再構成する。圧縮と再構成品質のトレードオフを参照するが、画像再構成と作業状態の再現は異なる。名称も本研究のScene Tokenとは区別する。 |
| SoftNav: Injecting 3D Scene Tokens into VLMs for Embodied Navigation — Yi Wuほか | 2026：[原論文](https://arxiv.org/abs/2607.14586) | 物体などの連続表現をsoft tokenとしてVLMへ入力する。将来のAI接続方式の参考。検証対象のナビゲーション条件での結果を、JSON一般の不適切さの根拠にはしない。 |
| Stream3D-VLM: Online 3D Spatial Understanding with Incremental Geometry Priors — Hanxun Yuほか | 2026：[原論文](https://arxiv.org/abs/2606.06891)、[公式コード](https://github.com/hanxunyu/Stream3D-VLM) | 逐次映像への幾何情報の統合と、幾何に応じた圧縮を参照する。操作イベントの取りこぼしや工程履歴は別途評価する。 |
| Neural Discrete Representation Learning — Aaron van den Oord, Oriol Vinyals, Koray Kavukcuoglu | 2017：[原論文](https://arxiv.org/abs/1711.00937) | VQ-VAEによる学習型離散表現の基礎。今回の構造化レコードとの用語の違いを説明するために使う。 |

## 本研究の仕様・評価への適用案

以下は文献の結果そのものではなく、本研究の設計への提案。

1. Workspace Awarenessと作業シナリオから必須情報を導き、各必須情報に正解値・許容誤差・許容時間差を定義する。
2. 作業者・部品・工程を共通IDで参照し、発話・把持・取付・工程完了を時刻付きで関連付ける。状態とイベントを区別する。
3. C1（音声＋空間）、C2（動作追加）、C3（作業状態追加）を同じ入力ログから生成する。更新周期・数値精度・出力形式・測定環境を統一する。
4. 正しい必須情報の保持率、Tokenからの作業状態再現、UTF-8出力のバイト数、生成・更新時間を評価する。学習モデルのToken数とJSONのバイト数を混同しない。
5. 関連付け自体の効果を主張する場合は、同じ情報項目を持つ「関連付けなし」と「ID・時刻による関連付けあり」も比較する。C1〜C3だけで情報統合方法の優位性まで断定しない。
6. C3を正解と決めず、場面ごとの必要情報とコストの関係から削減案を検討する。デモは動作証拠とし、妥当性は数値評価で示す。

読む順序の目安：Workspace Awareness → Dynamic Scene Graphs／3DGraphLLM → Point-3D LLM → SceneReasoner。SceneVerseは対象物と言語の対応、Moshiなどは将来拡張の参考として読む。

## 継続調査

- 協調VRでの発話・視線・指差し・操作イベントを統合する表現。
- 組立工程・前提条件・担当者間の引き継ぎを扱うタスク表現。
- タスク依存の情報符号化、意味通信、帯域制約下の情報選択。
- 同じ情報集合に対する関係表現の有無、状態とイベントの粒度、履歴保持範囲の比較。

これらと照合したうえで、本研究の独自性と評価対象を絞り込む。
