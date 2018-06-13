using System;
using Android.Content;
using Android.Preferences;

namespace TrackRadar
{
    public sealed class Preferences
    {
        public const int OffTrackDefaultAudioId = Resource.Raw.sonar_ping;
        public const int GpsLostDefaultAudioId = Resource.Raw.KDE_Error;
        public const int GpsOnDefaultAudioId = Resource.Raw.KDE_Dialog_Appear;
        // https://www.youtube.com/watch?v=Gms9qEWnqrM
        public const int CrossroadsDefaultAudioId = Resource.Raw.Message_Ringtone_Guitar_2_Markdarszs_Gms9qEWnqrM;

        public bool UseVibration { get; set; }
        public int DistanceAudioVolume { get; set; } // 0-100
        public string DistanceAudioFileName { get; set; }
        public int GpsLostAudioVolume { get; set; } // 0-100
        public string GpsLostAudioFileName { get; set; }
        public int GpsOnAudioVolume { get; set; } // 0-100
        public string GpsOnAudioFileName { get; set; }
        public int CrossroadsAudioVolume { get; set; } // 0-100
        public string CrossroadsAudioFileName { get; set; }

        // please note slight asymmetry here -- off-track is triggered by distance
        // and then alarm is repeated by given interval
        // on the other hand "no signal" is triggered by given timeout (without signal)
        // and then alarm is repeated by given interval

        public TimeSpan OffTrackAlarmInterval { get; set; } // seconds
        public int OffTrackAlarmDistance { get; set; } // in meters
        // max timeout for which it is accepted to have no signal
        public TimeSpan NoGpsAlarmFirstTimeout { get; set; } // seconds
        // amount of time to REPEAT the alarm
        public TimeSpan NoGpsAlarmAgainInterval { get; set; } // minutes

        public bool PrimaryAlarmEnabled => this.UseVibration || this.AudioDistanceEnabled;
        public bool AudioDistanceEnabled => this.DistanceAudioVolume > 0
            && (string.IsNullOrEmpty(this.DistanceAudioFileName) || System.IO.File.Exists(DistanceAudioFileName));
        public bool AudioGpsLostEnabled => this.GpsLostAudioVolume > 0
            && (string.IsNullOrEmpty(this.GpsLostAudioFileName) || System.IO.File.Exists(GpsLostAudioFileName));
        public bool AudioGpsOnEnabled => this.GpsOnAudioVolume > 0
            && (string.IsNullOrEmpty(this.GpsOnAudioFileName) || System.IO.File.Exists(GpsOnAudioFileName));
        public bool AudioCrossroadsEnabled => this.CrossroadsAudioVolume > 0
            && (string.IsNullOrEmpty(this.CrossroadsAudioFileName) || System.IO.File.Exists(CrossroadsAudioFileName));

        public Preferences()
        {
            this.DistanceAudioVolume = this.GpsLostAudioVolume = this.GpsOnAudioVolume = 100;
            this.OffTrackAlarmInterval = TimeSpan.FromSeconds(10);
            this.OffTrackAlarmDistance = 60;
            this.NoGpsAlarmAgainInterval = TimeSpan.FromMinutes(5);
            // please note that we won't make better than one update per second
            this.NoGpsAlarmFirstTimeout = TimeSpan.FromSeconds(15);
        }

        private class Keys
        {
            public const string TrackFileName = "TrackFileName";
            public const string UseVibration = "UseVibration";

            public const string OffTrackAlarmInterval = "OffTrackAlarmInterval";
            public const string OffTrackAlarmDistance = "OffTrackAlarmDistance";
            public const string NoGpsAgainAlarmInterval = "NoGpsAlarmInterval";
            public const string NoGpsFirstAlarmTimeout = "NoGpsAlarmTimeout";

            public const string DistanceAudioVolume = "DistanceAudioVolume";
            public const string DistanceAudioFileName = "DistanceAudioFileName";

            public const string GpsLostAudioVolume = "GpsLostAudioVolume";
            public const string GpsLostAudioFileName = "GpsLostAudioFileName";

            public const string GpsOnAudioVolume = "GpsOnAudioVolume";
            public const string GpsOnAudioFileName = "GpsOnAudioFileName";

            public const string CrossroadsAudioVolume = "CrossroadsAudioVolume";
            public const string CrossroadsAudioFileName = "CrossroadsAudioFileName";
        }

        public static void Save(Context context, Preferences data)
        {
            using (ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context))
            {
                using (ISharedPreferencesEditor editor = prefs.Edit())
                {
                    editor.PutBoolean(Keys.UseVibration, data.UseVibration);
                    editor.PutInt(Keys.OffTrackAlarmInterval, (int)data.OffTrackAlarmInterval.TotalSeconds);
                    editor.PutInt(Keys.OffTrackAlarmDistance, data.OffTrackAlarmDistance);
                    editor.PutInt(Keys.NoGpsAgainAlarmInterval, (int)data.NoGpsAlarmAgainInterval.TotalMinutes);
                    editor.PutInt(Keys.NoGpsFirstAlarmTimeout, (int)data.NoGpsAlarmFirstTimeout.TotalSeconds);

                    editor.PutInt(Keys.DistanceAudioVolume, data.DistanceAudioVolume);
                    editor.PutString(Keys.DistanceAudioFileName, data.DistanceAudioFileName);

                    editor.PutInt(Keys.GpsLostAudioVolume, data.GpsLostAudioVolume);
                    editor.PutString(Keys.GpsLostAudioFileName, data.GpsLostAudioFileName);

                    editor.PutInt(Keys.GpsOnAudioVolume, data.GpsOnAudioVolume);
                    editor.PutString(Keys.GpsOnAudioFileName, data.GpsOnAudioFileName);

                    editor.PutInt(Keys.CrossroadsAudioVolume, data.CrossroadsAudioVolume);
                    editor.PutString(Keys.CrossroadsAudioFileName, data.CrossroadsAudioFileName);

                    editor.Commit();
                }
            }
        }

        public static Preferences Load(Context context)
        {
            using (ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context))
            {
                var data = new Preferences();
                data.UseVibration = prefs.GetBoolean(Keys.UseVibration, data.UseVibration);
                data.OffTrackAlarmInterval = TimeSpan.FromSeconds(prefs.GetInt(Keys.OffTrackAlarmInterval, (int)data.OffTrackAlarmInterval.TotalSeconds));
                data.OffTrackAlarmDistance = prefs.GetInt(Keys.OffTrackAlarmDistance, data.OffTrackAlarmDistance);
                data.NoGpsAlarmAgainInterval = TimeSpan.FromMinutes(prefs.GetInt(Keys.NoGpsAgainAlarmInterval, (int)data.NoGpsAlarmAgainInterval.TotalMinutes));
                data.NoGpsAlarmFirstTimeout = TimeSpan.FromSeconds(prefs.GetInt(Keys.NoGpsFirstAlarmTimeout, (int)data.NoGpsAlarmFirstTimeout.TotalSeconds));

                data.DistanceAudioVolume = prefs.GetInt(Keys.DistanceAudioVolume, data.DistanceAudioVolume);
                data.DistanceAudioFileName = prefs.GetString(Keys.DistanceAudioFileName, data.DistanceAudioFileName);

                data.GpsLostAudioVolume = prefs.GetInt(Keys.GpsLostAudioVolume, data.GpsLostAudioVolume);
                data.GpsLostAudioFileName = prefs.GetString(Keys.GpsLostAudioFileName, data.GpsLostAudioFileName);

                data.GpsOnAudioVolume = prefs.GetInt(Keys.GpsOnAudioVolume, data.GpsOnAudioVolume);
                data.GpsOnAudioFileName = prefs.GetString(Keys.GpsOnAudioFileName, data.GpsOnAudioFileName);

                data.CrossroadsAudioVolume = prefs.GetInt(Keys.CrossroadsAudioVolume, data.CrossroadsAudioVolume);
                data.CrossroadsAudioFileName = prefs.GetString(Keys.CrossroadsAudioFileName, data.CrossroadsAudioFileName);

                return data;
            }
        }

        public static void SaveTrackFileName(Context context, string data)
        {
            using (ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context))
            {
                using (ISharedPreferencesEditor editor = prefs.Edit())
                {
                    editor.PutString(Keys.TrackFileName, data);

                    editor.Commit();
                }
            }
        }
        public static string LoadTrackFileName(Context context)
        {
            using (ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context))
            {
                return prefs.GetString(Keys.TrackFileName, "");
            }
        }

    }
}