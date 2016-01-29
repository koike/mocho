using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using NMeCab;

namespace mocho
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Blogからデータを収集中...");

            var blog = "";
            var urls = "http://ameblo.jp/asakuramomoblog/entrylist";
            var count = 1;
            var backup = "";
            var flag = true;
            while (flag)
            {
                var wc = new WebClient();
                var openurl = "";
                if (count > 1)
                {
                    openurl = urls + "-" + count + ".html";
                }
                else
                {
                    openurl = urls + ".html";
                }
                var st = wc.OpenRead(openurl);
                var sr = new StreamReader(st, Encoding.UTF8);
                var html = sr.ReadToEnd();
                sr.Close();
                st.Close();

                var first = true;

                var re = new Regex("<a class=\"contentTitle\" href=\"(?<url>.*?)\".*?>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                for (var m = re.Match(html); m.Success; m = m.NextMatch())
                {
                    var url = m.Groups["url"].Value;
                    var _wc = new WebClient();
                    var _st = _wc.OpenRead(url);
                    var _sr = new StreamReader(_st, Encoding.UTF8);
                    var _html = _sr.ReadToEnd();
                    _sr.Close();
                    _st.Close();
                    var _re = new Regex("<div class=\"articleText\">(?<t>.*?)<!--entryBottom-->", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    for (var _m = _re.Match(_html); _m.Success; _m = _m.NextMatch())
                    {
                        var text = _m.Groups["t"].Value;
                        text = text.Replace("<br />", "")
                            .Replace(Environment.NewLine, "")
                            .Replace("\r", "")
                            .Replace("\n", "")
                            .Replace("<div>", "")
                            .Replace("</div>", "")
                            .Replace("&gt;", ">")
                            .Replace("&lt;", "<")
                            .Replace("&amp;", "&")
                            .Replace("&quot;", "\"")
                            .Replace("<p>", "")
                            .Replace("</p>", "")
                            .Replace("<!-- google_ad_section_start(name=s1, weight=.9) -->", "")
                            .Replace("<!-- google_ad_section_end(name=s1) -->", "")
                            .Replace("</span>", "");
                        text = Regex.Replace(text, "<a (?<a>.*?)</a>", "");
                        text = Regex.Replace(text, "<img (?<a>.*?)>", "");
                        text = Regex.Replace(text, "<div (?<a>.*?)>", "");
                        text = Regex.Replace(text, "<span (?<a>.*?)>", "") + Environment.NewLine;
                        text = text.Replace("<a", "")
                            .Replace("</a>", "")
                            .Replace("<img>", "")
                            .Replace("<span>", "");
                        //Console.Write(text);
                        blog += text;
                        if (first)
                        {
                            if (!backup.Equals(text))
                            {
                                backup = String.Copy(text);
                                first = false;
                            }
                            else
                            {
                                flag = false;
                                break;
                            }
                        }
                    }
                }
                count++;
            }

            Console.WriteLine("収集しました");
            Console.WriteLine("記事を作成します\n\n");

            var mocho = blog.Split('\n');
            var mecab = MeCabTagger.Create();
            var data = new List<string>();
            foreach (var s in mocho)
            {
                var node = mecab.ParseToNode(s);
                while (node != null)
                {
                    if (!s.Equals(node.Surface) && node.Surface[0] != 0x00 && node.Surface[0] != 13)
                    {
                        data.Add(node.Surface + "|");
                    }
                    node = node.Next;
                }
                data.Add("\n");
            }
            var lines = string.Join("", data).Split('\n');
            var markovDic = new MarkovDictionary();
            foreach (var line in lines)
            {
                markovDic.AddSentence(line.Split('|'));
            }

            var output = new StreamWriter("output.log", false, Encoding.UTF8);

            for (var i = 0; i < 5; i++)
            {
                var sentence = markovDic.BuildSentence();
                Console.WriteLine(string.Join("", sentence) + "\n\n");
                output.WriteLine(string.Join("", sentence));
            }

            output.Close();
        }
    }
}
