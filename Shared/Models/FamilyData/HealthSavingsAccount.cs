using System.ComponentModel.DataAnnotations;
public class HealthSavingsAccount {
    public HealthSavingsAccount(Person person)
    {
        this.person = person;
    }

    public IRS.HSA HSAVariables {
        get {
            return this.person.FamilyYears.RetirementData.HSA;
        }
    }

    private Person person;
    public TriState Eligible { get; set; }
    public bool? NotEligible {
        get {
            switch (Eligible) {
                case TriState.ChoiceNeeded:
                case TriState.False:
                    return true;
                case TriState.True:
                default:
                    return false;
            }
        }
    }
    public EmployeeFamily? Family { get; set; }
    private string? _EmployerContributionString; 
    public string? EmployerContributionString { 
        get {
            return _EmployerContributionString;
        }
        set {
            _EmployerContributionString = value;
            if (_EmployerContributionString != null) {
                EmployerContribution = int.Parse(_EmployerContributionString);
            }
        }
     }
    public int? EmployerContribution { get; set; }

    public int? AmountToSave { get { return Limit - (EmployerContribution ?? 0); } }
    
    public int? Limit { 
        get {
            if (Eligible == TriState.False && person.OtherPerson != null && person.OtherPerson.HealthSavingsAccount.NotEligible.Value) return null;
            
            int? contributionLimit = null;

            switch (Family)
            {
                case EmployeeFamily.Family:
                    contributionLimit = HSAVariables.ContributionLimit.Family;
                    break;
                case EmployeeFamily.EmployeeOnly:
                    contributionLimit = HSAVariables.ContributionLimit.SelfOnly;
                    break;
                default:
                    break;
            }

            if (person.FiftyFiveOrOver) {
                if (contributionLimit == null) {
                    contributionLimit = HSAVariables.ContributionLimit.CatchUp;
                } else {
                    contributionLimit += HSAVariables.ContributionLimit.CatchUp;
                }
            }

            return contributionLimit;
        }
    }
}