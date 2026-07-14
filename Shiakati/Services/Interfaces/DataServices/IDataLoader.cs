using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Services.Interfaces.DataServices
{
    public interface IDataLoader
    {
        Task LoadAllEssentialDataAsync();
    }
}
