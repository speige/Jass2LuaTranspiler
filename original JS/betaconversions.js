//extracted JS-only & removed "zinc" from https://raw.githubusercontent.com/BribeFromTheHive/vJass2Lua/refs/heads/main/betaconversions.html
var options = {
    spacesPerIndent: 4,
    deleteComments: false,
    commentDebugLines: true,
    deleteExtraLineBreaks: true,
    deleteEmmyAnnotations: false,
    avoidRepeatUntilLoops: false,
};

var recent = "";
var converted = false;
var deleteComments = false;
var intStack = [];

function isVarInt(varName) {
    return intStack.indexOf(varName) >= 0;
}

function addToIntStack(varName) {
    if (!isVarInt(varName))
        intStack.push(varName);
}

function ReplaceStr(str, regexPattern, replacement) {
    str = str.replace(regexPattern, replacement);
    return str;
}

function RepeatActionOnString(str, action) {
    let tempStr = "";
    while (tempStr != str) {
        tempStr = str;
        str = action(str);
    }
    return str;
}

function insertComment(str) {
    if (deleteComments) return "";
    return "•#cmt#" + (window.insertCommentArray.push("--" + str) - 1);
}
function insertBlockComment(comment) {
    return insertComment('[[' + comment + ']]');
}
function insertString(str) {
    return "•#str#" + (window.insertStringArray.push(str) - 1);
}
function insertRawcode(str) {
    return "•#fcc#" + (window.insertRawcodeArray.push(str) - 1);
}

function unpackComment(str) {
    if (window.insertCommentArray.length > 0) {
        let finder = new RegExp("•#cmt#(\\d+)", "g");
        return ReplaceStr(str, finder, (_, num) => window.insertCommentArray[num]);
    }
    return str;
}
function unpackString(str) {
    if (window.insertStringArray.length > 0) {
        let finder = new RegExp("•#str#(\\d+)", "g");
        return ReplaceStr(str, finder, (_, num) => window.insertStringArray[num]);
    }
    return str;
}
function unpackRawcode(str) {
    if (window.insertRawcodeArray.length > 0) {
        let finder = new RegExp("•#fcc#(\\d+)", "g");
        return ReplaceStr(str, finder, (_, num) => window.insertRawcodeArray[num]);
    }
    return str;
}

function declarePackage(whichType, encoding, inlinedEncoding="•") {
    let array = [];
    encoding = inlinedEncoding+'#'+encoding+'#'
    window["insert"+whichType+"Array"] = array;
    window["insert"+whichType] = (str) => {
        if (whichType == "Comment"){
            if (deleteComments) return "";
            str = "--"+str;
        }
        return encoding+(array.push(str)-1);
    }
    window["unpack"+whichType] = (str) => {
        if (array.length > 0) {
            let finder = new RegExp(encoding+"(\\d+)","g");
            return ReplaceStr(str,finder, (_, num) => array[num]);
        }
        return str;
    }
}

declarePackage("Comment", "cmt");
declarePackage("String", "str", "`");
declarePackage("Rawcode", "fcc", "`");

function deleteEmmies(str) {
    if (options.deleteEmmyAnnotations) {
        return ReplaceStr(str,/---@.*/g, "");
    }
    return str;
}

function deleteLineBreaks(str) {
    if (options.deleteExtraLineBreaks) {
        return ReplaceStr(str,/(\r?\n)(?: *\r?\n)*/g, "$1");
    }
    return str;
}

function parseVar(line, isLocal=false) {
    const RC2NF = (type) => {
        if (type === "real")
            return "number";
        else if (type === "code")
            return "function";
        return type;
    };
    const replaceArray1 = new RegExp("^ *([A-Za-z][\\w]*) +array +([A-Za-z][\\w]*)(.*)", "m");
    const replaceArray2 = new RegExp("^ *\\[ *(\\d+) *\\]\\[ *(\\d+) *\\]", "m");
    const replaceArray3 = new RegExp("^ *\\[ *(\\d+) *\\]", "m");
    const replaceVar1   = new RegExp("^ *([A-Za-z][\\w]*)( +)([A-Za-z][\\w]*)(.*)", "m");

    function RC2NFline(type, lineToReplace, name, remainder) {
        if (type == "integer") {
            addToIntStack(name);
        } else if (type == "key") {
            remainder = "=vJass.key()" + remainder;
            type = "integer";
        }
        return lineToReplace + " ---@type " + RC2NF(type) + " " + remainder;
    }

    let newLine = ReplaceStr(line, replaceArray1, (_, type, name, remainder) => {
        let rawtype = type;
        type = " ---@type "+RC2NF(type);
        let result = ReplaceStr(remainder,replaceArray2,
        (_, width, height) =>
            `${name}=vJass.array2D(${width}, ${height})${type}[][] `
        );
        type+="[]";
        if (result == remainder) {
            result = ReplaceStr(remainder,replaceArray3, (_, size) => `${name}={size=${size}}${type} `);
            if (result == remainder) {
                let arrayType;
                switch (rawtype) {
                    case "integer":
                                    addToIntStack(name);
                    case "number":
                                    arrayType = "0";
                    break;
                    case "boolean": arrayType = "false";
                    break;
                    case "string":  arrayType = '""';
                    break;
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
        return newLine;
    }
    return ReplaceStr(line, replaceVar1,
    (_, type, tlen, name, remainder) => {
        let tail = "";
        let hasComment = remainder.search(/•/);
        tlen += " ".repeat(type.length);
        if (hasComment >= 0) {
            tail = remainder.substring(hasComment);
            remainder = remainder.substring(0, hasComment);
        }
        if (type == "integer") {
            addToIntStack(name);
        } else if (type == "key") {
            remainder = "=vJass.key()" + remainder;
            type = "integer";
            tlen = "";
        }
        tail = " ---@type " + RC2NF(type) + " " + tail;
        let isSet = remainder.search(/^ *\=/m);
        if (isSet < 0) {
            if (isLocal) return name + tail;
            return name + "=nil" + tail;
        }
        return name + tlen + remainder + tail;
    });
}

function parseIsolatedVar(prefix, wholeMatch, w1, w2, remainder, index) {
    const ignoredKeywords = ["type","return","returns","endif","elseif",
    "endwhile","extends","array","static","method","not","and","or",
    "function","module","implement","library","requires","scope",
    "optional","if","else","then","while","true","false","nil","do",
    "end","endfunction","endmethod","type","repeat","until","local",
    "constant","public","private","readonly","for","in","break"];
    if (ignoredKeywords.includes(w1) || ignoredKeywords.includes(w2)) return wholeMatch;
    return prefix+parseVar(wholeMatch);
}

function parseScript() {
    window.insertCommentArray = [];
    window.insertStringArray = [];
    window.insertRawcodeArray = [];

    let vJassSource = ""; 
    if (typeof options.script === "string") {
        vJassSource = options.script;
    }

    if (converted && (vJassSource.search(/^--Conversion by vJass2Lua/m) >= 0)) {
        return;
    } else {
        converted = true;
    }

    deleteComments = options.deleteComments;
    const commentDebug = options.commentDebugLines;
    let noRepeatUntil = options.avoidRepeatUntilLoops;
    let userDefinedSpacing = options.spacesPerIndent;
    let indentation = " ".repeat(userDefinedSpacing);

    let parsing = vJassSource;

    parsing = RepeatActionOnString(parsing, str=> ReplaceStr(str,/^([^\r\n\t]*)\t/gm, (_, leadingChars) => {
        let len = leadingChars.length % userDefinedSpacing;
        len = userDefinedSpacing - len;
        return leadingChars + " ".repeat(len);
    }));

    parsing = ReplaceStr(parsing,/^ *\/\/\! *novjass\b.*?^ *\/\/\! *\bendnovjass\b/gms, str => insertBlockComment('\n'+str));
    parsing = ReplaceStr(parsing,/" *\+/g, '\"..');
    parsing = ReplaceStr(parsing,/\+ *"/g, '..\"');
    parsing = ReplaceStr(parsing,/("(?:[^"\\]|\\"|\\[\\\w])*?")/gm, (_, str) => insertString(str));
    parsing = ReplaceStr(parsing,/'(?:[^'\\]|\\'|\\\\){4}'/g, str => insertRawcode('FourCC('+str+')'));
    parsing = ReplaceStr(parsing,/^([^\/]?)\/\/\!/gm, '$1--!');
    parsing = ReplaceStr(parsing,/\/\/(.*)/g, (_, str) => insertComment(str));
    parsing = ReplaceStr(parsing,/\/\*((?:(?!\*\/).)*?)\*\/( *•.*?)*$/gms, (_, a, b="") => insertBlockComment(a)+b);
    parsing = ReplaceStr(parsing,/([;}] *)\/\*((?:(?!\*\/).)*?)\*\//gms, (_, a, b) => a+insertBlockComment(b));
    parsing = ReplaceStr(parsing,/\/\*(?:(?!\*\/).)*?\*\//gms, '');

    parsing = ReplaceStr(parsing,/^ *(?:constant)? *native\b.*/gm, str => insertComment(str));

    parsing = ReplaceStr(parsing,/\b(?:do|in|end|nil|repeat|until)\b/g, '$&_');
    parsing = ReplaceStr(parsing,/([\w\$]+):([\w\$]+)/g, "$2[$1]");
    parsing = ReplaceStr(parsing,/\bnull\b/g, 'nil');
    parsing = ReplaceStr(parsing,/!=/g, '~=');

    parsing = ReplaceStr(parsing,/^( *)debug +(?:(?:set|call) *)?/gm, (_,indent) => {
        if (commentDebug) indent+="--debug ";
        return indent;
    });
    parsing = ReplaceStr(parsing,/^( *)(?:set|call|constant) +/gm, '$1');
    parsing = ReplaceStr(parsing,/^( *end)if/gm, '$1');
    parsing = ReplaceStr(parsing,/^( *)static +if\b/gm, '$1if');
    parsing = ReplaceStr(parsing,/\.exists\b/g, '');
    parsing = ReplaceStr(parsing,/'\\?(.)'/g, (_,char) => char.charCodeAt(0));
    parsing = ReplaceStr(parsing,/(.)\.([\$\w]+) *\(/gm, (_,firstChar,methodCaller) => {
        if (firstChar == " ") firstChar = " self";
        return firstChar + ":"+methodCaller+"(";
    });
    parsing = ReplaceStr(parsing,/^( *)private\. *type +(\w+) +extends +(\w+) +array *\[ *(\d+) *\]/g, "$1local $2 = Struct();$2.size = $4 ---@type $3[]");
    parsing = ReplaceStr(parsing,/^( *)interface\b +([\$\w]+)(.*?)^ *endinterface/gm, '$1Struct $2 = vJass.interface(true)\n$1--[[$3$1]]');
    parsing = ReplaceStr(parsing,/^( *)(?:public|private)* *function interface/gm, "$1---@class");

    parsing = ReplaceStr(parsing,/^( *local +)(.*)/gm, (_, local, line) => local + parseVar(line, true));

    parsing = ReplaceStr(parsing,/^( *)(private|public)(?: +constant)?\b +function *([\$\w]+)(.*?^ *endfunction)/gms,
    (_, indent, scope, name, body) => {
        body = indent + "local function " + name + body;
        if (scope == "public") {
            return body + `\n${indent}_G[SCOPE_PREFIX..'${name}'] = ${name}`;
        }
        return body;
    });

    parsing = ReplaceStr(parsing,/^( *)private +keyword\b/gm, '$1local');
    parsing = ReplaceStr(parsing,/\$([0-9a-fA-F]+[^\$])/g, "0x$1");
    parsing = ReplaceStr(parsing,/^( *)hook +(\w+) +(\w*(?:\.\w+)*)/gm, '$1vJass.hook("$2", $3)');

    parsing = ReplaceStr(parsing,/^( *)library +(\w+) *(?:initializer *(\w*))?([^\r\n]*)(.*?)endlibrary/gms,
    (_, indent, name, init, requirements, body) => {
        let reqLines = "";
        if (requirements !== undefined) {
            ReplaceStr(requirements,/(?:requires|needs|uses) +(.*)/m,
            (_, reqs) => {
                ReplaceStr(reqs, /(optional)? *(\w+)/g,
                (_, opt="", libName) => {
                    if (opt !== "") {
                        opt = ".optionally";
                    }
                    reqLines += "\n" + indent + indentation + "Require" + opt + " '" + libName + "'";
                    return "";
                });
                return "";
            });
        }
        body = `${indent}OnInit("${name}", function()${reqLines}\n${indent}${indentation}LIBRARY_${name} = true\n${indent}${indentation}local SCOPE_PREFIX = "${name}_" ---@type string ${body}`;
        if (init != undefined) { body += indent + indentation+"Require 'Init vJass Libraries'; "+init+"()\n"; }
        return body + "\n"+indent+"end)";
    });

    parsing = ReplaceStr(parsing,/^( *)(private|public)? *scope +(\w+) *(?:initializer *(\w*))?(.*?)endscope/gms,
    (_, indent, scope, name, init, body) => {
        if (scope != undefined) { name = SCOPE_PREFIX+'.."'+name; } else { name = '"'+name; }
        body = `${indent}OnInit(function()\n${indent}${indentation}local SCOPE_PREFIX = ${name}_" ---@type string ${body}`;
        if (init != undefined) { body += indent + indentation+"Require 'Init vJass Scopes'; "+init+"()\n"; }
        return body + "\n"+indent+"end)";
    });

    parsing = RepeatActionOnString(parsing, str => {
        return ReplaceStr(str,/^( *)(loop\b((?!\bendloop\b|\bloop\b).)*\bendloop)/gms,
        (_, indent, contents) => {
            contents = ReplaceStr(contents,/^loop\s+exitwhen *([^\r\n•]*)(.*end)loop/ms, (_,cond, cont) => {
                let original = cond;
                cond = ReplaceStr(cond, /^ *([\w$]+) *([\<\>\=\~]{1,2}) *([\w$]+) *$/m, (_,w1,compare,w2) => {
                    switch (compare) {
                        case "<":   compare = ">="; break;
                        case ">":   compare = "<="; break;
                        case "<=":  compare = ">";  break;
                        case ">=":  compare = "<";  break;
                        case "~=":  compare = "=="; break;
                        default:    compare = "~=";
                    }
                    return w1+" "+compare+" "+w2;
                });
                if (cond != original)
                    return 'while '+cond+' do '+cont;
                return 'while not ('+cond+') do '+cont;
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

    parsing = ReplaceStr(parsing,/^ *globals\b(.*?)\bendglobals\b/gms, (_, globals) => {
        globals = ReplaceStr(globals,/^( *)private(?: +constant)*\b/gm, '$1local');
        globals = ReplaceStr(globals,/^( *)public +constant +([\$\w]+) +([\$\w]+)([^\n\r]*)/gm, '$1local $2 $3$4\n$1_G[SCOPE_PREFIX.."$3"] = $3');
        globals = ReplaceStr(globals,/^( *)public +([\$\w]+) +([\$\w]+)\b([^\n\r]*)/gm, '$1local $2 $3$4\n$1GlobalRemap(SCOPE_PREFIX.."$3", function() return $3 end, function(val) $3 = val end)');
        globals = ReplaceStr(globals,/^( *(local +)*)(.*)/gm, (_, prefix, isLocal, remainder) => prefix + parseVar(remainder, isLocal));
        return globals;
    });

    parsing = ReplaceStr(parsing,/([\w\$\.]+)[\:\.](name|(?:execute|evaluate))\b *(\(([^()]*)\))?/g,
    (ignoreMatch,name, reference, hasArgs,args="") => {
        if (name == "vJass") return ignoreMatch;
        if (reference == "name") return "vJass.name("+name+")";
        if (hasArgs == undefined) return ignoreMatch;
        return "vJass."+reference+"("+name+", "+args+")";
    });

    function DoFloorInt(line) {
        return ReplaceStr(line, /([^\/])\/([^\/])/g, (_, a, b) => a+"//"+b);
    }

    parsing = ReplaceStr(parsing,/^( *)(?:(private|public) +)?type +([\w\$]+)( +)extends(.*)/gm,
    (matchFailed,indent, scope, word, gap, remainder="") => {
        if (remainder.search(/\bfunction\b/)>=0) {
            return "---@class "+word+":function --";
        }
        let size;
        remainder = ReplaceStr(remainder, /^[^\[]*\[ *([^,\] ]+)[^•\r\n]*(.*)/m, (_,num, ending) => {
            size = num;
            return ending;
        });
        if (size != undefined) {
            if (scope != undefined) {
                if (scope == "public")
                    remainder = "; _G[SCOPE_PREFIX.."+ word +"] = " + word;
                indent += "local ";
            }
            return indent+word + gap + " = vJass.dynamicArray("+size+")"+remainder;
        }
        return indent + "---@class " + word + ": " + gap + remainder;
    });

    parsing = RepeatActionOnString(parsing, str => {
        return ReplaceStr(str,/^( *)((?:[\w\$:\[\]\=]+ +)+?|[^\r\n]*?\bfunction )\btakes +([\$\w, ]+ +)*?\breturns +([\$\w]+)(.*?\bend)function\b/gms, 
        (_, indent, func, params, rtype, contents) => {
            const RC2NF = (type) => {
                if (type === "real") return "number";
                else if (type === "code") return "function";
                return type;
            };
            let paramEmmy = "";
            let returnEmmy = "";
            let argsResult = "";
            func = func.slice(0, -1);
            if (rtype != "nothing") {
                rtype = RC2NF(rtype);
                if (options.useAlias) {
                    returnEmmy = ":" + rtype;
                } else {
                    returnEmmy = indent + "---@param return " + rtype + "\n";
                    returnEmmy = returnEmmy.replace("return", "return");
                }
                if (rtype == "integer") {
                    contents = ReplaceStr(contents, /return.+\/.+/g, returnLine => DoFloorInt(returnLine));
                }
            }
            contents = ReplaceStr(contents,/function *([\$\w]+(?:\.[\w\$]+)? *[\)\,])/g, "$1");
            contents = ReplaceStr(contents, /([$\w]+) *\=[^=\n\r•][^\n\r•]*/g, (setterLine, varName) => {
                if (isVarInt(varName)) {
                    setterLine = DoFloorInt(setterLine);
                } else if (varName == "self") {
                    setterLine += "; _ENV = Struct.environment(self)";
                }
                return setterLine;
            });
            let doEmmyParse = (type, name) => {
                if (options.useAlias) {
                    paramEmmy += name + ": " + type + ", ";
                } else {
                    paramEmmy += indent + "---@param " + name + " " + type + "\n";
                }
            };
            if (func.includes(":")) doEmmyParse("thistype", "self");
            if (params && params.search(/\bnothing\b/) < 0) {
                params = ReplaceStr(params,/([A-Za-z][\w]*) +([A-Za-z][\w]+)/g, (_, type, name) => {
                    type = RC2NF(type);
                    doEmmyParse(type, name);
                    return name;
                }).trim();
                argsResult = params.replace(/[,\s]+/g, ", ");
            }
            let emmyResult = "";
            let allowEmmyParse = true;
            if (options.useAlias) {
                if (paramEmmy !== "") {
                    paramEmmy = paramEmmy.slice(0, -2);
                } else if (returnEmmy === "") {
                    allowEmmyParse = false;
                }
                if (allowEmmyParse) {
                    emmyResult = indent + "---@type fun(" + paramEmmy + ")" + returnEmmy + "\n";
                }
            } else {
                emmyResult = paramEmmy + returnEmmy;
            }
            return emmyResult + indent + func + "(" + argsResult + ")" + contents;
        });
    });

    parsing = ReplaceStr(parsing,/endfunction/g, "end");
    parsing = deleteEmmies(parsing);
    parsing = unpackComment(parsing);
    parsing = unpackComment(parsing);
    parsing = unpackString(parsing);
    parsing = unpackRawcode(parsing);
    parsing = deleteLineBreaks(parsing);
    recent = vJassSource;
    parsing += "\n--Conversion by vJass2Lua v0.A.3.0 beta";

    if (options.autoCopy) {
        try {
            navigator.clipboard.writeText(parsing);
        } catch(e) {}
    }

    options.result = parsing;
}
