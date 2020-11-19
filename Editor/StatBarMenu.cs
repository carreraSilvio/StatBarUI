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
                // Create and set the statBar size
                var statBar = new GameObject("StatBar");
                statBar.AddComponent<StatBar>();
                var statBarRect = statBar.GetComponent<RectTransform>();
                statBarRect.sizeDelta = new Vector2(160, 20);

                // Ensure it gets reparented if this was a context click (otherwise does nothing)
                GameObjectUtility.SetParentAndAlign(statBar, canvas.gameObject);

                // Register the creation in the undo system
                Undo.RegisterCreatedObjectUndo(statBar, "Create " + statBar.name);
                Selection.activeObject = statBar;

                CreateBackground();
                void CreateBackground()
                {
                    var bg = new GameObject("Background");
                    bg.AddComponent<Image>();
                    GameObjectUtility.SetParentAndAlign(bg, statBar.gameObject);
                    
                    //Make the bg extend horizontal and centralize
                    var bgRect = bg.GetComponent<RectTransform>();
                    bgRect.anchorMin = new Vector2(0f, 0.5f);
                    bgRect.anchorMax = new Vector2(1f, 0.5f);
                    bgRect.pivot = new Vector2(0.5f, 0.5f);
                    
                    //Match bg height with parent height
                    bgRect.sizeDelta = new Vector2(bgRect.sizeDelta.x, statBarRect.sizeDelta.y);

                    //Adjust Left and Right values to 0
                    bgRect.offsetMin = new Vector2(0f, bgRect.offsetMin.y); //offsetMin.x => "Left" value
                    bgRect.offsetMax = new Vector2(0f, bgRect.offsetMax.y); //offsetMax.x => "Right" value
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