using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Shiakati.Models;

namespace Shiakati.Services.Interfaces
{
    public interface IBarCodePrintService
    {
         
        void PrintBarCode(BarecodeLabelData data, string printerName="",int copies = 1);

        


    }
}
