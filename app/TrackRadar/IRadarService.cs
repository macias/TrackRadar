using MathUnit;
using System;

namespace TrackRadar
{   
    public interface IRadarService : ILogger
    {
        TimeSpan OffTrackAlarmInterval { get; }
        int OffTrackAlarmCountLimit { get; }
        Length OffTrackAlarmDistance { get; }
        TimeSpan TurnAheadAlarmDistance { get; }
        TimeSpan DoubleTurnAlarmDistance { get; }
        TimeSpan TurnAheadAlarmInterval { get; }
        Speed RestSpeedThreshold { get; }
        Speed RidingSpeedThreshold { get; }
        Length DriftWarningDistance { get; }
        int DriftMovingAwayCountLimit { get; }
        int DriftComingCloserCountLimit { get; }

        void WriteCrossroad(double latitudeDegrees, double longitudeDegrees);
        void WriteDebug(double latitudeDegrees, double longitudeDegrees,string name,string comment);
        void WriteOffTrack(double latitudeDegrees, double longitudeDegrees, string name = null);
        //bool TryAlarm(Alarm alarm,out string reason);
        //bool TryGetLatestTurnAheadAlarmAt(out long timeStamp);
    }
}