using System;
using System.Collections;
using System.Collections.Generic;

namespace TrackRadar
{
    public sealed class CustomExceptionHandler : Java.Lang.Object, Java.Lang.Thread.IUncaughtExceptionHandler
    {
        private readonly Java.Lang.Thread.IUncaughtExceptionHandler defaultHandler;
        private readonly LogFile sharedLogFile;

        public CustomExceptionHandler(Java.Lang.Thread.IUncaughtExceptionHandler defaultHandler, LogFile sharedWriter)
        {
            this.defaultHandler = defaultHandler;
            this.sharedLogFile = sharedWriter;
        }

        public void UncaughtException(Java.Lang.Thread thread, Java.Lang.Throwable ex)
        {
            String stackTrace = Android.Util.Log.GetStackTraceString(ex);
            String message = ex.Message;

            sharedLogFile.WriteLine(LogLevel.Error, message + Environment.NewLine + stackTrace);

            defaultHandler.UncaughtException(thread, ex);
        }
    }
}