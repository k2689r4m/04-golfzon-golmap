using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

public class HashTag
{
    public static List<string> ParseTag(string content)
    {
        string regex = @"#([\d|A-Z|a-z|ㄱ-ㅎ|ㅏ-ㅣ|가-힣]*)";

        Regex r = new Regex(regex, RegexOptions.IgnoreCase);
        Match m = r.Match(content);

        List<string> tags = new List<string>();

        while (m.Success)
        {
            string tag = m.Groups[1].Value;

            if (tag.Length != 0)
            {
                tags.Add(m.Groups[1].Value);
            }
            
            m = m.NextMatch();
        }

        return tags;
    }
}