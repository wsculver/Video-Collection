# Video-Collection

# DVD Download Process
## Useful Software
* [MakeMKV](https://www.makemkv.com/) - Converts DVD to MKV file
  * Make sure you set minimum title length in view->preferences. Oftentimes videos can be less than the default 120 seconds. I recommend setting this to 5 seconds.
* [HandBrake](https://handbrake.fr/) - Converts MKV files to MP4
* [MKVCleaver](https://www.videohelp.com/software/MKVcleaver) - Extracts subtitle files from MKV (requires [MKVToolNix](https://www.videohelp.com/software/MKVToolNix)
* [Subtitle Edit](https://nikse.dk/SubtitleEdit/) - Edit subtitle files
## Steps to Download
1. Connect DVD player to PC and insert disc to download
2. Open MakeMKV and select the disc to download
3. Select the videos you wish to download and start
4. Once downloading is complete open the folder containing your videos in HandBrake
5. In HandBrake you can select individual videos and convert them to MP4 with specified settings<br>
   1. Audio - This is where you can access commentaries or alternate audio for a single video<br>
   2. Subtitles - It is best to disable these completely (even foreign audio scan). Subtitles can be handled better elsewhere<br>
   3. All other settings I leave as default but feel free to mess with them
6. (Optional) SRT subtitle files
   1. Open the original downloaded MKV files in MKVCleaver and select which subtitles to extract ([UTF-8] will give srt files)
   2. Download srt files online - This method is best if MKVCleaver gives you image subtitle files rather than srt or if you want different languages
   3. Edit subtitle files using Subtitle Edit - Remove added subtitles from beginning/end, adjust times, and convert sub and idx files to srt (beware of incorrect characters)
   4. [Convert sub/idx files to srt online](https://subtitletools.com/convert-sub-idx-to-srt-online) - This tool is more reliable for converting files than Subtitle Edit. The limit for free conversions is 3 per day
7. Download thumbnails and format files to be used with Video Collection
