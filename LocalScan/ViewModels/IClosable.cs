using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalScan.ViewModels
{
    public interface IClosable
    {
        void OnClose();

        string Header { get; }
    }
}
