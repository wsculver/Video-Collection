using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace VideoCollection.Subtitles
{
    public sealed class SubtitleParser
    {
        private static readonly string[] _timeSpanStringFormats =
        {
            @"h\:m\:s",
            @"h\:m\:s\:f",
            @"h\:m\:s\:ff",
            @"h\:m\:s\:fff",
            @"h\:m\:ss",
            @"h\:m\:ss\:f",
            @"h\:m\:ss\:ff",
            @"h\:m\:ss\:fff",
            @"h\:mm\:s",
            @"h\:mm\:s\:f",
            @"h\:mm\:s\:ff",
            @"h\:mm\:s\:fff",
            @"h\:mm\:ss",
            @"h\:mm\:ss\:f",
            @"h\:mm\:ss\:ff",
            @"h\:mm\:ss\:fff",
            @"hh\:m\:s",
            @"hh\:m\:s\:f",
            @"hh\:m\:s\:ff",
            @"hh\:m\:s\:fff",
            @"hh\:m\:ss",
            @"hh\:m\:ss\:f",
            @"hh\:m\:ss\:ff",
            @"hh\:m\:ss\:fff",
            @"hh\:mm\:s",
            @"hh\:mm\:s\:f",
            @"hh\:mm\:s\:ff",
            @"hh\:mm\:s\:fff",
            @"hh\:mm\:ss",
            @"hh\:mm\:ss\:f",
            @"hh\:mm\:ss\:ff",
            @"hh\:mm\:ss\:fff",
        };

        public List<SubtitleSegment> ExtractSubtitles( string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            var subtitles = new List<SubtitleSegment>();
            using (var sr = new StreamReader(path))
            {
                var text = sr.ReadToEnd();
                var lines = text.Split(new[] { "\r\n" }, StringSplitOptions.None);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (TryParseSubtitleInterval(lines[i], out var interval))
                    {
                        var content = ExtractCurrentSubtitleContent(i, lines);
                        subtitles.Add(new SubtitleSegment
                        {
                            Start = interval[0].ToString(@"hh\:mm\:ss\:fff"),
                            End = interval[1].ToString(@"hh\:mm\:ss\:fff"),
                            Content = content
                        });
                    }
                }
            }
            return subtitles.OrderBy(s => s).ToList();
        }

        private string ExtractCurrentSubtitleContent(int startIndex, string[] lines)
        {
            var subtitleContent = new StringBuilder();
            int endIndex = Array.IndexOf(lines, string.Empty, startIndex);
            for (int i = startIndex + 1; i < endIndex; i++)
            {
                subtitleContent.AppendLine(lines[i].Trim(' '));
            }
            return subtitleContent.ToString();
        }

        private bool TryParseSubtitleInterval(string input, out List<TimeSpan> interval)
        {
            interval = null;
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }
            var segments = input.Split(new[] { "-->" }, StringSplitOptions.None);
            if (segments.Length != 2)
            {
                return false;
            }
            segments = segments.Select(s => s.Trim(' ').Replace(',', '.').Replace('.', ':')).ToArray();
            if (TimeSpan.TryParseExact(segments[0], _timeSpanStringFormats, DateTimeFormatInfo.InvariantInfo, out var start) &&
                TimeSpan.TryParseExact(segments[1], _timeSpanStringFormats, DateTimeFormatInfo.InvariantInfo, out var end) &&
                start < end)
            {
                interval = new List<TimeSpan>();
                interval.Add(start);
                interval.Add(end);
                return true;
            }
            return false;
        }
    }
}
