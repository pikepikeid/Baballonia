using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using VRCFaceTracking.Core.OSC;
using VRCFaceTracking.Core.Params.Expressions;

namespace VRCFaceTracking.Baballonia;

public class BabbleOsc
{

    private Socket? _receiver;

    private bool _loop = true;

    private readonly Thread? _thread;

    private readonly int _resolvedPort;

    private readonly string? _resolvedHost;

    private const string DefaultHost = "127.0.0.1";

    private const int DefaultPort = 8888;

    private const int TimeoutMs = 10000;

    public static float[] ExpressionBuffer = new float[UnifiedTracking.Data.Shapes.Length];

    // 生のOpenness（BabbleVRCが使用する）
    public static float LeftEyeOpenness = 1.0f;
    public static float RightEyeOpenness = 1.0f;

    public BabbleOsc(ILogger iLogger, string host, int? port)
    {
        if (_receiver != null)
        {
            iLogger.LogError("BabbleEyeOSC connection already exists.");
            return;
        }
        _resolvedHost = host ?? DefaultHost;
        _resolvedPort = port ?? TimeoutMs;

        iLogger.LogInformation($"Started BabbleEyeOSC with Host: {_resolvedHost} and Port {_resolvedPort}");
        ConfigureReceiver();
        _loop = true;
        _thread = new Thread(ListenLoop);
        _thread.Start();
    }

    private void ConfigureReceiver()
    {
        IPAddress address = IPAddress.Parse(_resolvedHost!);
        IPEndPoint localEp = new IPEndPoint(address, _resolvedPort);
        _receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _receiver.Bind(localEp);
        _receiver.ReceiveTimeout = TimeoutMs;
    }

    private void ListenLoop()
    {
        byte[] array = new byte[4096];
        while (_loop)
        {
            try
            {
                if (_receiver!.IsBound)
                {
                    int len = _receiver.Receive(array);
                    int messageIndex = 0;
                    OscMessage oscMessage;
                    try
                    {
                        oscMessage = new OscMessage(array, len, ref messageIndex);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    if (oscMessage.Value is float value)
                    {
                        switch (oscMessage.Address)
                        {
                            // 使わないのもあるけど一旦全部入れておく
                            /* mouth params */
                            case "/cheekPuffLeft":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.CheekPuffLeft] = value;
                                break;
                            case "/cheekPuffRight":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.CheekPuffRight] = value;
                                break;
                            case "/cheekSuckLeft":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.CheekSuckLeft] = value;
                                break;
                            case "/cheekSuckRight":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.CheekSuckRight] = value;
                                break;
                            case "/jawOpen":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.JawOpen] = value;
                                break;
                            case "/jawForward":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.JawForward]   = value;
                                break;
                            case "/jawLeft":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.JawLeft] = value;
                                break;
                            case "/jawRight":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.JawRight] = value;
                                break;
                            case "/noseSneerLeft":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.NoseSneerLeft] = value;
                                break;
                            case "/noseSneerRight":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.NoseSneerRight] = value;
                                break;
                            case "/mouthFunnel":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.LipFunnelLowerLeft] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.LipFunnelLowerRight] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.LipFunnelUpperLeft] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.LipFunnelUpperRight] = value;
                                break;
                            case "/mouthPucker":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.LipPuckerLowerLeft] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.LipPuckerLowerRight] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.LipPuckerUpperLeft] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.LipPuckerUpperRight] = value;
                                break;
                            case "/mouthLeft":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthUpperLeft] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthLowerLeft] = value;
                                break;
                            case "/mouthRight":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthUpperRight] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthLowerRight] = value;
                                break;
                            case "/mouthRollUpper":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.LipSuckUpperLeft] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.LipSuckUpperRight] = value;
                                break;
                            case "/mouthRollLower":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.LipSuckLowerLeft] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.LipSuckLowerRight] = value;
                                break;
                            case "/mouthShrugUpper":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthRaiserUpper] = value;
                                break;
                            case "/mouthShrugLower":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthRaiserLower] = value;
                                break;
                            case "/mouthClose":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthClosed] = value;
                                break;
                            case "/mouthSmileLeft":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthCornerPullLeft] = value;
                                break;
                            case "/mouthSmileRight":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthCornerPullRight] = value;
                                break;
                            case "/mouthFrownLeft":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthFrownLeft] = value;
                                break;
                            case "/mouthFrownRight":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthFrownRight] = value;
                                break;
                            case "/mouthDimpleLeft":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthDimpleLeft] = value;
                                break;
                            case "/mouthDimpleRight":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthDimpleRight] = value;
                                break;
                            case "/mouthUpperUpLeft":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthUpperUpLeft] = value;
                                break;
                            case "/mouthUpperUpRight":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthUpperUpRight] = value;
                                break;
                            case "/mouthLowerDownLeft":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthLowerDownLeft] = value;
                                break;
                            case "/mouthLowerDownRight":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthLowerDownRight] = value;
                                break;
                            case "/mouthPressLeft":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthPressLeft] = value;
                                break;
                            case "/mouthPressRight":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthPressRight] = value;
                                break;
                            case "/mouthStretchLeft":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthStretchLeft] = value;
                                break;
                            case "/mouthStretchRight":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.MouthStretchRight] = value;
                                break;
                            case "/tongueOut":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.TongueOut] = value;
                                break;
                            case "/tongueUp":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.TongueUp] = value;
                                break;
                            case "/tongueDown":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.TongueDown] = value;
                                break;
                            case "/tongueLeft":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.TongueLeft] = value;
                                break;
                            case "/tongueRight":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.TongueRight] = value;
                                break;
                            case "/tongueRoll":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.TongueRoll] = value;
                                break;
                            case "/tongueBendDown":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.TongueBendDown] = value;
                                break;
                            case "/tongueCurlUp":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.TongueCurlUp] = value;
                                break;
                            case "/tongueSquish":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.TongueSquish] = value;
                                break;
                            case "/tongueFlat":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.TongueFlat] = value;
                                break;
                            case "/tongueTwistLeft":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.TongueTwistLeft] = value;
                                break;
                            case "/tongueTwistRight":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.TongueTwistRight] = value;
                                break;


                            /* eye params */
                            case "/LeftEyeX":
                            case "/leftEyeX":
                                UnifiedTracking.Data.Eye.Left.Gaze.x = value;
                                break;
                            case "/LeftEyeY":
                            case "/leftEyeY":
                                UnifiedTracking.Data.Eye.Left.Gaze.y = value;
                                break;
                            case "/LeftEyeLid":
                            case "/leftEyeLid":
                                LeftEyeOpenness = value;
                                break;
                            case "/LeftEyeWiden":
                            case "/leftEyeWiden":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.EyeWideLeft] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.BrowOuterUpLeft] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.BrowInnerUpLeft] = value;
                                break;
                            case "/LeftEyeSquint":
                            case "/leftEyeSquint":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.EyeSquintLeft] = value;
                                break;
                            case "/LeftEyeBrow":
                            case "/leftEyeBrow":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.BrowLowererLeft] = value;
                                break;
                            case "/RightEyeX":
                            case "/rightEyeX":
                                UnifiedTracking.Data.Eye.Right.Gaze.x = value;
                                break;
                            case "/RightEyeY":
                            case "/rightEyeY":
                                UnifiedTracking.Data.Eye.Right.Gaze.y = value;
                                break;
                            case "/RightEyeLid":
                            case "/rightEyeLid":
                                RightEyeOpenness = value;
                                break;
                            case "/RightEyeWiden":
                            case "/rightEyeWiden":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.EyeWideRight] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.BrowOuterUpRight] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.BrowInnerUpRight] = value;
                                break;
                            case "/RightEyeSquint":
                            case "/rightEyeSquint":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.EyeSquintRight] = value;
                                break;
                            case "/RightEyeBrow":
                            case "/rightEyeBrow":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.BrowLowererRight] = value;
                                break;

                            /* combined eye params (single value driving both eyes) */
                            case "/CombinedEyeWiden":
                            case "/combinedEyeWiden":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.EyeWideLeft] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.BrowOuterUpLeft] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.BrowInnerUpLeft] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.EyeWideRight] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.BrowOuterUpRight] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.BrowInnerUpRight] = value;

                                break;
                            case "/CombinedEyeSquint":
                            case "/combinedEyeSquint":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.EyeSquintLeft] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.EyeSquintRight] = value;
                                break;
                            case "/CombinedEyeBrow":
                            case "/combinedEyeBrow":
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.BrowLowererLeft] = value;
                                BabbleOsc.ExpressionBuffer[(int)UnifiedExpressions.BrowLowererRight] = value;
                                break;
                        }
                    }
                }
                else
                {
                    _receiver.Close();
                    _receiver.Dispose();
                    ConfigureReceiver();
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }
    }

    public void Teardown()
    {
        _loop = false;
        _receiver!.Close();
        _receiver.Dispose();
        _thread!.Join();
    }
}
