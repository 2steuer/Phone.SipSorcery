using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phone.SipSorcery.CallMachine.Core.CallHandling
{
    public enum CallHandlerState
    {
        Waiting,
        Active,
        Failed,
        Finished
    }
}
