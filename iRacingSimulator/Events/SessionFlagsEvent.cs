using iRacingSimulator.Drivers;
using iRacingSimulator;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using iRacingSdkWrapper.Bitfields;
using iRacingSdkWrapper;

namespace iRCC.iRacingSimulator.Events
{
    public class SessionFlagsEvent
    {
        public SessionFlagsEvent()
        {
            {
                this.Time = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// The time (UTC) of the event.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// The iRacing session time of the event.
        /// </summary>
        public double SessionTime { get; set; }

        /// <summary>
        /// The lap of the event.
        /// </summary>
        public int Lap { get; set; }

        /// <summary>
        /// The current race flag status.
        /// </summary>
        public SessionFlags Flag { get; set; }

        /// <summary>
        /// Speed of the car.
        /// </summary>
        public double SpeedMph { get; set; }
    }
}
