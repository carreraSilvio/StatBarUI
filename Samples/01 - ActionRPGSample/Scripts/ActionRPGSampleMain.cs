﻿using UnityEngine;
using UnityEngine.UI;
using Visage.StatBarUI.Runtime;

namespace Visage.StatBarUI.Samples
{
    public class ActionRPGSampleMain : MonoBehaviour
    {
        public Button takeDamageBtn;
        public Button healBtn;
        public Button fireballBtn;
        public Button manaPotionBtn;

        public StatBar hpBar;
        public StatBar mpBar;

        // Start is called before the first frame update
        void Start()
        {
            takeDamageBtn.onClick.AddListener(() => { hpBar.Value -= 10; });
            healBtn.onClick.AddListener(() => { hpBar.Value += 20; });
            fireballBtn.onClick.AddListener(() => { mpBar.Value -= 20; });
            manaPotionBtn.onClick.AddListener(() => { mpBar.Value += 50; });
        }
    }
}