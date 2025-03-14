﻿using System.Text.RegularExpressions;
using Jass2Lua.Ast;
using NLua;
using SourcemapToolkit.SourcemapParser;

namespace Jass2Lua
{
    public partial class Jass2LuaTranspiler
    {
        protected const string SOURCE_MAP_LINE_COMMENT_PREFIX = "Jass2LuaTranspiler_SOURCE_MAP_LINE_INDEX:";
        protected static JassVariableType NativeGlobalScope;
        static Jass2LuaTranspiler()
        {
            var natives = EmbeddedResources.common_j + "\n\n" + EmbeddedResources.blizzard_j;
            natives = NativeFunctionRegex().Replace(natives, mm => "function " + mm.Groups[1].Value);

            NativeGlobalScope = new JassVariableType();
            foreach (var match in FunctionDeclarationRegex().Matches(natives).Cast<Match>())
            {
                var functionName = match.Groups[2].Value.Trim();
                var returnType = match.Groups[4].Success ? match.Groups[4].Value : null;
                NativeGlobalScope[functionName] = returnType;
            }
        }

        public class Options
        {
            public bool DeleteComments = false;
            public bool AddGithubAttributionLink = true;
            public bool PrependTranspilerWarnings = true;
        }

        [GeneratedRegex(@"^[ \t]*return[ \t]*$", RegexOptions.Multiline)]
        protected static partial Regex BlankReturnRegex();

        [GeneratedRegex(@"^[^[]*\[\d+:(\d+)\]")]
        protected static partial Regex ClearScriptLuaParseJSErrorCharIndexRegex();

        [GeneratedRegex(@"^\[[^]]*\]:(\d+):")]
        protected static partial Regex NLuaErrorLineNumberRegex();

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

        [GeneratedRegex(@"(?<String>""(?:\\.|[^""\\])*"")|(?<Rawcode>'(?:\\.|[^'\\])*')|(?<Comment>//.*?$)", RegexOptions.Multiline)]
        protected static partial Regex StringOrRawCodeOrCommentRegex();

        [GeneratedRegex(@"^[ \t]*(?:constant)?[ \t]*native\b(.*)", RegexOptions.Multiline)]
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

        [GeneratedRegex(@"^((?:[\w\$:\[\]\=]+[ \t]+)+?|[^\n]*?\bfunction[ \t]+)([\$\w]+)[ \t]+\btakes[ \t]+([\$\w, ]+[ \t]+)*?\breturns[ \t]+([\$\w]+)", RegexOptions.Multiline)]
        protected static partial Regex FunctionDeclarationRegex();

        [GeneratedRegex(@"^((?:[\w\$:\[\]\=]+[ \t]+)+?|[^\n]*?\bfunction[ \t]+)([\$\w]+)[ \t]+\btakes[ \t]+([\$\w, ]+[ \t]+)*?\breturns[ \t]+([\$\w]+)(.*?\bend)function\b", RegexOptions.Singleline | RegexOptions.Multiline)]
        protected static partial Regex FunctionDeclarationAndBodyRegex();

        [GeneratedRegex(@"([A-Za-z][\w]*)[ \t]+([A-Za-z][\w]*)")]
        protected static partial Regex ParamRegex();

        [GeneratedRegex(@"[,\s]+")]
        protected static partial Regex ParamCommaRegex();

        [GeneratedRegex(@"(\=|\(|,)[ \t]*function\s+([\w$]+)")]
        protected static partial Regex FunctionPointerRegex();

        [GeneratedRegex(@"endfunction")]
        protected static partial Regex EndFunctionRegex();

        [GeneratedRegex(@"(\n)(?:[ \t]*\n)*")]
        protected static partial Regex LineBreakRegex();

        [GeneratedRegex(@"^[ \t]*globals\b(.*?)\bendglobals\b", RegexOptions.Singleline | RegexOptions.Multiline)]
        protected static partial Regex GlobalsRegex();

        [GeneratedRegex(@"^[ \t]*([\w$]+)[ \t]*([\<\>\=\~]{1,2})[ \t]*([\w$]+)[ \t]*$")]
        protected static partial Regex ConditionCompareRegex();

        [GeneratedRegex(@"^((local[ \t]+)*)(.*)", RegexOptions.Multiline)]
        protected static partial Regex LocalGlobalRegex();

        [GeneratedRegex(@"^[ \t]*\=", RegexOptions.Multiline)]
        protected static partial Regex IsSetRegex();

        [GeneratedRegex(@"^([$\w]+)")]
        protected static partial Regex VarNameRegex();

        [GeneratedRegex(@"-[ \t]*-")]
        protected static partial Regex DoubleMinusRegex();

        [GeneratedRegex(@"(\+[ \t]*){2,}")]
        protected static partial Regex MultiPlusRegex();

        protected readonly Options _options;
        protected List<string> _commentList;
        protected List<string> _stringList;
        protected List<string> _rawcodeList;

        protected class JassVariableType : Dictionary<string, string> { }
        protected class JassFunctionLocalVariableTypes : Dictionary<string, JassVariableType> { }
        protected JassFunctionLocalVariableTypes _variablesPerScope;
        const string GLOBAL_SCOPE = "_G";

        protected string GetVariableTypeFromScope(string scope, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            if (scope != null && _variablesPerScope.TryGetValue(scope, out var variablesInScope) && variablesInScope.TryGetValue(name, out var type))
            {
                return type;
            }

            if (scope == GLOBAL_SCOPE)
            {
                return null;
            }

            return GetVariableTypeFromScope(GLOBAL_SCOPE, name);
        }

        protected void AddVariableTypeToScope(string scope, string name, string type)
        {
            if (!_variablesPerScope.ContainsKey(scope))
            {
                _variablesPerScope[scope] = new JassVariableType();
            }
            _variablesPerScope[scope][name] = type;
        }

        public Jass2LuaTranspiler(Options options = null)
        {
            _options = options ?? new Options();
            _variablesPerScope = new JassFunctionLocalVariableTypes();
            _variablesPerScope[GLOBAL_SCOPE] = new JassVariableType();
            foreach (var item in NativeGlobalScope)
            {
                _variablesPerScope[GLOBAL_SCOPE][item.Key] = item.Value;
            }
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

            _commentList.Add($"{str}");
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

        protected string UnpackComments(string str)
        {
            return RepeatActionOnString(str, s => {
                return InsertCommentRegex().Replace(s, m =>
                {
                    return "--" + _commentList[int.Parse(m.Groups[1].Value)];
                });
            });
        }

        protected string UnpackStrings(string str)
        {
            if (_stringList.Count > 0)
            {
                return InsertStringRegex().Replace(str, m => _stringList[int.Parse(m.Groups[1].Value)]);
            }
            return str;
        }

        protected string UnpackRawcodes(string str)
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

        protected string ParseVar(string scope, string line, bool isLocal = false)
        {
            var newLine = ParseVarReplaceArray1Regex().Replace(line, m =>
            {
                var name = m.Groups[2].Value;
                var remainder = m.Groups[3].Value;
                var rawtype = m.Groups[1].Value;
                var result = remainder;

                AddVariableTypeToScope(scope, name, rawtype);

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

                AddVariableTypeToScope(scope, name, type);

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

        protected string AddSourceMapComments(string jassScript)
        {
            const string prefix = "//" + SOURCE_MAP_LINE_COMMENT_PREFIX;
            var lines = jassScript.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
            var count = lines.Count;
            for (var i = count-1; i > 0; i--)
            {
                lines.Insert(i, prefix + i);
            }
            return string.Join("\n", lines);
        }

        protected string RemoveSourceMapComments(string transpiledLuaScript, out string sourceMap)
        {
            const string prefix = "--" + SOURCE_MAP_LINE_COMMENT_PREFIX;

            var lines = transpiledLuaScript.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var withoutComment = new List<string>();
            var mappings = new List<MappingEntry>();
            var removedLineCount = 0;

            for (var lineIdx = 0; lineIdx < lines.Length; lineIdx++)
            {
                var line = lines[lineIdx];
                
                var commentIdx = line.IndexOf(prefix);
                if (commentIdx != -1)
                {
                    if (int.TryParse(line.Substring(commentIdx + prefix.Length), out var originalLineNumber))
                    {
                        var mappingEntry = new MappingEntry
                        {
                            GeneratedSourcePosition = new SourcePosition() { ZeroBasedLineNumber = lineIdx - removedLineCount },
                            OriginalSourcePosition = new SourcePosition() { ZeroBasedLineNumber = originalLineNumber },
                            OriginalFileName = "original.j"
                        };
                        mappings.Add(mappingEntry);
                        removedLineCount++;
                    }
                }
                else
                {
                    withoutComment.Add(line);
                }
            }

            var _sourceMapGenerator = new SourceMapGenerator();
            sourceMap = _sourceMapGenerator.SerializeMapping(new SourceMap() { ParsedMappings = mappings, Version = 3, File = "transpiled.lua", Sources = new List<string>() { "original.j" } });

            return string.Join("\r\n", withoutComment);
        }

        public string Transpile(string jassScript, out string warnings, out string sourceMap)
        {
            if (!typeof(Lua).Assembly.FullName.Contains("Version=1.4.32.0"))
            {
                throw new Exception("Missing or wrong version of NLua dll. WC3 requires Lua 5.3 which matches NLua dll 1.4.32.0");
            }

            _commentList = new List<string>();
            _stringList = new List<string>();
            _rawcodeList = new List<string>();

            var result = AddSourceMapComments(jassScript);

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
                    return InsertComment(comment);
                }
                else if (m.Groups["Rawcode"].Success)
                {
                    var rawcode = m.Value.Substring(1, m.Value.Length - 2);
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
                s = MultiPlusRegex().Replace(s, "+");
                return s;
            });

            result = InsertSpaceBetweenNumbersAndKeywordseRegex().Replace(result, "$1 $2");
            result = NativeFunctionRegex().Replace(result, m => InsertComment(m.Value));
            result = LuaKeywordsRegex().Replace(result, "$&_");
            result = BlankReturnRegex().Replace(result, "return nil");
            result = ColonMethodRegex().Replace(result, "$2[$1]");
            result = NullRegex().Replace(result, "nil");
            result = NotEqualsRegex().Replace(result, "~=");

            result = DebugRegex().Replace(result, "--debug ");
            result = SetCallConstantRegex().Replace(result, "");
            result = EndIfRegex().Replace(result, "$1");

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
                    return ParseVar(GLOBAL_SCOPE, remainder, isLocal);
                });
                return globals;
            });
            
            result = RepeatActionOnString(result, s =>
            {
                return FunctionDeclarationAndBodyRegex().Replace(s, mm =>
                {
                    var functionName = mm.Groups[2].Value.Trim();
                    var parameters = mm.Groups[3].Success ? mm.Groups[3].Value : null;
                    var returnType = mm.Groups[4].Success ? mm.Groups[4].Value : null;
                    var contents = mm.Groups[5].Value;

                    AddVariableTypeToScope(GLOBAL_SCOPE, functionName, returnType);

                    contents = ParseScriptFunctionRegex().Replace(contents, "$1");
                    
                    if (parameters != null)
                    {
                        parameters = parameters.Trim();
                        if (parameters.Equals("nothing", StringComparison.OrdinalIgnoreCase))
                        {
                            parameters = "";
                        }
                        else if (parameters.IndexOf("nothing", StringComparison.Ordinal) < 0)
                        {
                            parameters = ParamRegex().Replace(parameters, mm2 => {
                                var type = mm2.Groups[1].Value;
                                var name = mm2.Groups[2].Value;

                                AddVariableTypeToScope(functionName, name, type);

                                return name;
                            });
                            parameters = ParamCommaRegex().Replace(parameters, ", ");
                        }
                    }

                    contents = LocalVarRegex().Replace(contents, m =>
                    {
                        var local = m.Groups[1].Value;
                        var line = m.Groups[2].Value;
                        return local + ParseVar(functionName, line, true);
                    });

                    return "function " + functionName + "(" + (parameters ?? "") + ")" + contents;
                });
            });

            result = FunctionPointerRegex().Replace(result, "$1 $2");
            result = EndFunctionRegex().Replace(result, "end");
            result = DeleteLineBreaks(result);

            result = UnpackComments(result);
            result = UnpackStrings(result);
            result = UnpackRawcodes(result);

            if (_options.AddGithubAttributionLink)
            {
                result = "--https://github.com/speige/Jass2LuaTranspiler\n\n" + result;
            }

            result = FixCompilerErrors(result, out warnings);

            result = FixJassSideEffects(result);

            return RemoveSourceMapComments(result, out sourceMap);
        }

        protected LuaASTNode GetScope(LuaASTNode node)
        {
            LuaASTNode result = null;
            var parentNode = node;
            while (parentNode.ParentNode != null)
            {
                parentNode = parentNode.ParentNode;
                if (parentNode.type == LuaASTType.FunctionDeclaration)
                {
                    return parentNode;
                }
            }

            return result;
        }

        protected bool IsIntegerExpression(LuaASTNode expression)
        {
            var scope = GetScope(expression);

            if (expression.type == LuaASTType.NumericLiteral)
            {
                return int.TryParse(expression.value.TrimEnd('.'), out _);
            }

            if (expression.type == LuaASTType.Identifier)
            {
                var variableType = GetVariableTypeFromScope(scope?.identifier?.name, expression.name);
                return string.Equals(variableType, "integer", StringComparison.InvariantCultureIgnoreCase);
            }

            if (expression.type == LuaASTType.CallExpression)
            {
                var functionReturnType = GetVariableTypeFromScope(GLOBAL_SCOPE, expression.@base?.name);
                return string.Equals(functionReturnType, "integer", StringComparison.InvariantCultureIgnoreCase);
            }

            foreach (var node in expression.AllNodes)
            {
                if (!IsIntegerExpression(node))
                {
                    return false;
                }
            }

            return true;
        }

        protected bool IsStringExpression(LuaASTNode expression)
        {
            var scope = GetScope(expression);

            if (expression.type == LuaASTType.StringLiteral)
            {
                return true;
            }

            if (expression.type == LuaASTType.Identifier)
            {
                var variableType = GetVariableTypeFromScope(scope?.identifier?.name, expression.name);
                return string.Equals(variableType, "string", StringComparison.InvariantCultureIgnoreCase);
            }

            if (expression.type == LuaASTType.CallExpression)
            {
                var functionReturnType = GetVariableTypeFromScope(GLOBAL_SCOPE, expression.@base?.name);
                return string.Equals(functionReturnType, "string", StringComparison.InvariantCultureIgnoreCase);
            }

            foreach (var node in expression.AllNodes)
            {
                if (IsStringExpression(node))
                {
                    return true;
                }
            }

            return false;
        }

        protected string FixJassSideEffects(string luaScript)
        {
            var parsed = LuaParser.ParseScript(luaScript);

            FixIntegerDivision(parsed);
            FixStringConcatenation(parsed);

            return LuaRenderer.Render(parsed);
        }

        protected void FixIntegerDivision(LuaAST parsed)
        {
            parsed.TransformTree(x =>
            {
                if (x == null)
                {
                    return x;
                }

                if (x.type != LuaASTType.BinaryExpression || x.@operator != "/")
                {
                    return x;
                }

                var leftInteger = IsIntegerExpression(x.left);
                var rightInteger = IsIntegerExpression(x.right);
                if (leftInteger && rightInteger)
                {
                    var result = new LuaASTNode()
                    {
                        type = LuaASTType.BinaryExpression,
                        @operator = "//",
                        left = x.left,
                        right = x.right,
                        ParentNode = x.ParentNode
                    };
                    result.SetParentNodeOfChildren(false);
                    return result;
                }

                return x;
            });
        }

        protected void FixStringConcatenation(LuaAST parsed)
        {
            parsed.TransformTree(x =>
            {
                if (x.type != LuaASTType.BinaryExpression || x.@operator != "+")
                {
                    return x;
                }

                var leftString = IsStringExpression(x.left);
                var rightString = IsStringExpression(x.right);
                if (leftString || rightString)
                {
                    var result = new LuaASTNode()
                    {
                        type = LuaASTType.BinaryExpression,
                        @operator = "..",
                        left = x.left,
                        right = x.right,
                        ParentNode = x.ParentNode
                    };
                    result.SetParentNodeOfChildren(false);
                    return result;
                }

                return x;
            });
        }
        /*
        protected void FixMissingParenthesisTripleBooleanOperatorLeftToRightPrecedence(LuaAST parsed)
        {
        }
        */

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

                        var lines = luaScript.Split(new[] { '\n' }, StringSplitOptions.None).ToList();

                        if (ex.Message.Contains("unexpected symbol near '+'", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var unaryPlus = lines[errorLine];
                            try
                            {
                                var shouldThrow = LuaParser.ParseScript(unaryPlus);
                            }
                            catch (Microsoft.ClearScript.ScriptEngineException e)
                            {
                                var errorCharIndex = ExtractErrorCharIndex(e);
                                lines[errorLine] = unaryPlus.Remove(errorCharIndex, 1);
                                luaScript = string.Join("\n", lines);
                                continue;
                            }
                        }

                        if (!ex.Message.Contains("'end' expected", StringComparison.InvariantCultureIgnoreCase) && !ex.Message.Contains("unexpected symbol near", StringComparison.InvariantCultureIgnoreCase))
                        {
                            warnings = ex.Message;

                            if (_options.PrependTranspilerWarnings)
                            {
                                warnings = warnings.Replace(":" + (errorLine + 1).ToString() + ":", ":" + (errorLine + 2).ToString() + ":");
                                luaScript = $"-- TRANSPILER WARNING: {warnings}\n{luaScript}";
                            }

                            return luaScript;
                        }

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
                            luaScript = "--TRANSPILER NOTE: Some transpiled lines were invalid and commented, please review for accuracy. Search for Jass2LuaTranspiler in script. (Example: Jass ignores code after return, but lua throws a parsing exception. This is a common hack to allow a jass function to return while inside a loop without break)   \n" + luaScript;
                        }
                        warningsRepaired = true;
                    }
                }
            }

            return "--TRANSPILER WARNING: Too many parsing errors found to automatically correct, must be corrected manually. \n" + originalScript;
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

        protected int ExtractErrorCharIndex(Microsoft.ClearScript.ScriptEngineException exception)
        {
            Match match = ClearScriptLuaParseJSErrorCharIndexRegex().Match(exception.Message);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int charIndex))
            {
                return charIndex;
            }

            return -1;
        }

        protected int ExtractErrorLineIndex(NLua.Exceptions.LuaException exception)
        {
            Match match = NLuaErrorLineNumberRegex().Match(exception.Message);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int lineNumber))
            {
                return lineNumber - 1;
            }

            return -1;
        }
    }
}