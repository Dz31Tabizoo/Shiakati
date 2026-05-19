using Shiakati.Models;

namespace Shiakati.Services.Interfaces
{
    public interface ICatalogService
    {
        Task<(List<BrandsModel>Brands,List<CategoryModel> Categories) > GetInitialGatalogDataAsync();
        Task<CategoryModel> AddCategoryModelAsync(CategoryModel category);
    }
}
