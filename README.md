# Jass2LuaTranspiler
## Used to convert WC3 Jass Scripts to Lua syntax.

NOTE: Only standard jass is implemented. If you want to use vJass/etc, workaround is to run the other transpiler first (JassHelper for vJass) to convert to standard jass before converting to lua.

NOTE: Doesn't run a real parser, only uses regexes, this means it will convert anything without warnings, but any jass compiler errors will turn into lua runtime errors. In addition, it will convert snippets without needing to be a full jass script.