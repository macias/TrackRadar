using System;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Preferences;
using Android.Widget;

namespace TrackRadar
{
    [Activity(Label = "Settings2Activity")]
    public class Settings2Activity : PreferenceActivity
    {
     
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.AddPreferencesFromResource(Resource.Layout.Preferences);
        }

      }
}