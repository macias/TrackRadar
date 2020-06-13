﻿using System;
using System.Threading;
using Android.Content;
using Android.Preferences;
using MathUnit;

namespace TrackRadar
{
    public sealed class Preferences : IPreferences
    {
        public const int OffTrackDefaultAudioId = Resource.Raw.sonar_ping;
        public const int GpsLostDefaultAudioId = Resource.Raw.KDE_Error;
        public const int GpsOnDefaultAudioId = Resource.Raw.KDE_Dialog_Appear;
        // https://www.youtube.com/watch?v=Gms9qEWnqrM
        public const int CrossroadsDefaultAudioId = Resource.Raw.Message_Ringtone_Guitar_2_Markdarszs_Gms9qEWnqrM;

        public Speed RestSpeedThreshold { get; set; }
        public Speed RidingSpeedThreshold { get; set; }

        public bool UseVibration { get; set; }
        public bool RequestGps { get; set; }
        public bool ShowTurnAhead { get; set; }
        public int DistanceAudioVolume { get; set; } // 0-100
        public string DistanceAudioFileName { get; set; }
        public int GpsLostAudioVolume { get; set; } // 0-100
        public string GpsLostAudioFileName { get; set; }
        public int GpsOnAudioVolume { get; set; } // 0-100
        public string GpsOnAudioFileName { get; set; }
        public int TurnAheadAudioVolume { get; set; } // 0-100
        public string TurnAheadAudioFileName { get; set; }

        // please note slight asymmetry here -- off-track is triggered by distance
        // and then alarm is repeated by given interval
        // on the other hand "no signal" is triggered by given timeout (without signal)
        // and then alarm is repeated by given interval

        public TimeSpan OffTrackAlarmInterval { get; set; } // seconds
        public Length OffTrackAlarmDistance { get; set; } // in meters
        // max timeout for which it is accepted to have no signal
        public TimeSpan NoGpsAlarmFirstTimeout { get; set; } // seconds
        // amount of time to REPEAT the alarm
        public TimeSpan NoGpsAlarmAgainInterval { get; set; } // minutes

        public Length TurnAheadAlarmDistance { get; set; }
        public TimeSpan TurnAheadAlarmInterval { get; set; }
        public TimeSpan TurnAheadScreenTimeout { get; set; }
        public Length TotalClimbs { get; set; }
        public Length RidingDistance { get; set; }
        public TimeSpan RidingTime { get; set; }
        public Speed TopSpeed { get; set; }

        private string trackName;
        public string TrackName
        {
            get { return Interlocked.CompareExchange(ref this.trackName, null, null); }
            private set { Interlocked.Exchange(ref this.trackName, value); }
        }

        public Preferences()
        {
            this.DistanceAudioVolume = this.GpsLostAudioVolume = this.GpsOnAudioVolume = 100;
            this.OffTrackAlarmInterval = TimeSpan.FromSeconds(10);
            this.OffTrackAlarmDistance = Length.FromMeters(60);
            this.NoGpsAlarmAgainInterval = TimeSpan.FromMinutes(3);
            // please note that we won't make better than one update per second
            this.NoGpsAlarmFirstTimeout = TimeSpan.FromSeconds(5);
            this.RestSpeedThreshold = Speed.FromKilometersPerHour(5); // average walking speed: https://en.wikipedia.org/wiki/Walking
            this.RidingSpeedThreshold = Speed.FromKilometersPerHour(10); // erderly person: https://en.wikipedia.org/wiki/Bicycle_performance
            this.TurnAheadAlarmDistance = Length.FromMeters(100);
            this.TurnAheadAlarmInterval = TimeSpan.FromSeconds(5);
            this.TurnAheadScreenTimeout = TimeSpan.FromSeconds(5);
        }

        private class Keys
        {
            public const string TotalClimbs = "TotalClimbs";
            public const string RidingDistance = "RidingDistance";
            public const string RidingTime = "RidingTime";
            public const string TopSpeed = "TopSpeed";

            public const string TrackFileName = "TrackFileName";
            public const string UseVibration = "UseVibration";
            public const string ShowTurnAhead = "ShowTurnAhead";
            public const string RequestGps = "RequestGps";

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

            public const string TurnAheadAudioVolume = "CrossroadsAudioVolume";
            public const string TurnAheadAudioFileName = "CrossroadsAudioFileName";

            public const string RestSpeedThreshold = "RestSpeedThreshold";
            public const string RidingSpeedThreshold = "RidingSpeedThreshold";

            public const string TurnAheadDistance = "TurnAheadDistance";
            public const string TurnAheadAlarmInterval = "TurnAheadAlarmInterval";
            public const string TurnAheadScreenTimeout = "TurnAheadScreenTimeout";
        }

        public static IPreferences SaveBehaviors(Context context, IPreferences data)
        {
            using (ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context))
            {
                using (ISharedPreferencesEditor editor = prefs.Edit())
                {
                    editor.PutBoolean(Keys.ShowTurnAhead, data.ShowTurnAhead);
                    editor.PutBoolean(Keys.UseVibration, data.UseVibration);
                    editor.PutBoolean(Keys.RequestGps, data.RequestGps);
                    editor.PutInt(Keys.OffTrackAlarmInterval, (int)data.OffTrackAlarmInterval.TotalSeconds);
                    editor.PutInt(Keys.OffTrackAlarmDistance, (int)data.OffTrackAlarmDistance.Meters);
                    editor.PutInt(Keys.NoGpsAgainAlarmInterval, (int)data.NoGpsAlarmAgainInterval.TotalMinutes);
                    editor.PutInt(Keys.NoGpsFirstAlarmTimeout, (int)data.NoGpsAlarmFirstTimeout.TotalSeconds);

                    editor.PutInt(Keys.DistanceAudioVolume, data.DistanceAudioVolume);
                    editor.PutString(Keys.DistanceAudioFileName, data.DistanceAudioFileName);

                    editor.PutInt(Keys.GpsLostAudioVolume, data.GpsLostAudioVolume);
                    editor.PutString(Keys.GpsLostAudioFileName, data.GpsLostAudioFileName);

                    editor.PutInt(Keys.GpsOnAudioVolume, data.GpsOnAudioVolume);
                    editor.PutString(Keys.GpsOnAudioFileName, data.GpsOnAudioFileName);

                    editor.PutInt(Keys.TurnAheadAudioVolume, data.TurnAheadAudioVolume);
                    editor.PutString(Keys.TurnAheadAudioFileName, data.TurnAheadAudioFileName);

                    editor.PutInt(Keys.RestSpeedThreshold, (int)data.RestSpeedThreshold.KilometersPerHour);
                    editor.PutInt(Keys.RidingSpeedThreshold, (int)data.RidingSpeedThreshold.KilometersPerHour);

                    editor.PutInt(Keys.TurnAheadDistance, (int)data.TurnAheadAlarmDistance.Meters);
                    editor.PutInt(Keys.TurnAheadAlarmInterval, (int)data.TurnAheadAlarmInterval.TotalSeconds);
                    editor.PutInt(Keys.TurnAheadScreenTimeout, (int)data.TurnAheadScreenTimeout.TotalSeconds);

                    editor.Commit();
                }
            }

            return data;
        }

        public static IPreferences LoadAll(Context context)
        {
            using (ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context))
            {
                var data = new Preferences();
                data.ShowTurnAhead = prefs.GetBoolean(Keys.ShowTurnAhead, data.ShowTurnAhead);
                data.UseVibration = prefs.GetBoolean(Keys.UseVibration, data.UseVibration);
                data.RequestGps = prefs.GetBoolean(Keys.RequestGps, data.RequestGps);
                data.OffTrackAlarmInterval = TimeSpan.FromSeconds(prefs.GetInt(Keys.OffTrackAlarmInterval, (int)data.OffTrackAlarmInterval.TotalSeconds));
                data.OffTrackAlarmDistance = Length.FromMeters(prefs.GetInt(Keys.OffTrackAlarmDistance, (int)data.OffTrackAlarmDistance.Meters));
                data.NoGpsAlarmAgainInterval = TimeSpan.FromMinutes(prefs.GetInt(Keys.NoGpsAgainAlarmInterval, (int)data.NoGpsAlarmAgainInterval.TotalMinutes));
                data.NoGpsAlarmFirstTimeout = TimeSpan.FromSeconds(prefs.GetInt(Keys.NoGpsFirstAlarmTimeout, (int)data.NoGpsAlarmFirstTimeout.TotalSeconds));

                data.DistanceAudioVolume = prefs.GetInt(Keys.DistanceAudioVolume, data.DistanceAudioVolume);
                data.DistanceAudioFileName = prefs.GetString(Keys.DistanceAudioFileName, data.DistanceAudioFileName);

                data.GpsLostAudioVolume = prefs.GetInt(Keys.GpsLostAudioVolume, data.GpsLostAudioVolume);
                data.GpsLostAudioFileName = prefs.GetString(Keys.GpsLostAudioFileName, data.GpsLostAudioFileName);

                data.GpsOnAudioVolume = prefs.GetInt(Keys.GpsOnAudioVolume, data.GpsOnAudioVolume);
                data.GpsOnAudioFileName = prefs.GetString(Keys.GpsOnAudioFileName, data.GpsOnAudioFileName);

                data.TurnAheadAudioVolume = prefs.GetInt(Keys.TurnAheadAudioVolume, data.TurnAheadAudioVolume);
                data.TurnAheadAudioFileName = prefs.GetString(Keys.TurnAheadAudioFileName, data.TurnAheadAudioFileName);

                data.RestSpeedThreshold = Speed.FromKilometersPerHour(prefs.GetInt(Keys.RestSpeedThreshold, (int)data.RestSpeedThreshold.KilometersPerHour));
                data.RidingSpeedThreshold = Speed.FromKilometersPerHour(prefs.GetInt(Keys.RidingSpeedThreshold, (int)data.RidingSpeedThreshold.KilometersPerHour));

                data.TurnAheadAlarmDistance = Length.FromMeters(prefs.GetInt(Keys.TurnAheadDistance, (int)data.TurnAheadAlarmDistance.Meters));
                data.TurnAheadAlarmInterval = TimeSpan.FromSeconds(prefs.GetInt(Keys.TurnAheadAlarmInterval, (int)data.TurnAheadAlarmInterval.TotalSeconds));
                data.TurnAheadScreenTimeout = TimeSpan.FromSeconds(prefs.GetInt(Keys.TurnAheadScreenTimeout, (int)data.TurnAheadScreenTimeout.TotalSeconds));

                data.TrackName = prefs.GetString(Keys.TrackFileName, "");

                data.TotalClimbs = Length.FromMeters(prefs.GetFloat(Keys.TotalClimbs, 0));
                data.RidingDistance = Length.FromMeters(prefs.GetFloat(Keys.RidingDistance, 0));
                data.TopSpeed = Speed.FromKilometersPerHour(prefs.GetFloat(Keys.TopSpeed, 0));
                data.RidingTime = TimeSpan.FromSeconds(prefs.GetFloat(Keys.RidingTime, 0));

                return data;
            }
        }

        public void SaveTrackFileName(Context context, string data)
        {
            using (ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context))
            {
                using (ISharedPreferencesEditor editor = prefs.Edit())
                {
                    editor.PutString(Keys.TrackFileName, data);

                    editor.Commit();
                }
            }

            this.TrackName = data;
        }

        public void SaveRideStatistics(Context context, Length totalClimbs, Length ridingDistance,TimeSpan ridingTime,Speed topSpeed)
        {
            using (ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context))
            {
                using (ISharedPreferencesEditor editor = prefs.Edit())
                {
                    editor.PutFloat(Keys.TotalClimbs, (float)totalClimbs.Meters);
                    editor.PutFloat(Keys.RidingDistance, (float)ridingDistance.Meters);
                    editor.PutFloat(Keys.RidingTime, (float)ridingTime.TotalSeconds);
                    editor.PutFloat(Keys.TopSpeed, (float)topSpeed.KilometersPerHour);

                    editor.Commit();
                }
            }

            this.TotalClimbs = totalClimbs;
            this.RidingDistance = ridingDistance;
            this.RidingTime = ridingTime;
            this.TopSpeed = topSpeed;
        }

    }
}