using System.Collections.Generic;
using System.IO;

/// <summary>
/// .env 파일(KEY=VALUE 줄)을 읽는다. '#'로 시작하는 줄과 빈 줄은 무시한다.
/// </summary>
public static class EnvFileReader
{
    /// <summary>파일이 없으면 빈 사전을 돌려준다.</summary>
    public static Dictionary<string, string> Read(string path)
    {
        var values = new Dictionary<string, string>();
        if (!File.Exists(path)) return values;

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;

            int separator = line.IndexOf('=');
            if (separator <= 0) continue;

            var key = line.Substring(0, separator).Trim();
            var value = line.Substring(separator + 1).Trim().Trim('"', '\'');
            values[key] = value;
        }
        return values;
    }
}
