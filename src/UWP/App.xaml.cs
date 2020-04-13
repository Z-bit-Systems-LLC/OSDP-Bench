﻿using MvvmCross.Core;
using MvxApplication = MvvmCross.Platforms.Uap.Views.MvxApplication;

namespace OSDPBenchUWP
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : MvxApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        protected override void RegisterSetup()
        {
            this.RegisterSetupType<Setup>();
        }
    }
}