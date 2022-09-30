using iRacingSimulator.Drivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace iRCC.iRacingSimulator.Events
{
    public class StartFinishEvent
    {
        public StartFinishEvent()
        {
            this.Time = DateTime.UtcNow;
        }

        public Driver Driver { get; set; }
        public DateTime Time { get; set; }
    }
}
