using Microsoft.ClearScript.V8;
using System.Text;

namespace Jass2Lua
{
    public class LuaParser
    {
        public static LuaAST ParseScript(string luaScript)
        {
            using (var v8 = new V8ScriptEngine())
            {
                v8.Execute(EmbeddedResources.luaparse_js);

                v8.Script.luaScript = luaScript;
                v8.Execute("ast = JSON.stringify(luaparse.parse(luaScript, { luaVersion: '5.3', ranges: 'true' }));");
                var result = LuaAST.FromJson((string)v8.Script.ast);
                return result;
            }
        }

        public static void TransformTree(LuaAST ast, Func<LuaASTNode, LuaASTNode> action)
        {
            ast.body = (ast.body ?? new List<LuaASTNode>()).Select(x => TransformNode_Recursive(x, action)).ToList();
        }

        public static LuaASTNode TransformNode_Recursive(LuaASTNode node, Func<LuaASTNode, LuaASTNode> action)
        {
            var transformed = action(node);
            if (transformed != node)
            {
                if (transformed.ParentNode != null)
                {
                    transformed.ParentNode.ReplaceChild(node, transformed);
                }
                return transformed;
            }

            var children = node.AllNodes.ToList();
            foreach (var child in children)
            {
                TransformNode_Recursive(child, action);
            }
            return node;
        }

        public static string RenderLuaAST(LuaAST luaAST)
        {
            return RenderLuaASTNodes(luaAST.body, "\n", 0).ToString();
        }

        protected static string RenderLuaASTNodes(IEnumerable<LuaASTNode> nodes, string separator, int indentationLevel)
        {
            if (nodes == null || !nodes.Any())
            {
                return "";
            }

            var result = new StringBuilder();
            foreach (var field in nodes)
            {
                var node = RenderLuaASTNode(field, indentationLevel);
                if (!string.IsNullOrWhiteSpace(node))
                {
                    result.Append(node);
                    result.Append(separator);
                }
            }

            result.Remove(result.Length - separator.Length, separator.Length);
            return result.ToString();
        }

        public static string RenderLuaASTNode(LuaASTNode luaAST, int indentationLevel = 0)
        {
            if (luaAST == null)
            {
                return "";
            }

            var indentation = new string('\t', indentationLevel);
            switch (luaAST.type)
            {
                case LuaASTType.AssignmentStatement:
                    return $"{indentation}{RenderLuaASTNodes(luaAST.variables, ", ", indentationLevel)} = {RenderLuaASTNodes(luaAST.init, ", ", indentationLevel)}";

                case LuaASTType.BinaryExpression:
                    return $"({RenderLuaASTNode(luaAST.left, indentationLevel)} {luaAST.@operator} {RenderLuaASTNode(luaAST.right, indentationLevel)})";

                case LuaASTType.BooleanLiteral:
                    return luaAST.raw;

                case LuaASTType.BreakStatement:
                    return $"{indentation}break";

                case LuaASTType.CallExpression:
                    return $"{RenderLuaASTNode(luaAST.@base, indentationLevel)}({RenderLuaASTNodes(luaAST.arguments, ", ", indentationLevel)})";

                case LuaASTType.CallStatement:
                    return $"{indentation}{RenderLuaASTNode(luaAST.expression, indentationLevel)}";

                case LuaASTType.Comment:
                    return $"{indentation}{luaAST.raw}";

                case LuaASTType.DoStatement:
                    return $"{indentation}do\n{RenderLuaASTNodes(luaAST.body, "\n", indentationLevel + 1)}\n{indentation}end";

                case LuaASTType.ElseClause:
                    return $"{indentation}else\n{RenderLuaASTNodes(luaAST.body, "\n", indentationLevel + 1)}";

                case LuaASTType.ElseifClause:
                    return $"{indentation}elseif {RenderLuaASTNode(luaAST.condition, indentationLevel)} then\n{RenderLuaASTNodes(luaAST.body, "\n", indentationLevel + 1)}";

                case LuaASTType.ForGenericStatement:
                    return $"{indentation}for {RenderLuaASTNodes(luaAST.variables, ", ", indentationLevel)} in {RenderLuaASTNodes(luaAST.iterators, ", ", indentationLevel)} do\n{RenderLuaASTNodes(luaAST.body, "\n", indentationLevel + 1)}\n{indentation}end";

                case LuaASTType.ForNumericStatement:
                    return $"{indentation}for {RenderLuaASTNode(luaAST.variable, indentationLevel)} = {RenderLuaASTNode(luaAST.start, indentationLevel)},{RenderLuaASTNode(luaAST.end, indentationLevel)},{RenderLuaASTNode(luaAST.step, indentationLevel)} do\n{RenderLuaASTNodes(luaAST.body, "\n", indentationLevel + 1)}\n{indentation}end";

                case LuaASTType.FunctionDeclaration:
                    return $"{indentation}{(luaAST.isLocal ? "local " : "")}function {luaAST.identifier?.name ?? ""}({RenderLuaASTNodes(luaAST.parameters, ", ", indentationLevel)})\n{RenderLuaASTNodes(luaAST.body, "\n", indentationLevel + 1)}\n{indentation}end";

                case LuaASTType.GotoStatement:
                    return $"{indentation}goto {RenderLuaASTNode(luaAST.label, indentationLevel)}";

                case LuaASTType.Identifier:
                    return luaAST.name;

                case LuaASTType.IfClause:
                    return $"if {RenderLuaASTNode(luaAST.condition, indentationLevel)} then\n{RenderLuaASTNodes(luaAST.body, "\n", indentationLevel + 1)}";

                case LuaASTType.IfStatement:
                    return $"{indentation}{RenderLuaASTNodes(luaAST.clauses, "\n", indentationLevel)}\n{indentation}end";

                case LuaASTType.IndexExpression:
                    return $"{RenderLuaASTNode(luaAST.@base, indentationLevel)}[{RenderLuaASTNode(luaAST.index, indentationLevel)}]";

                case LuaASTType.LabelStatement:
                    return $"{indentation}::{RenderLuaASTNode(luaAST.label, indentationLevel)}::";

                case LuaASTType.LocalStatement:
                    var init = RenderLuaASTNodes(luaAST.init, ", ", indentationLevel);
                    if (!string.IsNullOrWhiteSpace(init))
                    {
                        init = " = " + init;
                    }
                    return $"{indentation}local {RenderLuaASTNodes(luaAST.variables, ", ", indentationLevel)}{init}";

                case LuaASTType.LogicalExpression:
                    return $"{RenderLuaASTNode(luaAST.left, indentationLevel)} {luaAST.@operator} {RenderLuaASTNode(luaAST.right, indentationLevel)}";

                case LuaASTType.MemberExpression:
                    return $"{RenderLuaASTNode(luaAST.@base, indentationLevel)}{luaAST.indexer}{RenderLuaASTNode(luaAST.identifier, indentationLevel)}";

                case LuaASTType.NilLiteral:
                    return luaAST.raw;

                case LuaASTType.NumericLiteral:
                    return luaAST.raw;

                case LuaASTType.RepeatStatement:
                    return $"{indentation}repeat\n{RenderLuaASTNodes(luaAST.body, "\n", indentationLevel + 1)}\n{indentation}until {RenderLuaASTNode(luaAST.condition, indentationLevel)}";

                case LuaASTType.ReturnStatement:
                    return $"{indentation}return {RenderLuaASTNodes(luaAST.arguments, ", ", indentationLevel)}";

                case LuaASTType.StringCallExpression:
                    return $"{RenderLuaASTNode(luaAST.@base, indentationLevel)}{RenderLuaASTNode(luaAST.argument, indentationLevel)}";

                case LuaASTType.StringLiteral:
                    return luaAST.raw;

                case LuaASTType.TableCallExpression:
                    return $"{RenderLuaASTNode(luaAST.@base, indentationLevel)}{RenderLuaASTNode(luaAST.argument, indentationLevel)}";

                case LuaASTType.TableConstructorExpression:
                    return $"{{ {RenderLuaASTNodes(luaAST.fields, ", ", indentationLevel)} }}";

                case LuaASTType.TableKey:
                    return $"[{RenderLuaASTNode(luaAST.key, indentationLevel)}] = {RenderLuaASTNode(luaAST.tableValue, indentationLevel)}";

                case LuaASTType.TableKeyString:
                    return $"{RenderLuaASTNode(luaAST.key, indentationLevel)} = {RenderLuaASTNode(luaAST.tableValue, indentationLevel)}";

                case LuaASTType.TableValue:
                    return RenderLuaASTNode(luaAST.tableValue, indentationLevel);

                case LuaASTType.UnaryExpression:
                    return $"{luaAST.@operator}({RenderLuaASTNode(luaAST.argument, indentationLevel)})";

                case LuaASTType.VarargLiteral:
                    return luaAST.raw;

                case LuaASTType.WhileStatement:
                    return $"{indentation}while {RenderLuaASTNode(luaAST.condition, indentationLevel)} do\n{RenderLuaASTNodes(luaAST.body, "\n", indentationLevel + 1)}\n{indentation}end";
            }

            throw new NotImplementedException();
        }
    }
}