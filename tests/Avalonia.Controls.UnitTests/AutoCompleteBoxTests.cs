// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Markup.Data;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Moq;
using Xunit;
using System.Collections.ObjectModel;

namespace Avalonia.Controls.UnitTests
{
    public class AutoCompleteBoxTests
    {
        [Fact]
        public void Search_Filters()
        {
            Assert.True(GetFilter(AutoCompleteFilterMode.Contains)("am", "name"));
            Assert.True(GetFilter(AutoCompleteFilterMode.Contains)("AME", "name"));
            Assert.False(GetFilter(AutoCompleteFilterMode.Contains)("hello", "name"));

            Assert.True(GetFilter(AutoCompleteFilterMode.ContainsCaseSensitive)("na", "name"));
            Assert.False(GetFilter(AutoCompleteFilterMode.ContainsCaseSensitive)("AME", "name"));
            Assert.False(GetFilter(AutoCompleteFilterMode.ContainsCaseSensitive)("hello", "name"));

            Assert.Null(GetFilter(AutoCompleteFilterMode.Custom));
            Assert.Null(GetFilter(AutoCompleteFilterMode.None));

            Assert.True(GetFilter(AutoCompleteFilterMode.Equals)("na", "na"));
            Assert.True(GetFilter(AutoCompleteFilterMode.Equals)("na", "NA"));
            Assert.False(GetFilter(AutoCompleteFilterMode.Equals)("hello", "name"));

            Assert.True(GetFilter(AutoCompleteFilterMode.EqualsCaseSensitive)("na", "na"));
            Assert.False(GetFilter(AutoCompleteFilterMode.EqualsCaseSensitive)("na", "NA"));
            Assert.False(GetFilter(AutoCompleteFilterMode.EqualsCaseSensitive)("hello", "name"));

            Assert.True(GetFilter(AutoCompleteFilterMode.StartsWith)("na", "name"));
            Assert.True(GetFilter(AutoCompleteFilterMode.StartsWith)("NAM", "name"));
            Assert.False(GetFilter(AutoCompleteFilterMode.StartsWith)("hello", "name"));

            Assert.True(GetFilter(AutoCompleteFilterMode.StartsWithCaseSensitive)("na", "name"));
            Assert.False(GetFilter(AutoCompleteFilterMode.StartsWithCaseSensitive)("NAM", "name"));
            Assert.False(GetFilter(AutoCompleteFilterMode.StartsWithCaseSensitive)("hello", "name"));
        }

        [Fact]
        public void Ordinal_Search_Filters()
        {
            Assert.True(GetFilter(AutoCompleteFilterMode.ContainsOrdinal)("am", "name"));
            Assert.True(GetFilter(AutoCompleteFilterMode.ContainsOrdinal)("AME", "name"));
            Assert.False(GetFilter(AutoCompleteFilterMode.ContainsOrdinal)("hello", "name"));

            Assert.True(GetFilter(AutoCompleteFilterMode.ContainsOrdinalCaseSensitive)("na", "name"));
            Assert.False(GetFilter(AutoCompleteFilterMode.ContainsOrdinalCaseSensitive)("AME", "name"));
            Assert.False(GetFilter(AutoCompleteFilterMode.ContainsOrdinalCaseSensitive)("hello", "name"));

            Assert.True(GetFilter(AutoCompleteFilterMode.EqualsOrdinal)("na", "na"));
            Assert.True(GetFilter(AutoCompleteFilterMode.EqualsOrdinal)("na", "NA"));
            Assert.False(GetFilter(AutoCompleteFilterMode.EqualsOrdinal)("hello", "name"));

            Assert.True(GetFilter(AutoCompleteFilterMode.EqualsOrdinalCaseSensitive)("na", "na"));
            Assert.False(GetFilter(AutoCompleteFilterMode.EqualsOrdinalCaseSensitive)("na", "NA"));
            Assert.False(GetFilter(AutoCompleteFilterMode.EqualsOrdinalCaseSensitive)("hello", "name"));

            Assert.True(GetFilter(AutoCompleteFilterMode.StartsWithOrdinal)("na", "name"));
            Assert.True(GetFilter(AutoCompleteFilterMode.StartsWithOrdinal)("NAM", "name"));
            Assert.False(GetFilter(AutoCompleteFilterMode.StartsWithOrdinal)("hello", "name"));

            Assert.True(GetFilter(AutoCompleteFilterMode.StartsWithOrdinalCaseSensitive)("na", "name"));
            Assert.False(GetFilter(AutoCompleteFilterMode.StartsWithOrdinalCaseSensitive)("NAM", "name"));
            Assert.False(GetFilter(AutoCompleteFilterMode.StartsWithOrdinalCaseSensitive)("hello", "name"));
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
                control.Items = CreateSimpleStringArray();

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
                    control.Items = new string[] { custom };
                    Assert.Equal(search, e.Parameter);
                };
                control.Populated += (s, e) =>
                {
                    populated = true;
                    ReadOnlyCollection<object> collection = e.Data as ReadOnlyCollection<object>;
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
                control.ItemFilter = (search, item) =>
                {
                    string s = item as string;
                    return s == null ? false : true;
                };

                // Just set to null briefly to exercise that code path
                AutoCompleteFilterPredicate<object> filter = control.ItemFilter;
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
        
        /// <summary>
        /// Retrieves a defined predicate filter through a new AutoCompleteBox 
        /// control instance.
        /// </summary>
        /// <param name="mode">The FilterMode of interest.</param>
        /// <returns>Returns the predicate instance.</returns>
        private static AutoCompleteFilterPredicate<string> GetFilter(AutoCompleteFilterMode mode)
        {
            return new AutoCompleteBox { FilterMode = mode }
                .TextFilter;
        }

        /// <summary>
        /// Creates a large list of strings for AutoCompleteBox testing.
        /// </summary>
        /// <returns>Returns a new List of string values.</returns>
        private IList<string> CreateSimpleStringArray()
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
                control.Items = CreateSimpleStringArray();
                TextBox textBox = GetTextBox(control);
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
            var datePicker =
                new AutoCompleteBox
                {
                    Template = CreateTemplate()
                };

            datePicker.ApplyTemplate();
            return datePicker;
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
                        Name = "PART_TextBox"
                    }.RegisterInNameScope(scope);
                var listbox =
                    new ListBox
                    {
                        Name = "PART_SelectingItemsControl"
                    }.RegisterInNameScope(scope);
                var popup =
                    new Popup
                    {
                        Name = "PART_Popup"
                    }.RegisterInNameScope(scope);

                var panel = new Panel();
                panel.Children.Add(textBox);
                panel.Children.Add(popup);
                panel.Children.Add(listbox);

                return panel;
            });
        }
    }
}
