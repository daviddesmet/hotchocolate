using System;
using System.Collections.Generic;
using System.Globalization;
using static HotChocolate.Language.Properties.LangUtf8Resources;

namespace HotChocolate.Language;

// Implements the parsing rules in the Operations section.
public ref partial struct Utf8GraphQLParser
{
    private static readonly List<VariableDefinitionNode> _emptyVariableDefinitions = new();
    private static readonly List<ArgumentNode> _emptyArguments = new();

    /// <summary>
    /// Parses an operation definition.
    /// <see cref="OperationDefinitionNode" />:
    /// OperationType? OperationName? ($x : Type = DefaultValue?)? SelectionSet
    /// </summary>
    private OperationDefinitionNode ParseOperationDefinition()
    {
        TokenInfo start = Start();

        OperationType operation = ParseOperationType();
        NameNode? name = _reader.Kind == TokenKind.Name ? ParseName() : null;
        List<VariableDefinitionNode> variableDefinitions = ParseVariableDefinitions();
        List<DirectiveNode> directives = ParseDirectives(false);
        SelectionSetNode selectionSet = ParseSelectionSet();
        Location? location = CreateLocation(in start);

        return new OperationDefinitionNode
        (
            location,
            name,
            operation,
            variableDefinitions,
            directives,
            selectionSet
        );
    }

    /// <summary>
    /// Parses a short-hand form operation definition.
    /// <see cref="OperationDefinitionNode" />:
    /// SelectionSet
    /// </summary>
    private OperationDefinitionNode ParseShortOperationDefinition()
    {
        TokenInfo start = Start();
        SelectionSetNode selectionSet = ParseSelectionSet();
        Location? location = CreateLocation(in start);

        return new OperationDefinitionNode
        (
            location,
            null,
            OperationType.Query,
            Array.Empty<VariableDefinitionNode>(),
            Array.Empty<DirectiveNode>(),
            selectionSet
        );
    }

    /// <summary>
    /// Parses the <see cref="OperationType" />.
    /// </summary>
    private OperationType ParseOperationType()
    {
        if (_reader.Kind == TokenKind.Name)
        {
            if (_reader.Value.SequenceEqual(GraphQLKeywords.Query))
            {
                MoveNext();
                return OperationType.Query;
            }

            if (_reader.Value.SequenceEqual(GraphQLKeywords.Mutation))
            {
                MoveNext();
                return OperationType.Mutation;
            }

            if (_reader.Value.SequenceEqual(GraphQLKeywords.Subscription))
            {
                MoveNext();
                return OperationType.Subscription;
            }
        }

        throw Unexpected(TokenKind.Name);
    }

    /// <summary>
    /// Parses variable definitions.
    /// <see cref="IEnumerable{VariableDefinitionNode}" />:
    /// ( VariableDefinition+ )
    /// </summary>
    private List<VariableDefinitionNode> ParseVariableDefinitions()
    {
        if (_reader.Kind == TokenKind.LeftParenthesis)
        {
            var list = new List<VariableDefinitionNode>();

            // skip opening token
            MoveNext();

            while (_reader.Kind != TokenKind.RightParenthesis)
            {
                list.Add(ParseVariableDefinition());
            }

            // skip closing token
            ExpectRightParenthesis();

            return list;
        }

        return _emptyVariableDefinitions;
    }

    /// <summary>
    /// Parses a variable definition.
    /// <see cref="VariableDefinitionNode" />:
    /// $variable : Type = DefaultValue?
    /// </summary>
    private VariableDefinitionNode ParseVariableDefinition()
    {
        TokenInfo start = Start();

        VariableNode variable = ParseVariable();
        ExpectColon();
        ITypeNode type = ParseTypeReference();
        IValueNode? defaultValue = SkipEqual()
            ? ParseValueLiteral(true)
            : null;
        List<DirectiveNode> directives =
            ParseDirectives(true);

        Location? location = CreateLocation(in start);

        return new VariableDefinitionNode
        (
            location,
            variable,
            type,
            defaultValue,
            directives
        );
    }

    /// <summary>
    /// Parse a variable.
    /// <see cref="VariableNode" />:
    /// $Name
    /// </summary>
    private VariableNode ParseVariable()
    {
        TokenInfo start = Start();
        ExpectDollar();
        NameNode name = ParseName();
        Location? location = CreateLocation(in start);

        return new VariableNode
        (
            location,
            name
        );
    }

    /// <summary>
    /// Parses a selection set.
    /// <see cref="SelectionSetNode" />:
    /// { Selection+ }
    /// </summary>
    private SelectionSetNode ParseSelectionSet()
    {
        TokenInfo start = Start();

        if (_reader.Kind != TokenKind.LeftBrace)
        {
            throw new SyntaxException(_reader,
                string.Format(
                    CultureInfo.InvariantCulture,
                    ParseMany_InvalidOpenToken,
                    TokenKind.LeftBrace,
                    TokenPrinter.Print(in _reader)));
        }

        var selections = new List<ISelectionNode>();

        // skip opening token
        MoveNext();

        while (_reader.Kind != TokenKind.RightBrace)
        {
            selections.Add(ParseSelection());
        }

        // skip closing token
        ExpectRightBrace();

        Location? location = CreateLocation(in start);

        return new SelectionSetNode
        (
            location,
            selections
        );
    }

    /// <summary>
    /// Parses a selection.
    /// <see cref="ISelectionNode" />:
    /// - Field
    /// - FragmentSpread
    /// - InlineFragment
    /// </summary>
    private ISelectionNode ParseSelection()
    {
        if (_reader.Kind == TokenKind.Spread)
        {
            return ParseFragment();
        }
        return ParseField();
    }

    /// <summary>
    /// Parses a field.
    /// <see cref="FieldNode"  />:
    /// Alias? : Name Arguments? Directives? SelectionSet?
    /// </summary>
    private FieldNode ParseField()
    {
        TokenInfo start = Start();

        NameNode name = ParseName();
        NameNode? alias = null;

        if (SkipColon())
        {
            alias = name;
            name = ParseName();
        }

        List<ArgumentNode> arguments = ParseArguments(false);
        INullabilityNode? required = ParseRequiredStatus();
        List<DirectiveNode> directives = ParseDirectives(false);
        SelectionSetNode? selectionSet = _reader.Kind == TokenKind.LeftBrace
            ? ParseSelectionSet()
            : null;

        Location? location = CreateLocation(in start);

        return new FieldNode
        (
            location,
            name,
            alias,
            required,
            directives,
            arguments,
            selectionSet
        );
    }

    private INullabilityNode? ParseRequiredStatus()
    {
        ListNullabilityNode? list = ParseListNullability();
        INullabilityNode? modifier = ParseModifier(list);
        return modifier ?? list;
    }

    private ListNullabilityNode? ParseListNullability()
    {
        if (_reader.Kind == TokenKind.LeftBracket)
        {
            TokenInfo start = Start();
            _reader.Skip(TokenKind.LeftBracket);
            INullabilityNode? element = ParseRequiredStatus();
            _reader.Expect(TokenKind.RightBracket);
            Location? location = CreateLocation(in start);
            return new ListNullabilityNode(location, element);
        }

        return null;
    }

    private INullabilityNode? ParseModifier(ListNullabilityNode? listNullabilityNode)
    {
        if (_reader.Kind == TokenKind.QuestionMark)
        {
            TokenInfo start = Start();
            _reader.Skip(TokenKind.QuestionMark);
            Location? location = CreateLocation(in start);
            return new OptionalModifierNode(location, listNullabilityNode);
        }

        if (_reader.Kind == TokenKind.Bang)
        {
            TokenInfo start = Start();
            _reader.Skip(TokenKind.Bang);
            Location? location = CreateLocation(in start);
            return new RequiredModifierNode(location, listNullabilityNode);
        }

        return listNullabilityNode;
    }

    /// <summary>
    /// Parses an argument.
    /// <see cref="ArgumentNode" />:
    /// Name : Value[isConstant]
    /// </summary>
    private List<ArgumentNode> ParseArguments(bool isConstant)
    {
        if (_reader.Kind == TokenKind.LeftParenthesis)
        {
            var list = new List<ArgumentNode>();

            // skip opening token
            MoveNext();

            while (_reader.Kind != TokenKind.RightParenthesis)
            {
                list.Add(ParseArgument(isConstant));
            }

            // skip closing token
            ExpectRightParenthesis();

            return list;
        }
        return _emptyArguments;
    }


    /// <summary>
    /// Parses an argument.
    /// <see cref="ArgumentNode" />:
    /// Name : Value[isConstant]
    /// </summary>
    private ArgumentNode ParseArgument(bool isConstant)
    {
        TokenInfo start = Start();

        NameNode name = ParseName();
        ExpectColon();
        IValueNode value = ParseValueLiteral(isConstant);

        Location? location = CreateLocation(in start);

        return new ArgumentNode
        (
            location,
            name,
            value
        );
    }
}
