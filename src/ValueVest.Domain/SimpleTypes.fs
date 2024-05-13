namespace ValueVest.Domain

open System.Runtime.CompilerServices
open System
open System.Text.Json.Serialization
open System.Text.Json

[<IsReadOnly; Struct>]
type Currency = TRY | USD

[<IsReadOnly; Struct>]
type Symbol = private Symbol of string

[<IsReadOnly; Struct>]
type Price = private Price of decimal

[<IsReadOnly; Struct>]
type Name = private Name of string

[<IsReadOnly; Struct>]
type PriceEarnings = private PriceEarnings of float32

[<IsReadOnly; Struct>]
type PriceToBook = private PriceToBook of float32

[<IsReadOnly; Struct>]
type EvEbitda = private EvEbitda of float32

[<IsReadOnly; Struct>]
type EvSales = private EvSales of float32

[<IsReadOnly; Struct>]
type TagId = private TagId of int

[<IsReadOnly; Struct>]
type PublicOwnershipRatio = private PublicOwnershipRatio of float32

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
    | InvalidEvEbitda
    | InvalidEvSales
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

    let options =
        JsonFSharpOptions.Default()
            .ToJsonSerializerOptions()

    let Serialize element : string =
        JsonSerializer.Serialize(element, options)

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
        Price(price) |> Ok

    let DivideValues price1 price2 : decimal =
        let value1 = Value(price1)
        let value2 =  Value(price2)
        value1 / value2

module Name = 
    let Value(Name name) = name

    let Create (str: string) : Result<Name, ValidationError> = 
        match CommonFunctions.ValidateString(str) with 
        | Ok(s) -> Ok(Name(s))
        | Error(e) -> InvalidName |> Error

module PriceEarnings = 
    let Value(PriceEarnings pe) = pe

    let Create (rawPriceEarnings: float32) : Result<PriceEarnings, ValidationError> =
        if (rawPriceEarnings < 0.0f) then
            InvalidPriceEarnings |> Error 
        else 
            PriceEarnings(rawPriceEarnings) |> Ok

module PriceToBook = 
    let Value(PriceToBook pb) = pb

    let Create(rawPriceToBook: float32) : Result<PriceToBook, ValidationError> =
        if (rawPriceToBook < 0.0f) then
            InvalidPriceToBook|> Error 
        else 
            PriceToBook(rawPriceToBook) |> Ok

module EvEbitda = 
    let Value(EvEbitda pb) = pb

    let Create(rawEvEbitda: float32) : Result<EvEbitda, ValidationError> =
        if (rawEvEbitda < 0.0f) then
            InvalidEvEbitda|> Error 
        else 
            EvEbitda(rawEvEbitda) |> Ok

module EvSales = 
    let Value(EvSales pb) = pb

    let Create(rawEvSales: float32) : Result<EvSales, ValidationError> =
        if (rawEvSales < 0.0f) then
            InvalidEvSales|> Error 
        else 
            EvSales(rawEvSales) |> Ok

module TagId = 
    let Value(TagId tagId) = tagId

    let Create(rawId: int) : Result<TagId, ValidationError> = 
        if (rawId < 0) then 
            InvalidSectorId |> Error
        else
            TagId(rawId) |> Ok

    let GetSectorIdUrlString(sectorId: TagId) : string =
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
    let Create(ratio: float32) : Result<PublicOwnershipRatio, ValidationError> =
        if (ratio < 0.0f || ratio > 100.0f) then
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