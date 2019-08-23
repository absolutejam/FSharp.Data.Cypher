﻿namespace FSharp.Data.Cypher

open System
open System.Reflection
open FSharp.Reflection
open Neo4j.Driver.V1

module Deserialization =

    let isOption (typ : Type) = typ.IsGenericType && typ.GetGenericTypeDefinition() = typedefof<Option<_>>

    let hasInterface (typ : Type) (name : string) = typ.GetInterface name |> isNull |> not

    let checkCollection<'T> (rtnObj : obj) = 
        if isNull rtnObj then Seq.empty
        elif rtnObj.GetType() = typeof<Collections.Generic.List<obj>> then
            rtnObj 
            :?> Collections.Generic.List<obj>
            |> Seq.cast<'T>
        else 
            rtnObj :?> 'T |> Seq.singleton 
    
    let checkCollectionOption<'T> (rtnObj : obj) = 
        if isNull rtnObj then None
        elif rtnObj.GetType() = typeof<Collections.Generic.List<obj>> then
            rtnObj 
            :?> Collections.Generic.List<obj>
            |> Seq.cast<'T>
            |> Some
        else 
            rtnObj :?> 'T |> Seq.singleton |> Some// Fix up case when database isn't a collection

    let makeSeq<'T> rtnObj = checkCollection<'T> rtnObj |> box
    let makeSeqOption<'T> rtnObj = checkCollectionOption<'T> rtnObj |> box

    let makeArray<'T> rtnObj = checkCollection<'T> rtnObj |> Array.ofSeq |> box
    let makeArrayOption<'T> rtnObj = checkCollectionOption<'T> rtnObj |> Option.map Array.ofSeq |> box

    let makeList<'T> rtnObj = checkCollection<'T> rtnObj |> List.ofSeq |> box
    let makeListOption<'T> rtnObj = checkCollectionOption<'T> rtnObj |> Option.map List.ofSeq |> box

    let makeSet<'T when 'T : comparison> rtnObj = checkCollection<'T> rtnObj |> Set.ofSeq |> box
    let makeSetOption<'T when 'T : comparison> rtnObj = checkCollectionOption<'T> rtnObj |> Option.map Set.ofSeq |> box

    let fixInt (obj : obj) =
        let i = obj :?> int64
        if i <= int64 Int32.MaxValue then i |> int |> box
        else InvalidCastException "Can't convert int64 to int32. Value returned is greater than Int32.MaxValue" |> raise

    let makeOption<'T> (obj : obj) = if isNull obj then box None else obj :?> 'T |> Some |> box

    // TODO: this needs to be tidied up
    let fixTypes (name : string) (localType : Type) (dbObj : obj) =

        let nullCheck (obj : obj) = 
            if isNull obj then
                localType.Name
                |> sprintf "A null object was returned for %s on type %A" name
                |> ArgumentNullException
                |> raise
            else obj

        // Test of collections
        // Driver returns a System.Collections.Generic.List`1[System.Object]
        let makeCollections(propTyp : Type) (rtnObj : obj) =
            if propTyp = typeof<string seq> then makeSeq<string> rtnObj
            elif propTyp = typeof<string seq option> then makeSeqOption<string> rtnObj
            elif propTyp = typeof<int64 seq> then makeSeq<int64> rtnObj
            elif propTyp = typeof<int64 seq option> then makeSeqOption<int64> rtnObj
            elif propTyp = typeof<int seq> then makeSeq<int> rtnObj
            elif propTyp = typeof<int seq option> then makeSeqOption<int> rtnObj
            elif propTyp = typeof<float seq> then makeSeq<float> rtnObj
            elif propTyp = typeof<float seq option> then makeSeqOption<float> rtnObj
            elif propTyp = typeof<bool seq option> then makeSeqOption<bool> rtnObj
            elif propTyp = typeof<bool seq> then makeSeq<bool> rtnObj
            
            elif propTyp = typeof<string array> then makeArray<string> rtnObj
            elif propTyp = typeof<string array option> then makeArrayOption<string> rtnObj
            elif propTyp = typeof<int64 array> then makeArray<int64> rtnObj
            elif propTyp = typeof<int64 array option> then makeArrayOption<int64> rtnObj
            elif propTyp = typeof<int array> then makeArray<int> rtnObj
            elif propTyp = typeof<int array option> then makeArrayOption<int> rtnObj
            elif propTyp = typeof<float array> then makeArray<float> rtnObj
            elif propTyp = typeof<float array option> then makeArrayOption<float> rtnObj
            elif propTyp = typeof<bool array option> then makeArrayOption<bool> rtnObj
            elif propTyp = typeof<bool array> then makeArray<bool> rtnObj
            
            elif propTyp = typeof<string list> then makeList<string> rtnObj
            elif propTyp = typeof<string list option> then makeListOption<string> rtnObj
            elif propTyp = typeof<int64 list> then makeList<int64> rtnObj
            elif propTyp = typeof<int64 list option> then makeListOption<int64> rtnObj
            elif propTyp = typeof<int list> then makeList<int> rtnObj
            elif propTyp = typeof<int list option> then makeListOption<int> rtnObj
            elif propTyp = typeof<float list> then makeList<float> rtnObj
            elif propTyp = typeof<float list option> then makeListOption<float> rtnObj
            elif propTyp = typeof<bool list option> then makeListOption<bool> rtnObj
            elif propTyp = typeof<bool list> then makeList<bool> rtnObj
            
            elif propTyp = typeof<string Set> then makeSet<string> rtnObj
            elif propTyp = typeof<string Set option> then makeSetOption<string> rtnObj
            elif propTyp = typeof<int64 Set> then makeSet<int64> rtnObj
            elif propTyp = typeof<int64 Set option> then makeSetOption<int64> rtnObj
            elif propTyp = typeof<int Set> then makeSet<int> rtnObj
            elif propTyp = typeof<int Set option> then makeSetOption<int> rtnObj
            elif propTyp = typeof<float Set> then makeSet<float> rtnObj
            elif propTyp = typeof<float Set option> then makeSetOption<float> rtnObj
            elif propTyp = typeof<bool Set option> then makeSetOption<bool> rtnObj
            elif propTyp = typeof<bool Set> then makeSet<bool> rtnObj

            else 
                propTyp.Name
                |> sprintf "Unsupported collection type: %s"
                |> invalidOp

        if localType = typeof<string> then nullCheck dbObj
        elif localType = typeof<string option> then makeOption<string> dbObj
        elif localType = typeof<int64> then nullCheck dbObj
        elif localType = typeof<int64 option> then makeOption<int64> dbObj
        elif localType = typeof<int32> then dbObj |> nullCheck |> fixInt
        elif localType = typeof<int32 option> then makeOption<int> dbObj
        elif localType = typeof<float> then nullCheck dbObj
        elif localType = typeof<float option> then makeOption<float> dbObj
        elif localType = typeof<bool> then nullCheck dbObj
        elif localType = typeof<bool option> then makeOption<bool> dbObj
        elif hasInterface localType "IEnumerable" then makeCollections localType dbObj // String includes IEnumerable
        else 
            localType
            |> sprintf "Unsupported property/value: %s. Type: %A" name
            |> invalidOp
    
    let deserialize (typ : Type) (entity : IEntity) = 
        typ.GetProperties()
        |> Array.map (fun pi ->
            match entity.Properties.TryGetValue pi.Name with
            | true, v -> fixTypes pi.Name pi.PropertyType v
            | _ ->
                if isOption pi.PropertyType then box None 
                else 
                    pi.Name
                    |> sprintf "Could not deserialize IEntity from the graph. The required property was not found: %s"
                    |> invalidOp)
      
    // Look into FSharpValue.PreComputeRecordConstructor - is it faster?
    // https://codeblog.jonskeet.uk/2008/08/09/making-reflection-fly-and-exploring-delegates/
    let toRecord (typ : Type) (entity : IEntity) = FSharpValue.MakeRecord(typ, deserialize typ entity)
    
    // Support parameterless constuctors
    let toClass (typ : Type) (entity : IEntity) = 
        let obs = deserialize typ entity
        if Array.isEmpty obs 
        then Activator.CreateInstance(typ)
        else Activator.CreateInstance(typ, obs)