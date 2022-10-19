using MvvmCross.IoC;
using MvvmCross.ViewModels;

namespace OSDPBench.Core
{
    public class App : MvxApplication
    {
        public override void Initialize()
        {
            CreatableTypes()
                .EndingWith("Service")
                .AsInterfaces()
                .RegisterAsLazySingleton();

            RegisterAppStart<ViewModels.RootViewModel>();
        }
    }
}
