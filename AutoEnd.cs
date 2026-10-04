using System;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using RainbowMage.OverlayPlugin;

namespace FfxivAutoEnd
{
    public sealed class AutoEnd : IActPluginV1
    {
        private readonly object gate = new object();
        private EndDetector detector;
        private Func<string, DateTime, bool> writer;
        private Label status;
        private Timer startup;
        private volatile bool enabled;
        private bool subscribed;

        public void InitPlugin(TabPage tab, Label label)
        {
            status = label;
            tab.Text = "FFXIV Auto END";
            tab.Controls.Add(new Label { Dock = DockStyle.Fill, AutoSize = false,
                Text = "戦闘解除または全滅通知で END を戦闘ログに記録し、計測を終了します。\r\n"
                + "必要: FFXIV_ACT_Plugin / OverlayPlugin\r\n"
                + "ログ形式: 1000000|日時|END|チェックサム\r\n"
                + "OverlayPlugin側の自動終了2項目はOFFを推奨します。\r\n"
                + "ゲーム内の戦闘状態が解除されるフェーズ移行でも終了します。" });
            enabled = true;
            startup = new Timer { Interval = 1000 };
            startup.Tick += Start;
            startup.Start();
            status.Text = "OverlayPluginの初期化を待機中";
        }

        private void Start(object sender, EventArgs args)
        {
            try
            {
                var registry = Registry.GetContainer().Resolve<FFXIVCustomLogLines>();
                writer = registry.RegisterCustomLogLine(new LogLineRegistryEntry {
                    ID = 1000000, Name = "End", Source = "FfxivAutoEnd", Version = 1 });
                if (writer == null) throw new InvalidOperationException("ログID 1000000を登録できません。");
                detector = new EndDetector(ActGlobals.oFormActMain.InCombat);
                ActGlobals.oFormActMain.OnLogLineRead += OnLine;
                subscribed = true;
                startup.Stop();
                status.Text = "動作中: 戦闘終了・全滅を待機";
            }
            catch (Exception ex)
            {
                status.Text = "初期化待機: " + ex.Message;
            }
        }

        private void OnLine(bool import, LogLineEventArgs args)
        {
            if (!enabled || import) return;
            bool finish;
            lock (gate) finish = detector.Feed(import, args.originalLogLine);
            if (!finish) return;
            // Synchronous dispatch preserves ordering before the next encounter.
            try
            {
                ActGlobals.oFormActMain.Invoke((Action)delegate {
                    if (!enabled) return;
                    bool written = false;
                    try { written = writer("END", DateTime.Now); }
                    finally { ActGlobals.oFormActMain.EndCombat(true); }
                    status.Text = written ? "END 出力 / 計測終了: " + DateTime.Now.ToString("HH:mm:ss")
                        : "計測終了。ENDのログ出力に失敗しました。";
                });
            }
            catch (Exception ex)
            {
                ActGlobals.oFormActMain.WriteExceptionLog(ex, "FfxivAutoEnd");
            }
        }

        public void DeInitPlugin()
        {
            enabled = false;
            if (startup != null) { startup.Stop(); startup.Dispose(); startup = null; }
            if (subscribed) ActGlobals.oFormActMain.OnLogLineRead -= OnLine;
            subscribed = false;
            if (status != null) status.Text = "停止";
        }
    }
}
