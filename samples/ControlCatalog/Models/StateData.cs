namespace ControlCatalog.Models;

public class StateData
{
    public string Name { get; private set; }
    public string Abbreviation { get; private set; }
    public string Capital { get; private set; }

    public StateData(string name, string abbreviation, string capital)
    {
        Name = name;
        Abbreviation = abbreviation;
        Capital = capital;
    }

    public override string ToString()
    {
        return Name;
    }
}
