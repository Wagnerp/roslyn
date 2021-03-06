' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    Public Class ContainsChildrenGraphQueryTests
        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
        Sub ContainsChildrenForDocument()
            Using testState = New ProgressionTestState(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class C { }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithDocumentNode(filePath:="Z:\Project.cs")
                Dim outputContext = testState.GetGraphContextAfterQuery(inputGraph, New ContainsChildrenGraphQuery(), GraphContextDirection.Self)

                Dim node = outputContext.Graph.Nodes.Single()

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" ContainsChildren="True" Label="Project.cs"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
        Sub ContainsChildrenForEmptyDocument()
            Using testState = New ProgressionTestState(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                            </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithDocumentNode(filePath:="Z:\Project.cs")
                Dim outputContext = testState.GetGraphContextAfterQuery(inputGraph, New ContainsChildrenGraphQuery(), GraphContextDirection.Self)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" ContainsChildren="False" Label="Project.cs"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Sub

        <WorkItem(789685)>
        <WorkItem(794846)>
        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
        Sub ContainsChildrenForNotYetLoadedSolution()
            Using testState = New ProgressionTestState(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class C { }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithDocumentNode(filePath:="Z:\Project.cs")

                ''' To simulate the situation where a solution is not yet loaded and project info is not available,
                ''' remove a project from the solution.

                Dim oldSolution = testState.GetSolution()
                Dim newSolution = oldSolution.RemoveProject(oldSolution.ProjectIds.FirstOrDefault())
                Dim outputContext = testState.GetGraphContextAfterQueryWithSolution(inputGraph, newSolution, New ContainsChildrenGraphQuery(), GraphContextDirection.Self)

                ''' ContainsChildren should be set to false, so following updates will be tractable.

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" ContainsChildren="False" Label="Project.cs"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                        </IdentifierAliases>
                    </DirectedGraph>)

            End Using
        End Sub
    End Class
End Namespace
