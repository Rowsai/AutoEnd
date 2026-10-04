# FFXIV Auto END

FFXIVの戦闘解除、または全滅通知を検出し、本文 `END` のカスタムログを出力してACTの現在のEncounterを終了する追加プラグインです。

## 必要なもの

- Windows / 64bit ACT
- FFXIV_ACT_Plugin
- OverlayPlugin（260番の戦闘状態ログを出力する版）

ビルド参照: ACT 3.8.5.288、OverlayPlugin v0.19.109。これらの本体ファイルは同梱していません。

## 導入

1. ZIPを展開して、AutoEndフォルダを任意の保存場所へ移動します。
2. ACTでFFXIV_ACT_Plugin、OverlayPluginを先に有効化します。
3. Plugins → Plugin Listing → Browseから `FfxivAutoEnd.dll` を選び、Add/Enable Pluginを押します。
4. 状態欄が「動作中: 戦闘終了・全滅を待機」になることを確認します。
5. OverlayPlugin → Event Settingsの `End ACT encounter after wipe` と `End ACT encounter out of combat` は、終了処理の競合を避けるためOFFを推奨します。

他の自動終了プラグインを使用している場合も、どれが終了を担当するか統一してください。DLLがWindowsにブロックされている場合は、ファイルのプロパティで「許可する」を選択してください。

## 動作とログ

|条件|動作|
|---|---|
|ゲーム内の戦闘状態がON→OFF|ENDを出力し計測終了|
|33番ログの全滅通知（4000000F / 40000010）|戦闘中ならENDを出力し計測終了|
|同じ戦闘で終了通知が重複|追加出力しない|
|次の戦闘のONを検出|再び終了検出を有効にする|
|過去ログのインポート|処理しない|

出力例（日時・チェックサムは実際の値になります）:

```text
1000000|2026-10-04T21:00:00.0000000+09:00|END|<checksum>
```

本文は大文字の `END` です。ログ全体にはFFXIVログ形式の種別・日時・チェックサムが付きます。OverlayPluginのカスタムログ登録・出力機能を使い、FFXIV_ACT_Pluginの戦闘ログ出力先へ記録します。FFXIV_ACT_Plugin側のディスクへのログ保存を有効にしてください。ACTの診断用エラーログにENDを書く実装ではありません。

ENDを出力した後、`ActGlobals.oFormActMain.EndCombat(true)` を呼びます。END文字列自体がACTを終了させるわけではありません。終了するのは現在の計測であり、ACTアプリは動作を続け、次の戦闘も計測できます。

## 判定範囲と制約

- 「戦闘終了」は敵全滅の独自判定ではなく、ゲームの戦闘状態解除です。撤退や、戦闘状態が解除されるフェーズ移行も対象です。
- 「全滅」はコンテンツのワイプ通知を利用します。フィールドなど通知がない場面は戦闘状態解除で終了します。全員のHPを監視する実装ではありません。
- OverlayPluginが260番ログを正常に出力している必要があります。ゲーム更新後の対応状況は利用中のOverlayPluginに依存します。
- ログID 1000000はこのプラグイン用のローカル選択値です。公式予約IDではありません。他プラグインによる別用途の登録がある場合は初期化エラーになります。
- 初期化待機が続く場合は、FFXIV_ACT_PluginとOverlayPluginの動作を確認して、このプラグインを無効→有効にしてください。OverlayPluginを再起動した場合も同様です。

## 実機での確認

1. 通常の戦闘を行い、戦闘状態解除時に計測が止まることを確認。
2. 保存ログで `|END|` を検索し、その戦闘で1行だけ出ていることを確認。
3. 次の戦闘を開始し、再び計測・終了することを確認。
4. コンテンツで全滅した際、ENDが1回出て計測が止まることを確認。

## ビルドと検証状況

ソースは `AutoEnd.cs` と `EndDetector.cs` です。Windows PowerShellで次のようにビルドできます。パスは実際のインストール先に変更してください。

```powershell
.\build.ps1 -ActExe 'C:\ACT\Advanced Combat Tracker.exe' -OverlayDirectory 'C:\ACT\OverlayPlugin'
```

ビルド成功。終了判定ロジックは25項目の自動検証に合格しました（通常終了、全滅2形式、重複通知、再戦、インポート無視、不正入力など）。実際のACT/FFXIVプロセスでの読み込み・ログ保存・計測終了は未検証です。

## 参照した一次資料

- [OverlayPluginの戦闘状態ログ実装](https://github.com/OverlayPlugin/OverlayPlugin/blob/main/OverlayPlugin.Core/MemoryProcessors/InCombat/LineInCombat.cs)
- [OverlayPluginの全滅判定](https://github.com/OverlayPlugin/OverlayPlugin/blob/main/OverlayPlugin.Core/EventSources/FFXIVOptionalEventSource.cs)
- [OverlayPluginのカスタムログAPI](https://github.com/OverlayPlugin/OverlayPlugin/blob/main/OverlayPlugin.Core/Integration/FFXIVCustomLogLines.cs)
- [ACT API](https://advancedcombattracker.com/apidoc/html/T_Advanced_Combat_Tracker_FormActMain.htm)
