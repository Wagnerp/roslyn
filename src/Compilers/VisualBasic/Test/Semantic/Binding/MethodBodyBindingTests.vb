' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities


Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class MethodBodyBindingTests
        Inherits BasicTestBase

        <Fact>
        Public Sub MethodBodyBindingTest()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation name="Compilation">
        <file name="q.vb">
Namespace N1
    Class Q
    End Class
End Namespace

Namespace N2
    Class Q
    End Class
End Namespace
        </file>
        <file name="a.vb">
Option Strict On

Imports N1

Namespace N
    Partial Class C
        Private Function meth1(Of TParam1, TParam2)(x as TParam1) As TParam2
            dim q as Integer
            if q > 4 then
                dim y as integer = q
                dim z as string
                while y &lt; 10
                    dim a as double
                end while
            else
                Dim y As String = "foo"
            end if
            Return Nothing
        End Function
    End Class
End Namespace
    </file>
        <file name="b.vb">
Option Strict On

Imports N2

Namespace N
    Partial Class C
        Private Sub meth2(y As String)
        End Sub
    End Class
End Namespace
    </file>
    </compilation>)

            Dim lr As LookupResult
            Dim globalNS = compilation.GlobalNamespace
            Dim namespaceN = DirectCast(globalNS.GetMembers("N").Single(), NamespaceSymbol)
            Dim namespaceN1 = DirectCast(globalNS.GetMembers("N1").Single(), NamespaceSymbol)
            Dim namespaceN2 = DirectCast(globalNS.GetMembers("N2").Single(), NamespaceSymbol)
            Dim classC = DirectCast(namespaceN.GetMembers("C").Single(), NamedTypeSymbol)
            Dim classQ1 = DirectCast(namespaceN1.GetMembers("Q").Single(), NamedTypeSymbol)
            Dim classQ2 = DirectCast(namespaceN2.GetMembers("Q").Single(), NamedTypeSymbol)
            Dim meth1 = DirectCast(classC.GetMembers("meth1").Single(), SourceMethodSymbol)
            Dim meth2 = DirectCast(classC.GetMembers("meth2").Single(), SourceMethodSymbol)

            Dim meth1Context As MethodBodyBinder = DirectCast(BinderBuilder.CreateBinderForMethodBody(DirectCast(meth1.ContainingModule, SourceModuleSymbol), meth1.SyntaxTree, meth1), MethodBodyBinder)
            Dim meth1Binding = MethodBodySemanticModel.Create(meth1Context)
            Assert.Same(meth1Context, meth1Binding.RootBinder.ContainingBinder) ' Strip off SemanticModelBinder

            ' Make sure parameters, type parameters, and imports are correct.
            lr = New LookupResult()
            meth1Context.Lookup(lr, "x", 0, Nothing, Nothing)
            Assert.True(lr.IsGood)
            Assert.Equal(meth1.Parameters(0), lr.SingleSymbol)

            lr.Clear()
            meth1Context.Lookup(lr, "TParam1", 0, LookupOptions.NamespacesOrTypesOnly, Nothing)
            Assert.True(lr.IsGood)
            Assert.Equal(meth1.TypeParameters(0), lr.SingleSymbol)

            lr.Clear()
            meth1Context.Lookup(lr, "TParam2", 0, LookupOptions.NamespacesOrTypesOnly, Nothing)
            Assert.True(lr.IsGood)
            Assert.Equal(meth1.TypeParameters(1), lr.SingleSymbol)

            lr.Clear()
            meth1Context.Lookup(lr, "Q", 0, LookupOptions.NamespacesOrTypesOnly, Nothing)
            Assert.True(lr.IsGood)
            Assert.Equal(classQ1, lr.SingleSymbol)

            Dim meth2Context As MethodBodyBinder = DirectCast(BinderBuilder.CreateBinderForMethodBody(DirectCast(meth2.ContainingModule, SourceModuleSymbol), meth2.SyntaxTree, meth2), MethodBodyBinder)
            Dim meth2Binding = MethodBodySemanticModel.Create(meth2Context)
            Assert.Same(meth2Context, meth2Binding.RootBinder.ContainingBinder) ' Strip off SemanticModelBinder

            ' Make sure parameters, and imports are correct.
            lr.Clear()
            meth2Context.Lookup(lr, "y", 0, Nothing, Nothing)
            Assert.True(lr.IsGood)
            Assert.Equal(meth2.Parameters(0), lr.SingleSymbol)

            lr.Clear()
            meth2Context.Lookup(lr, "Q", 0, LookupOptions.NamespacesOrTypesOnly, Nothing)
            Assert.True(lr.IsGood)
            Assert.Equal(classQ2, lr.SingleSymbol)

            ' Get the mappings and check that they seem to be right.
            Dim meth1Stmts = meth1.BlockSyntax.Statements
            Dim context As BlockBaseBinder = DirectCast(meth1Binding.RootBinder.GetBinder(meth1Stmts), BlockBaseBinder)
            Assert.Equal(1, context.Locals.Length)
            Assert.Equal("q", context.Locals(0).Name)

            Dim ifBlock = DirectCast(meth1Stmts(1), MultiLineIfBlockSyntax)
            Dim ifPartStmts = ifBlock.Statements
            Dim elsePartStmts = ifBlock.ElseBlock.Statements
            Dim ifContext = DirectCast(meth1Binding.RootBinder.GetBinder(ifPartStmts), BlockBaseBinder)
            Assert.Equal(2, ifContext.Locals.Length)
            Assert.Equal("y", ifContext.Locals(0).Name)
            Assert.Equal("z", ifContext.Locals(1).Name)
            Assert.Same(context, ifContext.ContainingBinder)
            Dim elseContext = DirectCast(meth1Binding.RootBinder.GetBinder(elsePartStmts), BlockBaseBinder)
            Assert.Equal(1, elseContext.Locals.Length)
            Assert.Equal("y", elseContext.Locals(0).Name)
            Assert.Same(context, elseContext.ContainingBinder)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub


        <Fact>
        Public Sub Bug4273()

            Dim compilationDef =
<compilation name="VBTrueByRefArguments1">
    <file name="a.vb">
Module M

  Sub Main()
    Foo()
  End Sub

  Sub Foo()
    Dim foo as Integer
    Foo = 4273
    System.Console.WriteLine(Foo)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
4273
]]>)
        End Sub

        <WorkItem(538834, "DevDiv")>
        <Fact>
        Public Sub AssertPassMultipleArgumentsWithByRef()
            Dim options = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation name="Compilation">
        <file name="AssertArgs.vb">
Imports System

Namespace VBN
    Module Program

        Sub SUB17(ByVal X1 As Integer, ByRef X2 As Integer)
        End Sub

        Sub SUBX(ByRef X1 As Integer, ByVal X2 As Integer) 
        End Sub

        Sub Repro01()
            'Dim DS(1) As String
            'Call SUB17(1, DS)
        Dim x1 = 0, x2 =1
        SUB17(x1, x2)
        End Sub

        Sub Repro02()
            Dim local = 123%
            SUBX(local, 1)
        End Sub

        Sub Main()
            Repro01()
            Repro02()
        End Sub
    End Module

End Namespace
        </file>
    </compilation>)

            compilation.AssertNoDiagnostics()

        End Sub

        <WorkItem(538870, "DevDiv")>
        <Fact>
        Public Sub AssertInvalidArrayInitnializer()
            Dim options = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation name="Compilation">
        <file name="AssertAry.vb">
Imports System

Namespace VBN

    Module Program

        Sub Main()
            'COMPILEERROR: BC30375, "Short()" , BC32014, "{"
            Dim FixedRankArray_19 As Short()= New Short() ({1, 2})
        End Sub
    End Module

End Namespace
        </file>
    </compilation>)

            ' Dev10 BC32014; Roslyn BC32014, BC30987
            'Assert.InRange(compilation.GetDiagnostics().Count, 1, 2)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30332: Value of type 'Short()()' cannot be converted to 'Short()' because 'Short()' is not derived from 'Short'.
            Dim FixedRankArray_19 As Short()= New Short() ({1, 2})
                                              ~~~~~~~~~~~~~~~~~~~~
BC32014: Bounds can be specified only for the top-level array when initializing an array of arrays.
            Dim FixedRankArray_19 As Short()= New Short() ({1, 2})
                                                           ~~~~~~
BC30987: '{' expected.
            Dim FixedRankArray_19 As Short()= New Short() ({1, 2})
                                                                  ~
</expected>)
        End Sub


        <WorkItem(538967, "DevDiv")>
        <Fact>
        Public Sub Bug4745()
            Dim options = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation name="Bug4745">
        <file name="Bug4745.vb">
Module M1
Sub Main()
'COMPILEERROR: BC30247, "Shared"
Shared x As Integer = 10
End Sub
End Module

        </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30247: 'Shared' is not valid on a local variable declaration.
Shared x As Integer = 10
~~~~~~
</expected>)
        End Sub

        <WorkItem(538491, "DevDiv")>
        <Fact>
        Public Sub Bug4118()
            Dim options = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation name="Bug4118">
        <file name="A.vb">
 Module CondComp0080mod
    Sub CondComp0080()
#Const Scen2 = 1.1D
#If Scen2 &lt;&gt; 1.1D Then
#End If
    End Sub
End Module
        </file>
    </compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(542234, "DevDiv")>
        <Fact>
        Public Sub BindCatchStatementLocal()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation name="BindCatchStatementLocal">
        <file name="try.vb">
Imports System

Class TryCatch
    Sub TryCatchTest(ByRef p As String)
        Try
            p = p + "Try"
        Catch ax As ArgumentException When p.Length = 4 'BIND1:"ax"
            p = "Catch1"
        Catch ex As ArgumentException 'BIND2:"ex"
            p = "Catch2"
        Finally
        End Try
    End Sub
End Class
        </file>
    </compilation>)

            CompilationUtils.AssertNoErrors(compilation)

            Dim node1 = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "try.vb", 1)
            Dim node2 = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "try.vb", 2)
            Assert.Equal(SyntaxKind.CatchStatement, node1.Parent.Kind)
            Assert.Equal(SyntaxKind.CatchStatement, node2.Parent.Kind)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim sym1 = model.GetDeclaredSymbol(DirectCast(node1.Parent, CatchStatementSyntax))
            Assert.NotNull(sym1)
            Assert.Equal(SymbolKind.Local, sym1.Kind)
            Assert.Equal("ax", sym1.Name)

            Dim sym2 = model.GetDeclaredSymbol(DirectCast(node2.Parent, CatchStatementSyntax))
            Assert.NotNull(sym2)
            Assert.Equal(SymbolKind.Local, sym2.Kind)
            Assert.Equal("ex", sym2.Name)
        End Sub

        <WorkItem(542234, "DevDiv")>
        <Fact>
        Public Sub BindCatchStatementNonLocal()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation name="BindCatchStatementNonLocal">
        <file name="catch.vb">
Imports System

Friend Module TryCatch
    Function TryCatchTest(ByRef pex As NullReferenceException) As ULong
        Dim lex As ArgumentException = Nothing
        Dim local = 12345
        Try
            local = local - 111
        Catch pex                   'BIND1:"pex"
            local = local - 222
        Catch lex When local = 456  'BIND2:"lex"
            local = local - 333
        End Try
        Return local
    End Function

    Function ReturnException(p As Object) As InvalidCastException
        ReturnException = Nothing
        Dim local = ""
        Try
            local = p
        Catch ReturnException       'BIND3:"ReturnException"
            local = Nothing
        End Try
    End Function
End Module
        </file>
    </compilation>)

            CompilationUtils.AssertNoErrors(compilation)

            Dim node1 = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "catch.vb", 1)
            Dim node2 = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "catch.vb", 2)
            Dim node3 = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "catch.vb", 3)
            Assert.Equal(SyntaxKind.CatchStatement, node1.Parent.Kind)
            Assert.Equal(SyntaxKind.CatchStatement, node2.Parent.Kind)
            Assert.Equal(SyntaxKind.CatchStatement, node3.Parent.Kind)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))

            Dim sym1 = model.GetDeclaredSymbol(DirectCast(node1.Parent, CatchStatementSyntax))
            Assert.Null(sym1)
            Dim sym2 = model.GetDeclaredSymbol(DirectCast(node2.Parent, CatchStatementSyntax))
            Assert.Null(sym2)
            Dim sym3 = model.GetDeclaredSymbol(DirectCast(node3.Parent, CatchStatementSyntax))
            Assert.Null(sym3)
        End Sub

        <WorkItem(529206, "DevDiv")>
        <Fact>
        Public Sub IntrinsicAliases_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation>
        <file name="a.vb">
Module Module1

    Sub Main()
        Dim x As System.Integer = System.Integer.MinValue
        Dim y As Integer = System.Integer.MinValue

        System.Short.Parse("123")
        System.UShort.Parse("123")
        System.Integer.Parse("123")
        System.UInteger.Parse("123")
        System.Long.Parse("123")
        System.ULong.Parse("123")
        System.Date.FromOADate(123)

        system.[Short].Parse("123")
        system.[UShort].Parse("123")
        system.[integer].Parse("123")
        system.[UInteger].Parse("123")
        system.[Long].Parse("123")
        system.[ULong].Parse("123")
        system.[Date].FromOADate(123)
    End Sub
End Module
        </file>
    </compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC30002: Type 'System.Integer' is not defined.
        Dim x As System.Integer = System.Integer.MinValue
                 ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(529206, "DevDiv")>
        <Fact>
        Public Sub IntrinsicAliases_2()
            Dim compilation = CompilationUtils.CreateCompilationWithoutReferences(
    <compilation name="Bug529206">
        <file name="a.vb">
Class Module1
    Dim x = System.Integer.MinValue
End Class

Namespace System

    Public Class [Object]
    End Class

    Public Structure Void
    End Structure

    Public Class ValueType
    End Class
End Namespace
        </file>
    </compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC31091: Import of type 'Integer' from assembly or module 'Bug529206.dll' failed.
    Dim x = System.Integer.MinValue
            ~~~~~~~~~~~~~~
BC30456: 'MinValue' is not a member of 'Integer'.
    Dim x = System.Integer.MinValue
            ~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(529206, "DevDiv")>
        <Fact>
        Public Sub IntrinsicAliases_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation>
        <file name="a.vb">
Module Module1

    Sub Main()
        Dim x = System.Integer.MinValue 'BIND1:"System.Integer"
    End Sub
End Module
        </file>
    </compilation>)

            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees(0))
            Dim node1 = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)

            Dim symbolInfo As SymbolInfo = model.GetSymbolInfo(node1)

            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString())

            Dim typeInfo As TypeInfo = model.GetTypeInfo(node1)

            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())
        End Sub

    End Class
End Namespace