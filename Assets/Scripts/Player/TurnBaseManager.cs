using Game;
using UnityEngine;
using UnityEngine.Playables;

public class TurnBaseManager : MonoBehaviour
{
    public PlayableDirector exitTurnBase;
    public GameObject enemy;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ExitTurnBase()
    {
        exitTurnBase.Play();
    }

    public void TurnOffEnemy()
    {
        enemy?.SetActive(false);
    }
}
