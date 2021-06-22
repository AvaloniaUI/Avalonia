using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Markup;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using Avalonia.Data;

namespace ControlCatalog.Pages
{
    public class AutoCompleteBoxPage : UserControl
    {
        public class StateData
        {
            public string Name { get; private set; }
            public string Abbreviation { get; private set; }
            public string Capital { get; private set; }

            public StateData(string name, string abbreviatoin, string capital)
            {
                Name = name;
                Abbreviation = abbreviatoin;
                Capital = capital;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        private StateData[] BuildAllStates()
        {
            return new StateData[]
            {
                new StateData("Alabama","AL","Montgomery"),
                new StateData("Alaska","AK","Juneau"),
                new StateData("Arizona","AZ","Phoenix"),
                new StateData("Arkansas","AR","Little Rock"),
                new StateData("California","CA","Sacramento"),
                new StateData("Colorado","CO","Denver"),
                new StateData("Connecticut","CT","Hartford"),
                new StateData("Delaware","DE","Dover"),
                new StateData("Florida","FL","Tallahassee"),
                new StateData("Georgia","GA","Atlanta"),
                new StateData("Hawaii","HI","Honolulu"),
                new StateData("Idaho","ID","Boise"),
                new StateData("Illinois","IL","Springfield"),
                new StateData("Indiana","IN","Indianapolis"),
                new StateData("Iowa","IA","Des Moines"),
                new StateData("Kansas","KS","Topeka"),
                new StateData("Kentucky","KY","Frankfort"),
                new StateData("Louisiana","LA","Baton Rouge"),
                new StateData("Maine","ME","Augusta"),
                new StateData("Maryland","MD","Annapolis"),
                new StateData("Massachusetts","MA","Boston"),
                new StateData("Michigan","MI","Lansing"),
                new StateData("Minnesota","MN","St. Paul"),
                new StateData("Mississippi","MS","Jackson"),
                new StateData("Missouri","MO","Jefferson City"),
                new StateData("Montana","MT","Helena"),
                new StateData("Nebraska","NE","Lincoln"),
                new StateData("Nevada","NV","Carson City"),
                new StateData("New Hampshire","NH","Concord"),
                new StateData("New Jersey","NJ","Trenton"),
                new StateData("New Mexico","NM","Santa Fe"),
                new StateData("New York","NY","Albany"),
                new StateData("North Carolina","NC","Raleigh"),
                new StateData("North Dakota","ND","Bismarck"),
                new StateData("Ohio","OH","Columbus"),
                new StateData("Oklahoma","OK","Oklahoma City"),
                new StateData("Oregon","OR","Salem"),
                new StateData("Pennsylvania","PA","Harrisburg"),
                new StateData("Rhode Island","RI","Providence"),
                new StateData("South Carolina","SC","Columbia"),
                new StateData("South Dakota","SD","Pierre"),
                new StateData("Tennessee","TN","Nashville"),
                new StateData("Texas","TX","Austin"),
                new StateData("Utah","UT","Salt Lake City"),
                new StateData("Vermont","VT","Montpelier"),
                new StateData("Virginia","VA","Richmond"),
                new StateData("Washington","WA","Olympia"),
                new StateData("West Virginia","WV","Charleston"),
                new StateData("Wisconsin","WI","Madison"),
                new StateData("Wyoming","WY","Cheyenne"),
            };
        }
        public StateData[] States { get; private set; }
        
        private LinkedList<string>[] BuildAllSentences()
        {
            return new string[]
            {
                "Hello world",
                "No this is Patrick",
                "Never gonna give you up",
                "How does one patch KDE2 under FreeBSD"
            }
            .Select(x => new LinkedList<string>(x.Split(' ')))
            .ToArray();
        }
        public LinkedList<string>[] Sentences { get; private set; }

        public AutoCompleteBoxPage()
        {
            this.InitializeComponent();

            States = BuildAllStates();
            Sentences = BuildAllSentences();

            foreach (AutoCompleteBox box in GetAllAutoCompleteBox().Where(x => x.Name != "CustomAutocompleteBox"))
            {
                box.Items = States;
            }

            var converter = new FuncMultiValueConverter<string, string>(parts =>
            {
                return String.Format("{0} ({1})", parts.ToArray());
            });
            var binding = new MultiBinding { Converter = converter };
            binding.Bindings.Add(new Binding("Name"));
            binding.Bindings.Add(new Binding("Abbreviation"));

            var multibindingBox = this.FindControl<AutoCompleteBox>("MultiBindingBox");
            multibindingBox.ValueMemberBinding = binding;

            var asyncBox = this.FindControl<AutoCompleteBox>("AsyncBox");
            asyncBox.AsyncPopulator = PopulateAsync;

            var customAutocompleteBox = this.FindControl<AutoCompleteBox>("CustomAutocompleteBox");
            customAutocompleteBox.Items = Sentences.SelectMany(x => x);
            customAutocompleteBox.TextFilter = LastWordContains;
            customAutocompleteBox.TextSelector = AppendWord;
        }
        private IEnumerable<AutoCompleteBox> GetAllAutoCompleteBox()
        {
            return
                this.GetLogicalDescendants()
                    .OfType<AutoCompleteBox>();
        }

        private bool StringContains(string str, string query)
        {
            return str.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        private async Task<IEnumerable<object>> PopulateAsync(string searchText, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1.5), cancellationToken);

            return
                States.Where(data => StringContains(data.Name, searchText) || StringContains(data.Capital, searchText))
                      .ToList();
        }

        private bool LastWordContains(string searchText, string item)
        {
            var words = searchText.Split(' ');
            var options = Sentences.Select(x => x.First).ToArray();
            for (var i = 0; i < words.Length; ++i)
            {
                var word = words[i];
                for (var j = 0; j < options.Length; ++j)
                {
                    var option = options[j];
                    if (option == null)
                        continue;

                    if (i == words.Length - 1)
                    {
                        options[j] = option.Value.ToLower().Contains(word.ToLower()) ? option : null;
                    }
                    else
                    {
                        options[j] = option.Value.Equals(word, StringComparison.InvariantCultureIgnoreCase) ? option.Next : null;
                    }
                }
            }

            return options.Any(x => x != null && x.Value == item);
        }
        private string AppendWord(string text, string item)
        {
            string[] parts = text.Split(' ');
            if (parts.Length == 0)
                return item;

            parts[parts.Length - 1] = item;
            return string.Join(" ", parts);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
