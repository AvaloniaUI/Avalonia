namespace ControlCatalog.Models
{
    public class Country
    {
        public string Name { get; private set; }
        public string Region { get; private set; }
        public int Population { get; private set; }
        //Square Miles
        public int Area { get; private set; }
        //Per Square Mile
        public double PopulationDensity { get; private set; }
        //Coast / Area
        public double CoastLine { get; private set; }
        public double? NetMigration { get; private set; }
        //per 1000 births
        public double? InfantMortality { get; private set; }
        public int GDP { get; private set; }
        public double? LiteracyPercent { get; private set; }
        //per 1000
        public double? Phones { get; private set; }
        public double? BirthRate { get; private set; }
        public double? DeathRate { get; private set; }

        public Country(string name, string region, int population, int area, double density, double coast, double? migration, 
                       double? infantMorality, int gdp, double? literacy, double? phones, double? birth, double? death)
        {
            Name = name;
            Region = region;
            Population = population;
            Area = area;
            PopulationDensity = density;
            CoastLine = coast;
            NetMigration = migration;
            InfantMortality = infantMorality;
            GDP = gdp;
            LiteracyPercent = literacy;
            BirthRate = birth;
            DeathRate = death;
        }
    }
}
