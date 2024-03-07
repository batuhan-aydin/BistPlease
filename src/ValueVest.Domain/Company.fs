namespace ValueVest.Domain

open ValueVest.Domain
open FsToolkit.ErrorHandling


/// We will add more data into this, for now keeping it simple
type CompanyFinancials = {
    LastTermProfit: FinancialsByTerm
    OperationProfit: FinancialsByTerm 
}

type CompanyFinancialRatios = {
    PriceEarnings: PriceEarnings
    PriceToBook: PriceToBook
    EvEbitda: EvEbitda
    EvSales: EvSales
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
    CompanyValuationsTry: CompanyValuations
    CompanyValuationsUsd: CompanyValuations
}

type CompanyCreateRequest = {
    RawSymbol: string
    RawName: string
    RawPublicOwnershipRatio: float32
    RawPriceEarnings: float32
    RawPriceToBook: float32
    RawEvEbitda: float32
    RawEvSales: float32
    RawLastBalanceTerm: string
    UsdTryExhangeRate: decimal
    RawLastClosingPrice: decimal
    RawMarketValue: decimal
    RawCapital: decimal
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
          PriceEarnings = priceEarnings
          PriceToBook = priceToBook
          EvEbitda = evEbitda
          EvSales = evSales
          LastBalanceTerm = lastBalanceTerm
        }
        return result
      }


module CompanyValuations =
    let Create  (rawLastClosingPrice: decimal) (rawMarketValue: decimal) (rawCapital: decimal) (exchangeRate: decimal) (currency: Currency)  : Result<CompanyValuations, ValidationError> = 
        result {
             let! lastClosingPrice = Worth.Create(rawLastClosingPrice / exchangeRate, currency)
             let! marketValue = Worth.Create(rawMarketValue / exchangeRate, currency)
             let! capital = Worth.Create(rawCapital / exchangeRate, currency)
             let result = {
                LastClosingPrice = lastClosingPrice
                MarketValue = marketValue
                Capital = capital
             }
             return result
        }

module Company = 
    let Create (request: CompanyCreateRequest) : Result<Company, ValidationError> =
        result { 
            let! symbol = request.RawSymbol |> Symbol.Create 
            let! name = request.RawName |> Name.Create 
            let! publicOwnershipRatio = request.RawPublicOwnershipRatio |> PublicOwnershipRatio.Create
            let! companyFinancialRatios = CompanyFinancialRatios.Create request.RawPriceEarnings request.RawPriceToBook request.RawEvEbitda request.RawEvSales request.RawLastBalanceTerm
            let! companyValuationsTry = CompanyValuations.Create request.RawLastClosingPrice request.RawMarketValue request.RawCapital 1.0M Currency.TRY 
            let! companyValuationsUsd = CompanyValuations.Create request.RawLastClosingPrice request.RawMarketValue request.RawCapital request.UsdTryExhangeRate Currency.TRY 
            let company = { 
                Symbol = symbol
                Name = name
                PublicOwnershipRatio = publicOwnershipRatio
                FinancialRatios = companyFinancialRatios
                CompanyValuationsTry = companyValuationsTry
                CompanyValuationsUsd = companyValuationsUsd
            }
            return company
        } 
