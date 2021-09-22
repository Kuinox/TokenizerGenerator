using CK.CodeGen;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kuinox.TokenizerGenerator
{
    public class Class1
    {
        readonly List<TokenDefinition> _tokens = new();


        public enum TokenType
        {
            Simple,
            Greedy
        }
        class TokenDefinition
        {
            public string Value { get; }
            public Node? SubTree { get; }
            public TokenType Type { get; }

            public TokenDefinition( string token )
            {
                if( string.IsNullOrEmpty( token ) ) throw new ArgumentNullException( nameof( token ), $"{nameof( token )} is null or empty." );
                Value = token;
                Type = TokenType.Simple;
            }

            public TokenDefinition( string token, Node subTree )
            {
                Value = token;
                SubTree = subTree;
                Type = TokenType.Greedy;
            }
        }

        class Node
        {
            public readonly char Value;
            readonly Dictionary<char, Node> _subNodes = new();
            public TokenDefinition? TokenDefinition { get; private set; }
            public int Depth { get; }

            public Node( int depth, char value )
            {
                Depth = depth;
                Value = value;
            }

            public IEnumerable<Node> SubNodes => _subNodes.Values;

            public IEnumerable<Node> AllSubNodes => _subNodes.Values.SelectMany( s => s.AllSubNodes ).Concat( _subNodes.Values );

            public void AddToken( TokenDefinition tokenDefinition, ReadOnlySpan<char> slice )
            {
                if( slice.Length == 1 )
                {
                    if( TokenDefinition != null )
                    {
                        throw new InvalidDataException( $"Conflicting token: {tokenDefinition} and {TokenDefinition}." );
                    }
                    TokenDefinition = tokenDefinition;
                    return;
                }
                char next = slice[0];
                if( _subNodes.TryGetValue( next, out Node? nextToken ) )
                {
                    nextToken.AddToken( tokenDefinition, slice.Slice( 1 ) );
                }
                else
                {
                    _subNodes.Add( next, new Node( Depth + 1, next ) );
                }
            }


            public void GenerateSwitchCase( StringBuilder sb, string charVariableName, string defaultDeclaration, Func<Node, string> caseGenerator )
            {
                sb.Append( $"switch({charVariableName}){{" );
                foreach( Node node in _subNodes.Values )
                {
                    sb.AppendLine( $"case '{node.Value}':" );
                }
                sb.Append( "default:" );
                sb.AppendLine( defaultDeclaration );
                sb.Append( "}" );
            }
        }
        public void MountTree()
        {
            Node root = new( 0, '\0' );
            foreach( TokenDefinition token in _tokens )
            {
                root.AddToken( token, token.Value.AsSpan() );
            }
        }

        public StringBuilder GenerateTokenizer( Node root, string languageName )
        {
            string className = $"{languageName}Tokenizer";

            StringBuilder sb = new();
            sb.Append( @$"public ref struct {className}
{{
    SequenceReader<char> _reader;
    public {className}(SequenceReader<char> reader)
    {{
        _reader = reader;
    }}

    public TokenData Current {{get; private set;}} = null;

    public OperationStatus Read()
    {{
        if(!_reader.TryRead(out char current)) return OperationStatus.NeedMoreData;
        
    }}
}}" );
            sb.Append( "Tokenizer { public '" );

            return sb;
        }
    }

    public readonly struct TokenData
    {
        public readonly ReadOnlySequence<char> Raw;
        public readonly bool IsValid;
        public readonly bool CanBePartOfBiggerToken;
        public readonly bool IsEmpty;
        public TokenData( ReadOnlySequence<char> raw, bool isValid, bool canBePartOfBiggerToken, bool isEmpty )
        {
            Raw = raw;
            IsValid = isValid;
            CanBePartOfBiggerToken = canBePartOfBiggerToken;
            IsEmpty = isEmpty;
        }
    }
}
