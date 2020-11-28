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
        public enum FillTransition
        {
            None,
            ColorTint
        }

        [System.Serializable]
        public class ColorTintTransition
        {
            public int percent;
            public Color color;

            public float NormalizedPercent => Mathf.InverseLerp(0, 100, percent);
        }

        

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
        /// The trasition type of the StatBar.
        /// </summary>
        public FillTransition Transition { get { return _transition; } set { if (SetPropertyUtility.SetStruct(ref _transition, value)) UpdateVisuals(); } }

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
        /// The min number of digits visible
        /// </summary>
        public int MinDigits { get { return _minDigits; } set { if (SetPropertyUtility.SetStruct(ref _minDigits, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        public string LeadingZeroesString => _digitsVisibleString;

        [SerializeField] private int _minDigits = 2;
        [SerializeField] private RectTransform _valueLabel;

        [SerializeField] private RectTransform _fillRect;
        [Space]
        [SerializeField] private FillDirection _direction = FillDirection.LeftToRight;
        [SerializeField] private float _minValue = 0;
        [SerializeField] private float _maxValue = 1;
        [SerializeField] private bool _wholeNumbers = false;
        [SerializeField] protected float _value;

        [SerializeField] private FillTransition _transition;
        [SerializeField]
        private ColorTintTransition _normalColorTintTransition = new ColorTintTransition
        {
            percent = 100,
            color = Color.red
        };
        [SerializeField]
        private ColorTintTransition _lowColorTintTransition = new ColorTintTransition
        {
            percent = 40,
            color = new Color(0.75f, 0f, 0f)
        };
        [SerializeField]
        private ColorTintTransition _criticalColorTintTransition = new ColorTintTransition
        {
            percent = 20,
            color = new Color(0.25f, 0f, 0f)
        };

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
        public float NormalizedValue
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
        private Image _fillImage;
        private Transform _fillTransform;
        private RectTransform _fillContainerRect;

        private DrivenRectTransformTracker _tracker;

        private string _digitsVisibleString = "00";
        private object _valueLabelTextCmp; //Either Text or TMP_Text compoment

        // This "delayed" mechanism is required for case 1037681.
        private bool _delayedUpdateVisuals = false;
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
                _delayedUpdateVisuals = true;
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
            _tracker.Clear();
            base.OnDisable();
        }

        /// <summary>
        /// Update the rect based on the delayed update visuals.
        /// Got around issue of calling sendMessage from onValidate.
        /// </summary>
        protected virtual void Update()
        {
            if (_delayedUpdateVisuals)
            {
                _delayedUpdateVisuals = false;
                UpdateVisuals();
            }
        }

        protected override void OnDidApplyAnimationProperties()
        {
            // Has value changed? Various elements of the StatBar have the old normalisedValue assigned, we can use this to perform a comparison.
            // We also need to ensure the value stays within min/max.
            _value = ClampValue(_value);
            float oldNormalizedValue = NormalizedValue;
            if (_fillContainerRect != null)
            {
                if (_fillImage != null && _fillImage.type == Image.Type.Filled)
                    oldNormalizedValue = _fillImage.fillAmount;
                else
                    oldNormalizedValue = reverseValue ? 1 - _fillRect.anchorMin[(int)axis] : _fillRect.anchorMax[(int)axis];
            }

            UpdateVisuals();

            if (oldNormalizedValue != NormalizedValue)
            {
                UISystemProfilerApi.AddMarker("StatBar.value", this);
                onValueChanged.Invoke(_value);
            }
        }

        void UpdateCachedReferences()
        {
            if (_fillRect && _fillRect != (RectTransform)transform)
            {
                _fillTransform = _fillRect.transform;
                _fillImage = _fillRect.GetComponent<Image>();
                if (_fillTransform.parent != null)
                    _fillContainerRect = _fillTransform.parent.GetComponent<RectTransform>();
            }
            else
            {
                _fillRect = null;
                _fillContainerRect = null;
                _fillImage = null;
            }

            if (_valueLabel)
            {
                _valueLabelTextCmp = _valueLabel.GetComponent<Text>();
                if (_valueLabelTextCmp == null)
                {
                    _valueLabelTextCmp = _valueLabel.GetComponent<TMPro.TMP_Text>();
                }

                _digitsVisibleString = string.Empty;
                for (int zeroCount = _minDigits; zeroCount > 0; zeroCount--)
                {
                    _digitsVisibleString += "0";
                }
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

            if (!_delayedUpdateVisuals)
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

            _tracker.Clear();

            if (_fillContainerRect != null)
            {
                _tracker.Add(this, _fillRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;

                if (_fillImage != null && _fillImage.type == Image.Type.Filled)
                {
                    _fillImage.fillAmount = NormalizedValue;
                }
                else
                {
                    if (reverseValue)
                        anchorMin[(int)axis] = 1 - NormalizedValue;
                    else
                        anchorMax[(int)axis] = NormalizedValue;
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

            if(_transition == FillTransition.ColorTint)
            {
                float normalizedValue = NormalizedValue;
                if (normalizedValue <= _criticalColorTintTransition.NormalizedPercent)
                {
                    _fillImage.color = _criticalColorTintTransition.color;
                }
                else if (normalizedValue <= _lowColorTintTransition.NormalizedPercent)
                {
                    _fillImage.color = _lowColorTintTransition.color;
                }
                else if (normalizedValue <= _normalColorTintTransition.NormalizedPercent)
                {
                    _fillImage.color = _normalColorTintTransition.color;
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

        public void SetTransition(FillTransition transition)
        {
            Transition = transition;
            if(transition == FillTransition.None)
            {
                _fillImage.color = Color.red;
            }
            else if(transition == FillTransition.ColorTint)
            {
                _fillImage.color = _normalColorTintTransition.color;
            }
        }
    }

}