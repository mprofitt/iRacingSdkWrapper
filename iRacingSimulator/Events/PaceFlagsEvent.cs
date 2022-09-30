using iRacingSdkWrapper;
using iRacingSimulator.Drivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRCC.iRacingSimulator.Events
{
    public class PaceFlagsEvent
    {
        public PaceFlagsEvent()
        {
            this.Time = DateTime.UtcNow;
        }

        public PaceFlags Type { get; set; }
        public Driver Driver { get; set; }
        public DateTime Time { get; set; }   
        public int SessionTick { get; set; }
        public double SessionTime { get; set; }
    }
}
