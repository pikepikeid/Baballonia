using System.Reflection;
using Microsoft.Extensions.Logging;
using VRCFaceTracking.Core.Params.Expressions;

namespace VRCFaceTracking.Baballonia;

public enum SymmetricMode
{
    Max = 0,
    Avg = 1,
    Min = 2
}
public class BabbleVrc : ExtTrackingModule
{
    private BabbleOsc? babbleOSC;
    private Config? config;
    private bool needsEye;
    private bool needsExpression;
    private BabbleExtraConfig? extra;
    private static readonly UnifiedExpressions[] AllExprs =
    (UnifiedExpressions[])Enum.GetValues(typeof(UnifiedExpressions));


    // 前フレームの Openness
    float prevLeftLid = 1f;
    float prevRightLid = 1f;

    // ウィンク・瞬き履歴
    int leftWinkCounter = 0;
    int rightWinkCounter = 0;
    int blinkCounter = 0;

    // ロック状態
    bool winkLocked = false;
    bool leftWinkLocked = false;
    bool rightWinkLocked = false;

    // カウンタ周り
    int winkReleaseCounter = 0;
    bool winkJustReleased = false;
    int winkJustReleasedFrames = 0;
    int blinkReleaseCounter = 0;

    // config.ini から読み込むパラメータ
    private SymmetricMode mouthMode;
    private float PuckerJawOpenSuppression;
    private float JawOpenPuckerThreshold;
    private float JawOpenFunnelThreshold;
    private float JawOpenMax;
    private float WinkSquintClosed;
    private float WinkSquintOpen;
    private float BothClosedSquint;
    private float BothOpenOpenness;
    private float BlinkSquint;
    private int WinkThresholdFrames;
    private int BlinkThresholdFrames;
    private int WinkReleaseFrames;
    private int WinkJustReleasedIgnoreFrames;
    private float SquintStrength;
    private float BlinkLidThreshold; // これはオフセット
    private bool UseLidSync;
    private bool UseLidEyeCenter;
    private float LidEyeCenterThreshold;
    private float LidEyeCenterStrength;
    private bool UseEyeWide;
    private bool UseEyeWideCorrection;
    private float EyeWideLimit;
    private bool PreventCrossEye;
    private bool UseCrossEyeSync;
    private float CrossEyeStrength;
    private float CrossEyeSyncOpenThreshold;
    private float CrossEyeSyncDiffThreshold;
    private float EyeLeftScale;
    private float EyeRightScale;
    private bool UseWinkLock;
    private float EyeInnerLimit;
    private float EyeOuterLimit;
    private float EyeUpLimit;
    private float EyeDownLimit;


    private static float Clamp01(float v) // Utility
    {
        return MathF.Min(MathF.Max(v, 0f), 1f);
    }
    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    public void Initialize(BabbleExtraConfig extra)
    {
        mouthMode = extra.SymmetricMode switch
        {
            1 => SymmetricMode.Avg,
            2 => SymmetricMode.Min,
            _ => SymmetricMode.Max
        };
        PuckerJawOpenSuppression = extra.PuckerJawOpenSuppression;
        JawOpenPuckerThreshold = extra.JawOpenPuckerThreshold;
        JawOpenFunnelThreshold = extra.JawOpenFunnelThreshold;
        JawOpenMax = extra.JawOpenMax;

        UseWinkLock = extra.UseWinkLock;
        WinkThresholdFrames = extra.WinkThresholdFrames;
        BlinkThresholdFrames = extra.BlinkThresholdFrames;
        WinkReleaseFrames = extra.WinkReleaseFrames;
        WinkJustReleasedIgnoreFrames = extra.WinkJustReleasedIgnoreFrames;

        WinkSquintClosed = extra.WinkSquintClosed;
        WinkSquintOpen = extra.WinkSquintOpen;
        BothClosedSquint = extra.BothClosedSquint;
        BothOpenOpenness = extra.BothOpenOpenness;
        BlinkSquint = extra.BlinkSquint;

        SquintStrength = extra.SquintStrength;

        BlinkLidThreshold = BothOpenOpenness + extra.BlinkLidThresholdOffset; // これはオフセット

        UseLidSync = extra.UseLidSync;
        UseLidEyeCenter = extra.UseLidEyeCenter;
        LidEyeCenterThreshold = extra.LidEyeCenterThreshold;
        LidEyeCenterStrength = extra.LidEyeCenterStrength;
        UseEyeWide = extra.UseEyeWide;
        UseEyeWideCorrection = extra.UseEyeWideCorrection;
        EyeWideLimit = extra.EyeWideLimit;
        PreventCrossEye = extra.PreventCrossEye;
        UseCrossEyeSync = extra.UseCrossEyeSync;
        CrossEyeStrength = extra.CrossEyeStrength;
        CrossEyeSyncOpenThreshold = extra.CrossEyeSyncOpenThreshold;
        CrossEyeSyncDiffThreshold = extra.CrossEyeSyncDiffThreshold;
        EyeLeftScale = extra.EyeLeftScale;
        EyeRightScale = extra.EyeRightScale;

        EyeInnerLimit = -extra.EyeInnerLimit; //マイナス値にする
        EyeOuterLimit = extra.EyeOuterLimit;
        EyeUpLimit = extra.EyeUpLimit;
        EyeDownLimit = -extra.EyeDownLimit; //マイナス値にする
    }

    // We need to call GetBabbleConfig ahead of Initialize
    public override (bool SupportsEye, bool SupportsExpression) Supported => (true, true);

    public override (bool eyeSuccess, bool expressionSuccess) Initialize(bool eyeAvailable, bool expressionAvailable)
    {
        config = BabbleConfig.GetBabbleConfig();
        extra = BabbleExtraConfig.Load(Logger);
        babbleOSC = new BabbleOsc(Logger, config.Host, config.Port);
        Initialize(extra);

        List<Stream> list = new List<Stream>();
        Assembly executingAssembly = Assembly.GetExecutingAssembly();
        if (eyeAvailable && config.IsEyeSupported)
        {
            Logger.LogInformation("Baballonia will use Eye Tracking.");
            Stream manifestResourceStream = executingAssembly.GetManifestResourceStream("VRCFaceTracking.Baballonia.BabbleEyeLogo.png")!;
            list.Add(manifestResourceStream);
            needsEye = true;
        }
        if (expressionAvailable && config.IsFaceSupported)
        {
            Logger.LogInformation("Baballonia will use Face Tracking.");
            Stream manifestResourceStream = executingAssembly.GetManifestResourceStream("VRCFaceTracking.Baballonia.BabbleFaceLogo.png")!;
            list.Add(manifestResourceStream);
            needsExpression = true;
        }

        executingAssembly.GetManifestResourceNames();

        ModuleInformation = new ModuleMetadata
        {
            Name = "【Unofficial】Project Babble Module（Modified.8）",
            StaticImages = list
        };

        return (needsEye, needsExpression);
    }

    public override void Teardown()
    {
        babbleOSC?.Teardown();
    }

    // 口の補正
    // 左右対称化するパラメータ
    private static readonly (UnifiedExpressions L, UnifiedExpressions R)[] SymmetricPairs =
        {
            (UnifiedExpressions.MouthCornerPullLeft, UnifiedExpressions.MouthCornerPullRight),
            (UnifiedExpressions.MouthFrownLeft, UnifiedExpressions.MouthFrownRight),
            (UnifiedExpressions.MouthDimpleLeft, UnifiedExpressions.MouthDimpleRight),
            (UnifiedExpressions.MouthUpperUpLeft, UnifiedExpressions.MouthUpperUpRight),
            (UnifiedExpressions.MouthLowerDownLeft, UnifiedExpressions.MouthLowerDownRight),
            (UnifiedExpressions.MouthPressLeft, UnifiedExpressions.MouthPressRight),
            (UnifiedExpressions.MouthStretchLeft, UnifiedExpressions.MouthStretchRight),
        };
    private float[] CorrectMouth(float[] raw)
    {
        float[] corrected = new float[raw.Length];
        Array.Copy(raw, corrected, raw.Length);

        float GetSymmetric(UnifiedExpressions l, UnifiedExpressions r)
        {
            float lv = raw[(int)l];
            float rv = raw[(int)r];

            return mouthMode switch
            {
                SymmetricMode.Avg => (lv + rv) * 0.5f,
                SymmetricMode.Min => MathF.Min(lv, rv),
                _ => MathF.Max(lv, rv)
            };
        }

        // よく使うパラメータ
        float jawOpen = raw[(int)UnifiedExpressions.JawOpen];
        float pucker = MathF.Max(
            MathF.Max(raw[(int)UnifiedExpressions.LipPuckerLowerLeft], raw[(int)UnifiedExpressions.LipPuckerLowerRight]),
            MathF.Max(raw[(int)UnifiedExpressions.LipPuckerUpperLeft], raw[(int)UnifiedExpressions.LipPuckerUpperRight])
        );
        float funnel = MathF.Max(
            MathF.Max(raw[(int)UnifiedExpressions.LipFunnelLowerLeft], raw[(int)UnifiedExpressions.LipFunnelLowerRight]),
            MathF.Max(raw[(int)UnifiedExpressions.LipFunnelUpperLeft], raw[(int)UnifiedExpressions.LipFunnelUpperRight])
        );
        float stretch = GetSymmetric(UnifiedExpressions.MouthStretchLeft, UnifiedExpressions.MouthStretchRight);
        float lowerDown = GetSymmetric(UnifiedExpressions.MouthLowerDownLeft, UnifiedExpressions.MouthLowerDownRight);
        float upperUp = GetSymmetric(UnifiedExpressions.MouthUpperUpLeft, UnifiedExpressions.MouthUpperUpRight);
        float smile = GetSymmetric(UnifiedExpressions.MouthCornerPullLeft, UnifiedExpressions.MouthCornerPullRight);
        float raiserLower = raw[(int)UnifiedExpressions.MouthRaiserLower];
        float mouthpress = GetSymmetric(UnifiedExpressions.MouthPressLeft, UnifiedExpressions.MouthPressRight);

        foreach (UnifiedExpressions expr in AllExprs)
        {
            float value = raw[(int)expr];

            foreach (var (L, R) in SymmetricPairs)
            {
                if (expr == L || expr == R)
                {
                    value = GetSymmetric(L, R);
                    break;
                }
            }

            value = CorrectMouthExpression(expr, value, jawOpen, pucker, funnel, stretch, lowerDown, upperUp, smile, raiserLower, mouthpress);
            corrected[(int)expr] = value;
        }

        return corrected;
    }

    private float CorrectMouthExpression(
        UnifiedExpressions expr, float value,
        float jawOpen, float pucker, float funnel,
        float stretch, float lowerDown, float upperUp,
        float smile, float raiserLower, float mouthpress)
    {
        switch (expr)
        {
            // 口を開けている時や笑っているときは口角下げを抑制したり無効にする
            // 口を開けていても横に広げて口角を下げる場合は場合は強調する
            case UnifiedExpressions.MouthFrownLeft:
            case UnifiedExpressions.MouthFrownRight:
                value = MathF.Min(value + raiserLower, 1.0f);
                if (jawOpen >= 0.3f)
                    value = MathF.Min(value * 1.5f, 1.0f);
                if (jawOpen >= 0.3f && stretch >= 0.3f)
                    value = MathF.Min(value * 1.3f, 1.0f);
                if (smile >= 0.8f && jawOpen < 0.3f)
                    value = 0f;
                break;

            // 口をすぼめるときはJawOpenを抑制する
            case UnifiedExpressions.JawOpen:
                if (funnel <= JawOpenFunnelThreshold) // 規定値 0.25f
                {
                    // puckerが閾値を超えた分を0～1に正規化
                    float t = Clamp01((pucker - JawOpenPuckerThreshold) / (1.0f - JawOpenPuckerThreshold)); // 規定値 0.6f

                    // 抑制値を計算
                    float curve = t * t * (3f - 2f * t);

                    // 抑制値を適用
                    float suppressionFactor = 1.0f + (PuckerJawOpenSuppression - 1.0f) * curve; // 規定値 0.2f
                    value *= suppressionFactor;
                }
                else
                    value = MathF.Min(value, JawOpenMax); // 最大値を既定値0.85に制限
                break;

            // 口をすぼめるときはMouthClosedを抑制する（要るかなぁこれ）
            case UnifiedExpressions.MouthClosed:
                if (pucker >= 0.6f)
                    value *= 0.1f;
                break;

            // 口をすぼめるときや意図的に悲しい表情をしていないときはMouthStretchを抑制する
            case UnifiedExpressions.MouthStretchLeft:
            case UnifiedExpressions.MouthStretchRight:
                if (pucker >= 0.5f)
                    value = 0f;
                if (lowerDown < 0.2f || raiserLower < 0.5f)
                    value = 0f;
                break;

            // 口を開けて悲しい表情をするときはSmileを抑制する
            case UnifiedExpressions.MouthCornerPullLeft:
            case UnifiedExpressions.MouthCornerPullRight:
                if (jawOpen >= 0.5f && stretch >= 0.5f)
                    value *= 0.1f;
                break;

            // 口を開けている時や口をへの字にしてるときはLipFunnelを抑制する
            case UnifiedExpressions.LipFunnelLowerLeft:
            case UnifiedExpressions.LipFunnelLowerRight:
            case UnifiedExpressions.LipFunnelUpperLeft:
            case UnifiedExpressions.LipFunnelUpperRight:
                if (jawOpen >= 0.65f)
                    value *= 0.1f;
                if (raiserLower >= 0.3f || lowerDown >= 0.3f)
                    value = 0f;
                break;

            // 単に口を開けているだけの時に悲しい表情が出やすいのを抑制する
            case UnifiedExpressions.MouthLowerDownLeft:
            case UnifiedExpressions.MouthLowerDownRight:
                if (jawOpen >= 0.3f && stretch < 0.2f)
                    value *= 0.1f;
                break;

            // 笑顔の時に悲しい表情をしないようにする
            case UnifiedExpressions.MouthRaiserLower:
                if (smile >= 0.8f)
                    value = 0f;
                break;
        }
        return value;
    }

    // 目補正（ウィンクロック・瞬きロック・履歴）
    private (float left, float right) CorrectEyes(float leftLid, float rightLid, float leftSquint, float rightSquint)
    {
        // 左右の最小値を先に揃える
        float minLid = MathF.Min(leftLid, rightLid);
        if (!UseWinkLock) { return (Clamp01(leftLid), Clamp01(rightLid)); }

        // 補正前の生の値を取得（判定用）
        float rawLeft = BabbleOsc.LeftEyeOpenness;
        float rawRight = BabbleOsc.RightEyeOpenness;

        // 左右のSquintが0.99（デフォルト値）以上なら両目を閉じているとみなす
        bool bothClosed = leftSquint >= BothClosedSquint && rightSquint >= BothClosedSquint;
        // 両目を閉じてない場合にウィンク候補を判定
        bool leftWinkCandidate =
            leftSquint >= WinkSquintClosed &&
            rightSquint <= WinkSquintOpen &&
            !bothClosed;

        bool rightWinkCandidate =
            rightSquint >= WinkSquintClosed &&
            leftSquint <= WinkSquintOpen &&
            !bothClosed;

        // ウィンクロックされていないときの瞬き判定
        bool blinkCandidate =
            leftSquint < BlinkSquint &&
            rightSquint < BlinkSquint &&
            minLid < BlinkLidThreshold;

        // アルファテスト版かどうかの雑な判定
        bool isAlphaModel = (leftSquint > 0.01f || rightSquint > 0.01f);

        // ロック中・解除直後は候補を無効化
        if (winkLocked || winkJustReleased)
        {
            leftWinkCandidate = false;
            rightWinkCandidate = false;
        }
        // カウンタ更新
        leftWinkCounter = leftWinkCandidate ? leftWinkCounter + 1 : 0;
        rightWinkCounter = rightWinkCandidate ? rightWinkCounter + 1 : 0;

        // しきい値を超えたらウィンクと判定
        bool isLeftWink = leftWinkCounter >= WinkThresholdFrames;
        bool isRightWink = rightWinkCounter >= WinkThresholdFrames;

        // ウィンク判定が出たらロック状態にする
        if (isLeftWink || isRightWink)
        {
            winkLocked = true;
            leftWinkLocked = isLeftWink;
            rightWinkLocked = isRightWink;
            // 解除中フラグの初期化
            winkReleaseCounter = 0;
            winkJustReleased = false;
        }
        // ウィンクロック中の処理
        if (winkLocked)
        {
            // 両目のOpennessが0.45以上、かつ両目のSquintが0.1未満なら両目を開けていると判定（デフォルト値）
            bool bothOpen =
                rawLeft > BothOpenOpenness &&
                rawRight > BothOpenOpenness &&
                leftSquint < BlinkSquint &&
                rightSquint < BlinkSquint;

            if (bothOpen)
            {
                // 両目を開いている状態で一定フレーム数経過したらウィンクロックを解除
                winkReleaseCounter++;
                if (winkReleaseCounter >= WinkReleaseFrames)
                {
                    winkLocked = false;
                    leftWinkLocked = false;
                    rightWinkLocked = false;
                    // 解除中フラグをオン
                    winkJustReleased = true;
                    winkJustReleasedFrames = 0;
                }
            }
            else
            {
                winkReleaseCounter = 0;
            }

            if (winkLocked)
            {
                // ウィンクの閉じ側を0に固定
                if (leftWinkLocked)
                {
                    // 左目を閉じる
                    leftLid = 0f;

                    // 右目が瞬きしている場合は瞬きとして扱う
                    bool rightBlink = rightSquint > BlinkSquint && rightLid < BlinkLidThreshold;
                    if (rightBlink) rightLid = 0f;
                }
                else if (rightWinkLocked)
                {
                    rightLid = 0f;

                    bool leftBlink = leftSquint > BlinkSquint && leftLid < BlinkLidThreshold;
                    if (leftBlink) leftLid = 0f;
                }
            }
            // 現在のOpennessを前フレームの値として保存
            prevLeftLid = leftLid;
            prevRightLid = rightLid;

            return (Clamp01(leftLid), Clamp01(rightLid));
        }

        // ウィンク解除直後は瞬き候補も無効化して再ロック・誤検出を防ぐ
        if (winkJustReleased)
            blinkCandidate = false;

        blinkCounter = blinkCandidate ? blinkCounter + 1 : 0;

        // 2フレーム以上継続している場合は瞬きとして扱う
        bool isBlink = blinkCounter >= BlinkThresholdFrames;
        // 安定版の場合、左右差が大きいときは両目瞬き状態を解除
        if (!isAlphaModel)
        {
            float diff = MathF.Abs(rawLeft - rawRight);

            if (diff > 0.5f) // 左右差
            {
                blinkReleaseCounter++;
            }
            else
            {
                blinkReleaseCounter = 0;
            }
            if (blinkReleaseCounter >= 3)
            {
                isBlink = false;
            }
        }
        // 両目ともきっちりと閉じる
        if (isBlink)
        {
            leftLid = 0f;
            rightLid = 0f;
        }

        prevLeftLid = leftLid;
        prevRightLid = rightLid;

        // ウィンク解除直後は再ロックされないよう候補判定を数フレーム無効化（既定で2）
        if (winkJustReleased)
        {
            winkJustReleasedFrames++;
            // Opennessの跳ね防止のために、ウィンク解除直後は前フレームの値を返す
            leftLid = prevLeftLid;
            rightLid = prevRightLid;

            if (winkJustReleasedFrames >= WinkJustReleasedIgnoreFrames)
                winkJustReleased = false;
        }
        // Squintが強いほどOpennessを下げる
        if (!winkLocked && !leftWinkCandidate && !rightWinkCandidate && !isBlink)
        {
            leftLid *= (1f - leftSquint * SquintStrength);
            rightLid *= (1f - rightSquint * SquintStrength);
        }
        // ウィンクロックされていないときは、左右の目のOpennessを同期させる
        if (UseLidSync && !winkLocked && !leftWinkCandidate && !rightWinkCandidate)
        {
            float maxLid = MathF.Max(leftLid, rightLid);

            // 両目が開いているときだけ同期する
            if (rawLeft >= BothOpenOpenness && rawRight >= BothOpenOpenness)
            {
                leftLid = maxLid;
                rightLid = maxLid;
            }
        }
        // EyeSquint時のEyeY中央寄せ補正
        if (UseLidEyeCenter)
        {
            float leftEyeY = UnifiedTracking.Data.Eye.Left.Gaze.y;
            float rightEyeY = UnifiedTracking.Data.Eye.Right.Gaze.y;
            float threshold = LidEyeCenterThreshold;
            float strength = LidEyeCenterStrength; //多分強くしても0.0で頭打ちする

            // 左目
            if (rawLeft < threshold)
            {
                // Lidがthresholdを下回るほどtが増える
                float t = 1.0f - (rawLeft / threshold);

                // 補正強度
                float k = Clamp01(t * strength);

                // EyeYを中央(0.0)へ補間
                leftEyeY = Lerp(leftEyeY, 0.0f, k);
            }

            // 右目
            if (rawRight < threshold)
            {
                float t = 1.0f - (rawRight / threshold);
                float k = Clamp01(t * strength);

                rightEyeY = Lerp(rightEyeY, 0.0f, k);
            }

            UnifiedTracking.Data.Eye.Left.Gaze.y = leftEyeY;
            UnifiedTracking.Data.Eye.Right.Gaze.y = rightEyeY;
        }

        return (Clamp01(leftLid), Clamp01(rightLid));

    }

    // 出力
    private void CorrectWideEyes(float[] raw, float[] corrected)
    {
        // EyeWide 補正
        float rawWideL = raw[(int)UnifiedExpressions.EyeWideLeft];
        float rawWideR = raw[(int)UnifiedExpressions.EyeWideRight];

        float wideL = rawWideL;
        float wideR = rawWideR;

        if (!UseEyeWide)
        {
            wideL = 0f;
            wideR = 0f;
        }
        else if (UseEyeWideCorrection)
        {
            // 最小値を採用して左右のEyeWideを揃える
            float minWide = MathF.Min(rawWideL, rawWideR);
            float limitedWide = MathF.Min(minWide, EyeWideLimit);
            wideL = limitedWide;
            wideR = limitedWide;
        }

        // 出力に反映
        corrected[(int)UnifiedExpressions.EyeWideLeft] = wideL;
        corrected[(int)UnifiedExpressions.EyeWideRight] = wideR;
    }
    private void ApplyShapes(float[] corrected)
    {
        for (int i = 0; i < corrected.Length; i++)
            UnifiedTracking.Data.Shapes[i].Weight = corrected[i];
    }

    private void ApplyEyes(float left, float right)
    {
        UnifiedTracking.Data.Eye.Left.Openness = left;
        UnifiedTracking.Data.Eye.Right.Openness = right;
    }

    // 寄り目の修正
    private void CorrectCrossEye()
    {
        if (!PreventCrossEye)
            return;

        float leftEyeX = UnifiedTracking.Data.Eye.Left.Gaze.x;
        float rightEyeX = UnifiedTracking.Data.Eye.Right.Gaze.x;

        float convergence = leftEyeX - rightEyeX;
        float divergence = rightEyeX - leftEyeX;

        float correction = 1.0f - CrossEyeStrength;


        if (convergence > 0.3f)
        {
            leftEyeX *= correction;
            rightEyeX *= correction;
        }
        if (divergence > 0.6f)
        {
            // 外向きすぎるので内側へ戻す
            leftEyeX *= correction;
            rightEyeX *= correction;
        }

        // 視線の左右同期補正
        if (UseCrossEyeSync)
        {
            // 両目が開いている時だけ同期する（LidSync と同じ感じ）
            if (UnifiedTracking.Data.Eye.Left.Openness > CrossEyeSyncOpenThreshold &&
                UnifiedTracking.Data.Eye.Right.Openness > CrossEyeSyncOpenThreshold)
            {
                // 左右差が大きい時だけ同期する
                float diff = MathF.Abs(leftEyeX - rightEyeX);

                if (diff > CrossEyeSyncDiffThreshold)
                {
                    float avg = (leftEyeX + rightEyeX) * 0.5f;
                    leftEyeX = avg;
                    rightEyeX = avg;
                }

                // 両目が左を向いているときの倍率補正
                if (leftEyeX < 0f && rightEyeX < 0f)
                {
                    leftEyeX *= EyeLeftScale;
                    rightEyeX *= EyeRightScale;
                }
                // 両目が右を向いているときの倍率補正
                else if (leftEyeX > 0f && rightEyeX > 0f)
                {
                    leftEyeX *= EyeLeftScale;
                    rightEyeX *= EyeRightScale;
                }
            }
        }

        UnifiedTracking.Data.Eye.Left.Gaze.x = leftEyeX;
        UnifiedTracking.Data.Eye.Right.Gaze.x = rightEyeX;
    }
    public override void Update()
    {
        float[] raw = BabbleOsc.ExpressionBuffer;

        float[] corrected = CorrectMouth(raw);

        float leftLid = BabbleOsc.LeftEyeOpenness;
        float rightLid = BabbleOsc.RightEyeOpenness;

        float leftSquint = raw[(int)UnifiedExpressions.EyeSquintLeft];
        float rightSquint = raw[(int)UnifiedExpressions.EyeSquintRight];

        CorrectWideEyes(raw, corrected);

        (leftLid, rightLid) = CorrectEyes(leftLid, rightLid, leftSquint, rightSquint);

        ApplyShapes(corrected);
        ApplyEyes(leftLid, rightLid);

        // 視線リミッター
        float lx = UnifiedTracking.Data.Eye.Left.Gaze.x;
        float rx = UnifiedTracking.Data.Eye.Right.Gaze.x;
        float ly = UnifiedTracking.Data.Eye.Left.Gaze.y;
        float ry = UnifiedTracking.Data.Eye.Right.Gaze.y;
        lx = MathF.Min(MathF.Max(lx, EyeInnerLimit), EyeOuterLimit);
        rx = MathF.Min(MathF.Max(rx, EyeInnerLimit), EyeOuterLimit);
        ly = MathF.Min(MathF.Max(ly, EyeDownLimit), EyeUpLimit);
        ry = MathF.Min(MathF.Max(ry, EyeDownLimit), EyeUpLimit);
        UnifiedTracking.Data.Eye.Left.Gaze.x = lx;
        UnifiedTracking.Data.Eye.Right.Gaze.x = rx;
        UnifiedTracking.Data.Eye.Left.Gaze.y = ly;
        UnifiedTracking.Data.Eye.Right.Gaze.y = ry;

        CorrectCrossEye();
    }
}
