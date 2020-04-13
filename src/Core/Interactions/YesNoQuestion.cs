using System;

namespace OSDPBench.Core.Interactions
{
    public class YesNoQuestion
    {
        public Action<bool> YesNoCallback { get; set; }
        public string Question { get; set; }
    }
}
