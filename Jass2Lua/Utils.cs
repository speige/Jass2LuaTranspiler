namespace Jass2Lua
{
    public static class EmbeddedResources
    {
        public static string helperFunctions_lua = GetEmbeddedResource("Jass2Lua.EmbeddedResources.helperFunctions.lua");
        public static string luaparse_js = GetEmbeddedResource("Jass2Lua.EmbeddedResources.luaparse.js");
        public static string common_j = GetEmbeddedResource("Jass2Lua.EmbeddedResources.common.j");
        public static string blizzard_j = GetEmbeddedResource("Jass2Lua.EmbeddedResources.Blizzard.j");

        private static string GetEmbeddedResource(string resourceName)
        {
            var assembly = typeof(Jass2LuaTranspiler).Assembly;
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return null;
                }

                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}