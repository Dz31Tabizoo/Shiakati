using Shiakati.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Services.Interfaces.DataServices
{

    public interface ICatalogDataService
    {
        ObservableCollection<BrandsModel> Brands { get; }
        ObservableCollection<CategoryModel> Categories { get; }

        Task LoadCatalogAsync(bool forceRefresh = false);
        Task<CategoryModel> AddCategoryAsync(CategoryModel category);
        // I can Add AddBrandAsync later 

        event Action? CatalogDataChanged;
    }
}
    

