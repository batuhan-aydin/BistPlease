namespace BistPlease.Domain

open System.Runtime.CompilerServices
open System

[<IsReadOnly; Struct>]
type Currency = TL | USD

[<IsReadOnly; Struct>]
type Symbol = private Symbol of string

[<IsReadOnly; Struct>]
type Price = private Price of decimal

[<IsReadOnly; Struct>]
type Name = private Name of string

[<IsReadOnly; Struct>]
type PriceEarnings = private PriceEarnings of decimal

[<IsReadOnly; Struct>]
type PriceToBook = private PriceToBook of decimal

[<IsReadOnly; Struct>]
type SectorId = private SectorId of int

[<IsReadOnly; Struct>]
type PublicOwnershipRatio = private PublicOwnershipRatio of decimal

[<IsReadOnly; Struct>]
type Profit = private Profit of bigint

[<IsReadOnly; Struct>]
type AnnualProfit = private AnnualProfit of bigint

type LastBalanceTerm =
    | First
    | Second
    | Third
    | Fourth

type ValidationError =
    | InvalidSymbol 
    | InvalidPrice
    | InvalidName
    | InvalidPriceEarnings
    | InvalidPriceToBook
    | InvalidSectorId
    | InvalidPublicOwnershipRatio
    | InvalidLastFinancialsTerm
    | InvalidWorth

module CommonFunctions = 
    let internal ValidateString (str: string) : Result<string, string> =
        match str with
        | s when String.IsNullOrEmpty s -> Error "Empty string not error!" 
        | s -> Ok s

    let internal ResultToOption (result: Result<'a, 'b>) : 'a option =
        match result with
        | Ok value -> Some value
        | Error _ -> None

module Symbol = 
    let Value(Symbol symbol) = symbol

    let Create(unvalidatedSymbol: string) : Result<Symbol, ValidationError> =
        match CommonFunctions.ValidateString(unvalidatedSymbol) with
        | Ok validatedCode ->
            Symbol(validatedCode) |> Ok
        | Error _ ->
            InvalidSymbol|> Error

module Price = 
    let Value(Price price) = price

    let Create(price: decimal) : Result<Price, ValidationError> = 
        if (price < 0.0M) then 
            InvalidPrice |> Error
        else 
            Price(price) |> Ok

module Name = 
    let Value(Name name) = name

    let Create (str: string) : Result<Name, ValidationError> = 
        match CommonFunctions.ValidateString(str) with 
        | Ok(s) -> Ok(Name(s))
        | Error(e) -> InvalidName |> Error

module PriceEarnings = 
    let Value(PriceEarnings pe) = pe

    let Create (rawPriceEarnings: decimal) : Result<PriceEarnings, ValidationError> =
        if (rawPriceEarnings < 0.0M) then
            InvalidPriceEarnings |> Error 
        else 
            PriceEarnings(rawPriceEarnings) |> Ok

module PriceToBook = 
    let Value(PriceToBook pb) = pb

    let Create(rawPriceToBook: decimal) : Result<PriceToBook, ValidationError> =
        if (rawPriceToBook < 0.0M) then
            InvalidPriceToBook|> Error 
        else 
            PriceToBook(rawPriceToBook) |> Ok

module SectorId = 
    let Value(SectorId sectorId) = sectorId

    let Create(rawId: int) : Result<SectorId, ValidationError> = 
        if (rawId < 0) then 
            InvalidSectorId |> Error
        else
            SectorId(rawId) |> Ok

    let GetSectorIdUrlString(sectorId: SectorId) : string =
        let sectorIdValue = Value(sectorId)
        if (sectorIdValue < 10) then 
            sprintf "000%d" sectorIdValue
        elif (sectorIdValue < 100) then 
            sprintf "00%d" sectorIdValue
        elif (sectorIdValue < 1000) then 
            sprintf "0%d" sectorIdValue
        else 
            sprintf "%d" sectorIdValue

module PublicOwnershipRatio =
    let Create(ratio: decimal) : Result<PublicOwnershipRatio, ValidationError> =
        if (ratio < 0.0M || ratio > 100.0M) then
            InvalidPublicOwnershipRatio |> Error
        else 
            PublicOwnershipRatio(ratio) |> Ok

module LastBalanceTerm =
    let Create (lastTerm: string) : Result<LastBalanceTerm, ValidationError> =
        let validatedString = CommonFunctions.ValidateString(lastTerm)
        match validatedString with
        | Ok value -> 
            match value with 
            | _ when value.StartsWith("3") -> Ok LastBalanceTerm.First 
            | _ when value.StartsWith("6") -> Ok LastBalanceTerm.Second
            | _ when value.StartsWith("9") -> Ok LastBalanceTerm.Third
            | _ when value.StartsWith("12") -> Ok LastBalanceTerm.Fourth
            | _ -> Error InvalidLastFinancialsTerm
        | Error _ -> Error InvalidLastFinancialsTerm

module Profit = 
    let Value(Profit profit) = profit

module AnnualProfit = 
    let Create(lastFinancialTerm: LastBalanceTerm, lastProfit: Profit) : AnnualProfit =
        match lastFinancialTerm with 
        | LastBalanceTerm.First -> AnnualProfit(Profit.Value(lastProfit) * bigint 4)
        | LastBalanceTerm.Second -> AnnualProfit(Profit.Value(lastProfit) * bigint 2)
        | LastBalanceTerm.Third -> AnnualProfit(Profit.Value(lastProfit) + (Profit.Value(lastProfit) / bigint 3))
        | LastBalanceTerm.Fourth -> AnnualProfit(Profit.Value(lastProfit))