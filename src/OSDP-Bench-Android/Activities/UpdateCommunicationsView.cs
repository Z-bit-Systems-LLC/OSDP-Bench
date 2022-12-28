using Android.Views;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using MvvmCross.Platforms.Android.Views;
using OSDPBench.Core.ViewModels;

namespace OSDPBench.UI.Android.Activities;

[MvxActivityPresentation]
[Activity(Theme = "@style/Theme.AppCompat.Light",
    WindowSoftInputMode = SoftInput.AdjustPan)]
public class UpdateCommunicationsView : MvxActivity<UpdateCommunicationViewModel>
{
    protected override void OnCreate(Bundle bundle)
    {
        base.OnCreate(bundle);

        SetContentView(Resource.Layout.UpdateCommunicationsView);
    }
}