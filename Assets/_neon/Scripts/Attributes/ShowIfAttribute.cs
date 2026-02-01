using System;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Conditionally shows a serialized field in the Inspector based on the value of another field.
    /// When the condition field's value matches any of the compare values, the decorated field is shown.
    /// </summary>
    /// <example>
    /// [ShowIf("TriggerType", WaveTriggerType.ProgressionPercent)]
    /// public float TriggerProgressionPercent;
    ///
    /// [ShowIf("HasCameraBound")] // shorthand for comparing with true
    /// public float CameraBoundProgression;
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    public class ShowIfAttribute : PropertyAttribute
    {
        public string ConditionField { get; }
        public object[] CompareValues { get; }

        /// <summary>
        /// Shows the field when the condition field matches any of the provided values.
        /// </summary>
        /// <param name="conditionField">Name of the sibling field to compare against.</param>
        /// <param name="compareValues">Values that make this field visible. For enums, pass the enum value directly.</param>
        public ShowIfAttribute(string conditionField, params object[] compareValues)
        {
            ConditionField = conditionField;
            CompareValues = compareValues.Length > 0 ? compareValues : new object[] { true };
        }
    }
}
