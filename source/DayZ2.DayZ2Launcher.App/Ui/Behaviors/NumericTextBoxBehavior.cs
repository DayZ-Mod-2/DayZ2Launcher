using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace DayZ2.DayZ2Launcher.App.Ui.Behaviors
{
	/// <summary>
	///     Apply this behavior to a TextBox to ensure that it only accepts numeric values.
	///     The property <see cref="NumericTextBoxBehavior.AllowDecimal" /> controls whether or not
	///     the input is an integer or not.
	///     <para>
	///         A common requirement is to constrain the number count that appears after the decimal place.
	///         Setting <see cref="NumericTextBoxBehavior.DecimalLimit" /> specifies how many numbers appear here.
	///         If this value is 0, no limit is applied.
	///     </para>
	/// </summary>
	/// <remarks>
	///     In the view, this behavior is attached in the following way:
	///     <code>
	/// <TextBox Text="{Binding Price}">
	///             <i:Interaction.Behaviors>
	///                 <gl:NumericTextBoxBehavior AllowDecimal="False" />
	///             </i:Interaction.Behaviors>
	///         </TextBox>
	/// </code>
	///     <para>
	///         Add references to System.Windows.Interactivity to the view to use
	///         this behavior.
	///     </para>
	/// </remarks>
	public class NumericTextBoxBehavior : Behavior<TextBox>
	{
		private bool _allowDecimal = true;
		private int _decimalLimit;
		private bool _allowNegative = true;
		private string _pattern = string.Empty;

		/// <summary>
		///     Initialize a new instance of <see cref="NumericTextBoxBehavior" />.
		/// </summary>
		public NumericTextBoxBehavior()
		{
			AllowDecimal = true;
			AllowNegatives = true;
			DecimalLimit = 0;
		}

		/// <summary>
		///     Get or set whether the input allows decimal characters.
		/// </summary>
		public bool AllowDecimal
		{
			get { return _allowDecimal; }
			set
			{
				if (_allowDecimal == value) return;
				_allowDecimal = value;
				SetText();
			}
		}

		/// <summary>
		///     Get or set the maximum number of values to appear after
		///     the decimal.
		/// </summary>
		/// <remarks>
		///     If DecimalLimit is 0, then no limit is applied.
		/// </remarks>
		public int DecimalLimit
		{
			get { return _decimalLimit; }
			set
			{
				if (_decimalLimit == value) return;
				_decimalLimit = value;
				SetText();
			}
		}

		/// <summary>
		///     Get or set whether negative numbers are allowed.
		/// </summary>
		public bool AllowNegatives
		{
			get { return _allowNegative; }
			set
			{
				if (_allowNegative == value) return;
				_allowNegative = value;
				SetText();
			}
		}

		#region Overrides

		protected override void OnAttached()
		{
			base.OnAttached();

			AssociatedObject.PreviewTextInput += AssociatedObject_PreviewTextInput;
#if !SILVERLIGHT
			DataObject.AddPastingHandler(AssociatedObject, OnClipboardPaste);
#endif
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();
			AssociatedObject.PreviewTextInput -= AssociatedObject_PreviewTextInput;
#if !SILVERLIGHT
			DataObject.RemovePastingHandler(AssociatedObject, OnClipboardPaste);
#endif
		}

		#endregion

		#region Private methods

		private void SetText()
		{
			_pattern = string.Empty;
			GetRegularExpressionText();
		}

#if !SILVERLIGHT
		/// <summary>
		///     Handle paste operations into the textbox to ensure that the behavior
		///     is consistent with directly typing into the TextBox.
		/// </summary>
		/// <param name="sender">The TextBox sender.</param>
		/// <param name="dopea">Paste event arguments.</param>
		/// <remarks>This operation is only available in WPF.</remarks>
		private void OnClipboardPaste(object sender, DataObjectPastingEventArgs dopea)
		{
			string text = dopea.SourceDataObject.GetData(dopea.FormatToApply).ToString();

			if (!string.IsNullOrWhiteSpace(text) && !Validate(text))
				dopea.CancelCommand();
		}
#endif

		/// <summary>
		///     Preview the text input.
		/// </summary>
		/// <param name="sender">The TextBox sender.</param>
		/// <param name="e">The composition event arguments.</param>
		private void AssociatedObject_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = !Validate(e.Text);
		}

		/// <summary>
		///     Validate the contents of the textbox with the new content to see if it is
		///     valid.
		/// </summary>
		/// <param name="value">The text to validate.</param>
		/// <returns>True if this is valid, false otherwise.</returns>
		protected bool Validate(string value)
		{
			TextBox textBox = AssociatedObject;

			string pre = string.Empty;
			string post = string.Empty;

			if (!string.IsNullOrWhiteSpace(textBox.Text))
			{
				int selStart = textBox.SelectionStart;
				if (selStart > textBox.Text.Length)
					selStart--;
				pre = textBox.Text.Substring(0, selStart);
				post = textBox.Text.Substring(selStart + textBox.SelectionLength,
					textBox.Text.Length - (selStart + textBox.SelectionLength));
			}
			else
			{
				pre = textBox.Text.Substring(0, textBox.CaretIndex);
				post = textBox.Text.Substring(textBox.CaretIndex, textBox.Text.Length - textBox.CaretIndex);
			}
			string test = string.Concat(pre, value, post);

			string pattern = GetRegularExpressionText();

			return new Regex(pattern).IsMatch(test);
		}

		private string GetRegularExpressionText()
		{
			if (!string.IsNullOrWhiteSpace(_pattern))
			{
				return _pattern;
			}
			_pattern = GetPatternText();
			return _pattern;
		}

		private string GetPatternText()
		{
			string pattern = string.Empty;
			string signPattern = "[{0}+]";

			// If the developer has chosen to allow negative numbers, the pattern will be [-+].
			// If the developer chooses not to allow negatives, the pattern is [+].
			if (AllowNegatives)
			{
				signPattern = string.Format(signPattern, "-");
			}
			else
			{
				signPattern = string.Format(signPattern, string.Empty);
			}

			// If the developer doesn't allow decimals, return the pattern.
			if (!AllowDecimal)
			{
				return string.Format(@"^({0}?)(\d*)$", signPattern);
			}

			// If the developer has chosen to apply a decimal limit, the pattern matches
			// on a
			if (DecimalLimit > 0)
			{
				pattern = string.Format(@"^({2}?)(\d*)([{0}]?)(\d{{0,{1}}})$",
					NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator,
					DecimalLimit,
					signPattern);
			}
			else
			{
				pattern = string.Format(@"^({1}?)(\d*)([{0}]?)(\d*)$", NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator,
					signPattern);
			}

			return pattern;
		}

		#endregion
	}
}