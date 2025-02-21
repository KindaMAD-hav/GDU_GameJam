using UnityEngine;
using System.Collections;
public class PlayerAttack : MonoBehaviour
{
    public float attackRange = 2f;
    public float attackDelay = 0.5f;
    public LayerMask enemyLayer;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        yield return new WaitForSeconds(attackDelay);

        // Ray from the camera center forward (in a first-person scenario)
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        if (Physics.Raycast(ray, out RaycastHit hitInfo, attackRange, enemyLayer))
        {
            Enemy enemy = hitInfo.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Kill();
            }
        }
    }
}
