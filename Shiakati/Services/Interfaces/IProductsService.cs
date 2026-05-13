using Shiakati.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Services.Interfaces
{
    public interface IProductsService
    {
        Task<List<ProductModel>> GetProductsAsync();

    }
}
