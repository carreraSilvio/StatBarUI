using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Visage.Runtime;
using CGTespy.UI;

namespace Visage.Editor
{
    public sealed class StatBarMenu
    {
        // Add a menu item to create custom GameObjects.
        // Priority 1 ensures it is grouped with the other menu items of the same kind
        // and propagated to the hierarchy dropdown and hierarchy context menus.
        [MenuItem("GameObject/UI/Visage/StatBar", false, 70)]
        static void CreateStatBar(MenuCommand menuCommand)
        {
            Canvas canvas;
            FindOrCreateCanvas();
            FindOrCreateEventSystem();
            CreateBar();

            void FindOrCreateCanvas()
            {
                canvas = Object.FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    var go = new GameObject("Canvas");
                    canvas = go.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    go.AddComponent<CanvasScaler>();
                    go.AddComponent<GraphicRaycaster>();
                    GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
                }
            }
            void FindOrCreateEventSystem()
            {
                var eventSystem = Object.FindObjectOfType<EventSystem>();
                if (eventSystem == null)
                {
                    var go = new GameObject("EventSystem");
                    go.AddComponent<EventSystem>();
                    go.AddComponent<StandaloneInputModule>();
                }
            }
            void CreateBar()
            {
                // Create a custom game object
                GameObject go = new GameObject("StatBar");
                go.AddComponent<StatBar>();
                go.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 20);

                // Ensure it gets reparented if this was a context click (otherwise does nothing)
                GameObjectUtility.SetParentAndAlign(go, canvas.gameObject);
                // Register the creation in the undo system
                Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
                Selection.activeObject = go;

                CreateBackground();
                void CreateBackground()
                {
                    GameObject background = new GameObject("Background");
                    background.AddComponent<Image>();
                    background.transform.SetParent(go.transform);
                    //GameObjectUtility.SetParentAndAlign(background, go);
                    RectTransform childRect = background.GetComponent<RectTransform>();
                    childRect.ApplyAnchorPreset(TextAnchor.UpperCenter, true, true);
                        //childRect.size
                    //childRect.anchorMin = new Vector2(0.5f, 0.5f);
                    //childRect.anchorMax = new Vector2(0.5f, 0.5f);
                    //childRect.pivot = new Vector2(0.5f, 0.5f);
                }
            }
        }

        // Add a menu item called "Double Mass" to a Rigidbody's context menu.
        [MenuItem("CONTEXT/Rigidbody/Double Mass")]
        static void DoubleMass(MenuCommand command)
        {
            Rigidbody body = (Rigidbody)command.context;
            body.mass = body.mass * 2;
            Debug.Log("Doubled Rigidbody's Mass to " + body.mass + " from Context Menu.");
        }
    }
    
}