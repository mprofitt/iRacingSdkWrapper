using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using iRacingSdkWrapper;
using NLog;

namespace iRacingSimulator.Drivers
{
    [Serializable]
    public class DriverLiveInfo
    {
        private const float SPEED_CALC_INTERVAL = 0.5f;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static Logger mlog = NLog.LogManager.GetLogger("mlog");

        public DriverLiveInfo(Driver driver)
        {
            logger.Fatal($"**************************");
            logger.Fatal($"***** DriverLiveInfo *****");
            logger.Fatal($"**************************");
            logger.Fatal($"Fatal");
            logger.Error($"Error");
            logger.Warn($"Warn");
            logger.Info($"Info");
            logger.Debug($"Debug");
            logger.Trace($"Trace");

            // Debug
            _driver = driver;
        }

        private readonly Driver _driver;

        public Driver Driver
        {
            get { return _driver; }
        }

        /// <summary>
        ///  Laps started by car index.
        /// </summary>
        public int Lap { get; private set; }

        /// <summary>
        /// Laps completed by car index
        /// </summary>
        public int CarIdxLapCompleted { get; private set; }

        /// <summary>
        /// Track surface material type by car index
        /// </summary>
        /// 
        public TrackSurfaceMaterial CarIdxTrackSurfaceMaterial { get; private set; }

        /// <summary>
        /// Distance around track by car index
        /// </summary>
        public float LapDistance { get; private set; }
        private float _lapDistancePrev;
        private bool _lapDistanceReset;

        /// <summary>
        /// Total distance during session: 1.xxx, 2.xxx, 3.xxx, ...
        /// </summary>
        public float TotalLapDistance
        {
            get { return Lap + LapDistance; }
        }

        public TrackSurfaces TrackSurface { get; private set; }

        public TrackSurfaces TrackSurfacePrev { get; private set; }

        public bool OnPitRoad { get; private set; } 

        public int Gear { get; private set; }

        /// <summary>
        ///  Engine rpm by car index
        /// </summary>
        public float Rpm { get; private set; }

        /// <summary>
        /// Steering angle in radians by car index
        /// </summary>
        public double SteeringAngle { get; private set; }

        /// <summary>
        /// Car speed in Meters / Second
        /// </summary>
        public double Speed { get; private set; }

        /// <summary>
        /// Car speed in Kilometers / Hour
        /// </summary>       
        public double SpeedKph { get; private set; }

        /// <summary>
        /// Car speed in Miles / Hour
        /// </summary>
        public double SpeedMph { get; private set; }

        /// <summary>
        /// Push2Pass active or not
        /// </summary>
        public bool CarIdxP2P_Status { get; private set; }

        /// <summary>
        /// Push2Pass count of usage (or remaining in Race)
        /// </summary>
        public int CarIdxP2P_Count { get; private set; }

        public string DeltaToLeader { get; set; }
        public string DeltaToNext { get; set; }

        public int CurrentSector { get; set; }
        public int CurrentFakeSector { get; set; }

        public bool IsLeader { get; private set; }

        /// <summary>
        /// Race time behind leader or fastest lap time otherwise.
        /// </summary>
        public float F2Time { get; private set; }

        /// <summary>
        /// Estimated lap time for every car in the session.
        /// </summary>
        public float EstTime { get; private set; }

        /// <summary>
        ///  Cars position in race by car index
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        ///  Cars previous position in race by car index
        /// </summary>
        public int PositionPrev { get; set; }

        /// <summary>
        ///  Cars class position in race by car index
        /// </summary>
        public int ClassPosition { get; set; }

        /// <summary>
        /// Cars class id by car index
        /// </summary>
        public int Class { get; private set; }

        /// <summary>
        /// Cars last lap time
        /// </summary>
        public float LastLapTime { get; private set; }

        /// <summary>
        /// Cars best lap time
        /// </summary>
        public float BestLapTime { get; private set; }

        public int TireCompound { get; private set; }

        public int QualTireCompound { get; private set; }

        /// <summary>
        /// Cars Qual tire compound is locked-in
        /// </summary>
        public bool CarIdxQualTireCompoundLocked { get; private set; }


        public void ParseTelemetry(TelemetryInfo e)
        {
            this.Lap = e.CarIdxLap.Value[this.Driver.Id];
            this.CarIdxLapCompleted = e.CarIdxLapCompleted.Value[this.Driver.Id];
            this.CarIdxTrackSurfaceMaterial = e.CarIdxTrackSurfaceMaterial.Value[this.Driver.Id];

            this.CarIdxLapDistPctUpdate(e);
            this.CarIdxTrackSurfaceUpdate(e);

            this.Gear = e.CarIdxGear.Value[this.Driver.Id];
            this.Rpm = e.CarIdxRPM.Value[this.Driver.Id];
            this.SteeringAngle = e.CarIdxSteer.Value[this.Driver.Id];

            this.F2Time = e.CarIdxF2Time.Value[this.Driver.Id];
            this.EstTime = e.CarIdxEstTime.Value[this.Driver.Id];
            this.CheckForPositionChange(e.CarIdxPosition.Value[this.Driver.Id], this.Driver);
            this.ClassPosition = e.CarIdxClassPosition.Value[this.Driver.Id];
            this.Class = e.CarIdxClass.Value[this.Driver.Id];
            this.LastLapTime = e.CarIdxLastLapTime.Value[this.Driver.Id];
            this.BestLapTime = e.CarIdxBestLapTime.Value[this.Driver.Id];
            this.TireCompound = e.CarIdxTireCompound.Value[this.Driver.Id];
            this.QualTireCompound = e.CarIdxQualTireCompound.Value[this.Driver.Id];
            this.CarIdxQualTireCompoundLocked = e.CarIdxQualTireCompoundLocked.Value[this.Driver.Id];
            this.CarIdxP2P_Status = e.CarIdxP2P_Status.Value[this.Driver.Id];
            this.CarIdxP2P_Count = e.CarIdxP2P_Count.Value[this.Driver.Id];
            this.OnPitRoad = e.CarIdxOnPitRoad.Value[this.Driver.Id];

        }

        private double _prevSpeedUpdateTime;
        private double _prevSpeedUpdateDist;

        private void CheckForPositionChange(int pos, Driver driver)
        {
            this.PositionPrev = this.Position;
            this.Position = pos;
            if (this.PositionPrev != pos)
                if (pos > 0 && PositionPrev > 0)
                    Sim.Instance.NotifyPositionChange(pos, this.PositionPrev, driver);
        }

        private void CarIdxLapDistPctUpdate(TelemetryInfo info)
        {
            //if (this.Driver.IsCurrentDriver)
            //{
            //    logger.Debug($"***** CarIdxLapDistPctEvent *****");
            //    logger.Debug($"info.CarIdxLapDistPct.Value[{this.Driver.Id}]: {info.CarIdxLapDistPct.Value[this.Driver.Id]}");
            //    logger.Debug($"this.Driver.Name: {this.Driver.Name}");
            //}

            this._lapDistancePrev = this.LapDistance;
            this.LapDistance = info.CarIdxLapDistPct.Value[this.Driver.Id];

            if (LapDistance > .1 && LapDistance < .11) _lapDistanceReset = false;
            if (LapDistance < _lapDistancePrev)
            {
                if (!_lapDistanceReset)
                {
                    Sim.Instance.NotifyStartFinishEvent(LapDistance, Sim.Instance.SessionData.SessionTick, this.Driver);
                    _lapDistanceReset = true;
                }
            }
        }

        private void CarIdxTrackSurfaceUpdate(TelemetryInfo info)
        {
            this.TrackSurfacePrev = this.TrackSurface;
            this.TrackSurface = info.CarIdxTrackSurface.Value[this.Driver.Id];
        }

        public void CalculateSpeed(TelemetryInfo current, double? trackLengthKm)
        {
            if (current == null) return;
            if (trackLengthKm == null) return;

            try
            {
                var t1 = current.SessionTime.Value;
                var t0 = _prevSpeedUpdateTime;
                var time = t1 - t0;

                if (time < SPEED_CALC_INTERVAL)
                {
                    // Ignore
                    return;
                }

                var p1 = current.CarIdxLapDistPct.Value[this.Driver.Id];
                var p0 = _prevSpeedUpdateDist;

                if (p1 < -0.5 || _driver.Live.TrackSurface == TrackSurfaces.NotInWorld)
                {
                    // Not in world?
                    return;
                }

                if (p0 - p1 > 0.5)
                {
                    // Lap crossing
                    p1 += 1;
                }
                var distancePct = p1 - p0;

                var distance = distancePct * trackLengthKm.GetValueOrDefault() * 1000; //meters


                if (time >= Double.Epsilon)
                {
                    this.Speed = distance / (time); // m/s
                }
                else
                {
                    if (distance < 0)
                        this.Speed = Double.NegativeInfinity;
                    else
                        this.Speed = Double.PositiveInfinity;
                }
                this.SpeedKph = this.Speed * 3.6;
                this.SpeedMph = this.Speed * 2.236936;

                _prevSpeedUpdateTime = t1;
                _prevSpeedUpdateDist = p1;
            }
            catch (Exception ex)
            {
                //Log.Instance.LogError("Calculating speed of car " + this.Driver.Id, ex);
                this.Speed = 0;
            }
        }
    }
}
