using System;

namespace FfxivAutoEnd
{
    public sealed class EndDetector
    {
        private bool armed;
        private bool gameCombat;
        public EndDetector(bool active) { armed = active; gameCombat = active; }

        // Input is the original network log, not ACT's formatted display text.
        public bool Feed(bool import, string line)
        {
            if (import || String.IsNullOrEmpty(line)) return false;
            string[] p = line.Split('|');
            int type;
            if (p.Length < 4 || !Int32.TryParse(p[0], out type)) return false;
            if (type == 260)
            {
                if (p.Length < 6 || (p[3] != "0" && p[3] != "1")) return false;
                bool current = p[3] == "1";
                if (current && !gameCombat) armed = true;
                bool finish = gameCombat && !current && armed;
                gameCombat = current;
                if (finish) armed = false;
                return finish;
            }
            if (type == 33 && (p[3] == "4000000F" || p[3] == "40000010") && armed)
            {
                armed = false;
                return true;
            }
            return false;
        }
    }
}
