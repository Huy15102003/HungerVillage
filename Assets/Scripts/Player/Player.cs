using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Game
{
    [RequireComponent(typeof(FPController))]
    public class Player : MonoBehaviour
    {
        [Header("Component")]
        [SerializeField] FPController FPController;
        public GameObject TurnBase;
        public GameObject Canvas;

        #region Input Handling

        void OnMove(InputValue value)
        {
            if (FPController.IsPaused) return;
            FPController.MoveInput = value.Get<Vector2>();
        } 

        void OnLook(InputValue value)
        {
            if (FPController.IsPaused) return;
            FPController.LookInput = value.Get<Vector2>();
        }
        void OnSprint(InputValue value)
        {
            if (FPController.IsPaused) return;
            FPController.SprintInput = value.isPressed;
        }

        #endregion

        #region Unity Methods

        void OnValidate()
        {
            if (FPController == null)
                FPController = GetComponent<FPController>();
        }

        #endregion

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            FPController = GetComponent<FPController>();
            //TurnBase = GetComponent<TurnBaseManager>().gameObject;
            Canvas = GameObject.Find("PlayerCanvas");
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }
}
