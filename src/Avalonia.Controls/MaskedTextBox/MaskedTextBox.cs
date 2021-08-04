using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security;
using System.Text;

namespace Avalonia.Controls.MaskedTextBox
{
    public class MaskedTextBox : AvaloniaObject
    {
        #region STATIC MEMBERS

        private static readonly char[] MaskChars = { '0', '9', '#', 'L', '?', '&', 'C', 'A', 'a', '.', ',', ':', '/', '$', '<', '>', '|', '\\' };

        private static char DefaultPasswordChar = '\0';

        private static string NullMaskString = "<>";

        private static string GetRawText(MaskedTextProvider provider)
        {
            return provider.ToString(true, false, false, 0, provider.Length);
        }

        public static string GetFormatSpecifierFromMask(string mask, IFormatProvider formatProvider)
        {
            List<int> notUsed;

            return MaskedTextBox.GetFormatSpecifierFromMask(
              mask,
              MaskedTextBox.MaskChars,
              formatProvider,
              true,
              out notUsed);
        }

        private static string GetFormatSpecifierFromMask(
          string mask,
          char[] maskChars,
          IFormatProvider formatProvider,
          bool includeNonSeparatorLiteralsInValue,
          out List<int> unhandledLiteralsPositions)
        {
            unhandledLiteralsPositions = new List<int>();

            NumberFormatInfo numberFormatInfo = NumberFormatInfo.GetInstance(formatProvider);

            StringBuilder formatSpecifierBuilder = new StringBuilder(32);

            // Space will be considered as a separator literals and will be included 
            // no matter the value of IncludeNonSeparatorLiteralsInValue.
            bool lastCharIsLiteralIdentifier = false;
            int i = 0;
            int j = 0;

            while (i < mask.Length)
            {
                char currentChar = mask[i];

                if ((currentChar == '\\') && (!lastCharIsLiteralIdentifier))
                {
                    lastCharIsLiteralIdentifier = true;
                }
                else
                {
                    if ((lastCharIsLiteralIdentifier) || (Array.IndexOf(maskChars, currentChar) < 0))
                    {
                        lastCharIsLiteralIdentifier = false;

                        // The currentChar was preceeded by a liteal identifier or is not part of the MaskedTextProvider mask chars.
                        formatSpecifierBuilder.Append('\\');
                        formatSpecifierBuilder.Append(currentChar);

                        if ((!includeNonSeparatorLiteralsInValue) && (currentChar != ' '))
                            unhandledLiteralsPositions.Add(j);

                        j++;
                    }
                    else
                    {
                        // The currentChar is part of the MaskedTextProvider mask chars.  
                        if ((currentChar == '0') || (currentChar == '9') || (currentChar == '#'))
                        {
                            formatSpecifierBuilder.Append('0');
                            j++;
                        }
                        else if (currentChar == '.')
                        {
                            formatSpecifierBuilder.Append('.');
                            j += numberFormatInfo.NumberDecimalSeparator.Length;
                        }
                        else if (currentChar == ',')
                        {
                            formatSpecifierBuilder.Append(',');
                            j += numberFormatInfo.NumberGroupSeparator.Length;
                        }
                        else if (currentChar == '$')
                        {
                            string currencySymbol = numberFormatInfo.CurrencySymbol;

                            formatSpecifierBuilder.Append('"');
                            formatSpecifierBuilder.Append(currencySymbol);
                            formatSpecifierBuilder.Append('"');

                            for (int k = 0; k < currencySymbol.Length; k++)
                            {
                                if (!includeNonSeparatorLiteralsInValue)
                                    unhandledLiteralsPositions.Add(j);

                                j++;
                            }
                        }
                        else
                        {
                            formatSpecifierBuilder.Append(currentChar);

                            if ((!includeNonSeparatorLiteralsInValue) && (currentChar != ' '))
                                unhandledLiteralsPositions.Add(j);

                            j++;
                        }
                    }
                }

                i++;
            }

            return formatSpecifierBuilder.ToString();
        }

        #endregion STATIC MEMBERS

        #region CONSTRUCTORS

        static MaskedTextBox()
        {
            MaskedTextBox.TextProperty.OverrideMetadata(typeof(MaskedTextBox),
              new FrameworkPropertyMetadata(
              null,
              new CoerceValueCallback(MaskedTextBox.TextCoerceValueCallback)));
        }

        public MaskedTextBox()
        {
            CommandManager.AddPreviewCanExecuteHandler(this, new CanExecuteRoutedEventHandler(this.OnPreviewCanExecuteCommands));
            CommandManager.AddPreviewExecutedHandler(this, new ExecutedRoutedEventHandler(this.OnPreviewExecutedCommands));

            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, null, new CanExecuteRoutedEventHandler(this.CanExecutePaste)));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, null, new CanExecuteRoutedEventHandler(this.CanExecuteCut)));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, null, new CanExecuteRoutedEventHandler(this.CanExecuteCopy)));
            this.CommandBindings.Add(new CommandBinding(EditingCommands.ToggleInsert, new ExecutedRoutedEventHandler(this.ToggleInsertExecutedCallback)));

            this.CommandBindings.Add(new CommandBinding(EditingCommands.Delete, null, new CanExecuteRoutedEventHandler(this.CanExecuteDelete)));
            this.CommandBindings.Add(new CommandBinding(EditingCommands.DeletePreviousWord, null, new CanExecuteRoutedEventHandler(this.CanExecuteDeletePreviousWord)));
            this.CommandBindings.Add(new CommandBinding(EditingCommands.DeleteNextWord, null, new CanExecuteRoutedEventHandler(this.CanExecuteDeleteNextWord)));

            this.CommandBindings.Add(new CommandBinding(EditingCommands.Backspace, null, new CanExecuteRoutedEventHandler(this.CanExecuteBackspace)));

            System.Windows.DragDrop.AddPreviewQueryContinueDragHandler(this, new QueryContinueDragEventHandler(this.PreviewQueryContinueDragCallback));
            this.AllowDrop = false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1820:TestForEmptyStringsUsingStringLength")]
        private void InitializeMaskedTextProvider()
        {
            string preInitializedText = this.Text;

            string mask = this.Mask;

            if (mask == string.Empty)
            {
                m_maskedTextProvider = this.CreateMaskedTextProvider(MaskedTextBox.NullMaskString);
                m_maskIsNull = true;
            }
            else
            {
                m_maskedTextProvider = this.CreateMaskedTextProvider(mask);
                m_maskIsNull = false;
            }

            if ((!m_maskIsNull) && (preInitializedText != string.Empty))
            {
                bool success = m_maskedTextProvider.Add(preInitializedText);

                if ((!success) && (!DesignerProperties.GetIsInDesignMode(this)))
                    throw new InvalidOperationException("An attempt was made to apply a new mask that cannot be applied to the current text.");
            }
        }

        #endregion CONSTRUCTORS

        #region ISupportInitialize

        protected override void OnInitialized(EventArgs e)
        {
            this.InitializeMaskedTextProvider();

            this.SetIsMaskCompleted(m_maskedTextProvider.MaskCompleted);
            this.SetIsMaskFull(m_maskedTextProvider.MaskFull);

            base.OnInitialized(e);
        }

        #endregion ISupportInitialize

        #region AllowPromptAsInput Property

        public static readonly DependencyProperty AllowPromptAsInputProperty =
            DependencyProperty.Register("AllowPromptAsInput", typeof(bool), typeof(MaskedTextBox),
          new UIPropertyMetadata(
          true,
          new PropertyChangedCallback(MaskedTextBox.AllowPromptAsInputPropertyChangedCallback)));

        public bool AllowPromptAsInput
        {
            get
            {
                return (bool)GetValue(AllowPromptAsInputProperty);
            }
            set
            {
                SetValue(AllowPromptAsInputProperty, value);
            }
        }

        private static void AllowPromptAsInputPropertyChangedCallback(object sender, DependencyPropertyChangedEventArgs e)
        {
            MaskedTextBox maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return;

            if (maskedTextBox.m_maskIsNull)
                return;

            maskedTextBox.m_maskedTextProvider = maskedTextBox.CreateMaskedTextProvider(maskedTextBox.Mask);
        }

        #endregion AllowPromptAsInput Property

        #region ClipboardMaskFormat Property

        public MaskFormat ClipboardMaskFormat
        {
            get
            {
                return (MaskFormat)GetValue(ClipboardMaskFormatProperty);
            }
            set
            {
                SetValue(ClipboardMaskFormatProperty, value);
            }
        }

        public static readonly DependencyProperty ClipboardMaskFormatProperty =
            DependencyProperty.Register("ClipboardMaskFormat", typeof(MaskFormat), typeof(MaskedTextBox),
          new UIPropertyMetadata(MaskFormat.IncludeLiterals));

        #endregion ClipboardMaskFormat Property

        #region HidePromptOnLeave Property

        public bool HidePromptOnLeave
        {
            get
            {
                return (bool)GetValue(HidePromptOnLeaveProperty);
            }
            set
            {
                SetValue(HidePromptOnLeaveProperty, value);
            }
        }

        public static readonly DependencyProperty HidePromptOnLeaveProperty =
            DependencyProperty.Register("HidePromptOnLeave", typeof(bool), typeof(MaskedTextBox), new UIPropertyMetadata(false));

        #endregion HidePromptOnLeave Property

        #region IncludeLiteralsInValue Property

        public bool IncludeLiteralsInValue
        {
            get
            {
                return (bool)GetValue(IncludeLiteralsInValueProperty);
            }
            set
            {
                SetValue(IncludeLiteralsInValueProperty, value);
            }
        }

        public static readonly DependencyProperty IncludeLiteralsInValueProperty =
            DependencyProperty.Register("IncludeLiteralsInValue", typeof(bool), typeof(MaskedTextBox),
          new UIPropertyMetadata(
          true,
          new PropertyChangedCallback(MaskedTextBox.InlcudeLiteralsInValuePropertyChangedCallback)));

        private static void InlcudeLiteralsInValuePropertyChangedCallback(object sender, DependencyPropertyChangedEventArgs e)
        {
            MaskedTextBox maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return;

            maskedTextBox.RefreshConversionHelpers();
            maskedTextBox.RefreshValue();
        }

        #endregion IncludeLiteralsInValue Property

        #region IncludePromptInValue Property

        public bool IncludePromptInValue
        {
            get
            {
                return (bool)GetValue(IncludePromptInValueProperty);
            }
            set
            {
                SetValue(IncludePromptInValueProperty, value);
            }
        }

        public static readonly DependencyProperty IncludePromptInValueProperty =
            DependencyProperty.Register("IncludePromptInValue", typeof(bool), typeof(MaskedTextBox),
          new UIPropertyMetadata(
          false,
          new PropertyChangedCallback(MaskedTextBox.IncludePromptInValuePropertyChangedCallback)));

        private static void IncludePromptInValuePropertyChangedCallback(object sender, DependencyPropertyChangedEventArgs e)
        {
            MaskedTextBox maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return;

            maskedTextBox.RefreshValue();
        }

        #endregion IncludePromptInValue Property

        #region InsertKeyMode Property

        public InsertKeyMode InsertKeyMode
        {
            get
            {
                return (InsertKeyMode)GetValue(InsertKeyModeProperty);
            }
            set
            {
                SetValue(InsertKeyModeProperty, value);
            }
        }

        public static readonly DependencyProperty InsertKeyModeProperty =
            DependencyProperty.Register("InsertKeyMode", typeof(InsertKeyMode), typeof(MaskedTextBox), new UIPropertyMetadata(InsertKeyMode.Default));

        #endregion InsertKeyMode Property

        #region IsMaskCompleted Read-Only Property

        private static readonly DependencyPropertyKey IsMaskCompletedPropertyKey =
            DependencyProperty.RegisterReadOnly("IsMaskCompleted", typeof(bool), typeof(MaskedTextBox), new PropertyMetadata(false));

        public static readonly DependencyProperty IsMaskCompletedProperty = MaskedTextBox.IsMaskCompletedPropertyKey.DependencyProperty;


        public bool IsMaskCompleted
        {
            get
            {
                return (bool)this.GetValue(MaskedTextBox.IsMaskCompletedProperty);
            }
        }

        private void SetIsMaskCompleted(bool value)
        {
            this.SetValue(MaskedTextBox.IsMaskCompletedPropertyKey, value);
        }

        #endregion IsMaskCompleted Read-Only Property

        #region IsMaskFull Read-Only Property

        private static readonly DependencyPropertyKey IsMaskFullPropertyKey =
            DependencyProperty.RegisterReadOnly("IsMaskFull", typeof(bool), typeof(MaskedTextBox), new PropertyMetadata(false));

        public static readonly DependencyProperty IsMaskFullProperty = MaskedTextBox.IsMaskFullPropertyKey.DependencyProperty;

        public bool IsMaskFull
        {
            get
            {
                return (bool)this.GetValue(MaskedTextBox.IsMaskFullProperty);
            }
        }

        private void SetIsMaskFull(bool value)
        {
            this.SetValue(MaskedTextBox.IsMaskFullPropertyKey, value);
        }

        #endregion IsMaskFull Read-Only Property

        #region Mask Property

        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.Register("Mask", typeof(string), typeof(MaskedTextBox),
          new UIPropertyMetadata(
          string.Empty,
          new PropertyChangedCallback(MaskedTextBox.MaskPropertyChangedCallback),
          new CoerceValueCallback(MaskedTextBox.MaskCoerceValueCallback)));

        public string Mask
        {
            get
            {
                return (string)this.GetValue(MaskedTextBox.MaskProperty);
            }
            set
            {
                this.SetValue(MaskedTextBox.MaskProperty, value);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        private static object MaskCoerceValueCallback(DependencyObject sender, object value)
        {
            if (value == null)
                value = string.Empty;

            if (value.Equals(string.Empty))
                return value;

            // Validate the text against the would be new Mask.

            MaskedTextBox maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return value;

            bool valid;

            try
            {
                MaskedTextProvider provider = maskedTextBox.CreateMaskedTextProvider((string)value);

                string rawText = MaskedTextBox.GetRawText(maskedTextBox.m_maskedTextProvider);

                valid = provider.VerifyString(rawText);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("An error occured while testing the current text against the new mask.", exception);
            }

            if (!valid)
                throw new ArgumentException("The mask cannot be applied to the current text.", "Mask");

            return value;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1820:TestForEmptyStringsUsingStringLength")]
        private static void MaskPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            MaskedTextBox maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return;

            MaskedTextProvider provider = null;

            string mask = (string)e.NewValue;

            if (mask == string.Empty)
            {
                provider = maskedTextBox.CreateMaskedTextProvider(MaskedTextBox.NullMaskString);
                maskedTextBox.m_maskIsNull = true;
                maskedTextBox.Text = "";
            }
            else
            {
                provider = maskedTextBox.CreateMaskedTextProvider(mask);
                maskedTextBox.m_maskIsNull = false;
            }

            maskedTextBox.m_maskedTextProvider = provider;

            maskedTextBox.RefreshConversionHelpers();

            if (maskedTextBox.ValueDataType != null)
            {
                string textFromValue = maskedTextBox.GetTextFromValue(maskedTextBox.Value);
                maskedTextBox.m_maskedTextProvider.Set(textFromValue);
            }

            maskedTextBox.RefreshCurrentText(true);
        }

        #endregion Mask Property

        #region MaskedTextProvider Property

        public MaskedTextProvider MaskedTextProvider
        {
            get
            {
                if (!m_maskIsNull)
                    return m_maskedTextProvider.Clone() as MaskedTextProvider;

                return null;
            }
        }

        #endregion MaskedTextProvider Property

        #region PromptChar Property

        public static readonly DependencyProperty PromptCharProperty =
            DependencyProperty.Register("PromptChar", typeof(char), typeof(MaskedTextBox),
          new UIPropertyMetadata(
          '_',
          new PropertyChangedCallback(MaskedTextBox.PromptCharPropertyChangedCallback),
          new CoerceValueCallback(MaskedTextBox.PromptCharCoerceValueCallback)));

        public char PromptChar
        {
            get
            {
                return (char)this.GetValue(MaskedTextBox.PromptCharProperty);
            }
            set
            {
                this.SetValue(MaskedTextBox.PromptCharProperty, value);
            }
        }

        private static object PromptCharCoerceValueCallback(object sender, object value)
        {
            MaskedTextBox maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return value;

            MaskedTextProvider provider = maskedTextBox.m_maskedTextProvider.Clone() as MaskedTextProvider;

            try
            {
                provider.PromptChar = (char)value;
            }
            catch (Exception exception)
            {
                throw new ArgumentException("The prompt character is invalid.", exception);
            }

            return value;
        }

        private static void PromptCharPropertyChangedCallback(object sender, DependencyPropertyChangedEventArgs e)
        {
            MaskedTextBox maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return;

            if (maskedTextBox.m_maskIsNull)
                return;

            maskedTextBox.m_maskedTextProvider.PromptChar = (char)e.NewValue;

            maskedTextBox.RefreshCurrentText(true);
        }

        #endregion PromptChar Property

        #region RejectInputOnFirstFailure Property

        public bool RejectInputOnFirstFailure
        {
            get
            {
                return (bool)GetValue(RejectInputOnFirstFailureProperty);
            }
            set
            {
                SetValue(RejectInputOnFirstFailureProperty, value);
            }
        }

        public static readonly DependencyProperty RejectInputOnFirstFailureProperty =
            DependencyProperty.Register("RejectInputOnFirstFailure", typeof(bool), typeof(MaskedTextBox), new UIPropertyMetadata(true));

        #endregion RejectInputOnFirstFailure Property

        #region ResetOnPrompt Property

        public bool ResetOnPrompt
        {
            get
            {
                return (bool)GetValue(ResetOnPromptProperty);
            }
            set
            {
                SetValue(ResetOnPromptProperty, value);
            }
        }

        public static readonly DependencyProperty ResetOnPromptProperty =
            DependencyProperty.Register("ResetOnPrompt", typeof(bool), typeof(MaskedTextBox),
          new UIPropertyMetadata(
          true,
          new PropertyChangedCallback(MaskedTextBox.ResetOnPromptPropertyChangedCallback)));

        private static void ResetOnPromptPropertyChangedCallback(object sender, DependencyPropertyChangedEventArgs e)
        {
            MaskedTextBox maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return;

            if (maskedTextBox.m_maskIsNull)
                return;

            maskedTextBox.m_maskedTextProvider.ResetOnPrompt = (bool)e.NewValue;
        }

        #endregion ResetOnPrompt Property

        #region ResetOnSpace Property

        public bool ResetOnSpace
        {
            get
            {
                return (bool)GetValue(ResetOnSpaceProperty);
            }
            set
            {
                SetValue(ResetOnSpaceProperty, value);
            }
        }

        public static readonly DependencyProperty ResetOnSpaceProperty =
            DependencyProperty.Register("ResetOnSpace", typeof(bool), typeof(MaskedTextBox),
          new UIPropertyMetadata(
          true,
          new PropertyChangedCallback(MaskedTextBox.ResetOnSpacePropertyChangedCallback)));

        private static void ResetOnSpacePropertyChangedCallback(object sender, DependencyPropertyChangedEventArgs e)
        {
            MaskedTextBox maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return;

            if (maskedTextBox.m_maskIsNull)
                return;

            maskedTextBox.m_maskedTextProvider.ResetOnSpace = (bool)e.NewValue;
        }

        #endregion ResetOnSpace Property

        #region RestrictToAscii Property

        public bool RestrictToAscii
        {
            get
            {
                return (bool)GetValue(RestrictToAsciiProperty);
            }
            set
            {
                SetValue(RestrictToAsciiProperty, value);
            }
        }

        public static readonly DependencyProperty RestrictToAsciiProperty =
            DependencyProperty.Register("RestrictToAscii", typeof(bool), typeof(MaskedTextBox),
          new UIPropertyMetadata(
          false,
          new PropertyChangedCallback(MaskedTextBox.RestrictToAsciiPropertyChangedCallback),
          new CoerceValueCallback(MaskedTextBox.RestrictToAsciiCoerceValueCallback)));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        private static object RestrictToAsciiCoerceValueCallback(object sender, object value)
        {
            MaskedTextBox maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return value;

            if (maskedTextBox.m_maskIsNull)
                return value;

            bool restrictToAscii = (bool)value;

            if (!restrictToAscii)
                return value;

            // Validate the text to make sure that it is only made of Ascii characters.

            MaskedTextProvider provider = maskedTextBox.CreateMaskedTextProvider(
              maskedTextBox.Mask,
              maskedTextBox.GetCultureInfo(),
              maskedTextBox.AllowPromptAsInput,
              maskedTextBox.PromptChar,
              MaskedTextBox.DefaultPasswordChar,
              restrictToAscii);

            if (!provider.VerifyString(maskedTextBox.Text))
                throw new ArgumentException("The current text cannot be restricted to ASCII characters. The RestrictToAscii property is set to true.", "RestrictToAscii");

            return restrictToAscii;
        }

        private static void RestrictToAsciiPropertyChangedCallback(object sender, DependencyPropertyChangedEventArgs e)
        {
            MaskedTextBox maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return;

            if (maskedTextBox.m_maskIsNull)
                return;

            maskedTextBox.m_maskedTextProvider = maskedTextBox.CreateMaskedTextProvider(maskedTextBox.Mask);

            maskedTextBox.RefreshCurrentText(true);
        }

        #endregion RestrictToAscii Property

        #region SkipLiterals Property

        public bool SkipLiterals
        {
            get
            {
                return (bool)GetValue(SkipLiteralsProperty);
            }
            set
            {
                SetValue(SkipLiteralsProperty, value);
            }
        }

        public static readonly DependencyProperty SkipLiteralsProperty =
            DependencyProperty.Register("SkipLiterals", typeof(bool), typeof(MaskedTextBox),
          new UIPropertyMetadata(
          true,
          new PropertyChangedCallback(MaskedTextBox.SkipLiteralsPropertyChangedCallback)));

        private static void SkipLiteralsPropertyChangedCallback(object sender, DependencyPropertyChangedEventArgs e)
        {
            MaskedTextBox maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return;

            if (maskedTextBox.m_maskIsNull)
                return;

            maskedTextBox.m_maskedTextProvider.SkipLiterals = (bool)e.NewValue;
        }

        #endregion SkipLiterals Property

        #region Text Property

        private static object TextCoerceValueCallback(DependencyObject sender, object value)
        {
            MaskedTextBox maskedTextBox = sender as MaskedTextBox;

            if (!maskedTextBox.IsInitialized)
                return DependencyProperty.UnsetValue;

            if (maskedTextBox.IsInIMEComposition)
            {
                // In IME Composition.  We must return an uncoerced value or else the IME decorator won't disappear after text input.
                return value;
            }

            if (value == null)
                value = string.Empty;

            if ((maskedTextBox.IsForcingText) || (maskedTextBox.m_maskIsNull))
                return value;

            // Only direct affectation to the Text property or binding of the Text property should
            // come through here.  All other cases should pre-validate the text and affect it through the ForceText method.
            string text = maskedTextBox.ValidateText((string)value);

            return text;
        }

        private string ValidateText(string text)
        {
            string coercedText = text;

            if (this.RejectInputOnFirstFailure)
            {
                MaskedTextProvider provider = m_maskedTextProvider.Clone() as MaskedTextProvider;

                int notUsed;
                MaskedTextResultHint hint;

                //0 – Digit zero to 9[ Required ]
                //9 – Digit 0 – 9[ Optional ]
                //A – Alpha Numeric. [Required]
                //a – Alpha Numeric. [Optional]
                //L – Letters a-z, A-Z[ Required ]
                //? – Letters a-z, A-Z[ Optional ]
                //C – Any non-control character [Optional]
                //< - When first, all following characters are in lower case.
                //> - When first, all following characters are in upper case.
                if (provider.Set(text, out notUsed, out hint) || provider.Mask.StartsWith(">") || provider.Mask.StartsWith("<"))
                {
                    coercedText = this.GetFormattedString(provider, text);
                }
                else
                {
                    // Coerce the text to remain the same.
                    coercedText = this.GetFormattedString(m_maskedTextProvider, text);

                    // The TextPropertyChangedCallback won't be called.
                    // Therefore, we must sync the maskedTextProvider.
                    m_maskedTextProvider.Set(coercedText);
                }
            }
            else
            {
                MaskedTextProvider provider = (MaskedTextProvider)m_maskedTextProvider.Clone();

                int caretIndex;

                if (this.CanReplace(provider, text, 0, m_maskedTextProvider.Length, this.RejectInputOnFirstFailure, out caretIndex))
                {
                    coercedText = this.GetFormattedString(provider, text);
                }
                else
                {
                    // Coerce the text to remain the same.
                    coercedText = this.GetFormattedString(m_maskedTextProvider, text);

                    // The TextPropertyChangedCallback won't be called.
                    // Therefore, we must sync the maskedTextProvider.
                    m_maskedTextProvider.Set(coercedText);
                }
            }

            return coercedText;
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (!m_maskIsNull)
            {
                if ((this.IsInValueChanged) || (!this.IsForcingText))
                {
                    string newText = this.Text;

                    if (m_maskIsNull)
                    {
                        this.CaretIndex = newText.Length;
                    }
                    else
                    {
                        m_maskedTextProvider.Set(newText);

                        if (m_maskedTextProvider.Mask.StartsWith(">") || m_maskedTextProvider.Mask.StartsWith("<"))
                        {
                            this.CaretIndex = newText.Length;
                        }
                        else
                        {
                            int caretIndex = m_maskedTextProvider.FindUnassignedEditPositionFrom(0, true);

                            if (caretIndex == -1)
                                caretIndex = m_maskedTextProvider.Length;

                            this.CaretIndex = caretIndex;
                        }
                    }
                }
            }

            // m_maskedTextProvider can be null in the designer. With WPF 3.5 SP1, sometimes, 
            // TextChanged will be triggered before OnInitialized is called.
            if (m_maskedTextProvider != null)
            {
                this.SetIsMaskCompleted(m_maskedTextProvider.MaskCompleted);
                this.SetIsMaskFull(m_maskedTextProvider.MaskFull);
            }

            base.OnTextChanged(e);
        }

        #endregion Text Property


        #region COMMANDS

        private void OnPreviewCanExecuteCommands(object sender, CanExecuteRoutedEventArgs e)
        {
            if (m_maskIsNull)
                return;

            RoutedUICommand routedUICommand = e.Command as RoutedUICommand;

            if ((routedUICommand != null)
              && ((routedUICommand.Name == "Space") || (routedUICommand.Name == "ShiftSpace")))
            {
                if (this.IsReadOnly)
                {
                    e.CanExecute = false;
                }
                else
                {
                    MaskedTextProvider provider = (MaskedTextProvider)m_maskedTextProvider.Clone();
                    int caretIndex;
                    e.CanExecute = this.CanReplace(provider, " ", this.SelectionStart, this.SelectionLength, this.RejectInputOnFirstFailure, out caretIndex);
                }

                e.Handled = true;
            }
            else if ((e.Command == ApplicationCommands.Undo) || (e.Command == ApplicationCommands.Redo))
            {
                e.CanExecute = false;
                e.Handled = true;
            }
        }

        private void OnPreviewExecutedCommands(object sender, ExecutedRoutedEventArgs e)
        {
            if (m_maskIsNull)
                return;

            if (e.Command == EditingCommands.Delete)
            {
                e.Handled = true;
                this.Delete(this.SelectionStart, this.SelectionLength, true);
            }
            else if (e.Command == EditingCommands.DeleteNextWord)
            {
                e.Handled = true;
                EditingCommands.SelectRightByWord.Execute(null, this as IInputElement);
                this.Delete(this.SelectionStart, this.SelectionLength, true);
            }
            else if (e.Command == EditingCommands.DeletePreviousWord)
            {
                e.Handled = true;
                EditingCommands.SelectLeftByWord.Execute(null, this as IInputElement);
                this.Delete(this.SelectionStart, this.SelectionLength, false);
            }
            else if (e.Command == EditingCommands.Backspace)
            {
                e.Handled = true;
                this.Delete(this.SelectionStart, this.SelectionLength, false);
            }
            else if (e.Command == ApplicationCommands.Cut)
            {
                e.Handled = true;

                if (ApplicationCommands.Copy.CanExecute(null, this as IInputElement))
                    ApplicationCommands.Copy.Execute(null, this as IInputElement);

                this.Delete(this.SelectionStart, this.SelectionLength, true);
            }
            else if (e.Command == ApplicationCommands.Copy)
            {
                e.Handled = true;
                this.ExecuteCopy();
            }
            else if (e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;

                string clipboardContent = (string)Clipboard.GetDataObject().GetData("System.String");
                this.Replace(clipboardContent, this.SelectionStart, this.SelectionLength);
            }
            else
            {
                RoutedUICommand routedUICommand = e.Command as RoutedUICommand;

                if ((routedUICommand != null)
                  && ((routedUICommand.Name == "Space") || (routedUICommand.Name == "ShiftSpace")))
                {
                    e.Handled = true;
                    this.ProcessTextInput(" ");
                }
            }
        }

        private void CanExecuteDelete(object sender, CanExecuteRoutedEventArgs e)
        {
            if (m_maskIsNull)
                return;

            e.CanExecute = this.CanDelete(this.SelectionStart, this.SelectionLength, true, this.MaskedTextProvider.Clone() as MaskedTextProvider);
            e.Handled = true;

            if ((!e.CanExecute) && (this.BeepOnError))
                System.Media.SystemSounds.Beep.Play();
        }

        private void CanExecuteDeletePreviousWord(object sender, CanExecuteRoutedEventArgs e)
        {
            if (m_maskIsNull)
                return;

            bool canDeletePreviousWord = (!this.IsReadOnly) && (EditingCommands.SelectLeftByWord.CanExecute(null, this as IInputElement));

            if (canDeletePreviousWord)
            {
                int cachedSelectionStart = this.SelectionStart;
                int cachedSelectionLength = this.SelectionLength;

                EditingCommands.SelectLeftByWord.Execute(null, this as IInputElement);

                canDeletePreviousWord = this.CanDelete(this.SelectionStart, this.SelectionLength, false, this.MaskedTextProvider.Clone() as MaskedTextProvider);

                if (!canDeletePreviousWord)
                {
                    this.SelectionStart = cachedSelectionStart;
                    this.SelectionLength = cachedSelectionLength;
                }
            }

            e.CanExecute = canDeletePreviousWord;
            e.Handled = true;

            if ((!e.CanExecute) && (this.BeepOnError))
                System.Media.SystemSounds.Beep.Play();
        }

        private void CanExecuteDeleteNextWord(object sender, CanExecuteRoutedEventArgs e)
        {
            if (m_maskIsNull)
                return;

            bool canDeleteNextWord = (!this.IsReadOnly) && (EditingCommands.SelectRightByWord.CanExecute(null, this as IInputElement));

            if (canDeleteNextWord)
            {
                int cachedSelectionStart = this.SelectionStart;
                int cachedSelectionLength = this.SelectionLength;

                EditingCommands.SelectRightByWord.Execute(null, this as IInputElement);

                canDeleteNextWord = this.CanDelete(this.SelectionStart, this.SelectionLength, true, this.MaskedTextProvider.Clone() as MaskedTextProvider);

                if (!canDeleteNextWord)
                {
                    this.SelectionStart = cachedSelectionStart;
                    this.SelectionLength = cachedSelectionLength;
                }
            }

            e.CanExecute = canDeleteNextWord;
            e.Handled = true;

            if ((!e.CanExecute) && (this.BeepOnError))
                System.Media.SystemSounds.Beep.Play();
        }

        private void CanExecuteBackspace(object sender, CanExecuteRoutedEventArgs e)
        {
            if (m_maskIsNull)
                return;

            e.CanExecute = this.CanDelete(this.SelectionStart, this.SelectionLength, false, this.MaskedTextProvider.Clone() as MaskedTextProvider);
            e.Handled = true;

            if ((!e.CanExecute) && (this.BeepOnError))
                System.Media.SystemSounds.Beep.Play();
        }

        private void CanExecuteCut(object sender, CanExecuteRoutedEventArgs e)
        {
            if (m_maskIsNull)
                return;

            bool canCut = (!this.IsReadOnly) && (this.SelectionLength > 0);

            if (canCut)
            {
                int endPosition = (this.SelectionLength > 0) ? ((this.SelectionStart + this.SelectionLength) - 1) : this.SelectionStart;

                MaskedTextProvider provider = m_maskedTextProvider.Clone() as MaskedTextProvider;

                canCut = provider.RemoveAt(this.SelectionStart, endPosition);
            }

            e.CanExecute = canCut;
            e.Handled = true;

            if ((!canCut) && (this.BeepOnError))
                System.Media.SystemSounds.Beep.Play();
        }

        private void CanExecutePaste(object sender, CanExecuteRoutedEventArgs e)
        {
            if (m_maskIsNull)
                return;

            bool canPaste = false;

            if (!this.IsReadOnly)
            {
                string clipboardContent = string.Empty;

                try
                {
                    clipboardContent = (string)Clipboard.GetDataObject().GetData("System.String");

                    if (clipboardContent != null)
                    {
                        MaskedTextProvider provider = (MaskedTextProvider)m_maskedTextProvider.Clone();
                        int caretIndex;
                        canPaste = this.CanReplace(provider, clipboardContent, this.SelectionStart, this.SelectionLength, this.RejectInputOnFirstFailure, out caretIndex);
                    }
                }
                catch
                {
                }
            }

            e.CanExecute = canPaste;
            e.Handled = true;

            if ((!e.CanExecute) && (this.BeepOnError))
                System.Media.SystemSounds.Beep.Play();
        }

        private void CanExecuteCopy(object sender, CanExecuteRoutedEventArgs e)
        {
            if (m_maskIsNull)
                return;

            e.CanExecute = !m_maskedTextProvider.IsPassword;
            e.Handled = true;

            if ((!e.CanExecute) && (this.BeepOnError))
                System.Media.SystemSounds.Beep.Play();
        }

        private void ExecuteCopy()
        {
            string selectedText = this.GetSelectedText();
            try
            {
                new UIPermission(UIPermissionClipboard.AllClipboard).Demand();

                if (selectedText.Length == 0)
                {
                    Clipboard.Clear();
                }
                else
                {
                    Clipboard.SetText(selectedText);
                }
            }
            catch (SecurityException)
            {
            }
        }

        private void ToggleInsertExecutedCallback(object sender, ExecutedRoutedEventArgs e)
        {
            m_insertToggled = !m_insertToggled;
        }

        #endregion COMMANDS

        #region DRAG DROP

        private void PreviewQueryContinueDragCallback(object sender, QueryContinueDragEventArgs e)
        {
            if (m_maskIsNull)
                return;

            e.Action = DragAction.Cancel;
            e.Handled = true;
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            if (!m_maskIsNull)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }

            base.OnDragEnter(e);
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            if (!m_maskIsNull)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }

            base.OnDragOver(e);
        }

        #endregion DRAG DROP


        #region VALUE FROM TEXT

        protected override bool QueryValueFromTextCore(string text, out object value)
        {
            Type valueDataType = this.ValueDataType;

            if (valueDataType != null)
            {
                if ((m_unhandledLiteralsPositions != null)
                  && (m_unhandledLiteralsPositions.Count > 0))
                {
                    text = m_maskedTextProvider.ToString(false, false, true, 0, m_maskedTextProvider.Length);

                    for (int i = m_unhandledLiteralsPositions.Count - 1; i >= 0; i--)
                    {
                        text = text.Remove(m_unhandledLiteralsPositions[i], 1);
                    }
                }
            }

            return base.QueryValueFromTextCore(text, out value);
        }

        #endregion VALUE FROM TEXT

        #region TEXT FROM VALUE

        protected override string QueryTextFromValueCore(object value)
        {
            if ((m_valueToStringMethodInfo != null) && (value != null))
            {
                try
                {
                    string text = (string)m_valueToStringMethodInfo.Invoke(value, new object[] { m_formatSpecifier, this.GetActiveFormatProvider() });
                    return text;
                }
                catch
                {
                }
            }

            return base.QueryTextFromValueCore(value);
        }

        #endregion TEXT FROM VALUE


        #region PROTECTED METHODS

        protected virtual char[] GetMaskCharacters()
        {
            return MaskedTextBox.MaskChars;
        }

        private MaskedTextProvider CreateMaskedTextProvider(string mask)
        {
            return this.CreateMaskedTextProvider(
              mask,
              this.GetCultureInfo(),
              this.AllowPromptAsInput,
              this.PromptChar,
              MaskedTextBox.DefaultPasswordChar,
              this.RestrictToAscii);
        }

        protected virtual MaskedTextProvider CreateMaskedTextProvider(
          string mask,
          CultureInfo cultureInfo,
          bool allowPromptAsInput,
          char promptChar,
          char passwordChar,
          bool restrictToAscii)
        {
            MaskedTextProvider provider = new MaskedTextProvider(
              mask,
              cultureInfo,
              allowPromptAsInput,
              promptChar,
              passwordChar,
              restrictToAscii);

            provider.ResetOnPrompt = this.ResetOnPrompt;
            provider.ResetOnSpace = this.ResetOnSpace;
            provider.SkipLiterals = this.SkipLiterals;

            provider.IncludeLiterals = true;
            provider.IncludePrompt = true;

            provider.IsPassword = false;

            return provider;
        }

        internal override void OnIMECompositionEnded(CachedTextInfo cachedTextInfo)
        {
            // End of IME Composition.  Restore the critical infos.
            this.ForceText(cachedTextInfo.Text, false);
            this.CaretIndex = cachedTextInfo.CaretIndex;
            this.SelectionStart = cachedTextInfo.SelectionStart;
            this.SelectionLength = cachedTextInfo.SelectionLength;
        }

        protected override void OnTextInput(System.Windows.Input.TextCompositionEventArgs e)
        {
            if (this.IsInIMEComposition)
                this.EndIMEComposition();

            if ((m_maskIsNull) || (m_maskedTextProvider == null) || (this.IsReadOnly))
            {
                base.OnTextInput(e);
                return;
            }

            e.Handled = true;

            if (this.CharacterCasing == CharacterCasing.Upper)
            {
                this.ProcessTextInput(e.Text.ToUpper());
            }
            else if (this.CharacterCasing == CharacterCasing.Lower)
            {
                this.ProcessTextInput(e.Text.ToLower());
            }
            else
            {
                this.ProcessTextInput(e.Text);
            }

            base.OnTextInput(e);
        }

        private void ProcessTextInput(string text)
        {
            if (text.Length == 1)
            {
                string textOutput = this.MaskedTextOutput;

                int caretIndex;
                if (this.PlaceChar(text[0], this.SelectionStart, this.SelectionLength, this.IsOverwriteMode, out caretIndex))
                {
                    if (this.MaskedTextOutput != textOutput)
                        this.RefreshCurrentText(false);

                    this.SelectionStart = caretIndex + 1;
                }
                else
                {
                    if (this.BeepOnError)
                        System.Media.SystemSounds.Beep.Play();
                }

                if (this.SelectionLength > 0)
                    this.SelectionLength = 0;
            }
            else
            {
                this.Replace(text, this.SelectionStart, this.SelectionLength);
            }
        }

        protected override void ValidateValue(object value)
        {
            base.ValidateValue(value);

            // Validate if it fits in the mask
            if (!m_maskIsNull)
            {
                string representation = this.GetTextFromValue(value);

                MaskedTextProvider provider = m_maskedTextProvider.Clone() as MaskedTextProvider;

                if (!provider.VerifyString(representation))
                    throw new ArgumentException("The value representation '" + representation + "' does not match the mask.", "value");
            }
        }

        #endregion PROTECTED METHODS


        #region INTERNAL PROPERTIES

        internal bool IsForcingMask
        {
            get
            {
                return m_forcingMask;
            }
        }

        internal string FormatSpecifier
        {
            get
            {
                return m_formatSpecifier;
            }
            set
            {
                m_formatSpecifier = value;
            }
        }

        internal override bool IsTextReadyToBeParsed
        {
            get
            {
                return this.IsMaskCompleted;
            }
        }

        internal override bool GetIsEditTextEmpty()
        {
            if (!m_maskIsNull)
                return (this.MaskedTextProvider.AssignedEditPositionCount == 0);
            return true;
        }

        #endregion INTERNAL PROPERTIES

        #region INTERNAL METHODS

        internal override string GetCurrentText()
        {
            if (m_maskIsNull)
                return base.GetCurrentText();

            string displayText = this.GetFormattedString(m_maskedTextProvider, this.Text);

            return displayText;
        }

        internal override string GetParsableText()
        {
            if (m_maskIsNull)
                return base.GetParsableText();

            bool includePrompt = false;
            bool includeLiterals = true;

            if (this.ValueDataType == typeof(string))
            {
                includePrompt = this.IncludePromptInValue;
                includeLiterals = this.IncludeLiteralsInValue;
            }

            return m_maskedTextProvider
              .ToString(false, includePrompt, includeLiterals, 0, m_maskedTextProvider.Length);
        }

        internal override void OnFormatProviderChanged()
        {
            MaskedTextProvider provider = new MaskedTextProvider(this.Mask);

            m_maskedTextProvider = provider;

            this.RefreshConversionHelpers();
            this.RefreshCurrentText(true);

            base.OnFormatProviderChanged();
        }

        internal override void RefreshConversionHelpers()
        {
            Type type = this.ValueDataType;

            if ((type == null) || (!this.IsNumericValueDataType))
            {
                m_formatSpecifier = null;
                m_valueToStringMethodInfo = null;
                m_unhandledLiteralsPositions = null;
                return;
            }

            m_valueToStringMethodInfo = type.GetMethod("ToString", new Type[] { typeof(string), typeof(IFormatProvider) });

            string mask = m_maskedTextProvider.Mask;
            IFormatProvider activeFormatProvider = this.GetActiveFormatProvider();

            char[] maskChars = this.GetMaskCharacters();

            List<int> unhandledLiteralsPositions;

            m_formatSpecifier = MaskedTextBox.GetFormatSpecifierFromMask(
              mask,
              maskChars,
              activeFormatProvider,
              this.IncludeLiteralsInValue,
              out unhandledLiteralsPositions);

            NumberFormatInfo numberFormatInfo = activeFormatProvider.GetFormat(typeof(NumberFormatInfo)) as NumberFormatInfo;

            if (numberFormatInfo != null)
            {
                string negativeSign = numberFormatInfo.NegativeSign;

                if (m_formatSpecifier.Contains(negativeSign))
                {
                    // We must make sure that the value data type is numeric since we are about to 
                    // set the format specifier to its Positive,Negative,Zero format pattern.
                    // If we do not do this, the negative symbol would double itself when IncludeLiteralsInValue
                    // is set to True and a negative symbol is added to the mask as a literal.
                    Debug.Assert(this.IsNumericValueDataType);

                    m_formatSpecifier = m_formatSpecifier + ";" + m_formatSpecifier + ";" + m_formatSpecifier;
                }
                else
                {

                }
            }

            m_unhandledLiteralsPositions = unhandledLiteralsPositions;
        }

        internal void SetValueToStringMethodInfo(MethodInfo valueToStringMethodInfo)
        {
            m_valueToStringMethodInfo = valueToStringMethodInfo;
        }

        internal void ForceMask(string mask)
        {
            m_forcingMask = true;

            try
            {
                this.Mask = mask;
            }
            finally
            {
                m_forcingMask = false;
            }
        }

        #endregion INTERNAL METHODS

        #region PRIVATE PROPERTIES

        private bool IsOverwriteMode
        {
            get
            {
                if (!m_maskIsNull)
                {
                    switch (this.InsertKeyMode)
                    {
                        case InsertKeyMode.Default:
                            {
                                return m_insertToggled;
                            }

                        case InsertKeyMode.Insert:
                            {
                                return false;
                            }

                        case InsertKeyMode.Overwrite:
                            {
                                return true;
                            }
                    }
                }

                return false;
            }
        }

        #endregion PRIVATE PROPERTIES

        #region PRIVATE METHODS

        private bool PlaceChar(char ch, int startPosition, int length, bool overwrite, out int caretIndex)
        {
            return this.PlaceChar(m_maskedTextProvider, ch, startPosition, length, overwrite, out caretIndex);
        }


        private bool PlaceChar(MaskedTextProvider provider, char ch, int startPosition, int length, bool overwrite, out int caretPosition)
        {
            if (this.ShouldQueryAutoCompleteMask(provider.Clone() as MaskedTextProvider, ch, startPosition))
            {
                AutoCompletingMaskEventArgs e = new AutoCompletingMaskEventArgs(
                  m_maskedTextProvider.Clone() as MaskedTextProvider,
                  startPosition,
                  length,
                  ch.ToString());

                this.OnAutoCompletingMask(e);

                if ((!e.Cancel) && (e.AutoCompleteStartPosition > -1))
                {
                    caretPosition = startPosition;

                    // AutoComplete the block.
                    for (int i = 0; i < e.AutoCompleteText.Length; i++)
                    {
                        if (!this.PlaceCharCore(provider, e.AutoCompleteText[i], e.AutoCompleteStartPosition + i, 0, true, out caretPosition))
                            return false;
                    }

                    caretPosition = e.AutoCompleteStartPosition + e.AutoCompleteText.Length;
                    return true;
                }
            }

            return this.PlaceCharCore(provider, ch, startPosition, length, overwrite, out caretPosition);
        }

        private bool ShouldQueryAutoCompleteMask(MaskedTextProvider provider, char ch, int startPosition)
        {
            if (provider.IsEditPosition(startPosition))
            {
                int nextSeparatorIndex = provider.FindNonEditPositionFrom(startPosition, true);

                if (nextSeparatorIndex != -1)
                {
                    if (provider[nextSeparatorIndex].Equals(ch))
                    {
                        int previousSeparatorIndex = provider.FindNonEditPositionFrom(startPosition, false);

                        if (provider.FindUnassignedEditPositionInRange(previousSeparatorIndex, nextSeparatorIndex, true) != -1)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        protected virtual void OnAutoCompletingMask(AutoCompletingMaskEventArgs e)
        {
            if (this.AutoCompletingMask != null)
                this.AutoCompletingMask(this, e);
        }

        public event EventHandler<AutoCompletingMaskEventArgs> AutoCompletingMask;


        private bool PlaceCharCore(MaskedTextProvider provider, char ch, int startPosition, int length, bool overwrite, out int caretPosition)
        {
            caretPosition = startPosition;

            if (startPosition < m_maskedTextProvider.Length)
            {
                MaskedTextResultHint notUsed;

                if (length > 0)
                {
                    int endPosition = (startPosition + length) - 1;
                    return provider.Replace(ch, startPosition, endPosition, out caretPosition, out notUsed);
                }

                if (overwrite)
                    return provider.Replace(ch, startPosition, out caretPosition, out notUsed);

                return provider.InsertAt(ch, startPosition, out caretPosition, out notUsed);
            }

            return false;
        }

        internal void Replace(string text, int startPosition, int selectionLength)
        {
            MaskedTextProvider provider = (MaskedTextProvider)m_maskedTextProvider.Clone();
            int tentativeCaretIndex;

            if (this.CanReplace(provider, text, startPosition, selectionLength, this.RejectInputOnFirstFailure, out tentativeCaretIndex))
            {
                System.Diagnostics.Debug.WriteLine("Replace caret index to: " + tentativeCaretIndex.ToString());

                bool mustRefreshText = this.MaskedTextOutput != provider.ToString();
                m_maskedTextProvider = provider;

                if (mustRefreshText)
                    this.RefreshCurrentText(false);

                this.CaretIndex = tentativeCaretIndex + 1;
            }
            else
            {
                if (this.BeepOnError)
                    System.Media.SystemSounds.Beep.Play();
            }
        }

        internal virtual bool CanReplace(MaskedTextProvider provider, string text, int startPosition, int selectionLength, bool rejectInputOnFirstFailure, out int tentativeCaretIndex)
        {
            int endPosition = (startPosition + selectionLength) - 1;
            tentativeCaretIndex = -1;


            bool success = false;

            foreach (char ch in text)
            {
                if (!m_maskedTextProvider.VerifyEscapeChar(ch, startPosition))
                {
                    int editPositionFrom = provider.FindEditPositionFrom(startPosition, true);

                    if (editPositionFrom == MaskedTextProvider.InvalidIndex)
                        break;

                    startPosition = editPositionFrom;
                }

                int length = (endPosition >= startPosition) ? 1 : 0;
                bool overwrite = length > 0;

                if (this.PlaceChar(provider, ch, startPosition, length, overwrite, out tentativeCaretIndex))
                {
                    // Only one successfully inserted character is enough to declare the replace operation successful.
                    success = true;

                    startPosition = tentativeCaretIndex + 1;
                }
                else if (rejectInputOnFirstFailure)
                {
                    return false;
                }
            }

            if ((selectionLength > 0) && (startPosition <= endPosition))
            {

                // Erase the remaining of the assigned edit character.
                int notUsed;
                MaskedTextResultHint notUsedHint;
                if (!provider.RemoveAt(startPosition, endPosition, out notUsed, out notUsedHint))
                    success = false;
            }

            return success;
        }

        private bool CanDelete(int startPosition, int selectionLength, bool deleteForward, MaskedTextProvider provider)
        {
            if (this.IsReadOnly)
                return false;


            if (selectionLength == 0)
            {
                if (!deleteForward)
                {
                    if (startPosition == 0)
                        return false;

                    startPosition--;
                }
                else if ((startPosition + selectionLength) == provider.Length)
                {
                    return false;
                }
            }

            MaskedTextResultHint notUsed;
            int tentativeCaretPosition = startPosition;

            int endPosition = (selectionLength > 0) ? ((startPosition + selectionLength) - 1) : startPosition;

            bool success = provider.RemoveAt(startPosition, endPosition, out tentativeCaretPosition, out notUsed);

            return success;
        }

        private void Delete(int startPosition, int selectionLength, bool deleteForward)
        {
            if (this.IsReadOnly)
                return;


            if (selectionLength == 0)
            {
                if (!deleteForward)
                {
                    if (startPosition == 0)
                        return;

                    startPosition--;
                }
                else if ((startPosition + selectionLength) == m_maskedTextProvider.Length)
                {
                    return;
                }
            }

            MaskedTextResultHint hint;
            int tentativeCaretPosition = startPosition;

            int endPosition = (selectionLength > 0) ? ((startPosition + selectionLength) - 1) : startPosition;

            string oldTextOutput = this.MaskedTextOutput;

            bool success = m_maskedTextProvider.RemoveAt(startPosition, endPosition, out tentativeCaretPosition, out hint);

            if (!success)
            {
                if (this.BeepOnError)
                    System.Media.SystemSounds.Beep.Play();

                return;
            }

            if (this.MaskedTextOutput != oldTextOutput)
            {
                this.RefreshCurrentText(false);
            }
            else if (selectionLength > 0)
            {
                tentativeCaretPosition = startPosition;
            }
            else if (hint == MaskedTextResultHint.NoEffect)
            {
                if (deleteForward)
                {
                    tentativeCaretPosition = m_maskedTextProvider.FindEditPositionFrom(startPosition, true);
                }
                else
                {
                    if (m_maskedTextProvider.FindAssignedEditPositionFrom(startPosition, true) == MaskedTextProvider.InvalidIndex)
                    {
                        tentativeCaretPosition = m_maskedTextProvider.FindAssignedEditPositionFrom(startPosition, false);
                    }
                    else
                    {
                        tentativeCaretPosition = m_maskedTextProvider.FindEditPositionFrom(startPosition, false);
                    }

                    if (tentativeCaretPosition != MaskedTextProvider.InvalidIndex)
                        tentativeCaretPosition++;
                }

                if (tentativeCaretPosition == MaskedTextProvider.InvalidIndex)
                    tentativeCaretPosition = startPosition;
            }
            else if (!deleteForward)
            {
                tentativeCaretPosition = startPosition;
            }

            this.CaretIndex = tentativeCaretPosition;
        }

        private string MaskedTextOutput
        {
            get
            {
                System.Diagnostics.Debug.Assert(m_maskedTextProvider.EditPositionCount > 0);

                return m_maskedTextProvider.ToString();
            }
        }

        private string GetRawText()
        {
            if (m_maskIsNull)
                return this.Text;

            return MaskedTextBox.GetRawText(m_maskedTextProvider);
        }

        private string GetFormattedString(MaskedTextProvider provider, string text)
        {
            //System.Diagnostics.Debug.Assert( provider.EditPositionCount > 0 );


            bool includePrompt = (this.IsReadOnly) ? false : (!this.HidePromptOnLeave || this.IsFocused);

            string displayString = provider.ToString(false, includePrompt, true, 0, m_maskedTextProvider.Length);

            if (provider.Mask.StartsWith(">"))
                return displayString.ToUpper();
            if (provider.Mask.StartsWith("<"))
                return displayString.ToLower();

            return displayString;
        }

        private string GetSelectedText()
        {
            System.Diagnostics.Debug.Assert(!m_maskIsNull);

            int selectionLength = this.SelectionLength;

            if (selectionLength == 0)
                return string.Empty;

            bool includePrompt = (this.ClipboardMaskFormat & MaskFormat.IncludePrompt) != MaskFormat.ExcludePromptAndLiterals;
            bool includeLiterals = (this.ClipboardMaskFormat & MaskFormat.IncludeLiterals) != MaskFormat.ExcludePromptAndLiterals;

            return m_maskedTextProvider.ToString(true, includePrompt, includeLiterals, this.SelectionStart, selectionLength);
        }

        #endregion PRIVATE METHODS

        #region PRIVATE FIELDS

        private MaskedTextProvider m_maskedTextProvider; // = null;
        private bool m_insertToggled; // = false;
        private bool m_maskIsNull = true;
        private bool m_forcingMask; // = false;

        List<int> m_unhandledLiteralsPositions; // = null;
        private string m_formatSpecifier;
        private MethodInfo m_valueToStringMethodInfo; // = null;

        #endregion PRIVATE FIELDS
    }
}
