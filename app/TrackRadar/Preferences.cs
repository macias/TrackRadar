using System;
using System.Threading;
using Android.Content;
using Android.Preferences;
using MathUnit;

namespace TrackRadar
{
    public sealed class Preferences : IPreferences
    {
        private static class Keys
        {
            public const string TotalClimbs = "TotalClimbs";
            public const string RidingDistance = "RidingDistance";
            public const string RidingTime = "RidingTime";
            public const string TopSpeed = "TopSpeed";

            public const string TrackFileName = "TrackFileName";
            public const string UseVibration = "UseVibration";
            public const string ShowTurnAhead = "ShowTurnAhead";
            public const string GpsFilter = "GpsFilter";
            public const string GpsDump = "GpsDump";

            public const string OffTrackAlarmInterval = "OffTrackAlarmInterval";
            public const string OffTrackAlarmDistance = "OffTrackAlarmDistance";
            public const string OffTrackAlarmCountLimit = "OffTrackAlarmCountLimit";

            public const string NoGpsAgainAlarmInterval = "NoGpsAlarmInterval";
            public const string NoGpsAlarmTimeout = "NoGpsAlarmTimeout";
            public const string GpsLossTimeout = "GpsLossTimeout";

            public const string DistanceAudioVolume = "DistanceAudioVolume";
            public const string DistanceAudioFileName = "DistanceAudioFileName";

            public const string GpsLostAudioVolume = "GpsLostAudioVolume";
            public const string GpsLostAudioFileName = "GpsLostAudioFileName";

            public const string DisengageAudioVolume = "DisengageAudioVolume";
            public const string DisengageAudioFileName = "DisengageAudioFileName";

            public const string GpsOnAudioVolume = "GpsOnAudioVolume";
            public const string GpsOnAudioFileName = "GpsOnAudioFileName";

            public const string TurnAheadAudioVolume = "CrossroadsAudioVolume";
            public const string TurnAheadAudioFileName = "CrossroadsAudioFileName";

            public const string GoAheadAudioFileName = "GoAheadAudioFileName";
            public const string LeftEasyAudioFileName = "LeftEasyAudioFileName";
            public const string LeftCrossAudioFileName = "LeftCrossAudioFileName";
            public const string LeftSharpAudioFileName = "LeftSharpAudioFileName";
            public const string RightEasyAudioFileName = "RightEasyAudioFileName";
            public const string RightCrossAudioFileName = "RightCrossAudioFileName";
            public const string RightSharpAudioFileName = "RightSharpAudioFileName";
            public const string DoubleTurnAudioFileName = "DoubleTurnAudioFileName";

            public const string RestSpeedThreshold = "RestSpeedThreshold";
            public const string RidingSpeedThreshold = "RidingSpeedThreshold";

            public const string TurnAheadDistance = "TurnAheadDistance";
            public const string DoubleTurnDistance = "DoubleTurnDistance";
            public const string TurnAheadAlarmInterval = "TurnAheadAlarmInterval";
            public const string TurnAheadScreenTimeout = "TurnAheadScreenTimeout";

            public const string DriftWarningDistance = "DriftWarningDistance";
            public const string DriftMovingAwayCountLimit = "DriftMovingAwayCountLimit";
            public const string DriftComingCloserCountLimit = "DriftComingCloserCountLimit";
        }

        public const int OffTrackDefaultAudioId = Resource.Raw.sonar_ping;
        public const int GpsLostDefaultAudioId = Resource.Raw.KDE_Error;
        public const int GpsOnDefaultAudioId = Resource.Raw.KDE_Dialog_Appear;
        // https://www.youtube.com/watch?v=QKfy48_WWls
        public const int DisengageDefaultAudioId = Resource.Raw.Arpeggio_Sound_Effect_QKfy48_WWls;
        // https://www.youtube.com/watch?v=Gms9qEWnqrM
        public const int CrossroadsDefaultAudioId = Resource.Raw.Message_Ringtone_Guitar_2_Markdarszs_Gms9qEWnqrM;

        public const int GoAheadDefaultAudioId = Resource.Raw.ttsMP3_com_go_ahead;
        public const int LeftEasyDefaultAudioId = Resource.Raw.ttsMP3_com_left_easy;
        public const int LeftCrossDefaultAudioId = Resource.Raw.ttsMP3_com_left_cross;
        public const int LeftSharpDefaultAudioId = Resource.Raw.ttsMP3_com_left_sharp;
        public const int RightEasyDefaultAudioId = Resource.Raw.ttsMP3_com_right_easy;
        public const int RightCrossDefaultAudioId = Resource.Raw.ttsMP3_com_right_cross;
        public const int RightSharpDefaultAudioId = Resource.Raw.ttsMP3_com_right_sharp;
        public const int DoubleTurnDefaultAudioId = Resource.Raw.Bell_sound_effects_NtgXxZcEA90;

        public static Length DefaultOffTrackAlarmDistance => Length.FromMeters(60);

        public Speed RestSpeedThreshold { get; set; }
        public Speed RidingSpeedThreshold { get; set; }

        public bool UseVibration { get; set; }
        public bool GpsFilter { get; set; }
        public bool GpsDump { get; set; }
        public bool ShowTurnAhead { get; set; }
        public int OffTrackAudioVolume { get; set; } // 0-100
        public string DistanceAudioFileName { get; set; }
        public int GpsLostAudioVolume { get; set; } // 0-100
        public string GpsLostAudioFileName { get; set; }
        public int AcknowledgementAudioVolume { get; set; } // 0-100
        public string GpsOnAudioFileName { get; set; }
        public int DisengageAudioVolume { get; set; } // 0-100
        public string DisengageAudioFileName { get; set; }

        public int TurnAheadAudioVolume { get; set; } // 0-100
        public string TurnAheadAudioFileName { get; set; }

        public string GoAheadAudioFileName { get; set; }
        public string LeftEasyAudioFileName { get; set; }
        public string LeftCrossAudioFileName { get; set; }
        public string LeftSharpAudioFileName { get; set; }
        public string RightEasyAudioFileName { get; set; }
        public string RightCrossAudioFileName { get; set; }
        public string RightSharpAudioFileName { get; set; }
        public string DoubleTurnAudioFileName { get; set; }

        // please note slight asymmetry here -- off-track is triggered by distance
        // and then alarm is repeated by given interval
        // on the other hand "no signal" is triggered by given timeout (without signal)
        // and then alarm is repeated by given interval

        public TimeSpan OffTrackAlarmInterval { get; set; } // seconds
        public int OffTrackAlarmCountLimit { get; set; }
        public Length OffTrackAlarmDistance { get; set; } // in meters
        // time needed for getting stable gps signal
        public TimeSpan GpsAcquisitionTimeout { get; set; } // seconds
        // time "needed" to lose the signal
        public TimeSpan GpsLossTimeout { get; set; } // seconds
        // amount of time to REPEAT the alarm
        public TimeSpan NoGpsAlarmAgainInterval { get; set; } // minutes

        public TimeSpan TurnAheadAlarmDistance { get; set; }
        public TimeSpan TurnAheadAlarmInterval { get; set; }
        public TimeSpan TurnAheadScreenTimeout { get; set; }
        public TimeSpan DoubleTurnAlarmDistance { get; set; }
        public Length TotalClimbs { get; set; }
        public Length RidingDistance { get; set; }
        public TimeSpan RidingTime { get; set; }
        public Speed TopSpeed { get; set; }

        public Length DriftWarningDistance { get; set; } // in meters
        public int DriftMovingAwayCountLimit { get; set; } // count
        public int DriftComingCloserCountLimit { get; set; } // count

        private string trackName;
        public string TrackName
        {
            get { return Interlocked.CompareExchange(ref this.trackName, null, null); }
            private set { Interlocked.Exchange(ref this.trackName, value); }
        }

        public Preferences()
        {
            this.OffTrackAudioVolume
                = this.GpsLostAudioVolume
                = this.DisengageAudioVolume
                = this.TurnAheadAudioVolume
                = this.AcknowledgementAudioVolume = 100;

            this.OffTrackAlarmDistance = DefaultOffTrackAlarmDistance;
            this.OffTrackAlarmInterval = TimeSpan.FromSeconds(10);
            this.OffTrackAlarmCountLimit = 3;

            this.RestSpeedThreshold = Speed.FromKilometersPerHour(5); // average walking speed: https://en.wikipedia.org/wiki/Walking
            this.RidingSpeedThreshold = Speed.FromKilometersPerHour(10); // erderly person: https://en.wikipedia.org/wiki/Bicycle_performance

            // please note that we won't make better than one update per second
            this.GpsAcquisitionTimeout = TimeSpan.FromSeconds(5);
            this.GpsLossTimeout = TimeSpan.FromSeconds(3);
            this.NoGpsAlarmAgainInterval = TimeSpan.FromMinutes(3);

            this.TurnAheadAlarmDistance = TimeSpan.FromSeconds(16);

            this.DoubleTurnAlarmDistance = TimeSpan.FromSeconds(2);
            this.TurnAheadAlarmInterval = TimeSpan.FromSeconds(2);
            this.TurnAheadScreenTimeout = TimeSpan.FromSeconds(5);

            this.DriftWarningDistance = Length.FromMeters(30);
            this.DriftMovingAwayCountLimit = 10;
            this.DriftComingCloserCountLimit = 5;
        }


        public Preferences Clone()
        {
            return (Preferences)MemberwiseClone();
        }

        public Preferences SaveAll(Context context)
        {
            using (ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context))
            {
                using (ISharedPreferencesEditor editor = prefs.Edit())
                {
                    // clear everything first -- this way we get rid of some obsolete entries
                    editor.Clear();
                    editor.Commit();

                    storeStatistics(editor);
                    storeTrackInfo(editor);

                    editor.PutBoolean(Keys.ShowTurnAhead, this.ShowTurnAhead);
                    editor.PutBoolean(Keys.UseVibration, this.UseVibration);
                    editor.PutBoolean(Keys.GpsFilter, this.GpsFilter);
                    editor.PutBoolean(Keys.GpsDump, this.GpsDump);
                    editor.PutInt(Keys.OffTrackAlarmInterval, (int)this.OffTrackAlarmInterval.TotalSeconds);
                    editor.PutInt(Keys.OffTrackAlarmDistance, (int)this.OffTrackAlarmDistance.Meters);
                    editor.PutInt(Keys.OffTrackAlarmCountLimit, this.OffTrackAlarmCountLimit);
                    editor.PutInt(Keys.NoGpsAgainAlarmInterval, (int)this.NoGpsAlarmAgainInterval.TotalMinutes);
                    editor.PutInt(Keys.NoGpsAlarmTimeout, (int)this.GpsAcquisitionTimeout.TotalSeconds);
                    editor.PutInt(Keys.GpsLossTimeout, (int)this.GpsLossTimeout.TotalSeconds);

                    editor.PutInt(Keys.DistanceAudioVolume, this.OffTrackAudioVolume);
                    editor.PutString(Keys.DistanceAudioFileName, this.DistanceAudioFileName);

                    editor.PutInt(Keys.GpsLostAudioVolume, this.GpsLostAudioVolume);
                    editor.PutString(Keys.GpsLostAudioFileName, this.GpsLostAudioFileName);

                    editor.PutInt(Keys.DisengageAudioVolume, this.DisengageAudioVolume);
                    editor.PutString(Keys.DisengageAudioFileName, this.DisengageAudioFileName);

                    editor.PutInt(Keys.GpsOnAudioVolume, this.AcknowledgementAudioVolume);
                    editor.PutString(Keys.GpsOnAudioFileName, this.GpsOnAudioFileName);

                    editor.PutInt(Keys.TurnAheadAudioVolume, this.TurnAheadAudioVolume);
                    editor.PutString(Keys.TurnAheadAudioFileName, this.TurnAheadAudioFileName);

                    editor.PutString(Keys.GoAheadAudioFileName, this.GoAheadAudioFileName);
                    editor.PutString(Keys.LeftEasyAudioFileName, this.LeftEasyAudioFileName);
                    editor.PutString(Keys.LeftCrossAudioFileName, this.LeftCrossAudioFileName);
                    editor.PutString(Keys.LeftSharpAudioFileName, this.LeftSharpAudioFileName);
                    editor.PutString(Keys.RightEasyAudioFileName, this.RightEasyAudioFileName);
                    editor.PutString(Keys.RightCrossAudioFileName, this.RightCrossAudioFileName);
                    editor.PutString(Keys.RightSharpAudioFileName, this.RightSharpAudioFileName);
                    editor.PutString(Keys.DoubleTurnAudioFileName, this.DoubleTurnAudioFileName);

                    editor.PutInt(Keys.RestSpeedThreshold, (int)this.RestSpeedThreshold.KilometersPerHour);
                    editor.PutInt(Keys.RidingSpeedThreshold, (int)this.RidingSpeedThreshold.KilometersPerHour);

                    editor.PutInt(Keys.TurnAheadDistance, (int)this.TurnAheadAlarmDistance.TotalSeconds);
                    editor.PutInt(Keys.DoubleTurnDistance, (int)this.DoubleTurnAlarmDistance.TotalSeconds);
                    editor.PutInt(Keys.TurnAheadAlarmInterval, (int)this.TurnAheadAlarmInterval.TotalSeconds);
                    editor.PutInt(Keys.TurnAheadScreenTimeout, (int)this.TurnAheadScreenTimeout.TotalSeconds);


                    editor.PutInt(Keys.DriftWarningDistance, (int)this.DriftWarningDistance.Meters);
                    editor.PutInt(Keys.DriftMovingAwayCountLimit, (int)this.DriftMovingAwayCountLimit);
                    editor.PutInt(Keys.DriftComingCloserCountLimit, (int)this.DriftComingCloserCountLimit);

                    editor.Commit();
                }
            }

            return this;
        }

        public static IPreferences LoadAll(Context context)
        {
            using (ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context))
            {
                var data = new Preferences();

                data.ShowTurnAhead = prefs.GetBoolean(Keys.ShowTurnAhead, data.ShowTurnAhead);
                data.UseVibration = prefs.GetBoolean(Keys.UseVibration, data.UseVibration);
                data.GpsFilter = prefs.GetBoolean(Keys.GpsFilter, data.GpsFilter);
                data.GpsDump = prefs.GetBoolean(Keys.GpsDump, data.GpsDump);
                data.OffTrackAlarmInterval = TimeSpan.FromSeconds(prefs.GetInt(Keys.OffTrackAlarmInterval, (int)data.OffTrackAlarmInterval.TotalSeconds));
                data.OffTrackAlarmCountLimit = prefs.GetInt(Keys.OffTrackAlarmCountLimit, data.OffTrackAlarmCountLimit);
                data.OffTrackAlarmDistance = Length.FromMeters(prefs.GetInt(Keys.OffTrackAlarmDistance, (int)data.OffTrackAlarmDistance.Meters));
                data.NoGpsAlarmAgainInterval = TimeSpan.FromMinutes(prefs.GetInt(Keys.NoGpsAgainAlarmInterval, (int)data.NoGpsAlarmAgainInterval.TotalMinutes));
                data.GpsAcquisitionTimeout = TimeSpan.FromSeconds(prefs.GetInt(Keys.NoGpsAlarmTimeout, (int)data.GpsAcquisitionTimeout.TotalSeconds));
                data.GpsLossTimeout = TimeSpan.FromSeconds(prefs.GetInt(Keys.GpsLossTimeout, (int)data.GpsLossTimeout.TotalSeconds));

                data.DisengageAudioVolume = prefs.GetInt(Keys.DisengageAudioVolume, data.DisengageAudioVolume);
                data.DisengageAudioFileName = prefs.GetString(Keys.DisengageAudioFileName, data.DisengageAudioFileName);

                data.OffTrackAudioVolume = prefs.GetInt(Keys.DistanceAudioVolume, data.OffTrackAudioVolume);
                data.DistanceAudioFileName = prefs.GetString(Keys.DistanceAudioFileName, data.DistanceAudioFileName);

                data.GpsLostAudioVolume = prefs.GetInt(Keys.GpsLostAudioVolume, data.GpsLostAudioVolume);
                data.GpsLostAudioFileName = prefs.GetString(Keys.GpsLostAudioFileName, data.GpsLostAudioFileName);

                data.AcknowledgementAudioVolume = prefs.GetInt(Keys.GpsOnAudioVolume, data.AcknowledgementAudioVolume);
                data.GpsOnAudioFileName = prefs.GetString(Keys.GpsOnAudioFileName, data.GpsOnAudioFileName);

                data.TurnAheadAudioVolume = prefs.GetInt(Keys.TurnAheadAudioVolume, data.TurnAheadAudioVolume);
                data.TurnAheadAudioFileName = prefs.GetString(Keys.TurnAheadAudioFileName, data.TurnAheadAudioFileName);

                data.GoAheadAudioFileName = prefs.GetString(Keys.GoAheadAudioFileName, data.GoAheadAudioFileName);
                data.LeftEasyAudioFileName = prefs.GetString(Keys.LeftEasyAudioFileName, data.LeftEasyAudioFileName);
                data.LeftCrossAudioFileName = prefs.GetString(Keys.LeftCrossAudioFileName, data.LeftCrossAudioFileName);
                data.LeftSharpAudioFileName = prefs.GetString(Keys.LeftSharpAudioFileName, data.LeftSharpAudioFileName);
                data.RightEasyAudioFileName = prefs.GetString(Keys.RightEasyAudioFileName, data.RightEasyAudioFileName);
                data.RightCrossAudioFileName = prefs.GetString(Keys.RightCrossAudioFileName, data.RightCrossAudioFileName);
                data.RightSharpAudioFileName = prefs.GetString(Keys.RightSharpAudioFileName, data.RightSharpAudioFileName);
                data.DoubleTurnAudioFileName = prefs.GetString(Keys.DoubleTurnAudioFileName, data.DoubleTurnAudioFileName);

                data.RestSpeedThreshold = Speed.FromKilometersPerHour(prefs.GetInt(Keys.RestSpeedThreshold, (int)data.RestSpeedThreshold.KilometersPerHour));
                data.RidingSpeedThreshold = Speed.FromKilometersPerHour(prefs.GetInt(Keys.RidingSpeedThreshold, (int)data.RidingSpeedThreshold.KilometersPerHour));

                data.TurnAheadAlarmDistance = TimeSpan.FromSeconds(prefs.GetInt(Keys.TurnAheadDistance, (int)data.TurnAheadAlarmDistance.TotalSeconds));
                data.DoubleTurnAlarmDistance = TimeSpan.FromSeconds(prefs.GetInt(Keys.DoubleTurnDistance, (int)data.DoubleTurnAlarmDistance.TotalSeconds));
                data.TurnAheadAlarmInterval = TimeSpan.FromSeconds(prefs.GetInt(Keys.TurnAheadAlarmInterval, (int)data.TurnAheadAlarmInterval.TotalSeconds));
                data.TurnAheadScreenTimeout = TimeSpan.FromSeconds(prefs.GetInt(Keys.TurnAheadScreenTimeout, (int)data.TurnAheadScreenTimeout.TotalSeconds));

                data.TrackName = prefs.GetString(Keys.TrackFileName, data.TrackName);

                data.TotalClimbs = Length.FromMeters(prefs.GetFloat(Keys.TotalClimbs, 0));
                data.RidingDistance = Length.FromMeters(prefs.GetFloat(Keys.RidingDistance, 0));
                data.TopSpeed = Speed.FromKilometersPerHour(prefs.GetFloat(Keys.TopSpeed, 0));
                data.RidingTime = TimeSpan.FromSeconds(prefs.GetFloat(Keys.RidingTime, 0));

                data.DriftWarningDistance = Length.FromMeters(prefs.GetInt(Keys.DriftWarningDistance, (int)data.DriftWarningDistance.Meters));
                data.DriftMovingAwayCountLimit = prefs.GetInt(Keys.DriftMovingAwayCountLimit, data.DriftMovingAwayCountLimit);
                data.DriftComingCloserCountLimit = prefs.GetInt(Keys.DriftComingCloserCountLimit, data.DriftComingCloserCountLimit);

                return data;
            }
        }

        public void SaveTrackFileName(Context context, string trackPath)
        {
            this.TrackName = trackPath;

            using (ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context))
            {
                using (ISharedPreferencesEditor editor = prefs.Edit())
                {
                    storeTrackInfo(editor);

                    editor.Commit();
                }
            }
        }

        private void storeTrackInfo(ISharedPreferencesEditor editor)
        {
            editor.PutString(Keys.TrackFileName, this.TrackName);
        }

        public void SaveRideStatistics(Context context, Length totalClimbs, Length ridingDistance, TimeSpan ridingTime, Speed topSpeed)
        {
            this.TotalClimbs = totalClimbs;
            this.RidingDistance = ridingDistance;
            this.RidingTime = ridingTime;
            this.TopSpeed = topSpeed;

            using (ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context))
            {
                using (ISharedPreferencesEditor editor = prefs.Edit())
                {
                    storeStatistics(editor);

                    editor.Commit();
                }
            }
        }

        private void storeStatistics(ISharedPreferencesEditor editor)
        {
            editor.PutFloat(Keys.TotalClimbs, (float)this.TotalClimbs.Meters);
            editor.PutFloat(Keys.RidingDistance, (float)this.RidingDistance.Meters);
            editor.PutFloat(Keys.RidingTime, (float)this.RidingTime.TotalSeconds);
            editor.PutFloat(Keys.TopSpeed, (float)this.TopSpeed.KilometersPerHour);
        }
    }
}