using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iRacingSdkWrapper;

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
            IsOnTrackCheck(this.IsOnTrack = e.IsOnTrack.Value); 
            // TODO: add remaining parameters
        }

        private void IsOnTrackCheck(bool isOnTrack)
        {
            isOnTrackPrev = IsOnTrack;
            IsOnTrack = isOnTrack;
            if(isOnTrack != isOnTrackPrev)
            {
               Sim.Instance!.NotifyIsOnTrackEvent(IsOnTrack);
            }
        }
    }
}
