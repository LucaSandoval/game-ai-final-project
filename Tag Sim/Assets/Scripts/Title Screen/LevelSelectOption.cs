using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelSelectOption : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image BGImage;
    [SerializeField] private Text NameText;
    [SerializeField] private int LevelID;

    private LevelSelectComponent selectComponent;

    public void Init(LevelSelectComponent selectComponent)
    {
        this.selectComponent = selectComponent;
    }

    public int GetLevelID()
    {
        return LevelID;
    }

    public void Highlight()
    {
        NameText.color = Color.white;
    }

    public void Unhighlight()
    {
        NameText.color = Color.black;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (selectComponent)
        {
            selectComponent.SelectLevel(this);
        }
    }
}
