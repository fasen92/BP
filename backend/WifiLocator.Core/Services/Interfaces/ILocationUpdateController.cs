using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WifiLocator.Core.Services.Interfaces
{
    public interface ILocationUpdateController
    {
        void Pause();
        void Resume();
    }
}
