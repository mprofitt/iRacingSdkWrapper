using iRacingSdkWrapper;
using iRacingSimulator.Drivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRacingSimulator.Cars
{
    public class Car
    {
        public Car()
        {
            this.Fuel = new FuelInfo(this);
        }

        public FuelInfo Fuel { get; set; }
        
        public void UpdateSessionInfo(SessionInfo info)
        {
            Fuel.UpdateSessionInfo(info);
        }

        public void UpdateTelemetryInfo(TelemetryInfo info)
        {
            Fuel.UpdateTelemetryInfo(info);
        }
    }
}
