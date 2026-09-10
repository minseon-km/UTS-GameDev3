using UnityEngine;

public class ObjectController : MonoBehaviour
{
    // private string[] mov = {"Right", "Down", "Left", "Up"};
    private Vector3[] movStartPos = {new Vector3(-12.5f, 13.5f, 0.0f), new Vector3(-7.5f, 13.5f, 0.0f),
        new Vector3(-7.5f, 9.5f, 0.0f), new Vector3(-12.5f, 9.5f, 0.0f)};
    private float[] duration = {5.0f, 4.0f, 5.0f, 4.0f};
    private int moveCount = 0;
    private int moveNext = 1;

    [SerializeField]
    private GameObject player;
    private Animator playerAnimator;

    private Tweener tweener;

    public 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveCount = 0;
        moveNext = 1;
        
        tweener = GetComponent<Tweener>();
        playerAnimator = player.GetComponent<Animator>();
        playerAnimator.ResetTrigger("PlayerTurn");
    }

    // Update is called once per frame
    void Update()
    {
        //if tween does not exist
        if (! tweener.TweensExists(player.transform))
        {
            moveNext = (moveCount + 1) % 4;
            playerAnimator.SetTrigger("PlayerTurn");
            Vector3 startPos = movStartPos[moveCount];
            Vector3 endPos = movStartPos[moveNext];
            float dur = duration[moveCount];
            
            tweener.AddTween(player.transform, startPos, endPos, dur);
            moveCount = moveNext;
        }
    }
}
