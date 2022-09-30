using iRacingSdkWrapper;
using iRacingSimulator.Drivers;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRCC.iRacingSimulator.Events
{
    public class PositionChangeEvent
    {
        public PositionChangeEvent()
        {
            this.Time = DateTime.UtcNow;
        }

        public int Position { get; set; }
        public int PositionPrev { get; set; }
        public Driver? Driver { get; set; }
        public DateTime Time { get; private set; }
    }
}


