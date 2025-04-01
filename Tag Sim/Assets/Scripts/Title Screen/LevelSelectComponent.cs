using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LevelSelectComponent : MonoBehaviour
{
    [SerializeField] private GameObject LevelParent;
    private List<LevelSelectOption> levelSelectOptions;
    private LevelSelectOption selectedLevel;

    private void Awake()
    {
        levelSelectOptions = new List<LevelSelectOption>();
        // Init level select options
        if (LevelParent)
        {
            for(int i = 0; i < LevelParent.transform.childCount; i++)
            {
                LevelSelectOption option = LevelParent.transform.GetChild(i).GetComponent<LevelSelectOption>();
                if (option)
                {
                    option.Init(this);
                    levelSelectOptions.Add(option);
                }
            }
            SelectLevel(levelSelectOptions[0]);
        }
    }

    public void SelectLevel(LevelSelectOption level)
    {
        if (selectedLevel) selectedLevel.Unhighlight();
        foreach(var option in levelSelectOptions)
        {
            if (option.GetLevelID() == level.GetLevelID())
            {
                selectedLevel = level;
                selectedLevel.Highlight();
                return;
            }
        }
    }

    public void StartGame()
    {
        MapController.LevelID = selectedLevel.GetLevelID();
        Debug.Log(MapController.LevelID);
        SceneManager.LoadScene("MainLevel");
    }
}
