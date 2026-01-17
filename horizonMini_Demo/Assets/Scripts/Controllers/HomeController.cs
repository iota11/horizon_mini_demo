using UnityEngine;
using System.Collections.Generic;
using HorizonMini.Core;
using HorizonMini.Data;

namespace HorizonMini.Controllers
{
    /// <summary>
    /// Manages the home view with two horizontal scrollable rows of 3D world previews
    /// </summary>
    public class HomeController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Camera homeCamera;
        [SerializeField] private Transform myWorldsRow;
        [SerializeField] private Transform collectedWorldsRow;
        [SerializeField] private float cardSpacing = 15f;
        [SerializeField] private float cardScale = 0.5f;
        [SerializeField] private float scrollSpeed = 5f;

        [Header("Row Positions")]
        [SerializeField] private Vector3 myWorldsPosition = new Vector3(0, 5, 0);
        [SerializeField] private Vector3 collectedWorldsPosition = new Vector3(0, -5, 0);

        private AppRoot appRoot;
        private List<WorldInstance> myWorldInstances = new List<WorldInstance>();
        private List<WorldInstance> collectedWorldInstances = new List<WorldInstance>();

        private int myWorldsIndex = 0;
        private int collectedWorldsIndex = 0;

        private bool isActive = false;

        public void Initialize(AppRoot root)
        {
            appRoot = root;

            if (homeCamera == null)
            {
                homeCamera = Camera.main;
            }

            if (myWorldsRow == null)
            {
                GameObject row = new GameObject("MyWorldsRow");
                myWorldsRow = row.transform;
                myWorldsRow.SetParent(transform);
                myWorldsRow.localPosition = myWorldsPosition;
            }

            if (collectedWorldsRow == null)
            {
                GameObject row = new GameObject("CollectedWorldsRow");
                collectedWorldsRow = row.transform;
                collectedWorldsRow.SetParent(transform);
                collectedWorldsRow.localPosition = collectedWorldsPosition;
            }
        }

        public void SetActive(bool active)
        {
            isActive = active;
            gameObject.SetActive(active);

            if (active)
            {
                RefreshRows();
                PositionCamera();
            }
            else
            {
                ClearAllWorlds();
            }

            if (homeCamera != null)
            {
                homeCamera.gameObject.SetActive(active);
            }
        }

        private void RefreshRows()
        {
            ClearAllWorlds();
            LoadMyWorlds();
            LoadCollectedWorlds();
        }

        private void LoadMyWorlds()
        {
            List<string> worldIds = appRoot.SaveService.GetCreatedWorldIds();

            for (int i = 0; i < worldIds.Count; i++)
            {
                WorldInstance instance = appRoot.WorldLibrary.InstantiateWorld(worldIds[i], myWorldsRow);
                if (instance != null)
                {
                    instance.transform.localPosition = new Vector3(i * cardSpacing, 0, 0);
                    instance.transform.localScale = Vector3.one * cardScale;
                    instance.SetActivationLevel(ActivationLevel.Preloaded);
                    myWorldInstances.Add(instance);
                }
            }

            // If no worlds, show a placeholder message
            if (myWorldInstances.Count == 0)
            {
                Debug.Log("No created worlds yet");
            }
        }

        private void LoadCollectedWorlds()
        {
            List<string> worldIds = appRoot.SaveService.GetCollectedWorldIds();

            for (int i = 0; i < worldIds.Count; i++)
            {
                WorldInstance instance = appRoot.WorldLibrary.InstantiateWorld(worldIds[i], collectedWorldsRow);
                if (instance != null)
                {
                    instance.transform.localPosition = new Vector3(i * cardSpacing, 0, 0);
                    instance.transform.localScale = Vector3.one * cardScale;
                    instance.SetActivationLevel(ActivationLevel.Preloaded);
                    collectedWorldInstances.Add(instance);
                }
            }

            // If no worlds, show a placeholder message
            if (collectedWorldInstances.Count == 0)
            {
                Debug.Log("No collected worlds yet");
            }
        }

        private void ClearAllWorlds()
        {
            foreach (var instance in myWorldInstances)
            {
                if (instance != null)
                    Destroy(instance.gameObject);
            }
            myWorldInstances.Clear();

            foreach (var instance in collectedWorldInstances)
            {
                if (instance != null)
                    Destroy(instance.gameObject);
            }
            collectedWorldInstances.Clear();
        }

        private void PositionCamera()
        {
            if (homeCamera != null)
            {
                homeCamera.transform.position = new Vector3(0, 0, -30);
                homeCamera.transform.LookAt(Vector3.zero);
            }
        }

        private void Update()
        {
            if (!isActive)
                return;

            HandleInput();
        }

        private void HandleInput()
        {
            // Keyboard controls for testing
            // Arrow keys to navigate rows
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                ScrollMyWorldsLeft();
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                ScrollMyWorldsRight();
            }

            // Touch/swipe controls
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Ended)
                {
                    Vector2 delta = touch.deltaPosition;

                    // Determine which row was swiped
                    Vector3 touchWorldPos = homeCamera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, 10));

                    if (touchWorldPos.y > 0)
                    {
                        // My Worlds row
                        if (delta.x > 50)
                            ScrollMyWorldsLeft();
                        else if (delta.x < -50)
                            ScrollMyWorldsRight();
                    }
                    else
                    {
                        // Collected Worlds row
                        if (delta.x > 50)
                            ScrollCollectedWorldsLeft();
                        else if (delta.x < -50)
                            ScrollCollectedWorldsRight();
                    }
                }
            }

            // Mouse click for selection
            if (Input.GetMouseButtonDown(0))
            {
                HandleWorldSelection(Input.mousePosition);
            }
        }

        private void ScrollMyWorldsLeft()
        {
            if (myWorldsIndex > 0)
            {
                myWorldsIndex--;
                AnimateRowScroll(myWorldsRow, myWorldsIndex);
            }
        }

        private void ScrollMyWorldsRight()
        {
            if (myWorldsIndex < myWorldInstances.Count - 1)
            {
                myWorldsIndex++;
                AnimateRowScroll(myWorldsRow, myWorldsIndex);
            }
        }

        private void ScrollCollectedWorldsLeft()
        {
            if (collectedWorldsIndex > 0)
            {
                collectedWorldsIndex--;
                AnimateRowScroll(collectedWorldsRow, collectedWorldsIndex);
            }
        }

        private void ScrollCollectedWorldsRight()
        {
            if (collectedWorldsIndex < collectedWorldInstances.Count - 1)
            {
                collectedWorldsIndex++;
                AnimateRowScroll(collectedWorldsRow, collectedWorldsIndex);
            }
        }

        private void AnimateRowScroll(Transform row, int index)
        {
            float targetX = -index * cardSpacing;
            Vector3 targetPos = new Vector3(targetX, row.localPosition.y, row.localPosition.z);

            // Simple immediate scroll (can be enhanced with lerp/animation)
            row.localPosition = targetPos;
        }

        private void HandleWorldSelection(Vector2 screenPos)
        {
            Ray ray = homeCamera.ScreenPointToRay(screenPos);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                WorldInstance instance = hit.collider.GetComponentInParent<WorldInstance>();
                if (instance != null)
                {
                    OnWorldSelected(instance);
                }
            }
        }

        private void OnWorldSelected(WorldInstance instance)
        {
            Debug.Log($"Selected world: {instance.WorldData.worldTitle}");

            // Option 1: Enter browse mode at this world
            // Option 2: Show detail panel
            // For now, enter play mode directly
            appRoot.EnterPlayMode(instance.WorldId);
        }

        // Public methods for UI buttons
        public void OnMyWorldCardTapped(int index)
        {
            if (index >= 0 && index < myWorldInstances.Count)
            {
                OnWorldSelected(myWorldInstances[index]);
            }
        }

        public void OnCollectedWorldCardTapped(int index)
        {
            if (index >= 0 && index < collectedWorldInstances.Count)
            {
                OnWorldSelected(collectedWorldInstances[index]);
            }
        }
    }
}
