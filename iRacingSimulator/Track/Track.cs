using System.Collections.Generic;
using System.Globalization;
using iRacingSdkWrapper;

namespace iRacingSimulator
{
    public class Track
    {
        private readonly List<Sector> _sectors;

        public Track()
        {
            _sectors = new List<Sector>();
        }
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string CodeName { get; set; } = "";
        public double Length { get; set; }
        public bool NightMode { get; set; }
        public int PitSpeedLimit { get; set; }

        public List<Sector> Sectors
        {
            get { return _sectors; }
        }

        public static void UpdateTrackTelemetry(TelemetryInfo info)
        {
            Conditions.UpdateTelemtry(info);
        }

        public static void UpdateTrackSessionInfo(SessionInfo info)
        {
            Conditions.UpdateSessionInfo(info);
        }


        public static Track FromSessionInfo(SessionInfo info)
        {
            var query = info["WeekendInfo"];

            logger.Debug($"***** FromSessionInfo *****");
            logger.Debug($"query[\"TrackSpeedLimit\"].GetValue(): {query["TrackSpeedLimit"].GetValue()}");
            
            var track = new Track();

            
            track.Id = Parser.ParseInt(query["TrackID"].GetValue());
            track.Name = query["TrackDisplayName"].GetValue();
            track.CodeName = query["TrackName"].GetValue();
            track.Length = Parser.ParseTrackLength(query["TrackLength"].GetValue());
            track.NightMode = query["WeekendOptions"]["NightMode"].GetValue() == "1";
            track.PitSpeedLimit = Parser.ParseTrackPitSpeedLimit(query["TrackPitSpeedLimit"].GetValue());


            // Parse sectors
            track.Sectors.Clear();
            query = info["SplitTimeInfo"]["Sectors"];

            int nr = 0;
            while (nr >= 0)
            {
                var pctString = query["SectorNum", nr]["SectorStartPct"].GetValue();
                float pct;
                if (string.IsNullOrWhiteSpace(pctString) || !float.TryParse(pctString, NumberStyles.AllowDecimalPoint, 
                    CultureInfo.InvariantCulture, out pct))
                {
                    break;
                }

                var sector = new Sector();
                sector.Number = nr;
                sector.StartPercentage = pct;
                track.Sectors.Add(sector);

                nr++;
            }

            return track;
        }


    }
}
