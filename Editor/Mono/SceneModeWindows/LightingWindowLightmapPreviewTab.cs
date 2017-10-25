// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;
using UnityEngineInternal;
using Object = UnityEngine.Object;


namespace UnityEditor
{
    internal class LightingWindowLightmapPreviewTab
    {
        const string kEditorPrefsGBuffersLightmapsAlbedoEmissive = "LightingWindowGlobalMapsGLAE";
        const string kEditorPrefsTransmissionTextures = "LightingWindowGlobalMapsTT";
        const string kEditorPrefsInFlight = "LightingWindowGlobalMapsIF";

        Vector2 m_ScrollPositionLightmaps = Vector2.zero;
        Vector2 m_ScrollPositionMaps = Vector2.zero;
        int m_SelectedLightmap = -1;

        static Styles s_Styles;
        class Styles
        {
            public Styles()
            {
                boldFoldout.fontStyle = FontStyle.Bold;
            }

            public GUIStyle selectedLightmapHighlight = "LightmapEditorSelectedHighlight";
            public GUIStyle boldFoldout = new GUIStyle(EditorStyles.foldout);

            public GUIContent LightProbes = EditorGUIUtility.TextContent("Light Probes|A different LightProbes.asset can be assigned here. These assets are generated by baking a scene containing light probes.");
            public GUIContent LightingDataAsset = EditorGUIUtility.TextContent("Lighting Data Asset|A different LightingData.asset can be assigned here. These assets are generated by baking a scene in the OnDemand mode.");
            public GUIContent MapsArraySize = EditorGUIUtility.TextContent("Array Size|The length of the array of lightmaps.");
        }

        static void DrawHeader(Rect rect, bool showdrawDirectionalityHeader, bool showShadowMaskHeader, float maxLightmaps)
        {
            // we first needed to get the amount of space that the first texture would get
            // as that's done now, let's request the rect for the header
            rect.width = rect.width / maxLightmaps;

            // display the header
            EditorGUI.DropShadowLabel(rect, "Intensity");
            rect.x += rect.width;
            if (showdrawDirectionalityHeader)
            {
                EditorGUI.DropShadowLabel(rect, "Directionality");
                rect.x += rect.width;
            }

            if (showShadowMaskHeader)
            {
                EditorGUI.DropShadowLabel(rect, "Shadowmask");
            }
        }

        void MenuSelectLightmapUsers(Rect rect, int lightmapIndex)
        {
            if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
            {
                string[] menuText = { "Select Lightmap Users" };
                Rect r = new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 1, 1);
                EditorUtility.DisplayCustomMenu(r, EditorGUIUtility.TempContent(menuText), -1, SelectLightmapUsers, lightmapIndex);
                Event.current.Use();
            }
        }

        void SelectLightmapUsers(object userData, string[] options, int selected)
        {
            int lightmapIndex = (int)userData;
            ArrayList newSelection = new ArrayList();
            MeshRenderer[] renderers = Object.FindObjectsOfType(typeof(MeshRenderer)) as MeshRenderer[];
            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer != null && renderer.lightmapIndex == lightmapIndex)
                    newSelection.Add(renderer.gameObject);
            }
            Terrain[] terrains = Object.FindObjectsOfType(typeof(Terrain)) as Terrain[];
            foreach (Terrain terrain in terrains)
            {
                if (terrain != null && terrain.lightmapIndex == lightmapIndex)
                    newSelection.Add(terrain.gameObject);
            }
            Selection.objects = newSelection.ToArray(typeof(Object)) as Object[];
        }

        public void LightmapPreview(Rect r)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            const float headerHeight = 20;
            const float spacing = 10;
            GUI.Box(r, "", "PreBackground");
            m_ScrollPositionLightmaps = EditorGUILayout.BeginScrollView(m_ScrollPositionLightmaps, GUILayout.Height(r.height));
            int lightmapIndex = 0;
            bool haveDirectionalityLightMaps = false;
            bool haveShadowMaskLightMaps = false;

            foreach (LightmapData li in LightmapSettings.lightmaps)
            {
                if (li.lightmapDir != null) haveDirectionalityLightMaps = true;
                if (li.shadowMask != null)   haveShadowMaskLightMaps = true;
            }

            float maxLightmaps = 1.0f;
            if (haveDirectionalityLightMaps) ++maxLightmaps;
            if (haveShadowMaskLightMaps) ++maxLightmaps;

            // display the header
            Rect headerRect = GUILayoutUtility.GetRect(r.width, r.width, headerHeight, headerHeight);
            DrawHeader(headerRect, haveDirectionalityLightMaps, haveShadowMaskLightMaps, maxLightmaps);

            foreach (LightmapData li in LightmapSettings.lightmaps)
            {
                if (li.lightmapColor == null && li.lightmapDir == null && li.shadowMask == null)
                {
                    lightmapIndex++;
                    continue;
                }

                int lightmapColorMaxSize = li.lightmapColor ? Math.Max(li.lightmapColor.width, li.lightmapColor.height) : -1;
                int lightmapDirMaxSize = li.lightmapDir ? Math.Max(li.lightmapDir.width, li.lightmapDir.height) : -1;
                int lightMaskMaxSize = li.shadowMask ? Math.Max(li.shadowMask.width, li.shadowMask.height) : -1;

                Texture2D biggerLightmap;
                if (lightmapColorMaxSize > lightmapDirMaxSize)
                {
                    biggerLightmap = lightmapColorMaxSize > lightMaskMaxSize ? li.lightmapColor : li.shadowMask;
                }
                else
                {
                    biggerLightmap = lightmapDirMaxSize > lightMaskMaxSize ? li.lightmapDir : li.shadowMask;
                }

                // get rect for textures in this row
                GUILayoutOption[] layout = { GUILayout.MaxWidth(r.width), GUILayout.MaxHeight(biggerLightmap.height)};
                Rect rect = GUILayoutUtility.GetAspectRect(maxLightmaps, layout);

                // display the textures
                float rowSpacing = spacing * 0.5f;
                rect.width /= maxLightmaps;
                rect.width -= rowSpacing;
                rect.x += rowSpacing / 2;
                EditorGUI.DrawPreviewTexture(rect, li.lightmapColor);
                MenuSelectLightmapUsers(rect, lightmapIndex);

                if (li.lightmapDir)
                {
                    rect.x += rect.width + rowSpacing;
                    EditorGUI.DrawPreviewTexture(rect, li.lightmapDir);
                    MenuSelectLightmapUsers(rect, lightmapIndex);
                }
                if (li.shadowMask)
                {
                    rect.x += rect.width + rowSpacing;
                    EditorGUI.DrawPreviewTexture(rect, li.shadowMask);
                    MenuSelectLightmapUsers(rect, lightmapIndex);
                }
                GUILayout.Space(spacing);
                lightmapIndex++;
            }

            EditorGUILayout.EndScrollView();
        }

        public void UpdateLightmapSelection()
        {
            MeshRenderer renderer;
            Terrain terrain = null;
            // if the active object in the selection is a renderer or a terrain, we're interested in it's lightmapIndex
            if (Selection.activeGameObject == null ||
                ((renderer = Selection.activeGameObject.GetComponent<MeshRenderer>()) == null &&
                 (terrain = Selection.activeGameObject.GetComponent<Terrain>()) == null))
            {
                m_SelectedLightmap = -1;
                return;
            }
            m_SelectedLightmap = renderer != null ? renderer.lightmapIndex : terrain.lightmapIndex;
        }

        enum GlobalMapsViewType
        {
            Performance,
            Memory
        }

        string SizeString(float size)
        {
            return size.ToString("0.0") + " MB";
        }

        float SumSizes(float[] sizes)
        {
            float sum = 0.0f;
            foreach (var size in sizes)
                sum += size;

            return sum;
        }

        private void ShowObjectNamesAndSizes(string foldoutName, string editorPrefsName, string[] objectNames, float[] sizes)
        {
            Debug.Assert(objectNames.Length == sizes.Length);

            if (objectNames.Length == 0)
                return;

            const bool toggleOnLabelClick = true;
            string foldoutNameFull = foldoutName + " (" + SizeString(SumSizes(sizes)) + ")";
            bool showDetailsOld = EditorPrefs.GetBool(editorPrefsName, true);

            bool showDetails = EditorGUILayout.Foldout(showDetailsOld, foldoutNameFull, toggleOnLabelClick, s_Styles.boldFoldout);

            if (showDetails != showDetailsOld)
                EditorPrefs.SetBool(editorPrefsName, showDetails);

            if (!showDetails)
                return;

            GUILayout.BeginHorizontal();
            {
                string[] stringSeparators = new string[] { " | " };

                GUILayout.Space(20);

                GUILayout.BeginVertical();
                for (int i = 0; i < objectNames.Length; ++i)
                {
                    string fullName = objectNames[i];
                    string[] result = fullName.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                    Debug.Assert(result.Length > 0);
                    string objectName = result[0];
                    string tooltip = "";
                    if (result.Length > 1)
                        tooltip = result[1];

                    GUILayout.Label(new GUIContent(objectName, tooltip), EditorStyles.miniLabel);
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                for (int i = 0; i < sizes.Length; ++i)
                {
                    GUILayout.Label(sizes[i].ToString("0.0") + " MB", EditorStyles.miniLabel);
                }
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        public void Maps()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            GUI.changed = false;

            if (Lightmapping.giWorkflowMode == Lightmapping.GIWorkflowMode.OnDemand)
            {
                SerializedObject so = new SerializedObject(LightmapEditorSettings.GetLightmapSettings());
                SerializedProperty LightingDataAsset = so.FindProperty("m_LightingDataAsset");
                EditorGUILayout.PropertyField(LightingDataAsset, s_Styles.LightingDataAsset);
                so.ApplyModifiedProperties();
            }

            GUILayout.Space(10);

            GlobalMapsViewType viewType = GlobalMapsViewType.Performance;

            if (EditorPrefs.GetBool("InternalMode", false))
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                viewType = (GlobalMapsViewType)EditorPrefs.GetInt("LightingWindowGlobalMapsViewType", (int)viewType);

                EditorGUI.BeginChangeCheck();
                {
                    viewType = (GlobalMapsViewType)GUILayout.Toolbar((int)viewType, new string[] { "Performance", "Memory" }, EditorStyles.miniButton, GUILayout.ExpandWidth(false));
                }
                if (EditorGUI.EndChangeCheck())
                    EditorPrefs.SetInt("LightingWindowGlobalMapsViewType", (int)viewType);

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            LightmapData[] lightmaps = LightmapSettings.lightmaps;

            m_ScrollPositionMaps = GUILayout.BeginScrollView(m_ScrollPositionMaps);
            {
                bool showDirLightmap = false;
                bool showShadowMask = false;
                foreach (LightmapData lightmapData in lightmaps)
                {
                    if (lightmapData.lightmapDir != null)
                        showDirLightmap = true;
                    if (lightmapData.shadowMask != null)
                        showShadowMask = true;
                }

                if (viewType == GlobalMapsViewType.Performance)
                    PerformanceCentricView(lightmaps, showDirLightmap, showShadowMask, viewType);
                else
                    MemoryCentricView(lightmaps, showDirLightmap, showShadowMask, viewType);
            }
            GUILayout.EndScrollView();
        }

        void MemoryCentricView(LightmapData[] lightmaps, bool showDirLightmap, bool showShadowMask, GlobalMapsViewType viewType)
        {
            Lightmapping.ResetExplicitlyShownMemLabels();

            Dictionary<Hash128, SortedList<int, int>> gbufferHashToLightmapIndices = new Dictionary<Hash128, SortedList<int, int>>();
            for (int i = 0; i < lightmaps.Length; i++)
            {
                Hash128 gbufferHash;
                if (Lightmapping.GetGBufferHash(i, out gbufferHash))
                {
                    if (!gbufferHashToLightmapIndices.ContainsKey(gbufferHash))
                        gbufferHashToLightmapIndices.Add(gbufferHash, new SortedList<int, int>());

                    gbufferHashToLightmapIndices[gbufferHash].Add(i, i);
                }
            }

            float totalGBuffersSize = 0.0f;
            float totalLightmapsSize = 0.0f;
            float totalAlbedoEmissiveSize = 0.0f;
            foreach (var entry in gbufferHashToLightmapIndices)
            {
                float gbufferDataSize;
                Hash128 gbufferHash = entry.Key;
                Lightmapping.GetGBufferMemory(ref gbufferHash, out gbufferDataSize);
                totalGBuffersSize += gbufferDataSize;

                SortedList<int, int> lightmapIndices = entry.Value;
                foreach (var i in lightmapIndices)
                {
                    LightmapMemory lightmapMemory = Lightmapping.GetLightmapMemory(i.Value);
                    totalLightmapsSize += lightmapMemory.lightmapDataSize;
                    totalLightmapsSize += lightmapMemory.lightmapTexturesSize;
                    totalAlbedoEmissiveSize += lightmapMemory.albedoDataSize;
                    totalAlbedoEmissiveSize += lightmapMemory.albedoTextureSize;
                    totalAlbedoEmissiveSize += lightmapMemory.emissiveDataSize;
                    totalAlbedoEmissiveSize += lightmapMemory.emissiveTextureSize;
                }
            }

            if (gbufferHashToLightmapIndices.Count > 0)
            {
                const bool toggleOnLabelClick = true;
                string foldoutNameFull =
                    "G-buffers (" + SizeString(totalGBuffersSize) + ") | " +
                    "Lightmaps (" + SizeString(totalLightmapsSize) + ") | " +
                    "Albedo/Emissive (" + SizeString(totalAlbedoEmissiveSize) + ")";
                bool showDetailsOld = EditorPrefs.GetBool(kEditorPrefsGBuffersLightmapsAlbedoEmissive, true);

                bool showDetails = EditorGUILayout.Foldout(showDetailsOld, foldoutNameFull, toggleOnLabelClick, s_Styles.boldFoldout);

                if (showDetails != showDetailsOld)
                    EditorPrefs.SetBool(kEditorPrefsGBuffersLightmapsAlbedoEmissive, showDetails);

                if (showDetails)
                {
                    foreach (var entry in gbufferHashToLightmapIndices)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(15);
                            GUILayout.BeginVertical();
                            {
                                float gbufferDataSize;
                                Hash128 gbufferHash = entry.Key;
                                Lightmapping.GetGBufferMemory(ref gbufferHash, out gbufferDataSize);
                                GUILayout.Label(new GUIContent("G-buffer: " + gbufferDataSize.ToString("0.0") + " MB", gbufferHash.ToString()), EditorStyles.miniLabel, GUILayout.ExpandWidth(false));

                                SortedList<int, int> lightmapIndices = entry.Value;
                                foreach (var i in lightmapIndices)
                                {
                                    LightmapRow(i.Value, lightmaps, showDirLightmap, showShadowMask, viewType);
                                }
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.Space(10);
                    }
                }
            }

            {
                string[] objectNames;
                float[] sizes;
                Lightmapping.GetTransmissionTexturesMemLabels(out objectNames, out sizes);
                ShowObjectNamesAndSizes("Transmission textures", kEditorPrefsTransmissionTextures, objectNames, sizes);
            }

            {
                string[] objectNames;
                float[] sizes;
                Lightmapping.GetNotShownMemLabels(out objectNames, out sizes);
                string remainingEntriesFoldoutName = Lightmapping.isProgressiveLightmapperDone ? "Leaks" : "In-flight";
                ShowObjectNamesAndSizes(remainingEntriesFoldoutName, kEditorPrefsInFlight, objectNames, sizes);
            }
        }

        void PerformanceCentricView(LightmapData[] lightmaps, bool showDirLightmap, bool showShadowMask, GlobalMapsViewType viewType)
        {
            for (int i = 0; i < lightmaps.Length; i++)
            {
                LightmapRow(i, lightmaps, showDirLightmap, showShadowMask, viewType);
            }
        }

        void LightmapRow(int index, LightmapData[] lightmaps, bool showDirLightmap, bool showShadowMask, GlobalMapsViewType viewType)
        {
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            int statsLineCount = (viewType == GlobalMapsViewType.Performance) ? 5 : 7;
            GUILayout.Space(20);
            lightmaps[index].lightmapColor = LightmapField(lightmaps[index].lightmapColor, index, statsLineCount);

            if (showDirLightmap)
            {
                GUILayout.Space(5);
                lightmaps[index].lightmapDir = LightmapField(lightmaps[index].lightmapDir, index, statsLineCount);
            }

            if (showShadowMask)
            {
                GUILayout.Space(5);
                lightmaps[index].shadowMask = LightmapField(lightmaps[index].shadowMask, index, statsLineCount);
            }

            GUILayout.Space(5);
            if (viewType == GlobalMapsViewType.Performance)
                LightmapPerformanceStats(index);
            else
                LightmapMemoryStats(index);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        Texture2D LightmapField(Texture2D lightmap, int index, int statsLineCount)
        {
            float size =
                2 * EditorStyles.miniLabel.margin.top +
                (statsLineCount - 1) * Mathf.Max(EditorStyles.miniLabel.margin.top, EditorStyles.miniLabel.margin.bottom) +
                2 * EditorStyles.miniLabel.padding.top +
                (statsLineCount - 1) * EditorStyles.miniLabel.padding.vertical +
                (statsLineCount - 1) * EditorStyles.miniLabel.lineHeight +
                EditorStyles.miniLabel.font.fontSize;
            Rect rect = GUILayoutUtility.GetRect(size, size, EditorStyles.objectField);

            MenuSelectLightmapUsers(rect, index);
            Texture2D retval = null;
            using (new EditorGUI.DisabledScope(true))
            {
                retval = EditorGUI.ObjectField(rect, lightmap, typeof(Texture2D), false) as Texture2D;
            }
            if (index == m_SelectedLightmap && Event.current.type == EventType.Repaint)
                s_Styles.selectedLightmapHighlight.Draw(rect, false, false, false, false);

            return retval;
        }

        void LightmapPerformanceStats(int index)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("Index: " + index, EditorStyles.miniLabel);

            LightmapConvergence lc = Lightmapping.GetLightmapConvergence(index);
            if (lc.IsValid())
            {
                GUILayout.Label("Occupied: " + InternalEditorUtility.CountToString((ulong)lc.occupiedTexelCount), EditorStyles.miniLabel);

                GUIContent direct = EditorGUIUtility.TextContent("Direct: " + lc.minDirectSamples + " / " + lc.maxDirectSamples + " / " + lc.avgDirectSamples + "|min / max / avg samples per texel");
                GUILayout.Label(direct, EditorStyles.miniLabel);

                GUIContent gi = EditorGUIUtility.TextContent("GI: " + lc.minGISamples + " / " + lc.maxGISamples + " / " + lc.avgGISamples + "|min / max / avg samples per texel");
                GUILayout.Label(gi, EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Label("Occupied: N/A", EditorStyles.miniLabel);
                GUILayout.Label("Direct: N/A", EditorStyles.miniLabel);
                GUILayout.Label("GI: N/A", EditorStyles.miniLabel);
            }
            float mraysPerSec = Lightmapping.GetLightmapBakePerformance(index);
            if (mraysPerSec >= 0.0)
                GUILayout.Label(mraysPerSec.ToString("0.00") + " mrays/sec", EditorStyles.miniLabel);
            else
                GUILayout.Label("N/A mrays/sec", EditorStyles.miniLabel);

            GUILayout.EndVertical();
        }

        void LightmapMemoryStats(int index)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("Index: " + index, EditorStyles.miniLabel);

            LightmapMemory lightmapMemory = Lightmapping.GetLightmapMemory(index);
            GUILayout.Label("Lightmap data: " + lightmapMemory.lightmapDataSize.ToString("0.0") + " MB", EditorStyles.miniLabel);
            GUIContent lightmapTexturesSizeContent = null;
            if (lightmapMemory.lightmapTexturesSize > 0.0f)
                lightmapTexturesSizeContent = new GUIContent("Lightmap textures: " + SizeString(lightmapMemory.lightmapTexturesSize));
            else
                lightmapTexturesSizeContent = new GUIContent("Lightmap textures: N/A", "This lightmap has converged and is not owned by the Progressive Lightmapper anymore.");
            GUILayout.Label(lightmapTexturesSizeContent, EditorStyles.miniLabel);
            GUILayout.Label("Albedo data: " + lightmapMemory.albedoDataSize.ToString("0.0") + " MB", EditorStyles.miniLabel);
            GUILayout.Label("Albedo texture: " + lightmapMemory.albedoTextureSize.ToString("0.0") + " MB", EditorStyles.miniLabel);
            GUILayout.Label("Emissive data: " + lightmapMemory.emissiveDataSize.ToString("0.0") + " MB", EditorStyles.miniLabel);
            GUILayout.Label("Emissive texture: " + lightmapMemory.emissiveTextureSize.ToString("0.0") + " MB", EditorStyles.miniLabel);

            GUILayout.EndVertical();
        }
    }
} // namespace
