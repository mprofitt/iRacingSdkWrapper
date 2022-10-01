#define IsOnTrackCheck
//#undef IsOnTrackCheck

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iRacingSdkWrapper;
using NLog;

namespace iRacingSimulator.Drivers
{
    public class DriverPrivateInfo
    {
        public DriverPrivateInfo(Driver driver)
        {
            _driver = driver;
        }

        private readonly Driver _driver;

        public Driver Driver
        {
            get { return _driver; }
        }

        public double Speed { get; private set; }
        public float Throttle { get; private set; }
        public float Brake { get; private set; }
        public float Clutch { get; private set; }

        public float Fuel { get; private set; }
        public float FuelPercentage { get; private set; }
        public float FuelPressure { get; private set; }
        public int TireSetsAvailable { get; private set; }  
        public bool IsOnTrack { get; private set; } 
        private bool isOnTrackPrev;

        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger mlog = LogManager.GetLogger("mlog");

        public void ParseTelemetry(TelemetryInfo e)
        {
            this.Speed = e.Speed.Value;
            this.Throttle = e.Throttle.Value;
            this.Brake = e.Brake.Value;
            this.Clutch = e.Clutch.Value;
            this.Fuel = e.FuelLevel.Value;
            this.FuelPercentage = e.FuelLevelPct.Value;
            this.FuelPressure = e.FuelPress.Value;
            this.TireSetsAvailable = e.TireSetsAvailable.Value;
            IsOnTrackCheck(e.IsOnTrack.Value); 
            // TODO: add remaining parameters
        }

        private void IsOnTrackCheck(bool isOnTrack)
        {

#if IsOnTrackCheck

            mlog.Trace($"-------------------------------------------------------------");
            mlog.Trace($"--------------------Is On Track Check------------------------");
            mlog.Trace($"-------------------------------------------------------------");
            mlog.Trace($"isOnTrack:                     {isOnTrack}");
            mlog.Trace($"IsOnTrack:                     {IsOnTrack}");
            mlog.Trace($"isOnTrackPrev:                 {isOnTrackPrev}");
            mlog.Trace($"Instance.Driver is null:       {Sim.Instance.Driver == null}");

#endif
            //Debug
            //if (Sim.Instance.Driver == null) return;
            
            isOnTrackPrev = IsOnTrack;
            IsOnTrack = isOnTrack;

            mlog.Trace($"-------------------------------------------------------------"); 
            mlog.Trace($"IsOnTrack:                     {IsOnTrack}");
            mlog.Trace($"isOnTrackPrev:                 {isOnTrackPrev}");
            mlog.Trace($"IsOnTrack != isOnTrackPrev:    {IsOnTrack != isOnTrackPrev}");

            if (IsOnTrack != isOnTrackPrev)
            {
                mlog.Trace($"IsOnTrackCheck Event: IsOnTrack {IsOnTrack}");

                Sim.Instance.NotifyIsOnTrackEvent(IsOnTrack);
            }
        }
    }
}
