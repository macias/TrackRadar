using MathUnit;
using System;

namespace TrackRadar
{
    public interface IPreferences
    {
        Speed RestSpeedThreshold { get; }
        Speed RidingSpeedThreshold { get; }

        string GoAheadAudioFileName { get; }
        string LeftEasyAudioFileName { get; }
        string LeftCrossAudioFileName { get; }
        string LeftSharpAudioFileName { get; }
        string RightEasyAudioFileName { get; }
        string RightCrossAudioFileName { get; }
        string RightSharpAudioFileName { get; }

        string TurnAheadAudioFileName { get; }
        int TurnAheadAudioVolume { get; }
        string DistanceAudioFileName { get; }
        int OffTrackAudioVolume { get; }
        string GpsLostAudioFileName { get; }
        int GpsLostAudioVolume { get; }
        string GpsOnAudioFileName { get; }
        int AcknowledgementAudioVolume { get; }

        string DisengageAudioFileName { get; }
        int DisengageAudioVolume { get; }
        TimeSpan NoGpsAlarmAgainInterval { get; }
        TimeSpan NoGpsAlarmFirstTimeout { get; }
        Length OffTrackAlarmDistance { get; }
        TimeSpan TurnAheadAlarmDistance { get; }
        TimeSpan TurnAheadAlarmInterval { get; }
        TimeSpan TurnAheadScreenTimeout { get; }
        TimeSpan OffTrackAlarmInterval { get; }
        bool UseVibration { get; }
        bool DebugKillingService { get; }
        bool GpsFilter { get; }
        bool GpsDump { get; }
        bool ShowTurnAhead { get; }

        string TrackName { get; }

        Length TotalClimbs { get; }
        Length RidingDistance { get; }
        TimeSpan RidingTime { get; }
        Speed TopSpeed { get; }

        Preferences Clone();
        void SaveTrackFileName(Android.Content.Context context, string data);
        void SaveRideStatistics(Android.Content.Context context, Length totalClimbs, Length ridingDistance, TimeSpan ridingTime, Speed topSpeed);
    }

    public static class PreferencesExtensions
    {
        public static bool AlarmsValid(this IPreferences prefs)
        {
            if (prefs.TurnAheadAudioVolume > 0)
            {
                if (!validAlarmFileName(prefs.TurnAheadAudioFileName)
                    || !validAlarmFileName(prefs.GoAheadAudioFileName)
                    || !validAlarmFileName(prefs.LeftEasyAudioFileName)
                    || !validAlarmFileName(prefs.LeftCrossAudioFileName)
                    || !validAlarmFileName(prefs.LeftSharpAudioFileName)
                    || !validAlarmFileName(prefs.RightEasyAudioFileName)
                    || !validAlarmFileName(prefs.RightCrossAudioFileName)
                    || !validAlarmFileName(prefs.RightSharpAudioFileName))
                    return false;
            }

            if ( prefs.OffTrackAudioVolume > 0
                && !validAlarmFileName(prefs.DistanceAudioFileName))
                return false;
            if (prefs.GpsLostAudioVolume > 0
                && !validAlarmFileName(prefs.GpsLostAudioFileName))
                return false;
            if (prefs.AcknowledgementAudioVolume > 0
                && !validAlarmFileName(prefs.GpsOnAudioFileName))
                return false;
            if (prefs.DisengageAudioVolume > 0
                && !validAlarmFileName(prefs.DisengageAudioFileName))
                return false;

            // running program without absolute minimum functionality does not make sense really
            if (!prefs.UseVibration && (prefs.OffTrackAudioVolume == 0 || prefs.GpsLostAudioVolume == 0))
                return false;

            return true;
        }

        /*public static bool PrimaryAlarmEnabled(this IPreferences _this) => _this.UseVibration || _this.AudioDistanceEnabled();
        public static bool AudioDistanceEnabled(this IPreferences _this) => _this.OffTrackAudioVolume > 0
            && validAlarmFileName(_this.DistanceAudioFileName);
        public static bool AudioGpsLostEnabled(this IPreferences _this) => _this.GpsLostAudioVolume > 0
            && validAlarmFileName(_this.GpsLostAudioFileName);
        public static bool AudioGpsOnEnabled(this IPreferences _this) => _this.AcknowledgementAudioVolume > 0
            && validAlarmFileName(_this.GpsOnAudioFileName);
        public static bool AudioDisengageEnabled(this IPreferences _this) => _this.DisengageAudioVolume > 0
            && validAlarmFileName(_this.DisengageAudioFileName);
        public static bool AudioCrossroadEnabled(this IPreferences _this) => _this.TurnAheadAudioVolume > 0
            && validAlarmFileName(_this.TurnAheadAudioFileName);
        public static bool AudioGoAheadEnabled(this IPreferences _this) => _this.TurnAheadAudioVolume > 0
            && validAlarmFileName(_this.GoAheadAudioFileName);
        public static bool AudioLeftEasyEnabled(this IPreferences _this) => _this.TurnAheadAudioVolume > 0
            && validAlarmFileName(_this.LeftEasyAudioFileName);
        public static bool AudioLeftCrossEnabled(this IPreferences _this) => _this.TurnAheadAudioVolume > 0
            && validAlarmFileName(_this.LeftCrossAudioFileName);
        public static bool AudioLeftSharpEnabled(this IPreferences _this) => _this.TurnAheadAudioVolume > 0
            && validAlarmFileName(_this.LeftSharpAudioFileName);
        public static bool AudioRightEasyEnabled(this IPreferences _this) => _this.TurnAheadAudioVolume > 0
            && validAlarmFileName(_this.RightEasyAudioFileName);
        public static bool AudioRightCrossEnabled(this IPreferences _this) => _this.TurnAheadAudioVolume > 0
            && validAlarmFileName(_this.RightCrossAudioFileName);
        public static bool AudioRightSharpEnabled(this IPreferences _this) => _this.TurnAheadAudioVolume > 0
            && validAlarmFileName(_this.RightSharpAudioFileName);
            */
        private static bool validAlarmFileName(string s)
        {
            return string.IsNullOrEmpty(s) // using default alarm
                || System.IO.File.Exists(s);
        }
    }
}