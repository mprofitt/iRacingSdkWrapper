using iRacingSdkWrapper;
using iRacingSimulator.Drivers;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRacingSimulator.Cars
{

   
    public class FuelInfo
    {
        public FuelInfo(Car car)
        {
            _car = car;
        }

        private readonly Car _car;

        public Car Car
        {
            get { return _car; }
        }

        /// <summary>
        /// Maximunm fuel level in liters
        /// </summary>
        public float MaxLtr { get; private set; }
        
        /// <summary>
        /// Maximum fuel level in gallons
        /// </summary>
        public float MaxGal { get; private set; }

        /// <summary>
        /// Maximum fuel percentage
        /// </summary>
        public float MaxPct { get; private set; }
       
        /// <summary>
        /// Fuel weight - kilograms per liter
        /// </summary>
        public float KgPerLtr { get; private set; }

        /// <summary>
        /// Fuel weight - pounds per gallon
        /// </summary>
        public float LbsPerGal { get; private set; }

        /// <summary>
        /// Liters of fuel remaining
        /// </summary>
        public float LevelLtr { get; private set; }

        /// <summary>
        /// Gallons of fuel remaining
        /// </summary>
        public float LevelGal { get; private set; }
        private float prevLevelGal;

        /// <summary>
        /// Percent fuel remaining
        /// </summary>
        public float LevelPct { get; private set; }

        /// <summary>
        /// Engine fuel used instantaneous
        /// </summary>
        public float UsePerHour { get; private set; }

        /// <summary>
        /// Fuel consumed during 1 lap.
        /// </summary>
        public float LapUsage { get; private set; }

        /// <summary>
        /// Engine fuel pressure
        /// </summary>
        public float Pressure { get; private set; }

        private int prevLap;

        public void UpdateSessionInfo(SessionInfo info)
        {
            YamlQuery query = info["DriverInfo"];
            string output;

            if (query["FuelMaxLtr"].TryGetValue(out output)) MaxLtr = float.Parse(output);
            if (query["MaxFuelPct"].TryGetValue(out output)) MaxPct = float.Parse(output);
            if (query["FuelKgPerLtr"].TryGetValue(out output)) KgPerLtr = float.Parse(output);

            MaxGal = MaxLtr * 0.2641720524f;
            LbsPerGal = KgPerLtr = LbsPerGal * 2.2046226218f;
        }

        public void UpdateTelemetryInfo(TelemetryInfo info)
        {
            LevelLtr = info.FuelLevel.Value;
            LevelPct = info.FuelLevelPct.Value;
            UsePerHour = info.FuelUsePerHour.Value;
            Pressure = info.FuelPress.Value;
            LevelGal = info.FuelLevel.Value * 0.2641720524f;

            UpdateLapFuelUsage(info.LapCompleted.Value);
            
        }

        private void UpdateLapFuelUsage(int currLap)
        {
            if(currLap != prevLap)
            {
                LapUsage = LevelGal - prevLevelGal;
                prevLap = currLap;
                prevLevelGal = LevelGal;
            }

        }
    }
}
