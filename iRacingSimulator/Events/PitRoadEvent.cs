using iRacingSdkWrapper;
using iRacingSimulator.Drivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRCC.iRacingSimulator.Events
{
    public class PitRoadEvent
    {
        public PitRoadEvent()
        {
            this.Time = DateTime.UtcNow;
        }
        public PitAction Type { get; set; }
        public Driver? Driver { get; set; }
        public DateTime Time { get; set; }
    }
}
