using Shiakati.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Services.Interfaces.APIServices
{
    public interface IDashBordService
    {
        Task<DashboardStatsResponse> GetDashBordDataAsync(DashboardFilterRequest filter);

        Task<bool> AcknowledgeAlertAsync(int variantId);
    }
}
