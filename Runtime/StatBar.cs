using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Visage.StatBarUI.Runtime
{
    [AddComponentMenu("UI/Visage/Stat Bar", 0)]
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// A standard StatBar that can be moved between a minimum and maximum value.
    /// </summary>
    public class StatBar : UIBehaviour, ICanvasElement
    {
        public enum Transition
        {
            None,
            ColorTint
        }

        [System.Serializable]
        public class ColorTintTransition
        {
            public float percent;
            public Color color;
        }

        [SerializeField] private Transition _transition;
        [SerializeField] private ColorTintTransition _normalColorTintTransition = new ColorTintTransition
        {
            percent = 95,
            color = Color.red
        };
        [SerializeField] private ColorTintTransition _lowColorTintTransition = new ColorTintTransition
        {
            percent = 30,
            color = new Color(0.75f, 0f, 0f)
        };
        [SerializeField] private ColorTintTransition _criticalColorTintTransition = new ColorTintTransition
        {
            percent = 10,
            color = new Color(0.25f, 0f, 0f)
        };

        /// <summary>
        /// Setting that indicates one of four directions.
        /// </summary>
        public enum FillDirection
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
        public RectTransform FillRect { get { return _fillRect; } set { if (SetPropertyUtility.SetClass(ref _fillRect, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        /// <summary>
        /// The direction of the StatBar, from minimum to maximum value.
        /// </summary>
        public FillDirection Direction { get { return _direction; } set { if (SetPropertyUtility.SetStruct(ref _direction, value)) UpdateVisuals(); } }

        /// <summary>
        /// The minimum allowed value of the StatBar.
        /// </summary>
        public float MinValue { get { return _minValue; } set { if (SetPropertyUtility.SetStruct(ref _minValue, value)) { Set(_value); UpdateVisuals(); } } }

        /// <summary>
        /// The maximum allowed value of the StatBar.
        /// </summary>
        public float MaxValue { get { return _maxValue; } set { if (SetPropertyUtility.SetStruct(ref _maxValue, value)) { Set(_value); UpdateVisuals(); } } }

        /// <summary>
        /// Should the value only be allowed to be whole numbers?
        /// </summary>
        public bool WholeNumbers { get { return _wholeNumbers; } set { if (SetPropertyUtility.SetStruct(ref _wholeNumbers, value)) { Set(_value); UpdateVisuals(); } } }

        /// <summary>
        /// A label showing the current value with leading zeroes set
        /// </summary>
        public RectTransform ValueLabel { get { return _valueLabel; } set { if (SetPropertyUtility.SetClass(ref _valueLabel, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        /// <summary>
        /// The total number of leading zeroes
        /// </summary>
        public int LeadingZeroes { get { return _leadingZeroes; } set { if (SetPropertyUtility.SetStruct(ref _leadingZeroes, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        public string LeadingZeroesString => _leadingZeroesString;

        [SerializeField] private int _leadingZeroes = 2;
        [SerializeField] private RectTransform _valueLabel;

        [SerializeField] private RectTransform _fillRect;
        [Space]
        [SerializeField] private FillDirection _direction = FillDirection.LeftToRight;
        [SerializeField] private float _minValue = 0;
        [SerializeField] private float _maxValue = 1;
        [SerializeField] private bool _wholeNumbers = false;
        [SerializeField] protected float _value;

        public StatBar(float value)
        {
            _value = value;
        }

        /// <summary>
        /// The current value of the StatBar.
        /// </summary>
        public virtual float Value
        {
            get
            {
                if (WholeNumbers)
                    return Mathf.Round(_value);
                return _value;
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
                if (Mathf.Approximately(MinValue, MaxValue))
                    return 0;
                return Mathf.InverseLerp(MinValue, MaxValue, Value);
            }
            set
            {
                this.Value = Mathf.Lerp(MinValue, MaxValue, value);
            }
        }

        [Space]

        [SerializeField]
        private StatBarEvent _onValueChanged = new StatBarEvent();

        /// <summary>
        /// Callback executed when the value of the StatBar is changed.
        /// </summary>
        public StatBarEvent onValueChanged { get { return _onValueChanged; } set { _onValueChanged = value; } }

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

            if (WholeNumbers)
            {
                _minValue = Mathf.Round(_minValue);
                _maxValue = Mathf.Round(_maxValue);
            }

            //Onvalidate is called before OnEnabled. We need to make sure not to touch any other objects before OnEnable is run.
            if (IsActive())
            {
                UpdateCachedReferences();
                // Update rects in next update since other things might affect them even if value didn't change.
                m_DelayedUpdateVisuals = true;
                Set(_value, false);
            }

            if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this) && !Application.isPlaying)
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }

#endif // if UNITY_EDITOR

        public virtual void Rebuild(CanvasUpdate executing)
        {
#if UNITY_EDITOR
            if (executing == CanvasUpdate.Prelayout)
                onValueChanged.Invoke(Value);
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
            Set(_value, false);
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
            _value = ClampValue(_value);
            float oldNormalizedValue = normalizedValue;
            if (m_FillContainerRect != null)
            {
                if (m_FillImage != null && m_FillImage.type == Image.Type.Filled)
                    oldNormalizedValue = m_FillImage.fillAmount;
                else
                    oldNormalizedValue = reverseValue ? 1 - _fillRect.anchorMin[(int)axis] : _fillRect.anchorMax[(int)axis];
            }

            UpdateVisuals();

            if (oldNormalizedValue != normalizedValue)
            {
                UISystemProfilerApi.AddMarker("StatBar.value", this);
                onValueChanged.Invoke(_value);
            }
        }

        void UpdateCachedReferences()
        {
            if (_fillRect && _fillRect != (RectTransform)transform)
            {
                m_FillTransform = _fillRect.transform;
                m_FillImage = _fillRect.GetComponent<Image>();
                if (m_FillTransform.parent != null)
                    m_FillContainerRect = m_FillTransform.parent.GetComponent<RectTransform>();
            }
            else
            {
                _fillRect = null;
                m_FillContainerRect = null;
                m_FillImage = null;
            }

            if (_valueLabel)
            {
                _valueLabelTextCmp = _valueLabel.GetComponent<Text>();
                if (_valueLabelTextCmp == null)
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
            float newValue = Mathf.Clamp(input, MinValue, MaxValue);
            if (WholeNumbers)
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
            if (_value == newValue)
                return;

            _value = newValue;

            if (!m_DelayedUpdateVisuals)
            {
                UpdateVisuals();
            }

            if (sendCallback)
            {
                UISystemProfilerApi.AddMarker("StatBar.value", this);
                _onValueChanged.Invoke(newValue);
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

        Axis axis { get { return _direction == FillDirection.LeftToRight || _direction == FillDirection.RightToLeft ? Axis.Horizontal : Axis.Vertical; } }
        bool reverseValue { get { return _direction == FillDirection.RightToLeft || _direction == FillDirection.TopToBottom; } }

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
                m_Tracker.Add(this, _fillRect, DrivenTransformProperties.Anchors);
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

                _fillRect.anchorMin = anchorMin;
                _fillRect.anchorMax = anchorMax;
            }

            if (_valueLabel != null && _valueLabelTextCmp != null)
            {
                if (_valueLabelTextCmp is Text labelText)
                {
                    labelText.text = Value.ToString(!WholeNumbers ? "" : LeadingZeroesString);
                }
                else if (_valueLabelTextCmp is TMPro.TMP_Text labelTmpText)
                {
                    labelTmpText.text = Value.ToString(!WholeNumbers ? "" : LeadingZeroesString);
                }

            }
        }


        /// <summary>
        /// Sets the direction of this StatBar, optionally changing the layout as well.
        /// </summary>
        /// <param name="direction">The direction of the StatBar</param>
        /// <param name="includeRectLayouts">Should the layout be flipped together with the StatBar direction</param>
        public void SetDirection(FillDirection direction, bool includeRectLayouts)
        {
            Axis oldAxis = axis;
            bool oldReverse = reverseValue;
            this.Direction = direction;

            if (!includeRectLayouts)
                return;

            if (axis != oldAxis)
                RectTransformUtility.FlipLayoutAxes(transform as RectTransform, true, true);

            if (reverseValue != oldReverse)
                RectTransformUtility.FlipLayoutOnAxis(transform as RectTransform, (int)axis, true, true);
        }

        public void SetTransition(Transition transition)
        {
            if(transition == Transition.None)
            {
                m_FillImage.color = Color.red;
            }
            else if(transition == Transition.ColorTint)
            {
                m_FillImage.color = _normalColorTintTransition.color;
            }
        }
    }

}