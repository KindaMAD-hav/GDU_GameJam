using UnityEngine;
using System.Collections;

public class TriggerMoveUp : MonoBehaviour
{
    public GameObject targetObject; // Object to move
    public float moveUpAmount = 2.0f; // Amount to move up
    public float moveSpeed = 2.0f; // Speed of movement

    private bool hasTriggered = false; // Prevents multiple triggers

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && targetObject != null)
        {
            hasTriggered = true;
            StartCoroutine(MoveUpSmoothly());
        }
    }

    private IEnumerator MoveUpSmoothly()
    {
        Vector3 startPos = targetObject.transform.position;
        Vector3 targetPos = startPos + new Vector3(0, moveUpAmount, 0);
        float elapsedTime = 0;

        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * moveSpeed;
            targetObject.transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime);
            yield return null;
        }

        targetObject.transform.position = targetPos; // Ensure final position is exact
    }
}
