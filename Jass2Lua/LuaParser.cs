using Microsoft.ClearScript.V8;
using Newtonsoft.Json;
using System.Text;

namespace Jass2Lua
{
    public class LuaParser
    {
        public class IndexedNode
        {
            public LuaASTNode Node { get; set; }
            public int StartIndex { get; set; }
        }

        public static LuaAST ParseScript(string luaScript)
        {
            using (var v8 = new V8ScriptEngine())
            {
                v8.Execute(EmbeddedResources.luaparse_js);

                v8.Script.luaScript = luaScript;
                v8.Execute("ast = JSON.stringify(luaparse.parse(luaScript, { luaVersion: '5.3', ranges: 'true' }));");
                //NOTE: Can't upgrade NewtonSoft.Json above 12.0.3 due to bug which ignores MaxDepth, also can't upgrade ClearScript.V8 above 7.2.5 because it references a newer version of NewtonSoft.Json
                var result = JsonConvert.DeserializeObject<LuaAST>((string)v8.Script.ast, new JsonSerializerSettings() { MissingMemberHandling = MissingMemberHandling.Ignore, MaxDepth = Int32.MaxValue });
                AlignCommentsWithAST(result);
                return result;
            }
        }

        private static void AlignCommentsWithAST(LuaAST ast)
        {
            var indexedNodes = new List<IndexedNode>();
            IndexNodes(ast.Body, indexedNodes);

            indexedNodes.Sort((a, b) => a.StartIndex.CompareTo(b.StartIndex));

            var sortedComments = ast.Comments.OrderBy(c => c.Range[0]).ToList();

            foreach (var comment in sortedComments)
            {
                int commentStart = comment.Range[0];
                string commentText = comment.Value;

                int nodeIndex = indexedNodes.BinarySearch(
                    new IndexedNode { StartIndex = commentStart },
                    Comparer<IndexedNode>.Create((a, b) => a.StartIndex.CompareTo(b.StartIndex))
                );

                if (nodeIndex < 0)
                {
                    //NOTE: Negative result from BinarySearch means not found and is also the bitwise negated index of where it should be inserted to keep proper sort order
                    nodeIndex = ~nodeIndex;
                }

                LuaASTNode targetNode = nodeIndex < indexedNodes.Count ? indexedNodes[nodeIndex].Node : null;

                if (targetNode != null)
                {
                    InjectCommentBeforeNode(ast, targetNode, commentText);
                }
                else
                {
                    InjectCommentAtEnd(ast, commentText);
                }
            }
        }

        private static void IndexNodes(LuaASTNode[] nodes, List<IndexedNode> indexedNodes)
        {
            if (nodes == null)
            {
                return;
            }

            foreach (var node in nodes)
            {
                if (node.Range != null && node.Range.Length > 0)
                {
                    indexedNodes.Add(new IndexedNode { Node = node, StartIndex = node.Range[0] });
                }

                IndexNodes(node.Body, indexedNodes);
            }
        }

        private static void InjectCommentBeforeNode(LuaAST ast, LuaASTNode node, string comment)
        {
            var commentNode = new LuaASTNode
            {
                Type = LuaASTType.Comment,
                Raw = comment,
            };

            var parent = node.ParentNode;
            if (parent != null && parent.Body != null)
            {
                var parentIndex = Array.IndexOf(parent.Body, node);
                if (parentIndex >= 0)
                {
                    var newBody = new List<LuaASTNode>(parent.Body);
                    newBody.Insert(parentIndex, commentNode);
                    parent.Body = newBody.ToArray();
                    return;
                }
            }

            var bodyIndex = Array.IndexOf(ast.Body, node);
            if (bodyIndex == -1)
            {
                bodyIndex = 0;
            }

            if (bodyIndex >= 0)
            {
                var newBody = new List<LuaASTNode>(ast.Body);
                newBody.Insert(bodyIndex, commentNode);
                ast.Body = newBody.ToArray();
            }
        }

        private static void InjectCommentAtEnd(LuaAST ast, string comment)
        {
            var commentNode = new LuaASTNode
            {
                Type = LuaASTType.Comment,
                Raw = comment,
            };

            var newBody = new List<LuaASTNode>(ast.Body) { commentNode };
            ast.Body = newBody.ToArray();
        }

        public static void TransformTree(LuaAST ast, Func<LuaASTNode, LuaASTNode> action)
        {
            ast.Body = (ast.Body ?? new LuaASTNode[0]).Select(x => TransformNode_Recursive(x, action)).ToArray();
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

            foreach (var child in node.AllNodes)
            {
                TransformNode_Recursive(child, action);
            }
            return node;
        }

        public static string RenderLuaAST(LuaAST luaAST)
        {
            return RenderLuaASTNodes(luaAST.Body, "\n", 0).ToString();
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
            switch (luaAST.Type)
            {
                case LuaASTType.AssignmentStatement:
                    return $"{indentation}{RenderLuaASTNodes(luaAST.Variables, ", ", indentationLevel)} = {RenderLuaASTNodes(luaAST.Init, ", ", indentationLevel)}";

                case LuaASTType.BinaryExpression:
                    return $"({RenderLuaASTNode(luaAST.Left, indentationLevel)} {luaAST.Operator} {RenderLuaASTNode(luaAST.Right, indentationLevel)})";

                case LuaASTType.BooleanLiteral:
                    return luaAST.Raw;

                case LuaASTType.BreakStatement:
                    return $"{indentation}break";

                case LuaASTType.CallExpression:
                    return $"{RenderLuaASTNode(luaAST.Base, indentationLevel)}({RenderLuaASTNodes(luaAST.Arguments, ", ", indentationLevel)})";

                case LuaASTType.CallStatement:
                    return $"{indentation}{RenderLuaASTNode(luaAST.Expression, indentationLevel)}";

                case LuaASTType.Comment:
                    return $"{indentation}-- {luaAST.Raw}";

                case LuaASTType.DoStatement:
                    return $"{indentation}do\n{RenderLuaASTNodes(luaAST.Body, "\n", indentationLevel + 1)}\n{indentation}end";

                case LuaASTType.ElseClause:
                    return $"{indentation}else\n{RenderLuaASTNodes(luaAST.Body, "\n", indentationLevel + 1)}";

                case LuaASTType.ElseifClause:
                    return $"{indentation}elseif {RenderLuaASTNode(luaAST.Condition, indentationLevel)} then\n{RenderLuaASTNodes(luaAST.Body, "\n", indentationLevel + 1)}";

                case LuaASTType.ForGenericStatement:
                    return $"{indentation}for {RenderLuaASTNodes(luaAST.Variables, ", ", indentationLevel)} in {RenderLuaASTNodes(luaAST.Iterators, ", ", indentationLevel)} do\n{RenderLuaASTNodes(luaAST.Body, "\n", indentationLevel + 1)}\n{indentation}end";

                case LuaASTType.ForNumericStatement:
                    return $"{indentation}for {RenderLuaASTNode(luaAST.Variable, indentationLevel)} = {RenderLuaASTNode(luaAST.Start, indentationLevel)},{RenderLuaASTNode(luaAST.End, indentationLevel)},{RenderLuaASTNode(luaAST.Step, indentationLevel)} do\n{RenderLuaASTNodes(luaAST.Body, "\n", indentationLevel + 1)}\n{indentation}end";

                case LuaASTType.FunctionDeclaration:
                    return $"{indentation}{(luaAST.IsLocal ? "local " : "")}function {luaAST.Identifier?.Name ?? ""}({RenderLuaASTNodes(luaAST.Parameters, ", ", indentationLevel)})\n{RenderLuaASTNodes(luaAST.Body, "\n", indentationLevel + 1)}\n{indentation}end";

                case LuaASTType.GotoStatement:
                    return $"{indentation}goto {RenderLuaASTNode(luaAST.Label, indentationLevel)}";

                case LuaASTType.Identifier:
                    return luaAST.Name;

                case LuaASTType.IfClause:
                    return $"if {RenderLuaASTNode(luaAST.Condition, indentationLevel)} then\n{RenderLuaASTNodes(luaAST.Body, "\n", indentationLevel + 1)}";

                case LuaASTType.IfStatement:
                    return $"{indentation}{RenderLuaASTNodes(luaAST.Clauses, "\n", indentationLevel)}\n{indentation}end";

                case LuaASTType.IndexExpression:
                    return $"{RenderLuaASTNode(luaAST.Base, indentationLevel)}[{RenderLuaASTNode(luaAST.Index, indentationLevel)}]";

                case LuaASTType.LabelStatement:
                    return $"{indentation}::{RenderLuaASTNode(luaAST.Label, indentationLevel)}::";

                case LuaASTType.LocalStatement:
                    var init = RenderLuaASTNodes(luaAST.Init, ", ", indentationLevel);
                    if (!string.IsNullOrWhiteSpace(init))
                    {
                        init = " = " + init;
                    }
                    return $"{indentation}local {RenderLuaASTNodes(luaAST.Variables, ", ", indentationLevel)}{init}";

                case LuaASTType.LogicalExpression:
                    return $"{RenderLuaASTNode(luaAST.Left, indentationLevel)} {luaAST.Operator} {RenderLuaASTNode(luaAST.Right, indentationLevel)}";

                case LuaASTType.MemberExpression:
                    return $"{RenderLuaASTNode(luaAST.Base, indentationLevel)}{luaAST.Indexer}{RenderLuaASTNode(luaAST.Identifier, indentationLevel)}";

                case LuaASTType.NilLiteral:
                    return luaAST.Raw;

                case LuaASTType.NumericLiteral:
                    return luaAST.Raw;

                case LuaASTType.RepeatStatement:
                    return $"{indentation}repeat\n{RenderLuaASTNodes(luaAST.Body, "\n", indentationLevel + 1)}\n{indentation}until {RenderLuaASTNode(luaAST.Condition, indentationLevel)}";

                case LuaASTType.ReturnStatement:
                    return $"{indentation}return {RenderLuaASTNodes(luaAST.Arguments, ", ", indentationLevel)}";

                case LuaASTType.StringCallExpression:
                    return $"{RenderLuaASTNode(luaAST.Base, indentationLevel)}{RenderLuaASTNode(luaAST.Argument, indentationLevel)}";

                case LuaASTType.StringLiteral:
                    return luaAST.Raw;

                case LuaASTType.TableCallExpression:
                    return $"{RenderLuaASTNode(luaAST.Base, indentationLevel)}{RenderLuaASTNode(luaAST.Argument, indentationLevel)}";

                case LuaASTType.TableConstructorExpression:
                    return $"{{ {RenderLuaASTNodes(luaAST.Fields, ", ", indentationLevel)} }}";

                case LuaASTType.TableKey:
                    return $"[{RenderLuaASTNode(luaAST.Key, indentationLevel)}] = {RenderLuaASTNode(luaAST.TableValue, indentationLevel)}";

                case LuaASTType.TableKeyString:
                    return $"{RenderLuaASTNode(luaAST.Key, indentationLevel)} = {RenderLuaASTNode(luaAST.TableValue, indentationLevel)}";

                case LuaASTType.TableValue:
                    return RenderLuaASTNode(luaAST.TableValue, indentationLevel);

                case LuaASTType.UnaryExpression:
                    return $"{luaAST.Operator}({RenderLuaASTNode(luaAST.Argument, indentationLevel)})";

                case LuaASTType.VarargLiteral:
                    return luaAST.Raw;

                case LuaASTType.WhileStatement:
                    return $"{indentation}while {RenderLuaASTNode(luaAST.Condition, indentationLevel)} do\n{RenderLuaASTNodes(luaAST.Body, "\n", indentationLevel + 1)}\n{indentation}end";
            }

            throw new NotImplementedException();
        }
    }
}