namespace BistPlease.Domain

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

/// Possibly we can add more data into this, for now keeping it simple
type CompanyFinancials = {
    LastTermProfit: FinancialsByTerm
    OperationProfit: FinancialsByTerm 
}

type Company = {
    Symbol: Symbol
    Name: Name
    LastClosingPrice: Worth
    MarketValue: Worth
    PublicOwnershipRatio: PublicOwnershipRatio
    Capital: Worth
    PriceEarnings: PriceEarnings
    PriceToBook: PriceToBook
    //LastBalanceTerm: LastBalanceTerm
    //Financials: CompanyFinancials
}

type Sector = {
    Id: SectorId
    Name: Name
    Average_PE : PriceEarnings
    Avegage_PB : PriceToBook
    Companies: Company[]
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

module CompanyFinancials = 
    let Create lastTermProfit operationProfit : CompanyFinancials =
            let financials = { LastTermProfit = lastTermProfit; 
                OperationProfit = operationProfit }
            financials

module Company = 
    let Create rawSymbol rawName (rawLastClosingPrice : decimal) (rawMarketValue : decimal) rawPublicOwnershipRatio (rawCapital : decimal) rawPriceEarnings rawPriceToBook (currency : Currency) : Result<Company, ValidationError> =
        result {
            let! symbol = rawSymbol |> Symbol.Create 
            let! name = rawName |> Name.Create 
            let! lastClosingPrice = Worth.Create(rawLastClosingPrice, currency)
            let! marketValue = Worth.Create(rawMarketValue, currency)
            let! publicOwnershipRatio = rawPublicOwnershipRatio |> PublicOwnershipRatio.Create
            let! capital = Worth.Create(rawCapital, currency)
            let! priceEarnings = rawPriceEarnings |> PriceEarnings.Create 
            let! priceToBook = rawPriceToBook |> PriceToBook.Create
            //let! lastBalanceTerm = rawLastBalanceTerm |> LastBalanceTerm.Create
            let company = { Symbol = symbol; Name = name; LastClosingPrice = lastClosingPrice;
            MarketValue = marketValue; PublicOwnershipRatio = publicOwnershipRatio; 
            Capital = capital; PriceEarnings = priceEarnings; PriceToBook = priceToBook; 
            //LastBalanceTerm = lastBalanceTerm;
            //Financials = companyFinancials 
            }
            return company
        } 

    let ValidateCompany symbolResult nameResult lastClosingPriceResult = 
        symbolResult
        |> Result.bind nameResult
        |> Result.bind lastClosingPriceResult
        

type SectorList = private SectorList of list<Sector>

module SectorList = 
    let public InitSectorList() =
        SectorList(List.empty)

    let GetInnerSectorArray(SectorList sectors) = sectors

    let AddSector (sector: Sector, sectorList: SectorList) =
        let sectors = GetInnerSectorArray sectorList
        let newSectors = sector :: sectors
        SectorList(newSectors)