namespace ValueVest.Domain

open ValueVest.Domain
open FsToolkit.ErrorHandling


/// We will add more data into this, for now keeping it simple
type CompanyFinancials = {
    LastTermProfit: FinancialsByTerm
    OperationProfit: FinancialsByTerm 
}

type CompanyFinancialRatios = {
    PriceEarnings: PriceEarnings option
    PriceToBook: PriceToBook  option
    EvEbitda: EvEbitda option
    EvSales: EvSales option
    LastBalanceTerm: LastBalanceTerm
}

type CompanyValuations = {
    LastClosingPrice: Worth
    MarketValue: Worth
    Capital: Worth
}

type Company = {
    Symbol: Symbol
    Name: Name
    PublicOwnershipRatio: PublicOwnershipRatio
    FinancialRatios: CompanyFinancialRatios
    CompanyValuations: CompanyValuations
}

module CompanyFinancials = 
    let Create lastTermProfit operationProfit : CompanyFinancials =
            let financials = { LastTermProfit = lastTermProfit; 
                OperationProfit = operationProfit }
            financials

module CompanyFinancialRatios =
    let Create (rawPriceEarnings: float32) (rawPriceToBook: float32) (rawEvEbitda: float32) (rawEvSales: float32) (rawLastBalanceTerm: string)  : Result<CompanyFinancialRatios, ValidationError> =
      result {
        let! priceEarnings = rawPriceEarnings |> PriceEarnings.Create
        let! priceToBook = rawPriceToBook |> PriceToBook.Create
        let! evEbitda = rawEvEbitda |> EvEbitda.Create
        let! evSales = rawEvSales |> EvSales.Create
        let! lastBalanceTerm = rawLastBalanceTerm |> LastBalanceTerm.Create
        let result = {
          PriceEarnings = Some priceEarnings
          PriceToBook = Some priceToBook
          EvEbitda = Some evEbitda
          EvSales = Some evSales
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