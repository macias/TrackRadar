using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using TrackRadar.Implementation;

namespace TrackRadar
{
    // https://stackoverflow.com/questions/9361870/android-how-to-get-accurate-altitude
    // https://stackoverflow.com/questions/42194102/precision-of-gps-altitude-readouts-on-android-and-ios-phones

        // todo: przed statsami z lewej strony ikonka rowerzysty/pieszego
    // todo: na razie troche zle liczymy statsy, bo jesli gosc bedzie jechal wolno pod gore to nie zaliczymy ani czasu ani predkosci
    // todo: te thresholdy to trzeba chyba zmienic w zaleznosci od nachylenia 
    // todo: context menu --> clear stats (tylko jak nie ma serwisu)
    // todo: filtr kalmana
    // todo: wyprzedzenie dzwieku turnahead do ekranu
    // todo: miganie ekranem
    // todo: pokazywanie aktualnego wycinka trasy
    // todo: jesli wchodze ze sleepa to natychmiasto zasniecie
    // todo: musze miec layout wrap-around

    // turn ahead
    // https://stackoverflow.com/questions/35356848/android-how-to-launch-activity-over-lock-screen/35357288
    // https://stackoverflow.com/questions/51820240/android-activity-over-lock-screen
    // https://stackoverflow.com/questions/50643424/what-to-use-instead-of-deprecated-flag-show-when-locked-flag-to-start-activity-w

    // turn screen off
    // https://stackoverflow.com/questions/9561320/android-how-to-turn-screen-on-and-off-programmatically
    // https://stackoverflow.com/questions/49242379/turn-screen-off-programmatically
    // https://stackoverflow.com/questions/44679868/lock-or-turn-off-the-screen-programmatically
    public sealed class CanvasView : Android.Views.View
    {
        public CanvasView(Context context) : base(context)
        {

        }

        protected override void OnDraw(Canvas canvas)
        {
            //base.OnDraw(canvas);
            canvas.DrawRGB(0, 0, 0);
            using (var paint = new Paint())
            {
                paint.SetARGB(100, 200, 10, 10);
                paint.StrokeWidth = 10;
                canvas.DrawLine(0, 0, 200, 200, paint);
            }

        }
    }

    [Activity(Label = "TurnAheadActivity")]
    public class TurnAheadActivity : Activity
    {
        private TrackRadarApp app => (TrackRadarApp)Application;
        private WrapTimer timer;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.RequestWindowFeature(Android.Views.WindowFeatures.NoTitle);

            //Remove notification bar
            this.Window.SetFlags(Android.Views.WindowManagerFlags.Fullscreen, Android.Views.WindowManagerFlags.Fullscreen);

            /* if (Build.VERSION.SdkInt >= Build.VERSION_CODES.O_MR1)
             {
                 setShowWhenLocked(true);
                 setTurnScreenOn(true);
                 KeyguardManager keyguardManager = (KeyguardManager)getSystemService(Context.KEYGUARD_SERVICE);
                 if (keyguardManager != null)
                     keyguardManager.requestDismissKeyguard(this, null);
             }
             else*/
             //if (false)
            {
                this.Window.AddFlags(Android.Views.WindowManagerFlags.DismissKeyguard |
                        Android.Views.WindowManagerFlags.ShowWhenLocked |
                        Android.Views.WindowManagerFlags.TurnScreenOn);
            }

            //SetContentView(Resource.Layout.TurnAhead);
            SetContentView(new CanvasView(this));
            this.Title = nameof(TrackRadar) + " turn ahead";


            this.timer = new WrapTimer(Finish);
            this.timer.Change(app.Prefs.TurnAheadScreenTimeout, System.Threading.Timeout.InfiniteTimeSpan);
        }

        protected override void OnDestroy()
        {
            this.timer.Dispose();

            base.OnDestroy();
        }

        private void AutoClose()
        {
            this.Finish();
        }
    }
}