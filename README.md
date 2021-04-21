# TrackRadar

Xamarin/C# app for Android which helps you in your bicycle trips. It lets you load GPX track and then it plays audio whenever you are off-track 
or you lost GPS signal.

## Credits

Single sonar ping sound is extracted from the file: http://www.zedge.net/ringtone/1419653/
KDE_Error audio file is converted from KDE3 sounds.
Guitar ringtone file is amplified version from: https://www.youtube.com/watch?v=Gms9qEWnqrM
Arpeggio sound is shortened version from: https://www.youtube.com/watch?v=QKfy48_WWls
Bell sound extracted from: https://www.youtube.com/watch?v=NtgXxZcEA90

## Building

Please use Visual Studio 2017 or compatible tool. You need GPX library: https://github.com/macias/Gpx

**DevRadar**

To speed up testing I upload to my phone this app as TrackRadar (which I tested last time and it didn't have major problems) and another copy as DevRadar
(which is supposed to be improvement, but was not yet tested for real).

## Rationale

I need moderately priced, e-ink, around 5" device with GPS and ability to run custom apps. The problem is it does not exist, there are some options like:

* Yotaphone -- too expensive (as for 2020, dead end),

* HiSense A5 -- AFAIK no compass,

* dedicated bicycle GPS -- nano screens,

* DIY hybrids -- e-reader with wifi getting GPS data from smartphone.

All of them means paying extra cash for far from perfect solution. Until the dream device comes true I decided to re-use what I have -- Android Gingerbread smartphone --
and since it is completely unreadable in the sun I opted for audible feedback. 

