using UnityEngine;
using UnityEngine.UI;
using Visage.Runtime;

public class ActionRPGSample : MonoBehaviour
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
        takeDamageBtn.onClick.AddListener(() => { hpBar.value -= 10; });
        healBtn.onClick.AddListener(() => { hpBar.value += 20; });
        fireballBtn.onClick.AddListener(() => { mpBar.value -= 20; });
        manaPotionBtn.onClick.AddListener(() => { mpBar.value += 50; });
    }
}