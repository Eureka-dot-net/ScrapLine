// ItemMover.cs
using UnityEngine;

public class ItemMover : MonoBehaviour
{
    private RectTransform rectTransform;
    private UICell targetCell;
    private float moveSpeed = 100f;

    private Vector3 startWorldPosition;
    private Vector3 targetWorldPosition;
    private float journeyLength;
    private float startTime;

    public void StartMovement(UICell nextCellScript, Vector3 startPos, Vector3 nextCellPos)
    {
        rectTransform = GetComponent<RectTransform>();
        targetCell = nextCellScript;

        startWorldPosition = startPos;
        targetWorldPosition = nextCellPos;
        
        transform.position = startWorldPosition;

        journeyLength = Vector3.Distance(startWorldPosition, targetWorldPosition);
        startTime = Time.time;
    }

    void Update()
    {
        if (transform.position == targetWorldPosition)
        {
            return;
        }

        float distCovered = (Time.time - startTime) * moveSpeed;
        float fractionOfJourney = distCovered / journeyLength;
        
        transform.position = Vector3.Lerp(startWorldPosition, targetWorldPosition, fractionOfJourney);

        if (fractionOfJourney >= 1f)
        {
            transform.position = targetWorldPosition;

            if (targetCell != null)
            {
                targetCell.OnItemArrived(gameObject);
            }
            else
            {
                Destroy(gameObject); 
            }
        }
    }
}