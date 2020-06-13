using MathUnit;
using System;

namespace TrackRadar
{
    public interface IPreferences
    {
        Speed RestSpeedThreshold { get; }
        Speed RidingSpeedThreshold { get; }

        string TurnAheadAudioFileName { get; }
        int TurnAheadAudioVolume { get; }
        string DistanceAudioFileName { get; }
        int DistanceAudioVolume { get; }
        string GpsLostAudioFileName { get; }
        int GpsLostAudioVolume { get; }
        string GpsOnAudioFileName { get; }
        int GpsOnAudioVolume { get; }
        TimeSpan NoGpsAlarmAgainInterval { get; }
        TimeSpan NoGpsAlarmFirstTimeout { get; }
        Length OffTrackAlarmDistance { get; }
        Length TurnAheadAlarmDistance { get; }
        TimeSpan TurnAheadAlarmInterval { get; }
        TimeSpan TurnAheadScreenTimeout { get; }
        TimeSpan OffTrackAlarmInterval { get; }
        bool UseVibration { get; }
        bool RequestGps { get; }
        bool ShowTurnAhead { get; }

        string TrackName { get; }

        Length TotalClimbs { get; }
        Length RidingDistance { get;  }
        TimeSpan RidingTime { get; }
        Speed TopSpeed { get; }

        void SaveTrackFileName(Android.Content.Context context, string data);
        void SaveRideStatistics(Android.Content.Context context, Length totalClimbs, Length ridingDistance, TimeSpan ridingTime, Speed topSpeed);
    }

    public static class PreferencesExtensions
    {
        public static bool PrimaryAlarmEnabled(this IPreferences _this) => _this.UseVibration || _this.AudioDistanceEnabled();
        public static bool AudioDistanceEnabled(this IPreferences _this) => _this.DistanceAudioVolume > 0
            && (string.IsNullOrEmpty(_this.DistanceAudioFileName) || System.IO.File.Exists(_this.DistanceAudioFileName));
        public static bool AudioGpsLostEnabled(this IPreferences _this) => _this.GpsLostAudioVolume > 0
            && (string.IsNullOrEmpty(_this.GpsLostAudioFileName) || System.IO.File.Exists(_this.GpsLostAudioFileName));
        public static bool AudioGpsOnEnabled(this IPreferences _this) => _this.GpsOnAudioVolume > 0
            && (string.IsNullOrEmpty(_this.GpsOnAudioFileName) || System.IO.File.Exists(_this.GpsOnAudioFileName));
        public static bool AudioCrossroadsEnabled(this IPreferences _this) => _this.TurnAheadAudioVolume > 0
            && (string.IsNullOrEmpty(_this.TurnAheadAudioFileName) || System.IO.File.Exists(_this.TurnAheadAudioFileName));


    }
}