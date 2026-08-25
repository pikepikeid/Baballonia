using System.Reflection;
using Microsoft.Extensions.Logging;

namespace VRCFaceTracking.Baballonia;

public class BabbleExtraConfig
{
    // 設定値のデフォルト値
    private const float DEFAULT_EyeInnerLimit = 1.0f;  // 内側（寄り目方向）
    private const float DEFAULT_EyeOuterLimit = 1.0f;   // 外側
    private const float DEFAULT_EyeUpLimit = 1.0f;      // 上方向
    private const float DEFAULT_EyeDownLimit = 1.0f;   // 下方向
    private const bool DEFAULT_PreventCrossEye = true;
    private const float DEFAULT_CrossEyeStrength = 0.8f;
    private const bool DEFAULT_UseCrossEyeSync = true;
    private const float DEFAULT_CrossEyeSyncOpenThreshold = 0.2f;
    private const float DEFAULT_CrossEyeSyncDiffThreshold = 0.1f;
    private const float DEFAULT_EyeLeftScale = 1.0f;
    private const float DEFAULT_EyeRightScale = 1.0f;
    private const bool DEFAULT_UseLidEyeCenter = true;
    private const float DEFAULT_LidEyeCenterThreshold = 0.95f;
    private const float DEFAULT_LidEyeCenterStrength = 1.5f;
    private const bool DEFAULT_UseWinkLock = true;
    private const float DEFAULT_PuckerJawOpenSuppression = 0.3f;
    private const float DEFAULT_JawOpenPuckerThreshold = 0.6f;
    private const float DEFAULT_JawOpenFunnelThreshold = 0.25f;
    private const float DEFAULT_JawOpenMax = 1.0f;
    private const int DEFAULT_SymmetricMode = 0; // 0=Max, 1=Average, 2=Min

    private const bool DEFAULT_UseEyeWide = true;
    private const bool DEFAULT_UseEyeWideCorrection = true;
    private const float DEFAULT_EyeWideLimit = 1.0f;
    private const bool DEFAULT_UseLidSync = true;
    private const float DEFAULT_SquintStrength = 0.5f;

    private const float DEFAULT_WinkSquintClosed = 0.85f;
    private const float DEFAULT_WinkSquintOpen = 0.5f;
    private const float DEFAULT_BothClosedSquint = 0.99f;
    private const float DEFAULT_BothOpenOpenness = 0.3f;
    private const float DEFAULT_BlinkSquint = 0.2f;
    private const float DEFAULT_BlinkLidThresholdOffset = 0.05f;

    private const int DEFAULT_WinkThresholdFrames = 2;
    private const int DEFAULT_BlinkThresholdFrames = 1;
    private const int DEFAULT_WinkReleaseFrames = 2;
    private const int DEFAULT_WinkJustReleasedIgnoreFrames = 1;
    // 設定値の読み込み
    public float EyeInnerLimit { get; private set; } = DEFAULT_EyeInnerLimit;
    public float EyeOuterLimit { get; private set; } = DEFAULT_EyeOuterLimit;
    public float EyeUpLimit { get; private set; } = DEFAULT_EyeUpLimit;
    public float EyeDownLimit { get; private set; } = DEFAULT_EyeDownLimit;
    public bool PreventCrossEye { get; private set; } = DEFAULT_PreventCrossEye;
    public float CrossEyeStrength { get; private set; } = DEFAULT_CrossEyeStrength;
    public bool UseCrossEyeSync { get; private set; } = DEFAULT_UseCrossEyeSync;
    public float CrossEyeSyncOpenThreshold { get; private set; } = DEFAULT_CrossEyeSyncOpenThreshold;
    public float CrossEyeSyncDiffThreshold { get; private set; } = DEFAULT_CrossEyeSyncDiffThreshold;
    public float EyeLeftScale { get; private set; } = DEFAULT_EyeLeftScale;
    public float EyeRightScale { get; private set; } = DEFAULT_EyeRightScale;
    public bool UseLidEyeCenter { get; private set; } = DEFAULT_UseLidEyeCenter;
    public float LidEyeCenterThreshold { get; private set; } = DEFAULT_LidEyeCenterThreshold;
    public float LidEyeCenterStrength { get; private set; } = DEFAULT_LidEyeCenterStrength;
    public bool UseWinkLock { get; private set; } = DEFAULT_UseWinkLock;
    public float PuckerJawOpenSuppression { get; private set; } = DEFAULT_PuckerJawOpenSuppression;
    public float JawOpenPuckerThreshold { get; private set; } = DEFAULT_JawOpenPuckerThreshold;
    public float JawOpenFunnelThreshold { get; private set; } = DEFAULT_JawOpenFunnelThreshold;
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
                    case "EyeInnerLimit":
                        cfg.EyeInnerLimit = float.Parse(val);
                        break;

                    case "EyeOuterLimit":
                        cfg.EyeOuterLimit = float.Parse(val);
                        break;

                    case "EyeUpLimit":
                        cfg.EyeUpLimit = float.Parse(val);
                        break;

                    case "EyeDownLimit":
                        cfg.EyeDownLimit = float.Parse(val);
                        break;

                    case "PreventCrossEye":
                        cfg.PreventCrossEye = bool.Parse(val);
                        break;

                    case "CrossEyeStrength":
                        cfg.CrossEyeStrength = float.Parse(val);
                        break;

                    case "UseCrossEyeSync":
                        cfg.UseCrossEyeSync = bool.Parse(val);
                        break;

                    case "CrossEyeSyncOpenThreshold":
                        cfg.CrossEyeSyncOpenThreshold = float.Parse(val);
                        break;

                    case "CrossEyeSyncDiffThreshold":
                        cfg.CrossEyeSyncDiffThreshold = float.Parse(val);
                        break;
                    case "EyeLeftScale":
                        cfg.EyeLeftScale = float.Parse(val);
                        break;
                    case "EyeRightScale":
                        cfg.EyeRightScale = float.Parse(val);
                        break;

                    case "UseLidEyeCenter":
                            cfg.UseLidEyeCenter = bool.Parse(val);
                            break;

                        case "LidEyeCenterThreshold":
                            cfg.LidEyeCenterThreshold = float.Parse(val);
                            break;

                        case "LidEyeCenterStrength":
                            cfg.LidEyeCenterStrength = float.Parse(val);
                            break;

                        case "UseWinkLock":
                            cfg.UseWinkLock = bool.Parse(val);
                            break;
                        case "PuckerJawOpenSuppression":
                            cfg.PuckerJawOpenSuppression = float.Parse(val);
                            break;
                        case "JawOpenPuckerThreshold":
                            cfg.JawOpenPuckerThreshold = float.Parse(val);
                            break;
                        case "JawOpenFunnelThreshold":
                            cfg.JawOpenFunnelThreshold = float.Parse(val);
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
        logger.LogInformation($"UseCrossEyeSync = {UseCrossEyeSync}");
        logger.LogInformation($"CrossEyeSyncOpenThreshold = {CrossEyeSyncOpenThreshold}");
        logger.LogInformation($"CrossEyeSyncDiffThreshold = {CrossEyeSyncDiffThreshold}");
        logger.LogInformation($"EyeLeftScale = {EyeLeftScale}");
        logger.LogInformation($"EyeRightScale = {EyeRightScale}");

        logger.LogInformation($"UseLidEyeCenter = {UseLidEyeCenter}");
        logger.LogInformation($"LidEyeCenterThreshold = {LidEyeCenterThreshold}");
        logger.LogInformation($"LidEyeCenterStrength = {LidEyeCenterStrength}");

        logger.LogInformation($"EyeInnerLimit = {EyeInnerLimit}");
        logger.LogInformation($"EyeOuterLimit = {EyeOuterLimit}");
        logger.LogInformation($"EyeUpLimit = {EyeUpLimit}");
        logger.LogInformation($"EyeDownLimit = {EyeDownLimit}");

        logger.LogInformation($"UseWinkLock = {UseWinkLock}");
        logger.LogInformation($"PuckerJawOpenSuppression = {PuckerJawOpenSuppression}");
        logger.LogInformation($"JawOpenPuckerThreshold = {JawOpenPuckerThreshold}");
        logger.LogInformation($"JawOpenFunnelThreshold = {JawOpenFunnelThreshold}");
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
