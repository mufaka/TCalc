namespace TCalc.Web.Services;

/// <summary>
/// Recursive-descent parser and evaluator for mathematical expressions.
/// Supports: +, -, *, /, ^ (right-associative), parentheses, unary ±,
/// implicit multiplication (e.g. 2π, 3(4)), constants (π, e),
/// and scientific functions (sin, cos, tan, asin, acos, atan, log, ln,
/// sqrt, cbrt, abs, exp, ceil, floor, round, factorial/!).
/// </summary>
public sealed class CalculatorEngine : ICalculatorEngine
{
    private readonly ILogger<CalculatorEngine> _logger;

    public CalculatorEngine(ILogger<CalculatorEngine> logger) => _logger = logger;

    private static readonly Dictionary<string, Func<double, double>> UnaryFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sin"]   = Math.Sin,
        ["cos"]   = Math.Cos,
        ["tan"]   = Math.Tan,
        ["asin"]  = Math.Asin,
        ["acos"]  = Math.Acos,
        ["atan"]  = Math.Atan,
        ["log"]   = Math.Log10,
        ["ln"]    = Math.Log,
        ["sqrt"]  = Math.Sqrt,
        ["cbrt"]  = Math.Cbrt,
        ["abs"]   = Math.Abs,
        ["exp"]   = Math.Exp,
        ["ceil"]  = Math.Ceiling,
        ["floor"] = Math.Floor,
        ["round"] = Math.Round,
    };

    private static readonly Dictionary<string, Func<double, double, double>> BinaryFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pow"] = Math.Pow,
        ["mod"] = (a, b) => a % b,
    };

    private static readonly Dictionary<string, double> Constants = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pi"] = Math.PI,
        ["π"]  = Math.PI,
        ["e"]  = Math.E,
    };

    // ─── public API ───────────────────────────────────────────────

    public CalculationResult Evaluate(string expression, AngleMode angleMode = AngleMode.Radians)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return CalculationResult.Fail("Expression is empty.", expression);

        try
        {
            var parser = new Parser(expression, angleMode);
            double result = parser.ParseExpression();
            parser.ExpectEnd();

            if (double.IsNaN(result))
                return CalculationResult.Fail("Result is undefined (NaN).", expression);
            if (double.IsPositiveInfinity(result))
                return CalculationResult.Fail("Result is positive infinity.", expression);
            if (double.IsNegativeInfinity(result))
                return CalculationResult.Fail("Result is negative infinity.", expression);

            _logger.LogDebug("Evaluated '{Expression}' ({AngleMode}) = {Result}", expression, angleMode, result);
            return CalculationResult.Ok(result, expression);
        }
        catch (CalculatorException ex)
        {
            _logger.LogWarning("Evaluation failed for '{Expression}': {Error}", expression, ex.Message);
            return CalculationResult.Fail(ex.Message, expression);
        }
    }

    public double EvaluateFunction(string name, double[] args, AngleMode angleMode = AngleMode.Radians)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 1 && UnaryFunctions.TryGetValue(name, out var unary))
        {
            double a = ConvertAngleInput(name, args[0], angleMode);
            return ConvertAngleOutput(name, unary(a), angleMode);
        }

        if (args.Length == 2 && BinaryFunctions.TryGetValue(name, out var binary))
            return binary(args[0], args[1]);

        if (name.Equals("factorial", StringComparison.OrdinalIgnoreCase) && args.Length == 1)
            return Factorial(args[0]);

        throw new CalculatorException($"Unknown function '{name}' with {args.Length} argument(s).");
    }

    // ─── angle helpers ────────────────────────────────────────────

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180.0);
    private static double ToDegrees(double radians) => radians * (180.0 / Math.PI);

    /// <summary>Convert input angle for trig functions when in degree mode.</summary>
    private static double ConvertAngleInput(string funcName, double value, AngleMode mode)
    {
        if (mode == AngleMode.Degrees && IsTrigInput(funcName))
            return ToRadians(value);
        return value;
    }

    /// <summary>Convert output angle for inverse trig functions when in degree mode.</summary>
    private static double ConvertAngleOutput(string funcName, double value, AngleMode mode)
    {
        if (mode == AngleMode.Degrees && IsInverseTrig(funcName))
            return ToDegrees(value);
        return value;
    }

    private static bool IsTrigInput(string f) =>
        f.Equals("sin", StringComparison.OrdinalIgnoreCase) ||
        f.Equals("cos", StringComparison.OrdinalIgnoreCase) ||
        f.Equals("tan", StringComparison.OrdinalIgnoreCase);

    private static bool IsInverseTrig(string f) =>
        f.Equals("asin", StringComparison.OrdinalIgnoreCase) ||
        f.Equals("acos", StringComparison.OrdinalIgnoreCase) ||
        f.Equals("atan", StringComparison.OrdinalIgnoreCase);

    // ─── factorial ────────────────────────────────────────────────

    internal static double Factorial(double n)
    {
        if (n < 0)
            throw new CalculatorException("Factorial is not defined for negative numbers.");
        if (n != Math.Floor(n))
            throw new CalculatorException("Factorial is only defined for non-negative integers.");
        if (n > 170)
            throw new CalculatorException("Factorial input too large (max 170).");

        double result = 1;
        for (int i = 2; i <= (int)n; i++)
            result *= i;
        return result;
    }

    // ─── Parser (recursive descent) ──────────────────────────────

    private sealed class Parser
    {
        private readonly string _expression;
        private readonly AngleMode _angleMode;
        private int _pos;

        public Parser(string expression, AngleMode angleMode)
        {
            _expression = expression;
            _angleMode = angleMode;
            _pos = 0;
        }

        // Grammar:
        //   Expression  → Term   (('+' | '-') Term)*
        //   Term        → Power  (('*' | '/' | '%') Power)*     ← also implicit multiply
        //   Power       → Unary  ('^' Power)?                   ← right-associative
        //   Unary       → ('-' | '+') Unary | Postfix
        //   Postfix     → Primary ('!')*
        //   Primary     → Number | Constant | Function '(' Expr ')' | '(' Expr ')'

        public double ParseExpression()
        {
            double left = ParseTerm();
            while (true)
            {
                SkipWhitespace();
                if (Match('+')) left += ParseTerm();
                else if (Match('-')) left -= ParseTerm();
                else break;
            }
            return left;
        }

        public void ExpectEnd()
        {
            SkipWhitespace();
            if (_pos < _expression.Length)
                throw new CalculatorException($"Unexpected character '{_expression[_pos]}' at position {_pos + 1}.");
        }

        private double ParseTerm()
        {
            double left = ParsePower();
            while (true)
            {
                SkipWhitespace();
                if (Match('*') || Match('×'))
                    left *= ParsePower();
                else if (Match('/') || Match('÷'))
                {
                    double divisor = ParsePower();
                    left /= divisor; // IEEE 754: ±∞ or NaN, caught by Evaluate()
                }
                else if (Match('%'))
                {
                    double divisor = ParsePower();
                    left %= divisor; // IEEE 754: NaN when divisor is 0
                }
                else if (IsImplicitMultiply())
                {
                    left *= ParsePower();
                }
                else break;
            }
            return left;
        }

        private double ParsePower()
        {
            double baseVal = ParseUnary();
            SkipWhitespace();
            if (Match('^'))
            {
                double exp = ParsePower(); // right-associative
                return Math.Pow(baseVal, exp);
            }
            return baseVal;
        }

        private double ParseUnary()
        {
            SkipWhitespace();
            if (Match('-')) return -ParseUnary();
            if (Match('+')) return ParseUnary();
            return ParsePostfix();
        }

        private double ParsePostfix()
        {
            double value = ParsePrimary();
            SkipWhitespace();
            while (Match('!'))
            {
                value = Factorial(value);
                SkipWhitespace();
            }
            return value;
        }

        private double ParsePrimary()
        {
            SkipWhitespace();

            // Parenthesised sub-expression
            if (Match('('))
            {
                double value = ParseExpression();
                SkipWhitespace();
                if (!Match(')'))
                    throw new CalculatorException("Missing closing parenthesis.");
                return value;
            }

            // Number literal
            if (_pos < _expression.Length && (char.IsDigit(_expression[_pos]) || _expression[_pos] == '.'))
                return ParseNumber();

            // Named token (function or constant)
            if (_pos < _expression.Length && (char.IsLetter(_expression[_pos]) || _expression[_pos] == 'π'))
                return ParseNamedToken();

            if (_pos >= _expression.Length)
                throw new CalculatorException("Unexpected end of expression.");

            throw new CalculatorException($"Unexpected character '{_expression[_pos]}' at position {_pos + 1}.");
        }

        private double ParseNumber()
        {
            int start = _pos;
            while (_pos < _expression.Length && (char.IsDigit(_expression[_pos]) || _expression[_pos] == '.'))
                _pos++;

            // Scientific notation: E followed by optional +/- and digits (e.g. 1E-05)
            if (_pos < _expression.Length && _expression[_pos] == 'E')
            {
                int peek = _pos + 1;
                if (peek < _expression.Length && (_expression[peek] == '+' || _expression[peek] == '-'))
                    peek++;
                if (peek < _expression.Length && char.IsDigit(_expression[peek]))
                {
                    _pos = peek;
                    while (_pos < _expression.Length && char.IsDigit(_expression[_pos]))
                        _pos++;
                }
            }

            string token = _expression[start.._pos];
            if (!double.TryParse(token, System.Globalization.CultureInfo.InvariantCulture, out double value))
                throw new CalculatorException($"Invalid number '{token}'.");
            return value;
        }

        private double ParseNamedToken()
        {
            int start = _pos;
            // π is a single-char constant
            if (_expression[_pos] == 'π')
            {
                _pos++;
                return Math.PI;
            }

            while (_pos < _expression.Length && char.IsLetterOrDigit(_expression[_pos]))
                _pos++;

            string name = _expression[start.._pos];

            // Check for function call: name(...)
            SkipWhitespace();
            if (_pos < _expression.Length && _expression[_pos] == '(')
            {
                _pos++; // consume '('
                double arg1 = ParseExpression();

                SkipWhitespace();
                if (Match(','))
                {
                    // Two-argument function
                    double arg2 = ParseExpression();
                    SkipWhitespace();
                    if (!Match(')'))
                        throw new CalculatorException($"Missing closing parenthesis for function '{name}'.");

                    if (BinaryFunctions.TryGetValue(name, out var binaryFn))
                        return binaryFn(arg1, arg2);

                    throw new CalculatorException($"Unknown function '{name}' with 2 arguments.");
                }

                if (!Match(')'))
                    throw new CalculatorException($"Missing closing parenthesis for function '{name}'.");

                if (UnaryFunctions.TryGetValue(name, out var unaryFn))
                {
                    double a = ConvertAngleInput(name, arg1, _angleMode);
                    return ConvertAngleOutput(name, unaryFn(a), _angleMode);
                }

                if (name.Equals("factorial", StringComparison.OrdinalIgnoreCase))
                    return Factorial(arg1);

                throw new CalculatorException($"Unknown function '{name}'.");
            }

            // Constant
            if (Constants.TryGetValue(name, out double constVal))
                return constVal;

            throw new CalculatorException($"Unknown identifier '{name}'.");
        }

        // ─── helpers ──────────────────────────────────────────────

        private void SkipWhitespace()
        {
            while (_pos < _expression.Length && char.IsWhiteSpace(_expression[_pos]))
                _pos++;
        }

        private bool Match(char c)
        {
            if (_pos < _expression.Length && _expression[_pos] == c)
            {
                _pos++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Detects implicit multiplication: number followed by '(' or letter,
        /// e.g. "2π", "3(4+1)", ")(" , "π2".
        /// </summary>
        private bool IsImplicitMultiply()
        {
            if (_pos >= _expression.Length) return false;

            char next = _expression[_pos];
            if (next == '(' || char.IsLetter(next) || next == 'π')
            {
                // Look back to see if previous token was a number, ')', '!', or constant letter
                if (_pos > 0)
                {
                    char prev = _expression[_pos - 1];
                    if (char.IsDigit(prev) || prev == ')' || prev == '!' || prev == 'π')
                        return true;
                    // Handle cases like "pi(" — previous is a letter that ended a constant/number
                    // We already consumed the identifier so prev would be a letter
                }
            }
            return false;
        }
    }
}

public sealed class CalculatorException : Exception
{
    public CalculatorException(string message) : base(message) { }
}
