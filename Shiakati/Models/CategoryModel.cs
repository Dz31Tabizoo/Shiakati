using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public partial class CategoryModel : ObservableObject
    {
        [ObservableProperty]
        private int _categoryID;
        [ObservableProperty]
        private string _categoryName = string.Empty;
        [ObservableProperty]
        [AllowNull]
        private string _iconPath;
    }
    
}
