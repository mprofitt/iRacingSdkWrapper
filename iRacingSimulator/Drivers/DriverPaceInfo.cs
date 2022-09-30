using iRacingSdkWrapper;
using iRacingSimulator.Drivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRCC.iRacingSimulator.Drivers
{
    public class DriverPaceInfo
    {
        public DriverPaceInfo(Driver driver)
        {
            Driver = driver;
        }
        private Driver Driver;
        public int PaceLine { get; private set; }   
        public int PaceRow { get; private set; }  
        public PaceMode PaceMode { get; private set; }   
        public PaceFlags PaceFlag { get; private set; } 
        public double SessionTime { get; private set; }

        public void UpdatePaceInfo(TelemetryInfo info)
        {
            PaceLine = info.CarIdxPaceLine.Value[this.Driver.Id];
            PaceRow = info.CarIdxPaceRow.Value[this.Driver.Id];
            PaceMode = info.PaceMode.Value;
            PaceFlag = info.CarIdxPaceFlags.Value[this.Driver.Id];
            SessionTime = info.SessionTime.Value;   
        }

    }
}
