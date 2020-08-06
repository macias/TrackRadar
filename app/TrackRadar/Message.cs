namespace TrackRadar
{
    public static class Message
    {
        // http://stackoverflow.com/questions/18119763/how-to-limit-broadcast-to-its-own-android-app

        public const string Debug = nameof(TrackRadar) + ".Dbg";
        public const string Distance = nameof(TrackRadar) + ".Dist";
        public const string Alarm = nameof(TrackRadar) + ".Alarm";
        public const string Prefs = nameof(TrackRadar) + ".Prefs";
        public const string RequestInfo = nameof(TrackRadar) + ".RqInf";
        public const string Subscribe = nameof(TrackRadar) + ".Sub";
        public const string Unsubscribe = nameof(TrackRadar) + ".Unsub";
        public const string LoadRequest = nameof(TrackRadar) + ".RqLd";
        public const string LoadingProgress = nameof(TrackRadar) + ".LdInfo";

        public const string TotalClimbsMetersKey = "statcl";
        public const string RidingDistanceMetersKey = "statds";
        public const string TopSpeedKmPerHourKey = "statsp";
        public const string RidingTimeSecondsKey = "stattm";
        public const string DistanceKey = "dist";
        public const string AlarmKey = "alarm";
        public const string DebugKey = "dbg";
        public const string PathKey = "path";
        public const string MessageKey = "msg";
        public const string ProgressKey = "prg";
        public const string TagKey = "tag";

        public const string NoSignalText = "no signal";
    }
}