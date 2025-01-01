//extracted JS-only & removed "zinc" from https://raw.githubusercontent.com/BribeFromTheHive/vJass2Lua/refs/heads/main/index.html
        function deleteEmmies(str) {
            return ReplaceStr(str,/---@.*/g, "");
        }
        
        function deleteLineBreaks(str) {
			return ReplaceStr(str,/(\r?\n)(?: *\r?\n)*/g, "$1");
        }

        let lastRawResult = "";
        function buildvJassVarFinder(whichWord, capture=true)
        {
            let result = "\\$?\\b" + whichWord + "\\b\\$?";
            if (capture) {
                lastRawResult = result;
                result = "("+result+")";
            }
            return result;
        }
        const rawvJassVar = "[A-Za-z][\\w]*";
        const capturevJassVar = buildvJassVarFinder(rawvJassVar); //captures a vJass variable name to a group.
        const seekvJassVar = lastRawResult;  //finds a vJass variable name (must start with a letter, underscore or $ symbol).
        const seekIndent = "^ *";
        const captureIndent = "("+seekIndent+")";
        const seekToEOL = ".*";
        const captureToEOL = "("+seekToEOL+")";

        const ignoredKeywords = ["type","return","returns","endif","elseif",
        "endwhile","extends","array","static","method","not","and","or",
        "function","module","implement","library","requires","scope",
        "optional","if","else","then","while","true","false","nil","do",
        "end","endfunction","endmethod","type","repeat","until","local",
        "constant","public","private","readonly","for","in","break"];

        const replaceArray1 = new RegExp(seekIndent+capturevJassVar+" +array +"+capturevJassVar+captureToEOL, "m");
        const seekArray     = " *\\[ *(\\d+) *\\]";
        const replaceArray2 = new RegExp("^"+seekArray+seekArray, "m");
        const replaceArray3 = new RegExp("^"+seekArray, "m");
        const replaceVar1   = new RegExp(seekIndent+capturevJassVar+"( +)"+capturevJassVar+captureToEOL, "m");
        
        const seekComment   = /•/;
        const seekLineBreak = "\\r?\\n";
        const seekLineBreakR= new RegExp(seekLineBreak);

        function ReplaceStr(str, regexPattern, replacement)
        {
            return str.replace(regexPattern, replacement);
        }

        function RepeatActionOnString(str, action)
        {
            let tempStr = "";
            while (tempStr != str)
            {
                tempStr = str;
                str = action(str);
            }
            return str;
        }

        function parseVar(line, isLocal=false) {
            //check for array declarations, first
            let newLine = ReplaceStr(line,replaceArray1,
            (_, type, name, remainder) =>
            {
                //let tlen = " ".repeat(type.length) + "      "; //##formatting
                let rawtype = type;
                type = " ---@type "+type
                let result = ReplaceStr(remainder,replaceArray2,
                (_, width, height) =>
                    `${name}=vJass.array2D\(${width}, ${height}\)${type}\[\]\[\] `
                )
                type+="[]";
                if (result == remainder) {
                    result = ReplaceStr(remainder,replaceArray3, (_, size) => `${name}=\{size=${size}\}${type} `);
                    if (result == remainder) {
                        let arrayType;
                        switch (rawtype) {
                            case "integer":
                                            addToIntStack(name);
                            case "real":
                                            arrayType = "0"; break;
                            case "boolean": arrayType = "false"; break;
                            case "string":  arrayType = '""'; break;
                            default:        arrayType = "{}";
                        }
                        if (arrayType != "{}") {
                            arrayType = "__jarray("+arrayType+")";
                        }
                        result = name + "=" + arrayType+type+" " + remainder;
                    }
                }
                return result;
            });
            if (newLine != line) {
                return newLine; //array has been parsed
            }
            return ReplaceStr(line,replaceVar1, 
            (_, type, tlen, name, remainder) => 
            {
                let tail = "";
                let hasComment = remainder.search(seekComment);
                tlen += " ".repeat(type.length);
                if (hasComment >= 0)
                {
                    tail = remainder.substring(hasComment);
                    remainder = remainder.substring(0, hasComment);
                }
                if (type == "integer") addToIntStack(name);
                else if (type == "key") {
                    remainder = "=vJass.key()"+ remainder;
                    type = "integer";
                    tlen = "";
                }

                tail = " ---@type " + type + " " + tail;
                let isSet = remainder.search(/^ *\=/m);
                if (isSet < 0) {
                    if (isLocal)
                        return name + tail; //local variable declaration does not need assignment.
                    return name + "=nil" + tail; //global variable declaration needs assignment to be valid syntax.
                }
                return name + tlen + remainder + tail; //variable with assignment has been parsed
            });
        }
        const findIsolatedVar = /([\w$]+) +([\w$]+)(.*)/g;
        const findIsolatedArray = /([\w$]+) +array +([\w$]+)(.*)/g;

        function parseIsolatedVar(prefix, wholeMatch, w1, w2, index)
        {
            if (ignoredKeywords.includes(w1) || ignoredKeywords.includes(w2)) return wholeMatch;
            return prefix+parseVar(wholeMatch);
        }

        var deleteComments = false;

        //"window" works similarly to Lua's "_G" table in that you can use concatenation to generate variable names dynamically.
        function declarePackage(whichType, encoding, inlinedEncoding="•")
        {
            let array = [];
            encoding = inlinedEncoding+'#'+encoding+'#'
            window["insert"+whichType] = (str) =>
            {
                if (whichType == "Comment"){
                    if (deleteComments) return "";
                    str = "--"+str;
                }
                return encoding+(array.push(str)-1);
            }
            window["unpack"+whichType] = (str) =>
            {
                if (array.length > 0)
                {
                    let finder = new RegExp(encoding+"(\\d+)","g");
                    return ReplaceStr(str,finder, (_, num) => {
                        return array[num];
                    });
                }
                return str;
            }
        }
        declarePackage("Comment", "cmt");
        declarePackage("String", "str", "`");
        declarePackage("Rawcode", "fcc", "`");

        function insertBlockComment(comment)
        {
            return insertComment('[['+comment+']]');
        }
        function getEndBracketIndex(bracketStr, open, close)
        {
            let depth = 0;
            for (let i = 0; i < bracketStr.length; ++i)
            {   
                switch (bracketStr[i]) {
                    case open:
                        depth ++;
                        break;
                    case close:
                        if (--depth <= 0) return i+1;
                }
            }
            return -1; //no endbrackets were found.
        }

        const replaceLineBreak = new RegExp(seekLineBreak, "g");


        let intStack = [];

        let isVarInt = varName => intStack.indexOf(varName) >= 0;

        function addToIntStack(varName)
        {
            if (! isVarInt(varName))
                intStack.push(varName);
        }

		options = {
			spacesPerIndent: 4,
			deleteComments: false,
			commentDebugLines: true,
			deleteExtraLineBreaks: true,
			deleteEmmyAnnotations: false,
			avoidRepeatUntilLoops: false, //temporary workaround
		};

        function parseScript(vJassSource, options) {
            const userDefinedSpacing = options.spacesPerIndent;
            const indentation = " ".repeat(userDefinedSpacing);
            
            deleteComments = options.deleteComments;
            const commentDebug = options.commentDebugLines;

            let noRepeatUntil = options.avoidRepeatUntilLoops;
            let parsing = vJassSource;
            
            parsing = RepeatActionOnString(parsing, str=> ReplaceStr(str,/^([^\r\n\t]*)\t/gm, (_, leadingChars) =>
            {
                let len = leadingChars.length % userDefinedSpacing; //instead of zapping tabs to userDefinedSpacing...
                len = userDefinedSpacing - len;                     //get the remaining length of tabs...
                return leadingChars + " ".repeat(len);              //this preserves sub-indentation (such as when a user aligns = signs)
            })); //example: 17 characters + a tab, the tab should be equal to 3 spaces...
                 //17 mod 4 = 1, 4 - 1 = 3. That's our result.
            
            parsing = ReplaceStr(parsing,/^ *\/\/\! *novjass\b.*?^ *\/\/\! *\bendnovjass\b/gms, str => insertBlockComment('\n'+str)); //novjass blocks
            
            parsing = ReplaceStr(parsing,/(?<!^.*\/\/.*)("(?:[^"\\]|\\"|\\\\)*")/gm, (_, str) => insertString(str));

            parsing = ReplaceStr(parsing,/'(?:[^'\\]|\\'|\\\\){4}'/g, str => insertRawcode('FourCC('+str+')')); //Wrap "hfoo" or "Abil" in FourCC

            parsing = ReplaceStr(parsing,/^([^\/]?)\/\/\!/gm, '$1--!'); //preprocessor requests
            parsing = ReplaceStr(parsing,/\/\/(.*)/g, (_, str) => insertComment(str)); //line comments

            parsing = ReplaceStr(parsing,/\/\*((?:(?!\*\/).)*?)\*\/( *•.*?)*$/gms, (_, a, b="") => insertBlockComment(a)+b); //convert safe block comments that don't have trailing text at the end
            
            parsing = ReplaceStr(parsing,/([;}] *)\/\*((?:(?!\*\/).)*?)\*\//gms, (_, a, b) => a+insertBlockComment(b)); //convert safe block comments that were preceded by a ; or }
            
            parsing = ReplaceStr(parsing,/\/\*(?:(?!\*\/).)*?\*\//gms, ''); //delete all remaining blockquotes as "unsafe" to parse.
            parsing = ReplaceStr(parsing,/^ *native\b.*/gm, str => insertComment(str)); //comment-out natives

            parsing = ReplaceStr(parsing,/\b(?:do|in|end|nil|repeat|until)\b/g, '$&_'); //fix Lua keywords that aren't found in vJass.

            parsing = ReplaceStr(parsing,/([\w\$]+):([\w\$]+)/g, "$2[$1]"); //this is some weird vJass array variety that obviously won't work in Lua.

            //convert conventional null and != to Lua's weird versions of them.
            parsing = ReplaceStr(parsing,/\bnull\b/g, 'nil');
            parsing = ReplaceStr(parsing,/!=/g, '~=');
           
            parsing = ReplaceStr(parsing,/^( *)debug +(?:(?:set|call) *)?/gm, (_,indent) => 
            {
                if (commentDebug) indent+="--debug ";
                return indent;
            });  //convert debug lines to comments
            
            //Miscellaneous parsing:
            parsing = ReplaceStr(parsing,/^( *)(?:set|call|constant) +/gm, '$1');      //these keywords don't exist in Lua
            parsing = ReplaceStr(parsing,/^( *end)if/gm, '$1');
            parsing = ReplaceStr(parsing,/^( *)static +if\b/gm, '$1if');               //static-if is a vJass compile-time optimization, which Lua doesn't have.

            parsing = ReplaceStr(parsing,/\.exists\b/g, '');                           //This vJass feature is the same as simply reading the variable in Lua.
            parsing = ReplaceStr(parsing,/'\\?(.)'/g, (_,char) => char.charCodeAt(0)); //convert 'a' or ';' into their integer equivalents.
            parsing = ReplaceStr(parsing,/\" *\+/g, '\"..'); //..
            parsing = ReplaceStr(parsing,/\+ *\"/g, '..\"'); //fix predictable string cocatenation

            parsing = ReplaceStr(parsing,/(.)\.([\$\w]+) *\(/gm, (_,firstChar,methodCaller) =>
            {
                if (firstChar == " ") firstChar = " self";
                return firstChar + ":"+methodCaller+"("; //treat all x.method() as x:method() just in case we need to pass x as "self".
            });
            //Convert vJass dynamic array declarations
            parsing = ReplaceStr(parsing,/^( *)(?:private\.|public\.)* *type +(\w+) +extends +(\w+) +array *\[ *(\d+) *\]/g, "$1local $2 \= Struct\(\);$2\.size \= $4 ---@type $3\[\]");
            parsing = ReplaceStr(parsing,/^( *)interface\b +([\$\w]+)(.*?)^ *endinterface/gm, '$1Struct $2 = vJass\.interface\(true\)\n$1--[[$3$1]]');

            parsing = ReplaceStr(parsing,/^( *local +)(.*)/gm, (_, local, line) => local + parseVar(line, true));

            //parse scoped functions.
            parsing = ReplaceStr(parsing,/^( *)(private|public)(?: +constant)?\b +function *([\$\w]+)(.*?^ *endfunction)/gms,
            (_, indent, scope, name, body) =>
            {
                body = indent + "local function " +name + body
                if (scope == "public")
                {
                    return body + `\n${indent}_G[SCOPE_PREFIX..'${name}'] = ${name}`;
                }
                return body;
            });
            parsing = ReplaceStr(parsing,/^( *)private +keyword\b/gm, '$1local');

            parsing = ReplaceStr(parsing,/\$([0-9a-fA-F]+[^\$])/g, "0x$1") //JASS "$hexcode" must be converted to "0xhexcode" to work in Lua.

            parsing = ReplaceStr(parsing,/^( *)hook +(\w+) +(\w*(?:\.\w+)*)/gm, '$1vJass\.hook\("$2"\, $3\)');
            
            parsing = ReplaceStr(parsing,/^( *)library +(\w+) *(?:initializer *(\w*))?(.*?)endlibrary/gms,
            (_, indent, name, init, body) => 
            {
                body = `${indent}do  LIBRARY_${name} = true\n${indent}${indentation}local SCOPE_PREFIX = "${name}_" ---@type string ${body}`;
                if (init != undefined) { body += indent + indentation+"OnGlobalInit("+init+")\n"; }
                return body + "\n"+indent+"end";
            });
            parsing = ReplaceStr(parsing,/^( *)(private|public)? *scope +(\w+) *(?:initializer *(\w*))?(.*?)endscope/gms,
            (_, indent, scope, name, init, body) =>
            {
                if (scope != undefined) { name = SCOPE_PREFIX+'.."'+name; } else { name = '"'+name; }
                body = `${indent}do\n${indent}${indentation}local SCOPE_PREFIX = ${name}_" ---@type string ${body}`;
                if (init != undefined) { body += indent + indentation+"OnTrigInit("+init+")\n"; }
                return body + "\n"+indent+"end";
            });

            parsing = RepeatActionOnString(parsing, str=>
            {
                return ReplaceStr(str,/^( *)(loop\b((?!\bendloop\b|\bloop\b).)*\bendloop)/gms,
                (_, indent, contents) =>
                {
                    contents = ReplaceStr(contents,/^loop\s+exitwhen *([^\r\n•]*)(.*end)loop/ms, (_,cond, cont) =>
                    {
                        let original = cond;
                        cond = ReplaceStr(cond, /^ *([\w$]+) *([\<\>\=\~]{1,2}) *([\w$]+) *$/m, (_,w1,compare,w2) =>
                        {
                            switch (compare) //invert comparison to avoid need for "while not"
                            {
                                case "<":   compare = ">="; break;
                                case ">":   compare = "<="; break;
                                case "<=":  compare = ">";  break;
                                case ">=":  compare = "<";  break;
                                case "~=":  compare = "=="; break;
                                default:    compare = "~=";
                            }
                            return w1+" "+compare+" "+w2
                        });
                        if (cond != original)
                            return 'while '+cond+' do '+cont;

                        return 'while not \('+cond+'\) do '+cont;
                    });
                    if (!noRepeatUntil) {
                        contents = ReplaceStr(contents,/^loop(.*)\r?\n *exitwhen([^\n\r•]*)(\s*? *)endloop/ms, 'repeat$1$3until$2');
                    }
                    contents = ReplaceStr(contents,/^loop\b(.*end)loop/ms, 'while true do$1');
                    contents = ReplaceStr(contents,/^( *)exitwhen\b([^\r\n•]*)/gm, '$1if$2 then break end');
                    contents = ReplaceStr(contents,/^( *)if *true *then break end/gm, '$1break');
                    return indent + contents;
                });
            });

            parsing = ReplaceStr(parsing,/^ *globals\b(.*?)\bendglobals\b/gms, (_, globals) =>
            {
                globals = ReplaceStr(globals,/^( *)private(?: +constant)*\b/gm,                         '$1local');
                
                globals = ReplaceStr(globals,/^( *)public +constant +([\$\w]+) +([\$\w]+)([^\n\r]*)/gm, '$1local $2 $3$4\n$1_G\[SCOPE_PREFIX..\"$3\"\] \= $3');
                
                globals = ReplaceStr(globals,/^( *)public +([\$\w]+) +([\$\w]+)\b([^\n\r]*)/gm,         '$1local $2 $3$4\n$1GlobalRemap\(SCOPE_PREFIX..\"$3\"\, function\(\) return $3 end, function\(val\) $3 = val end\)');
                
                globals = ReplaceStr(globals,/^( *(local +)*)(.*)/gm, (_, prefix, isLocal, remainder) => prefix + parseVar(remainder, isLocal));
                return globals;
            });

            const macroHasArgs = /^ takes *(.*)/m;
            
            const macroWrapArgs = /\b\w+\b/g;
            parsing = ReplaceStr(parsing,/^( *)\-\-\! *textmacro +(\w+)(.*?)^ *\-\-\! *endtextmacro/gms, (_, indent, name, body) =>
            {
                let statements = ReplaceStr(body,macroHasArgs, "$1");
                if (statements != body) {
                    let linebreak = statements.search(seekLineBreakR);

                    body = statements;
                    statements = statements.substring(0, linebreak);
                    body = body.substring(linebreak);
                    
                    statements = ReplaceStr(statements,macroWrapArgs, arg => '\"'+arg+'\"');
                    
                    return indent + 'vJass.textmacro(\"'+name+'\", {'+statements+'}, [['+body+indent+']])';
                }
                return indent + `vJass.textmacro("${name}", nil, function(thistype)${body}${indent}end)`;
            });
            parsing = ReplaceStr(parsing,/^( *)\-\-\! *runtextmacro +(?:optional)* *(\w+) *\((.*?)\)/gm, '$1vJass.runtextmacro(\"$2\", $3)');

            const isStructMember = /^( *)(static|readonly|public|private|method) +(.*)/gm
            parsing = ReplaceStr(parsing,/^( *)(private|public)* *(struct|module) *([$\w]+) *(.*?^ *end)(?:struct|module)/gms, (_, indent, scope, strOrMod, name, body) =>
            {
                let linebreak = body.search(seekLineBreakR);
                let head = body.substring(0, linebreak);
                body = body.substring(linebreak);

                const isModule = (strOrMod == "module");

                body = ReplaceStr(body, /\bstub\s+/g, "");
                
                //parse all easily-detectable variable and method declarations
                body = ReplaceStr(body,isStructMember, function(_, indent, scope, line) {
                    if (scope != "static") {
                        line = ReplaceStr(line,/^( *)static *(?:constant +)*/m, '$1') //remove any case of "static" and "constant".
                    }
                    let isMethod;
                    if (! isModule || scope != "private") {
                        isMethod = scope == "method";
                        scope = "thistype"; //only keep "private" for modules so the table knows to point to the module rather than to the implementing struct.
                    }
                    let operator;
                    if (isMethod || line.substring(0,6) == "method") {
                        line = ReplaceStr(line,/^(?:method)* *\b([\$\w]+)\b/m, function(_, name) {
                            if (name != "operator") {
                                return "function "+scope+":"+name;
                            } else {
                                operator = true;
                                return "";
                            }
                        });
                        if (operator) {
                            line = ReplaceStr(line,/^ *(?:(\[ *\])|([\$\w]+)) *([=]*) *(.*)/m, function(_, bracket, word, setter, remainder) {
                                if (bracket != undefined) {
                                    if (setter != "") {
                                        return "function thistype:_setindex("+ReplaceStr(remainder,/takes +[\$\w]+ +([\$\w]+) *, *[\$\w]+ *([\$\w]+\b|\$).*/, "$1, $2)");
                                    } else {
                                        return "function thistype:_getindex("+ReplaceStr(remainder,/takes +[\$\w]+ +([\$\w]+\b|\$).*/, '$1)');
                                    }
                                } else {
                                    if (setter != "") {
                                        return 'function thistype:_operatorset("'+word+'", '+ReplaceStr(remainder,/takes +[\$\w]+ +([\$\w]+\b|\$).*/, 'function(self, $1)');
                                    } else {
                                        return 'function thistype:_operatorget("'+word+'", function(self)';
                                    }
                                }
                            });
                        }
                        line += "\n"+indent+indentation+"local _ENV = Struct.environment(self)"; //needed for Lua to know when to pick up invisible "self" reference.
                    } else {
                        line = scope+'.'+parseVar(line);
                    }
                    return indent + line;
                });

                body = ReplaceStr(body,/( *end)method/gm, "$1function");

                if (isModule) {
                    if (scope == undefined) {scope = ""}
                    head = `vJass.module("${name}", "${scope}", SCOPE_PREFIX, function(private, thistype)` + head;
                    body += ")";
                } else {
                    head = name+" = Struct() --" + head;
                    head = ReplaceStr(head,/\(\) -- *extends +([$\w]+)/, (_, extended) => {
                        if (extended == "array") { return "()"; }
                        return `(${extended}) --`;
                    });
                    if (scope != undefined) {
                        head = "local " + head;
                        if (scope == "public") {
                            head += "\n"+indent + '_G[SCOPE_PREFIX.."'+name+'"] = '+name;
                        }
                    }
                }
                body = ReplaceStr(body,/\bthis\b/g, 'self'); //implied 'this' in vJass doesn't work outside of structs, so we only need this replacer within a struct/module.
                body = ReplaceStr(body,new RegExp("([^\\w\\d\\]\\)])\\."+capturevJassVar, "gm"), '$1self\.$2'); //dot-syntax doesn't work in Lua without something before the dot.
                
                body = ReplaceStr(body,new RegExp("^( *)implement +(?:optional *)*"+capturevJassVar, "gm"), '$1vJass.implement\(\"$2\", SCOPE_PREFIX, thistype\)');
                let parseStructVar = (_,w1,w2,remainder,index) => parseIsolatedVar("thistype.",_,w1,w2,remainder,index);
                body = RepeatActionOnString(body, str=>
                {
                    str = ReplaceStr(str,findIsolatedArray, parseStructVar);
                    str = ReplaceStr(str,findIsolatedVar, parseStructVar);
                    return str;
                });
                body = ReplaceStr(body,/^( *)(public|private) *static(?: *constant)* +/gm, function(_, indent, scope) {
                    if (scope == "") { scope == "thistype"; }
                    return indent+ scope+".";
                });
                if (! isModule) { body = "do\n"+indent+indentation+"local thistype = "+name + body; }
                return indent + head + "\n" + indent + body;
            });

            parsing = ReplaceStr(parsing,/^( *)(?:public|private)* *function interface/gm, "$1---@class")

            //Fix vJass dynamic function calling/referencing.
            parsing = ReplaceStr(parsing,/([\w\$\.]+)[\:\.](name|(?:execute|evaluate))\b *(\(([^()]*)\))?/g, (ignoreMatch,name, reference, hasArgs,args="") =>
            {
                if (name == "vJass")        return ignoreMatch;
                if (reference == "name")    return "vJass.name("+name+")";  //This adds the function to the _G table and returns the string indicating where it is.
                if (hasArgs == undefined)   return ignoreMatch;             //ExecuteFunc will ignore strings that are not pointing to functions stored in the _G table.

                return "vJass."+reference+"("+name+", "+args+")"; //myFunction.execute(1, 10) becomes vJass.execute(myFunction, 1, 10)
            });

            const isNothing = /\bnothing\b/;
            const getArgPairs = new RegExp(capturevJassVar+" +" +capturevJassVar,"g");
            
            const getFunction = /^( *)((?:[\w\$:\[\]\=]+ +)+?|[^\r\n]*?\bfunction )\btakes +([\$\w, ]+ +)*?\breturns +([\$\w]+)(.*?\bend)function\b/gms;
            const findReturn = /return.+\/.+/g;
            const findSetter = /([$\w]+) *\=[^\n\r•]*/g;
            const replaceDiv = /([^\/])\/([^\/])/g;
            function DoFloorInt(line)
            {
                return ReplaceStr(line, replaceDiv, (_, a, b) => a+"//"+b);
            }

            parsing = ReplaceStr(parsing, /^( *)(?:(private|public) +)?type +([\w\$]+) +extends(.*)/gm, (matchFailed,indent, scope, word, remainder)=>
            {
                if (remainder.search(/\bfunction\b/)>=0) {
                    return "---@class "+word; //function interface. Just declare it for Emmy annotation.
                }
                let size;
                remainder = ReplaceStr(remainder, /^[^\[]*\[ *([^,\] ]+)[^•\r\n]*(.*)/m, (_,num, ending) => {
                    size = num;
                    return ending; //extract any comments the user may have included on the same line.
                });
                if (size != undefined)
                {
                    if (remainder == undefined) remainder = "";

                    if (scope != undefined)
                    {
                        if (scope == "public")
                            remainder = "; _G[SCOPE_PREFIX.."+ word +"] = " + word;
                        
                        indent += "local ";
                    }
                    return indent+word + " = vJass.dynamicArray("+size+")"+remainder;
                }
                return "--PARSER ERROR WITH: "+matchFailed
            });

            parsing = RepeatActionOnString(parsing, str=>
            {
                return ReplaceStr(str,getFunction, 
                (_, indent, func, params, rtype, contents) => 
                {
                    let paramEmmy = "";
                    let returnEmmy = "";
                    let argsResult = "";
                    func = func.slice(0, -1); //remove the last space
                    if (rtype != "nothing")
                    {
                        returnEmmy = indent + "---\@return " + rtype + "\n";
                        if (rtype == "integer")
                        {
                            contents = ReplaceStr(contents, findReturn, returnLine => DoFloorInt(returnLine));
                        }
                    }

                    contents = ReplaceStr(contents,/function *([\$\w]+(?:\.[\w\$]+)? *[\)\,])/g, "$1");

                    contents = ReplaceStr(contents, findSetter, (setterLine, varName) =>
                    {
                        if (isVarInt(varName))
                        {
                            setterLine = DoFloorInt(setterLine);
                        }
                        else if (varName == "self")
                        {
                            setterLine += "; _ENV = Struct.environment(self)"; //needed for when the reference is reassigned after the fact.
                        }
                        return setterLine;
                    });
                    if (! params.match(isNothing)) {
                        argsResult = ReplaceStr(params,getArgPairs, (_, type, name) => {
                            paramEmmy += indent + "---\@param " + name + " " + type + "\n";
                            return name;
                        }).slice(0, -1); //remove the last space
                    }
                    return paramEmmy + returnEmmy + indent + func + "(" + argsResult + ")"+contents;
                });
            });
            parsing = ReplaceStr(parsing,/endfunction/g, "end");
			if (options.deleteEmmyAnnotations) {
				parsing = deleteEmmies(parsing);
			}

            parsing = unpackComment(parsing);
            parsing = unpackComment(parsing); //do it again as block comments might have had packed comments
            parsing = unpackString(parsing);
            parsing = unpackRawcode(parsing);
            
			if (options.deleteExtraLineBreaks) {
				parsing = deleteLineBreaks(parsing);
			}

            parsing += "\n--Conversion by vJass2Lua v0.A.2.3";
            return parsing;
        }