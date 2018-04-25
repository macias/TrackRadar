using System;
using Android.Content;
using Android.Preferences;
using Android.Util;

namespace TrackRadar
{
    public sealed class IntEditTextPreference : EditTextPreference
    {

        public IntEditTextPreference(Context context) : base(context)
        {
        }

        public IntEditTextPreference(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public IntEditTextPreference(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
        }

        protected override String GetPersistedString(String defaultReturnValue)
        {
            return GetPersistedInt(-1).ToString();
        }

        protected override bool PersistString(String value)
        {
            return PersistInt(int.Parse(value));
        }
    }
}