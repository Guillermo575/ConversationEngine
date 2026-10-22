using ConversationScheme;
using System;
namespace ConversationBox
{
    public static class Helper
    {
        #region Parsers
        public static Int32 ParseInt(string input) => Int32.TryParse(input, out var result) ? result : 0;
        public static decimal ParseDecimal(string input) => decimal.TryParse(input, out var result) ? result : 0;
        public static bool ParseBool(string input) => bool.TryParse(input, out var result) ? result : false;
        #endregion

        public static bool ProcessConditions(ConditionalBranch conditionalBranch)
        {
            if (conditionalBranch.Conditions == null || conditionalBranch.Conditions.Count == 0) return false;
            foreach (var objConditions in conditionalBranch.Conditions)
            {
                if (!ProcessRule(objConditions)) return false;
            }
            return true;
        }

        public static bool ProcessRule(ConditionRule conditionRule)
        {
            if (conditionRule == null) return false;
            switch (conditionRule.ValueDataType)
            {
                case ConversationScheme.ValueType.Integer: return CompareInteger(conditionRule);
                case ConversationScheme.ValueType.Decimal: return CompareDecimal(conditionRule);
                case ConversationScheme.ValueType.String: return CompareString(conditionRule);
                case ConversationScheme.ValueType.Boolean: return CompareBool(conditionRule);
            }
            return false;
        }

        #region Comparations
        public static string getDict(string input)
        {
            GameManager gameManager = GameManager.GetSingleton();
            return gameManager.getValueDict(input);
        }
        public static bool CompareInteger(ConditionRule conditionRule)
        {
            int A = ParseInt(getDict(conditionRule.VariableName));
            int B = ParseInt(conditionRule.IsValueVariable ? getDict(conditionRule.Value) : conditionRule.Value);
            switch (conditionRule.Operator)
            {
                case ComparisonOperator.Equal: return A == B;
                case ComparisonOperator.NotEqual: return A != B;
                case ComparisonOperator.GreaterThan: return A > B;
                case ComparisonOperator.GreaterOrEqual: return A >= B;
                case ComparisonOperator.LessThan: return A < B;
                case ComparisonOperator.LessOrEqual: return A <= B;
            }
            return false;
        }
        public static bool CompareDecimal(ConditionRule conditionRule)
        {
            Decimal A = ParseDecimal(getDict(conditionRule.VariableName));
            Decimal B = ParseDecimal(conditionRule.IsValueVariable ? getDict(conditionRule.Value) : conditionRule.Value);
            switch (conditionRule.Operator)
            {
                case ComparisonOperator.Equal: return A == B;
                case ComparisonOperator.NotEqual: return A != B;
                case ComparisonOperator.GreaterThan: return A > B;
                case ComparisonOperator.GreaterOrEqual: return A >= B;
                case ComparisonOperator.LessThan: return A < B;
                case ComparisonOperator.LessOrEqual: return A <= B;
            }
            return false;
        }
        public static bool CompareString(ConditionRule conditionRule)
        {
            string A = getDict(conditionRule.VariableName);
            string B = conditionRule.IsValueVariable ? getDict(conditionRule.Value) : conditionRule.Value;
            switch (conditionRule.Operator)
            {
                case ComparisonOperator.Equal: return A == B;
                case ComparisonOperator.NotEqual: return A != B;
            }
            return false;
        }
        public static bool CompareBool(ConditionRule conditionRule)
        {
            bool A = ParseBool(getDict(conditionRule.VariableName));
            bool B = ParseBool(conditionRule.IsValueVariable ? getDict(conditionRule.Value) : conditionRule.Value);
            switch (conditionRule.Operator)
            {
                case ComparisonOperator.Equal: return A == B;
                case ComparisonOperator.NotEqual: return A != B;
            }
            return false;
        }
        #endregion

    }
}