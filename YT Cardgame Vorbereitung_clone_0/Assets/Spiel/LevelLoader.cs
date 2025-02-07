using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    public Animator transition;

    private void Start()
    {
        GameManager.StartTransitionEvent += LoadNextLevel;
    }

    private void OnDestroy()
    {
        GameManager.StartTransitionEvent -= LoadNextLevel;
    }


    public void LoadNextLevel()
    {
        transition.SetTrigger("Start");
    }
}
