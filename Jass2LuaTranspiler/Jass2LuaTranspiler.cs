using System.Text.RegularExpressions;

//extracted JS-only & removed "zinc" from https://raw.githubusercontent.com/BribeFromTheHive/vJass2Lua/refs/heads/main/index.html
public static class Transpiler
{
    public static string deleteEmmies(string str)
    {
        return ReplaceStr(str, new Regex(@"---@\S+\s+\S+"), "");
    }

    public static string deleteLineBreaks(string str)
    {
        return ReplaceStr(str, new Regex("(\\r?\\n)(?: *\\r?\\n)*"), "$1");
    }

    public static string buildvJassVarFinder(string whichWord, bool capture = true)
    {
        string result = "\\$?\\b" + whichWord + "\\b\\$?";
        if (capture)
        {
            result = "(" + result + ")";
        }
        return result;
    }

    private static readonly string rawvJassVar = "[A-Za-z][\\w]*";
    private static readonly string capturevJassVar = buildvJassVarFinder(rawvJassVar); //captures a vJass variable name to a group.

    private static readonly List<string> ignoredKeywords = new List<string>
    {
        "type","return","returns","endif","elseif",
        "endwhile","extends","array","static","method","not","and","or",
        "function","module","implement","library","requires","scope",
        "optional","if","else","then","while","true","false","nil","do",
        "end","endfunction","endmethod","type","repeat","until","local",
        "constant","public","private","readonly","for","in","break"
    };

    private static readonly Regex replaceArray1 = new Regex("^ *(" + rawvJassVar + ") +array +(" + rawvJassVar + ")(.*)", RegexOptions.Multiline);
    private static readonly string seekArray = " *\\[ *(\\d+) *\\]";
    private static readonly Regex replaceArray2 = new Regex("^" + seekArray + seekArray, RegexOptions.Multiline);
    private static readonly Regex replaceArray3 = new Regex("^" + seekArray, RegexOptions.Multiline);
    private static readonly Regex replaceVar1 = new Regex("^ *(" + rawvJassVar + ")( +)(" + rawvJassVar + ")(.*)", RegexOptions.Multiline);

    private static readonly Regex seekComment = new Regex("•");
    private static readonly string seekLineBreak = "\\r?\\n";
    private static readonly Regex seekLineBreakR = new Regex(seekLineBreak);

    public static string ReplaceStr(string str, Regex regexPattern, string replacement)
    {
        return regexPattern.Replace(str, replacement);
    }

    public static string ReplaceStr(string str, Regex regexPattern, MatchEvaluator evaluator)
    {
        return regexPattern.Replace(str, evaluator);
    }

    public static string RepeatActionOnString(string str, Func<string, string> action)
    {
        string tempStr = "";
        while (tempStr != str)
        {
            tempStr = str;
            str = action(str);
        }
        return str;
    }

    public static string parseVar(string line, bool isLocal = false)
    {
        //check for array declarations, first
        string newLine = ReplaceStr(line, replaceArray1,
            (Match match) =>
            {
                //let tlen = " ".repeat(type.length) + "      "; //##formatting
                string type = match.Groups[1].Value;
                string name = match.Groups[2].Value;
                string remainder = match.Groups[3].Value;
                string rawtype = type;
                type = " ---@type " + type;
                string result = ReplaceStr(remainder, replaceArray2, (Match m2) =>
                {
                    string width = m2.Groups[1].Value;
                    string height = m2.Groups[2].Value;
                    return $"{name}=vJass.array2D({width}, {height}){type}[][] ";
                });
                type += "[]";
                if (result == remainder)
                {
                    result = ReplaceStr(remainder, replaceArray3, (Match m3) =>
                    {
                        string size = m3.Groups[1].Value;
                        return $"{name}={{size={size}}}{type} ";
                    });
                    if (result == remainder)
                    {
                        string arrayType;
                        switch (rawtype)
                        {
                            case "integer":
                                addToIntStack(name);
                                goto case "real";
                            case "real":
                                arrayType = "0"; break;
                            case "boolean":
                                arrayType = "false"; break;
                            case "string":
                                arrayType = "\"\""; break;
                            default:
                                arrayType = "{}";
                                break;
                        }
                        if (arrayType != "{}")
                        {
                            arrayType = "__jarray(" + arrayType + ")";
                        }
                        result = name + "=" + arrayType + type + " " + remainder;
                    }
                }
                return result;
            }
        );
        if (newLine != line)
        {
            return newLine; //array has been parsed
        }
        return ReplaceStr(line, replaceVar1,
            (Match match) =>
            {
                string type = match.Groups[1].Value;
                string tlen = match.Groups[2].Value;
                string name = match.Groups[3].Value;
                string remainder = match.Groups[4].Value;

                string tail = "";
                int hasComment = seekComment.Match(remainder).Index;
                tlen += new string(' ', type.Length);
                if (hasComment >= 0)
                {
                    tail = remainder.Substring(hasComment);
                    remainder = remainder.Substring(0, hasComment);
                }
                if (type == "integer") addToIntStack(name);
                else if (type == "key")
                {
                    remainder = "=vJass.key()" + remainder;
                    type = "integer";
                    tlen = "";
                }

                tail = " ---@type " + type + " " + tail;
                int isSet = Regex.Match(remainder, "^ *=").Index;
                if (isSet < 0)
                {
                    if (isLocal)
                        return name + tail; //local variable declaration does not need assignment.
                    return name + "=nil" + tail; //global variable declaration needs assignment to be valid syntax.
                }
                return name + tlen + remainder + tail; //variable with assignment has been parsed
            }
        );
    }

    private static readonly Regex findIsolatedVar = new Regex("([\\w$]+) +([\\w$]+)(.*)", RegexOptions.Compiled);
    private static readonly Regex findIsolatedArray = new Regex("([\\w$]+) +array +([\\w$]+)(.*)", RegexOptions.Compiled);

    public static string parseIsolatedVar(string prefix, string wholeMatch, string w1, string w2, int index)
    {
        if (ignoredKeywords.Contains(w1) || ignoredKeywords.Contains(w2)) return wholeMatch;
        return prefix + parseVar(wholeMatch);
    }

    private static bool deleteComments = false;

    //"window" works similarly to Lua's "_G" table in that you can use concatenation to generate variable names dynamically.
    private static Dictionary<string, List<string>> packages = new Dictionary<string, List<string>>();
    private static Dictionary<string, Action<string>> packageInserters = new Dictionary<string, Action<string>>();
    private static Dictionary<string, Func<string, string>> packageUnpackers = new Dictionary<string, Func<string, string>>();

    public static void declarePackage(string whichType, string encoding, string inlinedEncoding = "•")
    {
        List<string> array = new List<string>();
        encoding = inlinedEncoding + '#' + encoding + '#';

        if (!packages.ContainsKey(whichType))
        {
            packages[whichType] = array;
        }
        packageInserters["insert" + whichType] = (str) =>
        {
            if (whichType == "Comment")
            {
                if (deleteComments) return;
                str = "--" + str;
            }
            array.Add(str);
        };

        packageUnpackers["unpack" + whichType] = (str) =>
        {
            if (array.Count > 0)
            {
                Regex finder = new Regex(Regex.Escape(encoding) + "(\\d+)", RegexOptions.Compiled);
                return ReplaceStr(str, finder, (Match m) =>
                {
                    return array[int.Parse(m.Groups[1].Value)];
                });
            }
            return str;
        };
    }

    public static void insertComment(string str)
    {
        packageInserters["insertComment"](str);
    }

    public static void insertString(string str)
    {
        packageInserters["insertString"](str);
    }

    public static void insertRawcode(string str)
    {
        packageInserters["insertRawcode"](str);
    }

    public static string unpackComment(string str)
    {
        return packageUnpackers["unpackComment"](str);
    }

    public static string unpackString(string str)
    {
        return packageUnpackers["unpackString"](str);
    }

    public static string unpackRawcode(string str)
    {
        return packageUnpackers["unpackRawcode"](str);
    }

    public static string insertBlockComment(string comment)
    {
        insertComment("[[" + comment + "]]");
        return "";
    }

    public static int getEndBracketIndex(string bracketStr, char openChar, char closeChar)
    {
        int depth = 0;
        for (int i = 0; i < bracketStr.Length; ++i)
        {
            switch (bracketStr[i])
            {
                case var c when c == openChar:
                    depth++;
                    break;
                case var c when c == closeChar:
                    if (--depth <= 0) return i + 1;
                    break;
            }
        }
        return -1; //no endbrackets were found.
    }

    private static List<string> intStack = new List<string>();

    public static bool isVarInt(string varName)
    {
        return intStack.IndexOf(varName) >= 0;
    }

    public static void addToIntStack(string varName)
    {
        if (!isVarInt(varName))
        {
            intStack.Add(varName);
        }
    }

    public class Options
    {
        public int spacesPerIndent = 4;
        public bool deleteComments = false;
        public bool commentDebugLines = true;
        public bool deleteExtraLineBreaks = true;
        public bool deleteEmmyAnnotations = true;
        public bool avoidRepeatUntilLoops = false; //temporary workaround
        public bool addGithubAttributionLink = true;
    }

    public static string parseScript(string vJassSource, Options options)
    {
        declarePackage("Comment", "cmt");
        declarePackage("String", "str", "`");
        declarePackage("Rawcode", "fcc", "`");

        int userDefinedSpacing = options.spacesPerIndent;
        string indentation = new string(' ', userDefinedSpacing);

        deleteComments = options.deleteComments;
        bool commentDebug = options.commentDebugLines;

        bool noRepeatUntil = options.avoidRepeatUntilLoops;
        string parsing = vJassSource;

        parsing = RepeatActionOnString(parsing, (str) =>
            ReplaceStr(str, new Regex("^([^\\r\\n\\t]*)\\t", RegexOptions.Multiline),
            (Match m) =>
            {
                string leadingChars = m.Groups[1].Value;
                int len = leadingChars.Length % userDefinedSpacing;
                len = userDefinedSpacing - len;
                return leadingChars + new string(' ', len);
            })
        );

        parsing = ReplaceStr(parsing, new Regex("^ *\\/\\/\\! *novjass\\b.*?^ *\\/\\/\\! *\\bendnovjass\\b", RegexOptions.Singleline | RegexOptions.Multiline),
            (Match m) => insertBlockComment("\n" + m.Value)
        );

        parsing = ReplaceStr(parsing, new Regex("(?<!^.*\\/\\/.*)(\"(?:[^\"\\\\]|\\\\\"|\\\\\\\\)*\")", RegexOptions.Multiline),
            (Match m) =>
            {
                insertString(m.Groups[1].Value);
                return "";
            }
        );

        parsing = ReplaceStr(parsing, new Regex("'(?:[^'\\\\]|\\\\'|\\\\\\\\){4}'"),
            (Match m) =>
            {
                insertRawcode("FourCC(" + m.Value + ")");
                return "";
            }
        );

        parsing = ReplaceStr(parsing, new Regex("^([^/]?)\\/\\/", RegexOptions.Multiline),
            "$1--"
        );

        parsing = ReplaceStr(parsing, new Regex("\\/\\/(.*)"),
            (Match m) =>
            {
                insertComment(m.Groups[1].Value);
                return "";
            }
        );

        parsing = ReplaceStr(parsing,
            new Regex("\\/\\*\\*((?:(?!\\*\\/).)*?)\\*\\/( *•.*?)*$", RegexOptions.Singleline),
            (Match m) =>
            {
                string a = m.Groups[1].Value;
                string b = m.Groups[2].Value;
                if (b == null) b = "";
                insertBlockComment(a);
                return b;
            }
        );

        parsing = ReplaceStr(parsing,
            new Regex("([;} ]*)\\/\\*((?:(?!\\*\\/).)*?)\\*\\/", RegexOptions.Singleline),
            (Match m) =>
            {
                string a = m.Groups[1].Value;
                string b = m.Groups[2].Value;
                insertBlockComment(b);
                return a;
            }
        );

        parsing = ReplaceStr(parsing, new Regex("\\/\\*(?:(?!\\*\\/).)*?\\*\\/", RegexOptions.Singleline),
            ""
        );

        parsing = ReplaceStr(parsing, new Regex("^ *native\\b.*", RegexOptions.Multiline),
            (Match m) =>
            {
                insertComment(m.Value);
                return "";
            }
        );

        parsing = ReplaceStr(parsing, new Regex("\\b(?:do|in|end|nil|repeat|until)\\b"),
            "$&_"
        );

        parsing = ReplaceStr(parsing, new Regex("([\\w\\$]+):([\\w\\$]+)"), "$2[$1]");
        parsing = ReplaceStr(parsing, new Regex("\\bnull\\b"), "nil");
        parsing = ReplaceStr(parsing, new Regex("!="), "~=");
        parsing = ReplaceStr(parsing, new Regex("^( *)debug +(?:(?:set|call) *)?", RegexOptions.Multiline),
            (Match m) =>
            {
                string indent = m.Groups[1].Value;
                if (commentDebug) indent += "--debug ";
                return indent;
            }
        );

        parsing = ReplaceStr(parsing, new Regex("^( *)(?:set|call|constant) +", RegexOptions.Multiline), "$1");
        parsing = ReplaceStr(parsing, new Regex("^( *end)if", RegexOptions.Multiline), "$1");
        parsing = ReplaceStr(parsing, new Regex("^( *)static +if\\b", RegexOptions.Multiline), "$1if");
        parsing = ReplaceStr(parsing, new Regex("\\.exists\\b"), "");
        parsing = ReplaceStr(parsing, new Regex("'\\\\?(.)'"),
            (Match m) =>
            {
                char c = m.Groups[1].Value[0];
                return ((int)c).ToString();
            }
        );
        parsing = ReplaceStr(parsing, new Regex("\\\" *\\+"), "\"..");
        parsing = ReplaceStr(parsing, new Regex("\\+ *\\\""), "..\"");

        parsing = ReplaceStr(parsing,
            new Regex("(.)\\.([\\$\\w]+) *\\(", RegexOptions.Multiline),
            (Match mm) =>
            {
                string firstChar = mm.Groups[1].Value;
                string methodCaller = mm.Groups[2].Value;
                if (firstChar == " ") firstChar = " self";
                return firstChar + ":" + methodCaller + "(";
            }
        );

        parsing = ReplaceStr(parsing,
            new Regex("^( *)(?:private\\.|public\\.)* *type +(\\w+) +extends +(\\w+) +array *\\[ *(\\d+) *\\]", RegexOptions.Multiline),
            "$1local $2 = Struct();$2.size = $4 ---@type $3[]"
        );

        parsing = ReplaceStr(parsing,
            new Regex("^( *)interface\\b +([\\$\\w]+)(.*?)^ *endinterface", RegexOptions.Singleline | RegexOptions.Multiline),
            "$1Struct $2 = vJass.interface(true)\n$1--[[$3$1]]"
        );

        parsing = ReplaceStr(parsing,
            new Regex("^( *local +)(.*)", RegexOptions.Multiline),
            (Match mm) =>
            {
                string local = mm.Groups[1].Value;
                string line = mm.Groups[2].Value;
                return local + parseVar(line, true);
            }
        );

        parsing = ReplaceStr(parsing,
            new Regex("^( *)(private|public)(?: +constant)?\\b +function *([\\$\\w]+)(.*?^ *endfunction)", RegexOptions.Singleline | RegexOptions.Multiline),
            (Match m) =>
            {
                string indent = m.Groups[1].Value;
                string scope = m.Groups[2].Value;
                string name = m.Groups[3].Value;
                string body = m.Groups[4].Value;
                body = indent + "local function " + name + body;
                if (scope == "public")
                {
                    return body + $"\n{indent}_G[SCOPE_PREFIX..'{name}'] = {name}";
                }
                return body;
            }
        );

        parsing = ReplaceStr(parsing, new Regex("^( *)private +keyword\\b", RegexOptions.Multiline), "$1local");
        parsing = ReplaceStr(parsing, new Regex("\\$([0-9a-fA-F]+[^\\$])"), "0x$1");
        parsing = ReplaceStr(parsing, new Regex("^( *)hook +(\\w+) +(\\w*(?:\\.\\w+)*)", RegexOptions.Multiline), "$1vJass.hook(\"$2\", $3)");

        parsing = ReplaceStr(parsing,
            new Regex("^( *)library +(\\w+) *(?:initializer *(\\w*))?(.*?)endlibrary", RegexOptions.Singleline | RegexOptions.Multiline),
            (Match m) =>
            {
                string indent = m.Groups[1].Value;
                string name = m.Groups[2].Value;
                string init = m.Groups[3].Value;
                string body = m.Groups[4].Value;
                body = $"{indent}do  LIBRARY_{name} = true\n{indent}{indentation}local SCOPE_PREFIX = \"{name}_\" ---@type string {body}";
                if (init != null)
                {
                    body += indent + indentation + "OnGlobalInit(" + init + ")\n";
                }
                return body + "\n" + indent + "end";
            }
        );

        parsing = ReplaceStr(parsing,
            new Regex("^( *)(private|public)? *scope +(\\w+) *(?:initializer *(\\w*))?(.*?)endscope", RegexOptions.Singleline | RegexOptions.Multiline),
            (Match m) =>
            {
                string indent = m.Groups[1].Value;
                string scope = m.Groups[2].Value;
                string name = m.Groups[3].Value;
                string init = m.Groups[4].Value;
                string body = m.Groups[5].Value;
                if (scope != null)
                {
                    name = "SCOPE_PREFIX..\"" + name;
                }
                else
                {
                    name = "\"" + name;
                }
                body = $"{indent}do\n{indent}{indentation}local SCOPE_PREFIX = {name}_\" ---@type string {body}";
                if (init != null)
                {
                    body += indent + indentation + "OnTrigInit(" + init + ")\n";
                }
                return body + "\n" + indent + "end";
            }
        );

        parsing = RepeatActionOnString(parsing, (str) =>
        {
            return ReplaceStr(str,
                new Regex("^( *)(loop\\b((?!\\bendloop\\b|\\bloop\\b).)*\\bendloop)", RegexOptions.Singleline),
                (Match mm) =>
                {
                    string indent = mm.Groups[1].Value;
                    string contents = mm.Groups[2].Value;
                    contents = ReplaceStr(contents,
                        new Regex("^loop\\s+exitwhen *([^\\r\\n•]*)(.*end)loop", RegexOptions.Singleline),
                        (Match m2) =>
                        {
                            string cond = m2.Groups[1].Value;
                            string cont = m2.Groups[2].Value;
                            string original = cond;
                            cond = ReplaceStr(cond,
                                new Regex("^ *([\\w$]+) *([\\<\\>\\=\\~]{1,2}) *([\\w$]+) *$"),
                                (Match mm2) =>
                                {
                                    string compare = mm2.Groups[2].Value;
                                    switch (compare)
                                    {
                                        case "<": compare = ">="; break;
                                        case ">": compare = "<="; break;
                                        case "<=": compare = ">"; break;
                                        case ">=": compare = "<"; break;
                                        case "~=": compare = "=="; break;
                                        default: compare = "~="; break;
                                    }
                                    return mm2.Groups[1].Value + " " + compare + " " + mm2.Groups[3].Value;
                                }
                            );
                            if (cond != original)
                            {
                                return "while " + cond + " do " + cont;
                            }
                            return "while not (" + cond + ") do " + cont;
                        }
                    );
                    if (!noRepeatUntil)
                    {
                        contents = ReplaceStr(contents,
                            new Regex("^loop(.*)\\r?\\n *exitwhen([^\\n\\r•]*)(\\s*? *)endloop", RegexOptions.Singleline),
                            "repeat$1$3until$2"
                        );
                    }
                    contents = ReplaceStr(contents,
                        new Regex("^loop\\b(.*end)loop", RegexOptions.Singleline),
                        "while true do$1"
                    );
                    contents = ReplaceStr(contents,
                        new Regex("^( *)exitwhen\\b([^\\r\\n•]*)", RegexOptions.Multiline),
                        "$1if$2 then break end"
                    );
                    contents = ReplaceStr(contents,
                        new Regex("^( *)if *true *then break end", RegexOptions.Multiline),
                        "$1break"
                    );
                    return indent + contents;
                }
            );
        });

        parsing = ReplaceStr(parsing,
            new Regex("^ *globals\\b(.*?)\\bendglobals\\b", RegexOptions.Singleline),
            (Match mm) =>
            {
                string globals = mm.Groups[1].Value;
                globals = ReplaceStr(globals,
                    new Regex("^( *)private(?: +constant)*\\b", RegexOptions.Multiline),
                    "$1local"
                );
                globals = ReplaceStr(globals,
                    new Regex("^( *)public +constant +([\\$\\w]+) +([\\$\\w]+)([^\\n\\r]*)", RegexOptions.Multiline),
                    "$1local $2 $3$4\n$1_G[SCOPE_PREFIX..\"$3\"] = $3"
                );
                globals = ReplaceStr(globals,
                    new Regex("^( *)public +([\\$\\w]+) +([\\$\\w]+)\\b([^\\n\\r]*)", RegexOptions.Multiline),
                    "$1local $2 $3$4\n$1GlobalRemap(SCOPE_PREFIX..\"$3\", function() return $3 end, function(val) $3 = val end)"
                );
                globals = ReplaceStr(globals,
                    new Regex("^( *(local +)*)(.*)", RegexOptions.Multiline),
                    (Match m2) =>
                    {
                        string prefix = m2.Groups[1].Value;
                        bool isLocal = prefix.Contains("local");
                        string remainder = m2.Groups[3].Value;
                        return prefix + parseVar(remainder, isLocal);
                    }
                );
                return globals;
            }
        );

        Regex macroHasArgs = new Regex("^ takes *(.*)", RegexOptions.Multiline);
        Regex macroWrapArgs = new Regex("\\b\\w+\\b", RegexOptions.Compiled);
        parsing = ReplaceStr(parsing,
            new Regex("^( *)\\-\\-\\! *textmacro +(\\w+)(.*?)^ *\\-\\-\\! *endtextmacro", RegexOptions.Singleline | RegexOptions.Multiline),
            (Match mm) =>
            {
                string indent = mm.Groups[1].Value;
                string name = mm.Groups[2].Value;
                string body = mm.Groups[3].Value;

                string statements = body;
                Match matchTake = macroHasArgs.Match(statements);
                if (matchTake.Success)
                {
                    statements = macroHasArgs.Replace(statements, "$1");
                    int linebreak = -1;
                    Match lbMatch = seekLineBreakR.Match(statements);
                    if (lbMatch.Success) linebreak = lbMatch.Index;

                    if (linebreak >= 0)
                    {
                        string head = statements.Substring(0, linebreak);
                        body = statements.Substring(linebreak);
                        statements = head;
                    }

                    statements = macroWrapArgs.Replace(statements, (Match m2) => "\"" + m2.Value + "\"");
                    return indent + "vJass.textmacro(\"" + name + "\", {" + statements + "}, [[" + body + indent + "]])";
                }
                return indent + $"vJass.textmacro(\"{name}\", nil, function(thistype){body}{indent}end)";
            }
        );

        parsing = ReplaceStr(parsing,
            new Regex("^( *)\\-\\-\\! *runtextmacro +(?:optional)* *(\\w+) *\\((.*?)\\)", RegexOptions.Multiline),
            "$1vJass.runtextmacro(\"$2\", $3)"
        );

        Regex isStructMember = new Regex("^( *)(static|readonly|public|private|method) +(.*)", RegexOptions.Multiline);

        parsing = ReplaceStr(parsing,
            new Regex("^( *)(private|public)* *(struct|module) *([\\$\\w]+) *(.*?^ *end)(?:struct|module)", RegexOptions.Singleline | RegexOptions.Multiline),
            (Match mm) =>
            {
                string indent = mm.Groups[1].Value;
                string scope = mm.Groups[2].Value;
                string strOrMod = mm.Groups[3].Value;
                string name = mm.Groups[4].Value;
                string body = mm.Groups[5].Value;

                int linebreak = -1;
                Match lbm = seekLineBreakR.Match(body);
                if (lbm.Success) linebreak = lbm.Index;
                string head = "";
                if (linebreak >= 0)
                {
                    head = body.Substring(0, linebreak);
                    body = body.Substring(linebreak);
                }
                bool isModule = (strOrMod == "module");
                body = ReplaceStr(body, new Regex("\\bstub\\s+"), "");

                body = ReplaceStr(body, isStructMember, (Match matchMember) =>
                {
                    string indent2 = matchMember.Groups[1].Value;
                    string scope2 = matchMember.Groups[2].Value;
                    string line = matchMember.Groups[3].Value;
                    if (scope2 != "static")
                    {
                        line = ReplaceStr(line, new Regex("^( *)static *(?:constant +)*"), "$1");
                    }
                    bool isMethod = (scope2 == "method");
                    if (!isModule || scope2 != "private")
                    {
                        if (isMethod) scope2 = "thistype";
                        else scope2 = "thistype";
                    }
                    if (isMethod || line.StartsWith("method"))
                    {
                        line = ReplaceStr(line, new Regex("^(?:method)* *\\b([\\$\\w]+)\\b"),
                            (Match mm2) =>
                            {
                                if (mm2.Groups[1].Value != "operator")
                                {
                                    return "function " + scope2 + ":" + mm2.Groups[1].Value;
                                }
                                else
                                {
                                    return "";
                                }
                            }
                        );
                        if (line.Contains("function thistype:_operator"))
                        {
                            // do nothing
                        }
                        line += "\n" + indent2 + indentation + "local _ENV = Struct.environment(self)";
                    }
                    else
                    {
                        line = scope2 + '.' + parseVar(line);
                    }
                    return indent2 + line;
                });

                body = ReplaceStr(body, new Regex("( *end)method"), "$1function");
                if (isModule)
                {
                    if (scope == null) scope = "";
                    head = $"vJass.module(\"{name}\", \"{scope}\", SCOPE_PREFIX, function(private, thistype)" + head;
                    body += ")";
                }
                else
                {
                    head = name + " = Struct() --" + head;
                    head = ReplaceStr(head,
                        new Regex("\\(\\) -- *extends +([\\$\\w]+)"),
                        (Match mm2) =>
                        {
                            string extended = mm2.Groups[1].Value;
                            if (extended == "array") return "()";
                            return $"({extended}) --";
                        }
                    );
                    if (scope != null)
                    {
                        head = "local " + head;
                        if (scope == "public")
                        {
                            head += "\n" + indent + "_G[SCOPE_PREFIX..\"" + name + "\"] = " + name;
                        }
                    }
                }

                body = ReplaceStr(body, new Regex("\\bthis\\b"), "self");
                body = ReplaceStr(body,
                    new Regex("([^\\w\\d\\]\\)])\\." + capturevJassVar, RegexOptions.Multiline),
                    "$1self.$2"
                );
                body = ReplaceStr(body,
                    new Regex("^( *)implement +(?:optional *)*" + capturevJassVar, RegexOptions.Multiline),
                    "$1vJass.implement(\"$2\", SCOPE_PREFIX, thistype)"
                );
                Func<string, string> parseStructVar = (string matchAll) =>
                {
                    return matchAll;
                };
                body = RepeatActionOnString(body, (bb) =>
                {
                    bb = findIsolatedArray.Replace(bb, (Match ma) =>
                    {
                        return parseIsolatedVar("thistype.", ma.Value, ma.Groups[1].Value, ma.Groups[2].Value, ma.Index);
                    });
                    bb = findIsolatedVar.Replace(bb, (Match ma) =>
                    {
                        return parseIsolatedVar("thistype.", ma.Value, ma.Groups[1].Value, ma.Groups[2].Value, ma.Index);
                    });
                    return bb;
                });
                body = ReplaceStr(body,
                    new Regex("^( *)(public|private) *static(?: *constant)* +", RegexOptions.Multiline),
                    (Match mm2) =>
                    {
                        string indent2 = mm2.Groups[1].Value;
                        string scope2 = mm2.Groups[2].Value;
                        if (scope2 == "") scope2 = "thistype";
                        return indent2 + scope2 + ".";
                    }
                );
                if (!isModule)
                {
                    body = "do\n" + indent + indentation + "local thistype = " + name + body;
                }
                return indent + head + "\n" + indent + body;
            }
        );

        parsing = ReplaceStr(parsing, new Regex("^( *)(?:public|private)* *function interface", RegexOptions.Multiline), "$1---@class");

        parsing = ReplaceStr(parsing,
            new Regex("([\\w\\$\\.]+)[\\:\\.](name|(?:execute|evaluate))\\b *(\\(([^()]*)\\))?"),
            (Match mm) =>
            {
                string name = mm.Groups[1].Value;
                string reference = mm.Groups[2].Value;
                string hasArgs = mm.Groups[3].Value;
                if (name == "vJass") return mm.Value;
                if (reference == "name") return "vJass.name(" + name + ")";
                if (string.IsNullOrEmpty(hasArgs)) return mm.Value;
                return "vJass." + reference + "(" + name + ", " + mm.Groups[4].Value + ")";
            }
        );

        Regex isNothing = new Regex("\\bnothing\\b");
        Regex getArgPairs = new Regex("(" + rawvJassVar + ") +(" + rawvJassVar + ")", RegexOptions.Compiled);
        Regex getFunction = new Regex("^( *)((?:[\\w\\$:\\[\\]\\=]+ +)+?|[^\\r\\n]*?\\bfunction )\\btakes +([\\$\\w, ]+ +)*?\\breturns +([\\$\\w]+)(.*?\\bend)function\\b", RegexOptions.Singleline | RegexOptions.Multiline);
        Regex findReturn = new Regex("return.+\\/.+");
        Regex findSetter = new Regex("([\\$\\w]+) *=[^\\n\\r•]*");
        Regex replaceDiv = new Regex("([^/])\\/([^/])");

        string DoFloorInt(string line2)
        {
            return ReplaceStr(line2, replaceDiv, (Match mm2) =>
            {
                return mm2.Groups[1].Value + "//" + mm2.Groups[2].Value;
            });
        }

        parsing = ReplaceStr(parsing,
            new Regex("^( *)(?:(private|public) +)?type +([\\w\\$]+) +extends(.*)", RegexOptions.Multiline),
            (Match matchFailed) =>
            {
                string indent = matchFailed.Groups[1].Value;
                string scope = matchFailed.Groups[2].Value;
                string word = matchFailed.Groups[3].Value;
                string remainder = matchFailed.Groups[4].Value;
                if (remainder.IndexOf("function", StringComparison.Ordinal) >= 0)
                {
                    return "---@class " + word;
                }
                string size = null;
                remainder = ReplaceStr(remainder,
                    new Regex("^[^\\[]*\\[ *([^,\\] ]+)[^•\\r\\n]*(.*)", RegexOptions.Multiline),
                    (Match mm2) =>
                    {
                        size = mm2.Groups[1].Value;
                        return mm2.Groups[2].Value;
                    }
                );
                if (size != null)
                {
                    if (scope != null)
                    {
                        if (scope == "public")
                        {
                            remainder += "; _G[SCOPE_PREFIX.." + word + "] = " + word;
                        }
                        indent += "local ";
                    }
                    return indent + word + " = vJass.dynamicArray(" + size + ")" + remainder;
                }
                return "--PARSER ERROR WITH: " + matchFailed.Value;
            }
        );

        parsing = RepeatActionOnString(parsing, (str2) =>
        {
            return ReplaceStr(str2, getFunction,
                (Match gf) =>
                {
                    string indent = gf.Groups[1].Value;
                    string func = gf.Groups[2].Value;
                    string paramsStr = gf.Groups[3].Value;
                    string rtype = gf.Groups[4].Value;
                    string contents = gf.Groups[5].Value;
                    func = func.Substring(0, func.Length - 1);
                    string paramEmmy = "";
                    string returnEmmy = "";
                    string argsResult = "";

                    if (rtype != "nothing")
                    {
                        returnEmmy = indent + "---@return " + rtype + "\n";
                        if (rtype == "integer")
                        {
                            contents = ReplaceStr(contents, findReturn, (Match retLine) =>
                            {
                                return DoFloorInt(retLine.Value);
                            });
                        }
                    }

                    contents = ReplaceStr(contents,
                        new Regex("function *([\\$\\w]+(?:\\.[\\w\\$]+)? *[\\)\\,])"), "$1"
                    );

                    contents = ReplaceStr(contents, findSetter, (Match setterLine) =>
                    {
                        string varName = setterLine.Groups[1].Value;
                        if (isVarInt(varName))
                        {
                            return DoFloorInt(setterLine.Value);
                        }
                        else if (varName == "self")
                        {
                            return setterLine.Value + "; _ENV = Struct.environment(self)";
                        }
                        return setterLine.Value;
                    });

                    if (!isNothing.IsMatch(paramsStr))
                    {
                        argsResult = ReplaceStr(paramsStr, getArgPairs, (Match mm2) =>
                        {
                            paramEmmy += indent + "---@param " + mm2.Groups[2].Value + " " + mm2.Groups[1].Value + "\n";
                            return mm2.Groups[2].Value;
                        });
                        argsResult = argsResult.TrimEnd(' ');
                    }

                    return paramEmmy + returnEmmy + indent + func + "(" + argsResult + ")" + contents;
                }
            );
        });

        parsing = ReplaceStr(parsing, new Regex("endfunction"), "end");
        if (options.deleteEmmyAnnotations)
        {
            parsing = deleteEmmies(parsing);
        }

        parsing = unpackComment(parsing);
        parsing = unpackString(parsing);
        parsing = unpackRawcode(parsing);

        if (options.deleteExtraLineBreaks)
        {
            parsing = deleteLineBreaks(parsing);
        }

        if (options.addGithubAttributionLink)
        {
            parsing = "--https://github.com/speige/Jass2LuaTranspiler\n\n" + parsing;
        }
        return parsing;
    }
}
