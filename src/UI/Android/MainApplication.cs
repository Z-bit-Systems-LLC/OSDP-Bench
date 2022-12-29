using Android.Runtime;
using MvvmCross.Platforms.Android.Views;
using OSDPBench.Core;

namespace OSDPBench.UI.Android;

[Application]
public class MainApplication : MvxAndroidApplication<Setup, App>
{
    public MainApplication(IntPtr javaReference, JniHandleOwnership transfer) 
        : base(javaReference, transfer)
    {
    }
}