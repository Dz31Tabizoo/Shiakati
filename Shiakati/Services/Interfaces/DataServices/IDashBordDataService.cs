using Shiakati.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Services.Interfaces.DataServices
{
    public interface IDashBordDataService
    {       

        Task<DashboardStatsResponse> GetDashboardDataAsync(DashboardFilterRequest filter);
        Task<bool> AcknowledgeAlertAsync(int variantId);

        event Action? DashBordDataChanged;
    }
}
