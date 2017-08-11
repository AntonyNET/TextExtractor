namespace TextExtractor.Content.Rtf
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;

    public class RtfExtractor : IContentExtractor
    {
        private readonly Stack<Dictionary<string, object>> _groupsStack = new Stack<Dictionary<string, object>>();
        private readonly Dictionary<string, int> _fonts = new Dictionary<string, int>();

        private bool IsPlainText(IReadOnlyDictionary<string, object> group)
        {
            var bannedChars = new[] {"*", "fonttbl", "colortbl", "datastore", "themedata", "stylesheet", "info", "picw", "pich"};

            return bannedChars.All(t => group.ContainsKey(t) == false);
        }

        private string FromMacRoman(string c)
        {
            var charMapping = new Dictionary<string, string>
                                  {
                                      {"0x83", "&#x00c9;"},
                                      {"0x84", "&#x00d1;"},
                                      {"0x87", "&#x00e1;"},
                                      {"0x8e", "&#x00e9;"},
                                      {"0x92", "&#x00ed;"},
                                      {"0x96", "&#x00f1;"},
                                      {"0x97", "&#x00f3;"},
                                      {"0x9c", "&#x00fa;"},
                                      {"0xe7", "&#x00c1;"},
                                      {"0xea", "&#x00cd;"},
                                      {"0xee", "&#x00d3;"},
                                      {"0xf2", "&#x00da;"}
                                  };

            return charMapping.ContainsKey(c)
                       ? charMapping[c]
                       : c;
        }

        public string Extract(Stream stream)
        {
            string text;

            using (var reader = new StreamReader(stream))
                text = reader.ReadToEnd();

            if (string.IsNullOrEmpty(text))
                return "";

            if (text.Length > 1024*1024)
            {
                text = Regex.Replace(text, "[\r\n]", "");
                text = Regex.Replace(text, "#[0-9a-f]{128,}#is", "");
            }

            text = text.Replace("\\'3f", "?");
            text = text.Replace("\\'3F", "?");
            text = text.Replace("\r\n", "");

            var document = new StringBuilder();
            var lastWordType = WordType.ControlWord;
            var i = 0;

            while (i < text.Length)
            {
                var word = ReadWord(text, i, OpenNewGroup, CloseGroup);

                i += word.Offset;

                if (word.Text == " ")
                {
                    var nextWord = ReadWord(text, i + 1, () => { }, () => { });

                    if (nextWord.Text == " " || lastWordType == WordType.Word && (nextWord.WordType == WordType.Word || nextWord.WordType == WordType.CloseTag))
                        document.Append(word.Text);
                }
                else if (string.IsNullOrEmpty(word.Text) == false)
                    document.Append(word.Text);

                lastWordType = word.WordType;

                i++;
            }

            return WebUtility.HtmlDecode(document.ToString());
        }
        
        private void OpenNewGroup()
        {
            if (_groupsStack.Count == 0)
                _groupsStack.Push(new Dictionary<string, object>());
            else
            {
                var newGroup = _groupsStack.Peek()
                                           .ToDictionary(dictionary => dictionary.Key,
                                                         dictionary => dictionary.Value);

                _groupsStack.Push(newGroup);
            }
        }

        private void CloseGroup()
        {
            _groupsStack.Pop();
        }

        private RtfWord ReadWord(string text, int i, Action newGroup, Action closeGroup)
        {
            var symbol = text[i];

            switch (symbol)
            {
                case '\\':
                    var word = ReadControlWord(_groupsStack.Peek(), text, i);
                    word.Offset++;
                    return word;
                case '{':
                    newGroup();
                    return new RtfWord(WordType.OpenTag);
                case '}':
                    closeGroup();
                    return new RtfWord(WordType.CloseTag);
                case '\0':
                case '\r':
                case '\f':
                case '\b':
                case '\t':
                case '\n':
                    break;
                default:
                    if (IsPlainText(_groupsStack.Peek()))
                            return new RtfWord(symbol.ToString());
                    break;
            }

            return new RtfWord(WordType.Word);
        }

        private RtfWord ReadControlWord(Dictionary<string, object> words, string text, int i)
        {
            var nextChar = text[i + 1];

            if (nextChar == '\\' && IsPlainText(words)) return new RtfWord("\\");
            if (nextChar == '~' && IsPlainText(words)) return new RtfWord(" ");
            if (nextChar == '_' && IsPlainText(words)) return new RtfWord("-");

            if (nextChar == '*')
            {
                words["*"] = true;
                return new RtfWord(WordType.Word);
            }
            
            if (nextChar == '\'')
            {
                var hex = text.Substring(i + 2, 2);

                if (IsPlainText(words))
                {
                    if (words.ContainsKey("mac") || words.ContainsKey("f") && _fonts[(string) words["f"]] == 77)
                        return new RtfWord(FromMacRoman(string.Format("0x{0}", hex)))
                                   {
                                       Offset = 2
                                   };

                    if (words.ContainsKey("ansicpg") && ((string) words["ansicpg"]).StartsWith("125")
                        || words.ContainsKey("lang") && (string) words["lang"] == "1029")
                        return new RtfWord(Encoding.Default.GetString(new[] {(byte) Convert.ToInt32(hex, 16)}))
                                   {
                                       Offset = 2
                                   };

                    return new RtfWord("&#x" + hex + ";"){
                        Offset = 2};
                }
                
                return new RtfWord(WordType.Word)
                           {
                               Offset = 2
                           };
            }
            
            if (nextChar >= 'a' && nextChar <= 'z' || nextChar >= 'A' && nextChar <= 'Z')
                return ParseControlWord(i, text, words);
            
            return new RtfWord(" ");
        }

        private RtfWord ParseControlWord(int i,string text,Dictionary<string,object>  words)
        {
            var controlWord = new RtfWord(WordType.ControlWord);

            var m = 0;
            var word = "";
            string param = null;
            
            for (var k = i + 1; k < text.Length; k++, m++)
            {
                var nextChar = text[k];
                
                if (nextChar >= 'a' && nextChar <= 'z' || nextChar >= 'A' && nextChar <= 'Z')
                {
                    if (string.IsNullOrEmpty(param))
                        word += nextChar;
                    else
                        break;
                }
                else if (nextChar >= '0' && nextChar <= '9')
                    param += nextChar;
                else if (nextChar == '-')
                {
                    if (string.IsNullOrEmpty(param))
                        param += nextChar;
                    else
                        break;
                }
                else
                    break;
            }

            controlWord.Offset= m - 1;

            var toText = "";

            switch (word.ToLowerInvariant())
            {
                case "u":
                    toText += ((char)int.Parse(param)).ToString();
                    var ucDelta = words.ContainsKey("uc")
                                      ? int.Parse((string)words["uc"])
                                      : 1;

                    if (ucDelta > 0)
                        controlWord.Offset = ucDelta;
                    break;
                case "par":
                case "page":
                case "column":
                case "line":
                case "lbr":
                    toText += "\n";
                    break;
                case "emspace":
                case "enspace":
                case "qmspace":
                    toText += " ";
                    break;
                case "tab":
                    toText += "\t";
                    break;
                case "chdate":
                case "chdpl":
                case "chdpa":
                    toText += DateTime.Today.ToShortDateString();
                    break;
                case "chtime":
                    toText += DateTime.Today.ToShortTimeString();
                    break;
                case "emdash":
                    toText += "—";
                    break;
                case "endash":
                    toText += "–";
                    break;
                case "bullet":
                    toText += "";
                    break;
                case "lquote":
                    toText += "‘";
                    break;
                case "rquote":
                    toText += "’";
                    break;
                case "ldblquote":
                    toText += "«";
                    break;
                case "rdblquote":
                    toText += "»";
                    break;

                case "bin":
                    controlWord .Offset= param.Length;
                    break;

                case "fcharset":
                    _fonts[(string)words["f"]] = int.Parse(param);
                    break;

                default:
                    words[word.ToLowerInvariant()] = string.IsNullOrEmpty(param)
                                                         ? (dynamic)true
                                                         : param;
                    break;
            }

            controlWord.Text = IsPlainText(words)
                                ? toText
                                : string.Empty;

            return controlWord;
        }
    }
}