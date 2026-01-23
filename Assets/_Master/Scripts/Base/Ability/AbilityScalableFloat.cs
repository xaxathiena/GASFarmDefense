using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace _Master.Base.Ability
{
    /// <summary>
    /// A float value that can scale based on level using AnimationCurve (similar to UE's FScalableFloat)
    /// </summary>
    [Serializable]
    [MovedFrom(true, null, "_Master.Base.Ability", "AbilityScalableFloat")]
    public class ScalableFloat
    {
        [SerializeField] private ScalingMode scalingMode = ScalingMode.FlatValue;
        
        [SerializeField] private float flatValue = 0f;
        
        [Tooltip("Curve where X = Level, Y = Value")]
        [SerializeField] private AnimationCurve scalingCurve = AnimationCurve.Linear(1, 0, 10, 100);

        [Tooltip("CSV asset with first column = Level, other columns = CurveName")]
        [SerializeField] private TextAsset csvAsset;

        [Tooltip("Column name in CSV to build curve from")]
        [SerializeField] private string csvColumn;

        [Tooltip("Preview level for inspector")]
        [SerializeField] private float previewLevel = 1f;

        [NonSerialized] private int cachedTextHash;
        [NonSerialized] private string cachedColumn;
        [NonSerialized] private int cachedRowCount;
        [NonSerialized] private bool hasCachedCurve;
        
        [Tooltip("When Attribute is selected, value is read from owner's AttributeSet")]
        [SerializeField] private EGameplayAttributeType attributeType = EGameplayAttributeType.Health;

        public enum ScalingMode
        {
            FlatValue,      // Fixed value, không scale
            Curve,          // Scale theo AnimationCurve
            Attribute       // Read from AttributeSet
        }
        
        /// <summary>
        /// Get value at specified level (optional owner ASC for Attribute mode)
        /// </summary>
        public float GetValueAtLevel(float level, AbilitySystemComponent ownerAsc = null)
        {
            switch (scalingMode)
            {
                case ScalingMode.FlatValue:
                    return flatValue;
                
                case ScalingMode.Curve:
                    EnsureCurveFromCsv(false);
                    return scalingCurve.Evaluate(level);

                case ScalingMode.Attribute:
                    if (ownerAsc != null && ownerAsc.AttributeSet != null)
                    {
                        var attribute = ownerAsc.AttributeSet.GetAttribute(attributeType);
                        if (attribute != null)
                            return attribute.CurrentValue;
                    }
                    return 0f;
                
                default:
                    return flatValue;
            }
        }
        
        /// <summary>
        /// Get value at level 1 (base value)
        /// </summary>
        public float GetBaseValue(AbilitySystemComponent ownerAsc = null)
        {
            return GetValueAtLevel(1f, ownerAsc);
        }
        
        /// <summary>
        /// Implicit conversion to float (uses level 1)
        /// </summary>
        public static implicit operator float(ScalableFloat scalable)
        {
            return scalable?.GetBaseValue() ?? 0f;
        }
        
        /// <summary>
        /// Constructor for flat value
        /// </summary>
        public ScalableFloat(float value)
        {
            scalingMode = ScalingMode.FlatValue;
            flatValue = value;
        }
        
        /// <summary>
        /// Constructor for curve
        /// </summary>
        public ScalableFloat(AnimationCurve curve)
        {
            scalingMode = ScalingMode.Curve;
            scalingCurve = curve;
        }
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public ScalableFloat()
        {
            scalingMode = ScalingMode.FlatValue;
            flatValue = 0f;
        }

        public TextAsset CsvAsset => csvAsset;
        public string CsvColumn => csvColumn;
        public float PreviewLevel => previewLevel;
        public int CachedRowCount => cachedRowCount;
        public EGameplayAttributeType AttributeType => attributeType;

        public void SetCsvSource(TextAsset asset, string column)
        {
            csvAsset = asset;
            csvColumn = column;
        }

        public void EnsureCurveFromCsv(bool force)
        {
            if (scalingMode != ScalingMode.Curve)
                return;

            if (csvAsset == null || string.IsNullOrEmpty(csvColumn))
                return;

            string csvText = csvAsset.text;
            if (string.IsNullOrEmpty(csvText))
                return;

            int textHash = ComputeStableHash(csvText);
            if (!force && hasCachedCurve && cachedTextHash == textHash && cachedColumn == csvColumn)
                return;

            if (CsvCurveTable.TryBuildCurve(csvText, csvColumn, out AnimationCurve curve, out int rowCount))
            {
                scalingCurve = curve;
                cachedTextHash = textHash;
                cachedColumn = csvColumn;
                cachedRowCount = rowCount;
                hasCachedCurve = true;
            }
        }

        public void RebuildCurveFromCsv()
        {
            EnsureCurveFromCsv(true);
        }

        public float GetPreviewValue()
        {
            return GetValueAtLevel(previewLevel);
        }

        private static int ComputeStableHash(string text)
        {
            unchecked
            {
                int hash = 23;
                for (int i = 0; i < text.Length; i++)
                {
                    hash = (hash * 31) + text[i];
                }
                return hash;
            }
        }
    }
}
