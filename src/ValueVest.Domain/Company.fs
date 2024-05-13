namespace ValueVest.Domain

open ValueVest.Domain
open FsToolkit.ErrorHandling
open System.Text.Json
open System.Text.Json.Serialization



/// We will add more data into this, for now keeping it simple
type CompanyFinancials = {
    LastTermProfit: FinancialsByTerm
    OperationProfit: FinancialsByTerm 
}

type CompanyFinancialRatios = {
    PriceEarnings: PriceEarnings option
    PriceToBook: PriceToBook  option
    EvEbitda: EvEbitda option
    //EvSales: EvSales option
    LastBalanceTerm: LastBalanceTerm
}

type CompanyValuations = {
    LastClosingPrice: Worth
    MarketValue: Worth
    Capital: Worth
}

[<JsonFSharpConverter>]
type Company = {
    Symbol: Symbol
    Name: Name
    PublicOwnershipRatio: PublicOwnershipRatio
    FinancialRatios: CompanyFinancialRatios
    Valuations: CompanyValuations
}

module CompanyFinancials = 
    let Create lastTermProfit operationProfit : CompanyFinancials =
            let financials = { LastTermProfit = lastTermProfit; 
                OperationProfit = operationProfit }
            financials

module CompanyFinancialRatios =
    let Create (rawPriceEarnings: float32 option) (rawPriceToBook: float32 option) (rawEvEbitda: float32 option) (rawLastBalanceTerm: string)  : Result<CompanyFinancialRatios, ValidationError> =
      result {
        let priceEarnings = match rawPriceEarnings with 
                            | None  -> None 
                            | Some(value) -> match (value |> PriceEarnings.Create) with 
                                                      | Ok(value)  -> Some(value)
                                                      | _ -> None
        let priceToBook = match rawPriceToBook with 
                            | None  -> None 
                            | Some(value) -> match (value |> PriceToBook.Create) with 
                                                      | Ok(value)  -> Some(value)
                                                      | _ -> None
        let evEbitda = match rawEvEbitda with 
                            | None  -> None 
                            | Some(value) -> match (value |> EvEbitda.Create) with 
                                                      | Ok(value)  -> Some(value)
                                                      | _ -> None
        let! lastBalanceTerm = rawLastBalanceTerm |> LastBalanceTerm.Create
        let result = {
          PriceEarnings = priceEarnings
          PriceToBook = priceToBook
          EvEbitda = evEbitda
          LastBalanceTerm = lastBalanceTerm
        }
        return result
      }


module CompanyValuations =
    let Create  (rawLastClosingPrice: decimal) (rawMarketValue: decimal) (rawCapital: decimal) (currency: Currency)  : Result<CompanyValuations, ValidationError> = 
        result {
             let! lastClosingPrice = Worth.Create(rawLastClosingPrice, currency)
             let! marketValue = Worth.Create(rawMarketValue, currency)
             let! capital = Worth.Create(rawCapital, currency)
             let result = {
                LastClosingPrice = lastClosingPrice
                MarketValue = marketValue
                Capital = capital
             }
             return result
        }

module Company = 
    let Create symbol name publicOwnershipRatio financials valuations = 
        let company = {
            Symbol = symbol
            Name = name 
            PublicOwnershipRatio = publicOwnershipRatio
            FinancialRatios = financials
            Valuations = valuations
        }
        company

    let Seriliaze company = 
        JsonSerializer.Serialize company