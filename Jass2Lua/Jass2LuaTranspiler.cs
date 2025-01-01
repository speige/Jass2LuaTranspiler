using NLua;
using System.Text.RegularExpressions;

public partial class Jass2LuaTranspiler
{
    public class Options
    {
        public bool DeleteComments = false;
        public bool AddStringPlusOperatorOverload = true;
        public bool AddGithubAttributionLink = true;
        public bool PrependTranspilerWarnings = true;
    }

    [GeneratedRegex(@"^\[[^]]*\]:(\d+):")]
    protected static partial Regex LuaErrorLineNumberRegex();

    [GeneratedRegex(@"([0-9])(and|or|then)\b")]
    protected static partial Regex InsertSpaceBetweenNumbersAndKeywordseRegex();

    [GeneratedRegex(@"•#cmt#(\d+)•")]
    protected static partial Regex InsertCommentRegex();

    [GeneratedRegex(@"•#str#(\d+)•")]
    protected static partial Regex InsertStringRegex();

    [GeneratedRegex(@"•#fcc#(\d+)•")]
    protected static partial Regex InsertRawcodeRegex();

    [GeneratedRegex(@"^([A-Za-z][\w]*)[ \t]+array[ \t]+([A-Za-z][\w]*)(.*)", RegexOptions.Multiline)]
    protected static partial Regex ParseVarReplaceArray1Regex();

    [GeneratedRegex(@"^\[[ \t]*(\d+)[ \t]*\]", RegexOptions.Multiline)]
    protected static partial Regex ParseVarReplaceArray3Regex();

    [GeneratedRegex(@"^([A-Za-z][\w]*)([ \t]+)([A-Za-z][\w]*)(.*)", RegexOptions.Multiline)]
    protected static partial Regex ParseVarReplaceVar1Regex();

    [GeneratedRegex(@"^function[ \t]*([\$\w]+(?:\.[\w\$]+)?[ \t]*[\)\,])", RegexOptions.Multiline)]
    protected static partial Regex ParseScriptFunctionRegex();

    [GeneratedRegex(@"(•#str#\d+•)[ \t]*\+")]
    protected static partial Regex StringPlusRegex();

    [GeneratedRegex(@"\+[ \t]*(•#str#•)")]
    protected static partial Regex PlusStringRegex();

    [GeneratedRegex(@"(?<String>""(?:\\.|[^""\\])*"")|(?<Rawcode>'(?:\\.|[^'\\])*')|(?<Comment>//.*?$)", RegexOptions.Multiline)]
    protected static partial Regex StringOrRawCodeOrCommentRegex();

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

    [GeneratedRegex(@"^(local[ \t]+)(.*)", RegexOptions.Multiline)]
    protected static partial Regex LocalVarRegex();

    [GeneratedRegex(@"\$([0-9a-fA-F]+[^\$])")]
    protected static partial Regex HexRegex();

    [GeneratedRegex(@"^loop\b", RegexOptions.Multiline)]
    protected static partial Regex LoopRegex();

    [GeneratedRegex(@"^endloop\b", RegexOptions.Multiline)]
    protected static partial Regex EndLoopRegex();

    [GeneratedRegex(@"^exitwhen\b[ \t]*([^\n]*)", RegexOptions.Multiline)]
    protected static partial Regex LoopExitWhenIfRegex();

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

    [GeneratedRegex(@"-[ \t]*-")]
    protected static partial Regex DoubleMinusRegex();

    [GeneratedRegex(@"\+[ \t]*\+")]
    protected static partial Regex DoublePlusRegex();

    protected readonly Options _options;
    protected List<string> _commentList;
    protected List<string> _stringList;
    protected List<string> _rawcodeList;
    protected List<string> _intStack;

    public Jass2LuaTranspiler(Options options = null)
    {
        _options = options ?? new Options();
    }

    protected static int FromFourCCToInt(string code)
    {
        if (code.Length != 4)
        {
            return 0;
        }

        var result = 0;
        for (var i = 0; i < 4; i++)
        {
            var byteValue = (byte)code[i];
            var bytesLeft = 4 - i - 1;
            result |= byteValue << (bytesLeft * 8);
            if (byteValue >= 0x80 && bytesLeft < 3)
            {
                result -= 1 << ((bytesLeft + 1) * 8);
            }
        }

        return result;
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
        _commentList.Add($"--{str}");
        return "•#cmt#" + (_commentList.Count - 1) + "•";
    }

    protected string InsertString(string str)
    {
        _stringList.Add(str.Replace("\n", "|n").Replace("\r", "|r"));
        return "•#str#" + (_stringList.Count - 1) + "•";
    }

    protected string InsertRawcode(string str)
    {
        _rawcodeList.Add(str);
        return "•#fcc#" + (_rawcodeList.Count - 1) + "•";
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
            if (type == "integer")
            {
                AddToIntStack(name);
            }
            var isSet = IsSetRegex().Match(remainder);
            if (!isSet.Success)
            {
                if (isLocal)
                {
                    return name;
                }
                return name + "=nil";
            }
            return name + tlen + remainder;
        });
    }

    protected string TrimAllWhitespaceConvertCRtoLF(string script)
    {
        var lines = script.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
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

    protected string ReformatLua(string luaScript)
    {
        luaScript = TrimAllWhitespaceConvertCRtoLF(luaScript);
        var lines = luaScript.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        var reindented = IndentLua(lines);

        //NOTE: can't foreach since we're modifying list during loop
        for (var i = 0; i < reindented.Count; i++)
        {
            if (Regex.IsMatch(reindented[i], @"\t*function\b"))
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

    protected List<string> IndentLua(List<string> lines)
    {
        var result = new List<string>();

        var indentLevel = 0;
        foreach (var line in lines.Select(x => x.Trim()))
        {
            if (Regex.IsMatch(line, @"^(end|until|elseif.*then|else)\b"))
            {
                indentLevel = Math.Max(indentLevel - 1, 0);
            }

            result.Add(new string('\t', indentLevel) + line);
            if (Regex.IsMatch(line, @"^(function|while|if|elseif|repeat|else)\b(?!.*end)"))
            {
                indentLevel++;
            }
        }

        return result;
    }

    public string Transpile(string jassScript, out string warnings)
    {
        if (!typeof(Lua).Assembly.FullName.Contains("Version=1.4.32.0"))
        {
            throw new Exception("Missing or wrong version of NLua dll. WC3 requires Lua 5.3 which matches NLua dll 1.4.32.0");
        }

        _commentList = new List<string>();
        _stringList = new List<string>();
        _rawcodeList = new List<string>();
        _intStack = new List<string>();

        var result = jassScript;

        result = StringOrRawCodeOrCommentRegex().Replace(result, m => {
            if (m.Groups["String"].Success)
            {
                return InsertString(m.Value.Replace("\n", "\\n").Replace("\r", "\\r"));
            }
            else if (m.Groups["Comment"].Success)
            {
                var comment = m.Value.Trim();
                if (comment.StartsWith("//"))
                {
                    comment = comment.Substring(2);
                }
                return "\n" + InsertComment(comment) + "\n";
            }
            else if (m.Groups["Rawcode"].Success)
            {
                var rawcode = m.Value.Substring(1, m.Value.Length-2);
                if (rawcode.Length == 1)
                {
                    return ((int)rawcode[0]).ToString();
                }

                if (rawcode.Length == 4 && rawcode.Any(x => x < ' ' || x > '~'))
                {
                    return FromFourCCToInt(rawcode).ToString();
                }

                if (rawcode.Length != 4)
                {
                    return "'" + m.Value + "'";
                }

                return InsertRawcode(" FourCC('" + rawcode + "') ");
            }

            return m.Value;
        });

        result = TrimAllWhitespaceConvertCRtoLF(result);

        result = RepeatActionOnString(result, s =>
        {
            s = DoubleMinusRegex().Replace(s, "+");
            s = DoublePlusRegex().Replace(s, "+");
            return s;
        });

        result = StringPlusRegex().Replace(result, "$1..");
        result = PlusStringRegex().Replace(result, "..$1");

        result = InsertSpaceBetweenNumbersAndKeywordseRegex().Replace(result, "$1 $2");
        result = NativeFunctionRegex().Replace(result, m => InsertComment(m.Value));
        result = LuaKeywordsRegex().Replace(result, "$&_");
        result = ColonMethodRegex().Replace(result, "$2[$1]");
        result = NullRegex().Replace(result, "nil");
        result = NotEqualsRegex().Replace(result, "~=");

        result = DebugRegex().Replace(result, "--debug ");
        result = SetCallConstantRegex().Replace(result, "");
        result = EndIfRegex().Replace(result, "$1");

        result = LocalVarRegex().Replace(result, m =>
        {
            var local = m.Groups[1].Value;
            var line = m.Groups[2].Value;
            return local + ParseVar(line, true);
        });

        result = HexRegex().Replace(result, "0x$1");

        result = LoopRegex().Replace(result, "while true do");
        result = LoopExitWhenIfRegex().Replace(result, "if $1 then break end");
        result = EndLoopRegex().Replace(result, "end");

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
        result = DeleteLineBreaks(result);

        result = ReformatLua(result);
        result = UnpackComment(result);
        result = UnpackString(result);
        result = UnpackRawcode(result);

        if (_options.AddStringPlusOperatorOverload)
        {
            result = "(getmetatable(\"\") or {}).__add = function(a, b) return (a and tostring(a) or \"\") .. (b and tostring(b) or \"\") end\n" + result;
        }

        if (_options.AddGithubAttributionLink)
        {
            result = "--https://github.com/speige/Jass2LuaTranspiler\n\n" + result;
        }

        result = FixCompilerErrors(result, out warnings);

        return result.Replace("\n", "\r\n");
    }

    protected string FixCompilerErrors(string luaScript, out string warnings)
    {
        luaScript ??= "";
        warnings = null;
        int MAX_ERRORS_TO_COMMENT = Math.Max(50, luaScript.Split('\n').Length / 100);
        var originalScript = luaScript;

        bool warningsRepaired = false;

        for (var i = 0; i < MAX_ERRORS_TO_COMMENT; i++)
        {
            using (Lua lua = new Lua())
            {
                try
                {
                    var result = lua.DoString($"return load({WrapLuaScriptInMultiLineString(luaScript)})");
                    if (result[0] is LuaFunction)
                    {
                        return luaScript;
                    }
                    else
                    {
                        throw new NLua.Exceptions.LuaException((string)result[1]);
                    }
                }
                catch (NLua.Exceptions.LuaException ex)
                {
                    int errorLine = ExtractErrorLineIndex(ex); //parse and convert to 0 based due to C# arrays whereas Lua Exceptions are 1 based

                    if (!ex.Message.Contains("'end' expected", StringComparison.InvariantCultureIgnoreCase) && !ex.Message.Contains("unexpected symbol near", StringComparison.InvariantCultureIgnoreCase))
                    {
                        warnings = ex.Message;

                        if (_options.PrependTranspilerWarnings)
                        {
                            warnings = warnings.Replace(":" + (errorLine+1).ToString() + ":", ":" + (errorLine+2).ToString() + ":");
                            luaScript = $"-- TRANSPILER WARNING: {warnings}\n{luaScript}";
                        }

                        return luaScript;
                    }

                    var lines = luaScript.Split(new[] { '\n' }, StringSplitOptions.None).ToList();
                    var indentedLines = IndentLua(lines);

                    if (errorLine < 0 || errorLine >= indentedLines.Count)
                    {
                        //mis-aligned with compiler
                        return originalScript;
                    }

                    var wrongLine = indentedLines[errorLine];
                    var tabCount = wrongLine.Length - wrongLine.TrimStart('\t').Length;
                    while (true)
                    {
                        if (errorLine < 0 || errorLine >= indentedLines.Count)
                        {
                            break;
                        }

                        wrongLine = indentedLines[errorLine];

                        if (wrongLine.Contains("--[[ Jass2LuaTranspiler:"))
                        {
                            //recursive loop fixing compiler errors
                            return originalScript;
                        }    

                        if (tabCount > wrongLine.Length - wrongLine.TrimStart('\t').Length)
                        {
                            break;
                        }

                        lines[errorLine] = $"-- --[[ Jass2LuaTranspiler: Unreachable code after return ]] {lines[errorLine]}";
                        errorLine++;
                    }

                    luaScript = string.Join("\n", lines);
                    if (!warningsRepaired)
                    {
                        luaScript = "-- TRANSPILER NOTE: Some transpiled lines were invalid and commented, please review for accuracy. Search for Jass2LuaTranspiler in script. (Example: Jass ignores code after return, but lua throws a parsing exception. This is a common hack to allow a jass function to return while inside a loop without break)   \n" + luaScript;
                    }
                    warningsRepaired = true;
                }
            }
        }

        return "-- TRANSPILER WARNING: Too many parsing errors found to automatically correct, must be corrected manually. \n" + originalScript;
    }

    protected string WrapLuaScriptInMultiLineString(string luaScript)
    {
        int delimiterLevel = 0;

        while (luaScript.Contains(new string(']', delimiterLevel + 1)) ||
               luaScript.Contains(new string('=', delimiterLevel + 1)))
        {
            delimiterLevel++;
        }

        string openDelimiter = "[" + new string('=', delimiterLevel) + "[";
        string closeDelimiter = "]" + new string('=', delimiterLevel) + "]";

        return $"{openDelimiter}\n{luaScript}\n{closeDelimiter}";
    }

    protected int ExtractErrorLineIndex(NLua.Exceptions.LuaException exception)
    {
        Match match = LuaErrorLineNumberRegex().Match(exception.Message);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int lineNumber))
        {
            return lineNumber-1;
        }

        return -1;
    }
}