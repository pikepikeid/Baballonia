using System.Reflection;

namespace VRCFaceTracking.Baballonia;

public class BabbleExtraConfig
{
    public bool PreventCrossEye { get; private set; }
    public float CrossEyeStrength { get; private set; }
    public bool UseWinkLock { get; private set; }
    public float JawOpenMax { get; private set; }

    public static BabbleExtraConfig Load()
    {
        string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string path = Path.Combine(dir, "config.ini");

        var cfg = new BabbleExtraConfig();

        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || !line.Contains('=')) continue;

            var parts = line.Split('=', 2);
            string key = parts[0].Trim();
            string val = parts[1].Trim();

            switch (key)
            {
                case "PreventCrossEye":
                    cfg.PreventCrossEye = bool.Parse(val);
                    break;

                case "CrossEyeStrength":
                    cfg.CrossEyeStrength = float.Parse(val);
                    break;

                case "UseWinkLock":
                    cfg.UseWinkLock = bool.Parse(val);
                    break;

                case "JawOpenMax":
                    cfg.JawOpenMax = float.Parse(val);
                    break;
            }
        }

        return cfg;
    }
}
