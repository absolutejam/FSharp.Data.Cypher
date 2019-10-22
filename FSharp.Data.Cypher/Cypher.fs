﻿namespace FSharp.Data.Cypher

open System
open System.Collections
open Neo4j.Driver

type ReturnContination<'T> = Generic.IReadOnlyDictionary<string, obj> -> 'T

type ParameterList = (string * obj option) list

[<RequireQualifiedAccess>]
type Clause =
    | ASC
    | CALL
    | CREATE
    | DELETE
    | DESC
    | DETACH_DELETE
    | FOREACH
    | LIMIT
    | MATCH
    | MERGE
    | ON_CREATE_SET
    | ON_MATCH_SET
    | OPTIONAL_MATCH
    | ORDER_BY
    | REMOVE
    | RETURN
    | RETURN_DISTINCT
    | SET
    | SKIP
    | UNION
    | UNION_ALL
    | UNWIND
    | WHERE
    | WITH
    | YIELD
    // To Add:
    // Paths... Path<'P> ...
    // Constraints:
    // CREATE CONSTRAINT ON, DROP CONSTRAINT ON, ASSERT, IS UNIQUE
    // Query profiling:
    // PROFILE, EXPLAIN
    // Indexes:
    // CREATE INDEX ON, DROP INDEX ON
    // Case:
    // CASE, WHEN, THEN, ELSE, END
    // Stored Proceedures:
    // CALL, YIELD

    override this.ToString() =
        match this with
        | ASC -> "ASC"
        | CALL -> "CALL"
        | CREATE -> "CREATE"
        | DELETE -> "DELETE"
        | DESC -> "DESC"
        | DETACH_DELETE -> "DETACH DELETE"
        | FOREACH -> "FOREACH"
        | LIMIT -> "LIMIT"
        | MATCH -> "MATCH"
        | MERGE -> "MERGE"
        | ON_CREATE_SET -> "ON CREATE SET"
        | ON_MATCH_SET -> "ON MATCH SET"
        | OPTIONAL_MATCH -> "OPTIONAL MATCH"
        | ORDER_BY -> "ORDER BY"
        | REMOVE -> "REMOVE"
        | RETURN -> "RETURN"
        | RETURN_DISTINCT -> "RETURN DISTINCT"
        | SET -> "SET"
        | SKIP -> "SKIP"
        | UNION -> "UNION"
        | UNION_ALL ->"UNION ALL"
        | UNWIND ->"UNWIND"
        | WHERE -> "WHERE"
        | WITH -> "WITH"
        | YIELD -> "YIELD"

    member this.IsWrite =
        match this with
        | CREATE | DELETE | DETACH_DELETE | FOREACH | MERGE
        | ON_CREATE_SET | ON_MATCH_SET | REMOVE | SET -> true
        | _  -> false

    member this.IsRead = not this.IsWrite

[<Sealed; NoComparison; NoEquality>]
type CypherStep(clause : Clause, statement : string, rawStatement : string, parameters : ParameterList) =
    member _.Clause = clause
    member _.Statement = statement
    member _.RawStatement = rawStatement
    member _.Parameters = parameters

type Query (steps : CypherStep list) =
    let sb = Text.StringBuilder()
    let makeQuery (paramterized : bool) (multiline : bool) =
        let add (s : string) = sb.Append s |> ignore
        let mutable count : int = 1
        let mutable isForEach = false
        let mutable padding = Text.StringBuilder()
        let pad() =
            isForEach <- true
            padding <- padding.Append "    "
        let unPad() =
            isForEach <- false
            padding <- padding.Remove(0, 4)

        for step in steps do
            add (string padding)
            add (string step.Clause)

            if multiline && step.Clause = Clause.FOREACH then pad()

            if step.Statement <> "" then
                add " "
                if paramterized then add step.Statement else add step.RawStatement

            if multiline && isForEach && step.RawStatement.EndsWith ")" then unPad()

            if count < steps.Length then
                if multiline then add Environment.NewLine else add " "

            count <- count + 1

        let qry = string sb
        sb.Clear() |> ignore
        qry

    let parameters = steps |> List.collect (fun cs -> cs.Parameters)
    let query = makeQuery true false
    let queryMultiline = makeQuery true true
    let rawQuery = makeQuery false false
    let rawQueryMultiline = makeQuery false true
    let isWrite = steps |> List.exists (fun x -> x.Clause.IsWrite)

    member _.Value = query
    member _.Multiline = queryMultiline
    member _.Raw = rawQuery
    member _.RawMultiline = rawQueryMultiline
    member _.IsWrite = isWrite
    member _.Parameters = parameters

type QueryResult<'T>(results : 'T [], summary : IResultSummary) =
    member _.Results = results
    member _.Summary = summary

module QueryResult =

    let results (qr : QueryResult<'T>) = qr.Results

    let summary (qr : QueryResult<'T>) = qr.Summary

type TransactionResult<'T>(results : 'T [], summary : IResultSummary, session : IAsyncSession, transaction : IAsyncTransaction) =
    member _.Results = results
    member _.Summary = summary
    member _.Session = session
    member _.Transaction = transaction
    member this.AsyncCommit() =
        async {
            do! this.Transaction.CommitAsync() |> Async.AwaitTask
            do! this.Session.CloseAsync() |> Async.AwaitTask
            return QueryResult(this.Results, this.Summary)
        }
    member this.Commit() = this.AsyncCommit() |> Async.RunSynchronously
    member this.AsyncRollback() =
        async {
            do! this.Transaction.RollbackAsync() |> Async.AwaitTask
            do! this.Session.CloseAsync() |> Async.AwaitTask
            return ()
        }
    member this.Rollback() = this.AsyncRollback() |> Async.RunSynchronously

module TransactionResult =

    let results (tr : TransactionResult<'T>) = tr.Results

    let summary (tr : TransactionResult<'T>) = tr.Summary

    let transaction (tr : TransactionResult<'T>) = tr.Transaction

    let commit (tr : TransactionResult<'T>) = tr.Commit()

    let rollback (tr : TransactionResult<'T>) = tr.Rollback()

    let asyncCommit (tr : TransactionResult<'T>) = tr.AsyncCommit()

    let asyncRollback (tr : TransactionResult<'T>) = tr.AsyncRollback()

type Cypher<'T> internal (continuation, query) =
    member _.Continuation : ReturnContination<'T> option = continuation
    member _.Query : Query = query

module Cypher =

    // Neo4j Driver is not happy unless this is Dictionary - doesn't like some F# collections even though implement IDictionary
    // it will give Neo4j.Driver.V1.ServiceUnavailableException: Unexpected end of stream, read returned 0
    // private as it introduces null
    let private makeParameters (cypher : Cypher<'T>) =
        cypher.Query.Parameters
        |> List.map (fun (k, v) -> k, if v.IsNone then null else v.Value)
        |> dict
        |> Generic.Dictionary

    // https://neo4j.com/docs/driver-manual/1.7/sessions-transactions/#driver-transactions-access-mode
    // Should I use array / parallel here? Lots of reflecion so may be worth while
    // TODO: Moved to 4.0 driver
    // - AsyncSession() - Add in session Config() options? Or expose passing in the session?
    // - WriteTransactionAsync() - Add in TransactionConfig()
    // - Back pressure handling on IStatementResultCursor : Not supported currently in dotnet driver

    let private asyncRunTransaction (driver : IDriver) (map : 'T -> 'U) (cypher : Cypher<'T>) =
        async {
            let session = driver.AsyncSession()
            try
                let run (t : IAsyncTransaction) = t.RunAsync(cypher.Query.Value, makeParameters cypher)

                let! statementCursor =
                    if cypher.Query.IsWrite
                    then session.WriteTransactionAsync run 
                    else session.ReadTransactionAsync run
                    |> Async.AwaitTask

                let! results =
                    match cypher.Continuation with
                    | Some continuation -> statementCursor.ToListAsync(fun record -> continuation record.Values |> map) |> Async.AwaitTask
                    | None -> async.Return(ResizeArray())

                let! summary = statementCursor.SummaryAsync() |> Async.AwaitTask

                return QueryResult(Seq.toArray results, summary)

            finally
                session.CloseAsync()
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> ignore
        }

    let asyncRunMap driver map cypher = asyncRunTransaction driver map cypher

    let asyncRun driver cypher = asyncRunMap driver id cypher

    let runMap (driver : IDriver) map cypher = asyncRunMap driver map cypher |> Async.RunSynchronously

    let run (driver : IDriver) cypher = runMap driver id cypher

    let spoof (di : Generic.IReadOnlyDictionary<string, obj>) (cypher : Cypher<'T>) =
        match cypher.Continuation with
        | Some continuation -> continuation di
        | None -> invalidOp "No RETURN clause given when running spoof."

    let rawQuery (cypher : Cypher<'T>) = cypher.Query.Raw

    let query (cypher : Cypher<'T>) = cypher.Query.Value

    /// Returns a TransactionResult - where the transation needs to be commited to the database or rolled back manually
    module Explicit =

        let private runTransaction (session : IAsyncSession) (map : 'T -> 'U) (cypher : Cypher<'T>) =
            async {
                let! transaction = session.BeginTransactionAsync() |> Async.AwaitTask
                let! statementCursor = transaction.RunAsync(cypher.Query.Value, makeParameters cypher) |> Async.AwaitTask

                let! results =
                    match cypher.Continuation with
                    | Some continuation ->
                        statementCursor.ToListAsync(fun record -> continuation record.Values |> map) |> Async.AwaitTask
                    | None -> async.Return(ResizeArray())

                let! summary = statementCursor.SummaryAsync() |> Async.AwaitTask

                return TransactionResult(Array.ofSeq results, summary, session, transaction)
            }

        let asyncRunMap (driver : IDriver) map (cypher : Cypher<'T>) =
            let session =
                if cypher.Query.IsWrite then
                    driver.AsyncSession(fun sc -> 
                        sc.DefaultAccessMode <- AccessMode.Write)
                else
                    driver.AsyncSession(fun sc ->
                        sc.DefaultAccessMode <- AccessMode.Read)
            try runTransaction session map cypher
            with e ->
                session.CloseAsync()
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> ignore
                raise e

        let asyncRun driver cypher = asyncRunMap driver id cypher

        let runMap driver map cypher = async.Return (runMap driver map cypher)

        let run (driver : IDriver) cypher = runMap driver id cypher