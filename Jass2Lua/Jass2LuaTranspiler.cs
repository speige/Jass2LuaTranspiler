﻿using System.Text.RegularExpressions;

//extracted JS-only & removed "zinc" and "vJass" from https://raw.githubusercontent.com/BribeFromTheHive/vJass2Lua/refs/heads/main/betaconversions.html
//NOTE: vJass is not implemented, workaround is to run JassHelper 1st to convert from vJass to standard Jass before converting to lua.
public partial class Jass2LuaTranspiler
{
    public class Options
    {
        public bool DeleteComments = false;
        public bool AvoidRepeatUntilLoops = false; //temporary workaround
        public bool AddStringPlusOperatorOverload = true;
        public bool AddGithubAttributionLink = true;
    }

    [GeneratedRegex("•#cmt#(\\d+)")]
    protected static partial Regex InsertCommentRegex();

    [GeneratedRegex("•#str#(\\d+)")]
    protected static partial Regex InsertStringRegex();

    [GeneratedRegex("•#fcc#(\\d+)")]
    protected static partial Regex InsertRawcodeRegex();

    [GeneratedRegex("^([A-Za-z][\\w]*)[ \t]+array[ \t]+([A-Za-z][\\w]*)(.*)", RegexOptions.Multiline)]
    protected static partial Regex ParseVarReplaceArray1Regex();

    [GeneratedRegex("^\\[[ \t]*(\\d+)[ \t]*\\]", RegexOptions.Multiline)]
    protected static partial Regex ParseVarReplaceArray3Regex();

    [GeneratedRegex("^([A-Za-z][\\w]*)([ \t]+)([A-Za-z][\\w]*)(.*)", RegexOptions.Multiline)]
    protected static partial Regex ParseVarReplaceVar1Regex();

    [GeneratedRegex("^function[ \t]*([\\$\\w]+(?:\\.[\\w\\$]+)?[ \t]*[\\)\\,])", RegexOptions.Multiline)]
    protected static partial Regex ParseScriptFunctionRegex();

    [GeneratedRegex(@"""[ \t]*\+")]
    protected static partial Regex DoubleQuotePlusRegex();

    [GeneratedRegex(@"\+[ \t]*""")]
    protected static partial Regex PlusDoubleQuoteRegex();

    [GeneratedRegex(@"(""(?:[^""\\]|\\""|\\[\\\w])*?"")", RegexOptions.Multiline)]
    protected static partial Regex StringLiteralRegex();

    [GeneratedRegex(@"'(?:[^'\\]|\\'|\\\\){4}'")]
    protected static partial Regex RawcodeRegex();

    [GeneratedRegex(@"\/\/(.*)")]
    protected static partial Regex CommentRegex();

    [GeneratedRegex(@"^[ \t]*(?:constant)?[ \t]*native\b.*", RegexOptions.Multiline)]
    protected static partial Regex NativeFunctionRegex();

    [GeneratedRegex(@"\b(?:do|in|end|nil|repeat|until)\b")]
    protected static partial Regex LuaKeywordsRegex();

    [GeneratedRegex(@"([\w\$]+):([\w\$]+)")]
    protected static partial Regex ColonMethodRegex();

    [GeneratedRegex(@"\bnull\b")]
    protected static partial Regex NullRegex();

    [GeneratedRegex(@"!=")]
    protected static partial Regex NotEqualsRegex();

    [GeneratedRegex(@"^debug[ \t]+", RegexOptions.Multiline)]
    protected static partial Regex DebugRegex();

    [GeneratedRegex(@"^(?:set|call|constant)[ \t]+", RegexOptions.Multiline)]
    protected static partial Regex SetCallConstantRegex();

    [GeneratedRegex(@"^(end)if", RegexOptions.Multiline)]
    protected static partial Regex EndIfRegex();

    [GeneratedRegex(@"'\\?(.)'")]
    protected static partial Regex SingleQuoteCharRegex();

    [GeneratedRegex(@"^(local[ \t]+)(.*)", RegexOptions.Multiline)]
    protected static partial Regex LocalVarRegex();

    [GeneratedRegex(@"\$([0-9a-fA-F]+[^\$])")]
    protected static partial Regex HexRegex();

    [GeneratedRegex(@"^(loop\b((?!\bendloop\b|\bloop\b).)*\bendloop)", RegexOptions.Singleline)]
    protected static partial Regex LoopRegex();

    [GeneratedRegex(@"^loop\s+exitwhen[ \t]*([^\n•]*)(.*end)loop", RegexOptions.Singleline)]
    protected static partial Regex LoopExitWhenRegex();

    [GeneratedRegex(@"^loop(.*)\n[ \t]*exitwhen([^\n•]*)(\s*?[ \t]*)endloop", RegexOptions.Singleline)]
    protected static partial Regex LoopRepeatUntilRegex();

    [GeneratedRegex(@"^loop\b(.*end)loop", RegexOptions.Singleline)]
    protected static partial Regex LoopWhileTrueRegex();

    [GeneratedRegex(@"^exitwhen\b([^\n•]*)", RegexOptions.Multiline)]
    protected static partial Regex LoopExitWhenIfRegex();

    [GeneratedRegex(@"^if[ \t]*true[ \t]*then[ \t]+break[ \t]+end", RegexOptions.Multiline)]
    protected static partial Regex LoopBreakIfTrueRegex();

    [GeneratedRegex(@"^((?:[\w\$:\[\]\=]+[ \t]+)+?|[^\n]*?\bfunction[ \t]+)\btakes[ \t]+([\$\w, ]+[ \t]+)*?\breturns[ \t]+([\$\w]+)(.*?\bend)function\b", RegexOptions.Singleline | RegexOptions.Multiline)]
    protected static partial Regex FunctionDeclarationRegex();

    [GeneratedRegex(@"([$\w]+)[ \t]*\=[^=\n•][^\n•]*", RegexOptions.Multiline)]
    protected static partial Regex VarAssignmentRegex();

    [GeneratedRegex(@"([A-Za-z][\w]*)[ \t]+([A-Za-z][\w]*)")]
    protected static partial Regex ParamRegex();

    [GeneratedRegex(@"[,\s]+")]
    protected static partial Regex ParamCommaRegex();

    [GeneratedRegex(@"(\=|\(|,)[ \t]*function\s+([\w$]+)")]
    protected static partial Regex InlineFunctionRegex();

    [GeneratedRegex(@"endfunction")]
    protected static partial Regex EndFunctionRegex();

    [GeneratedRegex(@"(\n)(?:[ \t]*\n)*")]
    protected static partial Regex LineBreakRegex();

    [GeneratedRegex(@"^[ \t]*globals\b(.*?)\bendglobals\b", RegexOptions.Singleline | RegexOptions.Multiline)]
    protected static partial Regex GlobalsRegex();

    [GeneratedRegex(@"return.+\/.+")]
    protected static partial Regex ReturnDivRegex();

    [GeneratedRegex(@"^[ \t]*([\w$]+)[ \t]*([\<\>\=\~]{1,2})[ \t]*([\w$]+)[ \t]*$")]
    protected static partial Regex ConditionCompareRegex();

    [GeneratedRegex(@"^((local[ \t]+)*)(.*)", RegexOptions.Multiline)]
    protected static partial Regex LocalGlobalRegex();

    [GeneratedRegex(@"([^\/])\/([^\/])")]
    protected static partial Regex FloorIntRegex();

    [GeneratedRegex(@"^[ \t]*\=", RegexOptions.Multiline)]
    protected static partial Regex IsSetRegex();

    [GeneratedRegex(@"^([$\w]+)")]
    protected static partial Regex VarNameRegex();

    protected readonly Options _options;
    protected List<string> _commentList = new();
    protected List<string> _stringList = new();
    protected List<string> _rawcodeList = new();
    protected List<string> _intStack = new();

    public Jass2LuaTranspiler(Options options = null)
    {
        _options = options ?? new Options();
    }

    protected bool IsVarInt(string varName)
    {
        return _intStack.IndexOf(varName) >= 0;
    }

    protected void AddToIntStack(string varName)
    {
        if (!IsVarInt(varName))
        {
            _intStack.Add(varName);
        }
    }

    protected string RepeatActionOnString(string str, Func<string, string> action)
    {
        var tempStr = "";
        while (tempStr != str)
        {
            tempStr = str;
            str = action(str);
        }
        return str;
    }

    protected string InsertComment(string str)
    {
        if (_options.DeleteComments)
        {
            return "";
        }
        _commentList.Add("--" + str);
        return "•#cmt#" + (_commentList.Count - 1);
    }

    protected string InsertString(string str)
    {
        _stringList.Add(str);
        return "•#str#" + (_stringList.Count - 1);
    }

    protected string InsertRawcode(string str)
    {
        _rawcodeList.Add(str);
        return "•#fcc#" + (_rawcodeList.Count - 1);
    }

    protected string UnpackComment(string str)
    {
        if (_commentList.Count > 0)
        {
            return InsertCommentRegex().Replace(str, m => _commentList[int.Parse(m.Groups[1].Value)]);
        }
        return str;
    }

    protected string UnpackString(string str)
    {
        if (_stringList.Count > 0)
        {
            return InsertStringRegex().Replace(str, m => _stringList[int.Parse(m.Groups[1].Value)]);
        }
        return str;
    }

    protected string UnpackRawcode(string str)
    {
        if (_rawcodeList.Count > 0)
        {
            return InsertRawcodeRegex().Replace(str, m => _rawcodeList[int.Parse(m.Groups[1].Value)]);
        }
        return str;
    }

    protected string DeleteLineBreaks(string str)
    {
        return LineBreakRegex().Replace(str, "$1");
    }

    protected string FixReturnType(string type)
    {
        if (type == "real")
        {
            return "number";
        }
        if (type == "code")
        {
            return "function";
        }
        return type;
    }

    protected string ParseVar(string line, bool isLocal = false)
    {
        var newLine = ParseVarReplaceArray1Regex().Replace(line, m =>
        {
            var name = m.Groups[2].Value;
            var remainder = m.Groups[3].Value;
            var rawtype = m.Groups[1].Value;
            var result = remainder;
            if (result == remainder)
            {
                result = ParseVarReplaceArray3Regex().Replace(remainder, mm =>
                {
                    var size = mm.Groups[1].Value;
                    return $"{name}={{size={size}}} ";
                });
                if (result == remainder)
                {
                    string arrayType;
                    switch (rawtype)
                    {
                        case "integer":
                            AddToIntStack(name);
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
                    result = name + "=" + arrayType + " " + remainder;
                }
            }
            return result;
        });

        if (newLine != line)
        {
            return newLine;
        }

        return ParseVarReplaceVar1Regex().Replace(line, m =>
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
                AddToIntStack(name);
            }
            var isSet = IsSetRegex().Match(remainder);
            if (!isSet.Success)
            {
                if (isLocal)
                {
                    return name + tail;
                }
                return name + "=nil" + tail;
            }
            return name + tlen + remainder + tail;
        });
    }

    protected string RemoveAllWhitespace(string script)
    {
        var lines = script.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var cleanedLines = new List<string>();
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                cleanedLines.Add(trimmed);
            }
        }
        return string.Join("\n", cleanedLines);
    }

    protected string IndentLua(string luaScript)
    {
        luaScript = RemoveAllWhitespace(luaScript);
        var lines = luaScript.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var indentLevel = 0;
        var reindented = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (Regex.IsMatch(line, @"^(end|until|elseif\s+.*then|else)\b"))
            {
                indentLevel = Math.Max(indentLevel - 1, 0);
            }
            reindented.Add(new string('\t', indentLevel) + line);
            if (Regex.IsMatch(line, @"\b(function|then|do|repeat|else)\b(?!.*end)"))
            {
                indentLevel++;
            }
        }

        for (var i = 0; i < reindented.Count; i++)
        {
            var trimmedLine = reindented[i].Trim();
            if (Regex.IsMatch(trimmedLine, @"^function\b"))
            {
                if (i > 0 && !string.IsNullOrWhiteSpace(reindented[i - 1]))
                {
                    reindented.Insert(i, "");
                    i++;
                }
            }
        }
        return string.Join("\n", reindented);
    }

    public string Transpile(string jassScript)
    {
        _commentList = new List<string>();
        _stringList = new List<string>();
        _rawcodeList = new List<string>();

        var result = jassScript;
        result = RemoveAllWhitespace(result);

        result = DoubleQuotePlusRegex().Replace(result, "\"..");
        result = PlusDoubleQuoteRegex().Replace(result, "..\"");
        result = StringLiteralRegex().Replace(result, m => InsertString(m.Groups[1].Value));
        result = RawcodeRegex().Replace(result, str => InsertRawcode("FourCC(" + str.Value + ")"));
        result = CommentRegex().Replace(result, m => InsertComment(m.Groups[1].Value));

        result = NativeFunctionRegex().Replace(result, str => InsertComment(str.Value));
        result = LuaKeywordsRegex().Replace(result, "$&_");
        result = ColonMethodRegex().Replace(result, "$2[$1]");
        result = NullRegex().Replace(result, "nil");
        result = NotEqualsRegex().Replace(result, "~=");

        result = DebugRegex().Replace(result, "--debug ");
        result = SetCallConstantRegex().Replace(result, "");
        result = EndIfRegex().Replace(result, "$1");
        result = SingleQuoteCharRegex().Replace(result, m => ((int)(m.Groups[1].Value[0])).ToString());

        result = LocalVarRegex().Replace(result, m =>
        {
            var local = m.Groups[1].Value;
            var line = m.Groups[2].Value;
            return local + ParseVar(line, true);
        });

        result = HexRegex().Replace(result, "0x$1");

        result = RepeatActionOnString(result, s =>
        {
            return LoopRegex().Replace(s, mm =>
            {
                var contents = mm.Groups[1].Value;
                contents = LoopExitWhenRegex().Replace(contents, m =>
                {
                    var cond = m.Groups[1].Value;
                    var cont = m.Groups[2].Value;
                    var original = cond;
                    cond = ConditionCompareRegex().Replace(cond, cc =>
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
                });
                if (!_options.AvoidRepeatUntilLoops)
                {
                    contents = LoopRepeatUntilRegex().Replace(contents, "repeat$1$3until$2");
                }
                contents = LoopWhileTrueRegex().Replace(contents, "while true do$1");
                contents = LoopExitWhenIfRegex().Replace(contents, "if$1 then break end");
                contents = LoopBreakIfTrueRegex().Replace(contents, "break");
                return contents;
            });
        });

        result = GlobalsRegex().Replace(result, mm =>
        {
            var globals = mm.Groups[1].Value;
            globals = LocalGlobalRegex().Replace(globals, m =>
            {
                var isLocal = m.Groups[2].Success;
                var remainder = m.Groups[3].Value;
                return ParseVar(remainder, isLocal);
            });
            return globals;
        });

        string DoFloorInt(string line)
        {
            return FloorIntRegex().Replace(line, m => m.Groups[1].Value + "//" + m.Groups[2].Value);
        }

        result = RepeatActionOnString(result, s =>
        {
            return FunctionDeclarationRegex().Replace(s, mm =>
            {
                var func = mm.Groups[1].Value;
                var myParams = mm.Groups[2].Success ? mm.Groups[2].Value : null;
                var rtype = mm.Groups[3].Value;
                var contents = mm.Groups[4].Value;
                func = func.Substring(0, func.Length - 1);
                if (rtype != "nothing")
                {
                    rtype = FixReturnType(rtype);
                    if (rtype == "integer")
                    {
                        contents = ReturnDivRegex().Replace(contents, returnLine => DoFloorInt(returnLine.Value));
                    }
                }
                contents = ParseScriptFunctionRegex().Replace(contents, "$1");
                contents = VarAssignmentRegex().Replace(contents, setterLine =>
                {
                    var line2 = setterLine.Value;
                    var varName = VarNameRegex().Match(line2).Groups[1].Value;
                    if (IsVarInt(varName))
                    {
                        line2 = DoFloorInt(line2);
                    }
                    return line2;
                });
                if (myParams != null)
                {
                    myParams = myParams.Trim();
                    if (myParams.Equals("nothing", StringComparison.OrdinalIgnoreCase))
                    {
                        myParams = "";
                    }
                    else if (myParams.IndexOf("nothing", StringComparison.Ordinal) < 0)
                    {
                        myParams = ParamRegex().Replace(myParams, mm2 => mm2.Groups[2].Value);
                        myParams = ParamCommaRegex().Replace(myParams, ", ");
                    }
                }
                return func + "(" + (myParams ?? "") + ")" + contents;
            });
        });

        result = InlineFunctionRegex().Replace(result, "$1 $2");
        result = EndFunctionRegex().Replace(result, "end");
        result = UnpackComment(result);
        result = UnpackString(result);
        result = UnpackRawcode(result);
        result = DeleteLineBreaks(result);

        if (_options.AddStringPlusOperatorOverload)
        {
            result = "getmetatable(\"\").__add = function(obj, obj2) return obj .. obj2 end\n\n" + result;
        }

        if (_options.AddGithubAttributionLink)
        {
            result = "--https://github.com/speige/Jass2LuaTranspiler\n\n" + result;
        }

        result = IndentLua(result);
        return result.Replace("\n", "\r\n");
    }
}