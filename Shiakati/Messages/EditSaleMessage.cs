using Shiakati.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Messages
{
    public class EditSaleMessage
    {
        public SaleModel Sale { get; }
        public IEnumerable<SaleItemModel> Items { get; }

        public EditSaleMessage(SaleModel sale, IEnumerable<SaleItemModel> items)
        {
            Sale = sale;
            Items = items;
        }
    }

    
}
