using Microsoft.ClearScript.V8;

namespace Jass2Lua.Ast
{
    public class LuaParser
    {
        public static LuaAST ParseScript(string luaScript)
        {
            using (var v8 = new V8ScriptEngine())
            {
                v8.Execute(EmbeddedResources.luaparse_js);
                v8.Script.luaScript = luaScript;
                v8.Execute("ast = JSON.stringify(luaparse.parse(luaScript, { luaVersion: '5.3', locations: true, ranges: true }));");
                var result = LuaAST.FromJson((string)v8.Script.ast);
                return result;
            }
        }
    }
}