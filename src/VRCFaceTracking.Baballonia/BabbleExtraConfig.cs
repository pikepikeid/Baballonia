using System.Reflection;
using Microsoft.Extensions.Logging;

namespace VRCFaceTracking.Baballonia;

public class BabbleExtraConfig
{
    // 設定値のデフォルト値
    private const bool DEFAULT_PreventCrossEye = true;
    private const float DEFAULT_CrossEyeStrength = 0.5f;
    private const bool DEFAULT_UseWinkLock = true;
    private const float DEFAULT_PuckerJawOpenSuppression = 0.2f;
    private const float DEFAULT_JawOpenMax = 0.9f;
    private const int DEFAULT_SymmetricMode = 0; // 0=Max, 1=Average, 2=Min

    private const bool DEFAULT_UseEyeWide = true;
    private const bool DEFAULT_UseEyeWideCorrection = true;
    private const float DEFAULT_EyeWideLimit = 1.0f;
    private const bool DEFAULT_UseLidSync = true;
    private const float DEFAULT_SquintStrength = 0.5f;

    private const float DEFAULT_WinkSquintClosed = 0.85f;
    private const float DEFAULT_WinkSquintOpen = 0.5f;
    private const float DEFAULT_BothClosedSquint = 0.99f;
    private const float DEFAULT_BothOpenOpenness = 0.45f;
    private const float DEFAULT_BlinkSquint = 0.2f;
    private const float DEFAULT_BlinkLidThresholdOffset = 0.05f;

    private const int DEFAULT_WinkThresholdFrames = 2;
    private const int DEFAULT_BlinkThresholdFrames = 1;
    private const int DEFAULT_WinkReleaseFrames = 2;
    private const int DEFAULT_WinkJustReleasedIgnoreFrames = 1;
    // 設定値の読み込み
    public bool PreventCrossEye { get; private set; } = DEFAULT_PreventCrossEye;
    public float CrossEyeStrength { get; private set; } = DEFAULT_CrossEyeStrength;
    public bool UseWinkLock { get; private set; } = DEFAULT_UseWinkLock;
    public float PuckerJawOpenSuppression { get; private set; } = DEFAULT_PuckerJawOpenSuppression;
    public float JawOpenMax { get; private set; } = DEFAULT_JawOpenMax;
    public int SymmetricMode { get; private set; } = DEFAULT_SymmetricMode;
    public bool UseEyeWide { get; private set; } = DEFAULT_UseEyeWide;
    public bool UseEyeWideCorrection { get; private set; } = DEFAULT_UseEyeWideCorrection;
    public float EyeWideLimit { get; private set; } = DEFAULT_EyeWideLimit;
    public bool UseLidSync { get; private set; } = DEFAULT_UseLidSync;
    public float SquintStrength { get; private set; } = DEFAULT_SquintStrength;

    public float WinkSquintClosed { get; private set; } = DEFAULT_WinkSquintClosed;
    public float WinkSquintOpen { get; private set; } = DEFAULT_WinkSquintOpen;
    public float BothClosedSquint { get; private set; } = DEFAULT_BothClosedSquint;
    public float BothOpenOpenness { get; private set; } = DEFAULT_BothOpenOpenness;
    public float BlinkSquint { get; private set; } = DEFAULT_BlinkSquint;
    public float BlinkLidThresholdOffset { get; private set; } = DEFAULT_BlinkLidThresholdOffset;

    public int WinkThresholdFrames { get; private set; } = DEFAULT_WinkThresholdFrames;
    public int BlinkThresholdFrames { get; private set; } = DEFAULT_BlinkThresholdFrames;
    public int WinkReleaseFrames { get; private set; } = DEFAULT_WinkReleaseFrames;
    public int WinkJustReleasedIgnoreFrames { get; private set; } = DEFAULT_WinkJustReleasedIgnoreFrames;

    public static BabbleExtraConfig Load(ILogger logger)
    {
        string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string path = Path.Combine(dir, "config.ini");

        var cfg = new BabbleExtraConfig();

        // config.ini が存在しない場合は埋め込みリソースから初期設定を生成する
        if (!File.Exists(path))
        {
            using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("VRCFaceTracking.Baballonia.config.ini");
            if (stream == null)
            {
                logger.LogWarning("[BabbleExtraConfig] 埋め込みリソース config.ini が見つかりません。空の設定を生成します。");
                File.WriteAllText(path, "");
            }
            else
            {
                logger.LogInformation("[BabbleExtraConfig] config.ini が存在しないため初期設定を生成しました。");
                logger.LogInformation("[BabbleExtraConfig] 初期設定を読み込みます。");
                using var reader = new StreamReader(stream);
                File.WriteAllText(path, reader.ReadToEnd());
            }
        }

        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || !line.Contains('=')) continue;

            var parts = line.Split('=', 2);
            string key = parts[0].Trim();
            string val = parts[1].Trim();

            try
            {
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
                    case "PuckerJawOpenSuppression":
                        cfg.PuckerJawOpenSuppression = float.Parse(val);
                        break;

                    case "JawOpenMax":
                        cfg.JawOpenMax = float.Parse(val);
                        break;

                    case "SymmetricMode":
                        cfg.SymmetricMode = int.Parse(val);
                        break;

                    case "UseEyeWide":
                        cfg.UseEyeWide = bool.Parse(val);
                        break;

                    case "UseEyeWideCorrection":
                        cfg.UseEyeWideCorrection = bool.Parse(val);
                        break;

                    case "EyeWideLimit":
                        cfg.EyeWideLimit = float.Parse(val);
                        break;
                    case "UseLidSync":
                        cfg.UseLidSync = bool.Parse(val);
                        break;
                    case "SquintStrength":
                        cfg.SquintStrength = float.Parse(val);
                        break;

                    case "WinkSquintClosed":
                        cfg.WinkSquintClosed = float.Parse(val);
                        break;

                    case "WinkSquintOpen":
                        cfg.WinkSquintOpen = float.Parse(val);
                        break;

                    case "BothClosedSquint":
                        cfg.BothClosedSquint = float.Parse(val);
                        break;

                    case "BothOpenOpenness":
                        cfg.BothOpenOpenness = float.Parse(val);
                        break;

                    case "BlinkSquint":
                        cfg.BlinkSquint = float.Parse(val);
                        break;

                    case "WinkThresholdFrames":
                        cfg.WinkThresholdFrames = int.Parse(val);
                        break;

                    case "BlinkThresholdFrames":
                        cfg.BlinkThresholdFrames = int.Parse(val);
                        break;

                    case "WinkReleaseFrames":
                        cfg.WinkReleaseFrames = int.Parse(val);
                        break;

                    case "WinkJustReleasedIgnoreFrames":
                        cfg.WinkJustReleasedIgnoreFrames = int.Parse(val);
                        break;

                    case "BlinkLidThresholdOffset":
                        cfg.BlinkLidThresholdOffset = float.Parse(val);
                        break;


                    default:
                        logger.LogWarning($"[BabbleExtraConfig] 不明なキーを無視しました: {key}");
                        break;
                }
            }
            catch
            {
                logger.LogWarning($"[BabbleExtraConfig] {key} の値が不正なため初期値を使用します。");
            }
        }
        cfg.PrintConfig(logger);
        return cfg;
    }
    private void PrintConfig(ILogger logger)
    {
        logger.LogInformation("===== BabbleExtraConfig Loaded =====");
        logger.LogInformation($"SymmetricMode = {SymmetricMode}");
        logger.LogInformation($"PreventCrossEye = {PreventCrossEye}");
        logger.LogInformation($"CrossEyeStrength = {CrossEyeStrength}");
        logger.LogInformation($"UseWinkLock = {UseWinkLock}");
        logger.LogInformation($"PuckerJawOpenSuppression = {PuckerJawOpenSuppression}");
        logger.LogInformation($"JawOpenMax = {JawOpenMax}");
        logger.LogInformation($"UseEyeWide = {UseEyeWide}");
        logger.LogInformation($"UseEyeWideCorrection = {UseEyeWideCorrection}");
        logger.LogInformation($"EyeWideLimit = {EyeWideLimit}");
        logger.LogInformation($"UseLidSync = {UseLidSync}");
        logger.LogInformation($"SquintStrength = {SquintStrength}");

        logger.LogInformation($"WinkSquintClosed = {WinkSquintClosed}");
        logger.LogInformation($"WinkSquintOpen = {WinkSquintOpen}");
        logger.LogInformation($"BothClosedSquint = {BothClosedSquint}");
        logger.LogInformation($"BothOpenOpenness = {BothOpenOpenness}");
        logger.LogInformation($"BlinkSquint = {BlinkSquint}");

        logger.LogInformation($"BlinkThresholdFrames = {BlinkThresholdFrames}");
        logger.LogInformation($"WinkThresholdFrames = {WinkThresholdFrames}");
        logger.LogInformation($"WinkReleaseFrames = {WinkReleaseFrames}");
        logger.LogInformation($"WinkJustReleasedIgnoreFrames = {WinkJustReleasedIgnoreFrames}");
        logger.LogInformation($"BlinkLidThresholdOffset = {BlinkLidThresholdOffset}");
        logger.LogInformation("====================================");
    }
}
