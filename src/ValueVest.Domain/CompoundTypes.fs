namespace ValueVest.Domain

open FsToolkit.ErrorHandling
open System

type Worth = 
    struct 
        val Price: Price 
        val Currency: Currency
        new (price: Price, currency: Currency) = {Price = price; Currency = currency}
    end

type FinancialsValueDto = {
    ItemCode: string 
    Value1: string option 
    Value2: string option 
    Value3: string option 
    Value4: string option
}

type FinancialsDto = {
    FinancialsList: FinancialsValueDto list
}

type FinancialsByTerm = {
    FirstTerm: Worth option
    SecondTerm: Worth option
    ThirdTerm: Worth option
    FourhTerm: Worth option
}


type TagFinancialRatios = {
    PriceEarnings: PriceEarnings
    PriceToBook: PriceToBook
    EvEbitda: EvEbitda
}

type Tag = {
    Id: TagId
    Name: Name
    FinancialRatios: TagFinancialRatios
}

module Worth = 
    let Create(rawPrice: decimal, currency: Currency) : Result<Worth, ValidationError> =
        let priceResult = Price.Create(rawPrice);
        match priceResult with 
            | Ok price -> 
                Worth(price, currency) |> Ok
            | Error _ -> Error ValidationError.InvalidWorth

    let CreateFromNullable (rawPrice: System.Nullable<decimal>, currency: Currency) : Result<Worth, ValidationError> = 
        match Option.ofNullable rawPrice with 
            | None ->  ValidationError.InvalidWorth |> Error
            | Some value -> Create(value, currency)

    let CreateFromString (rawPrice: string, currency: Currency) : Result<Worth, ValidationError> = 
        match Decimal.TryParse rawPrice with 
            | (true, value) ->  Create(value, currency)
            | _ -> ValidationError.InvalidWorth |> Error

module FinancialsByTerm = 
    let Create rawFirstTerm rawSecondTerm rawThirdTerm rawFourthTerm currency : Result<FinancialsByTerm, ValidationError> = 
        result {
            let firstTerm = Worth.CreateFromString(rawFirstTerm, currency) |> CommonFunctions.ResultToOption
            let secondTerm = Worth.CreateFromString(rawSecondTerm, currency) |> CommonFunctions.ResultToOption
            let thirdTerm = Worth.CreateFromString(rawThirdTerm, currency) |> CommonFunctions.ResultToOption
            let fourthTerm = Worth.CreateFromString(rawFourthTerm, currency) |> CommonFunctions.ResultToOption
            let financialsTerm = { FirstTerm = firstTerm ; SecondTerm = secondTerm; 
            ThirdTerm = thirdTerm; FourhTerm = fourthTerm }
            return financialsTerm
        }
