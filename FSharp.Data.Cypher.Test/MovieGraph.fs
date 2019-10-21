﻿namespace FSharp.Data.Cypher.Test.MovieGraph

open Neo4j.Driver
open FSharp.Data.Cypher
open FSharp.Data.Cypher.Test
open Xunit
open GraphDomains.MovieGraph

module ``Primative Types`` =

    open RETURN.``Deserialize: Spoofed Results``

    let f x =
        Cypher.run Graph.Driver x
        |> QueryResult.results
        |> Seq.head

    let [<Fact>] ``Bool true`` () = fBoolTrue f
    let [<Fact>] ``Bool false`` () = fBoolFalse f
    let [<Fact>] ``int32`` () = fInt32 f
    let [<Fact>] ``int64`` () = fInt64 f
    let [<Fact>] ``float`` () = fFloat f
    let [<Fact>] ``string`` () = fString f
    let [<Fact>] ``tuple of all`` () = fTupleOfAll f

module ``Can deserialize all`` =

    [<Fact>]
    let movies () =
        cypher {
            for m in Graph.Movie do
            MATCH (Node(m, m.Label))
            RETURN m
        }
        |> Cypher.run Graph.Driver
        |> QueryResult.results
        |> fun xs -> Assert.All(xs, fun x -> Assert.IsType(typeof<Movie>, x))

    [<Fact>]
    let people () =
        cypher {
            for p in Graph.Person do
            MATCH (Node(p, p.Label))
            RETURN p
        }
        |> Cypher.run Graph.Driver
        |> QueryResult.results
        |> fun xs -> Assert.All(xs, fun x -> Assert.IsType(typeof<Person>, x))

    [<Fact>]
    let actedIn () =
        cypher {
            for a in Graph.ActedIn do
            MATCH (Node() -- Rel(a, a.Label) -- Node())
            RETURN a
        }
        |> Cypher.run Graph.Driver
        |> QueryResult.results
        |> fun xs -> Assert.All(xs, fun x -> Assert.IsType(typeof<ActedIn>, x))

    [<Fact>]
    let directed() =
        cypher {
            for d in Graph.Directed do
            MATCH (Node() -- Rel(d, d.Label) -- Node())
            RETURN d
        }
        |> Cypher.run Graph.Driver
        |> QueryResult.results
        |> fun xs -> Assert.All(xs, fun x -> Assert.IsType(typeof<Directed>, x))

    [<Fact>]
    let follows () =
        cypher {
            for f in Graph.Follows do
            MATCH (Node() -- Rel(f, f.Label) -- Node())
            RETURN f
        }
        |> Cypher.run Graph.Driver
        |> QueryResult.results
        |> fun xs -> Assert.All(xs, fun x -> Assert.IsType(typeof<Follows>, x))

    [<Fact>]
    let produced () =
        cypher {
            for pr in Graph.Produced do

            MATCH (Node() -- Rel(pr, pr.Label) -- Node())
            RETURN pr
        }
        |> Cypher.run Graph.Driver
        |> QueryResult.results
        |> fun xs -> Assert.All(xs, fun x -> Assert.IsType(typeof<Produced>, x))

    [<Fact>]
    let reviewed () =
        cypher {
            for r in Graph.Reviewed do
            MATCH (Node() -- Rel(r, r.Label) -- Node())
            RETURN r
        }
        |> Cypher.run Graph.Driver
        |> QueryResult.results
        |> fun xs -> Assert.All(xs, fun x -> Assert.IsType(typeof<Reviewed>, x))

    [<Fact>]
    let wrote () =
        cypher {
            for w in Graph.Wrote do
            MATCH (Node() -- Rel(w, w.Label) -- Node())
            RETURN w
        }
        |> Cypher.run Graph.Driver
        |> QueryResult.results
        |> fun xs -> Assert.All(xs, fun x -> Assert.IsType(typeof<Wrote>, x))