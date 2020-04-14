using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MvvmCross.Base;
using MvvmCross.ViewModels;
using MvvmCross.Views;

namespace OSDPBench.Core.Tests
{
    public class MockDispatcher : MvxMainThreadAsyncDispatcher, IMvxViewDispatcher
    {
        public readonly List<MvxViewModelRequest> Requests = new List<MvxViewModelRequest>();
        public readonly List<MvxPresentationHint> Hints = new List<MvxPresentationHint>();

        public override bool RequestMainThreadAction(Action action, bool maskExceptions = true)
        {
            action();
            return true;
        }

        public override bool IsOnMainThread { get; }
        public async Task<bool> ShowViewModel(MvxViewModelRequest request)
        {
            Requests.Add(request);
            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> ChangePresentation(MvxPresentationHint hint)
        {
            Hints.Add(hint);
            await Task.CompletedTask;
            return true;
        }
    }
}