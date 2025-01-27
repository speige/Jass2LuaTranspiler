using System.Text;

namespace Jass2Lua.Ast
{
    public class LuaRenderer
    {
        protected List<LuaASTNode> _comments;
        protected int _commentIndex = 0;

        protected readonly StringBuilder _buffer = new StringBuilder();
        protected readonly string _indent;
        protected readonly string _lineBreak;

        protected LuaRenderer(string indent, string lineBreak)
        {
            _indent = indent;
            _lineBreak = lineBreak;
        }

        public static string Render(LuaAST ast, string indent = "\t", string lineBreak = "\n")
        {
            ast.comments?.Sort((a, b) => a.range[0].CompareTo(b.range[0]));

            var renderer = new LuaRenderer(indent, lineBreak);
            renderer._comments = ast.comments;
            renderer.RenderNodes(ast.body, string.Empty, 0);
            renderer.ConsumeCommentsUpTo(int.MaxValue, 0);

            return renderer._buffer.ToString();
        }

        public static string Render(LuaASTNode node)
        {
            return Render(new LuaAST() { body = new List<LuaASTNode>() { node } });
        }

        protected void RenderNodes(List<LuaASTNode> nodes, string separator, int indentationLevel)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return;
            }

            var separatorAdded = false;
            foreach (var field in nodes)
            {
                var oldLength = _buffer.Length;
                RenderNode(field, indentationLevel);
                if (separator.Length > 0 && _buffer.Length != oldLength)
                {
                    _buffer.Append(separator);
                    separatorAdded = true;
                }
            }

            if (separatorAdded && separator.Length > 0)
            {
                _buffer.Remove(_buffer.Length - separator.Length, separator.Length);
            }
        }

        protected void ConsumeCommentsUpTo(int position, int indentationLevel)
        {
            while (_commentIndex < _comments.Count)
            {
                var comment = _comments[_commentIndex];
                if (comment.range[0] > position)
                {
                    break;
                }

                RenderComment(comment, indentationLevel);
                _commentIndex++;
            }
        }

        protected void RenderComment(LuaASTNode commentNode, int indentationLevel)
        {
            var comment = commentNode.raw;
            var inline = comment.StartsWith("--[[");

            var startOfLine = commentNode.loc.start.column == 0;
            if (_buffer.Length > 0 && startOfLine && !BufferAtNewline)
            {
                _buffer.Append(_lineBreak);
                _buffer.Append(GetIndentation(indentationLevel));
            }
            else if (BufferAtNewline)
            {
                _buffer.Append(GetIndentation(indentationLevel));
            }

            _buffer.Append(comment);

            if (!inline)
            {
                _buffer.Append(_lineBreak);
            }
        }

        protected StringBuilder GetIndentation(int indentationLevel)
        {
            var result = new StringBuilder();
            for (var i = 0; i < indentationLevel; i++)
            {
                result.Append(_indent);
            }
            return result;
        }

        protected bool BufferAtNewline
        {
            get
            {
                return _buffer.Length >= _lineBreak.Length && _buffer.ToString(_buffer.Length - _lineBreak.Length, _lineBreak.Length) == _lineBreak;
            }
        }

        protected void RenderNode(LuaASTNode node, int indentationLevel)
        {
            ConsumeCommentsUpTo(node.range[0], indentationLevel);

            var indentation = GetIndentation(indentationLevel);

            switch (node.type)
            {
                case LuaASTType.AssignmentStatement:
                    _buffer.Append(indentation);
                    RenderNodes(node.variables, ", ", indentationLevel);
                    _buffer.Append(" = ");
                    RenderNodes(node.init, ", ", indentationLevel);
                    break;

                case LuaASTType.BinaryExpression:
                case LuaASTType.LogicalExpression:
                    _buffer.Append("(");
                    RenderNode(node.left, indentationLevel);
                    _buffer.Append(" ");
                    _buffer.Append(node.@operator);
                    _buffer.Append(" ");
                    RenderNode(node.right, indentationLevel);
                    _buffer.Append(")");
                    break;

                case LuaASTType.BooleanLiteral:
                case LuaASTType.NilLiteral:
                case LuaASTType.NumericLiteral:
                case LuaASTType.StringLiteral:
                case LuaASTType.VarargLiteral:
                    _buffer.Append(node.raw);
                    break;

                case LuaASTType.BreakStatement:
                    _buffer.Append(indentation);
                    _buffer.Append("break");
                    break;

                case LuaASTType.CallExpression:
                    RenderNode(node.@base, indentationLevel);
                    _buffer.Append("(");
                    RenderNodes(node.arguments, ", ", indentationLevel);
                    _buffer.Append(")");
                    break;

                case LuaASTType.CallStatement:
                    _buffer.Append(indentation);
                    RenderNode(node.expression, indentationLevel);
                    break;

                case LuaASTType.DoStatement:
                    _buffer.Append(indentation);
                    _buffer.Append("do");
                    _buffer.Append(_lineBreak);
                    RenderNodes(node.body, string.Empty, indentationLevel + 1);
                    _buffer.Append(indentation);
                    _buffer.Append("end");
                    break;

                case LuaASTType.ElseClause:
                    _buffer.Append(indentation);
                    _buffer.Append("else");
                    _buffer.Append(_lineBreak);
                    RenderNodes(node.body, string.Empty, indentationLevel + 1);
                    break;

                case LuaASTType.ElseifClause:
                    _buffer.Append(indentation);
                    _buffer.Append("elseif ");
                    RenderNode(node.condition, indentationLevel);
                    _buffer.Append(" then");
                    _buffer.Append(_lineBreak);
                    RenderNodes(node.body, string.Empty, indentationLevel + 1);
                    break;

                case LuaASTType.ForGenericStatement:
                    _buffer.Append(indentation);
                    _buffer.Append("for ");
                    RenderNodes(node.variables, ", ", indentationLevel);
                    _buffer.Append(" in ");
                    RenderNodes(node.iterators, ", ", indentationLevel);
                    _buffer.Append(" do");
                    _buffer.Append(_lineBreak);
                    RenderNodes(node.body, string.Empty, indentationLevel + 1);
                    _buffer.Append(indentation);
                    _buffer.Append("end");
                    break;

                case LuaASTType.ForNumericStatement:
                    _buffer.Append(indentation);
                    _buffer.Append("for ");
                    RenderNode(node.variable, indentationLevel);
                    _buffer.Append(" = ");
                    RenderNode(node.start, indentationLevel);
                    _buffer.Append(",");
                    RenderNode(node.end, indentationLevel);
                    _buffer.Append(",");
                    RenderNode(node.step, indentationLevel);
                    _buffer.Append(" do");
                    _buffer.Append(_lineBreak);
                    RenderNodes(node.body, string.Empty, indentationLevel + 1);
                    _buffer.Append(indentation);
                    _buffer.Append("end");
                    break;

                case LuaASTType.FunctionDeclaration:
                    _buffer.Append(indentation);
                    _buffer.Append(node.isLocal ? "local " : "");
                    _buffer.Append("function ");
                    RenderNode(node.identifier, indentationLevel);
                    _buffer.Append("(");
                    RenderNodes(node.parameters, ", ", indentationLevel);
                    _buffer.Append(")");
                    _buffer.Append(_lineBreak);
                    RenderNodes(node.body, string.Empty, indentationLevel + 1);
                    _buffer.Append(indentation);
                    _buffer.Append("end");
                    _buffer.Append(_lineBreak);
                    break;

                case LuaASTType.GotoStatement:
                    _buffer.Append(indentation);
                    _buffer.Append("goto ");
                    RenderNode(node.label, indentationLevel);
                    break;

                case LuaASTType.Identifier:
                    _buffer.Append(node.name);
                    break;

                case LuaASTType.IfClause:
                    _buffer.Append("if ");
                    RenderNode(node.condition, indentationLevel);
                    _buffer.Append(" then");
                    _buffer.Append(_lineBreak);
                    RenderNodes(node.body, string.Empty, indentationLevel + 1);
                    break;

                case LuaASTType.IfStatement:
                    _buffer.Append(indentation);
                    RenderNodes(node.clauses, string.Empty, indentationLevel);
                    _buffer.Append(_lineBreak);
                    _buffer.Append(indentation);
                    _buffer.Append("end");
                    break;

                case LuaASTType.IndexExpression:
                    RenderNode(node.@base, indentationLevel);
                    _buffer.Append("[");
                    RenderNode(node.index, indentationLevel);
                    _buffer.Append("]");
                    break;

                case LuaASTType.LabelStatement:
                    _buffer.Append(indentation);
                    _buffer.Append("::");
                    RenderNode(node.label, indentationLevel);
                    _buffer.Append("::");
                    break;

                case LuaASTType.LocalStatement:
                    _buffer.Append(indentation);
                    _buffer.Append("local ");
                    RenderNodes(node.variables, ", ", indentationLevel);
                    var oldLength = _buffer.Length;
                    RenderNodes(node.init, ", ", indentationLevel);
                    if (_buffer.Length != oldLength)
                    {
                        _buffer.Insert(oldLength, " = ");
                    }
                    break;

                case LuaASTType.MemberExpression:
                    RenderNode(node.@base, indentationLevel);
                    _buffer.Append(node.indexer);
                    RenderNode(node.identifier, indentationLevel);
                    break;

                case LuaASTType.RepeatStatement:
                    _buffer.Append(indentation);
                    _buffer.Append("repeat");
                    _buffer.Append(_lineBreak);
                    RenderNodes(node.body, string.Empty, indentationLevel + 1);
                    _buffer.Append(indentation);
                    _buffer.Append("until ");
                    RenderNode(node.condition, indentationLevel);
                    break;

                case LuaASTType.ReturnStatement:
                    _buffer.Append(indentation);
                    _buffer.Append("return ");
                    RenderNodes(node.arguments, ", ", indentationLevel);
                    break;

                case LuaASTType.StringCallExpression:
                    RenderNode(node.@base, indentationLevel);
                    RenderNode(node.argument, indentationLevel);
                    break;

                case LuaASTType.TableCallExpression:
                    RenderNode(node.@base, indentationLevel);
                    RenderNode(node.tableCallArgument, indentationLevel);
                    break;

                case LuaASTType.TableConstructorExpression:
                    _buffer.Append("{ ");
                    RenderNodes(node.fields, ", ", indentationLevel);
                    _buffer.Append(" }");
                    break;

                case LuaASTType.TableKey:
                    _buffer.Append("[");
                    RenderNode(node.key, indentationLevel);
                    _buffer.Append("] = ");
                    RenderNode(node.tableValue, indentationLevel);
                    break;

                case LuaASTType.TableKeyString:
                    RenderNode(node.key, indentationLevel);
                    _buffer.Append(" = ");
                    RenderNode(node.tableValue, indentationLevel);
                    break;

                case LuaASTType.TableValue:
                    RenderNode(node.tableValue, indentationLevel);
                    break;

                case LuaASTType.UnaryExpression:
                    _buffer.Append(node.@operator);
                    _buffer.Append("(");
                    RenderNode(node.argument, indentationLevel);
                    _buffer.Append(")");
                    break;

                case LuaASTType.WhileStatement:
                    _buffer.Append(indentation);
                    _buffer.Append("while ");
                    RenderNode(node.condition, indentationLevel);
                    _buffer.Append(" do");
                    _buffer.Append(_lineBreak);
                    RenderNodes(node.body, string.Empty, indentationLevel + 1);
                    _buffer.Append(indentation);
                    _buffer.Append("end");
                    break;

                default:
                    _buffer.Append($"--[[ Unhandled node type: {node.type} ]]");
                    break;
            }

            ConsumeCommentsUpTo(node.range[1]+1, indentationLevel);

            var isStatement = Enum.GetName(node.type).EndsWith("Statement");
            if (isStatement && !BufferAtNewline)
            {
                _buffer.Append(_lineBreak);
            }
        }
    }
}
