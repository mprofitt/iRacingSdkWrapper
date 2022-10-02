#define SDK_SESSION
#undef SDK_SESSION
#define SDK_TELEMETERY
#undef SDK_TELEMETERY
#define GET_DRIVERS
#undef GET_DRIVERS
#define ON_ON_TRACK_EVENT
#undef ON_ON_TRACK_EVENT


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iRacingSdkWrapper;
using iRacingSimulator.Cars;
//using iRacingSdkWrapper.Bitfields;
using iRacingSimulator.Drivers;
using iRCC.iRacingSimulator.Events;
using NLog;
using static System.Net.Mime.MediaTypeNames;
using static iRacingSimulator.Sim;

namespace iRacingSimulator
{
    public class Sim
    {
        private Sim()
        {
            logger.Info($"***************");
            logger.Info($"***** Sim *****");
            logger.Info($"***************");
            logger.Fatal($"Fatal");
            logger.Error($"Error");
            logger.Warn($"Warn");
            logger.Info($"Info");
            logger.Debug($"Debug");
            logger.Trace($"Trace");          // Debug
            _sdk = new SdkWrapper();
            _drivers = new List<Driver>();
            _sessionData = new SessionData();
            _mustUpdateSessionData = true;

            // Subcribed events
            _sdk.Connected += SdkOnConnected;
            _sdk.Disconnected += SdkOnDisconnected;
            _sdk.TelemetryUpdated += SdkOnTelemetryUpdated;
            _sdk.SessionInfoUpdated += SdkOnSessionInfoUpdated;
        }

        Car _car = new Car();

        public static Sim Instance
        {
            get { return _instance ?? (_instance = new Sim()); }
        }
        private static Sim _instance;

        private TelemetryInfo _telemetry;
        private SessionInfo _sessionInfo;

        private bool _mustUpdateSessionData, _mustReloadDrivers;
        private TimeDelta _timeDelta;

        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger mlog = LogManager.GetLogger("mlog");

#region Properties and Fields

        /// <summary>
        /// Thr irsdk wrapper object
        /// </summary>
        public SdkWrapper Sdk { get { return _sdk; } }
        private readonly SdkWrapper _sdk;

        /// <summary>
        /// The current session number
        /// </summary>
        public int? CurrentSessionNumber { get { return _currentSessionNumber; } }
        private int? _currentSessionNumber;

        /// <summary>
        /// Telemetry Info as sent from irsdk.
        /// </summary>
        public TelemetryInfo Telemetry { get { return _telemetry; } }

        /// <summary>
        /// Session Info as sent from irsdk.
        /// </summary>
        public SessionInfo SessionInfo { get { return _sessionInfo; } }

        /// <summary>
        /// An object with all related Session Data
        /// </summary>
        public SessionData SessionData { get { return _sessionData; } }
        private SessionData _sessionData;

        /// <summary>
        /// The Driver object
        /// </summary>
        public Driver Driver { get { return _driver; } }
        private Driver _driver = null;

        public Car Car { get { return _car; } }

        /// <summary>
        /// The Leader is the Driver object
        /// </summary>
        public Driver Leader{ get { return _leader; } }
        private Driver _leader;

       
        public bool IsReplay { get { return _isReplay; } }
        private bool _isReplay;

        public List<Driver> Drivers { get { return _drivers; } }
        private readonly List<Driver> _drivers; 
        private bool _isUpdatingDrivers;

#endregion

#region Sim Control

        public void Start(double updateFrequency = 10)
        {
            this.Reset();
            _sdk.TelemetryUpdateFrequency = updateFrequency;
            _sdk.Start();
        }
        public void Stop()
        {
            _sdk.Stop();
            this.Reset();
        }
        private void Reset()
        {
            _mustUpdateSessionData = true;
            _mustReloadDrivers = true;
            _currentSessionNumber = null;
            _driver = null;
            _leader = null;
            _drivers.Clear();
            _timeDelta = null;
            _telemetry = null;
            _sessionInfo = null;
            _isUpdatingDrivers = false;
        }
        private void ResetSession()
        {
            // Need to re-load all drivers when session info updates
            _mustReloadDrivers = true;
        }

#endregion

#region Subscribed Events Methods

        private void SdkOnSessionInfoUpdated(object sender, SdkWrapper.SessionInfoUpdatedEventArgs e)
        {

#if SDK_SESSION

            mlog.Trace($"-------------------------------------------------------------");
            mlog.Trace($"-----------------SdkOnSessionInfoUpdated---------------------");
            mlog.Trace($"-------------------------------------------------------------");
            mlog.Trace($"_currentSessionNumber is null  {_currentSessionNumber == null}");
            mlog.Trace($"_mustUpdateSessionData         {_mustUpdateSessionData}");

            YamlQuery query = e.SessionInfo["DriverInfo"]["Drivers"]["CarIdx", 0];

            string name;
            if (!query["UserName"].TryGetValue(out name))
            {
                mlog.Trace($"Driver Not Found");
                // Driver not found
            }
            mlog.Trace($"name         {name}");
            mlog.Trace($"Query work? {query["UserName"].TryGetValue(out name)}");

#endif
            //Debug 
            // Cache info
            _sessionInfo = e.SessionInfo;
            mlog.Trace($"_sessionInfo         {_sessionInfo}");
            // Stop if we don't have a session number yet
            if (_currentSessionNumber == null) return;

            if (_mustUpdateSessionData)
            {
                _sessionData.Update(_sessionInfo);

                mlog.Trace($"_sessionData.Track.Length: { _sessionData.Track.Length * 1000f}");

                _timeDelta = new TimeDelta((float)_sessionData.Track.Length * 1000f, 20, 64);
                _mustUpdateSessionData = false;

                this.OnStaticInfoChanged();
            }
            // Update drivers
            this.UpdateDriverList(_sessionInfo);

            Track.UpdateTrackSessionInfo(_sessionInfo);

            // This will become the origin for all session updates.
            UpdateSessionInfo(_sessionInfo);

            this.OnSessionInfoUpdated(e);
        }
        private void SdkOnTelemetryUpdated(object sender, SdkWrapper.TelemetryUpdatedEventArgs e)
        {

#if SDK_TELEMETERY

            mlog.Trace($"-------------------------------------------------------------");
            mlog.Trace($"------------------SdkOnTelemetryUpdated----------------------");
            mlog.Trace($"-------------------------------------------------------------");
            mlog.Trace($"_currentSessionNumber: {_currentSessionNumber}");
#endif
            //Debug 
            // Cache info
            _telemetry = e.TelemetryInfo;

            _isReplay = e.TelemetryInfo.IsReplayPlaying.Value;

            // Check if session changed
            // Has the session changed?
            ///////////////////////////
            if (_currentSessionNumber == null || (_currentSessionNumber.Value != e.TelemetryInfo.SessionNum.Value))
            {
                _mustUpdateSessionData = true;

                // Session changed, reset session info
                this.ResetSession();
            }

            // Store current session number
            _currentSessionNumber = e.TelemetryInfo.SessionNum.Value;

            // Get previous state
            var sessionWasFinished = this.SessionData.IsFinished;
            var prevFlags = this.SessionData.Flags;
            
            // Update session state
            _sessionData.UpdateState(e.TelemetryInfo.SessionState.Value);

            // Update drivers telemetry
            this.UpdateDriverTelemetry(e.TelemetryInfo);

            // Update session data
            this.SessionData.Update(e.TelemetryInfo);

            // Check if flags updated
            this.CheckSessionFlags(prevFlags, this.SessionData.Flags);

            Track.UpdateTrackTelemetry(e.TelemetryInfo);

            UpdateTelemetryInfo(e.TelemetryInfo);

            if (!sessionWasFinished && this.SessionData.IsFinished)
            {
                // If session just finished, get winners
                // Use result position (not live position)
                //var winners =
                //    Drivers.Where(d => d.CurrentResults != null && d.CurrentResults.ClassPosition == 1).OrderBy(d => d.CurrentResults.Position);
                //foreach (var winner in winners)
                //{
                //    var ev = new RaceEvent();
                //    ev.Driver = winner;
                //    ev.SessionTime = _telemetry.SessionTime.Value;
                //    ev.Lap = winner.Live.Lap;
                //    ev.Type = Events.RaceEvent.EventTypes.Winner;   
                //    this.OnRaceEvent(ev);
                //}
            }
            this.OnTelemetryUpdated(e);
        }
        private void SdkOnDisconnected(object sender, EventArgs e)
        {

#if SDK

            mlog.Trace($"-------------------------------------------------------------");
            mlog.Trace($"--------------------SdkOnDisconnected------------------------");
            mlog.Trace($"-------------------------------------------------------------");

#endif
            //Debug
            this.Reset();
            this.OnDisconnected();
        }
        private void SdkOnConnected(object sender, EventArgs e)
        {

#if SDK

            mlog.Trace($"-------------------------------------------------------------");
            mlog.Trace($"----------------------SdkOnConnected-------------------------");
            mlog.Trace($"-------------------------------------------------------------");

#endif
            //Debug
            this.OnConnected();
        }

#endregion

#region SessionInfo Methods

        private void UpdateDriverList(SessionInfo info)
        {
            mlog.Trace("============================================= UpdateDriverList Called");
            _isUpdatingDrivers = true;
            mlog.Trace("============================================= Calling GetDrivers");
            this.GetDrivers(info);
            _isUpdatingDrivers = false;

            this.GetResults(info);
        }
        private void GetDrivers(SessionInfo info)
        {
            mlog.Trace("============================================= GetDrivers Called");
            if (_mustReloadDrivers)
            {
                Debug.WriteLine("MustReloadDrivers: true");
                _drivers.Clear();
                _mustReloadDrivers = false;
            }

            // Assume max 70 drivers
            for (int id = 0; id < 70; id++)
            {
                // Find existing driver in list
                Driver driver = _drivers.SingleOrDefault(d => d.Id == id);

                if (driver == null)
                {
                    driver = Driver.FromSessionInfo(info, id);

                    // If no driver found, end of list reached
                    if (driver == null) break;

                    driver.IsCurrentDriver = false;

                    // Add to list
                    _drivers.Add(driver);


#if GET_DRIVERS

                    mlog.Trace($"-------------------------------------------------------------");
                    mlog.Trace($"------------------------Get Drivers--------------------------");
                    mlog.Trace($"-------------------------------------------------------------");
                    mlog.Trace($"driver.Name:       {driver.Name}");
                    mlog.Trace($"driver.Id:         {driver.Id}");
                    mlog.Trace($"driver.CarNumber:  {driver.CarNumber}");
                    mlog.Trace($"driver.IRating:    {driver.IRating}");

#endif
                    //Debug
                }
                else
                {
                    // Update and check if driver swap occurred
                    var oldId = driver.CustId;
                    var oldName = driver.Name;
                    driver.ParseDynamicSessionInfo(info);

                    //if (oldId != driver.CustId)
                    //{
                    //    var e = new RaceEvent();
                    //    e.Driver = driver;
                    //    e.PreviousDriverId = oldId;
                    //    e.PreviousDriverName = oldName;
                    //    e.CurrentDriverId = driver.Id;
                    //    e.CurrentDriverName = driver.Name;
                    //    e.SessionTime = _telemetry.SessionTime.Value;
                    //    e.Lap = driver.Live.Lap;

                    //    this.OnRaceEvent(e);
                    //}
                }

                if (_sdk.DriverId == driver.Id)
                {
                    _driver = driver;
                    _driver.IsCurrentDriver = true;
                }
            }
        }
        private void GetResults(SessionInfo info)
        {
            // If currently updating list, or no session yet, then no need to update result info 
            if (_isUpdatingDrivers) return;
            if (_currentSessionNumber == null) return;

            this.GetQualyResults(info);
            this.GetRaceResults(info);
        }
        private void GetQualyResults(SessionInfo info)
        {
            // TODO: stop if qualy is finished
            var query =
                info["QualifyResultsInfo"]["Results"];

            for (int position = 0; position < _drivers.Count; position++)
            {
                var positionQuery = query["Position", position];

                string idValue;
                if (!positionQuery["CarIdx"].TryGetValue(out idValue))
                {
                    // Driver not found
                    continue;
                }

                // Find driver and update results
                int id = int.Parse(idValue);

                var driver = _drivers.SingleOrDefault(d => d.Id == id);
                if (driver != null)
                {
                    driver.UpdateQualyResultsInfo(positionQuery, position);
                }
            }
        }
        private void GetRaceResults(SessionInfo info)
        {
            var query =
                info["SessionInfo"]["Sessions"]["SessionNum", _currentSessionNumber]["ResultsPositions"];

            for (int position = 1; position <= _drivers.Count; position++)
            {
                var positionQuery = query["Position", position];

                string idValue;
                if (!positionQuery["CarIdx"].TryGetValue(out idValue))
                {
                    // Driver not found
                    continue;
                }

                // Find driver and update results
                int id = int.Parse(idValue);

                var driver = _drivers.SingleOrDefault(d => d.Id == id);
                if (driver != null)
                {
                    var previousPosition = driver.Results.Current.ClassPosition;

                    driver.UpdateResultsInfo(_currentSessionNumber.Value, positionQuery, position);

                    if (_telemetry != null)
                    {
                        //// Check for new leader
                        //if (previousPosition > 1 && driver.Results.Current.ClassPosition == 1)
                        //{
                        //    var e = new RaceEvent();
                        //    e.Driver = driver;
                        //    e.SessionTime = _telemetry.SessionTime.Value;
                        //    e.Lap = driver.Live.Lap;

                        //    this.OnRaceEvent(e);
                        //}

                        //// Check for new best lap
                        //var bestlap = _sessionData.UpdateFastestLap(driver.CurrentResults.FastestTime, driver);
                        //if (bestlap != null)
                        //{
                        //    var e = new RaceEvent();
                        //    e.Driver = driver;
                        //    e.BestLap = bestlap;
                        //    e.SessionTime = _telemetry.SessionTime.Value;
                        //    e.Lap = driver.Live.Lap;

                        //    this.OnRaceEvent(e);
                        //}
                    }
                }
            }
        }
        private void UpdateSessionInfo(SessionInfo info)
        {
            Car.Fuel.UpdateSessionInfo(info);
        }

#endregion

#region Telemetry Methods

        private void UpdateDriverTelemetry(TelemetryInfo info)
        {
            // If currently updating list, no need to update telemetry info 
            if (_isUpdatingDrivers) return;
            if (_driver != null) _driver.UpdatePrivateInfo(info);

            foreach (var driver in _drivers)
            {
                driver.Live.CalculateSpeed(info, _sessionData.Track.Length);
                driver.UpdateLiveInfo(info);
                driver.UpdatePaceInfo(info);
                //driver.UpdatePitInfo(info); 
                driver.UpdateSectorTimes(_sessionData.Track, info);
                UpdateLeader();
            }
            //this.CalculateLivePositions();

            this.UpdateTimeDelta();
        }
        private void UpdateLeader()
        {
            Single lead = 9999;
            Driver leadDriver = null;
            foreach (var driver in _drivers)
            {
                if (driver.Live.F2Time < lead)
                {
                    lead = driver.Live.F2Time;
                    leadDriver = driver;
                }
            }
            foreach (var driver in _drivers)
            {
                if(driver == leadDriver)
                {
                    driver.IsLeader = true;
                    _leader = driver;
                }
            }
        }
        private void CalculateLivePositions()
        {
            // In a race that is not yet in checkered flag mode,
            // Live positions are determined from track position (total lap distance)
            // Any other conditions (race finished, P, Q, etc), positions are ordered as result positions

            if (this.SessionData.EventType == "Race" && !this.SessionData.IsCheckered)
            {
                // Determine live position from lapdistance
                int pos = 1;
                foreach (var driver in _drivers.OrderByDescending(d => d.Live.TotalLapDistance))
                {
                    driver.Live.PositionPrev = driver.Live.Position;
                    driver.Live.Position = pos;
                    driver.IsLeader = false;
                    if (pos == 1)
                    {
                        _leader = driver;
                        driver.IsLeader = true;
                    }
                    if (driver.Live.PositionPrev > 0)
                        if (driver.Live.PositionPrev != pos)
                            NotifyPositionChange(pos, driver.Live.PositionPrev, driver);
                    pos++;
                }
            }
            else
            {
                // In P or Q, set live position from result position (== best lap according to iRacing)
                foreach (var driver in _drivers.OrderBy(d => d.Results.Current.Position))
                {
                    if (this.Leader == null) _leader = driver;
                    driver.Live.Position = driver.Results.Current.Position;
                }
            }

            // Determine live class position from live positions and class
            // Group drivers in dictionary with key = classid and value = list of all drivers in that class
            var dict = (from driver in _drivers
                        group driver by driver.Car.CarClassId)
                .ToDictionary(d => d.Key, d => d.ToList());

            // Set class position
            foreach (var drivers in dict.Values)
            {
                var pos = 1;
                foreach (var driver in drivers.OrderBy(d => d.Live.Position))
                {
                    driver.Live.ClassPosition = pos;
                    pos++;
                }
            }

            if (this.Leader != null && this.Leader.CurrentResults != null)
                _sessionData.LeaderLap = this.Leader.CurrentResults.LapsComplete + 1;
        }
        private void UpdateTimeDelta()
        {
            if (_timeDelta == null) return;

            // Update the positions of all cars
            _timeDelta.Update(_telemetry.SessionTime.Value, _telemetry.CarIdxLapDistPct.Value);

            // Order drivers by live position
            var drivers = _drivers.OrderBy(d => d.Live.Position).ToList();
            if (drivers.Count > 0)
            {
                // Get leader
                //var leader = drivers[0];
                this.Leader.Live.DeltaToLeader = "-";
                this.Leader.Live.DeltaToNext = "-";

                // Loop through drivers
                for (int i = 1; i < drivers.Count; i++)
                {
                    var behind = drivers[i];
                    var ahead = drivers[i - 1];

                    // Lapped?
                    var leaderLapDiff = Math.Abs(this.Leader.Live.TotalLapDistance - behind.Live.TotalLapDistance);
                    var nextLapDiff = Math.Abs(ahead.Live.TotalLapDistance - behind.Live.TotalLapDistance);

                    if (leaderLapDiff < 1)
                    {
                        var leaderDelta = _timeDelta.GetDelta(behind.Id, this.Leader.Id);
                        behind.Live.DeltaToLeader = TimeDelta.DeltaToString(leaderDelta);
                    }
                    else
                    {
                        behind.Live.DeltaToLeader = Math.Floor(leaderLapDiff) + " L";
                    }

                    if (nextLapDiff < 1)
                    {
                        var nextDelta = _timeDelta.GetDelta(behind.Id, ahead.Id);
                        behind.Live.DeltaToNext = TimeDelta.DeltaToString(nextDelta);
                    }
                    else
                    {
                        behind.Live.DeltaToNext = Math.Floor(nextLapDiff) + " L";
                    }
                }
            }
        }
        private void CheckSessionFlags(iRacingSdkWrapper.SessionFlags prevFlags, iRacingSdkWrapper.SessionFlags currFlags)
        {

            //logger.Debug($"***** UpdateSessionFlags *****");
            //logger.Debug($"prevFlags: {prevFlags}");
            //logger.Debug($"currFlags: {currFlags}");
            //logger.Debug($"prevFlags.Value: {prevFlags.Value}");
            //logger.Debug($"currFlags.Value: {currFlags.Value}");
            //logger.Debug($"(currFlags.Value == currFlags.Value): {(currFlags.Value == currFlags.Value)}");

            bool isGreen = !prevFlags.HasFlag(SessionFlags.StartGo) && currFlags.HasFlag(SessionFlags.StartGo) 
                || !prevFlags.HasFlag(SessionFlags.Green) && currFlags.HasFlag(SessionFlags.Green);

            //if (!prevFlags.Contains(SessionFlags.StartGo) && currFlags.Contains(SessionFlags.StartGo))
            //{
            //    NotifySessionFlagsEvent(SessionFlags.StartGo);
            //}
            //if (!prevFlags.Contains(SessionFlags.Green) && currFlags.Contains(SessionFlags.Green))
            //{
            //    NotifySessionFlagsEvent(SessionFlags.Green);
            //}

            if (isGreen)
            {
                NotifySessionFlagsEvent(SessionFlags.Green);
            }
            else if (!prevFlags.HasFlag(SessionFlags.Yellow) && currFlags.HasFlag(SessionFlags.Yellow))
            {
                NotifySessionFlagsEvent(SessionFlags.Yellow);
            }

            else if (!prevFlags.HasFlag(SessionFlags.Checkered) && currFlags.HasFlag(SessionFlags.Checkered))
            {
                NotifySessionFlagsEvent(SessionFlags.Checkered);
            }

            else if (!prevFlags.HasFlag(SessionFlags.StartReady) && currFlags.HasFlag(SessionFlags.StartReady))
            {
                NotifySessionFlagsEvent(SessionFlags.StartReady);
            }

            else if (!prevFlags.HasFlag(SessionFlags.StartSet) && currFlags.HasFlag(SessionFlags.StartSet))
            {
                NotifySessionFlagsEvent(SessionFlags.StartSet);
            }

            else if (!prevFlags.HasFlag(SessionFlags.StartHidden) && currFlags.HasFlag(SessionFlags.StartHidden))
            {
                NotifySessionFlagsEvent(SessionFlags.StartHidden);
            }

            else if (!prevFlags.HasFlag(SessionFlags.Black) && currFlags.HasFlag(SessionFlags.Black))
            {
                double speed = _driver.Live.SpeedMph;

                logger.Debug($"***** UpdateSessionFlags *****");

                NotifySessionFlagsEvent(SessionFlags.Black, speed);
            }

            else if (!prevFlags.HasFlag(SessionFlags.Blue) && currFlags.HasFlag(SessionFlags.Blue))
            {
                NotifySessionFlagsEvent(SessionFlags.Blue);
            }

            else if (!prevFlags.HasFlag(SessionFlags.CautionWaving) && currFlags.HasFlag(SessionFlags.CautionWaving))
            {
                NotifySessionFlagsEvent(SessionFlags.CautionWaving);
            }

            else if (!prevFlags.HasFlag(SessionFlags.Disqualify) && currFlags.HasFlag(SessionFlags.Disqualify))
            {
                NotifySessionFlagsEvent(SessionFlags.Disqualify);
            }

            else if (!prevFlags.HasFlag(SessionFlags.Debris) && currFlags.HasFlag(SessionFlags.Debris))
            {
                NotifySessionFlagsEvent(SessionFlags.Debris);
            }

            else if (!prevFlags.HasFlag(SessionFlags.Crossed) && currFlags.HasFlag(SessionFlags.Crossed))
            {
                NotifySessionFlagsEvent(SessionFlags.Crossed);
            }

            else if (!prevFlags.HasFlag(SessionFlags.FiveToGo) && currFlags.HasFlag(SessionFlags.FiveToGo))
            {
                NotifySessionFlagsEvent(SessionFlags.FiveToGo);
            }

            else if (!prevFlags.HasFlag(SessionFlags.GreenHeld) && currFlags.HasFlag(SessionFlags.GreenHeld))
            {
                NotifySessionFlagsEvent(SessionFlags.GreenHeld);
            }

            else if (!prevFlags.HasFlag(SessionFlags.Furled) && currFlags.HasFlag(SessionFlags.Furled))
            {
                NotifySessionFlagsEvent(SessionFlags.Furled);
            }

            else if (!prevFlags.HasFlag(SessionFlags.OneLapToGreen) && currFlags.HasFlag(SessionFlags.OneLapToGreen))
            {
                NotifySessionFlagsEvent(SessionFlags.OneLapToGreen);
            }

            else if (!prevFlags.HasFlag(SessionFlags.RandomWaving) && currFlags.HasFlag(SessionFlags.RandomWaving))
            {
                NotifySessionFlagsEvent(SessionFlags.RandomWaving);
            }

            else if (!prevFlags.HasFlag(SessionFlags.Red) && currFlags.HasFlag(SessionFlags.Red))
            {
                NotifySessionFlagsEvent(SessionFlags.Red);
            }

            else if (!prevFlags.HasFlag(SessionFlags.Repair) && currFlags.HasFlag(SessionFlags.Repair))
            {
                NotifySessionFlagsEvent(SessionFlags.Repair);
            }

            else if (!prevFlags.HasFlag(SessionFlags.White) && currFlags.HasFlag(SessionFlags.White))
            {
                NotifySessionFlagsEvent(SessionFlags.White);
            }

        }
        private void UpdateTelemetryInfo(TelemetryInfo info)
        {
            Car.UpdateTelemetryInfo(info);
        }

        #endregion

        #region Published Event Handler

        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler StaticInfoChanged;
        public event EventHandler<SdkWrapper.SessionInfoUpdatedEventArgs> SessionInfoUpdated;
        public event EventHandler<SdkWrapper.TelemetryUpdatedEventArgs> TelemetryUpdated;
        public event EventHandler SimulationUpdated;
        public event EventHandler<PaceFlagsEventArgs> PaceFlagsEvent;
        public event EventHandler<SessionFlagsEventArgs> SessionFlagsEvent;
        public event EventHandler<StartFinishEventArgs> StartFinishEvent;
        public event EventHandler<PitRoadEventArgs> PitRoadEvent;
        public event EventHandler<OnTrackEventArgs> OnTrackEvent;
        public event EventHandler<PositionChangeEventArgs> PositionChangeEvent;

#endregion

#region Raising Published Events

        protected virtual void OnConnected()
        {
            if (this.Connected != null) this.Connected(this, EventArgs.Empty);
        }
        protected virtual void OnDisconnected()
        {
            if (this.Disconnected != null) this.Disconnected(this, EventArgs.Empty);
        }
        protected virtual void OnStaticInfoChanged()
        {
            if (this.StaticInfoChanged != null) this.StaticInfoChanged(this, EventArgs.Empty);
        }
        protected virtual void OnSessionInfoUpdated(SdkWrapper.SessionInfoUpdatedEventArgs e)
        {
            if (this.SessionInfoUpdated != null) this.SessionInfoUpdated(this, e);
        }
        protected virtual void OnTelemetryUpdated(SdkWrapper.TelemetryUpdatedEventArgs e)
        {
            if (this.TelemetryUpdated != null) this.TelemetryUpdated(this, e);
        }
        protected virtual void OnSimulationUpdated()
        {
            if (this.SimulationUpdated != null) this.SimulationUpdated(this, EventArgs.Empty);
        }
        protected virtual void OnPaceFlagsEvent(PaceFlagsEvent _event)
        {
            if (this.PaceFlagsEvent != null) this.PaceFlagsEvent(this, new PaceFlagsEventArgs(_event));
        }
        protected virtual void OnSessionFlagsEvent(SessionFlagsEvent _event)
        {
            if (this.SessionFlagsEvent != null) this.SessionFlagsEvent(this, new SessionFlagsEventArgs(_event));
        }
        protected virtual void OnStartFinishEvent(StartFinishEvent _event)
        {
            if (this.StartFinishEvent != null) this.StartFinishEvent(this, new StartFinishEventArgs(_event));
        }
        protected virtual void OnPitRoadEvent(PitRoadEvent _event)
        {
            if (this.PitRoadEvent != null) PitRoadEvent(this, new PitRoadEventArgs(_event));
        }
        protected virtual void OnOnTrackEvent(bool _event)
        {

#if ON_ON_TRACK_EVENT

            mlog.Trace($"------------------------------------------------------------------");
            mlog.Trace($"----------------------- On On Track Event-------------------------");
            mlog.Trace($"------------------------------------------------------------------");
            mlog.Trace($" ");

#endif
            //Debug

            if (this.OnTrackEvent != null) OnTrackEvent(this, new OnTrackEventArgs(_event));
        }
        protected virtual void OnPositionChange(PositionChangeEvent _event)
        {
            if (this.PositionChangeEvent != null) PositionChangeEvent(this, new PositionChangeEventArgs(_event));
        }

#endregion

#region Notification of Published Events

        public void NotifyPaceFlagsEvent(PaceFlags type, Driver driver)
        {
            PaceFlagsEvent e = new PaceFlagsEvent();

            e.Type = type;
            e.Driver = driver;
            this.OnPaceFlagsEvent(e);
        }
        public void NotifyPitRoadEvent(PitAction type, Driver driver)
        {
            PitRoadEvent e = new PitRoadEvent();
            e.Driver = driver;
            e.Type = type;
            this.OnPitRoadEvent(e);
        }
        public void NotifySessionFlagsEvent(SessionFlags type, double speed = 0)
        {
            logger.Debug($"***** NotifySessionFlagsEvent *****");
            logger.Debug($"speed: {speed}");
            logger.Debug($"type: {type}");


            SessionFlagsEvent e = new SessionFlagsEvent();
            e.SpeedMph = speed;
            e.Flag = type;
            e.SessionTime = SessionData.SessionTime;
            e.Lap = _telemetry.Lap.Value;
            this.OnSessionFlagsEvent(e);
        }
        public void NotifyStartFinishEvent(float lapDistance, int sessionTick, Driver driver)
        {
            //logger.Debug($"***************************************");
            //logger.Debug($"***** NotifyCarIdxLapDistPctEvent *****");
            //logger.Debug($"***************************************");

            StartFinishEvent e = new StartFinishEvent();
            e.Driver = driver;
            OnStartFinishEvent(e);
        }
        public void NotifyIsOnTrackEvent(bool onTrack)
        {

#if ON_ON_TRACK_EVENT

            mlog.Trace($"--------------------------------------------------------");
            mlog.Trace($"--------------- Notify Is On Track Event ---------------");
            mlog.Trace($"--------------------------------------------------------");
            mlog.Trace($"onTrack:         {onTrack}");

#endif // Debug
            OnOnTrackEvent(onTrack);
        }
        public void NotifyPositionChange(int pos, int posPrev, Driver driver)
        {
            if (driver.Live.TrackSurfacePrev == TrackSurfaces.OffTrack) return;
            if (driver.Live.TrackSurfacePrev == TrackSurfaces.NotInWorld) return;
            
            logger.Debug($"***** NotifyPositionChange *****");
            logger.Debug($"pos: {pos}");
            logger.Debug($"posPrev: {posPrev}");
            logger.Debug($"driver.Name {driver.Name}");         //Debug
            PositionChangeEvent e = new PositionChangeEvent();
            e.Position = pos;
            e.PositionPrev = posPrev;
            e.Driver = driver;
            this.OnPositionChange(e);
        } 
#endregion

#region Published Event Arguments
        public class PitRoadEventArgs : EventArgs
        {
            public PitRoadEventArgs(PitRoadEvent _event)
            {
                Event = _event;
            }

            public PitRoadEvent Event { get; private set; }
        }

        public class PaceFlagsEventArgs : EventArgs
        {
            public PaceFlagsEventArgs(PaceFlagsEvent _event)
            {
                Event = _event;
            }

            public PaceFlagsEvent Event { get; private set; }
        }

        public class SessionFlagsEventArgs : EventArgs
        {
            public SessionFlagsEventArgs(SessionFlagsEvent _event)
            {
                Event = _event;
            }

            public SessionFlagsEvent Event { get; private set; }
        }

        public class StartFinishEventArgs : EventArgs
        {
            public StartFinishEventArgs(StartFinishEvent _event)
            {
                Event = _event;
            }
            public StartFinishEvent Event { get; private set; }
        }

        public class OnTrackEventArgs : EventArgs
        {
            public bool OnTrack { get; set; }
            public OnTrackEventArgs(bool _event)
            {
                OnTrack = _event;
            }
        }

        public class PositionChangeEventArgs : EventArgs
        {
            public PositionChangeEventArgs(PositionChangeEvent _event)
            {
                Event = _event;
            }
            public PositionChangeEvent Event { get; private set; }
        }

#endregion
    }
}
