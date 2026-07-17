using Shiakati.Models;

namespace Shiakati.Services.Interfaces.PrintServices
{
    public interface IPrintService
    {
          void PrintReceipt(ReceipModel receipt, string configuredPrinterName = "");
    }
}
