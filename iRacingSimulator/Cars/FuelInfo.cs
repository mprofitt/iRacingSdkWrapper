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
        public float FuelMaxLtr { get; private set; }
        
        /// <summary>
        /// Maximum fuel level in gallons
        /// </summary>
        public float FuelMaxGal { get; private set; }

        /// <summary>
        /// Maximum feul percentage
        /// </summary>
        public float MaxFuelPct { get; private set; }
       
        /// <summary>
        /// Fuel weight - kilograms per liter
        /// </summary>
        public float FuelKgPerLtr { get; private set; }

        /// <summary>
        /// Fuel weight - pounds per gallon
        /// </summary>
        public float FuelLbsPerGal { get; private set; }

        /// <summary>
        /// Liters of fuel remaining
        /// </summary>
        public float FuelLevel { get; private set; }

        /// <summary>
        /// Percent fuel remaining
        /// </summary>
        public float FuelLevelPct { get; private set; }

        /// <summary>
        /// Engine fuel used instantaneous
        /// </summary>
        public float FuelUsePerHour { get; private set; }

        /// <summary>
        /// Engine fuel pressure
        /// </summary>
        public float FuelPress { get; private set; }    


        public void UpdateSessionInfo(SessionInfo info)
        {
            YamlQuery query = info["DriverInfo"];
            string output;

            if (query["FuelMaxLtr"].TryGetValue(out output)) FuelMaxLtr = float.Parse(output);
            if (query["MaxFuelPct"].TryGetValue(out output)) MaxFuelPct = float.Parse(output);
            if (query["FuelKgPerLtr"].TryGetValue(out output)) FuelKgPerLtr = float.Parse(output);

            FuelMaxGal = FuelMaxLtr * 0.2641720524f;
            FuelLbsPerGal = FuelKgPerLtr = FuelLbsPerGal * 2.2046226218f;
        }

        public void UpdateTelemetryInfo(TelemetryInfo info)
        {
            FuelLevel = info.FuelLevel.Value;
            FuelLevelPct = info.FuelLevelPct.Value;
            FuelUsePerHour = info.FuelUsePerHour.Value;
            FuelPress = info.FuelPress.Value;
        }
    }
}
