using System.Reflection;
using Microsoft.Extensions.Logging;
using VRCFaceTracking.Core.Params.Expressions;

namespace VRCFaceTracking.Baballonia;

public class BabbleVrc : ExtTrackingModule
{
    private BabbleOsc babbleOSC;
    private Config config;
    private bool needsEye;
    private bool needsExpression;
    private BabbleExtraConfig extra;

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

    // 設定値（調整可能）
    const int WinkThresholdFrames = 3;
    const int BlinkThresholdFrames = 2;
    const int WinkReleaseFrames = 3;

    int winkReleaseCounter = 0;

    bool winkJustReleased = false;
    int winkJustReleasedFrames = 0;
    const int WinkJustReleasedIgnoreFrames = 2; // 2フレームだけ候補判定を無効化


    // We need to call GetBabbleConfig ahead of Initialize
    public override (bool SupportsEye, bool SupportsExpression) Supported => (true, true);

    public override (bool eyeSuccess, bool expressionSuccess) Initialize(bool eyeAvailable, bool expressionAvailable)
    {
        config = BabbleConfig.GetBabbleConfig();
        extra = BabbleExtraConfig.Load();
        babbleOSC = new BabbleOsc(Logger, config.Host, config.Port);

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
            Name = "Project Babble Module（Modified.1）",
            StaticImages = list
        };

        return (needsEye, needsExpression);
    }

    public override void Teardown()
    {
        babbleOSC.Teardown();
    }

    // 口の補正
    private float[] CorrectMouth(float[] raw)
    {
        float[] corrected = new float[raw.Length];
        Array.Copy(raw, corrected, raw.Length);
        // 左右対称化するパラメータ
        var symmetricPairs = new (UnifiedExpressions L, UnifiedExpressions R)[]
        {
            (UnifiedExpressions.NoseSneerLeft, UnifiedExpressions.NoseSneerRight),
            (UnifiedExpressions.MouthCornerPullLeft, UnifiedExpressions.MouthCornerPullRight),
            (UnifiedExpressions.MouthFrownLeft, UnifiedExpressions.MouthFrownRight),
            (UnifiedExpressions.MouthDimpleLeft, UnifiedExpressions.MouthDimpleRight),
            (UnifiedExpressions.MouthUpperUpLeft, UnifiedExpressions.MouthUpperUpRight),
            (UnifiedExpressions.MouthLowerDownLeft, UnifiedExpressions.MouthLowerDownRight),
            (UnifiedExpressions.MouthPressLeft, UnifiedExpressions.MouthPressRight),
            (UnifiedExpressions.MouthStretchLeft, UnifiedExpressions.MouthStretchRight),
        };

        float GetSymmetric(UnifiedExpressions l, UnifiedExpressions r)
            => MathF.Max(raw[(int)l], raw[(int)r]);

        // よく使うパラメータ
        float jawOpen = raw[(int)UnifiedExpressions.JawOpen];
        float pucker = Math.Max(
            Math.Max(raw[(int)UnifiedExpressions.LipPuckerLowerLeft], raw[(int)UnifiedExpressions.LipPuckerLowerRight]),
            Math.Max(raw[(int)UnifiedExpressions.LipPuckerUpperLeft], raw[(int)UnifiedExpressions.LipPuckerUpperRight])
        );
        float funnel = Math.Max(
            Math.Max(raw[(int)UnifiedExpressions.LipFunnelLowerLeft], raw[(int)UnifiedExpressions.LipFunnelLowerRight]),
            Math.Max(raw[(int)UnifiedExpressions.LipFunnelUpperLeft], raw[(int)UnifiedExpressions.LipFunnelUpperRight])
        );
        float stretch = GetSymmetric(UnifiedExpressions.MouthStretchLeft, UnifiedExpressions.MouthStretchRight);
        float lowerDown = GetSymmetric(UnifiedExpressions.MouthLowerDownLeft, UnifiedExpressions.MouthLowerDownRight);
        float upperUp = GetSymmetric(UnifiedExpressions.MouthUpperUpLeft, UnifiedExpressions.MouthUpperUpRight);
        float smile = GetSymmetric(UnifiedExpressions.MouthCornerPullLeft, UnifiedExpressions.MouthCornerPullRight);
        float raiserLower = raw[(int)UnifiedExpressions.MouthRaiserLower];
        
        foreach (UnifiedExpressions expr in Enum.GetValues(typeof(UnifiedExpressions)))
        {
            float value = raw[(int)expr];

            foreach (var (L, R) in symmetricPairs)
            {
                if (expr == L || expr == R)
                {
                    value = GetSymmetric(L, R);
                    break;
                }
            }

            value = CorrectMouthExpression(expr, value, jawOpen, pucker, funnel, stretch, lowerDown, upperUp, smile, raiserLower);
            corrected[(int)expr] = value;
        }

        return corrected;
    }
    
    private float CorrectMouthExpression(
        UnifiedExpressions expr, float value,
        float jawOpen, float pucker, float funnel,
        float stretch, float lowerDown, float upperUp,
        float smile, float raiserLower)
    {
        switch (expr)
        {
            // 口を開けている時や笑っているときは口角下げを抑制したり無効にする
            case UnifiedExpressions.MouthFrownLeft:
            case UnifiedExpressions.MouthFrownRight:
                value = Math.Min(value + raiserLower, 1.0f);
                if (jawOpen >= 0.3f)
                    value = Math.Min(value * 1.5f, 1.0f);
                if (upperUp >= 0.8f && jawOpen < 0.3f)
                    value = 0f;
                break;

            // 口をすぼめるときはJawOpenを抑制する
            case UnifiedExpressions.JawOpen:
                if (pucker >= 0.6f && funnel <= 0.25f)
                    value *= 0.5f;
                else
                    value = Math.Min(value, extra.JawOpenMax); // 最大値を既定値0.85に制限
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

            // 口を開けている時に悲しい表情が出やすいのを抑制する
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
        float Clamp01(float v) => MathF.Min(MathF.Max(v, 0f), 1f);
        if (!extra.UseWinkLock) { return (Clamp01(leftLid), Clamp01(rightLid)); }

        // 補正前の生の値を取得（判定用）
        float rawLeft = BabbleOsc.LeftEyeOpenness;
        float rawRight = BabbleOsc.RightEyeOpenness;
        // 左右のSquintが0.99以上なら両目を閉じているとみなす
        bool bothClosed = leftSquint >= 0.99f && rightSquint >= 0.99f;
        // 両目を閉じてない場合にウィンク候補を判定
        bool leftWinkCandidate =
            leftSquint >= 0.85f &&
            rightSquint <= 0.5f &&
            !bothClosed;

        bool rightWinkCandidate =
            rightSquint >= 0.85f &&
            leftSquint <= 0.5f &&
            !bothClosed;

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
            // 両目のOpennessが0.45以上、かつ両目のSquintが0.1未満なら両目を開けていると判定
            bool bothOpen =
                rawLeft > 0.45f &&
                rawRight > 0.45f &&
                leftSquint < 0.1f &&
                rightSquint < 0.1f;

            if (bothOpen)
            {
                // 両目を開いている状態で一定フレーム数（既定3）経過したらウィンクロックを解除
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
                    bool rightBlink = rightSquint > 0.2f && rightLid < 0.5f;
                    if (rightBlink) rightLid = 0f;
                }
                else if (rightWinkLocked)
                {
                    rightLid = 0f;

                    bool leftBlink = leftSquint > 0.2f && leftLid < 0.5f;
                    if (leftBlink) leftLid = 0f;
                }
            }
            // 現在のOpennessを前フレームの値として保存
            prevLeftLid = leftLid;
            prevRightLid = rightLid;

            return (Clamp01(leftLid), Clamp01(rightLid));
        }

        // ウィンクロックされていないときの瞬き判定
        bool blinkCandidate =
            leftSquint > 0.2f &&
            rightSquint > 0.2f &&
            leftLid < 0.5f &&
            rightLid < 0.5f;

        // ウィンク解除直後は瞬き候補も無効化して再ロック・誤検出を防ぐ
        if (winkJustReleased)
            blinkCandidate = false;

        blinkCounter = blinkCandidate ? blinkCounter + 1 : 0;

        // 2フレーム以上継続している場合は瞬きとして扱う
        bool isBlink = blinkCounter >= BlinkThresholdFrames;
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

        return (Clamp01(leftLid), Clamp01(rightLid));

    }

    // 出力
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
        if (!extra.PreventCrossEye)
            return;

        float leftEyeX = UnifiedTracking.Data.Eye.Left.Gaze.x;
        float rightEyeX = UnifiedTracking.Data.Eye.Right.Gaze.x;

        float convergence = leftEyeX - rightEyeX;
        float divergence = rightEyeX - leftEyeX;

        float correction = extra.CrossEyeStrength;


        if (convergence > 0.8f)
        {
            leftEyeX *= correction;
            rightEyeX *= correction;
        }
        if (divergence > 0.8f)
        {
            // 外向きすぎるので内側へ戻す
            leftEyeX *= correction;
            rightEyeX *= correction;
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

        (leftLid, rightLid) = CorrectEyes(leftLid, rightLid, leftSquint, rightSquint);

        ApplyShapes(corrected);
        ApplyEyes(leftLid, rightLid);

        CorrectCrossEye();
    }
}
