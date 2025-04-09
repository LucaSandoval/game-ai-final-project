using UnityEngine;

public class TitleScreenController : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private Sound TitleScreenMusic;
    void Start()
    {
        SoundController.Instance?.PlaySound(TitleScreenMusic);
    }
}
