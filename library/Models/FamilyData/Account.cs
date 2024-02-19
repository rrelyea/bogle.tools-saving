using System.Text.Json.Serialization;

public class Account 
{
    public Account() {
        Investments = new();
        AvailableFunds = new();
    }

    public Account(int? pin) : this()
    {
        PIN = pin;
    }

    private int? _PIN;

    [JsonIgnore]
    public int? PIN {
        get { return _PIN; }
        set { 
            _PIN = value; 
            foreach (var investment in Investments) {
                investment.PIN = _PIN;
            }
        }
    }

    public string Title {
        get {
            return (Identifier != null ? Identifier + " " : "") + AccountType + (!string.IsNullOrEmpty(Custodian) ? " at " + Custodian : "") + (Note != null ? $" ({Note})":"");
        }
    }

    [JsonIgnore]
    public Investment? SelectedInvestment { get; set; }
    [JsonIgnore]
    public int Owner { get; set; }
    public string? Identifier { get; set; }
    private string? _AccountType;

    public Investment? SettlementInvestment {
        get {
            foreach (var investment in this.Investments)
            {
                if (investment.AssetType == AssetType.Cash_MoneyMarket 
                    || investment.AssetType == AssetType.Cash_BankAccount 
                    || investment.AssetType == AssetType.Cash)
                {
                    return investment;
                }
            }

            return null;
        }
    }

    public string? AccountType {
        get { return _AccountType; }
        set {
            // migrate old values on 7/22/2023
            _AccountType = value switch
            {
                "401k" => "401(k)",
                "403b" => "403(b)",
                "457b" => "457(b)",
                "Roth 401k" => "Roth 401(k)",
                "Solo 401k" => "Solo 401(k)",
                _ => value,
            };
            if (Identifier == "our" && TaxType != "Taxable" && TaxType != "Other") {
                Identifier = null;
            }
        }
    }
    public string? Custodian { get; set; }
    public string? Note { get; set; }
    public double Value { 
        get {
            double newValue = 0;
            foreach (var investment in Investments) 
            {
                newValue += investment.ValuePIN ?? 0;
                foreach (var transaction in investment.Transactions)
                {
                    newValue += transaction.CustomValue(investment) ?? 0;
                }
            }

            return newValue;
        }
    }

    [JsonIgnore]
    public bool Edit { get; set; }
    [JsonIgnore]
    public bool View { get; set; }
    [JsonIgnore]
    public double Percentage { get; set; }

    public string FullName 
    {
        get {
            return (Identifier != null ? Identifier+ " " : "") + AccountType + (Custodian != null ? " at " + Custodian : "");
        }
    }

    public Investment? FindInvestment(string ticker)
    {
        foreach (var investment in Investments)
        {
            if (investment.Ticker == ticker)
            {
                return investment;
            }
        }

        return null;
    }

    public List<Investment> Investments { get; set; }

    public List<Investment> AvailableFunds { get; set; }

    [JsonIgnore]
    public Account? ReplaceAccount { get; set; }

    [JsonIgnore]
    public bool Import { get; set; }

    public double? CalculateCostBasis() {
        double costBasis = 0.0;
        int investmentsMissingCostBasis = 0;
        switch (TaxType) {
            case "Taxable":
                foreach (var investment in Investments)
                {
                    if (investment.CostBasis != null)
                    {
                        costBasis += investment.CostBasis.Value;
                    }
                    else if (investment.AssetType == AssetType.Cash
                        || investment.AssetType == AssetType.Cash_BankAccount
                        || investment.AssetType == AssetType.Cash_MoneyMarket)
                    {
                        costBasis += investment.Value ?? 0.0;
                    }
                    else
                    {
                        investmentsMissingCostBasis++;
                    }
                }
                break;
            case "Post-Tax":
            case "Pre-Tax":
                return costBasis;
        }

        if (investmentsMissingCostBasis > 0) {
            return null;
        } else {
            return costBasis;
        }
    }

    public void UpdatePercentages(double totalValue, FamilyData familyData)
    {
        foreach (var investment in Investments)
        {
            investment.Percentage = (investment.Value ?? 0) / totalValue * 100;
            
            UpdateInvestmentCategoryTotals(investment, familyData);
            foreach (var transaction in investment.Transactions)
            {
                UpdateInvestmentCategoryTotals(investment, familyData, overrideValue:transaction.CustomValue(this.FindInvestment(transaction.HostTicker)));
            }

            if (investment.ExpenseRatio.HasValue && investment.Value.HasValue) {
                familyData.OverallER += investment.Percentage / 100.0 * investment.ExpenseRatio.Value;
                familyData.ExpensesTotal += investment.Value.Value * investment.ExpenseRatio.Value / 100.0;
            } else {
                if (investment.IsETF || investment.IsFund)
                {
                    familyData.InvestmentsMissingER++;
                }
            }
        }
    }

    public void UpdateInvestmentCategoryTotals(Investment investment, FamilyData familyData, double? overrideValue = null, bool useNegative = false)
    {
        double? value = (investment.Value ?? 0.0) * (useNegative?-1:1);
        if (overrideValue != null)
        {
            value = overrideValue * (useNegative?-1:1);
        }

        if (investment.AssetType == null)
        {
            familyData.OtherBalance += value;
        } 
        else
        {
            switch (investment.AssetType) {
                case AssetType.Stock: 
                case AssetType.USStock:
                case AssetType.USStock_ETF:
                case AssetType.USStock_Fund:
                    familyData.StockBalance += value;
                    break;
                case AssetType.InternationalStock:
                case AssetType.InternationalStock_ETF:
                case AssetType.InternationalStock_Fund:
                    familyData.InternationalStockBalance += value;
                    break;
                case AssetType.Bond:
                case AssetType.Bond_ETF:
                case AssetType.Bond_Fund:
                case AssetType.IBond:
                case AssetType.InternationalBond:
                case AssetType.InternationalBond_ETF:
                case AssetType.InternationalBond_Fund:
                    familyData.BondBalance += value;
                    break;
                case AssetType.Cash:
                case AssetType.Cash_BankAccount:
                case AssetType.Cash_MoneyMarket:
                    familyData.CashBalance += value;
                    break;
                case AssetType.StocksAndBonds_ETF:
                case AssetType.StocksAndBonds_Fund:
                    familyData.StockBalance += value * investment.GetPercentage(AssetType.USStock);
                    familyData.InternationalStockBalance += value * investment.GetPercentage(AssetType.InternationalStock);
                    familyData.BondBalance += value * investment.GetPercentage(AssetType.Bond);
                    familyData.BondBalance += value * investment.GetPercentage(AssetType.InternationalBond);
                    familyData.CashBalance += value * investment.GetPercentage(AssetType.Cash);
                    break;
                case AssetType.Unknown:
                    familyData.OtherBalance += value;
                    break;
                default:
                    throw new InvalidDataException("unexpected case");
            }
        }
    }

    public void GuessAccountType() 
    {
        if (Note is not null) {
            if (Note.Contains("401K") || Note.Contains("401k"))
            {
                AccountType = "401(k)";
            }
            else if (Note.Contains("HSA") || Note.Contains("Health Savings Account"))
            {
                AccountType = "HSA";
            }
        }
    }

    public string TaxType {
        get {
            return AccountType switch
            {
                "401(k)" or "403(b)" or "457(b)" or "457(b) Governmental" or "SEP IRA" or "Solo 401(k)" or "SIMPLE IRA" => "Pre-Tax(work)",
                "Annuity (Qualified)" or "Inherited IRA" or "Traditional IRA" or "Rollover IRA" => "Pre-Tax(other)",
                "Inherited Roth IRA" or "Roth 401(k)" or "Roth IRA" or "HSA" => "Post-Tax",
                "Annuity (Non-Qualified)" or "Brokerage" or "Individual" or "Taxable" => "Taxable",
                "Refundable Deposit" => "Refundable Deposits",
                "Life Insurance" => "For Beneficiaries (POD)",
                "529" => "Education Savings",
                _ => "Other",
            };
        }
    }

    public double? AfterTaxPercentage { get; set; }

    public string TaxType2 { 
        get {
            return TaxType.StartsWith("Pre-Tax") ? "Pre-Tax" : TaxType;
        }
    }

    public int PortfolioReviewOrder {
        get {
            var ownerCategory = (Owner + 1) * 7; // 7=Joint, 14=First Person, 21= Second Person
            int order = AccountType switch
            {
                "Annuity (Non-Qualified)" or "Brokerage" or "Individual" or "Taxable" => 1 + Owner,
                "401(k)" or "403(b)" or "457(b)" or "457(b) Governmental" or "Roth 401(k)" or "SIMPLE IRA" or "Solo 401(k)" or "SEP IRA" => 3 + ownerCategory,
                "Annuity (Qualified)" or "Inherited IRA" or "Traditional IRA" or "Rollover IRA" => 4 + ownerCategory,
                "Inherited Roth IRA" or "Roth IRA" => 5 + ownerCategory,
                "HSA" => 6 + ownerCategory,
                _ => 7 + ownerCategory,
            };

            if (TaxType == "Pre-Tax(work)" && CurrentOrPrevious == "current") {
                order--;
            }

            return order;
        }
    }

    [JsonIgnore] // on 2/9/2024 - moved from bool to string.
    public bool CurrentEmployerRetirementFund {
        get {
            return false;
        }
        set {
            //transition value to new property.
            if (value)
            {
                CurrentOrPrevious = "current";
            }
            else
            {
                CurrentOrPrevious = "previous";
            }
        }
    }

    public string CurrentEmployerString {
        get {
            if (CurrentOrPrevious == "n/a" || CurrentOrPrevious == "") return "";
            else return CurrentOrPrevious;
        }
    }

    public string CurrentOrPrevious { get; set; } = "";
    public static List<string> CurrentOrPreviousOptions = new List<string> { "current", "previous" };
}