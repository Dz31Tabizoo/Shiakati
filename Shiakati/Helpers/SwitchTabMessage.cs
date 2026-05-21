using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Helpers
{
    public class SwitchTabMessage
    {
        public string TabName { get; }
        public SwitchTabMessage(string tabName) => TabName = tabName;
    }
}
