public class Person {
    public Person(TaxFiling taxFiling, int personIndex) {
        this.EmployerPlan = new EmployerPlan(this);
        this.IRA = new IRA(this);
        this.RothIRA = new RothIRA(this);
        this.PersonIndex = personIndex;
        this.TaxFiling = taxFiling;
    }

    public Person? Spouse { 
        get {
            if (TaxFiling.TaxFilingStatus == TaxFilingStatus.MarriedFilingJointly 
            || TaxFiling.TaxFilingStatus == TaxFilingStatus.MarriedFilingSeperatelyAndLivingApart 
            || TaxFiling.TaxFilingStatus == TaxFilingStatus.MarriedFilingSeperately)
            {
                switch (this.PersonIndex) {
                    case 0:
                        return TaxFiling.People[1];
                    case 1:
                        return TaxFiling.People[0];
                    default:
                        throw new InvalidDataException("PersonIndex not expected to be " + this.PersonIndex);
                }
            }
            else
            {
                return null;
            }
        }
    }

    public int PersonIndex { private set; get; }
    public TaxFiling TaxFiling { private set; get; }

    public bool FiftyOrOver { get; set; }

    public EmployerPlan EmployerPlan { get; set; }
    public HealthSavingsAccount HealthSavingsAccount { get; set; } = new();
    public IRA IRA { get; set; }
    public RothIRA RothIRA { get; set; }
}