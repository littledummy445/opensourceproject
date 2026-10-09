using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace HandwritingCalculatorUwp;

public sealed partial class MainPage : Page
{
    private readonly ObservableCollection<string> _history = new();
    private string _expression = "0";
    private double _memory;

    public MainPage()
    {
        InitializeComponent();
        HistoryList.ItemsSource = _history;

        InkPad.InkPresenter.InputDeviceTypes =
            CoreInputDeviceTypes.Mouse |
            CoreInputDeviceTypes.Pen |
            CoreInputDeviceTypes.Touch;

        var attributes = new InkDrawingAttributes
        {
            Color = Windows.UI.Colors.DeepSkyBlue,
            IgnorePressure = false,
            FitToCurve = true,
            Size = new Windows.Foundation.Size(3, 3)
        };
        InkPad.InkPresenter.UpdateDefaultDrawingAttributes(attributes);
    }

    private void DigitClick(object sender, RoutedEventArgs e)
    {
        AppendToken(GetButtonToken(sender));
    }

    private void OperatorClick(object sender, RoutedEventArgs e)
    {
        var token = NormalizeOperator(GetButtonToken(sender));

        if (token is "(" or ")")
        {
            AppendToken(token);
            return;
        }

        if (_expression.Length == 0)
        {
            _expression = "0";
        }

        if (IsOperator(_expression[^1]))
        {
            _expression = _expression[..^1] + token;
        }
        else
        {
            _expression += token;
        }

        UpdateDisplay();
    }

    private void DecimalClick(object sender, RoutedEventArgs e)
    {
        var currentNumber = GetCurrentNumber();
        if (currentNumber.Contains('.'))
        {
            return;
        }

        if (_expression == "0" || IsOperator(_expression[^1]) || _expression[^1] == '(')
        {
            AppendToken("0.");
            return;
        }

        AppendToken(".");
    }

    private void EqualsClick(object sender, RoutedEventArgs e)
    {
        TryEvaluateAndSetExpression(_expression, fromHandwriting: false);
    }

    private void BackspaceClick(object sender, RoutedEventArgs e)
    {
        if (_expression.Length <= 1)
        {
            _expression = "0";
            UpdateDisplay();
            return;
        }

        _expression = _expression[..^1];
        UpdateDisplay();
    }

    private void ClearClick(object sender, RoutedEventArgs e)
    {
        _expression = "0";
        UpdateDisplay();
    }

    private void MemoryClearClick(object sender, RoutedEventArgs e)
    {
        _memory = 0;
    }

    private void MemoryRecallClick(object sender, RoutedEventArgs e)
    {
        _expression = _memory.ToString(CultureInfo.InvariantCulture);
        UpdateDisplay();
    }

    private void MemoryAddClick(object sender, RoutedEventArgs e)
    {
        if (TryEvaluateExpression(_expression, out var value))
        {
            _memory += value;
        }
    }

    private void MemorySubtractClick(object sender, RoutedEventArgs e)
    {
        if (TryEvaluateExpression(_expression, out var value))
        {
            _memory -= value;
        }
    }

    private async void RecognizeAndCalculateClick(object sender, RoutedEventArgs e)
    {
        RecognizedTextBlock.Text = "Recognizing handwriting...";
        var recognizedExpression = await RecognizeInkExpressionAsync();

        if (string.IsNullOrWhiteSpace(recognizedExpression))
        {
            RecognizedTextBlock.Text = "No expression recognized.";
            return;
        }

        RecognizedTextBlock.Text = $"Recognized: {recognizedExpression}";
        TryEvaluateAndSetExpression(recognizedExpression, fromHandwriting: true);
    }

    private void ClearInkClick(object sender, RoutedEventArgs e)
    {
        InkPad.InkPresenter.StrokeContainer.Clear();
        RecognizedTextBlock.Text = "Write a math expression above.";
    }

    private void AppendToken(string token)
    {
        if (_expression == "0" && token is not "(" and token != "0.")
        {
            _expression = token;
        }
        else
        {
            _expression += token;
        }

        UpdateDisplay();
    }

    private string GetCurrentNumber()
    {
        var index = _expression.Length - 1;

        while (index >= 0)
        {
            var c = _expression[index];
            if (!char.IsDigit(c) && c != '.')
            {
                break;
            }

            index--;
        }

        return _expression[(index + 1)..];
    }

    private void TryEvaluateAndSetExpression(string rawExpression, bool fromHandwriting)
    {
        var expression = PrepareExpression(rawExpression);
        if (!TryEvaluateExpression(expression, out var result))
        {
            DisplayBox.Text = "Error";
            return;
        }

        var formattedResult = result.ToString("G15", CultureInfo.InvariantCulture);
        if (fromHandwriting)
        {
            _history.Insert(0, $"🖊 {expression} = {formattedResult}");
        }
        else
        {
            _history.Insert(0, $"{expression} = {formattedResult}");
        }

        _expression = formattedResult;
        UpdateDisplay();
    }

    private static string PrepareExpression(string expression)
    {
        return expression
            .Replace("×", "*")
            .Replace("÷", "/")
            .Replace("x", "*", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty);
    }

    private static string NormalizeOperator(string token)
    {
        return token switch
        {
            "×" => "*",
            "÷" => "/",
            _ => token,
        };
    }

    private static bool IsOperator(char c)
    {
        return c is '+' or '-' or '*' or '/';
    }

    private void UpdateDisplay()
    {
        DisplayBox.Text = _expression;
    }

    private static bool TryEvaluateExpression(string expression, out double result)
    {
        result = 0;

        try
        {
            var values = new Stack<double>();
            var operators = new Stack<char>();

            for (var i = 0; i < expression.Length; i++)
            {
                var current = expression[i];

                if (char.IsWhiteSpace(current))
                {
                    continue;
                }

                if (char.IsDigit(current) || current == '.')
                {
                    var numberBuilder = new StringBuilder();
                    while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
                    {
                        numberBuilder.Append(expression[i]);
                        i++;
                    }

                    i--;
                    values.Push(double.Parse(numberBuilder.ToString(), CultureInfo.InvariantCulture));
                    continue;
                }

                if (current == '(')
                {
                    operators.Push(current);
                    continue;
                }

                if (current == ')')
                {
                    while (operators.Count > 0 && operators.Peek() != '(')
                    {
                        ComputeTopOperation(values, operators);
                    }

                    if (operators.Count == 0 || operators.Pop() != '(')
                    {
                        return false;
                    }

                    continue;
                }

                if (!IsOperator(current))
                {
                    return false;
                }

                if (current == '-' && (i == 0 || expression[i - 1] == '(' || IsOperator(expression[i - 1])))
                {
                    values.Push(0);
                }

                while (operators.Count > 0 && Precedence(operators.Peek()) >= Precedence(current))
                {
                    ComputeTopOperation(values, operators);
                }

                operators.Push(current);
            }

            while (operators.Count > 0)
            {
                if (operators.Peek() == '(')
                {
                    return false;
                }

                ComputeTopOperation(values, operators);
            }

            if (values.Count != 1)
            {
                return false;
            }

            result = values.Pop();
            return !double.IsInfinity(result) && !double.IsNaN(result);
        }
        catch
        {
            return false;
        }
    }

    private static void ComputeTopOperation(Stack<double> values, Stack<char> operators)
    {
        if (values.Count < 2 || operators.Count == 0)
        {
            throw new InvalidOperationException("Malformed expression.");
        }

        var right = values.Pop();
        var left = values.Pop();
        var op = operators.Pop();

        var computed = op switch
        {
            '+' => left + right,
            '-' => left - right,
            '*' => left * right,
            '/' when right != 0 => left / right,
            '/' => throw new DivideByZeroException(),
            _ => throw new InvalidOperationException("Unknown operator."),
        };

        values.Push(computed);
    }

    private static int Precedence(char op)
    {
        return op is '+' or '-' ? 1 : 2;
    }

    private async Task<string?> RecognizeInkExpressionAsync()
    {
        var strokeContainer = InkPad.InkPresenter.StrokeContainer;
        if (!strokeContainer.GetStrokes().Any())
        {
            return null;
        }

        var recognizer = new InkRecognizerContainer();
        var results = await recognizer.RecognizeAsync(strokeContainer, InkRecognitionTarget.All);
        var topResult = results.FirstOrDefault();

        if (topResult is null)
        {
            return null;
        }

        return PrepareExpression(topResult.GetTextCandidates().FirstOrDefault() ?? topResult.Text);
    }

    private static string GetButtonToken(object sender)
    {
        return (sender as Button)?.Content?.ToString() ?? string.Empty;
    }
}
