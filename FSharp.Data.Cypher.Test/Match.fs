﻿namespace FSharp.Data.Cypher.Test.Match

open System.Collections
open FSharp.Data.Cypher
open FSharp.Data.Cypher.Test
open Xunit


module Node =

    module ``Empty Constructor`` =
        
        [<Fact>]
        let ``Single Node`` () =

            cypher {
                MATCH (Node())
                RETURN ()
            }
            |> Cypher.queryNonParameterized
            |> fun q -> Assert.Equal(q, "MATCH () RETURN ")
            
        [<Fact>]
        let ``Two Nodes --`` () =

            cypher {
                MATCH (Node() -- Node())
                RETURN ()
            }
            |> Cypher.queryNonParameterized
            |> fun q -> Assert.Equal(q, "MATCH ()--() RETURN ")
            
        [<Fact>]
        let ``Two Nodes -->`` () =

            cypher {
                MATCH (Node() --> Node())
                RETURN ()
            }
            |> Cypher.queryNonParameterized
            |> fun q -> Assert.Equal(q, "MATCH ()-->() RETURN ")
            
        [<Fact>]
        let ``Two Nodes <--`` () =

            cypher {
                MATCH (Node() <-- Node())
                RETURN ()
            }
            |> Cypher.queryNonParameterized
            |> fun q -> Assert.Equal(q, "MATCH ()<--() RETURN ")
    
    module ``Single Parameter Constructor`` =

        module ``NodeLabel`` =
        
            let label = "NodeLabel"
            let nodeLabel = NodeLabel label
            let rtnSt = sprintf "MATCH (:%s) RETURN " label
        
            [<Fact>]
            let ``Create in Node Constructor`` () =
                cypher {
                    MATCH (Node(NodeLabel label))
                    RETURN ()
                }
                |> Cypher.queryNonParameterized
                |> fun q -> Assert.Equal(q, rtnSt)

            [<Fact>]
            let ``Variable outside function`` () =

                cypher {
                    MATCH (Node nodeLabel)
                    RETURN ()
                }
                |> Cypher.queryNonParameterized
                |> fun q -> Assert.Equal(q, rtnSt)

            [<Fact>]
            let ``Variable passed as function parameter`` () =
                let f (nodeLabel : NodeLabel) =
                    cypher {
                        MATCH (Node nodeLabel)
                        RETURN ()
                    }

                f nodeLabel
                |> Cypher.queryNonParameterized
                |> fun q -> Assert.Equal(q, rtnSt)
        
            [<Fact>]
            let ``Variable in statement`` () =
                cypher {
                    let nodeLabel = NodeLabel label
                    MATCH (Node nodeLabel)
                    RETURN ()
                }
                |> Cypher.queryNonParameterized
                |> fun q -> Assert.Equal(q, rtnSt)
        
        module ``NodeLabel list`` =
        
            let label = "NodeLabel"
            let nodeLabels = [ NodeLabel label; NodeLabel label; NodeLabel label]
            let rtnSt = sprintf "MATCH (:%s:%s:%s) RETURN " label label label
        
            [<Fact>]
            let ``Create in Node Constructor`` () =
                cypher {
                    MATCH (Node([ NodeLabel label; NodeLabel label; NodeLabel label]))
                    RETURN ()
                }
                |> Cypher.queryNonParameterized
                |> fun q -> Assert.Equal(q, rtnSt)

            [<Fact>]
            let ``Variable outside function`` () =

                cypher {
                    MATCH (Node nodeLabels)
                    RETURN ()
                }
                |> Cypher.queryNonParameterized
                |> fun q -> Assert.Equal(q, rtnSt)

            [<Fact>]
            let ``Variable passed as function parameter`` () =
                let f (nodeLabels : NodeLabel list) =
                    cypher {
                        MATCH (Node nodeLabels)
                        RETURN ()
                    }

                f nodeLabels
                |> Cypher.queryNonParameterized
                |> fun q -> Assert.Equal(q, rtnSt)
        
            [<Fact>]
            let ``Variable in statement`` () =
                cypher {
                    let nodeLabels = [ NodeLabel label; NodeLabel label; NodeLabel label]
                    MATCH (Node nodeLabels)
                    RETURN ()
                }
                |> Cypher.queryNonParameterized
                |> fun q -> Assert.Equal(q, rtnSt)

module Relationship =

    module ``Single Parameter Constructor`` =

        module ``RelLabel`` =
        
            let label = "REL_LABEL"
            let relLabel = RelLabel label
            let rtnSt = sprintf "MATCH [:%s] RETURN " label
        
            [<Fact>]
            let ``Create in Rel Constructor`` () =
                cypher {
                    MATCH (Rel(RelLabel label))
                    RETURN ()
                }
                |> Cypher.queryNonParameterized
                |> fun q -> Assert.Equal(q, rtnSt)

            [<Fact>]
            let ``Variable outside function`` () =

                cypher {
                    MATCH (Rel relLabel)
                    RETURN ()
                }
                |> Cypher.queryNonParameterized
                |> fun q -> Assert.Equal(q, rtnSt)

            [<Fact>]
            let ``Variable passed as function parameter`` () =
                let f (relLabel : RelLabel) =
                    cypher {
                        MATCH (Rel relLabel)
                        RETURN ()
                    }

                f relLabel
                |> Cypher.queryNonParameterized
                |> fun q -> Assert.Equal(q, rtnSt)
        
            [<Fact>]
            let ``Variable in statement`` () =
                cypher {
                    let relLabel = RelLabel label
                    MATCH (Rel relLabel)
                    RETURN ()
                }
                |> Cypher.queryNonParameterized
                |> fun q -> Assert.Equal(q, rtnSt)
        
        module ``RelLabel Combination Operator`` =
        
            let label = "REL_LABEL"
            let relLabel = RelLabel label / RelLabel label / RelLabel label
            let rtnSt = sprintf "MATCH [:%s|:%s|:%s] RETURN " label label label
        
            [<Fact>]
            let ``Create in Rel Constructor`` () =
                cypher {
                    MATCH (Rel(RelLabel label / RelLabel label / RelLabel label))
                    RETURN ()
                }
                |> Cypher.queryNonParameterized
                |> fun q -> Assert.Equal(q, rtnSt)

            [<Fact>]
            let ``Variable outside function`` () =

                cypher {
                    MATCH (Rel relLabel)
                    RETURN ()
                }
                |> Cypher.queryNonParameterized
                |> fun q -> Assert.Equal(q, rtnSt)

            [<Fact>]
            let ``Variable passed as function parameter`` () =
                let f (relLabel : RelLabel) =
                    cypher {
                        MATCH (Rel relLabel)
                        RETURN ()
                    }

                f relLabel
                |> Cypher.queryNonParameterized
                |> fun q -> Assert.Equal(q, rtnSt)
        
            [<Fact>]
            let ``Variable in statement`` () =
                cypher {
                    let relLabel = RelLabel label / RelLabel label / RelLabel label
                    MATCH (Rel relLabel)
                    RETURN ()
                }
                |> Cypher.queryNonParameterized
                |> fun q -> Assert.Equal(q, rtnSt)
    
        module ``IRelationship`` =

            let rel = { new IFSRelationship }
            let rtnSt = "MATCH [rel] RETURN "

            // Quotations can't contain object expressions
            // so use a graph for binding test
            type Graph =
                static member Rel : Query<IFSRelationship> = NA
        
            //[<Fact>]
            //let ``Create in Rel Constructor`` () =
            //    cypher {
            //        MATCH (Rel { new IFSRelationship })
            //        RETURN ()
            //    }
            //    |> Cypher.queryNonParameterized
            //    |> fun q -> Assert.Equal(q, rtnSt)

            [<Fact>]
            let ``Variable outside function`` () =
                cypher {
                    MATCH (Rel rel)
                    RETURN ()
                }
                |> Cypher.queryNonParameterized
                |> fun q -> Assert.Equal(q, rtnSt)

            [<Fact>]
            let ``Variable passed as function parameter`` () =
                let f (rel : IFSRelationship) =
                    cypher {
                        MATCH (Rel rel)
                        RETURN ()
                    }

                f rel
                |> Cypher.queryNonParameterized
                |> fun q -> Assert.Equal(q, rtnSt)
        
            [<Fact>]
            let ``Variable in statement`` () =
                cypher {
                    for rel in Graph.Rel do
                    MATCH (Rel rel)
                    RETURN ()
                }
                |> Cypher.queryNonParameterized
                |> fun q -> Assert.Equal(q, rtnSt)