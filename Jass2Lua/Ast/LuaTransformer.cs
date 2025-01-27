namespace Jass2Lua.Ast
{
    public static class LuaTransformer
    {
        public static void TransformTree(this LuaAST ast, Func<LuaASTNode, LuaASTNode> action)
        {
            ast.body = (ast.body ?? new List<LuaASTNode>()).Select(x => TransformNode_Recursive(x, action)).ToList();
        }

        public static LuaASTNode TransformNode_Recursive(this LuaASTNode node, Func<LuaASTNode, LuaASTNode> action)
        {
            if (node == null)
            {
                return node;
            }

            var transformed = action(node);

            if (transformed != node)
            {
                if (node.ParentNode != null)
                {
                    node.ParentNode.ReplaceChild(node, transformed);
                }

                if (transformed == null)
                {
                    return null;
                }

                node = transformed;
            }

            var children = node.AllNodes.ToList();
            foreach (var child in children)
            {
                TransformNode_Recursive(child, action);
            }
            return node;
        }
    }
}