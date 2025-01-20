using SpanJson;
using SpanJson.Formatters;
using SpanJson.Resolvers;
using System.Runtime.CompilerServices;
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
                node.SetParentNodeOfChildren();
            }

            result.AlignCommentsWithAST();
            return result;
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

    [JsonCustomSerializer(typeof(LuaASTNodeOrListLuaASTNodeCustomSerializer))]
    public class LuaASTNodeOrListLuaASTNode
    {
        public LuaASTNode Node { get; set; }
        public List<LuaASTNode> Nodes { get; set; }
    }

    [JsonCustomSerializer(typeof(LuaASTNodeOrStringCustomSerializer))]
    public class LuaASTNodeOrString
    {
        public LuaASTNode Node { get; set; }
        public string String { get; set; }
    }

    public class LuaASTNodeTextSpan
    {
        public LuaASTNodeTextPosition start { get; set; }
        public LuaASTNodeTextPosition end { get; set; }
    }

    public class LuaASTNodeTextPosition
    {
        public int line { get; set; }
        public int column { get; set; }
    }

    public class LuaASTNode
    {
        public LuaASTNodeTextSpan loc { get; set; }
        public int[] range { get; set; }

        public bool isLocal { get; set; }
        public LuaASTType type { get; set; }

        public LuaASTNode argument { get; set; }
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

        [DataMember(Name = "arguments")]
        public LuaASTNodeOrListLuaASTNode arguments_internal { get; set; }

        [DataMember(Name = "value")]
        public LuaASTNodeOrString value_internal { get; set; }

        public LuaASTNode tableValue
        {
            get
            {
                return value_internal?.Node;
            }
        }
        public string value
        {
            get
            {
                return value_internal.String;
            }
        }

        public LuaASTNode tableCallArgument
        {
            get
            {
                return arguments_internal?.Node;
            }
        }

        public List<LuaASTNode> arguments
        {
            get
            {
                return arguments_internal?.Nodes;
            }
        }

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

            if (value_internal?.Node == child)
            {
                value_internal.Node = replacement;
            }

            if (arguments_internal?.Node == child)
            {
                arguments_internal.Node = replacement;
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

            if (arguments_internal?.Nodes != null)
            {
                for (int i = 0; i < arguments_internal.Nodes.Count; i++)
                {
                    if (arguments_internal.Nodes[i] == child)
                    {
                        arguments_internal.Nodes[i] = replacement;
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

        public void SetParentNodeOfChildren(bool recursive = true)
        {
            var children = this.AllNodes.ToList();
            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child != null)
                    {
                        child.ParentNode = this;
                        if (recursive)
                        {
                            child.SetParentNodeOfChildren();
                        }
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

                if (tableCallArgument != null)
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

    public sealed class LuaASTNodeOrStringCustomSerializer : ICustomJsonFormatter<LuaASTNodeOrString>
    {
        public static readonly LuaASTNodeOrStringCustomSerializer Default = new LuaASTNodeOrStringCustomSerializer();
        public object Arguments { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaASTNodeOrString DeserializeInternal<TSymbol>(ref JsonReader<char> reader) where TSymbol : struct
        {
            var result = new LuaASTNodeOrString();

            var token = reader.ReadNextToken();
            if (token == JsonToken.BeginObject)
            {
                result.Node = ComplexClassFormatter<LuaASTNode, char, IncludeNullsOriginalCaseResolver<char>>.Default.Deserialize(ref reader);
                return result;
            }
            else
            {
                var value = reader.ReadDynamic();
                result.String = value?.ToString();
            }

            return result;
        }

        public LuaASTNodeOrString Deserialize(ref JsonReader<byte> reader)
        {
            throw new NotImplementedException();
        }

        public LuaASTNodeOrString Deserialize(ref JsonReader<char> reader)
        {
            return DeserializeInternal<char>(ref reader);
        }

        public void Serialize(ref JsonWriter<byte> writer, LuaASTNodeOrString value)
        {
            throw new NotImplementedException();
        }

        public void Serialize(ref JsonWriter<char> writer, LuaASTNodeOrString value)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class LuaASTNodeOrListLuaASTNodeCustomSerializer : ICustomJsonFormatter<LuaASTNodeOrListLuaASTNode>
    {
        public static readonly LuaASTNodeOrListLuaASTNodeCustomSerializer Default = new LuaASTNodeOrListLuaASTNodeCustomSerializer();
        public object Arguments { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaASTNodeOrListLuaASTNode DeserializeInternal<TSymbol>(ref JsonReader<char> reader) where TSymbol : struct
        {
            var result = new LuaASTNodeOrListLuaASTNode();

            var token = reader.ReadNextToken();
            if (token == JsonToken.BeginObject)
            {
                result.Node = ComplexClassFormatter<LuaASTNode, char, IncludeNullsOriginalCaseResolver<char>>.Default.Deserialize(ref reader);
                return result;
            }
            else if (token == JsonToken.BeginArray)
            {
                var test = reader.ReadDynamic();
                var serialized = JsonSerializer.Generic.Utf16.Serialize(test);
                result.Nodes = JsonSerializer.Generic.Utf16.Deserialize<List<LuaASTNode>>(serialized);

                //throws exception for some reason
                //result.Nodes = ComplexClassFormatter<List<LuaASTNode>, char, IncludeNullsOriginalCaseResolver<char>>.Default.Deserialize(ref reader)?.ToList();
            }

            return result;
        }

        public LuaASTNodeOrListLuaASTNode Deserialize(ref JsonReader<byte> reader)
        {
            throw new NotImplementedException();
        }

        public LuaASTNodeOrListLuaASTNode Deserialize(ref JsonReader<char> reader)
        {
            return DeserializeInternal<char>(ref reader);
        }

        public void Serialize(ref JsonWriter<byte> writer, LuaASTNodeOrListLuaASTNode value)
        {
            throw new NotImplementedException();
        }

        public void Serialize(ref JsonWriter<char> writer, LuaASTNodeOrListLuaASTNode value)
        {
            throw new NotImplementedException();
        }
    }
}