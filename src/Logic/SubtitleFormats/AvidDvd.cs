﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Nikse.SubtitleEdit.Logic.SubtitleFormats
{
    public class AvidDvd : SubtitleFormat
    {

        //25    10:03:20:23 10:03:23:05 some text
        //I see, on my way.|New line also.
        //
        //26    10:03:31:18 10:03:34:00 even more text
        //Panessa, why didn't they give them
        //an escape route ?

        private static readonly Regex RegexTimeCode = new Regex(@"^\d+\t\d\d:\d\d:\d\d:\d\d\t\d\d:\d\d:\d\d:\d\d\t.+$", RegexOptions.Compiled);

        public override string Extension
        {
            get { return ".txt"; }
        }

        public override string Name
        {
            get { return "Avid DVD"; }
        }

        public override bool IsTimeBased
        {
            get { return true; }
        }

        public override bool IsMine(List<string> lines, string fileName)
        {
            if (fileName != null && fileName.EndsWith(".dost", StringComparison.OrdinalIgnoreCase))
                return false;

            var subtitle = new Subtitle();
            LoadSubtitle(subtitle, lines, fileName);
            return subtitle.Paragraphs.Count > _errorCount;
        }

        private static string MakeTimeCode(TimeCode tc)
        {
            return string.Format("{0:00}:{1:00}:{2:00}:{3:00}", tc.Hours, tc.Minutes, tc.Seconds, MillisecondsToFramesMaxFrameRate(tc.Milliseconds));
        }

        public override string ToText(Subtitle subtitle, string title)
        {
            var sb = new StringBuilder();
            int count = 1;
            bool italic = false;
            for (int i = 0; i < subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = subtitle.Paragraphs[i];
                string text = p.Text;
                if (text.StartsWith('{') && text.Length > 6 && text[6] == '}')
                    text = text.Remove(0, 6);
                if (text.StartsWith("<i>") && text.EndsWith("</i>"))
                {
                    if (!italic)
                    {
                        italic = true;
                        sb.AppendLine("$Italic = TRUE");
                    }
                }
                else if (italic)
                {
                    italic = false;
                    sb.AppendLine("$Italic = FALSE");
                }

                text = Utilities.RemoveHtmlTags(text);
                sb.AppendLine(string.Format("{0}\t{1}\t{2}\t{3}", count, MakeTimeCode(p.StartTime), MakeTimeCode(p.EndTime), text.Replace(Environment.NewLine, "|")));
                sb.AppendLine();
                count++;
            }

            return sb.ToString();
        }

        private static TimeCode DecodeTimeCode(string timeCode)
        {
            string[] arr = timeCode.Split(":;,".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            return new TimeCode(int.Parse(arr[0]), int.Parse(arr[1]), int.Parse(arr[2]), FramesToMillisecondsMax999(int.Parse(arr[3])));
        }

        public override void LoadSubtitle(Subtitle subtitle, List<string> lines, string fileName)
        {
            _errorCount = 0;
            Paragraph p = null;
            var sb = new StringBuilder();
            bool italic = false;
            foreach (string line in lines)
            {
                string s = line.TrimEnd();
                if (RegexTimeCode.IsMatch(s))
                {
                    try
                    {
                        if (p != null)
                        {
                            p.Text = sb.ToString().Replace("|", Environment.NewLine).Trim();
                            subtitle.Paragraphs.Add(p);
                        }
                        sb = new StringBuilder();
                        string[] arr = s.Split('\t');
                        if (arr.Length >= 3)
                        {
                            string text = s.Remove(0, arr[0].Length + arr[1].Length + arr[2].Length + 2).Trim();

                            if (text.Replace("0", string.Empty).Replace("1", string.Empty).Replace("2", string.Empty).Replace("3", string.Empty).Replace("4", string.Empty).Replace("5", string.Empty).
                                Replace("6", string.Empty).Replace("7", string.Empty).Replace("8", string.Empty).Replace("9", string.Empty).Replace(".", string.Empty).Replace(":", string.Empty).Replace(",", string.Empty).Trim().Length == 0)
                                _errorCount++;
                            if (italic)
                                text = "<i>" + text + "</i>";
                            sb.AppendLine(text);

                            p = new Paragraph(DecodeTimeCode(arr[1]), DecodeTimeCode(arr[2]), string.Empty);
                        }
                    }
                    catch
                    {
                        _errorCount++;
                        p = null;
                    }
                }
                else if (s.StartsWith('$'))
                {
                    if (s.Replace(" ", string.Empty).ToLower() == "$italic=true")
                    {
                        italic = true;
                    }
                    else if (s.Replace(" ", string.Empty).ToLower() == "$italic=false")
                    {
                        italic = false;
                    }
                }
                else if (s.Trim().Length > 0)
                {
                    sb.AppendLine(s);
                }
            }
            if (p != null)
            {
                p.Text = sb.ToString().Replace("|", Environment.NewLine).Trim();
                subtitle.Paragraphs.Add(p);
            }
            subtitle.Renumber(1);
        }

    }
}
