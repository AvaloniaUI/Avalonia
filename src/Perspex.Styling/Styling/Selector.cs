





namespace Perspex.Styling
{
    using System;
    using System.Collections.Generic;

    public class Selector
    {
        private Func<IStyleable, SelectorMatch> evaluate;

        private bool inTemplate;

        private bool stopTraversal;

        private string description;

        public Selector()
        {
            this.evaluate = _ => SelectorMatch.True;
        }

        public Selector(
            Selector previous,
            Func<IStyleable, SelectorMatch> evaluate,
            string selectorString,
            bool inTemplate = false,
            bool stopTraversal = false)
            : this()
        {
            Contract.Requires<ArgumentNullException>(previous != null);

            this.Previous = previous;
            this.evaluate = evaluate;
            this.SelectorString = selectorString;
            this.inTemplate = inTemplate || previous.inTemplate;
            this.stopTraversal = stopTraversal;
        }

        public Selector Previous
        {
            get;
            private set;
        }

        public string SelectorString
        {
            get;
            set;
        }

        public Selector MovePrevious()
        {
            return this.stopTraversal ? null : this.Previous;
        }

        public SelectorMatch Match(IStyleable control)
        {
            List<IObservable<bool>> inputs = new List<IObservable<bool>>();
            Selector selector = this;

            while (selector != null)
            {
                if (selector.inTemplate && control.TemplatedParent == null)
                {
                    return SelectorMatch.False;
                }

                var match = selector.evaluate(control);

                if (match.ImmediateResult == false)
                {
                    return match;
                }
                else if (match.ObservableResult != null)
                {
                    inputs.Add(match.ObservableResult);
                }

                selector = selector.MovePrevious();
            }

            if (inputs.Count > 0)
            {
                return new SelectorMatch(new StyleActivator(inputs));
            }
            else
            {
                return SelectorMatch.True;
            }
        }

        public override string ToString()
        {
            if (this.description == null)
            {
                string result = string.Empty;

                if (this.Previous != null)
                {
                    result = this.Previous.ToString();
                }

                this.description = result + this.SelectorString;
            }

            return this.description;
        }
    }
}
