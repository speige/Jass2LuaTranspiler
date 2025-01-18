using SpanJson;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Jass2Lua
{
    public class LuaAST
    {
        public string type { get; set; }
        public List<LuaASTNode> body { get; set; }
        public List<LuaASTNode> comments { get; set; }

        public class IndexedNode
        {
            public LuaASTNode Node { get; set; }
            public int StartIndex { get; set; }
        }

        public static LuaAST FromJson(string json)
        {
            var result = JsonSerializer.Generic.Utf16.Deserialize<LuaAST>(json);
            var allNodes = result.body.Concat(result.comments);
            foreach (var node in allNodes)
            {
                OnDeserialized(node);
            }

            result.AlignCommentsWithAST();
            return result;
        }

        private static void OnDeserialized(LuaASTNode node)
        {
            node.argument = JsonSerializer.Generic.Utf16.Deserialize<LuaASTNode>(JsonSerializer.Generic.Utf16.Serialize(node.argument_internal));

            if (node.type == LuaASTType.TableCallExpression)
            {
                if (node.arguments_internal != null)
                {
                    node.argument = JsonSerializer.Generic.Utf16.Deserialize<LuaASTNode>(JsonSerializer.Generic.Utf16.Serialize(node.arguments_internal));
                }
            }
            else
            {
                if (node.arguments_internal != null)
                {
                    node.arguments = JsonSerializer.Generic.Utf16.Deserialize<List<LuaASTNode>>(JsonSerializer.Generic.Utf16.Serialize(node.arguments_internal));
                }
            }

            if (node.type == LuaASTType.TableValue || node.type == LuaASTType.TableKey || node.type == LuaASTType.TableKeyString)
            {
                if (node.value_internal != null)
                {
                    node.tableValue = JsonSerializer.Generic.Utf16.Deserialize<LuaASTNode>(JsonSerializer.Generic.Utf16.Serialize(node.value_internal));
                }
            }
            else
            {
                node.value = (string)node.value_internal?.ToString();
            }

            var children = node.AllNodes.ToList();
            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child != null)
                    {
                        child.ParentNode = node;
                        OnDeserialized(child);
                    }
                }
            }
        }

        private static void IndexNodes(List<LuaASTNode> nodes, List<IndexedNode> indexedNodes)
        {
            if (nodes == null)
            {
                return;
            }

            foreach (var node in nodes)
            {
                if (node.range != null && node.range.Length > 0)
                {
                    indexedNodes.Add(new IndexedNode { Node = node, StartIndex = node.range[0] });
                }

                IndexNodes(node.body, indexedNodes);
            }
        }

        protected void AlignCommentsWithAST()
        {
            if (comments == null)
            {
                return;
            }

            var indexedNodes = new List<IndexedNode>();
            IndexNodes(body, indexedNodes);

            indexedNodes.Sort((a, b) => a.StartIndex.CompareTo(b.StartIndex));

            var sortedComments = comments.OrderBy(c => c.range[0]).ToList();

            foreach (var comment in sortedComments)
            {
                int commentStart = comment.range[0];

                int nodeIndex = indexedNodes.BinarySearch(new IndexedNode { StartIndex = commentStart }, Comparer<IndexedNode>.Create((a, b) => a.StartIndex.CompareTo(b.StartIndex)));

                if (nodeIndex < 0)
                {
                    //NOTE: Negative result from BinarySearch means not found and is also the bitwise negated index of where it should be inserted to keep proper sort order
                    nodeIndex = ~nodeIndex;
                }

                LuaASTNode targetNode = nodeIndex < indexedNodes.Count ? indexedNodes[nodeIndex].Node : null;

                if (targetNode != null)
                {
                    InjectCommentBeforeNode(targetNode, comment);
                }
                else
                {
                    InjectCommentAtEnd(comment);
                }
            }
        }

        protected void InjectCommentBeforeNode(LuaASTNode node, LuaASTNode comment)
        {
            var parent = node.ParentNode;
            if (parent != null && parent.body != null)
            {
                var parentIndex = parent.body.IndexOf(node);
                if (parentIndex >= 0)
                {
                    parent.body.Insert(parentIndex, comment);
                    return;
                }
            }

            var bodyIndex = body.IndexOf(node);
            if (bodyIndex == -1)
            {
                bodyIndex = 0;
            }

            if (bodyIndex >= 0)
            {
                body.Insert(bodyIndex, comment);
            }
        }

        protected void InjectCommentAtEnd(LuaASTNode node)
        {
            body.Add(node);
        }
    }

    public class LuaASTNode
    {
        public int[] range { get; set; }

        public bool isLocal { get; set; }
        public LuaASTType type { get; set; }

        public LuaASTNode @base { get; set; }
        public List<LuaASTNode> body { get; set; }
        public LuaASTNode condition { get; set; }
        public LuaASTNode end { get; set; }
        public LuaASTNode expression { get; set; }
        public LuaASTNode identifier { get; set; }
        public LuaASTNode index { get; set; }
        public LuaASTNode key { get; set; }
        public LuaASTNode left { get; set; }
        public LuaASTNode label { get; set; }
        public LuaASTNode start { get; set; }
        public LuaASTNode step { get; set; }
        public LuaASTNode right { get; set; }
        public LuaASTNode variable { get; set; }

        public List<LuaASTNode> clauses { get; set; }
        public List<LuaASTNode> fields { get; set; }
        public List<LuaASTNode> init { get; set; }
        public List<LuaASTNode> iterators { get; set; }
        public List<LuaASTNode> parameters { get; set; }
        public List<LuaASTNode> variables { get; set; }

        public string indexer { get; set; }
        public string name { get; set; }
        public string @operator { get; set; }
        public string raw { get; set; }

        //ambiguous columns, used differently in JSON depending on parent Type
        [DataMember(Name = "argument")]
        public object argument_internal { get; set; }
        [DataMember(Name = "arguments")]
        public object arguments_internal { get; set; }
        [DataMember(Name = "value")]
        public object value_internal { get; set; }

        [IgnoreDataMember]
        public LuaASTNode argument { get; set; }
        [IgnoreDataMember]
        public List<LuaASTNode> arguments { get; set; }
        [IgnoreDataMember]
        public string value { get; set; }
        [IgnoreDataMember]
        public LuaASTNode tableValue { get; set; }

        public LuaASTNode ParentNode { get; set; }

        public void ReplaceChild(LuaASTNode child, LuaASTNode replacement)
        {
            //note: could write much shorter code since this is a repeating pattern, but performance will be better this way. Important due to frequent use by recursion & filtering algorithms.
            if (argument == child)
            {
                argument = replacement;
            }
            
            if (@base == child)
            {
                @base = replacement;
            }

            if (condition == child)
            {
                condition = replacement;
            }

            if (end == child)
            {
                end = replacement;
            }

            if (expression == child)
            {
                expression = replacement;
            }

            if (identifier == child)
            {
                identifier = replacement;
            }

            if (index == child)
            {
                index = replacement;
            }

            if (key == child)
            {
                key = replacement;
            }

            if (left == child)
            {
                left = replacement;
            }

            if (label == child)
            {
                label = replacement;
            }

            if (start == child)
            {
                start = replacement;
            }

            if (step == child)
            {
                step = replacement;
            }

            if (right == child)
            {
                right = replacement;
            }

            if (variable == child)
            {
                variable = replacement;
            }

            if (tableValue == child)
            {
                tableValue = replacement;
            }

            if (body != null)
            {
                for (int i = 0; i < body.Count; i++)
                {
                    if (body[i] == child)
                    {
                        body[i] = replacement;
                    }
                }
            }

            if (arguments != null)
            {
                for (int i = 0; i < arguments.Count; i++)
                {
                    if (arguments[i] == child)
                    {
                        arguments[i] = replacement;
                    }
                }
            }

            if (clauses != null)
            {
                for (int i = 0; i < clauses.Count; i++)
                {
                    if (clauses[i] == child)
                    {
                        clauses[i] = replacement;
                    }
                }
            }

            if (fields != null)
            {
                for (int i = 0; i < fields.Count; i++)
                {
                    if (fields[i] == child)
                    {
                        fields[i] = replacement;
                    }
                }
            }

            if (init != null)
            {
                for (int i = 0; i < init.Count; i++)
                {
                    if (init[i] == child)
                    {
                        init[i] = replacement;
                    }
                }
            }

            if (iterators != null)
            {
                for (int i = 0; i < iterators.Count; i++)
                {
                    if (iterators[i] == child)
                    {
                        iterators[i] = replacement;
                    }
                }
            }

            if (parameters != null)
            {
                for (int i = 0; i < parameters.Count; i++)
                {
                    if (parameters[i] == child)
                    {
                        parameters[i] = replacement;
                    }
                }
            }

            if (variables != null)
            {
                for (int i = 0; i < variables.Count; i++)
                {
                    if (variables[i] == child)
                    {
                        variables[i] = replacement;
                    }
                }
            }
        }

        public IEnumerable<LuaASTNode> AllNodes
        {
            get
            {
                //note: could write much shorter code since this is a repeating pattern, but performance will be better this way. Important due to frequent use by recursion & filtering algorithms.
                if (argument != null)
                {
                    yield return argument;
                }

                if (@base != null)
                {
                    yield return @base;
                }

                if (condition != null)
                {
                    yield return condition;
                }

                if (end != null)
                {
                    yield return end;
                }

                if (expression != null)
                {
                    yield return expression;
                }

                if (identifier != null)
                {
                    yield return identifier;
                }

                if (index != null)
                {
                    yield return index;
                }

                if (key != null)
                {
                    yield return key;
                }

                if (left != null)
                {
                    yield return left;
                }

                if (label != null)
                {
                    yield return label;
                }

                if (start != null)
                {
                    yield return start;
                }

                if (step != null)
                {
                    yield return step;
                }

                if (right != null)
                {
                    yield return right;
                }

                if (variable != null)
                {
                    yield return variable;
                }

                if (tableValue != null)
                {
                    yield return tableValue;
                }

                if (body != null)
                {
                    foreach (var child in body)
                    {
                        yield return child;
                    }
                }

                if (arguments != null)
                {
                    foreach (var child in arguments)
                    {
                        yield return child;
                    }
                }

                if (clauses != null)
                {
                    foreach (var child in clauses)
                    {
                        yield return child;
                    }
                }

                if (fields != null)
                {
                    foreach (var child in fields)
                    {
                        yield return child;
                    }
                }

                if (init != null)
                {
                    foreach (var child in init)
                    {
                        yield return child;
                    }
                }

                if (iterators != null)
                {
                    foreach (var child in iterators)
                    {
                        yield return child;
                    }
                }

                if (parameters != null)
                {
                    foreach (var child in parameters)
                    {
                        yield return child;
                    }
                }

                if (variables != null)
                {
                    foreach (var child in variables)
                    {
                        yield return child;
                    }
                }
            }
        }
    }

    public enum LuaASTType { AssignmentStatement, BinaryExpression, BooleanLiteral, BreakStatement, CallExpression, CallStatement, Comment, DoStatement, ElseClause, ElseifClause, ForGenericStatement, ForNumericStatement, FunctionDeclaration, GotoStatement, Identifier, IfClause, IfStatement, IndexExpression, LabelStatement, LocalStatement, LogicalExpression, MemberExpression, NilLiteral, NumericLiteral, RepeatStatement, ReturnStatement, StringCallExpression, StringLiteral, TableCallExpression, TableConstructorExpression, TableKey, TableKeyString, TableValue, UnaryExpression, VarargLiteral, WhileStatement };
}