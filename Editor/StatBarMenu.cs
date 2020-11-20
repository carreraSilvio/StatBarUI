using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Visage.Runtime;

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

            GameObject statBarHolder;
            StatBar statBar;
            GameObject fillArea;
            GameObject fill;

            CreateBar();
            CreateBackground();
            CreateFillArea();
            CreateFill();

            statBar.fillRect = fill.GetComponent<RectTransform>();
            statBar.value = statBar.maxValue;
            

            #region Locals
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
                statBarHolder = new GameObject("StatBar");
                statBar = statBarHolder.AddComponent<StatBar>();
                var statBarRect = statBarHolder.GetComponent<RectTransform>();
                statBarRect.sizeDelta = new Vector2(160, 20);

                // Ensure it gets reparented if this was a context click (otherwise does nothing)
                GameObjectUtility.SetParentAndAlign(statBarHolder, canvas.gameObject);

                // Register the creation in the undo system
                Undo.RegisterCreatedObjectUndo(statBarHolder, "Create " + statBarHolder.name);
                Selection.activeObject = statBarHolder;
            }
            void CreateBackground()
            {
                var bg = new GameObject("Background");
                var img = bg.AddComponent<Image>();
                img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
                img.type = Image.Type.Sliced;
                img.SetNativeSize();

                GameObjectUtility.SetParentAndAlign(bg, statBarHolder);
                MatchParentSize(bg, false, true);
                Stretch(bg, true, false);
            }
            void CreateFillArea()
            {
                fillArea = new GameObject("Fill Area");
                fillArea.AddComponent<RectTransform>();

                GameObjectUtility.SetParentAndAlign(fillArea, statBarHolder);
                MatchParentSize(fillArea, false, true);
                Stretch(fillArea, true, false);
            }
            void CreateFill()
            {
                fill = new GameObject("Fill");
                fill.AddComponent<RectTransform>();
                var img = fill.AddComponent<Image>();
                img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                img.type = Image.Type.Sliced;
                img.color = Color.red;
                img.SetNativeSize();

                GameObjectUtility.SetParentAndAlign(fill, fillArea.gameObject);
                Stretch(fill, true, true);
            }
            #endregion
        }

        /// <summary>
        /// Match child width or height with its parent's.
        /// </summary>
        /// <remarks>It won't stretch. For that, see <see cref="Stretch"/></remarks>
        private static void MatchParentSize(GameObject child, bool width, bool height)
        {
            var parentRect = child.transform.parent.GetComponent<RectTransform>();
            var childRect = child.GetComponent<RectTransform>();

            childRect.sizeDelta = new Vector2(
                width ? parentRect.sizeDelta.x : childRect.sizeDelta.x, 
                height ? parentRect.sizeDelta.y : childRect.sizeDelta.y);
        }

        /// <summary>
        /// Make the child stretch to match the parent size
        /// </summary>
        private static void Stretch(GameObject child, bool horizontal, bool vertical)
        {
            (float min, float max) streteched = (0f, 1f);
            (float min, float max) nonStreteched = (0.5f, 0.5f);

            //Make the child extend horizontal/vertical and centralize
            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = new Vector2(
                horizontal ? streteched.min : nonStreteched.min,
                vertical ? streteched.min : nonStreteched.min
                );
            childRect.anchorMax = new Vector2(
                horizontal ? streteched.max : nonStreteched.max,
                vertical ? streteched.max : nonStreteched.max
                );
            childRect.pivot = new Vector2(0.5f, 0.5f);

            //Adjust left and/or bot values
            childRect.offsetMin = new Vector2(
                horizontal ? 0f : childRect.offsetMin.x, //Left
                vertical ? 0f : childRect.offsetMin.y    //Bot
                );

            //Adjust right and/or top values
            childRect.offsetMax = new Vector2(
                horizontal ? 0f : childRect.offsetMax.x, //Left
                vertical ? 0f : childRect.offsetMax.y    //Top
                );

            //Left rectTransform.offsetMin.x;
            //Right rectTransform.offsetMax.x;
            //Top rectTransform.offsetMax.y;
            //Bottom rectTransform.offsetMin.y;
        }

    }
}