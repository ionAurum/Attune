using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Aurumworks;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// Static: the feedback file (design, Feedback; ruling A8). Each answer to "How did that feel?"
    /// is one CSV row in Feedback/feedback-&lt;date&gt;.csv under the game's save folder; a self-test
    /// writes only to the folder its -attuneFeedbackFolder flag names, and never to the tester's.
    /// Nothing is uploaded. Rows are only ever added; nothing here changes or deletes a row.
    /// </summary>
    public static class FeedbackLog
    {
        /// <summary>The flag that gives a self-test its own feedback folder.</summary>
        public const string FolderFlag = "-attuneFeedbackFolder";

        /// <summary>The columns, in order.</summary>
        public static readonly string[] Columns =
        {
            "time", "profile", "demo", "source", "seconds in demo", "presses", "answer", "calibration summary", "version",
        };

        /// <summary>The answers a player can give; Skip writes nothing.</summary>
        public static readonly string[] Answers = { "Easy", "Tiring", "Frustrating", "Fun" };

        /// <summary>
        /// The folder rows go to: the flag's in a self-test (null without one, so nothing is written),
        /// else Feedback under the save folder (LocalLow/Jon Goldman/Attune/Feedback).
        /// </summary>
        public static string Folder
        {
            get
            {
                string flagged = CommandLine.Value(FolderFlag);
                if (!string.IsNullOrEmpty(flagged))
                {
                    return flagged;
                }

                return SelfTest.IsRunning ? null : Path.Combine(Application.persistentDataPath, "Feedback");
            }
        }

        /// <summary>Today's file in Folder, or null when there is no folder.</summary>
        public static string TodaysFile
        {
            get
            {
                string folder = Folder;
                return folder == null ? null : Path.Combine(folder, "feedback-" + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".csv");
            }
        }

        /// <summary>The path of the last row written this session, or null.</summary>
        public static string LastFile { get; private set; }

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => LastFile = null;

        /// <summary>
        /// Adds one answer's row to today's file, with the header if the file is new; returns the
        /// file's path, or null if nothing was written.
        /// </summary>
        public static string Write(string profile, string demo, string source, float secondsInDemo, int presses, string answer,
            string calibration)
        {
            string file = TodaysFile;
            if (file == null)
            {
                Debug.Log($"Feedback ({answer}) not written: a self-test without {FolderFlag} writes no feedback.");
                return null;
            }

            string line = Row(
                DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture),
                profile,
                demo,
                source,
                secondsInDemo.ToString("0.0", CultureInfo.InvariantCulture),
                presses.ToString(CultureInfo.InvariantCulture),
                answer,
                calibration,
                BuildInfo.Version);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                bool isNew = !File.Exists(file);
                using (var writer = new StreamWriter(file, append: true, new UTF8Encoding(false)))
                {
                    if (isNew)
                    {
                        writer.Write(Row(Columns) + "\r\n");
                    }

                    writer.Write(line + "\r\n");
                }

                LastFile = file;
                return file;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Feedback couldn't be written to {file}: {exception.Message}");
                return null;
            }
        }

        /// <summary>One CSV row: the fields escaped and joined with commas.</summary>
        public static string Row(params string[] fields)
        {
            var escaped = new List<string>(fields.Length);
            foreach (string field in fields)
            {
                escaped.Add(Escape(field));
            }

            return string.Join(",", escaped);
        }

        /// <summary>
        /// One field as CSV has it (RFC 4180): in quotes, with its quotes doubled, if it holds a comma,
        /// a quote or a line break; as it is otherwise.
        /// </summary>
        public static string Escape(string field)
        {
            if (string.IsNullOrEmpty(field))
            {
                return "";
            }

            if (field.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0 && field.Trim() == field)
            {
                return field;
            }

            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>Splits one CSV row back into its fields (for the self-test's check of the row it wrote).</summary>
        public static List<string> Split(string row)
        {
            var fields = new List<string>();
            var field = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < row.Length; i++)
            {
                char c = row[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < row.Length && row[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        quoted = false;
                    }
                    else
                    {
                        field.Append(c);
                    }
                }
                else if (c == '"')
                {
                    quoted = true;
                }
                else if (c == ',')
                {
                    fields.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }

            fields.Add(field.ToString());
            return fields;
        }
    }
}
