using System.IO;
using System.Windows;
using System.Windows.Media.Effects;

namespace OSDPBench.Windows.Views.Pages;

class InvertEffect : ShaderEffect
{
    private const string KshaderAsBase64 =
        @"AAP///7/HwBDVEFCHAAAAE8AAAAAA///AQAAABwAAAAAAQAASAAAADAAAAADAAAAAQACADgAAAAA
AAAAaW5wdXQAq6sEAAwAAQABAAEAAAAAAAAAcHNfM18wAE1pY3Jvc29mdCAoUikgSExTTCBTaGFk
ZXIgQ29tcGlsZXIgMTAuMQCrUQAABQAAD6AAAIA/AAAAAAAAAAAAAAAAHwAAAgUAAIAAAAOQHwAA
AgAAAJAACA+gQgAAAwAAD4AAAOSQAAjkoAIAAAMAAAeAAADkgQAAAKAFAAADAAgHgAAA/4AAAOSA
AQAAAgAICIAAAP+A//8AAA==";

    private static readonly PixelShader Shader;

    static InvertEffect()
    {
        Shader = new PixelShader();
        Shader.SetStreamSource(new MemoryStream(Convert.FromBase64String(KshaderAsBase64)));
    }

    public InvertEffect()
    {
        PixelShader = Shader;
        UpdateShaderValue(InputProperty);
    }

    public static readonly DependencyProperty InputProperty =
        RegisterPixelShaderSamplerProperty("Input", typeof(InvertEffect), 0);

}