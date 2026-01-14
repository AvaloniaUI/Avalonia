using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;
using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using Avalonia.Headless;
using Avalonia.Harfbuzz;
using Avalonia.Input;
using Avalonia.Platform;
using Moq;

namespace Avalonia.Controls.UnitTests
{
    public class AutoCompleteBoxTests : ScopedTestBase
    {
        [Fact]
        public void Search_Filters()
        {
            Assert.True(GetNotNullFilter(AutoCompleteFilterMode.Contains)("am", "name"));
            Assert.True(GetNotNullFilter(AutoCompleteFilterMode.Contains)("AME", "name"));
            Assert.False(GetNotNullFilter(AutoCompleteFilterMode.Contains)("hello", "name"));

            Assert.True(GetNotNullFilter(AutoCompleteFilterMode.ContainsCaseSensitive)("na", "name"));
            Assert.False(GetNotNullFilter(AutoCompleteFilterMode.ContainsCaseSensitive)("AME", "name"));
            Assert.False(GetNotNullFilter(AutoCompleteFilterMode.ContainsCaseSensitive)("hello", "name"));

            Assert.Null(GetFilter(AutoCompleteFilterMode.Custom));
            Assert.Null(GetFilter(AutoCompleteFilterMode.None));

            Assert.True(GetNotNullFilter(AutoCompleteFilterMode.Equals)("na", "na"));
            Assert.True(GetNotNullFilter(AutoCompleteFilterMode.Equals)("na", "NA"));
            Assert.False(GetNotNullFilter(AutoCompleteFilterMode.Equals)("hello", "name"));

            Assert.True(GetNotNullFilter(AutoCompleteFilterMode.EqualsCaseSensitive)("na", "na"));
            Assert.False(GetNotNullFilter(AutoCompleteFilterMode.EqualsCaseSensitive)("na", "NA"));
            Assert.False(GetNotNullFilter(AutoCompleteFilterMode.EqualsCaseSensitive)("hello", "name"));

            Assert.True(GetNotNullFilter(AutoCompleteFilterMode.StartsWith)("na", "name"));
            Assert.True(GetNotNullFilter(AutoCompleteFilterMode.StartsWith)("NAM", "name"));
            Assert.False(GetNotNullFilter(AutoCompleteFilterMode.StartsWith)("hello", "name"));

            Assert.True(GetNotNullFilter(AutoCompleteFilterMode.StartsWithCaseSensitive)("na", "name"));
            Assert.False(GetNotNullFilter(AutoCompleteFilterMode.StartsWithCaseSensitive)("NAM", "name"));
            Assert.False(GetNotNullFilter(AutoCompleteFilterMode.StartsWithCaseSensitive)("hello", "name"));
        }

        [Fact]
        public void Ordinal_Search_Filters()
        {
            Assert.True(GetNotNullFilter(AutoCompleteFilterMode.ContainsOrdinal)("am", "name"));
            Assert.True(GetNotNullFilter(AutoCompleteFilterMode.ContainsOrdinal)("AME", "name"));
            Assert.False(GetNotNullFilter(AutoCompleteFilterMode.ContainsOrdinal)("hello", "name"));

            Assert.True(GetNotNullFilter(AutoCompleteFilterMode.ContainsOrdinalCaseSensitive)("na", "name"));
            Assert.False(GetNotNullFilter(AutoCompleteFilterMode.ContainsOrdinalCaseSensitive)("AME", "name"));
            Assert.False(GetNotNullFilter(AutoCompleteFilterMode.ContainsOrdinalCaseSensitive)("hello", "name"));

            Assert.True(GetNotNullFilter(AutoCompleteFilterMode.EqualsOrdinal)("na", "na"));
            Assert.True(GetNotNullFilter(AutoCompleteFilterMode.EqualsOrdinal)("na", "NA"));
            Assert.False(GetNotNullFilter(AutoCompleteFilterMode.EqualsOrdinal)("hello", "name"));

            Assert.True(GetNotNullFilter(AutoCompleteFilterMode.EqualsOrdinalCaseSensitive)("na", "na"));
            Assert.False(GetNotNullFilter(AutoCompleteFilterMode.EqualsOrdinalCaseSensitive)("na", "NA"));
            Assert.False(GetNotNullFilter(AutoCompleteFilterMode.EqualsOrdinalCaseSensitive)("hello", "name"));

            Assert.True(GetNotNullFilter(AutoCompleteFilterMode.StartsWithOrdinal)("na", "name"));
            Assert.True(GetNotNullFilter(AutoCompleteFilterMode.StartsWithOrdinal)("NAM", "name"));
            Assert.False(GetNotNullFilter(AutoCompleteFilterMode.StartsWithOrdinal)("hello", "name"));

            Assert.True(GetNotNullFilter(AutoCompleteFilterMode.StartsWithOrdinalCaseSensitive)("na", "name"));
            Assert.False(GetNotNullFilter(AutoCompleteFilterMode.StartsWithOrdinalCaseSensitive)("NAM", "name"));
            Assert.False(GetNotNullFilter(AutoCompleteFilterMode.StartsWithOrdinalCaseSensitive)("hello", "name"));
        }

        [Fact]
        public void Fires_DropDown_Events()
        {
            RunTest((control, textbox) =>
            {
                bool openEvent = false;
                bool closeEvent = false;
                control.DropDownOpened += (s, e) => openEvent = true;
                control.DropDownClosed += (s, e) => closeEvent = true;
                control.ItemsSource = CreateSimpleStringArray();

                textbox.Text = "a";
                Dispatcher.UIThread.RunJobs();
                Assert.True(control.SearchText == "a");
                Assert.True(control.IsDropDownOpen);
                Assert.True(openEvent);

                textbox.Text = String.Empty;
                Dispatcher.UIThread.RunJobs();
                Assert.True(control.SearchText == String.Empty);
                Assert.False(control.IsDropDownOpen);
                Assert.True(closeEvent);
            });
        }

        [Fact]
        public void Custom_FilterMode_Without_ItemFilter_Setting_Throws_Exception()
        {
            RunTest((control, textbox) =>
            {
                control.FilterMode = AutoCompleteFilterMode.Custom;
                Assert.Throws<Exception>(() => { control.Text = "a"; });
            });
        }

        [Fact]
        public void Text_Completion_Via_Text_Property()
        {
            RunTest((control, textbox) =>
            {
                control.IsTextCompletionEnabled = true;

                Assert.Equal(String.Empty, control.Text);
                control.Text = "close";
                Assert.NotNull(control.SelectedItem);
            });
        }

        [Fact]
        public void Text_Completion_Selects_Text()
        {
            RunTest((control, textbox) =>
            {
                control.IsTextCompletionEnabled = true;

                textbox.Text = "ac";
                textbox.SelectionEnd = textbox.SelectionStart = 2;
                Dispatcher.UIThread.RunJobs();

                Assert.True(control.IsDropDownOpen);
                Assert.True(Math.Abs(textbox.SelectionEnd - textbox.SelectionStart) > 2);
            });
        }

        [Fact]
        public void TextChanged_Event_Fires()
        {
            RunTest((control, textbox) =>
            {
                bool textChanged = false;
                control.TextChanged += (s, e) => textChanged = true;

                textbox.Text = "a";
                Dispatcher.UIThread.RunJobs();
                Assert.True(textChanged);

                textChanged = false;
                control.Text = "conversati";
                Dispatcher.UIThread.RunJobs();
                Assert.True(textChanged);

                textChanged = false;
                control.Text = null;
                Dispatcher.UIThread.RunJobs();
                Assert.True(textChanged);
            });
        }

        [Fact]
        public void MinimumPrefixLength_Works()
        {
            RunTest((control, textbox) =>
            {
                textbox.Text = "a";
                Dispatcher.UIThread.RunJobs();
                Assert.True(control.IsDropDownOpen);


                textbox.Text = String.Empty;
                Dispatcher.UIThread.RunJobs();
                Assert.False(control.IsDropDownOpen);

                control.MinimumPrefixLength = 3;

                textbox.Text = "a";
                Dispatcher.UIThread.RunJobs();
                Assert.False(control.IsDropDownOpen);

                textbox.Text = "acc";
                Dispatcher.UIThread.RunJobs();
                Assert.True(control.IsDropDownOpen);
            });
        }

        [Fact]
        public void Can_Cancel_DropDown_Opening()
        {
            RunTest((control, textbox) =>
            {
                control.DropDownOpening += (s, e) => e.Cancel = true;

                textbox.Text = "a";
                Dispatcher.UIThread.RunJobs();
                Assert.False(control.IsDropDownOpen);
            });
        }

        [Fact]
        public void Can_Cancel_DropDown_Closing()
        {
            RunTest((control, textbox) =>
            {
                control.DropDownClosing += (s, e) => e.Cancel = true;

                textbox.Text = "a";
                Dispatcher.UIThread.RunJobs();
                Assert.True(control.IsDropDownOpen);

                control.IsDropDownOpen = false;
                Assert.True(control.IsDropDownOpen);
            });
        }

        [Fact]
        public void Can_Cancel_Population()
        {
            RunTest((control, textbox) =>
            {
                bool populating = false;
                bool populated = false;
                control.FilterMode = AutoCompleteFilterMode.None;
                control.Populating += (s, e) =>
                {
                    e.Cancel = true;
                    populating = true;
                };
                control.Populated += (s, e) => populated = true;

                textbox.Text = "accounti";
                Dispatcher.UIThread.RunJobs();

                Assert.True(populating);
                Assert.False(populated);
            });
        }

        [Fact]
        public void Custom_Population_Supported()
        {
            RunTest((control, textbox) =>
            {
                string custom = "Custom!";
                string search = "accounti";
                bool populated = false;
                bool populatedOk = false;
                control.FilterMode = AutoCompleteFilterMode.None;
                control.Populating += (s, e) =>
                {
                    control.ItemsSource = new string[] { custom };
                    Assert.Equal(search, e.Parameter);
                };
                control.Populated += (s, e) =>
                {
                    populated = true;
                    var collection = e.Data as ReadOnlyCollection<object>;
                    populatedOk = collection != null && collection.Count == 1;
                };

                textbox.Text = search;
                Dispatcher.UIThread.RunJobs();

                Assert.True(populated);
                Assert.True(populatedOk);
            });
        }

        [Fact]
        public void Text_Completion()
        {
            RunTest((control, textbox) =>
            {
                control.IsTextCompletionEnabled = true;
                textbox.Text = "accounti";
                textbox.SelectionStart = textbox.SelectionEnd = textbox.Text.Length;
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("accounti", control.SearchText);
                Assert.Equal("accounting", textbox.Text);
            });
        }

        [Fact]
        public void String_Search()
        {
            RunTest((control, textbox) =>
            {
                textbox.Text = "a";
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(textbox.Text, control.Text);

                textbox.Text = "acc";
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(textbox.Text, control.Text);

                textbox.Text = "a";
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(textbox.Text, control.Text);

                textbox.Text = "";
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(textbox.Text, control.Text);

                textbox.Text = "cook";
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(textbox.Text, control.Text);

                textbox.Text = "accept";
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(textbox.Text, control.Text);

                textbox.Text = "cook";
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(textbox.Text, control.Text);
            });
        }

        [Fact]
        public void Item_Search()
        {
            RunTest((control, textbox) =>
            {
                control.FilterMode = AutoCompleteFilterMode.Custom;
                control.ItemFilter = (_, item) => item is string;

                // Just set to null briefly to exercise that code path
                var filter = control.ItemFilter;
                Assert.NotNull(filter);
                control.ItemFilter = null;
                Assert.Null(control.ItemFilter);
                control.ItemFilter = filter;
                Assert.NotNull(control.ItemFilter);

                textbox.Text = "a";
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(textbox.Text, control.Text);

                textbox.Text = "acc";
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(textbox.Text, control.Text);

                textbox.Text = "a";
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(textbox.Text, control.Text);

                textbox.Text = "";
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(textbox.Text, control.Text);

                textbox.Text = "cook";
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(textbox.Text, control.Text);

                textbox.Text = "accept";
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(textbox.Text, control.Text);

                textbox.Text = "cook";
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(textbox.Text, control.Text);
            });
        }

        [Fact]
        public void Custom_TextSelector()
        {
            RunTest((control, textbox) =>
            {
                Assert.NotNull(control.ItemsSource);

                object selectedItem = control.ItemsSource.Cast<object>().First();
                string input = "42";

                control.TextSelector = (text, item) => text + item;
                Assert.Equal(control.TextSelector("4", "2"), "42");

                control.Text = input;
                control.SelectedItem = selectedItem;
                Assert.Equal(control.Text, control.TextSelector(input, selectedItem.ToString()));
            });
        }

        [Fact]
        public void Custom_ItemSelector()
        {
            RunTest((control, textbox) =>
            {
                Assert.NotNull(control.ItemsSource);

                object selectedItem = control.ItemsSource.Cast<object>().First();
                string input = "42";

                control.ItemSelector = (text, item) => text + item;
                Assert.Equal(control.ItemSelector("4", 2), "42");

                control.Text = input;
                control.SelectedItem = selectedItem;
                Assert.Equal(control.Text, control.ItemSelector(input, selectedItem));
            });
        }
        
        [Fact]
        public void Text_Validation()
        {
            RunTest((control, textbox) =>
            {
                var exception = new InvalidCastException("failed validation");
                var textObservable = new BehaviorSubject<BindingNotification>(new BindingNotification(exception, BindingErrorType.DataValidationError));
                control.Bind(AutoCompleteBox.TextProperty, textObservable);
                Dispatcher.UIThread.RunJobs();

                Assert.True(DataValidationErrors.GetHasErrors(control));
                Assert.Equal([exception], DataValidationErrors.GetErrors(control));
            });
        }
        
        [Fact]
        public void Text_Validation_TextBox_Errors_Binding()
        {
            RunTest((control, textbox) =>
            {
                // simulate the TemplateBinding that would be used within the AutoCompleteBox control theme for the inner PART_TextBox
                //      DataValidationErrors.Errors="{TemplateBinding (DataValidationErrors.Errors)}"
                textbox.Bind(DataValidationErrors.ErrorsProperty, control.GetBindingObservable(DataValidationErrors.ErrorsProperty));
                
                var exception = new InvalidCastException("failed validation");
                var textObservable = new BehaviorSubject<BindingNotification>(new BindingNotification(exception, BindingErrorType.DataValidationError));
                control.Bind(AutoCompleteBox.TextProperty, textObservable);
                Dispatcher.UIThread.RunJobs();
                
                Assert.True(DataValidationErrors.GetHasErrors(control));
                Assert.Equal([exception], DataValidationErrors.GetErrors(control));
                
                Assert.True(DataValidationErrors.GetHasErrors(textbox));
                Assert.Equal([exception], DataValidationErrors.GetErrors(textbox));
            });
        }
        
        [Fact]
        public void SelectedItem_Validation()
        {
            RunTest((control, textbox) =>
            {
                var exception = new InvalidCastException("failed validation");
                var itemObservable = new BehaviorSubject<BindingNotification>(new BindingNotification(exception, BindingErrorType.DataValidationError));
                control.Bind(AutoCompleteBox.SelectedItemProperty, itemObservable);
                Dispatcher.UIThread.RunJobs();

                Assert.True(DataValidationErrors.GetHasErrors(control));
                Assert.Equal([exception], DataValidationErrors.GetErrors(control));
            });
        }

        [Fact]
        public void Explicit_Dropdown_Open_Request_MinimumPrefixLength_0()
        {
            RunTest((control, textbox) =>
            {
                control.Text = "";
                control.MinimumPrefixLength = 0;
                Dispatcher.UIThread.RunJobs();

                Assert.False(control.IsDropDownOpen);

                control.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.Down
                });

                Dispatcher.UIThread.RunJobs();

                Assert.True(control.IsDropDownOpen);
            });
        }

        [Fact]
        public void CaretIndex_Changes()
        {
            string text = "Sample text";
            string expectedText = "Saple text";
            RunTest((control, textbox) =>
            {
                control.Text = text;
                control.Measure(Size.Infinity);
                Dispatcher.UIThread.RunJobs();

                textbox.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.Right
                });
                Dispatcher.UIThread.RunJobs();

                Assert.Equal(1, control.CaretIndex);
                Assert.Equal(textbox.CaretIndex, control.CaretIndex);

                control.CaretIndex = 3;

                Assert.Equal(3, control.CaretIndex);
                Assert.Equal(textbox.CaretIndex, control.CaretIndex);

                textbox.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.Back
                });
                Dispatcher.UIThread.RunJobs();

                Assert.Equal(2, control.CaretIndex);
                Assert.Equal(textbox.CaretIndex, control.CaretIndex);
                Assert.True(control.Text == expectedText && textbox.Text == expectedText);
            });
        }

        [Fact]
        public void Attempting_To_Open_Without_Items_Does_Not_Prevent_Future_Opening_With_Items()
        {
            RunTest((control, textbox) =>
            {
                // Allow the drop down to open without anything entered.
                control.MinimumPrefixLength = 0;

                // Clear the items.
                var source = control.ItemsSource;
                control.ItemsSource = null;
                control.IsDropDownOpen = true;

                // DropDown was not actually opened because there are no items.
                Assert.False(control.IsDropDownOpen);

                // Set the items and try to open the drop down again.
                control.ItemsSource = source;
                control.IsDropDownOpen = true;

                // DropDown can now be opened.
                Assert.True(control.IsDropDownOpen);
            });
        }

        [Fact]
        public void Opening_Context_Menu_Does_not_Lose_Selection()
        {
            using (UnitTestApplication.Start(FocusServices))
            {
                var target1 = CreateControl();
                target1.ContextMenu = new TestContextMenu();
                var textBox1 = GetTextBox(target1);
                textBox1.Text = "1234";

                var target2 = CreateControl();
                var textBox2 = GetTextBox(target2);
                textBox2.Text = "5678";

                var sp = new StackPanel();
                sp.Children.Add(target1);
                sp.Children.Add(target2);

                target1.ApplyTemplate();
                target2.ApplyTemplate();

                var root = new TestRoot() { Child = sp };

                textBox1.SelectionStart = 0;
                textBox1.SelectionEnd = 3;

                target1.Focus();
                Assert.False(target2.IsFocused);
                Assert.True(target1.IsFocused);

                target2.Focus();

                Assert.Equal("123", textBox1.SelectedText);
            }
        }

        /// <summary>
        /// Retrieves a defined predicate filter through a new AutoCompleteBox 
        /// control instance.
        /// </summary>
        /// <param name="mode">The FilterMode of interest.</param>
        /// <returns>Returns the predicate instance.</returns>
        private static AutoCompleteFilterPredicate<string?>? GetFilter(AutoCompleteFilterMode mode)
        {
            return new AutoCompleteBox { FilterMode = mode }
                .TextFilter;
        }

        private static AutoCompleteFilterPredicate<string?> GetNotNullFilter(AutoCompleteFilterMode mode)
        {
            var filter = GetFilter(mode);
            Assert.NotNull(filter);
            return filter;
        }

        /// <summary>
        /// Creates a large list of strings for AutoCompleteBox testing.
        /// </summary>
        /// <returns>Returns a new List of string values.</returns>
        private static IList<string> CreateSimpleStringArray()
        {
            return new List<string>
            {
            "a",
            "abide",
            "able",
            "about",
            "above",
            "absence",
            "absurd",
            "accept",
            "acceptance",
            "accepted",
            "accepting",
            "access",
            "accessed",
            "accessible",
            "accident",
            "accidentally",
            "accordance",
            "account",
            "accounting",
            "accounts",
            "accusation",
            "accustomed",
            "ache",
            "across",
            "act",
            "active",
            "actual",
            "actually",
            "ada",
            "added",
            "adding",
            "addition",
            "additional",
            "additions",
            "address",
            "addressed",
            "addresses",
            "addressing",
            "adjourn",
            "adoption",
            "advance",
            "advantage",
            "adventures",
            "advice",
            "advisable",
            "advise",
            "affair",
            "affectionately",
            "afford",
            "afore",
            "afraid",
            "after",
            "afterwards",
            "again",
            "against",
            "age",
            "aged",
            "agent",
            "ago",
            "agony",
            "agree",
            "agreed",
            "agreement",
            "ah",
            "ahem",
            "air",
            "airs",
            "ak",
            "alarm",
            "alarmed",
            "alas",
            "alice",
            "alive",
            "all",
            "allow",
            "almost",
            "alone",
            "along",
            "aloud",
            "already",
            "also",
            "alteration",
            "altered",
            "alternate",
            "alternately",
            "altogether",
            "always",
            "am",
            "ambition",
            "among",
            "an",
            "ancient",
            "and",
            "anger",
            "angrily",
            "angry",
            "animal",
            "animals",
            "ann",
            "annoy",
            "annoyed",
            "another",
            "answer",
            "answered",
            "answers",
            "antipathies",
            "anxious",
            "anxiously",
            "any",
            "anyone",
            "anything",
            "anywhere",
            "appealed",
            "appear",
            "appearance",
            "appeared",
            "appearing",
            "appears",
            "applause",
            "apple",
            "apples",
            "applicable",
            "apply",
            "approach",
            "arch",
            "archbishop",
            "arches",
            "archive",
            "are",
            "argue",
            "argued",
            "argument",
            "arguments",
            "arise",
            "arithmetic",
            "arm",
            "arms",
            "around",
            "arranged",
            "array",
            "arrived",
            "arrow",
            "arrum",
            "as",
            "ascii",
            "ashamed",
            "ask",
            "askance",
            "asked",
            "asking",
            "asleep",
            "assembled",
            "assistance",
            "associated",
            "at",
            "ate",
            "atheling",
            "atom",
            "attached",
            "attempt",
            "attempted",
            "attempts",
            "attended",
            "attending",
            "attends",
            "audibly",
            "australia",
            "author",
            "authority",
            "available",
            "avoid",
            "away",
            "awfully",
            "axes",
            "axis",
            "b",
            "baby",
            "back",
            "backs",
            "bad",
            "bag",
            "baked",
            "balanced",
            "bank",
            "banks",
            "banquet",
            "bark",
            "barking",
            "barley",
            "barrowful",
            "based",
            "bat",
            "bathing",
            "bats",
            "bawled",
            "be",
            "beak",
            "bear",
            "beast",
            "beasts",
            "beat",
            "beating",
            "beau",
            "beauti",
            "beautiful",
            "beautifully",
            "beautify",
            "became",
            "because",
            "become",
            "becoming",
            "bed",
            "beds",
            "bee",
            "been",
            "before",
            "beg",
            "began",
            "begged",
            "begin",
            "beginning",
            "begins",
            "begun",
            "behead",
            "beheaded",
            "beheading",
            "behind",
            "being",
            "believe",
            "believed",
            "bells",
            "belong",
            "belongs",
            "beloved",
            "below",
            "belt",
            "bend",
            "bent",
            "besides",
            "best",
            "better",
            "between",
            "bill",
            "binary",
            "bird",
            "birds",
            "birthday",
            "bit",
            "bite",
            "bitter",
            "blacking",
            "blades",
            "blame",
            "blasts",
            "bleeds",
            "blew",
            "blow",
            "blown",
            "blows",
            "body",
            "boldly",
            "bone",
            "bones",
            "book",
            "books",
            "boon",
            "boots",
            "bore",
            "both",
            "bother",
            "bottle",
            "bottom",
            "bough",
            "bound",
            "bowed",
            "bowing",
            "box",
            "boxed",
            "boy",
            "brain",
            "branch",
            "branches",
            "brandy",
            "brass",
            "brave",
            "breach",
            "bread",
            "break",
            "breath",
            "breathe",
            "breeze",
            "bright",
            "brightened",
            "bring",
            "bringing",
            "bristling",
            "broke",
            "broken",
            "brother",
            "brought",
            "brown",
            "brush",
            "brushing",
            "burn",
            "burning",
            "burnt",
            "burst",
            "bursting",
            "busily",
            "business",
            "business@pglaf",
            "busy",
            "but",
            "butter",
            "buttercup",
            "buttered",
            "butterfly",
            "buttons",
            "by",
            "bye",
            "c",
            "cackled",
            "cake",
            "cakes",
            "calculate",
            "calculated",
            "call",
            "called",
            "calling",
            "calmly",
            "came",
            "camomile",
            "can",
            "canary",
            "candle",
            "cannot",
            "canterbury",
            "canvas",
            "capering",
            "capital",
            "card",
            "cardboard",
            "cards",
            "care",
            "carefully",
            "cares",
            "carried",
            "carrier",
            "carroll",
            "carry",
            "carrying",
            "cart",
            "cartwheels",
            "case",
            "cat",
            "catch",
            "catching",
            "caterpillar",
            "cats",
            "cattle",
            "caucus",
            "caught",
            "cauldron",
            "cause",
            "caused",
            "cautiously",
            "cease",
            "ceiling",
            "centre",
            "certain",
            "certainly",
            "chain",
            "chains",
            "chair",
            "chance",
            "chanced",
            "change",
            "changed",
            "changes",
            "changing",
            "chapter",
            "character",
            "charge",
            "charges",
            "charitable",
            "charities",
            "chatte",
            "cheap",
            "cheated",
            "check",
            "checked",
            "checks",
            "cheeks",
            "cheered",
            "cheerfully",
            "cherry",
            "cheshire",
            "chief",
            "child",
            "childhood",
            "children",
            "chimney",
            "chimneys",
            "chin",
            "choice",
            "choke",
            "choked",
            "choking",
            "choose",
            "choosing",
            "chop",
            "chorus",
            "chose",
            "christmas",
            "chrysalis",
            "chuckled",
            "circle",
            "circumstances",
            "city",
            "civil",
            "claim",
            "clamour",
            "clapping",
            "clasped",
            "classics",
            "claws",
            "clean",
            "clear",
            "cleared",
            "clearer",
            "clearly",
            "clever",
            "climb",
            "clinging",
            "clock",
            "close",
            "closed",
            "closely",
            "closer",
            "clubs",
            "coast",
            "coaxing",
            "codes",
            "coils",
            "cold",
            "collar",
            "collected",
            "collection",
            "come",
            "comes",
            "comfits",
            "comfort",
            "comfortable",
            "comfortably",
            "coming",
            "commercial",
            "committed",
            "common",
            "commotion",
            "company",
            "compilation",
            "complained",
            "complaining",
            "completely",
            "compliance",
            "comply",
            "complying",
            "compressed",
            "computer",
            "computers",
            "concept",
            "concerning",
            "concert",
            "concluded",
            "conclusion",
            "condemn",
            "conduct",
            "confirmation",
            "confirmed",
            "confused",
            "confusing",
            "confusion",
            "conger",
            "conqueror",
            "conquest",
            "consented",
            "consequential",
            "consider",
            "considerable",
            "considered",
            "considering",
            "constant",
            "consultation",
            "contact",
            "contain",
            "containing",
            "contempt",
            "contemptuous",
            "contemptuously",
            "content",
            "continued",
            "contract",
            "contradicted",
            "contributions",
            "conversation",
            "conversations",
            "convert",
            "cook",
            "cool",
            "copied",
            "copies",
            "copy",
            "copying",
            "copyright",
            "corner",
            "corners",
            "corporation",
            "corrupt",
            "cost",
            "costs",
            "could",
            "couldn",
            "counting",
            "countries",
            "country",
            "couple",
            "couples",
            "courage",
            "course",
            "court",
            "courtiers",
            "coward",
            "crab",
            "crash",
            "crashed",
            "crawled",
            "crawling",
            "crazy",
            "created",
            "creating",
            "creation",
            "creature",
            "creatures",
            "credit",
            "creep",
            "crept",
            "cried",
            "cries",
            "crimson",
            "critical",
            "crocodile",
            "croquet",
            "croqueted",
            "croqueting",
            "cross",
            "crossed",
            "crossly",
            "crouched",
            "crowd",
            "crowded",
            "crown",
            "crumbs",
            "crust",
            "cry",
            "crying",
            "cucumber",
            "cunning",
            "cup",
            "cupboards",
            "cur",
            "curiosity",
            "curious",
            "curiouser",
            "curled",
            "curls",
            "curly",
            "currants",
            "current",
            "curtain",
            "curtsey",
            "curtseying",
            "curving",
            "cushion",
            "custard",
            "custody",
            "cut",
            "cutting",
            };
        }
        private void RunTest(Action<AutoCompleteBox, TextBox> test)
        {
            using (UnitTestApplication.Start(Services))
            {
                AutoCompleteBox control = CreateControl();
                control.ItemsSource = CreateSimpleStringArray();
                TextBox textBox = GetTextBox(control);
                var window = new Window {Content = control};
                window.ApplyStyling();
                window.ApplyTemplate();
                window.Presenter!.ApplyTemplate();
                Dispatcher.UIThread.RunJobs();
                test.Invoke(control, textBox);
            }
        }

        private static TestServices Services => TestServices.StyledWindow;

        /*private static TestServices Services => TestServices.MockThreadingInterface.With(
            standardCursorFactory: Mock.Of<IStandardCursorFactory>(),
            windowingPlatform: new MockWindowingPlatform());*/

        private AutoCompleteBox CreateControl()
        {
            var autoCompleteBox =
                new AutoCompleteBox
                {
                    Template = CreateTemplate()
                };

            autoCompleteBox.ApplyTemplate();
            return autoCompleteBox;
        }
        private TextBox GetTextBox(AutoCompleteBox control)
        {
            return control.GetTemplateChildren()
                          .OfType<TextBox>()
                          .First();
        }
        private IControlTemplate CreateTemplate()
        {
            return new FuncControlTemplate<AutoCompleteBox>((control, scope) =>
            {
                var textBox =
                    new TextBox
                    {
                        Name = "PART_TextBox",
                        [!!TextBox.CaretIndexProperty] = control[!!AutoCompleteBox.CaretIndexProperty]
                    }.RegisterInNameScope(scope);
                var listbox =
                    new ListBox
                    {
                        Name = "PART_SelectingItemsControl"
                    }.RegisterInNameScope(scope);
                var popup =
                    new Popup
                    {
                        Name = "PART_Popup",
                        PlacementTarget = control
                    }.RegisterInNameScope(scope);

                var panel = new Panel();
                panel.Children.Add(textBox);
                panel.Children.Add(popup);
                panel.Children.Add(listbox);

                return panel;
            });
        }

        private static TestServices FocusServices => TestServices.MockThreadingInterface.With(
            keyboardDevice: () => new KeyboardDevice(),
            keyboardNavigation: () => new KeyboardNavigationHandler(),
            inputManager: new InputManager(),
            standardCursorFactory: Mock.Of<ICursorFactory>(),
            textShaperImpl: new HarfBuzzTextShaper(),
            fontManagerImpl: new HeadlessFontManagerStub());

        private class TestContextMenu : ContextMenu
        {
            public TestContextMenu()
            {
                IsOpen = true;
            }
        }
    }
}
