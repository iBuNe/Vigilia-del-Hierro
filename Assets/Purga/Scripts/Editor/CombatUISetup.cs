using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Purga.EditorTools
{
    /// <summary>
    /// One-click setup for the provisional UI Toolkit combat screen: creates the
    /// PanelSettings the runtime loads from Resources. Run once from the menu.
    /// </summary>
    public static class CombatUISetup
    {
        const string Dir = "Assets/Purga/Resources/Purga/UI";
        const string Path = Dir + "/CombatPanelSettings.asset";

        [MenuItem("Purga/UI/Crear ajustes de UI de combate")]
        public static void CreateCombatPanelSettings()
        {
            if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);

            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(Path);
            bool created = ps == null;
            if (created)
            {
                ps = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(ps, Path);
            }

            // Assign an existing runtime theme if the project has one (Unity auto-creates
            // UnityDefaultRuntimeTheme.tss the first time you make any runtime UI). Without a
            // theme, controls fall back to the built-in font we set at runtime — text still shows.
            var guids = AssetDatabase.FindAssets("t:ThemeStyleSheet");
            if (guids.Length > 0)
            {
                var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(AssetDatabase.GUIDToAssetPath(guids[0]));
                ps.themeStyleSheet = theme;
                Debug.Log($"[Purga] Tema de runtime asignado: {theme.name}.");
            }
            else
            {
                Debug.LogWarning("[Purga] No hay ThemeStyleSheet en el proyecto. Los ajustes se crean igual y el texto usa la fuente de reserva. Si el estilo se ve raro, crea un tema con Assets → Create → UI Toolkit → Panel Settings Asset y vuelve a ejecutar este menú.");
            }

            ps.scaleMode = PanelScaleMode.ConstantPixelSize;
            ps.sortingOrder = 100;

            EditorUtility.SetDirty(ps);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Purga] {(created ? "Creados" : "Actualizados")} los ajustes de UI de combate en {Path}. Dale a Play y pulsa F9 en combate.");
        }
    }
}
