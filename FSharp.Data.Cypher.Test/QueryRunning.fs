﻿namespace FSharp.Data.Cypher.Test.QueryRunning

open System
open System.Collections.Generic
open FSharp.Data.Cypher
open FSharp.Data.Cypher.Test
open Neo4j.Driver.V1
open Xunit

module ``Primtive Types`` =

    let spoofDic =  
        [ "True", box true // dotnet use caps in serialization
          "False", box false
          "5", box 5L // DB returns int64
          "7", box 7L
          "5.5", box 5.5
          "\"EMU\"", box "EMU" ]
        |> Map.ofList
        |> Dictionary

    [<Fact>]
    let ``Bool true`` () =
        cypher {
            RETURN true
        }
        |> Cypher.read driver
        |> CypherResult.results
        |> Seq.head
        |> fun r -> Assert.Equal(true, r)
    
    [<Fact>]
    let ``Bool false`` () =
        cypher {
            RETURN false
        }
        |> Cypher.read driver
        |> CypherResult.results
        |> Seq.head
        |> fun r -> Assert.Equal(false, r)
        
    [<Fact>]
    let ``int32`` () =
        cypher {
            RETURN 5
        }
        |> Cypher.read driver
        |> CypherResult.results
        |> Seq.head
        |> fun r -> Assert.Equal(5, r)

    [<Fact>]
    let ``int64`` () =
        cypher {
            RETURN 5L
        }
        |> Cypher.read driver
        |> CypherResult.results
        |> Seq.head
        |> fun r -> Assert.Equal(5L, r)

    [<Fact>]
    let ``float`` () =
        cypher {
            RETURN 5.5
        }
        |> Cypher.read driver
        |> CypherResult.results
        |> Seq.head
        |> fun r -> Assert.Equal(5.5, r)

    [<Fact>]
    let ``string`` () =
        cypher {
            RETURN "EMU"
        }
        |> Cypher.read driver
        |> CypherResult.results
        |> Seq.head
        |> fun r -> Assert.Equal("EMU", r)

    let ``Tuple of all`` = 
        cypher {
            RETURN (true, false, 5, 7L, 5.5, "EMU")
        }
        |> Cypher.read driver
        |> CypherResult.results
        |> Seq.head
        |> fun r -> Assert.Equal((true, false, 5, 7L, 5.5, "EMU"), r)

module ``Complex Queries with Record Types`` = 
    
    open ``Movie Graph As Records``

    [<Fact>]
    let ``Can do string, int deserialization`` () =
        cypher {
            for m in Graph.Movie do
            for a in Graph.ActedIn do
            for d in Graph.Directed do
            for p in Graph.Person do
            MATCH (p -| a |-> m <-| d |- p)
            RETURN (m.title, m.released)
        }
        |> Cypher.read driver
        |> CypherResult.results
        |> Seq.head
        |> fun (s, i) -> 
            (Assert.IsType<string> s) |> ignore
            (Assert.IsType<int> i)
    
    [<Fact>]
    let ``Can do basic Type deserialization`` () =
        cypher {
            for m in Graph.Movie do
            for a in Graph.ActedIn do
            for d in Graph.Directed do
            for p in Graph.Person do
            MATCH (p -| a |-> m <-| d |- p)
            RETURN (m, p, a, d)
        }
        |> Cypher.read driver
        |> CypherResult.results
        |> Seq.head
        |> fun (m, p, a, d) -> 
            Assert.IsType<Movie> m |> ignore
            Assert.IsType<ActedIn> a |> ignore
            Assert.IsType<Directed> d |> ignore
            Assert.IsType<Person> p

module ``Complex Queries with Classes`` = 
    
    open ``Movie Graph As Classes``

    [<Fact>]
    let ``Can do string, int deserialization`` () =
        cypher {
            for m in Graph.Movie do
            for a in Graph.ActedIn do
            for d in Graph.Directed do
            for p in Graph.Person do
            MATCH (p -| a |-> m <-| d |- p)
            RETURN (m.title, m.released)
        }
        |> Cypher.read driver
        |> CypherResult.results
        |> Seq.head
        |> fun (s, i) -> 
            (Assert.IsType<string> s) |> ignore
            (Assert.IsType<int> i)
    
    [<Fact>]
    let ``Can do basic Type deserialization`` () =
        cypher {
            for m in Graph.Movie do
            for a in Graph.ActedIn do
            for d in Graph.Directed do
            for p in Graph.Person do
            MATCH (p -| a |-> m <-| d |- p)
            RETURN (m, p, a, d)
        }
        |> Cypher.read driver
        |> CypherResult.results
        |> Seq.head
        |> fun (m, p, a, d) -> 
            Assert.IsType<Movie> m |> ignore
            Assert.IsType<ActedIn> a |> ignore
            Assert.IsType<Directed> d |> ignore
            Assert.IsType<Person> p