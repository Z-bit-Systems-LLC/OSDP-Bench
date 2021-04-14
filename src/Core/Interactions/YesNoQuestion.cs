using System;

namespace OSDPBench.Core.Interactions
{
    public class YesNoQuestion
    {
        public YesNoQuestion(string question, Action<bool> yesNoCallback)
        {
            Question = question;
            YesNoCallback = yesNoCallback;
        }

        public Action<bool> YesNoCallback { get; }

        public string Question { get; }
    }
}
