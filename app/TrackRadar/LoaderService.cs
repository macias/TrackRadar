using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using System.Threading;
using TrackRadar.Implementation;
using MathUnit;
using System.Linq;

namespace TrackRadar
{
    [Service]
    internal sealed partial class LoaderService : Service
    {
        private readonly object threadLock = new object();
        private JobQueue queue;
        private LoaderReceiver receiver;
      //  private LogFile serviceLog;
        private DisposableGuard guard;
        private int subscriptions;
        private string lastMessage;
        private double lastProgress;

        private bool hasSubscribers => this.subscriptions > 0;

        private TrackRadarApp app => (TrackRadarApp)Application;

        public LoaderService()
        {
            StrictMode.SetThreadPolicy(new StrictMode.ThreadPolicy.Builder()
                .DetectAll()
                .PenaltyLog()
                .Build());
            StrictMode.SetVmPolicy(new StrictMode.VmPolicy.Builder()
                       .DetectAll()
                       .PenaltyLog()
                       .Build());

        }

        public override IBinder OnBind(Intent intent)
        {
            logDebug(LogLevel.Error, $"{nameof(OnBind)} called");
            throw null;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent loadIntent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            try
            {
                this.guard = new DisposableGuard();
                this.subscriptions = 1;
                // this.serviceLog = new LogFile(this, "service.log", DateTime.UtcNow.AddDays(-2));

                if (!(Java.Lang.Thread.DefaultUncaughtExceptionHandler is CustomExceptionHandler))
                    Java.Lang.Thread.DefaultUncaughtExceptionHandler
                        = new CustomExceptionHandler(Java.Lang.Thread.DefaultUncaughtExceptionHandler);//, this.serviceLog);

                this.lastProgress = -1;

                this.queue = new JobQueue();

                this.receiver = LoaderReceiver.Create(this);
                receiver.LoadRequest += Receiver_LoadRequest;
                receiver.InfoRequest += Receiver_InfoRequest;
                receiver.Subscribe += Receiver_Subscribe;
                receiver.Unsubscribe += Receiver_Unsubscribe;

                logDebug(LogLevel.Info, $"started (+testing log)");

                this.receiver.ProcessLoadRequest(loadIntent);

            }
            catch (Exception ex)
            {
                logDebug(LogLevel.Error, $"Error on start {ex}");
            }

            return StartCommandResult.Sticky;
        }

        private void Receiver_InfoRequest(object sender, EventArgs _)
        {
            using (this.guard.TryEnter(out bool allowed))
            {
                if (!allowed)
                    return;

                lock (this.threadLock)
                {
                    if (this.lastProgress != -1)
                        MainReceiver.SendLoadingProgress(this, lastMessage, lastProgress);
                }

            }
        }


        private void Receiver_Subscribe(object sender, EventArgs e)
        {
            using (this.guard.TryEnter(out bool allowed))
            {
                if (!allowed)
                    return;

                int sub = Interlocked.Increment(ref this.subscriptions);
                this.logLocal(LogLevel.Verbose, $"Subscribing");
                if (sub != 1)
                    this.logLocal(LogLevel.Error, $"Something wrong with sub {sub}");
            }
        }

        private void Receiver_Unsubscribe(object sender, EventArgs e)
        {
            using (this.guard.TryEnter(out bool allowed))
            {
                if (!allowed)
                    return;

                int sub = Interlocked.Decrement(ref this.subscriptions);
                this.logLocal(LogLevel.Verbose, $"Unsubscribing");
                if (sub != 0)
                    this.logLocal(LogLevel.Error, $"Something wrong with unsub {sub}");
            }
        }

        private void Receiver_LoadRequest(object sender, EventFileArgs args)
        {
            using (this.guard.TryEnter(out bool allowed))
            {
                if (!allowed)
                    return;

                logLocal(LogLevel.Verbose, "Received load request");

                this.queue.Enqueue(token => LoadTrack(args.TagRequest, args.Path, args.OffTrackDistance, token));
            }
        }

        private void SendProgress(int tagRequest, string message, double progress)
        {
            lock (this.threadLock)
            {
                logLocal(LogLevel.Verbose, $"Sending load progress {progress}");
                this.lastMessage = message;
                this.lastProgress = progress;
                MainReceiver.SendLoadingProgress(this, lastMessage, lastProgress);
            }
        }

        private void LoadTrack(int tagRequest, string path, Length offTrackDistance, CancellationToken token)
        {
            SendProgress(tagRequest, null, 0);

            try
            {
                GpxData data = null;
                string failure = null;
                if (path == null || !System.IO.File.Exists(path))
                { 
                    failure = "Track is not available.";
                }
                else
                {
                    data = GpxLoader.ReadGpx(path, offTrackDistance,
                                            onProgress: i =>
                                            {
                                                i = i * 0.8 + 0.1; // clip the values within range 0.1-0.9
                                                SendProgress(tagRequest, null, i);
                                            },
                                            token);

                    if (data == null || !data.Segments.Any())
                    {
                        data = null;
                        failure = "Empty track.";
                    }
                }

                if (token.IsCancellationRequested)
                    return;

                app.TrackData = data; // null on fail, non-null on success
                app.TrackTag = tagRequest;

                SendProgress(tagRequest, failure, 1);
            }
            catch (Exception ex)
            {
                logDebug(LogLevel.Error, $"Error while loading GPX {ex.Message}");

                SendProgress(tagRequest, "Error while loading GPX", 1);
            }

        }

        public override void OnLowMemory()
        {
            logDebug(LogLevel.Info, $"OnLowMemory called");

            base.OnLowMemory();
        }

        public override void OnDestroy()
        {
            try
            {
                logDebug(LogLevel.Info, $"OnDestroy: before guard");

                this.guard.Dispose();

                logDebug(LogLevel.Info, $"OnDestroy: before stop");

                logDebug(LogLevel.Info, $"OnDestroy: disposing signal");

                logDebug(LogLevel.Verbose, $"removing events handlers");

                this.receiver.LoadRequest -= Receiver_LoadRequest;
                this.receiver.InfoRequest -= Receiver_InfoRequest;
                this.receiver.Subscribe -= Receiver_Subscribe;
                this.receiver.Unsubscribe -= Receiver_Unsubscribe;

                logDebug(LogLevel.Verbose, $" unregistering receiver");

                UnregisterReceiver(this.receiver);
                this.receiver = null;

                this.queue.Dispose();

                logDebug(LogLevel.Info, "OnDestroy: disposing log");

                /*{
                    IDisposable disp = this.serviceLog;
                    this.serviceLog = null;
                    disp.Dispose();
                }*/

                base.OnDestroy();
            }
            catch (Exception ex)
            {
                logDebug(LogLevel.Error, $"exception during destroying loader service {ex}");
            }
        }

        private void logLocal(LogLevel level, string message)
        {
            decorateMessage(ref message);

            try
            {
                Common.Log(level, message);
                //if (level > LogLevel.Verbose)
                  //  this.serviceLog?.WriteLine(level, message);
            }
            catch (Exception ex)
            {
                Common.Log(LogLevel.Error, $"CRASH {nameof(logLocal)} {ex}");
            }
        }

        private void logDebug(LogLevel level, string message)
        {
            decorateMessage(ref message);

            try
            {
                logLocal(level, message);
                if (this.hasSubscribers)
                    MainReceiver.SendDebug(this, message);
            }
            catch (Exception ex)
            {
                Common.Log(LogLevel.Error, $"CRASH logDebug {ex}");
            }
        }

        private static void decorateMessage(ref string message)
        {
            const string prefix = nameof(LoaderService);
            if (!message.StartsWith(prefix))
                message = $"{prefix} {message}";
        }
    }
}