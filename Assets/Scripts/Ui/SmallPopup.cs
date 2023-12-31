using UnityEngine;
using TMPro;

public class SmallPopup : MonoBehaviour, IDefaultUi
{
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private float enableTime;
    private Animator _animator;
    private int _animIdTrigger;

    public void InitUi()
    {
        _animator = GetComponent<Animator>();
        _animIdTrigger = Animator.StringToHash("Trigger");
        gameObject.SetActive(false);
    }
    
    // private void Start()
    // {
    //     _animator = GetComponent<Animator>();
    //     _animIdTrigger = Animator.StringToHash("Trigger");
    // }

    public void EnableInfo(string description)
    {
        if(!gameObject.activeSelf) gameObject.SetActive(true);
        infoText.text = description;
        _animator.SetBool(_animIdTrigger, true);
    }

    public void DisableInfo() => _animator.SetBool(_animIdTrigger, false);

    public void PopupInfo(string description)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        infoText.text = description;
        _animator.SetBool(_animIdTrigger, true);

        Invoke(nameof(DisableInfo), enableTime);
    }
}
