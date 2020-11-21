using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Visage.Runtime
{
    [AddComponentMenu("UI/Visage/Stat Bar", 0)]
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// A standard StatBar that can be moved between a minimum and maximum value.
    /// </summary>
    /// <remarks>
    /// The StatBar component is a Selectable that controls a fill, a handle, or both. The fill, when used, spans from the minimum value to the current value while the handle, when used, follow the current value.
    /// The anchors of the fill and handle RectTransforms are driven by the StatBar. The fill and handle can be direct children of the GameObject with the StatBar, or intermediary RectTransforms can be placed in between for additional control.
    /// When a change to the StatBar value occurs, a callback is sent to any registered listeners of UI.StatBar.onValueChanged.
    /// </remarks>
    public class StatBar : UIBehaviour, ICanvasElement
    { 
        /// <summary>
        /// Setting that indicates one of four directions.
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// From the left to the right
            /// </summary>
            LeftToRight,

            /// <summary>
            /// From the right to the left
            /// </summary>
            RightToLeft,

            /// <summary>
            /// From the bottom to the top.
            /// </summary>
            BottomToTop,

            /// <summary>
            /// From the top to the bottom.
            /// </summary>
            TopToBottom,
        }

        [Serializable]
        /// <summary>
        /// Event type used by the StatBar.
        /// </summary>
        public class StatBarEvent : UnityEvent<float> { }

        /// <summary>
        /// Optional RectTransform to use as fill for the StatBar.
        /// </summary>
        public RectTransform fillRect { get { return m_FillRect; } set { if (SetPropertyUtility.SetClass(ref m_FillRect, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        /// <summary>
        /// The direction of the StatBar, from minimum to maximum value.
        /// </summary>
        public Direction direction { get { return m_Direction; } set { if (SetPropertyUtility.SetStruct(ref m_Direction, value)) UpdateVisuals(); } }

        /// <summary>
        /// The minimum allowed value of the StatBar.
        /// </summary>
        public float minValue { get { return m_MinValue; } set { if (SetPropertyUtility.SetStruct(ref m_MinValue, value)) { Set(m_Value); UpdateVisuals(); } } }

        /// <summary>
        /// The maximum allowed value of the StatBar.
        /// </summary>
        public float maxValue { get { return m_MaxValue; } set { if (SetPropertyUtility.SetStruct(ref m_MaxValue, value)) { Set(m_Value); UpdateVisuals(); } } }

        /// <summary>
        /// Should the value only be allowed to be whole numbers?
        /// </summary>
        public bool wholeNumbers { get { return m_WholeNumbers; } set { if (SetPropertyUtility.SetStruct(ref m_WholeNumbers, value)) { Set(m_Value); UpdateVisuals(); } } }

        /// <summary>
        /// A label showing the current value with leading zeroes set
        /// </summary>
        public RectTransform ValueLabel{ get { return _valueLabel; } set { if (SetPropertyUtility.SetClass(ref _valueLabel, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        /// <summary>
        /// The total number of leading zeroes
        /// </summary>
        public int LeadingZeroes { get { return _leadingZeroes; } set { if (SetPropertyUtility.SetStruct(ref _leadingZeroes, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        public string LeadingZeroesString => _leadingZeroesString; 

        [SerializeField] private int _leadingZeroes = 2;
        [SerializeField] private RectTransform _valueLabel;

        [SerializeField] private RectTransform m_FillRect;
        [Space]
        [SerializeField] private Direction m_Direction = Direction.LeftToRight;
        [SerializeField] private float m_MinValue = 0;
        [SerializeField] private float m_MaxValue = 1;
        [SerializeField] private bool m_WholeNumbers = false;
        [SerializeField] protected float m_Value;

        /// <summary>
        /// The current value of the StatBar.
        /// </summary>
        public virtual float value
        {
            get
            {
                if (wholeNumbers)
                    return Mathf.Round(m_Value);
                return m_Value;
            }
            set
            {
                Set(value);
            }
        }

        /// <summary>
        /// Set the value of the StatBar without invoking onValueChanged callback.
        /// </summary>
        /// <param name="input">The new value for the StatBar.</param>
        public virtual void SetValueWithoutNotify(float input)
        {
            Set(input, false);
        }

        /// <summary>
        /// The current value of the StatBar normalized into a value between 0 and 1.
        /// </summary>
        public float normalizedValue
        {
            get
            {
                if (Mathf.Approximately(minValue, maxValue))
                    return 0;
                return Mathf.InverseLerp(minValue, maxValue, value);
            }
            set
            {
                this.value = Mathf.Lerp(minValue, maxValue, value);
            }
        }

        [Space]

        [SerializeField]
        private StatBarEvent m_OnValueChanged = new StatBarEvent();

        /// <summary>
        /// Callback executed when the value of the StatBar is changed.
        /// </summary>
        public StatBarEvent onValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }

        #region Private Fields
        private Image m_FillImage;
        private Transform m_FillTransform;
        private RectTransform m_FillContainerRect;

        private DrivenRectTransformTracker m_Tracker;

        private string _leadingZeroesString = "00";
        private object _valueLabelTextCmp; //Either Text or TMP_Text compoment

        // This "delayed" mechanism is required for case 1037681.
        private bool m_DelayedUpdateVisuals = false; 
        #endregion

        protected StatBar() { }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (wholeNumbers)
            {
                m_MinValue = Mathf.Round(m_MinValue);
                m_MaxValue = Mathf.Round(m_MaxValue);
            }

            //Onvalidate is called before OnEnabled. We need to make sure not to touch any other objects before OnEnable is run.
            if (IsActive())
            {
                UpdateCachedReferences();
                // Update rects in next update since other things might affect them even if value didn't change.
                m_DelayedUpdateVisuals = true;
                Set(m_Value, false);
            }

            if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this) && !Application.isPlaying)
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }

#endif // if UNITY_EDITOR

        public virtual void Rebuild(CanvasUpdate executing)
        {
#if UNITY_EDITOR
            if (executing == CanvasUpdate.Prelayout)
                onValueChanged.Invoke(value);
#endif
        }

        /// <summary>
        /// See ICanvasElement.LayoutComplete
        /// </summary>
        public virtual void LayoutComplete()
        { }

        /// <summary>
        /// See ICanvasElement.GraphicUpdateComplete
        /// </summary>
        public virtual void GraphicUpdateComplete()
        { }

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateCachedReferences();
            Set(m_Value, false);
            // Update rects since they need to be initialized correctly.
            UpdateVisuals();
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();
            base.OnDisable();
        }

        /// <summary>
        /// Update the rect based on the delayed update visuals.
        /// Got around issue of calling sendMessage from onValidate.
        /// </summary>
        protected virtual void Update()
        {
            if (m_DelayedUpdateVisuals)
            {
                m_DelayedUpdateVisuals = false;
                UpdateVisuals();
            }
        }

        protected override void OnDidApplyAnimationProperties()
        {
            // Has value changed? Various elements of the StatBar have the old normalisedValue assigned, we can use this to perform a comparison.
            // We also need to ensure the value stays within min/max.
            m_Value = ClampValue(m_Value);
            float oldNormalizedValue = normalizedValue;
            if (m_FillContainerRect != null)
            {
                if (m_FillImage != null && m_FillImage.type == Image.Type.Filled)
                    oldNormalizedValue = m_FillImage.fillAmount;
                else
                    oldNormalizedValue = (reverseValue ? 1 - m_FillRect.anchorMin[(int)axis] : m_FillRect.anchorMax[(int)axis]);
            }

            UpdateVisuals();

            if (oldNormalizedValue != normalizedValue)
            {
                UISystemProfilerApi.AddMarker("StatBar.value", this);
                onValueChanged.Invoke(m_Value);
            }
        }

        void UpdateCachedReferences()
        {
            if (m_FillRect && m_FillRect != (RectTransform)transform)
            {
                m_FillTransform = m_FillRect.transform;
                m_FillImage = m_FillRect.GetComponent<Image>();
                if (m_FillTransform.parent != null)
                    m_FillContainerRect = m_FillTransform.parent.GetComponent<RectTransform>();
            }
            else
            {
                m_FillRect = null;
                m_FillContainerRect = null;
                m_FillImage = null;
            }

            if (_valueLabel)
            {
                _valueLabelTextCmp = _valueLabel.GetComponent<Text>();
                if(_valueLabelTextCmp == null)
                {
                    _valueLabelTextCmp = _valueLabel.GetComponent<TMPro.TMP_Text>();
                }

                _leadingZeroesString = string.Empty;
                for (int zeroCount = _leadingZeroes; zeroCount > 0; zeroCount--)
                    _leadingZeroesString += "0";
            }
            else
            {
                _valueLabel = null;
            }
     
        }

        float ClampValue(float input)
        {
            float newValue = Mathf.Clamp(input, minValue, maxValue);
            if (wholeNumbers)
                newValue = Mathf.Round(newValue);
            return newValue;
        }

        /// <summary>
        /// Set the value of the StatBar.
        /// </summary>
        /// <param name="input">The new value for the StatBar.</param>
        /// <param name="sendCallback">If the OnValueChanged callback should be invoked.</param>
        /// <remarks>
        /// Process the input to ensure the value is between min and max value. If the input is different set the value and send the callback is required.
        /// </remarks>
        protected virtual void Set(float input, bool sendCallback = true)
        {
            // Clamp the input
            float newValue = ClampValue(input);

            // If the stepped value doesn't match the last one, it's time to update
            if (m_Value == newValue)
                return;

            m_Value = newValue;
            
            if (!m_DelayedUpdateVisuals)
            {
                UpdateVisuals();
            }

            if (sendCallback)
            {
                UISystemProfilerApi.AddMarker("StatBar.value", this);
                m_OnValueChanged.Invoke(newValue);
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            //This can be invoked before OnEnabled is called. So we shouldn't be accessing other objects, before OnEnable is called.
            if (!IsActive())
                return;

            UpdateVisuals();
        }

        enum Axis
        {
            Horizontal = 0,
            Vertical = 1
        }

        Axis axis { get { return (m_Direction == Direction.LeftToRight || m_Direction == Direction.RightToLeft) ? Axis.Horizontal : Axis.Vertical; } }
        bool reverseValue { get { return m_Direction == Direction.RightToLeft || m_Direction == Direction.TopToBottom; } }

        // Force-update the statBar. Useful if you've changed the properties and want it to update visually.
        private void UpdateVisuals()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateCachedReferences();
#endif

            m_Tracker.Clear();

            if (m_FillContainerRect != null)
            {
                m_Tracker.Add(this, m_FillRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;

                if (m_FillImage != null && m_FillImage.type == Image.Type.Filled)
                {
                    m_FillImage.fillAmount = normalizedValue;
                }
                else
                {
                    if (reverseValue)
                        anchorMin[(int)axis] = 1 - normalizedValue;
                    else
                        anchorMax[(int)axis] = normalizedValue;
                }

                m_FillRect.anchorMin = anchorMin;
                m_FillRect.anchorMax = anchorMax;
            }

            if (_valueLabel != null && _valueLabelTextCmp != null)
            {
                if(_valueLabelTextCmp is Text labelText)
                {
                    labelText.text = value.ToString(!wholeNumbers ? "" : LeadingZeroesString);
                }
                else if(_valueLabelTextCmp is TMPro.TMP_Text labelTmpText)
                {
                    labelTmpText.text = value.ToString(!wholeNumbers ? "" : LeadingZeroesString);
                }

            }
        }


        /// <summary>
        /// Sets the direction of this StatBar, optionally changing the layout as well.
        /// </summary>
        /// <param name="direction">The direction of the StatBar</param>
        /// <param name="includeRectLayouts">Should the layout be flipped together with the StatBar direction</param>
        public void SetDirection(Direction direction, bool includeRectLayouts)
        {
            Axis oldAxis = axis;
            bool oldReverse = reverseValue;
            this.direction = direction;

            if (!includeRectLayouts)
                return;

            if (axis != oldAxis)
                RectTransformUtility.FlipLayoutAxes(transform as RectTransform, true, true);

            if (reverseValue != oldReverse)
                RectTransformUtility.FlipLayoutOnAxis(transform as RectTransform, (int)axis, true, true);
        }
    }
    
}