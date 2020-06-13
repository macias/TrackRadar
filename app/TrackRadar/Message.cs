﻿namespace TrackRadar
{
    public static class Message
    {
        // http://stackoverflow.com/questions/18119763/how-to-limit-broadcast-to-its-own-android-app

        public const string Dbg = nameof(TrackRadar) + "." + nameof(Dbg);
        public const string Dist = nameof(TrackRadar) + "." + nameof(Dist);
        public const string Alarm = nameof(TrackRadar) + "." + nameof(Alarm);
        public const string Prefs = nameof(TrackRadar) + "." + nameof(Prefs);
        public const string Req = nameof(TrackRadar) + "." + nameof(Req);
        public const string Sub = nameof(TrackRadar) + "." + nameof(Sub);
        public const string Unsub = nameof(TrackRadar) + "." + nameof(Unsub);

        public const string TotalClimbsMetersKey = "statcl";
        public const string RidingDistanceMetersKey = "statds";
        public const string TopSpeedKmPerHourKey = "statsp";
        public const string RidingTimeSecondsKey = "stattm";
        public const string DistanceKey = "dist";
        public const string AlarmKey = "alarm";
        public const string DebugKey = "dbg";

        public const string NoSignalText = "no signal";
    }
}