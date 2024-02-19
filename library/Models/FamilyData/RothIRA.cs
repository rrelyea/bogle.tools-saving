using System.Text.Json.Serialization;

public class RothIRA {
    // back pointer
    [JsonIgnore]
    public Person person { get; set; }

    private IRS.RothIRA rothIraVariables {
        get { return this.person.FamilyData.AppData.IRSData.RetirementData.RothIRA; }
    }

    public int MaximumContributionByAge { 
        get
        {
            return rothIraVariables.ContributionLimit
                + (person.FiftyOrOver ? rothIraVariables.CatchUpContributionLimit : 0);
        }
    }

    public int? ContributionAllowed {
        get { 
            if (person.FamilyData.PersonCount > person.PersonIndex && person.FamilyData.AdjustedGrossIncome != null) {
                return CalculateAllowedContribution(person.FamilyData.AdjustedGrossIncome, person.FamilyData.TaxFilingStatus, person);
            } else {
                return null;
            }
        }
    }

    public int? CalculateAllowedContribution(int? income, TaxFilingStatus taxFilingStatus, Person person)
    {
        if (rothIraVariables is not null && rothIraVariables.ContributionPhaseOutRange is not null) {
            switch (taxFilingStatus) {
                case TaxFilingStatus.Single:
                    return ApplyRange(rothIraVariables.ContributionPhaseOutRange.Single!.Start,
                                    rothIraVariables.ContributionPhaseOutRange.Single.End,
                                    person.FamilyData.AdjustedGrossIncome, MaximumContributionByAge);
                case TaxFilingStatus.MarriedFilingJointly:
                    return ApplyRange(rothIraVariables.ContributionPhaseOutRange.MarriedFilingJointly!.Start,
                                    rothIraVariables.ContributionPhaseOutRange.MarriedFilingJointly.End,
                                    person.FamilyData.AdjustedGrossIncome, MaximumContributionByAge);
                case TaxFilingStatus.MarriedFilingSeperately: 
                    if (this.person.FamilyData.TaxFilingStatusLivingSeperately) {
                        return ApplyRange(rothIraVariables.ContributionPhaseOutRange.Single!.Start,
                                    rothIraVariables.ContributionPhaseOutRange.Single.End,
                                    person.FamilyData.AdjustedGrossIncome, MaximumContributionByAge);
                    } else {
                        return ApplyRange(rothIraVariables.ContributionPhaseOutRange.MarriedFilingSeparately!.Start,
                                    rothIraVariables.ContributionPhaseOutRange.MarriedFilingSeparately.End,
                                    person.FamilyData.AdjustedGrossIncome, MaximumContributionByAge);
                    }
                case TaxFilingStatus.ChoiceNeeded:
                default:
                    return null;
            }
        }
        return null;
    }

    private int? ApplyRange(int low, int high, int? income, int? contributionAllowed) 
    {
        if (income <= low) return contributionAllowed;
        if (income >= high) return 0;
        return contributionAllowed * (high - income) / (high - low);
    }

    public int? AmountToSave {
        get {
            return ContributionAllowed;
        }
    }
}