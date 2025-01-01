using System.Text.RegularExpressions;

//extracted JS-only & removed "zinc" from https://raw.githubusercontent.com/BribeFromTheHive/vJass2Lua/refs/heads/main/betaconversions.html
public static class Transpiler
{
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

    private static bool deleteComments = false;
    private static List<string> intStack = new List<string>();

    private static bool isVarInt(string varName)
    {
        return intStack.IndexOf(varName) >= 0;
    }

    private static void addToIntStack(string varName)
    {
        if (!isVarInt(varName))
            intStack.Add(varName);
    }

    public static string ReplaceStr(string str, Regex regexPattern, MatchEvaluator evaluator)
    {
        return regexPattern.Replace(str, evaluator);
    }

    private static string ReplaceStr(string str, string regexPattern, string replacement, RegexOptions options = default)
    {
        return Regex.Replace(str, regexPattern, replacement, options);
    }

    private static string ReplaceStr(string str, string regexPattern, MatchEvaluator evaluator, RegexOptions options = default)
    {
        return ReplaceStr(str, new Regex(regexPattern, options), evaluator);
    }

    private static string RepeatActionOnString(string str, Func<string, string> action)
    {
        string tempStr = "";
        while (tempStr != str)
        {
            tempStr = str;
            str = action(str);
        }
        return str;
    }

    private static List<string> insertCommentArray = new List<string>();
    private static List<string> insertStringArray = new List<string>();
    private static List<string> insertRawcodeArray = new List<string>();

    private static string insertComment(string str)
    {
        if (deleteComments) return "";
        insertCommentArray.Add("--" + str);
        return "•#cmt#" + (insertCommentArray.Count - 1);
    }
    private static string insertBlockComment(string comment)
    {
        return insertComment("[[" + comment + "]]");
    }
    private static string insertString(string str)
    {
        insertStringArray.Add(str);
        return "•#str#" + (insertStringArray.Count - 1);
    }
    private static string insertRawcode(string str)
    {
        insertRawcodeArray.Add(str);
        return "•#fcc#" + (insertRawcodeArray.Count - 1);
    }

    private static string unpackComment(string str)
    {
        if (insertCommentArray.Count > 0)
        {
            var finder = new Regex("•#cmt#(\\d+)", RegexOptions.Compiled);
            return ReplaceStr(str, finder, m => insertCommentArray[int.Parse(m.Groups[1].Value)]);
        }
        return str;
    }

    private static string unpackString(string str)
    {
        if (insertStringArray.Count > 0)
        {
            var finder = new Regex("•#str#(\\d+)", RegexOptions.Compiled);
            return ReplaceStr(str, finder, m => insertStringArray[int.Parse(m.Groups[1].Value)]);
        }
        return str;
    }
    private static string unpackRawcode(string str)
    {
        if (insertRawcodeArray.Count > 0)
        {
            var finder = new Regex("•#fcc#(\\d+)", RegexOptions.Compiled);
            return ReplaceStr(str, finder, m => insertRawcodeArray[int.Parse(m.Groups[1].Value)]);
        }
        return str;
    }

    private static void declarePackage(string whichType, string encoding, string inlinedEncoding = "•")
    {
        var array = new List<string>();
        encoding = inlinedEncoding + "#" + encoding + "#";
        if (whichType == "Comment")
        {
            insertCommentArray = array;
        }
        else if (whichType == "String")
        {
            insertStringArray = array;
        }
        else if (whichType == "Rawcode")
        {
            insertRawcodeArray = array;
        }
    }

    static Transpiler()
    {
        declarePackage("Comment", "cmt");
        declarePackage("String", "str", "`");
        declarePackage("Rawcode", "fcc", "`");
    }

    private static string deleteEmmies(string str)
    {
        return ReplaceStr(str, @"---@.*", "");
    }

    private static string deleteLineBreaks(string str)
    {
        return ReplaceStr(str, @"(\r?\n)(?: *\r?\n)*", "$1");
    }

    private static string parseVar(string line, bool isLocal = false)
    {
        Func<string, string> RC2NF = (type) =>
        {
            if (type == "real")
                return "number";
            else if (type == "code")
                return "function";
            return type;
        };
        var replaceArray1 = new Regex("^ *([A-Za-z][\\w]*) +array +([A-Za-z][\\w]*)(.*)", RegexOptions.Multiline);
        var replaceArray2 = new Regex("^ *\\[ *(\\d+) *\\]\\[ *(\\d+) *\\]", RegexOptions.Multiline);
        var replaceArray3 = new Regex("^ *\\[ *(\\d+) *\\]", RegexOptions.Multiline);
        var replaceVar1 = new Regex("^ *([A-Za-z][\\w]*)( +)([A-Za-z][\\w]*)(.*)", RegexOptions.Multiline);

        string RC2NFline(string type, string lineToReplace, string name, string remainder)
        {
            if (type == "integer")
            {
                addToIntStack(name);
            }
            else if (type == "key")
            {
                remainder = "=vJass.key()" + remainder;
                type = "integer";
            }
            return lineToReplace + " ---@type " + RC2NF(type) + " " + remainder;
        }

        string newLine = ReplaceStr(line, replaceArray1, m =>
        {
            var type = m.Groups[1].Value;
            var name = m.Groups[2].Value;
            var remainder = m.Groups[3].Value;
            var rawtype = type;
            type = " ---@type " + RC2NF(type);

            var result = ReplaceStr(remainder, replaceArray2, mm =>
            {
                var width = mm.Groups[1].Value;
                var height = mm.Groups[2].Value;
                return $"{name}=vJass.array2D({width}, {height}){type}[][] ";
            });
            if (result == remainder)
            {
                result = ReplaceStr(remainder, replaceArray3, mm =>
                {
                    var size = mm.Groups[1].Value;
                    return $"{name}={{size={size}}}{type} ";
                });
                if (result == remainder)
                {
                    string arrayType;
                    switch (rawtype)
                    {
                        case "integer":
                            addToIntStack(name);
                            goto case "number";
                        case "number":
                            arrayType = "0";
                            break;
                        case "boolean":
                            arrayType = "false";
                            break;
                        case "string":
                            arrayType = "\"\"";
                            break;
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
        });
        if (newLine != line)
        {
            return newLine;
        }
        return ReplaceStr(line, replaceVar1, m =>
        {
            var type = m.Groups[1].Value;
            var tlen = m.Groups[2].Value;
            var name = m.Groups[3].Value;
            var remainder = m.Groups[4].Value;
            var tail = "";
            var hasComment = remainder.IndexOf('•');
            if (hasComment >= 0)
            {
                tail = remainder.Substring(hasComment);
                remainder = remainder.Substring(0, hasComment);
            }
            if (type == "integer")
            {
                addToIntStack(name);
            }
            else if (type == "key")
            {
                remainder = "=vJass.key()" + remainder;
                type = "integer";
                tlen = "";
            }
            tail = " ---@type " + RC2NF(type) + " " + tail;
            var isSet = Regex.Match(remainder, @"^ *\=", RegexOptions.Multiline);
            if (!isSet.Success)
            {
                if (isLocal) return name + tail;
                return name + "=nil" + tail;
            }
            return name + tlen + remainder + tail;
        });
    }

    private static string parseIsolatedVar(string prefix, string wholeMatch, string w1, string w2, string remainder, int index)
    {
        string[] ignoredKeywords = {
            "type","return","returns","endif","elseif","endwhile","extends","array","static","method",
            "not","and","or","function","module","implement","library","requires","scope","optional","if",
            "else","then","while","true","false","nil","do","end","endfunction","endmethod","type",
            "repeat","until","local","constant","public","private","readonly","for","in","break"
        };
        foreach (var kw in ignoredKeywords)
        {
            if (kw == w1 || kw == w2) return wholeMatch;
        }
        return prefix + parseVar(wholeMatch);
    }

    public static string parseScript(string script, Options options)
    {
        insertCommentArray = new List<string>();
        insertStringArray = new List<string>();
        insertRawcodeArray = new List<string>();

        var vJassSource = script;

        deleteComments = options.deleteComments;
        bool commentDebug = options.commentDebugLines;
        bool noRepeatUntil = options.avoidRepeatUntilLoops;
        int userDefinedSpacing = options.spacesPerIndent;
        string indentation = new string(' ', userDefinedSpacing);

        string parsing = vJassSource;

        parsing = RepeatActionOnString(parsing, s =>
            ReplaceStr(s, @"^([^\r\n\t]*)\t", m =>
            {
                var leadingChars = m.Groups[1].Value;
                int len = leadingChars.Length % userDefinedSpacing;
                len = userDefinedSpacing - len;
                return leadingChars + new string(' ', len);
            })
        );

        parsing = ReplaceStr(parsing, @"^ *\/\/\! *novjass\b.*?^ *\/\/\! *\bendnovjass\b", str => insertBlockComment("\n" + str), RegexOptions.Singleline | RegexOptions.Multiline);
        parsing = ReplaceStr(parsing, @""" *\+", "\"..");
        parsing = ReplaceStr(parsing, @"\+ *""", "..\"");
        parsing = ReplaceStr(parsing, @"(""(?:[^""\\]|\\""|\\[\\\w])*?"")", m => insertString(m.Groups[1].Value), RegexOptions.Multiline);
        parsing = ReplaceStr(parsing, @"'(?:[^'\\]|\\'|\\\\){4}'", str => insertRawcode("FourCC(" + str + ")"));
        parsing = ReplaceStr(parsing, @"^([^\/]?)\/\/\!", "$1--!");
        parsing = ReplaceStr(parsing, @"\/\/(.*)", m => insertComment(m.Groups[1].Value));
        parsing = ReplaceStr(parsing, @"\/\*((?:(?!\*\/).)*?)\*\/( *•.*?)*$", m => insertBlockComment(m.Groups[1].Value) + (m.Groups[2].Success ? m.Groups[2].Value : ""), RegexOptions.Singleline);
        parsing = ReplaceStr(parsing, @"([;}] *)\/\*((?:(?!\*\/).)*?)\*\/", m => m.Groups[1].Value + insertBlockComment(m.Groups[2].Value), RegexOptions.Singleline);
        parsing = ReplaceStr(parsing, @"\/\*(?:(?!\*\/).)*?\*\/", "");

        parsing = ReplaceStr(parsing, @"^ *(?:constant)? *native\b.*", str => insertComment(str.Value), RegexOptions.Multiline);

        parsing = ReplaceStr(parsing, @"\b(?:do|in|end|nil|repeat|until)\b", "$&_");
        parsing = ReplaceStr(parsing, @"([\w\$]+):([\w\$]+)", "$2[$1]");
        parsing = ReplaceStr(parsing, @"\bnull\b", "nil");
        parsing = ReplaceStr(parsing, @"!=", "~=");

        parsing = ReplaceStr(parsing, @"^( *)debug +(?:(?:set|call) *)?", m =>
        {
            var indent = m.Groups[1].Value;
            if (commentDebug) indent += "--debug ";
            return indent;
        }, RegexOptions.Multiline);
        parsing = ReplaceStr(parsing, @"^( *)(?:set|call|constant) +", "$1", RegexOptions.Multiline);
        parsing = ReplaceStr(parsing, @"^( *end)if", "$1", RegexOptions.Multiline);
        parsing = ReplaceStr(parsing, @"^( *)static +if\b", "$1if", RegexOptions.Multiline);
        parsing = ReplaceStr(parsing, @"\.exists\b", "");
        parsing = ReplaceStr(parsing, @"'\\?(.)'", m => ((int)(m.Groups[1].Value[0])).ToString());
        parsing = ReplaceStr(parsing, @"(.)\.([\$\w]+) *\(", mm =>
        {
            var firstChar = mm.Groups[1].Value;
            var methodCaller = mm.Groups[2].Value;
            if (firstChar == " ") firstChar = " self";
            return firstChar + ":" + methodCaller + "(";
        }, RegexOptions.Multiline);
        parsing = ReplaceStr(parsing, @"^( *)private\. *type +(\w+) +extends +(\w+) +array *\[ *(\d+) *\]", "$1local $2 = Struct();$2.size = $4 ---@type $3[]", RegexOptions.Multiline);
        parsing = ReplaceStr(parsing, @"^( *)interface\b +([\$\w]+)(.*?)^ *endinterface", "$1Struct $2 = vJass.interface(true)\n$1--[[$3$1]]", RegexOptions.Singleline | RegexOptions.Multiline);
        parsing = ReplaceStr(parsing, @"^( *)(?:public|private)* *function interface", "$1---@class", RegexOptions.Multiline);

        parsing = ReplaceStr(parsing, @"^( *local +)(.*)", m =>
        {
            var local = m.Groups[1].Value;
            var line = m.Groups[2].Value;
            return local + parseVar(line, true);
        }, RegexOptions.Multiline);

        parsing = ReplaceStr(parsing,
            @"^( *)(private|public)(?: +constant)?\b +function *([\$\w]+)(.*?^ *endfunction)",
            mm =>
            {
                var indent = mm.Groups[1].Value;
                var scope = mm.Groups[2].Value;
                var name = mm.Groups[3].Value;
                var body = mm.Groups[4].Value;
                body = indent + "local function " + name + body;
                if (scope == "public")
                {
                    return body + "\n" + indent + "_G[SCOPE_PREFIX..'" + name + "'] = " + name;
                }
                return body;
            },
            RegexOptions.Singleline | RegexOptions.Multiline
        );

        parsing = ReplaceStr(parsing, @"^( *)private +keyword\b", "$1local", RegexOptions.Multiline);
        parsing = ReplaceStr(parsing, @"\$([0-9a-fA-F]+[^\$])", "0x$1");
        parsing = ReplaceStr(parsing, @"^( *)hook +(\w+) +(\w*(?:\.\w+)*)", "$1vJass.hook(\"$2\", $3)", RegexOptions.Multiline);

        parsing = ReplaceStr(parsing,
            @"^( *)library +(\w+) *(?:initializer *(\w*))?([^\r\n]*)(.*?)endlibrary",
            mm =>
            {
                var indent = mm.Groups[1].Value;
                var name = mm.Groups[2].Value;
                var init = mm.Groups[3].Success ? mm.Groups[3].Value : null;
                var requirements = mm.Groups[4].Value;
                var body = mm.Groups[5].Value;
                var reqLines = "";
                if (requirements != null)
                {
                    Regex.Replace(requirements, @"(?:requires|needs|uses) +(.*)", r =>
                    {
                        var reqs = r.Groups[1].Value;
                        Regex.Replace(reqs, @"(optional)? *(\w+)", rr =>
                        {
                            var opt = rr.Groups[1].Value;
                            var libName = rr.Groups[2].Value;
                            if (!string.IsNullOrEmpty(opt))
                            {
                                opt = ".optionally";
                            }
                            reqLines += "\n" + indent + indentation + "Require" + opt + " '" + libName + "'";
                            return "";
                        });
                        return "";
                    });
                }
                body = indent + "OnInit(\"" + name + "\", function()" + reqLines + "\n" +
                       indent + indentation + "LIBRARY_" + name + " = true\n" +
                       indent + indentation + "local SCOPE_PREFIX = \"" + name + "_\" ---@type string " + body;
                if (!string.IsNullOrEmpty(init))
                {
                    body += indent + indentation + "Require 'Init vJass Libraries'; " + init + "()\n";
                }
                return body + "\n" + indent + "end)";
            },
            RegexOptions.Singleline | RegexOptions.Multiline
        );

        parsing = ReplaceStr(parsing,
            @"^( *)(private|public)? *scope +(\w+) *(?:initializer *(\w*))?(.*?)endscope",
            mm =>
            {
                var indent = mm.Groups[1].Value;
                var scope = mm.Groups[2].Success ? mm.Groups[2].Value : null;
                var name = mm.Groups[3].Value;
                var init = mm.Groups[4].Success ? mm.Groups[4].Value : null;
                var body = mm.Groups[5].Value;
                if (scope != null)
                {
                    name = "SCOPE_PREFIX..\"\"" + name;
                }
                else
                {
                    name = "\"" + name + "\"";
                }
                body = indent + "OnInit(function()\n" +
                       indent + indentation + "local SCOPE_PREFIX = " + name + "_\" ---@type string " + body;
                if (!string.IsNullOrEmpty(init))
                {
                    body += indent + indentation + "Require 'Init vJass Scopes'; " + init + "()\n";
                }
                return body + "\n" + indent + "end)";
            },
            RegexOptions.Singleline | RegexOptions.Multiline
        );

        parsing = RepeatActionOnString(parsing, s =>
        {
            return ReplaceStr(s, @"^( *)(loop\b((?!\bendloop\b|\bloop\b).)*\bendloop)", mm =>
            {
                var indent = mm.Groups[1].Value;
                var contents = mm.Groups[2].Value;
                contents = ReplaceStr(contents, @"^loop\s+exitwhen *([^\r\n•]*)(.*end)loop", m =>
                {
                    var cond = m.Groups[1].Value;
                    var cont = m.Groups[2].Value;
                    var original = cond;
                    cond = ReplaceStr(cond, @"^ *([\w$]+) *([\<\>\=\~]{1,2}) *([\w$]+) *$", cc =>
                    {
                        var w1 = cc.Groups[1].Value;
                        var compare = cc.Groups[2].Value;
                        var w2 = cc.Groups[3].Value;
                        switch (compare)
                        {
                            case "<": compare = ">="; break;
                            case ">": compare = "<="; break;
                            case "<=": compare = ">"; break;
                            case ">=": compare = "<"; break;
                            case "~=": compare = "=="; break;
                            default: compare = "~="; break;
                        }
                        return w1 + " " + compare + " " + w2;
                    });
                    if (cond != original)
                    {
                        return "while " + cond + " do " + cont;
                    }
                    return "while not (" + cond + ") do " + cont;
                }, RegexOptions.Singleline);
                if (!noRepeatUntil)
                {
                    contents = ReplaceStr(contents, @"^loop(.*)\r?\n *exitwhen([^\n\r•]*)(\s*? *)endloop", "repeat$1$3until$2", RegexOptions.Singleline);
                }
                contents = ReplaceStr(contents, @"^loop\b(.*end)loop", "while true do$1", RegexOptions.Singleline);
                contents = ReplaceStr(contents, @"^( *)exitwhen\b([^\r\n•]*)", "$1if$2 then break end", RegexOptions.Multiline);
                contents = ReplaceStr(contents, @"^( *)if *true *then break end", "$1break", RegexOptions.Multiline);
                return indent + contents;
            }, RegexOptions.Singleline);
        });

        parsing = ReplaceStr(parsing, @"^ *globals\b(.*?)\bendglobals\b", mm =>
        {
            var globals = mm.Groups[1].Value;
            globals = ReplaceStr(globals, @"^( *)private(?: +constant)*\b", "$1local", RegexOptions.Multiline);
            globals = ReplaceStr(globals, @"^( *)public +constant +([\$\w]+) +([\$\w]+)([^\n\r]*)", "$1local $2 $3$4\n$1_G[SCOPE_PREFIX..\"$3\"] = $3", RegexOptions.Multiline);
            globals = ReplaceStr(globals, @"^( *)public +([\$\w]+) +([\$\w]+)\b([^\n\r]*)", "$1local $2 $3$4\n$1GlobalRemap(SCOPE_PREFIX..\"$3\", function() return $3 end, function(val) $3 = val end)", RegexOptions.Multiline);
            globals = ReplaceStr(globals, @"^( *(local +)*)(.*)", m =>
            {
                var prefix = m.Groups[1].Value;
                var isLocal = m.Groups[2].Success;
                var remainder = m.Groups[3].Value;
                return prefix + parseVar(remainder, isLocal);
            }, RegexOptions.Multiline);
            return globals;
        }, RegexOptions.Singleline | RegexOptions.Multiline);

        parsing = ReplaceStr(parsing, @"([\w\$\.]+)[\:\.](name|(?:execute|evaluate))\b *(\(([^()]*)\))?", mm =>
        {
            var ignoreMatch = mm.Value;
            var name = mm.Groups[1].Value;
            var reference = mm.Groups[2].Value;
            var hasArgs = mm.Groups[3].Success;
            var args = mm.Groups[4].Value;
            if (name == "vJass") return ignoreMatch;
            if (reference == "name") return "vJass.name(" + name + ")";
            if (!hasArgs) return ignoreMatch;
            return "vJass." + reference + "(" + name + ", " + args + ")";
        }, RegexOptions.Multiline);

        string DoFloorInt(string line)
        {
            return ReplaceStr(line, @"([^\/])\/([^\/])", m =>
            {
                return m.Groups[1].Value + "//" + m.Groups[2].Value;
            });
        }

        parsing = ReplaceStr(parsing,
            @"^( *)(?:(private|public) +)?type +([\w\$]+)( +)extends(.*)",
            mm =>
            {
                var matchFailed = mm.Value;
                var indent = mm.Groups[1].Value;
                var scope = mm.Groups[2].Success ? mm.Groups[2].Value : null;
                var word = mm.Groups[3].Value;
                var gap = mm.Groups[4].Value;
                var remainder = mm.Groups[5].Value;
                if (remainder.IndexOf("function", StringComparison.Ordinal) >= 0)
                {
                    return "@@@class " + word + ":function --".Replace("@@@", "---@");
                }
                string size = null;
                remainder = ReplaceStr(remainder, @"^[^\[]*\[ *([^,\] ]+)[^•\r\n]*(.*)", m =>
                {
                    size = m.Groups[1].Value;
                    return m.Groups[2].Value;
                }, RegexOptions.Multiline);
                if (size != null)
                {
                    if (scope != null)
                    {
                        if (scope == "public")
                            remainder = "; _G[SCOPE_PREFIX.." + word + "] = " + word;
                        indent += "local ";
                    }
                    return indent + word + gap + " = vJass.dynamicArray(" + size + ")" + remainder;
                }
                return indent + "---@class " + word + ": " + gap + remainder;
            },
            RegexOptions.Multiline
        );

        parsing = RepeatActionOnString(parsing, s =>
        {
            return ReplaceStr(s, @"^( *)((?:[\w\$:\[\]\=]+ +)+?|[^\r\n]*?\bfunction )\btakes +([\$\w, ]+ +)*?\breturns +([\$\w]+)(.*?\bend)function\b",
                mm =>
                {
                    var indent = mm.Groups[1].Value;
                    var func = mm.Groups[2].Value;
                    var myParams = mm.Groups[3].Success ? mm.Groups[3].Value : null;
                    var rtype = mm.Groups[4].Value;
                    var contents = mm.Groups[5].Value;
                    Func<string, string> RC2NF = (type) =>
                    {
                        if (type == "real") return "number";
                        else if (type == "code") return "function";
                        return type;
                    };
                    string paramEmmy = "";
                    string returnEmmy = "";
                    string argsResult = "";
                    func = func.Substring(0, func.Length - 1);
                    if (rtype != "nothing")
                    {
                        rtype = RC2NF(rtype);
                        returnEmmy = ":" + rtype;
                        if (rtype == "integer")
                        {
                            contents = ReplaceStr(contents, @"return.+\/.+", returnLine => DoFloorInt(returnLine.Value));
                        }
                    }
                    contents = ReplaceStr(contents, @"function *([\$\w]+(?:\.[\w\$]+)? *[\)\,])", "$1");
                    contents = ReplaceStr(contents, @"([$\w]+) *\=[^=\n\r•][^\n\r•]*", setterLine =>
                    {
                        var line = setterLine.Value;
                        var varName = Regex.Match(line, @"^([$\w]+)").Groups[1].Value;
                        if (isVarInt(varName))
                        {
                            line = DoFloorInt(line);
                        }
                        else if (varName == "self")
                        {
                            line += "; _ENV = Struct.environment(self)";
                        }
                        return line;
                    }, RegexOptions.Multiline);
                    Action<string, string> doEmmyParse = (type, name) =>
                    {
                        paramEmmy += name + ": " + type + ", ";
                    };
                    if (func.Contains(":")) doEmmyParse("thistype", "self");
                    if (myParams != null && myParams.IndexOf("nothing", StringComparison.Ordinal) < 0)
                    {
                        myParams = ReplaceStr(myParams, @"([A-Za-z][\w]*) +([A-Za-z][\w]+)", mm2 =>
                        {
                            var t = RC2NF(mm2.Groups[1].Value);
                            var n = mm2.Groups[2].Value;
                            doEmmyParse(t, n);
                            return n;
                        });
                        argsResult = Regex.Replace(myParams, @"[,\s]+", ", ");
                    }
                    string emmyResult = "";
                    if (paramEmmy != "")
                    {
                        paramEmmy = paramEmmy.Substring(0, paramEmmy.Length - 2);
                    }
                    emmyResult = indent + "---@type fun(" + paramEmmy + ")" + returnEmmy + "\n";
                    return emmyResult + indent + func + "(" + argsResult + ")" + contents;
                },
                RegexOptions.Singleline | RegexOptions.Multiline
            );
        });

        parsing = ReplaceStr(parsing, @"endfunction", "end");
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