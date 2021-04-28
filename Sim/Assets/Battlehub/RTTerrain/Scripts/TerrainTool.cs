using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Battlehub.RTHandles;
using Battlehub.RTCommon;
using System.Linq;

namespace Battlehub.RTTerrain
{
    public interface ITerrainTool
    {
        bool Enabled
        {
            get; set;
        }

        int Spacing
        {
            get;
            set;
        }

        bool EnableZTest
        {
            get;
            set;
        }

        void ResetPosition();
        void CutHoles();
    }

    public class TerrainTool : MonoBehaviour, ITerrainTool
    {
        public enum Interpolation
        {
            Bilinear,
            Bicubic
        }

        private Interpolation m_prevInterpolation;

        [SerializeField]
        private bool m_enableZTest = true;

        public bool EnableZTest
        {
            get { return m_enableZTest; }
            set
            {
                m_enableZTest = value;
                if(!Enabled)
                {
                    return;
                }

                foreach(GameObject go in m_handles.Keys)
                {
                    TerrainToolHandle handle = go.GetComponent<TerrainToolHandle>();
                    handle.ZTest = value;
                }
            }
        }

        public int Spacing
        {
            get
            {
                if(!Enabled)
                {
                    return 0;
                }
                return m_state.Spacing;
            }
            set
            {
                if (!Enabled)
                {
                    return;
                }

                if (m_state.Spacing != value)
                {
                    m_state.Spacing = value;
                    m_count = m_state.Size / m_state.Spacing + 1;
                    if (Enabled)
                    {
                        Refresh();
                    }
                }
            }
        }
           
        public bool Enabled
        {
            get { return gameObject.activeSelf; }
            set { gameObject.SetActive(value); }
        }

        [SerializeField]
        public TerrainToolHandle m_handlePrefab;
        private Terrain m_activeTerrain;
        private TerrainToolState m_state;

        private IRTE m_editor;
        private int m_count;
        private float[,] m_lerpGrid;
        private Dictionary<GameObject, int> m_handles;
        private GameObject[] m_targetHandles;
        private bool m_isDragging;
        private IRuntimeSceneComponent m_sceneComponent;
        private TerrainToolHandle m_pointerOverHandle;
        private CachedBicubicInterpolator m_interpolator;
        private ITerrainCutoutMaskRenderer m_cutoutMaskRenderer;
        
        private void Awake()
        {
            m_editor = IOC.Resolve<IRTE>();
            IOC.RegisterFallback<ITerrainTool>(this);
            m_cutoutMaskRenderer = IOC.Resolve<ITerrainCutoutMaskRenderer>();
            m_cutoutMaskRenderer.ObjectImageLayer = m_editor.CameraLayerSettings.ResourcePreviewLayer;
            Enabled = false;
        }

        private void OnEnable()
        {
            m_editor.Undo.Store();

            m_editor.ActiveWindowChanged += OnActiveWindowChanged;
            m_editor.Selection.SelectionChanged += OnSelectionChanged;
            m_interpolator = new CachedBicubicInterpolator();
            m_activeTerrain = Terrain.activeTerrain;
            m_state = m_activeTerrain.GetComponent<TerrainToolState>();
            if (m_state == null)
            {
                m_state = m_activeTerrain.gameObject.AddComponent<TerrainToolState>();
                m_state.Size = (int)m_activeTerrain.terrainData.size.x;
                m_count = m_state.Size / m_state.Spacing + 1;
                m_state.Grid = new float[m_count * m_count];
                m_state.HeightMap = new float[m_activeTerrain.terrainData.heightmapResolution * m_activeTerrain.terrainData.heightmapResolution];
                float[,] hmap = m_activeTerrain.terrainData.GetHeights(0, 0, m_activeTerrain.terrainData.heightmapResolution, m_activeTerrain.terrainData.heightmapResolution);
                for (int i = 0; i < m_activeTerrain.terrainData.heightmapResolution; ++i)
                {
                    for(int j = 0; j < m_activeTerrain.terrainData.heightmapResolution; ++j)
                    {
                        m_state.HeightMap[i * m_activeTerrain.terrainData.heightmapResolution + j] = hmap[i, j];
                    }
                }
                m_state.CutoutTexture = m_cutoutMaskRenderer.CreateMask(null);
            }
            else
            {
                TryRefreshGrid();
            }
            InitHandles();

            OnSelectionChanged(null);
            OnActiveWindowChanged(m_editor.ActiveWindow);
            EnableZTest = EnableZTest;
        }

        private void OnDisable()
        {
            if (m_sceneComponent != null)
            {
                m_sceneComponent.PositionHandle.BeforeDrag.RemoveListener(OnBeforeDrag);
                m_sceneComponent.PositionHandle.Drop.RemoveListener(OnDrop);
            }

            m_sceneComponent = null;

            if (m_editor != null)
            {
                m_editor.ActiveWindowChanged -= OnActiveWindowChanged;
                m_editor.Selection.SelectionChanged -= OnSelectionChanged;
                m_targetHandles = null;
            }

            DestroyHandles();

            m_editor.Undo.Restore();
        }

        private void DestroyHandles()
        {
            if (m_handles != null)
            {
                foreach (KeyValuePair<GameObject, int> kvp in m_handles)
                {
                    GameObject handle = kvp.Key;
                    Destroy(handle);
                }
                m_handles = null;
            }

            m_editor.Selection.activeGameObject = null;
        }

        private void OnDestroy()
        {
            IOC.UnregisterFallback<ITerrainTool>(this);
            if(m_state != null)
            {
                if (m_state.CutoutTexture != null)
                {
                    Destroy(m_state.CutoutTexture);
                }
            }
        }

        public void ResetPosition()
        {
            if (m_targetHandles != null)
            {
                foreach (GameObject handle in m_targetHandles)
                {
                    Vector3 pos = handle.transform.localPosition;
                    pos.y = 0;
                    handle.transform.localPosition = pos;
                    UpdateTerrain(handle);
                }
            }
        }

        public void CutHoles()
        {
            GameObject[] objects = m_editor.Selection.gameObjects;
            if(objects != null)
            {
                objects = objects.Where(o => !m_handles.ContainsKey(o) && !o.GetComponent<Terrain>()).ToArray();
            }

            if(m_state.CutoutTexture != null)
            {
                Destroy(m_state.CutoutTexture);
            }

            m_state.CutoutTexture = m_cutoutMaskRenderer.CreateMask(objects);

            int width = m_activeTerrain.terrainData.heightmapResolution;
            int height = m_activeTerrain.terrainData.heightmapResolution;

            float[,] hmap = m_activeTerrain.terrainData.GetHeights(0, 0, width, height);

            for (int i = 0; i < width; ++i)
            {
                for (int j = 0; j < height; ++j)
                {
                    float u = (float)(i) / width;
                    float v = (float)(j) / height;
                    if (u >= 0 && u <= 1 && v >= 0 && v <= 1)
                    {
                        Color color = m_state.CutoutTexture.GetPixelBilinear(u, v);
                        if (Mathf.Approximately(color.a, 1))
                        {
                            hmap[j, i] = 0;
                        }
                        else
                        {
                            hmap[j, i] = m_state.HeightMap[j * width + i];
                        }
                    }
                }
            }

            m_activeTerrain.terrainData.SetHeights(0, 0, hmap);
        }

        private void Refresh()
        {
            DestroyHandles();
            m_editor.Undo.Restore();
            m_editor.Undo.Store();
            TryRefreshGrid();
            InitHandles();
        }

        private void TryRefreshGrid()
        {
            if(m_count * m_count == m_state.Grid.Length)
            {
                return;
            }

            m_count = m_state.Size / m_state.Spacing + 1;
            m_state.Grid = new float[m_count * m_count];

            TerrainData data = m_activeTerrain.terrainData;
            float[,] heightsvalues = data.GetHeights(
                0, 0, data.heightmapResolution, data.heightmapResolution);

            for (int i = 0; i < heightsvalues.GetLength(0); ++i)
            {
                for (int j = 0; j < heightsvalues.GetLength(1); ++j)
                {
                    heightsvalues[i, j] = 0;
                    m_state.HeightMap[i * heightsvalues.GetLength(1) + j] = 0;
                }
            }

            data.SetHeights(0, 0, heightsvalues);
        }

        private void InitHandles()
        {
            m_prevInterpolation = m_state.Interpolation;
            InitLerpGrid();

            if (m_handles != null)
            {
                foreach (KeyValuePair<GameObject, int> kvp in m_handles)
                {
                    GameObject handle = kvp.Key;
                    int z = kvp.Value / m_count;
                    int x = kvp.Value % m_count;
                    float y = m_state.Grid[kvp.Value] * m_activeTerrain.terrainData.heightmapScale.y;

                    handle.transform.position = new Vector3(x * m_state.Spacing, y, z * m_state.Spacing);
                }
            }
            else
            {
                m_handles = new Dictionary<GameObject, int>(m_state.Grid.Length);

                for (int x = 0; x < m_count; ++x)
                {
                    for (int z = 0; z < m_count; ++z)
                    {
                        m_handlePrefab.gameObject.SetActive(false);
                        TerrainToolHandle handle = Instantiate(m_handlePrefab, transform);
                        handle.gameObject.hideFlags = HideFlags.HideInHierarchy;

                        LockAxes lockAxes = handle.gameObject.AddComponent<LockAxes>();
                        lockAxes.PositionX = true;
                        lockAxes.PositionZ = true;

                        float y = m_state.Grid[z * m_count + x] * m_activeTerrain.terrainData.heightmapScale.y;
                        handle.transform.localPosition = new Vector3(x * m_state.Spacing, y, z * m_state.Spacing);
                        handle.name = "h " + x + "," + z;
                        handle.gameObject.SetActive(true);

                        m_handles.Add(handle.gameObject, z * m_count + x);
                    }
                }
            }
        }

        private void OnActiveWindowChanged(RuntimeWindow window)
        {
            if (m_editor.ActiveWindow == null)
            {
                return;
            }

            if (m_editor.ActiveWindow.WindowType == RuntimeWindowType.Scene)
            {
                if (m_sceneComponent != null)
                {
                    m_sceneComponent.PositionHandle.BeforeDrag.RemoveListener(OnBeforeDrag);
                    m_sceneComponent.PositionHandle.Drop.RemoveListener(OnDrop);
                }

                m_sceneComponent = m_editor.ActiveWindow.IOCContainer.Resolve<IRuntimeSceneComponent>();
                if (m_sceneComponent != null)
                {
                    m_sceneComponent.PositionHandle.BeforeDrag.AddListener(OnBeforeDrag);
                    m_sceneComponent.PositionHandle.Drop.AddListener(OnDrop);
                }
            }
        }

        private void OnSelectionChanged(Object[] unselectedObjects)
        {
            if(unselectedObjects != null)
            {
                foreach (Object obj in unselectedObjects)
                {
                    GameObject go = obj as GameObject;
                    if (go != null)
                    {
                        TerrainToolHandle handle = go.GetComponent<TerrainToolHandle>();
                        if (handle != null)
                        {
                            handle.IsSelected = false;
                        }
                    }
                }
            }
            
            if (m_editor.Selection.gameObjects == null || m_editor.Selection.gameObjects.Length == 0)
            {
                m_targetHandles = null;
            }
            else
            {
                m_targetHandles = m_editor.Selection.gameObjects.Where(go => go != null && m_handles.ContainsKey(go)).ToArray();
                foreach(GameObject go in m_targetHandles)
                {
                    TerrainToolHandle handle = go.GetComponent<TerrainToolHandle>();
                    handle.IsSelected = true;
                }
                if(m_targetHandles.Length == 0)
                {
                    m_targetHandles = null;
                }
            }
        }


        private void OnBeforeDrag(BaseHandle handle)
        {
            m_isDragging = m_targetHandles != null;
            if(m_isDragging)
            {
                GameObject[] targetHandles = m_targetHandles.ToArray();

                handle.EnableUndo = false;
                m_editor.Undo.BeginRecord();
                m_editor.Undo.CreateRecord(record => { return false; }, record =>
                {
                    if (targetHandles != null)
                    {
                        for (int i = 0; i < targetHandles.Length; ++i)
                        {
                            UpdateTerrain(targetHandles[i]);
                        }
                    }
                    return false;
                });
                if (targetHandles != null)
                {
                    for (int i = 0; i < targetHandles.Length; ++i)
                    {
                        m_editor.Undo.BeginRecordTransform(targetHandles[i].transform);
                    }
                }
                m_editor.Undo.EndRecord();
            }
        }

        private void OnDrop(BaseHandle handle)
        {
            if(m_isDragging)
            {
                GameObject[] targetHandles = m_targetHandles.ToArray();

                m_isDragging = false;
                m_editor.Undo.BeginRecord();
                if (targetHandles != null)
                {
                    for (int i = 0; i < targetHandles.Length; ++i)
                    {
                        m_editor.Undo.EndRecordTransform(targetHandles[i].transform);
                    }
                }
                m_editor.Undo.CreateRecord(record =>
                {
                    if (targetHandles != null)
                    {
                        for (int i = 0; i < targetHandles.Length; ++i)
                        {
                            UpdateTerrain(targetHandles[i]);
                        }
                    }
                    return false;
                }, record => { return false; });
                m_editor.Undo.EndRecord();
                handle.EnableUndo = true;
                
                if (m_targetHandles != null)
                {
                    for (int i = 0; i < m_targetHandles.Length; ++i)
                    {
                        UpdateTerrain(m_targetHandles[i]);
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if(m_activeTerrain == null)
            {
                gameObject.SetActive(false);
                return;
            }


            Transform terrainTransform = m_activeTerrain.transform;
            if(terrainTransform.position != gameObject.transform.position ||
               terrainTransform.rotation != gameObject.transform.rotation ||
               terrainTransform.localScale != gameObject.transform.localScale)
            {
                gameObject.transform.position = terrainTransform.position;
                gameObject.transform.rotation = terrainTransform.rotation;
                gameObject.transform.localScale = terrainTransform.localScale;
            }

            if(m_editor.ActiveWindow != null)
            {
                RuntimeWindow window = m_editor.ActiveWindow;
                if(window.WindowType == RuntimeWindowType.Scene)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(window.Pointer, out hit))
                    {
                        TryHitTerrainHandle(hit);
                    }
                }
            }

            if(m_state.Interpolation != m_prevInterpolation)
            {
                m_prevInterpolation = m_state.Interpolation;
                InitLerpGrid();
            }

            if(m_isDragging)
            {
                if (m_targetHandles != null && m_targetHandles.Length == 1)
                {
                    for(int i = 0; i < m_targetHandles.Length; ++i)
                    {
                        UpdateTerrain(m_targetHandles[i]);
                    }
                }
            }
        }

        private void TryHitTerrainHandle(RaycastHit hit)
        {
            TerrainToolHandle handle = hit.collider.GetComponent<TerrainToolHandle>();
            if (m_pointerOverHandle != handle)
            {
                if (m_pointerOverHandle != null)
                {
                    m_pointerOverHandle.IsPointerOver = false;
                }

                m_pointerOverHandle = handle;

                if (m_pointerOverHandle != null)
                {
                    m_pointerOverHandle.IsPointerOver = true;
                }
            }
        }

        private void UpdateTerrain(GameObject handle)
        {
            switch (m_state.Interpolation)
            {
                case Interpolation.Bilinear: UpdateTerrainBilinear(handle); break;
                case Interpolation.Bicubic: UpdateTerrainBicubic(handle); break;
            }
        }

        private void UpdateTerrainBilinear(GameObject handle)
        {
            int hid;
            if (!m_handles.TryGetValue(handle, out hid))
            {
                Debug.LogError("Handle is not found!");
            }
            
            Vector3 pos = handle.transform.localPosition;
            UpdateTerrainBilinear(hid, pos);
        }

        private void UpdateTerrainBilinear(int hid, Vector3 position)
        {
            TerrainData data = m_activeTerrain.terrainData;
            float[] grid = m_state.Grid;
            grid[hid] = position.y / data.heightmapScale.y;

            m_lerpGrid[0, 0] = grid[hid - m_count - 1];
            m_lerpGrid[0, 1] = grid[hid - m_count];
            m_lerpGrid[0, 2] = grid[hid - m_count + 1];
            m_lerpGrid[1, 0] = grid[hid - 1];
            m_lerpGrid[1, 1] = grid[hid];
            m_lerpGrid[1, 2] = grid[hid + 1];
            m_lerpGrid[2, 0] = grid[hid + m_count - 1];
            m_lerpGrid[2, 1] = grid[hid + m_count];
            m_lerpGrid[2, 2] = grid[hid + m_count + 1];

            Vector2Int blockSize = new Vector2Int(
                (int)(m_state.Spacing / data.heightmapScale.x),
                (int)(m_state.Spacing / data.heightmapScale.z));

            Vector2Int hPos = new Vector2Int(
                (int)(position.x / data.heightmapScale.x),
                (int)(position.z / data.heightmapScale.z));

            hPos -= blockSize;

            float[,] heightsvalues = data.GetHeights(
                hPos.x, hPos.y,
                blockSize.x * 2 + 1, blockSize.y * 2 + 1);

            for (int gy = 0; gy < 2; gy++)
            {
                int baseY = gy * blockSize.y;

                for (int gx = 0; gx < 2; gx++)
                {
                    int baseX = gx * blockSize.x;

                    for (int y = 0; y < blockSize.y; y++)
                    {
                        float ty = (float)y / blockSize.y;
                        for (int x = 0; x < blockSize.x; x++)
                        {
                            float tx = (float)x / blockSize.x;
                            heightsvalues[baseY + y, baseX + x] =
                                Mathf.Lerp(
                                    Mathf.Lerp(m_lerpGrid[gy, gx], m_lerpGrid[gy, gx + 1], tx),
                                    Mathf.Lerp(m_lerpGrid[gy + 1, gx], m_lerpGrid[gy + 1, gx + 1], tx),
                                    ty);
                        }
                    }
                }
            }

            data.SetHeights(hPos.x, hPos.y, heightsvalues);
        }

        private void UpdateTerrainBicubic(GameObject handle)
        {
            Vector3 position = handle.transform.localPosition;// Position;
            int hid = -1;
            m_handles.TryGetValue(handle, out hid);

            UpdateTerrainBicubic(hid, position);
        }

        private void UpdateTerrainBicubic(int hid, Vector3 position)
        {
            var data = m_activeTerrain.terrainData;
            if (hid >= 0)
            {
                m_state.Grid[hid] = position.y / data.heightmapScale.y;
            }
            else
            {
                Debug.LogError("Gizmo is not found!");
            }

            int2 iidx = new int2(hid % m_count, hid / m_count);

            for (int y = 0; y < 7; y++)
            {
                int _y = math.clamp(iidx.y - 3 + y, 0, m_count - 1);

                for (int x = 0; x < 7; x++)
                {
                    int _x = math.clamp(iidx.x - 3 + x, 0, m_count - 1);
                    m_lerpGrid[y, x] = m_state.Grid[m_count * _y + _x];
                }
            }

            float2 heightmapScale = ((float3)data.heightmapScale).xz;
            float2 pos = ((float3)position).xz;
            int2 block_size = (int2)(new float2(m_state.Spacing) / heightmapScale);

            int2 hPos = (int2)(pos / heightmapScale);
            hPos -= block_size * 2;

            int2 max_block = new int2(block_size.x * 4, block_size.y * 4);
            int res = data.heightmapResolution;// - 1;
            RectInt r = new RectInt(hPos.x, hPos.y, max_block.x, max_block.y);
            r.xMin = math.clamp(r.xMin, 0, res);
            r.xMax = math.clamp(r.xMax, 0, res);
            r.yMin = math.clamp(r.yMin, 0, res);
            r.yMax = math.clamp(r.yMax, 0, res);

            float[,] hmap = data.GetHeights(r.x, r.y, r.width, r.height);

            for (int gy = 0; gy < 4; gy++)
            {
                int base_y = gy * block_size.y;

                for (int gx = 0; gx < 4; gx++)
                {
                    int base_x = gx * block_size.x;

                    m_interpolator.UpdateCoefficients(new float4x4(
                        m_lerpGrid[gy,     gx], m_lerpGrid[gy,     gx + 1], m_lerpGrid[gy,     gx + 2], m_lerpGrid[gy, gx + 3],
                        m_lerpGrid[gy + 1, gx], m_lerpGrid[gy + 1, gx + 1], m_lerpGrid[gy + 1, gx + 2], m_lerpGrid[gy + 1, gx + 3],
                        m_lerpGrid[gy + 2, gx], m_lerpGrid[gy + 2, gx + 1], m_lerpGrid[gy + 2, gx + 2], m_lerpGrid[gy + 2, gx + 3],
                        m_lerpGrid[gy + 3, gx], m_lerpGrid[gy + 3, gx + 1], m_lerpGrid[gy + 3, gx + 2], m_lerpGrid[gy + 3, gx + 3]
                    ));

                    for (int y = 0; y < block_size.y; y++)
                    {
                        int _y = hPos.y + base_y + y;
                        if (_y >= r.yMin && _y < r.yMax)
                        {
                            float ty = (float)y / block_size.y;

                            for (int x = 0; x < block_size.x; x++)
                            {
                                int _x = hPos.x + base_x + x;
                                if (_x >= r.xMin && _x < r.xMax)
                                {
                                    float tx = (float)x / block_size.x;

                                    try
                                    {
                                        float height = m_interpolator.GetValue(tx, ty);
                                        float u = (float)(r.x + (_x - r.xMin)) / data.heightmapResolution;
                                        float v = (float)(r.y + (_y - r.yMin)) / data.heightmapResolution;
                                        if (u >= 0 && u <= 1 && v >= 0 && v <= 1)
                                        {
                                            Color color = m_state.CutoutTexture.GetPixelBilinear(u, v);
                                            if (Mathf.Approximately(color.a, 1))
                                            {
                                                hmap[_y - r.yMin, _x - r.xMin] = 0;
                                            }
                                            else
                                            {
                                                hmap[_y - r.yMin, _x - r.xMin] = height;
                                            }
                                        }
                                        else
                                        {
                                            hmap[_y - r.yMin, _x - r.xMin] = height;
                                        }
                                        
                                         m_state.HeightMap[(r.y + (_y - r.yMin)) * m_activeTerrain.terrainData.heightmapResolution + r.x + (_x - r.xMin)] = height;
                                    }
                                    catch
                                    {
                                        Debug.LogError("!!!!!!!!!!!!!!!!!!!");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            data.SetHeights(r.x, r.y, hmap);
        }

        private void InitLerpGrid()
        {
            if (m_state.Interpolation == Interpolation.Bilinear)
            {
                m_lerpGrid = new float[3, 3];
            }
            else
            {
                m_lerpGrid = new float[7, 7];
            }
        }
    }
}
