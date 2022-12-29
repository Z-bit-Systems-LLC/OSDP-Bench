using System;
using System.Threading.Tasks;

namespace OSDPBench.Core.Interactions
{
    public class YesNoQuestion
    {
        public YesNoQuestion(string question, Func<bool, Task> yesNoCallback)
        {
            Question = question;
            YesNoCallback = yesNoCallback;
        }

        public Func<bool, Task> YesNoCallback { get; }

        public string Question { get; }
    }
}
