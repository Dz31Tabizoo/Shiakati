using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Messages
{
    public class StockAlertCountMessage
    {
        public int Count { get; }

        public StockAlertCountMessage(int count)
        {
            Count = count;
        }

    }
}
