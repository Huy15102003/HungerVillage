using UnityEngine;
using System.Collections;

namespace Game
{
    public class ItemInteraction : MonoBehaviour, IInteractable
    {
        public string InteractMessage => "Nhấn E để tương tác";
        public bool CanInteract => true;

        public GameObject model;

        public bool hasThug = false;

        // Points to award when interacting (used when hasThug == false)
        public int ScoreValue = 1;

        Coroutine runningMoveCoroutine;

        public void Interact()
        {
            Debug.Log("Đã tương tác");
            if(hasThug == true)
            {
                ThugGuard();
            }
            else
            {
                // Award points via persistent GameManager
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddScore(ScoreValue);
                    gameObject.SetActive(false);
                }
            }
           
        }

        private void ThugGuard()
        {
            Player player = FindAnyObjectByType<Player>();
            FPController controller = FindAnyObjectByType<FPController>();

            controller.PauseLookAt(gameObject);
            player.TurnBase?.SetActive(true);

            // Xoay object về phía player
            if (player != null)
            {
                Vector3 targetPos = player.transform.position;

                // Nếu không muốn nghiêng lên xuống
                targetPos.y = transform.position.y;

                transform.LookAt(targetPos);
            }

            // Di chuyển model sang X + 5 theo thời gian
            if (model != null)
            {
                if (runningMoveCoroutine != null)
                    StopCoroutine(runningMoveCoroutine);

                runningMoveCoroutine = StartCoroutine(
                    MoveModelLocalX(model.transform.localPosition.x - 5f, 3f)
                );
            }
            player.TurnBase.GetComponent<TurnBaseManager>().enemy = gameObject;
            player.TurnBase.GetComponentInChildren<CameraOcclusion>().target = gameObject.transform;
        }

        IEnumerator MoveModelLocalX(float targetX, float duration)
        {
            Vector3 startPos = model.transform.localPosition;
            Vector3 endPos = startPos;
            endPos.x = targetX;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / duration);

                model.transform.localPosition = Vector3.Lerp(
                    startPos,
                    endPos,
                    t
                );

                yield return null;
            }

            model.transform.localPosition = endPos;
            runningMoveCoroutine = null;
        }
    }
}