using Android.Graphics;
using MvvmCross.Binding;
using MvvmCross.Binding.Bindings.Target;

namespace OSDPBench.UI.Android.Bindings;

public class TintColorMvxTargetBinding : MvxTargetBinding<ImageView, string>
{
    public TintColorMvxTargetBinding(ImageView target)
        : base(target)
    {
    }

    public override MvxBindingMode DefaultMode => MvxBindingMode.OneWay;

    protected override void SetValue(string value)
    {
        Target.SetColorFilter(Color.ParseColor(value));
    }
}