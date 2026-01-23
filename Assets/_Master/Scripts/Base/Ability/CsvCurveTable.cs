using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace _Master.Base.Ability
{
    public static class CsvCurveTable
    {
        public static bool TryBuildCurve(string csvText, string columnName, out AnimationCurve curve, out int rowCount)
        {
            curve = new AnimationCurve();
            rowCount = 0;

            if (string.IsNullOrEmpty(csvText) || string.IsNullOrEmpty(columnName))
                return false;

            string[] lines = SplitLines(csvText);
            if (lines.Length < 2)
                return false;

            string[] headers = SplitCsvLine(lines[0]);
            if (headers.Length < 2)
                return false;

            int levelIndex = 0;
            int valueIndex = Array.FindIndex(headers, h => string.Equals(h, columnName, StringComparison.OrdinalIgnoreCase));
            if (valueIndex < 0)
                return false;

            var keys = new List<Keyframe>();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                string[] cells = SplitCsvLine(lines[i]);
                if (cells.Length <= Math.Max(levelIndex, valueIndex))
                    continue;

                if (!TryParseFloat(cells[levelIndex], out float level))
                    continue;

                if (!TryParseFloat(cells[valueIndex], out float value))
                    continue;

                keys.Add(new Keyframe(level, value));
                rowCount++;
            }

            if (keys.Count == 0)
                return false;

            curve = new AnimationCurve(keys.ToArray());
            return true;
        }

        public static string[] GetHeaders(string csvText)
        {
            if (string.IsNullOrEmpty(csvText))
                return Array.Empty<string>();

            string[] lines = SplitLines(csvText);
            if (lines.Length == 0)
                return Array.Empty<string>();

            return SplitCsvLine(lines[0]);
        }

        private static string[] SplitLines(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        }

        private static string[] SplitCsvLine(string line)
        {
            return line.Split(',');
        }

        private static bool TryParseFloat(string value, out float result)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
    }
}